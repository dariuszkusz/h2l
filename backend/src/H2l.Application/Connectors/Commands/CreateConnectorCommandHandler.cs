using System.Text.Json;
using H2l.Application.Common;
using H2l.Application.Connectors.Contracts;
using H2l.Application.Connectors.DTOs;
using H2l.Application.Connectors.Services;
using H2l.Application.Licensing;
using H2l.Domain.Audit;
using H2l.Domain.Common;
using H2l.Domain.Connectors;

namespace H2l.Application.Connectors.Commands;

public sealed class CreateConnectorCommandHandler
{
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly IWorkflowConnectorRepository _connectorRepository;
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly IEntitlementService _entitlementService;
    private readonly ISsrfGuard _ssrfGuard;
    private readonly IConnectorConfigSerializer _configSerializer;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _dateTimeProvider;

    public CreateConnectorCommandHandler(
        ITenantContext tenantContext,
        ICurrentUserContext currentUserContext,
        IWorkflowConnectorRepository connectorRepository,
        IAuditLogRepository auditLogRepository,
        IEntitlementService entitlementService,
        ISsrfGuard ssrfGuard,
        IConnectorConfigSerializer configSerializer,
        IUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider)
    {
        _tenantContext = tenantContext;
        _currentUserContext = currentUserContext;
        _connectorRepository = connectorRepository;
        _auditLogRepository = auditLogRepository;
        _entitlementService = entitlementService;
        _ssrfGuard = ssrfGuard;
        _configSerializer = configSerializer;
        _unitOfWork = unitOfWork;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<ConnectorSummaryResponse>> Handle(
        CreateConnectorCommand command,
        CancellationToken cancellationToken)
    {
        if (_tenantContext.TenantId is null || _tenantContext.TenantId == Guid.Empty)
        {
            return Result.Failure<ConnectorSummaryResponse>(
                Error.Unauthorized("TENANT_CONTEXT_MISSING", "Tenant context is required."));
        }

        if (command.IsActive)
        {
            var entitlement = await _entitlementService.CanActivateConnectorAsync(cancellationToken);
            if (entitlement.IsFailure)
            {
                return Result.Failure<ConnectorSummaryResponse>(entitlement.Error);
            }
        }

        var baseUrlCheck = await _ssrfGuard.ValidateUrlAsync(command.BaseUrl, cancellationToken);
        if (baseUrlCheck.IsFailure)
        {
            return Result.Failure<ConnectorSummaryResponse>(baseUrlCheck.Error);
        }

        if (!string.IsNullOrWhiteSpace(command.CallbackUrl))
        {
            var callbackCheck = await _ssrfGuard.ValidateUrlAsync(command.CallbackUrl, cancellationToken);
            if (callbackCheck.IsFailure)
            {
                return Result.Failure<ConnectorSummaryResponse>(callbackCheck.Error);
            }
        }

        var nameExists = await _connectorRepository.ExistsByNameAsync(command.Name, null, cancellationToken);
        if (nameExists)
        {
            return Result.Failure<ConnectorSummaryResponse>(
                Error.Conflict("CONNECTOR_NAME_EXISTS", "A connector with this name already exists."));
        }

        var tenantId = _tenantContext.TenantId.Value;
        var credentialsData = command.Credentials is null
            ? null
            : new ConnectorCredentialsData(
                command.Credentials.Username,
                command.Credentials.Password,
                command.Credentials.BearerToken,
                command.Credentials.ApiKeyHeaderName,
                command.Credentials.ApiKey,
                command.Credentials.HmacSecret,
                command.Credentials.ClientId,
                command.Credentials.ClientSecret,
                command.Credentials.TokenEndpoint,
                command.Credentials.Audience,
                command.Credentials.Scope);

        var (encryptedConfig, hasCredentials, hasCustomCa) = _configSerializer.SerializeAndEncrypt(
            tenantId,
            credentialsData,
            command.CustomCaCertificatePem);

        var now = _dateTimeProvider.UtcNow;
        var createResult = WorkflowConnector.Create(
            tenantId,
            command.Name,
            command.ConnectorType,
            command.AuthType,
            command.BaseUrl,
            command.CallbackUrl,
            encryptedConfig,
            hasCredentials,
            hasCustomCa,
            command.IsActive,
            now);

        if (createResult.IsFailure)
        {
            return Result.Failure<ConnectorSummaryResponse>(createResult.Error);
        }

        var connector = createResult.Value;
        await _connectorRepository.AddAsync(connector, cancellationToken);

        var auditResult = AuditLog.Create(
            tenantId,
            _currentUserContext.UserId,
            "CONNECTOR_CREATED",
            "WorkflowConnector",
            connector.Id,
            JsonSerializer.Serialize(new
            {
                connectorType = connector.ConnectorType.ToString(),
                authType = connector.AuthType.ToString(),
                isActive = connector.IsActive
            }),
            now);

        if (auditResult.IsSuccess)
        {
            await _auditLogRepository.AddAsync(auditResult.Value, cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(new ConnectorSummaryResponse(
            connector.Id,
            connector.Name,
            connector.ConnectorType,
            connector.BaseUrl,
            connector.AuthType,
            connector.HasCredentials,
            connector.HasCustomCaCertificate,
            connector.CallbackUrl,
            connector.IsActive,
            connector.CreatedAt));
    }
}
