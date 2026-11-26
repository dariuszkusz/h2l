namespace H2l.Application.Setup.Commands;

public sealed record SmtpSettingsRequest(
    string Host,
    int Port,
    bool UseSsl,
    string? Username,
    string? Password,
    string FromAddress,
    string? FromName);
