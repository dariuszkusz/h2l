using H2l.Domain.Identity;

namespace H2l.Application.Auth.DTOs;

public sealed record UserProfileResponse(
    Guid Id,
    string Email,
    string? ExternalId,
    SystemRole SystemRole,
    string PreferredLanguage,
    string PreferredTheme,
    bool IsActive,
    DateTimeOffset CreatedAt);
