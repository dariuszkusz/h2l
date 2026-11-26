using H2l.Domain.Common;

namespace H2l.Domain.Connectors;

public sealed class WorkflowConnector : AggregateRoot, ITenantScoped, ISoftDeletable
{
    public Guid TenantId { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public ConnectorType ConnectorType { get; private set; }

    public ConnectorAuthType AuthType { get; private set; }

    public string BaseUrl { get; private set; } = string.Empty;

    public string? CallbackUrl { get; private set; }

    public string? EncryptedConfig { get; private set; }

    public bool HasCredentials { get; private set; }

    public bool HasCustomCaCertificate { get; private set; }

    public bool IsActive { get; private set; }

    public DateTimeOffset? DeletedAt { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    private WorkflowConnector()
    {
    }

    private WorkflowConnector(
        Guid id,
        Guid tenantId,
        string name,
        ConnectorType connectorType,
        ConnectorAuthType authType,
        string baseUrl,
        string? callbackUrl,
        string? encryptedConfig,
        bool hasCredentials,
        bool hasCustomCaCertificate,
        bool isActive,
        DateTimeOffset createdAt)
        : base(id)
    {
        TenantId = tenantId;
        Name = name;
        ConnectorType = connectorType;
        AuthType = authType;
        BaseUrl = baseUrl;
        CallbackUrl = callbackUrl;
        EncryptedConfig = encryptedConfig;
        HasCredentials = hasCredentials;
        HasCustomCaCertificate = hasCustomCaCertificate;
        IsActive = isActive;
        CreatedAt = createdAt;
    }

    public static Result<WorkflowConnector> Create(
        Guid tenantId,
        string name,
        ConnectorType connectorType,
        ConnectorAuthType authType,
        string baseUrl,
        string? callbackUrl,
        string? encryptedConfig,
        bool hasCredentials,
        bool hasCustomCaCertificate,
        bool isActive = true,
        DateTimeOffset? createdAt = null)
    {
        var validation = ValidateInputs(tenantId, name, baseUrl, callbackUrl);
        if (validation.IsFailure)
        {
            return Result.Failure<WorkflowConnector>(validation.Error);
        }

        var normalizedCallback = string.IsNullOrWhiteSpace(callbackUrl) ? null : callbackUrl.Trim();
        var connector = new WorkflowConnector(
            Guid.CreateVersion7(),
            tenantId,
            name.Trim(),
            connectorType,
            authType,
            baseUrl.Trim(),
            normalizedCallback,
            encryptedConfig,
            hasCredentials,
            hasCustomCaCertificate,
            isActive,
            createdAt ?? DateTimeOffset.UtcNow);

        return Result.Success(connector);
    }

    public Result Update(
        string name,
        ConnectorType connectorType,
        ConnectorAuthType authType,
        string baseUrl,
        string? callbackUrl,
        string? encryptedConfig,
        bool hasCredentials,
        bool hasCustomCaCertificate,
        bool isActive)
    {
        if (DeletedAt is not null)
        {
            return Result.Failure(Error.Conflict("CONNECTOR_DELETED", "Deleted connector cannot be updated."));
        }

        var validation = ValidateInputs(TenantId, name, baseUrl, callbackUrl);
        if (validation.IsFailure)
        {
            return validation;
        }

        Name = name.Trim();
        ConnectorType = connectorType;
        AuthType = authType;
        BaseUrl = baseUrl.Trim();
        CallbackUrl = string.IsNullOrWhiteSpace(callbackUrl) ? null : callbackUrl.Trim();
        EncryptedConfig = encryptedConfig;
        HasCredentials = hasCredentials;
        HasCustomCaCertificate = hasCustomCaCertificate;
        IsActive = isActive;

        return Result.Success();
    }

    public Result SoftDelete(DateTimeOffset deletedAt)
    {
        if (DeletedAt is not null)
        {
            return Result.Failure(Error.Conflict("CONNECTOR_ALREADY_DELETED", "Connector is already deleted."));
        }

        IsActive = false;
        DeletedAt = deletedAt;
        EncryptedConfig = null;
        HasCredentials = false;
        HasCustomCaCertificate = false;
        return Result.Success();
    }

    private static Result ValidateInputs(
        Guid tenantId,
        string name,
        string baseUrl,
        string? callbackUrl)
    {
        if (tenantId == Guid.Empty)
        {
            return Result.Failure(Error.Validation("TENANT_ID_REQUIRED", "Tenant identifier is required."));
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            return Result.Failure(Error.Validation("CONNECTOR_NAME_REQUIRED", "Connector name is required."));
        }

        if (name.Trim().Length > 200)
        {
            return Result.Failure(Error.Validation("CONNECTOR_NAME_TOO_LONG", "Connector name cannot exceed 200 characters."));
        }

        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            return Result.Failure(Error.Validation("CONNECTOR_BASE_URL_REQUIRED", "Connector base URL is required."));
        }

        var trimmedBaseUrl = baseUrl.Trim();
        if (trimmedBaseUrl.Length > 2048 || !Uri.TryCreate(trimmedBaseUrl, UriKind.Absolute, out var parsedBase) ||
            (parsedBase.Scheme != Uri.UriSchemeHttp && parsedBase.Scheme != Uri.UriSchemeHttps))
        {
            return Result.Failure(Error.Validation("CONNECTOR_BASE_URL_INVALID", "Connector base URL must be a valid HTTP or HTTPS URI."));
        }

        if (!string.IsNullOrWhiteSpace(callbackUrl))
        {
            var trimmedCallback = callbackUrl.Trim();
            if (trimmedCallback.Length > 2048 || !Uri.TryCreate(trimmedCallback, UriKind.Absolute, out var parsedCallback) ||
                (parsedCallback.Scheme != Uri.UriSchemeHttp && parsedCallback.Scheme != Uri.UriSchemeHttps))
            {
                return Result.Failure(Error.Validation("CONNECTOR_CALLBACK_URL_INVALID", "Connector callback URL must be a valid HTTP or HTTPS URI."));
            }
        }

        return Result.Success();
    }
}
