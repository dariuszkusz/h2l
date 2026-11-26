using H2l.Domain.Tasks;

namespace H2l.Application.Tasks.DTOs;

public sealed record TaskSummaryResponse(
    Guid Id,
    Guid FormDefinitionId,
    int FormVersionNumber,
    string FormName,
    string ProcessName,
    string ProjectName,
    HumanTaskStatus Status,
    Guid? AssignedUserId,
    Guid? ClaimedByUserId,
    bool CanClaim,
    bool CanUnclaim,
    DateTimeOffset? ExpiresAt,
    DateTimeOffset CreatedAt);
