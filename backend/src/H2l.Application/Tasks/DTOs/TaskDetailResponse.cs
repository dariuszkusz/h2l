using System.Text.Json;
using H2l.Domain.Tasks;

namespace H2l.Application.Tasks.DTOs;

public sealed record TaskDetailResponse(
    Guid Id,
    HumanTaskStatus Status,
    Guid FormVersionId,
    int FormVersionNumber,
    JsonElement Schema,
    JsonElement PrefilledData,
    Guid? AssignedUserId,
    Guid? ClaimedByUserId,
    bool CanClaim,
    bool CanUnclaim,
    SensitiveDataMode SensitiveDataMode,
    DateTimeOffset? ExpiresAt,
    DateTimeOffset CreatedAt,
    DateTimeOffset? CompletedAt);
