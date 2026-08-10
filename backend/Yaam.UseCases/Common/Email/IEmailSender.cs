namespace Yaam.UseCases.Common.Email;

public interface IEmailSender
{
    Task SendAsync(string to, string subject, string textBody, CancellationToken cancellationToken);
}
