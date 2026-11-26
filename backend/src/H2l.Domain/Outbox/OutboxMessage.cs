using H2l.Domain.Common;

namespace H2l.Domain.Outbox;

public sealed class OutboxMessage : AggregateRoot, ITenantScoped
{
    public Guid TenantId { get; private set; }

    public Guid HumanTaskId { get; private set; }

    public Guid? FormSubmissionId { get; private set; }

    public string MessageType { get; private set; } = string.Empty;

    public OutboxMessageStatus Status { get; private set; }

    public int RetryCount { get; private set; }

    public DateTimeOffset NextAttemptAt { get; private set; }

    public string? DeadLetterReason { get; private set; }

    public DateTimeOffset? ProcessedAt { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    private OutboxMessage()
    {
    }

    private OutboxMessage(
        Guid id,
        Guid tenantId,
        Guid humanTaskId,
        Guid? formSubmissionId,
        string messageType,
        DateTimeOffset nextAttemptAt,
        DateTimeOffset createdAt)
        : base(id)
    {
        TenantId = tenantId;
        HumanTaskId = humanTaskId;
        FormSubmissionId = formSubmissionId;
        MessageType = messageType;
        Status = OutboxMessageStatus.Pending;
        RetryCount = 0;
        NextAttemptAt = nextAttemptAt;
        CreatedAt = createdAt;
    }

    public static Result<OutboxMessage> Create(
        Guid tenantId,
        Guid humanTaskId,
        Guid? formSubmissionId,
        string messageType,
        DateTimeOffset? createdAt = null)
    {
        if (tenantId == Guid.Empty)
        {
            return Result.Failure<OutboxMessage>(Error.Validation("TENANT_ID_REQUIRED", "Tenant identifier is required."));
        }

        if (humanTaskId == Guid.Empty)
        {
            return Result.Failure<OutboxMessage>(Error.Validation("HUMAN_TASK_ID_REQUIRED", "Human task identifier is required."));
        }

        if (formSubmissionId == Guid.Empty)
        {
            return Result.Failure<OutboxMessage>(Error.Validation("FORM_SUBMISSION_ID_INVALID", "Form submission identifier cannot be empty when specified."));
        }

        if (string.IsNullOrWhiteSpace(messageType))
        {
            return Result.Failure<OutboxMessage>(Error.Validation("OUTBOX_MESSAGE_TYPE_REQUIRED", "Outbox message type is required."));
        }

        var trimmedType = messageType.Trim();
        if (trimmedType.Length > 100)
        {
            return Result.Failure<OutboxMessage>(Error.Validation("OUTBOX_MESSAGE_TYPE_TOO_LONG", "Outbox message type cannot exceed 100 characters."));
        }

        var now = createdAt ?? DateTimeOffset.UtcNow;
        return Result.Success(new OutboxMessage(
            Guid.CreateVersion7(),
            tenantId,
            humanTaskId,
            formSubmissionId,
            trimmedType,
            now,
            now));
    }

    public void MarkProcessed(DateTimeOffset processedAt)
    {
        Status = OutboxMessageStatus.Processed;
        ProcessedAt = processedAt;
        DeadLetterReason = null;
    }

    public void ScheduleRetry(int maxRetries, TimeSpan delay, string reason, DateTimeOffset now)
    {
        RetryCount++;
        if (RetryCount >= maxRetries)
        {
            MoveToDeadLetter(reason, now);
            return;
        }

        Status = OutboxMessageStatus.Pending;
        NextAttemptAt = now.Add(delay);
        DeadLetterReason = null;
    }

    public void MoveToDeadLetter(string reason, DateTimeOffset now)
    {
        Status = OutboxMessageStatus.DeadLetter;
        DeadLetterReason = SanitizeReason(reason);
        ProcessedAt = now;
    }

    public Result RetryFromDeadLetter(DateTimeOffset now)
    {
        if (Status != OutboxMessageStatus.DeadLetter)
        {
            return Result.Failure(Error.Conflict(
                "OUTBOX_NOT_DEAD_LETTER",
                $"Outbox message in status '{Status}' cannot be retried from dead-letter."));
        }

        Status = OutboxMessageStatus.Pending;
        RetryCount = 0;
        NextAttemptAt = now;
        DeadLetterReason = null;
        ProcessedAt = null;
        return Result.Success();
    }

    private static string SanitizeReason(string? reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            return "Delivery failed.";
        }

        var trimmed = reason.Trim();
        return trimmed.Length <= 2000 ? trimmed : trimmed[..2000];
    }
}
