using System.Diagnostics;
using System.Text;
using System.Text.Json;
using H2l.Application.Connectors.Contracts;
using H2l.Application.Connectors.Services;
using H2l.Domain.Connectors;
using H2l.Infrastructure.Connectors.Security;

namespace H2l.Infrastructure.Connectors;

public sealed class Camunda8WorkflowConnector : IPullWorkflowConnector
{
    public const string JobType = "h2l-human-task";
    public const string ErrorTaskExpired = "H2L_TASK_EXPIRED";
    public const string ErrorTaskCancelled = "H2L_TASK_CANCELLED";
    public const string ErrorFormNotFound = "H2L_FORM_NOT_FOUND";
    public const string ErrorAssigneeNotFound = "H2L_ASSIGNEE_NOT_FOUND";

    private readonly IConnectorHttpClientFactory _httpClientFactory;
    private readonly ISsrfGuard _ssrfGuard;
    private readonly ITokenProvider _tokenProvider;
    private readonly IConnectorPayloadBuilder _payloadBuilder;

    public ConnectorType ConnectorType => ConnectorType.Camunda8;

    public Camunda8WorkflowConnector(
        IConnectorHttpClientFactory httpClientFactory,
        ISsrfGuard ssrfGuard,
        ITokenProvider tokenProvider,
        IConnectorPayloadBuilder payloadBuilder)
    {
        _httpClientFactory = httpClientFactory;
        _ssrfGuard = ssrfGuard;
        _tokenProvider = tokenProvider;
        _payloadBuilder = payloadBuilder;
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
            return new ConnectorTestResult(false, "Camunda8", null, sw.ElapsedMilliseconds, ssrf.Error.Message);
        }

        try
        {
            var topologyUrl = $"{config.BaseUrl.TrimEnd('/')}/v2/topology";
            using var client = _httpClientFactory.CreateClient(config.CustomCaCertificatePem, TimeSpan.FromSeconds(15));
            using var request = new HttpRequestMessage(HttpMethod.Get, topologyUrl);
            await ApplyAuthorizationAsync(request, config, cancellationToken);

            using var response = await client.SendAsync(request, cancellationToken);
            sw.Stop();

            if (!response.IsSuccessStatusCode)
            {
                return new ConnectorTestResult(
                    false,
                    "Camunda8",
                    null,
                    sw.ElapsedMilliseconds,
                    $"Camunda 8 topology endpoint responded with HTTP {(int)response.StatusCode}.");
            }

            string? version = null;
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!string.IsNullOrWhiteSpace(body))
            {
                try
                {
                    using var doc = JsonDocument.Parse(body);
                    if (doc.RootElement.TryGetProperty("gatewayVersion", out var versionProp))
                    {
                        version = versionProp.GetString();
                    }
                }
                catch (JsonException)
                {
                    // Non-JSON or unexpected topology body is still reachable.
                }
            }

