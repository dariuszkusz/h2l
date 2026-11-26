using H2l.Domain.Identity;

namespace H2l.Application.Users.DTOs;

public sealed record UserSummaryResponse(
    Guid Id,
    string Email,
    string? ExternalId,
    SystemRole SystemRole,
    string PreferredLanguage,
    string PreferredTheme,
    bool IsActive,
    IReadOnlyList<Guid> GroupIds,
    DateTimeOffset CreatedAt);
