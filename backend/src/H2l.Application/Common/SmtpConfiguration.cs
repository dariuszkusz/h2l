namespace H2l.Application.Common;

public sealed record SmtpConfiguration(
    string Host,
    int Port,
    bool UseSsl,
    string? Username,
    string? Password,
    string FromAddress,
    string? FromName);
