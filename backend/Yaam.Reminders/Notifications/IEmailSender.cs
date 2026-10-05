namespace Yaam.Reminders.Notifications;

public interface IEmailSender
{
    Task SendAsync(string to, string subject, string textBody, CancellationToken cancellationToken);
}
