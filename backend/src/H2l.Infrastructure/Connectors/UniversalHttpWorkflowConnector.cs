using System.Diagnostics;
using System.Text;
using H2l.Application.Common;
using H2l.Application.Connectors.Contracts;
using H2l.Application.Connectors.Services;
using H2l.Domain.Connectors;
using H2l.Infrastructure.Connectors.Security;

namespace H2l.Infrastructure.Connectors;

public sealed class UniversalHttpWorkflowConnector : IWorkflowConnector
{
    private readonly IConnectorHttpClientFactory _httpClientFactory;
    private readonly ISsrfGuard _ssrfGuard;
    private readonly IHmacWebhookSigner _hmacSigner;
    private readonly ITokenProvider _tokenProvider;
    private readonly IConnectorPayloadBuilder _payloadBuilder;
    private readonly IDateTimeProvider _dateTimeProvider;

    public ConnectorType ConnectorType => ConnectorType.UniversalHttp;

    public UniversalHttpWorkflowConnector(
        IConnectorHttpClientFactory httpClientFactory,
        ISsrfGuard ssrfGuard,
        IHmacWebhookSigner hmacSigner,
        ITokenProvider tokenProvider,
        IConnectorPayloadBuilder payloadBuilder,
        IDateTimeProvider dateTimeProvider)
    {
        _httpClientFactory = httpClientFactory;
        _ssrfGuard = ssrfGuard;
        _hmacSigner = hmacSigner;
        _tokenProvider = tokenProvider;
        _payloadBuilder = payloadBuilder;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<ConnectorTestResult> TestConnectionAsync(
        ConnectorRuntimeConfig config,
        CancellationToken cancellationToken = default)
    {
        var sw = Stopwatch.StartNew();
        var ssrf = await _ssrfGuard.ValidateUrlAsync(config.BaseUrl, cancellationToken);
        if (ssrf.IsFailure)
        {
            sw.Stop();
            return new ConnectorTestResult(false, "UniversalHttp", null, sw.ElapsedMilliseconds, ssrf.Error.Message);
        }

        try
        {
            using var client = _httpClientFactory.CreateClient(config.CustomCaCertificatePem, TimeSpan.FromSeconds(15));
            using var request = new HttpRequestMessage(HttpMethod.Get, ssrf.Value);
            await ApplyAuthHeadersAsync(request, config, string.Empty, cancellationToken);

            using var response = await client.SendAsync(request, cancellationToken);
            sw.Stop();

            if (response.IsSuccessStatusCode || (int)response.StatusCode == 405)
            {
                return new ConnectorTestResult(true, "UniversalHttp", null, sw.ElapsedMilliseconds, null);
            }

            return new ConnectorTestResult(
                false,
                "UniversalHttp",
                null,
                sw.ElapsedMilliseconds,
                $"Endpoint responded with HTTP {(int)response.StatusCode}.");
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or InvalidOperationException)
        {
            sw.Stop();
            return new ConnectorTestResult(false, "UniversalHttp", null, sw.ElapsedMilliseconds, ex.Message);
        }
    }

    public Task<ConnectorDeliveryResult> CompleteTaskAsync(
        ConnectorRuntimeConfig config,
        ConnectorTaskContext taskContext,
        ConnectorTaskCompletionPayload payload,
        CancellationToken cancellationToken = default)
    {
        var body = _payloadBuilder.BuildWebhookCompletionBody(taskContext, payload);
        return SendWebhookCallbackAsync(config, taskContext, body, cancellationToken);
    }

    public Task<ConnectorDeliveryResult> CancelTaskAsync(
        ConnectorRuntimeConfig config,
        ConnectorTaskContext taskContext,
        string status,
        string reason,
        CancellationToken cancellationToken = default)
    {
        var body = _payloadBuilder.BuildWebhookCancellationBody(taskContext, status, reason);
        return SendWebhookCallbackAsync(config, taskContext, body, cancellationToken);
    }

    private async Task<ConnectorDeliveryResult> SendWebhookCallbackAsync(
        ConnectorRuntimeConfig config,
        ConnectorTaskContext taskContext,
        string rawBody,
        CancellationToken cancellationToken)
    {
        var targetUrl = !string.IsNullOrWhiteSpace(taskContext.ResumeUrl)
            ? taskContext.ResumeUrl
            : !string.IsNullOrWhiteSpace(config.CallbackUrl)
                ? config.CallbackUrl
                : config.BaseUrl;

        var ssrf = await _ssrfGuard.ValidateUrlAsync(targetUrl, cancellationToken);
        if (ssrf.IsFailure)
        {
            return ConnectorDeliveryResult.PermanentFailure($"Callback URL rejected by SSRF policy: {ssrf.Error.Code}");
        }

        try
        {
            using var client = _httpClientFactory.CreateClient(config.CustomCaCertificatePem);
            using var request = new HttpRequestMessage(HttpMethod.Post, ssrf.Value)
            {
                Content = new StringContent(rawBody, Encoding.UTF8, "application/json")
            };

            request.Headers.TryAddWithoutValidation("X-H2L-Idempotency-Key", taskContext.IdempotencyKey);
            await ApplyAuthHeadersAsync(request, config, rawBody, cancellationToken);

            using var response = await client.SendAsync(request, cancellationToken);
            var statusCode = (int)response.StatusCode;
            if (response.IsSuccessStatusCode)
            {
                return ConnectorDeliveryResult.Success(statusCode);
            }

            if (statusCode is >= 300 and < 400)
            {
                return ConnectorDeliveryResult.PermanentFailure(
                    $"Callback endpoint returned HTTP {statusCode} redirect, which is blocked for SSRF protection.",
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
            return ConnectorDeliveryResult.TransientFailure("Callback request timed out.");
        }
        catch (HttpRequestException)
        {
            return ConnectorDeliveryResult.TransientFailure("Network error occurred while delivering callback.");
        }
        catch (InvalidOperationException ex)
        {
            return ConnectorDeliveryResult.PermanentFailure(ex.Message);
        }
    }

    private async Task ApplyAuthHeadersAsync(
        HttpRequestMessage request,
        ConnectorRuntimeConfig config,
        string rawBody,
        CancellationToken cancellationToken)
    {
        if (config.AuthType == ConnectorAuthType.HmacSha256 &&
            !string.IsNullOrWhiteSpace(config.Credentials?.HmacSecret))
        {
            var timestamp = _dateTimeProvider.UtcNow.ToUnixTimeSeconds();
            var signature = _hmacSigner.ComputeSignature(config.Credentials.HmacSecret, timestamp, rawBody);
            request.Headers.TryAddWithoutValidation("X-H2L-Timestamp", timestamp.ToString());
            request.Headers.TryAddWithoutValidation("X-H2L-Signature", signature);
            return;
        }

        if (config.AuthType == ConnectorAuthType.ApiKey &&
            !string.IsNullOrWhiteSpace(config.Credentials?.ApiKey))
        {
            var headerName = string.IsNullOrWhiteSpace(config.Credentials.ApiKeyHeaderName)
                ? "X-API-Key"
                : config.Credentials.ApiKeyHeaderName.Trim();
            request.Headers.TryAddWithoutValidation(headerName, config.Credentials.ApiKey);
            return;
        }

        var authHeader = await _tokenProvider.GetAuthorizationHeaderValueAsync(config, cancellationToken);
        if (!string.IsNullOrWhiteSpace(authHeader))
        {
            request.Headers.TryAddWithoutValidation("Authorization", authHeader);
        }
    }
}
