namespace H2l.Application.Outbox.Commands;

public sealed record RetryDeadLetterCommand(Guid OutboxId);
