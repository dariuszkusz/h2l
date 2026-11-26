using System.Diagnostics;
using System.Text;
using H2l.Application.Common;
using H2l.Application.Connectors.Contracts;
using H2l.Application.Connectors.Services;
using H2l.Domain.Connectors;
using H2l.Infrastructure.Connectors.Security;

namespace H2l.Infrastructure.Connectors;

public sealed class N8nWorkflowConnector : IWorkflowConnector
{
    private readonly IConnectorHttpClientFactory _httpClientFactory;
    private readonly ISsrfGuard _ssrfGuard;
    private readonly IHmacWebhookSigner _hmacSigner;
    private readonly IConnectorPayloadBuilder _payloadBuilder;
    private readonly IDateTimeProvider _dateTimeProvider;

    public ConnectorType ConnectorType => ConnectorType.N8n;

    public N8nWorkflowConnector(
        IConnectorHttpClientFactory httpClientFactory,
        ISsrfGuard ssrfGuard,
        IHmacWebhookSigner hmacSigner,
        IConnectorPayloadBuilder payloadBuilder,
        IDateTimeProvider dateTimeProvider)
    {
        _httpClientFactory = httpClientFactory;
        _ssrfGuard = ssrfGuard;
        _hmacSigner = hmacSigner;
        _payloadBuilder = payloadBuilder;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<ConnectorTestResult> TestConnectionAsync(
        ConnectorRuntimeConfig config,
        CancellationToken cancellationToken = default)
    {
        var sw = Stopwatch.StartNew();
        var baseSsrf = await _ssrfGuard.ValidateUrlAsync(config.BaseUrl, cancellationToken);
        if (baseSsrf.IsFailure)
        {
            sw.Stop();
            return new ConnectorTestResult(false, "n8n", null, sw.ElapsedMilliseconds, baseSsrf.Error.Message);
        }

        var normalizedBase = config.BaseUrl.TrimEnd('/');
        var hasApiKey = !string.IsNullOrWhiteSpace(config.Credentials?.ApiKey);
        var probeUrl = hasApiKey
            ? $"{normalizedBase}/api/v1/workflows?limit=1"
            : $"{normalizedBase}/healthz";

        try
        {
            using var client = _httpClientFactory.CreateClient(config.CustomCaCertificatePem, TimeSpan.FromSeconds(15));
            using var request = new HttpRequestMessage(HttpMethod.Get, probeUrl);

            if (hasApiKey)
            {
                var headerName = string.IsNullOrWhiteSpace(config.Credentials?.ApiKeyHeaderName)
                    ? "X-N8N-API-KEY"
                    : config.Credentials!.ApiKeyHeaderName!.Trim();
                request.Headers.TryAddWithoutValidation(headerName, config.Credentials!.ApiKey);
            }

            using var response = await client.SendAsync(request, cancellationToken);
            sw.Stop();

            if (response.IsSuccessStatusCode)
            {
                return new ConnectorTestResult(true, "n8n", null, sw.ElapsedMilliseconds, null);
            }

            return new ConnectorTestResult(
                false,
                "n8n",
                null,
                sw.ElapsedMilliseconds,
                $"n8n instance responded with HTTP {(int)response.StatusCode}.");
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or InvalidOperationException)
        {
            sw.Stop();
            return new ConnectorTestResult(false, "n8n", null, sw.ElapsedMilliseconds, ex.Message);
        }
    }

    public Task<ConnectorDeliveryResult> CompleteTaskAsync(
        ConnectorRuntimeConfig config,
        ConnectorTaskContext taskContext,
        ConnectorTaskCompletionPayload payload,
        CancellationToken cancellationToken = default)
    {
        var body = _payloadBuilder.BuildWebhookCompletionBody(taskContext, payload);
        return SendResumeCallbackAsync(config, taskContext, body, cancellationToken);
    }

    public Task<ConnectorDeliveryResult> CancelTaskAsync(
        ConnectorRuntimeConfig config,
        ConnectorTaskContext taskContext,
        string status,
        string reason,
        CancellationToken cancellationToken = default)
    {
        var body = _payloadBuilder.BuildWebhookCancellationBody(taskContext, status, reason);
        return SendResumeCallbackAsync(config, taskContext, body, cancellationToken);
    }

    private async Task<ConnectorDeliveryResult> SendResumeCallbackAsync(
        ConnectorRuntimeConfig config,
        ConnectorTaskContext taskContext,
        string rawBody,
        CancellationToken cancellationToken)
    {
        var targetUrl = !string.IsNullOrWhiteSpace(taskContext.ResumeUrl)
            ? taskContext.ResumeUrl
            : config.CallbackUrl;

        if (string.IsNullOrWhiteSpace(targetUrl))
        {
            return ConnectorDeliveryResult.PermanentFailure(
                "n8n resumeUrl is missing for this task and no fallback callbackUrl is configured.");
        }

        var ssrf = await _ssrfGuard.ValidateUrlAsync(targetUrl, cancellationToken);
        if (ssrf.IsFailure)
        {
            return ConnectorDeliveryResult.PermanentFailure($"n8n resumeUrl rejected by SSRF policy: {ssrf.Error.Code}");
        }

        try
        {
            using var client = _httpClientFactory.CreateClient(config.CustomCaCertificatePem);
            using var request = new HttpRequestMessage(HttpMethod.Post, ssrf.Value)
            {
                Content = new StringContent(rawBody, Encoding.UTF8, "application/json")
            };

            request.Headers.TryAddWithoutValidation("X-H2L-Idempotency-Key", taskContext.IdempotencyKey);

            if (!string.IsNullOrWhiteSpace(config.Credentials?.HmacSecret))
            {
                var timestamp = _dateTimeProvider.UtcNow.ToUnixTimeSeconds();
                var signature = _hmacSigner.ComputeSignature(config.Credentials.HmacSecret, timestamp, rawBody);
                request.Headers.TryAddWithoutValidation("X-H2L-Timestamp", timestamp.ToString());
                request.Headers.TryAddWithoutValidation("X-H2L-Signature", signature);
            }
            else if (!string.IsNullOrWhiteSpace(config.Credentials?.BearerToken))
            {
                request.Headers.TryAddWithoutValidation("Authorization", $"Bearer {config.Credentials.BearerToken.Trim()}");
            }
            else if (!string.IsNullOrWhiteSpace(config.Credentials?.ApiKey))
            {
                var headerName = string.IsNullOrWhiteSpace(config.Credentials.ApiKeyHeaderName)
                    ? "X-API-Key"
                    : config.Credentials.ApiKeyHeaderName.Trim();
                request.Headers.TryAddWithoutValidation(headerName, config.Credentials.ApiKey);
            }

            using var response = await client.SendAsync(request, cancellationToken);
            var statusCode = (int)response.StatusCode;
            if (response.IsSuccessStatusCode)
            {
                return ConnectorDeliveryResult.Success(statusCode);
            }

            if (statusCode is >= 300 and < 400)
            {
                return ConnectorDeliveryResult.PermanentFailure(
                    $"n8n resume endpoint returned HTTP {statusCode} redirect, which is blocked for SSRF protection.",
                    statusCode);
            }

            TimeSpan? retryAfter = response.Headers.RetryAfter?.Delta;
            return ConnectorDeliveryResult.ClassifyHttpFailure(
                statusCode,
                response.ReasonPhrase ?? "Error",
                retryAfter);
        }
        catch (TaskCanceledException)
        {
            return ConnectorDeliveryResult.TransientFailure("n8n resume callback timed out.");
        }
        catch (HttpRequestException)
        {
            return ConnectorDeliveryResult.TransientFailure("Network error occurred while delivering n8n resume callback.");
        }
        catch (InvalidOperationException ex)
        {
            return ConnectorDeliveryResult.PermanentFailure(ex.Message);
        }
    }
}
