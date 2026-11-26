namespace H2l.Application.Outbox.DTOs;

public sealed record DeadLetterItemResponse(
    Guid Id,
    Guid HumanTaskId,
    Guid? FormSubmissionId,
    string ConnectorName,
    string MessageType,
    int RetryCount,
    string DeadLetterReason,
    DateTimeOffset CreatedAt);
