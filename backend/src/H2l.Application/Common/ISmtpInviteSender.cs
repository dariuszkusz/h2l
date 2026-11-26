namespace H2l.Application.Common;

public interface ISmtpInviteSender
{
    Task SendActivationInviteAsync(
        SmtpConfiguration smtpConfiguration,
        string recipientEmail,
        string activationLink,
        string language,
        CancellationToken cancellationToken = default);
}
