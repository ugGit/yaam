using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using Yaam.UseCases.Common.Email;

namespace Yaam.Infrastructure.Email;

public class SmtpEmailSender(IOptions<EmailSettings> emailOptions) : IEmailSender
{
    public async Task SendAsync(string to, string subject, string textBody, CancellationToken cancellationToken)
    {
        var settings = emailOptions.Value;

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(settings.FromName, settings.FromAddress));
        message.To.Add(MailboxAddress.Parse(to));
        message.Subject = subject;
        message.Body = new TextPart("plain") { Text = textBody };

        using var client = new SmtpClient();
        var socketOptions = settings.AllowInsecureConnection ? SecureSocketOptions.None : SecureSocketOptions.Auto;
        await client.ConnectAsync(settings.SmtpHost, settings.SmtpPort, socketOptions, cancellationToken);
        if (!string.IsNullOrEmpty(settings.Username) && !string.IsNullOrEmpty(settings.Password))
            await client.AuthenticateAsync(settings.Username, settings.Password, cancellationToken);
        await client.SendAsync(message, cancellationToken);
        await client.DisconnectAsync(true, cancellationToken);
    }
}
