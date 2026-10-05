using Yaam.Reminders.Domain;
using Yaam.Reminders.Reminders.Dtos;

namespace Yaam.Reminders.Reminders;

internal static class ReminderMapper
{
    internal static ReminderDto ToDto(Reminder reminder, ApplicationSnapshot snapshot) => new(
        reminder.Id, reminder.ApplicationId,
        snapshot.CompanyName, snapshot.Role, snapshot.DateApplied, snapshot.ContactName,
        reminder.DelayDays, reminder.DueDate, reminder.Note,
        reminder.NotifiedAt, reminder.CompletedAt, reminder.CreatedAt);
}
