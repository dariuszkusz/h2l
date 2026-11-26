namespace H2l.Application.Setup.Commands;

public sealed record InitializeSetupCommand(
    string AdminEmail,
    string AdminPassword,
    string DefaultLanguage,
    string SelectedPalette,
    SmtpSettingsRequest? Smtp);
