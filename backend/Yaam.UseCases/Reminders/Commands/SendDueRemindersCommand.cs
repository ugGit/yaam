using System.Text;
using MediatR;
using Microsoft.Extensions.Options;
using Yaam.Domain.Entities;
using Yaam.Domain.Repositories;
using Yaam.UseCases.Common.Email;

namespace Yaam.UseCases.Reminders.Commands;

public record SendDueRemindersCommand : IRequest;

public class SendDueRemindersCommandHandler(
    IReminderRepository repository,
    IEmailSender emailSender,
    IOptions<EmailSettings> emailOptions)
    : IRequestHandler<SendDueRemindersCommand>
{
    public async Task Handle(SendDueRemindersCommand command, CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var dueReminders = await repository.GetDueAsync(today, cancellationToken);

        foreach (var reminder in dueReminders)
        {
            var subject = $"Follow-up reminder: {reminder.Application.CompanyName} — {reminder.Application.Role}";
            var body = BuildEmailBody(reminder);

            await emailSender.SendAsync(
                emailOptions.Value.NotificationEmail,
                subject,
                body,
                cancellationToken);

            reminder.NotifiedAt = DateTime.UtcNow;
            await repository.UpdateAsync(reminder, cancellationToken);
        }
    }

    private static string BuildEmailBody(Reminder reminder)
    {
        var application = reminder.Application;
        var salutation = string.IsNullOrEmpty(application.ContactName)
            ? "Dear Hiring Team,"
            : $"Dear {application.ContactName},";
        var dateApplied = application.DateApplied.HasValue
            ? application.DateApplied.Value.ToString("d MMMM yyyy")
            : null;
        var appliedLine = dateApplied is not null ? $", which I submitted on {dateApplied}" : "";

        var stringBuilder = new StringBuilder();
        stringBuilder.AppendLine($"Your follow-up reminder for {application.CompanyName} ({application.Role}) is due today.");
        if (dateApplied is not null)
            stringBuilder.AppendLine($"Applied: {dateApplied}");
        if (!string.IsNullOrEmpty(reminder.Note))
            stringBuilder.AppendLine($"Your note: {reminder.Note}");
        stringBuilder.AppendLine();
        stringBuilder.AppendLine("--- Follow-up email draft ---");
        stringBuilder.AppendLine();
        stringBuilder.AppendLine(salutation);
        stringBuilder.AppendLine();
        stringBuilder.AppendLine($"I am writing to follow up on my application for the {application.Role} position at {application.CompanyName}{appliedLine}.");
        stringBuilder.AppendLine();
        stringBuilder.AppendLine("I remain very interested in this opportunity and would welcome the chance to discuss my application further. Please let me know if you need any additional information.");
        stringBuilder.AppendLine();
        stringBuilder.AppendLine("Kind regards");

        return stringBuilder.ToString();
    }
}
