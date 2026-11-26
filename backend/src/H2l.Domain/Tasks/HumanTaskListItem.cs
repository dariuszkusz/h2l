namespace H2l.Domain.Tasks;

public sealed record HumanTaskListItem(
    Guid Id,
    Guid FormDefinitionId,
    int FormVersionNumber,
    string FormName,
    string ProcessName,
    string ProjectName,
    HumanTaskStatus Status,
    Guid? AssignedUserId,
    Guid? ClaimedByUserId,
    DateTimeOffset? ExpiresAt,
    DateTimeOffset CreatedAt);
