using H2l.Domain.Identity;

namespace H2l.Application.Groups.DTOs;

public sealed record GroupMemberResponse(
    Guid UserId,
    string Email,
    string? ExternalId,
    SystemRole SystemRole,
    bool IsActive);
