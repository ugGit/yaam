using Yaam.Domain.Entities;
using Yaam.UseCases.Reminders.Dtos;

namespace Yaam.UseCases.Reminders;

internal static class ReminderMapper
{
    internal static ApplicationReminderDto ToApplicationReminderDto(Reminder r) => new(
        r.Id, r.DelayDays, r.DueDate, r.Note, r.NotifiedAt, r.CompletedAt, r.CreatedAt);

    internal static ReminderSummaryDto ToSummaryDto(Reminder r) => new(
        r.Id, r.ApplicationId,
        r.Application.CompanyName, r.Application.Role, r.Application.DateApplied, r.Application.ContactName,
        r.DelayDays, r.DueDate, r.Note, r.NotifiedAt, r.CompletedAt, r.CreatedAt);
}
