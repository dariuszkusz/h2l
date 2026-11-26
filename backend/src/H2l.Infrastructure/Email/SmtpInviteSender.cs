using System.Net;
using System.Net.Mail;
using H2l.Application.Common;

namespace H2l.Infrastructure.Email;

public sealed class SmtpInviteSender : ISmtpInviteSender
{
    public async Task SendActivationInviteAsync(
        SmtpConfiguration smtpConfiguration,
        string recipientEmail,
        string activationLink,
        string language,
        CancellationToken cancellationToken = default)
    {
        using var client = new SmtpClient(smtpConfiguration.Host, smtpConfiguration.Port)
        {
            EnableSsl = smtpConfiguration.UseSsl
        };

        if (!string.IsNullOrWhiteSpace(smtpConfiguration.Username))
        {
            client.Credentials = new NetworkCredential(
                smtpConfiguration.Username,
                smtpConfiguration.Password ?? string.Empty);
        }

        var (subject, body) = BuildLocalizedContent(language, activationLink);
        var from = string.IsNullOrWhiteSpace(smtpConfiguration.FromName)
            ? new MailAddress(smtpConfiguration.FromAddress)
            : new MailAddress(smtpConfiguration.FromAddress, smtpConfiguration.FromName);

        using var message = new MailMessage(from, new MailAddress(recipientEmail))
        {
            Subject = subject,
            Body = body,
            IsBodyHtml = false
        };

        await client.SendMailAsync(message, cancellationToken);
    }

    private static (string Subject, string Body) BuildLocalizedContent(string language, string activationLink)
        => language.Trim().ToLowerInvariant() switch
        {
            "de" => (
                "Aktiviere dein h2l-Konto",
                $"Du wurdest zu h2l eingeladen. Aktiviere dein Konto über den folgenden einmaligen Link:\n{activationLink}"),
            "pl" => (
                "Aktywuj swoje konto w h2l",
                $"Zostałeś zaproszony do h2l. Aktywuj swoje konto korzystając z jednorazowego linku:\n{activationLink}"),
            _ => (
                "Activate your h2l account",
                $"You have been invited to h2l. Activate your account using the following one-time link:\n{activationLink}")
        };
}
