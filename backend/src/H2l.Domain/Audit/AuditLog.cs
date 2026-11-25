using H2l.Domain.Common;

namespace H2l.Domain.Audit;

public sealed class AuditLog : Entity, ITenantScoped
{
    public Guid TenantId { get; private set; }

    public Guid? ActorUserId { get; private set; }

    public string Action { get; private set; } = string.Empty;

    public string ResourceType { get; private set; } = string.Empty;

    public Guid ResourceId { get; private set; }

    public string? MetadataJsonb { get; private set; }

    public DateTimeOffset OccurredAt { get; private set; }

    private AuditLog()
    {
    }

    private AuditLog(
        Guid id,
        Guid tenantId,
        Guid? actorUserId,
        string action,
        string resourceType,
        Guid resourceId,
        string? metadataJsonb,
        DateTimeOffset occurredAt)
        : base(id)
    {
        TenantId = tenantId;
        ActorUserId = actorUserId;
        Action = action;
        ResourceType = resourceType;
        ResourceId = resourceId;
        MetadataJsonb = metadataJsonb;
        OccurredAt = occurredAt;
    }

    public static Result<AuditLog> Create(
        Guid tenantId,
        Guid? actorUserId,
        string action,
        string resourceType,
        Guid resourceId,
        string? metadataJsonb = null,
        DateTimeOffset? occurredAt = null)
    {
        if (tenantId == Guid.Empty)
        {
            return Result.Failure<AuditLog>(Error.Validation("TENANT_ID_REQUIRED", "Tenant identifier is required."));
        }

        if (string.IsNullOrWhiteSpace(action))
        {
            return Result.Failure<AuditLog>(Error.Validation("AUDIT_ACTION_REQUIRED", "Audit action is required."));
        }

        if (string.IsNullOrWhiteSpace(resourceType))
        {
            return Result.Failure<AuditLog>(Error.Validation("AUDIT_RESOURCE_TYPE_REQUIRED", "Audit resource type is required."));
        }

        var entry = new AuditLog(
            Guid.CreateVersion7(),
            tenantId,
            actorUserId,
            action.Trim(),
            resourceType.Trim(),
            resourceId,
            metadataJsonb,
            occurredAt ?? DateTimeOffset.UtcNow);

        return Result.Success(entry);
    }
}
