using Yaam.Reminders.Domain;
using Yaam.Reminders.Reminders.Dtos;

namespace Yaam.Reminders.Reminders;

internal static class ReminderMapper
{
    internal static ReminderDto ToDto(Reminder reminder) => new(
        reminder.Id, reminder.ApplicationId,
        reminder.Application.CompanyName, reminder.Application.Role,
        reminder.Application.DateApplied, reminder.Application.ContactName,
        reminder.DelayDays, reminder.DueDate, reminder.Note,
        reminder.NotifiedAt, reminder.CompletedAt, reminder.CreatedAt);
}
