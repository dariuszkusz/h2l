namespace H2l.Application.Setup.Commands;

public sealed record InitializeSetupRequest(
    string AdminEmail,
    string AdminPassword,
    string DefaultLanguage,
    string SelectedPalette,
    SmtpSettingsRequest? Smtp);
