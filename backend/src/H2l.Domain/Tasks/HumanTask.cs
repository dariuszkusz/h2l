using H2l.Domain.Common;

namespace H2l.Domain.Tasks;

public sealed class HumanTask : AggregateRoot, ITenantScoped
{
    public Guid TenantId { get; private set; }

    public Guid? ConnectorId { get; private set; }

    public Guid FormVersionId { get; private set; }

    public string ExternalTaskId { get; private set; } = string.Empty;

    public HumanTaskStatus Status { get; private set; }

    public SensitiveDataMode SensitiveDataMode { get; private set; }

    public Guid? AssignedUserId { get; private set; }

    public Guid? ClaimedByUserId { get; private set; }

    public string? PrefilledDataJsonb { get; private set; }

    public string? EncryptedResumeUrl { get; private set; }

    public DateTimeOffset? ExpiresAt { get; private set; }

    public DateTimeOffset? CompletedAt { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public Guid ConcurrencyStamp { get; private set; }

    private HumanTask()
    {
    }

    private HumanTask(
        Guid id,
        Guid tenantId,
        Guid? connectorId,
        Guid formVersionId,
        string externalTaskId,
        HumanTaskStatus status,
        SensitiveDataMode sensitiveDataMode,
        Guid? assignedUserId,
        string? prefilledDataJsonb,
        string? encryptedResumeUrl,
        DateTimeOffset? expiresAt,
        DateTimeOffset createdAt)
        : base(id)
    {
        TenantId = tenantId;
        ConnectorId = connectorId;
        FormVersionId = formVersionId;
        ExternalTaskId = externalTaskId;
        Status = status;
        SensitiveDataMode = sensitiveDataMode;
        AssignedUserId = assignedUserId;
        PrefilledDataJsonb = prefilledDataJsonb;
        EncryptedResumeUrl = encryptedResumeUrl;
        ExpiresAt = expiresAt;
        CreatedAt = createdAt;
        ConcurrencyStamp = Guid.CreateVersion7();
    }

    public static Result<HumanTask> Create(
        Guid tenantId,
        Guid formVersionId,
        string externalTaskId,
        Guid? connectorId = null,
        SensitiveDataMode sensitiveDataMode = SensitiveDataMode.ClaimCheck,
        Guid? assignedUserId = null,
        HumanTaskStatus status = HumanTaskStatus.Pending,
        string? prefilledDataJsonb = null,
        string? encryptedResumeUrl = null,
        DateTimeOffset? expiresAt = null,
        DateTimeOffset? createdAt = null)
    {
        if (tenantId == Guid.Empty)
        {
            return Result.Failure<HumanTask>(Error.Validation("TENANT_ID_REQUIRED", "Tenant identifier is required."));
        }

        if (formVersionId == Guid.Empty)
        {
            return Result.Failure<HumanTask>(Error.Validation("FORM_VERSION_ID_REQUIRED", "Form version identifier is required."));
        }

        if (connectorId == Guid.Empty)
        {
            return Result.Failure<HumanTask>(Error.Validation("CONNECTOR_ID_INVALID", "Connector identifier cannot be empty when specified."));
        }

        if (assignedUserId == Guid.Empty)
        {
            return Result.Failure<HumanTask>(Error.Validation("ASSIGNED_USER_ID_INVALID", "Assigned user identifier cannot be empty when specified."));
        }

        if (string.IsNullOrWhiteSpace(externalTaskId))
        {
            return Result.Failure<HumanTask>(Error.Validation("EXTERNAL_TASK_ID_REQUIRED", "External task identifier is required."));
        }

        var trimmedExternalTaskId = externalTaskId.Trim();
        if (trimmedExternalTaskId.Length > 200)
        {
            return Result.Failure<HumanTask>(Error.Validation("EXTERNAL_TASK_ID_TOO_LONG", "External task identifier cannot exceed 200 characters."));
        }

        var task = new HumanTask(
            Guid.CreateVersion7(),
            tenantId,
            connectorId,
            formVersionId,
            trimmedExternalTaskId,
            status,
            sensitiveDataMode,
            assignedUserId,
            prefilledDataJsonb,
            encryptedResumeUrl,
            expiresAt,
            createdAt ?? DateTimeOffset.UtcNow);

        return Result.Success(task);
    }

    public bool CanClaim(Guid userId)
        => userId != Guid.Empty
           && AssignedUserId is null
           && ClaimedByUserId is null
           && Status == HumanTaskStatus.Pending;

    public bool CanUnclaim(Guid userId)
        => userId != Guid.Empty
           && AssignedUserId is null
           && ClaimedByUserId == userId
           && Status == HumanTaskStatus.InProgress;

    public Result Claim(Guid userId)
    {
        if (userId == Guid.Empty)
        {
            return Result.Failure(Error.Validation("USER_ID_REQUIRED", "User identifier is required."));
        }

        if (AssignedUserId is not null)
        {
            return Result.Failure(Error.Conflict(
                "TASK_DIRECTLY_ASSIGNED",
                "Directly assigned tasks cannot be claimed from the group queue."));
        }

        if (Status is HumanTaskStatus.Completed or HumanTaskStatus.Cancelled or HumanTaskStatus.Expired or HumanTaskStatus.NeedsAssignment)
        {
            return Result.Failure(Error.Conflict(
                "TASK_NOT_CLAIMABLE",
                $"Task in status '{Status}' cannot be claimed."));
        }

        if (ClaimedByUserId is not null)
        {
            return Result.Failure(Error.Conflict(
                "TASK_ALREADY_CLAIMED",
                "This task has already been claimed by another user."));
        }

        ClaimedByUserId = userId;
        Status = HumanTaskStatus.InProgress;
        ConcurrencyStamp = Guid.CreateVersion7();
        return Result.Success();
    }

    public Result Unclaim(Guid userId)
    {
        if (userId == Guid.Empty)
        {
            return Result.Failure(Error.Validation("USER_ID_REQUIRED", "User identifier is required."));
        }

        if (AssignedUserId is not null)
        {
            return Result.Failure(Error.Conflict(
                "TASK_DIRECTLY_ASSIGNED",
                "Directly assigned tasks cannot be unclaimed."));
        }

        if (Status is HumanTaskStatus.Completed or HumanTaskStatus.Cancelled or HumanTaskStatus.Expired or HumanTaskStatus.NeedsAssignment)
        {
            return Result.Failure(Error.Conflict(
                "TASK_NOT_UNCLAIMABLE",
                $"Task in status '{Status}' cannot be unclaimed."));
        }

        if (ClaimedByUserId is null)
        {
            return Result.Failure(Error.Conflict(
                "TASK_NOT_CLAIMED",
                "Task is not currently claimed."));
        }

        if (ClaimedByUserId != userId)
        {
            return Result.Failure(Error.Forbidden(
                "TASK_CLAIMED_BY_OTHER_USER",
                "Only the user who claimed this task can release it back to the group queue."));
        }

        ClaimedByUserId = null;
        Status = HumanTaskStatus.Pending;
        ConcurrencyStamp = Guid.CreateVersion7();
        return Result.Success();
    }

    public Result Complete(Guid userId, DateTimeOffset completedAt)
    {
        if (userId == Guid.Empty)
        {
            return Result.Failure(Error.Validation("USER_ID_REQUIRED", "User identifier is required."));
        }

        if (Status is HumanTaskStatus.Completed or HumanTaskStatus.Cancelled or HumanTaskStatus.Expired or HumanTaskStatus.NeedsAssignment)
        {
            return Result.Failure(Error.Conflict(
                "TASK_NOT_SUBMITTABLE",
                $"Task in status '{Status}' cannot be submitted."));
        }

        if (AssignedUserId is not null && AssignedUserId != userId)
        {
            return Result.Failure(Error.Forbidden(
                "TASK_NOT_ASSIGNED_TO_USER",
                "This task is directly assigned to another user."));
        }

        if (AssignedUserId is null && ClaimedByUserId is not null && ClaimedByUserId != userId)
        {
            return Result.Failure(Error.Forbidden(
                "TASK_CLAIMED_BY_OTHER_USER",
                "This task is currently claimed by another user."));
        }

        Status = HumanTaskStatus.Completed;
        CompletedAt = completedAt;
        EncryptedResumeUrl = null;
        ConcurrencyStamp = Guid.CreateVersion7();
        return Result.Success();
    }
}
