using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using H2l.Application.Common;
using H2l.Application.Connectors.Commands;
using H2l.Application.Connectors.Contracts;
using H2l.Application.Connectors.DTOs;
using H2l.Application.Connectors.Services;
using H2l.Domain.Common;
using H2l.Domain.Connectors;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Wolverine;

namespace H2l.WebApi.Controllers;

[Route("api/v1/webhooks")]
[Tags("Connectors")]
[AllowAnonymous]
public sealed class WebhooksController : ApiControllerBase
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly IWorkflowConnectorRepository _connectorRepository;
    private readonly IConnectorConfigSerializer _configSerializer;
    private readonly IHmacWebhookSigner _hmacSigner;
    private readonly ITenantSetter _tenantSetter;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IMessageBus _messageBus;

    public WebhooksController(
        IWorkflowConnectorRepository connectorRepository,
        IConnectorConfigSerializer configSerializer,
        IHmacWebhookSigner hmacSigner,
        ITenantSetter tenantSetter,
        IDateTimeProvider dateTimeProvider,
        IMessageBus messageBus)
    {
        _connectorRepository = connectorRepository;
        _configSerializer = configSerializer;
        _hmacSigner = hmacSigner;
        _tenantSetter = tenantSetter;
        _dateTimeProvider = dateTimeProvider;
        _messageBus = messageBus;
    }

    [HttpPost("connectors/{connectorId:guid}/tasks")]
    [ProducesResponseType(typeof(InboundWorkflowTaskResponse), StatusCodes.Status202Accepted)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<IActionResult> ReceiveConnectorTaskWebhook(
        [FromRoute] Guid connectorId,
        CancellationToken cancellationToken)
    {
        var connector = await _connectorRepository.GetByIdIgnoringTenantFilterAsync(connectorId, cancellationToken);
        if (connector is null || !connector.IsActive)
        {
            return ToProblemResult(Error.NotFound("CONNECTOR_NOT_FOUND", "Active workflow connector was not found."));
        }

        _tenantSetter.SetTenantId(connector.TenantId);

        string rawBody;
        using (var reader = new StreamReader(Request.Body, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, leaveOpen: true))
        {
            rawBody = await reader.ReadToEndAsync(cancellationToken);
        }

        if (string.IsNullOrWhiteSpace(rawBody))
        {
            return ToProblemResult(Error.Validation("WEBHOOK_BODY_EMPTY", "Webhook request body is required."));
        }

        var runtimeConfig = _configSerializer.DeserializeRuntimeConfig(connector);
        var authResult = AuthenticateWebhookRequest(runtimeConfig, rawBody);
        if (authResult.IsFailure)
        {
            return ToProblemResult(authResult.Error);
        }

        InboundWorkflowTaskRequest? payload;
        try
        {
            payload = JsonSerializer.Deserialize<InboundWorkflowTaskRequest>(rawBody, JsonOptions);
        }
        catch (JsonException)
        {
            return ToProblemResult(Error.Validation("WEBHOOK_JSON_INVALID", "Webhook body is not valid JSON."));
        }

        if (payload is null)
        {
            return ToProblemResult(Error.Validation("WEBHOOK_BODY_INVALID", "Webhook body could not be parsed."));
        }

        var result = await _messageBus.InvokeAsync<Result<InboundWorkflowTaskResponse>>(
            new CreateInboundHumanTaskCommand(
                connector.Id,
                payload.ExternalTaskId,
                payload.FormId,
                payload.FormVersion,
                payload.Assignee,
                payload.CandidateGroups,
                payload.PrefilledData,
                payload.ResumeUrl,
                payload.TimeoutIso8601,
                payload.SensitiveDataMode),
            cancellationToken);

        if (result.IsFailure)
        {
            return ToProblemResult(result.Error);
        }

        return Accepted(result.Value);
    }

    private Result AuthenticateWebhookRequest(ConnectorRuntimeConfig config, string rawBody)
    {
        var credentials = config.Credentials;
        var timestampHeader = Request.Headers["X-H2L-Timestamp"].FirstOrDefault();
        var signatureHeader = Request.Headers["X-H2L-Signature"].FirstOrDefault();

        if (!string.IsNullOrWhiteSpace(credentials?.HmacSecret))
        {
            if (!string.IsNullOrWhiteSpace(signatureHeader) || config.AuthType == ConnectorAuthType.HmacSha256)
            {
                return _hmacSigner.VerifySignature(
                    credentials.HmacSecret,
                    timestampHeader,
                    signatureHeader,
                    rawBody,
                    _dateTimeProvider.UtcNow);
            }
        }

        if (!string.IsNullOrWhiteSpace(credentials?.BearerToken))
        {
            var authHeader = Request.Headers.Authorization.FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(authHeader) &&
                authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                var token = authHeader["Bearer ".Length..].Trim();
                if (FixedTimeEquals(credentials.BearerToken.Trim(), token))
                {
                    return Result.Success();
                }
            }

            if (config.AuthType == ConnectorAuthType.Bearer)
            {
                return Result.Failure(Error.Unauthorized("WEBHOOK_BEARER_INVALID", "Invalid Bearer token."));
            }
        }

        if (!string.IsNullOrWhiteSpace(credentials?.ApiKey))
        {
            var headerName = string.IsNullOrWhiteSpace(credentials.ApiKeyHeaderName)
                ? "X-API-Key"
                : credentials.ApiKeyHeaderName.Trim();
            var providedKey = Request.Headers[headerName].FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(providedKey) &&
                FixedTimeEquals(credentials.ApiKey.Trim(), providedKey.Trim()))
            {
                return Result.Success();
            }

            if (config.AuthType == ConnectorAuthType.ApiKey)
            {
                return Result.Failure(Error.Unauthorized("WEBHOOK_API_KEY_INVALID", "Invalid API key."));
            }
        }

        if (config.AuthType == ConnectorAuthType.None && credentials is null)
        {
            return Result.Success();
        }

        return Result.Failure(Error.Unauthorized(
            "WEBHOOK_AUTHENTICATION_FAILED",
            "Webhook request could not be authenticated against connector credentials."));
    }

    private static bool FixedTimeEquals(string expected, string provided)
    {
        var expectedBytes = Encoding.UTF8.GetBytes(expected);
        var providedBytes = Encoding.UTF8.GetBytes(provided);
        return expectedBytes.Length == providedBytes.Length
            && CryptographicOperations.FixedTimeEquals(expectedBytes, providedBytes);
    }
}
