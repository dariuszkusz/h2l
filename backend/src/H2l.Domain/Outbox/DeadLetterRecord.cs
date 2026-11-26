namespace H2l.Domain.Outbox;

public sealed record DeadLetterRecord(
    Guid Id,
    Guid HumanTaskId,
    Guid? FormSubmissionId,
    string ConnectorName,
    string MessageType,
    int RetryCount,
    string DeadLetterReason,
    DateTimeOffset CreatedAt);
