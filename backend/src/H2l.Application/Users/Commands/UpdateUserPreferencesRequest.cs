namespace H2l.Application.Users.Commands;

public sealed record UpdateUserPreferencesRequest(
    string? PreferredLanguage,
    string? PreferredTheme);