            return new ConnectorTestResult(true, "Camunda8", version, sw.ElapsedMilliseconds, null);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or InvalidOperationException)
        {
            sw.Stop();
            return new ConnectorTestResult(false, "Camunda8", null, sw.ElapsedMilliseconds, ex.Message);
        }
    }

    public async Task<ConnectorDeliveryResult> CompleteTaskAsync(
        ConnectorRuntimeConfig config,
        ConnectorTaskContext taskContext,
        ConnectorTaskCompletionPayload payload,
        CancellationToken cancellationToken = default)
    {
        var ssrf = await _ssrfGuard.ValidateUrlAsync(config.BaseUrl, cancellationToken);
        if (ssrf.IsFailure)
        {
            return ConnectorDeliveryResult.PermanentFailure($"Camunda URL rejected by SSRF policy: {ssrf.Error.Code}");
        }

        var variables = _payloadBuilder.BuildCamundaCompletionVariables(taskContext, payload);
        var body = JsonSerializer.Serialize(new { variables });
        var endpoint = $"{config.BaseUrl.TrimEnd('/')}/v2/jobs/{Uri.EscapeDataString(taskContext.ExternalTaskId)}/completion";

        return await SendCommandAsync(HttpMethod.Post, endpoint, body, config, cancellationToken);
    }

    public async Task<ConnectorDeliveryResult> CancelTaskAsync(
        ConnectorRuntimeConfig config,
        ConnectorTaskContext taskContext,
        string status,
        string reason,
        CancellationToken cancellationToken = default)
    {
        var ssrf = await _ssrfGuard.ValidateUrlAsync(config.BaseUrl, cancellationToken);
        if (ssrf.IsFailure)
        {
            return ConnectorDeliveryResult.PermanentFailure($"Camunda URL rejected by SSRF policy: {ssrf.Error.Code}");
        }

        var errorCode = MapBpmnErrorCode(status, reason);
        var body = JsonSerializer.Serialize(new
        {
            errorCode,
            errorMessage = reason
        });

        var endpoint = $"{config.BaseUrl.TrimEnd('/')}/v2/jobs/{Uri.EscapeDataString(taskContext.ExternalTaskId)}/error";
        return await SendCommandAsync(HttpMethod.Post, endpoint, body, config, cancellationToken);
    }

    public async Task<ConnectorDeliveryResult> ExtendLockAsync(
        ConnectorRuntimeConfig config,
        ConnectorTaskContext taskContext,
        TimeSpan newTimeout,
        CancellationToken cancellationToken = default)
    {
        var ssrf = await _ssrfGuard.ValidateUrlAsync(config.BaseUrl, cancellationToken);
        if (ssrf.IsFailure)
        {
            return ConnectorDeliveryResult.PermanentFailure($"Camunda URL rejected by SSRF policy: {ssrf.Error.Code}");
        }

        var timeoutMs = Math.Max(60_000L, (long)newTimeout.TotalMilliseconds);
        var body = JsonSerializer.Serialize(new
        {
            changeset = new { timeout = timeoutMs }
        });

        var endpoint = $"{config.BaseUrl.TrimEnd('/')}/v2/jobs/{Uri.EscapeDataString(taskContext.ExternalTaskId)}";
        return await SendCommandAsync(HttpMethod.Patch, endpoint, body, config, cancellationToken);
    }

    public async Task<IReadOnlyList<Camunda8ActivatedJob>> ActivateJobsAsync(
        ConnectorRuntimeConfig config,
        int maxJobsToActivate,
        TimeSpan lockTimeout,
        CancellationToken cancellationToken = default)
    {
        var ssrf = await _ssrfGuard.ValidateUrlAsync(config.BaseUrl, cancellationToken);
        if (ssrf.IsFailure)
        {
            return [];
        }

        var workerName = $"h2l-worker-{config.TenantId:N}-{config.ConnectorId:N}";
        var requestBody = JsonSerializer.Serialize(new
        {
            type = JobType,
            worker = workerName,
            timeout = (long)lockTimeout.TotalMilliseconds,
            maxJobsToActivate
        });

        var endpoint = $"{config.BaseUrl.TrimEnd('/')}/v2/jobs/activation";
        using var client = _httpClientFactory.CreateClient(config.CustomCaCertificatePem, TimeSpan.FromSeconds(15));
        using var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
        {
            Content = new StringContent(requestBody, Encoding.UTF8, "application/json")
        };

        await ApplyAuthorizationAsync(request, config, cancellationToken);

        using var response = await client.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return [];
        }

        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        using var doc = JsonDocument.Parse(json);
        if (!doc.RootElement.TryGetProperty("jobs", out var jobsElement) ||
            jobsElement.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        var results = new List<Camunda8ActivatedJob>();
        foreach (var jobElem in jobsElement.EnumerateArray())
        {
            string? jobKey = null;
            if (jobElem.TryGetProperty("jobKey", out var keyProp))
            {
                jobKey = keyProp.ValueKind == JsonValueKind.String
                    ? keyProp.GetString()
                    : keyProp.GetRawText();
            }

            if (string.IsNullOrWhiteSpace(jobKey))
            {
                continue;
            }

            var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (jobElem.TryGetProperty("customHeaders", out var headersProp) &&
                headersProp.ValueKind == JsonValueKind.Object)
            {
                foreach (var prop in headersProp.EnumerateObject())
                {
                    if (prop.Value.ValueKind == JsonValueKind.String)
                    {
                        headers[prop.Name] = prop.Value.GetString() ?? string.Empty;
                    }
                    else
                    {
                        headers[prop.Name] = prop.Value.GetRawText();
                    }
                }
            }

            var variables = new Dictionary<string, JsonElement>(StringComparer.Ordinal);
            if (jobElem.TryGetProperty("variables", out var varsProp) &&
                varsProp.ValueKind == JsonValueKind.Object)
            {
                foreach (var prop in varsProp.EnumerateObject())
                {
                    variables[prop.Name] = prop.Value.Clone();
                }
            }

            results.Add(new Camunda8ActivatedJob(jobKey, headers, variables));
        }

        return results;
    }

    public static string MapBpmnErrorCode(string status, string reason)
    {
        if (string.Equals(reason, ErrorFormNotFound, StringComparison.OrdinalIgnoreCase))
        {
            return ErrorFormNotFound;
        }

        if (string.Equals(reason, ErrorAssigneeNotFound, StringComparison.OrdinalIgnoreCase))
        {
            return ErrorAssigneeNotFound;
        }

        if (string.Equals(status, "expired", StringComparison.OrdinalIgnoreCase)
            || string.Equals(reason, ErrorTaskExpired, StringComparison.OrdinalIgnoreCase))
        {
            return ErrorTaskExpired;
        }

        return ErrorTaskCancelled;
    }

    private async Task<ConnectorDeliveryResult> SendCommandAsync(
        HttpMethod method,
        string endpoint,
        string rawBody,
        ConnectorRuntimeConfig config,
        CancellationToken cancellationToken)
    {
        try
        {
            using var client = _httpClientFactory.CreateClient(config.CustomCaCertificatePem);
            using var request = new HttpRequestMessage(method, endpoint)
            {
                Content = new StringContent(rawBody, Encoding.UTF8, "application/json")
            };

            await ApplyAuthorizationAsync(request, config, cancellationToken);

            using var response = await client.SendAsync(request, cancellationToken);
            var statusCode = (int)response.StatusCode;
            if (response.IsSuccessStatusCode)
            {
                return ConnectorDeliveryResult.Success(statusCode);
            }

            TimeSpan? retryAfter = response.Headers.RetryAfter?.Delta;
            return ConnectorDeliveryResult.ClassifyHttpFailure(
                statusCode,
                response.ReasonPhrase ?? "Error",
                retryAfter);
        }
        catch (TaskCanceledException)
        {
            return ConnectorDeliveryResult.TransientFailure("Camunda 8 request timed out.");
        }
        catch (HttpRequestException)
        {
            return ConnectorDeliveryResult.TransientFailure("Network error occurred while communicating with Camunda 8.");
        }
        catch (InvalidOperationException ex)
        {
            return ConnectorDeliveryResult.PermanentFailure(ex.Message);
        }
    }

    private async Task ApplyAuthorizationAsync(
        HttpRequestMessage request,
        ConnectorRuntimeConfig config,
        CancellationToken cancellationToken)
    {
        var authHeader = await _tokenProvider.GetAuthorizationHeaderValueAsync(config, cancellationToken);
        if (!string.IsNullOrWhiteSpace(authHeader))
        {
            request.Headers.TryAddWithoutValidation("Authorization", authHeader);
        }
    }
}
