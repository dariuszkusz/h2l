using H2l.Domain.Common;

namespace H2l.Domain.Workflows;

public sealed class WorkflowProcess : AggregateRoot, ITenantScoped, ISoftDeletable
{
    public Guid TenantId { get; private set; }

    public Guid ProjectId { get; private set; }

    public Guid? ConnectorId { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public string ExternalProcessKey { get; private set; } = string.Empty;

    public WorkflowProcessStatus Status { get; private set; }

    public DateTimeOffset? DeletedAt { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    private WorkflowProcess()
    {
    }

    private WorkflowProcess(
        Guid id,
        Guid tenantId,
        Guid projectId,
        Guid? connectorId,
        string name,
        string externalProcessKey,
        WorkflowProcessStatus status,
        DateTimeOffset createdAt)
        : base(id)
    {
        TenantId = tenantId;
        ProjectId = projectId;
        ConnectorId = connectorId;
        Name = name;
        ExternalProcessKey = externalProcessKey;
        Status = status;
        CreatedAt = createdAt;
    }

    public static Result<WorkflowProcess> Create(
        Guid tenantId,
        Guid projectId,
        string name,
        string externalProcessKey,
        Guid? connectorId = null,
        WorkflowProcessStatus status = WorkflowProcessStatus.Draft,
        DateTimeOffset? createdAt = null)
    {
        if (tenantId == Guid.Empty)
        {
            return Result.Failure<WorkflowProcess>(Error.Validation("TENANT_ID_REQUIRED", "Tenant identifier is required."));
        }

        if (projectId == Guid.Empty)
        {
            return Result.Failure<WorkflowProcess>(Error.Validation("PROJECT_ID_REQUIRED", "Project identifier is required."));
        }

        if (connectorId == Guid.Empty)
        {
            return Result.Failure<WorkflowProcess>(Error.Validation("CONNECTOR_ID_INVALID", "Connector identifier cannot be empty when specified."));
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            return Result.Failure<WorkflowProcess>(Error.Validation("PROCESS_NAME_REQUIRED", "Workflow process name is required."));
        }

        var trimmedName = name.Trim();
        if (trimmedName.Length > 200)
        {
            return Result.Failure<WorkflowProcess>(Error.Validation("PROCESS_NAME_TOO_LONG", "Workflow process name cannot exceed 200 characters."));
        }

        if (string.IsNullOrWhiteSpace(externalProcessKey))
        {
            return Result.Failure<WorkflowProcess>(Error.Validation("EXTERNAL_PROCESS_KEY_REQUIRED", "External process key is required."));
        }

        var trimmedKey = externalProcessKey.Trim();
        if (trimmedKey.Length > 200)
        {
            return Result.Failure<WorkflowProcess>(Error.Validation("EXTERNAL_PROCESS_KEY_TOO_LONG", "External process key cannot exceed 200 characters."));
        }

        var process = new WorkflowProcess(
            Guid.CreateVersion7(),
            tenantId,
            projectId,
            connectorId,
            trimmedName,
            trimmedKey,
            status,
            createdAt ?? DateTimeOffset.UtcNow);

        return Result.Success(process);
    }

    public Result Activate()
    {
        if (DeletedAt is not null)
        {
            return Result.Failure(Error.Validation("PROCESS_DELETED", "A deleted workflow process cannot be activated."));
        }

        if (Status == WorkflowProcessStatus.Archived)
        {
            return Result.Failure(Error.Conflict("PROCESS_ARCHIVED", "An archived workflow process cannot be activated."));
        }

        Status = WorkflowProcessStatus.Active;
        return Result.Success();
    }

    public Result Archive()
    {
        if (DeletedAt is not null)
        {
            return Result.Failure(Error.Validation("PROCESS_DELETED", "A deleted workflow process cannot be archived."));
        }

        Status = WorkflowProcessStatus.Archived;
        return Result.Success();
    }

    public void SoftDelete(DateTimeOffset? deletedAt = null)
    {
        DeletedAt = deletedAt ?? DateTimeOffset.UtcNow;
    }
}
