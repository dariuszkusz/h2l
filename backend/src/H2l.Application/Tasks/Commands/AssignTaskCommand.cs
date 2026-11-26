namespace H2l.Application.Tasks.Commands;

public sealed record AssignTaskCommand(Guid TaskId, Guid TargetUserId);
