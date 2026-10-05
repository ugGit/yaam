using System.Text;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Yaam.Reminders.Domain;
using Yaam.Reminders.Persistence;

namespace Yaam.Reminders.Notifications;

public record SendDueRemindersCommand : IRequest;

public class SendDueRemindersCommandHandler(
    RemindersDbContext db,
    IEmailSender emailSender,
    IOptions<EmailSettings> emailOptions,
    TimeProvider timeProvider,
    ILogger<SendDueRemindersCommandHandler> logger)
    : IRequestHandler<SendDueRemindersCommand>
{
    public async Task Handle(SendDueRemindersCommand command, CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
        var due = await (
                from reminder in db.Reminders
                join snapshot in db.ApplicationSnapshots on reminder.ApplicationId equals snapshot.ApplicationId
                where reminder.CompletedAt == null && reminder.NotifiedAt == null
                      && reminder.DueDate <= today && !snapshot.IsDeleted
                select new { reminder, snapshot })
            .ToListAsync(cancellationToken);

        foreach (var row in due)
        {
            try
            {
                await emailSender.SendAsync(
                    emailOptions.Value.NotificationEmail,
                    $"Follow-up reminder: {row.snapshot.CompanyName} — {row.snapshot.Role}",
                    BuildEmailBody(row.reminder, row.snapshot),
                    cancellationToken);

                row.reminder.NotifiedAt = timeProvider.GetUtcNow().UtcDateTime;
                await db.SaveChangesAsync(cancellationToken);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                logger.LogError(exception, "Failed to send reminder {ReminderId}", row.reminder.Id);
            }
        }
    }

    private static string BuildEmailBody(Reminder reminder, ApplicationSnapshot application)
    {
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
