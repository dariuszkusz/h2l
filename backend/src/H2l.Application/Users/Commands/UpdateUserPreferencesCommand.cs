namespace H2l.Application.Users.Commands;

public sealed record UpdateUserPreferencesCommand(
    string? PreferredLanguage,
    string? PreferredTheme);
