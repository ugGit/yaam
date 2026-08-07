using Yaam.Domain.Entities;
using Yaam.UseCases.Applications.Dtos;
using Yaam.UseCases.Reminders;

namespace Yaam.UseCases.Applications;

internal static class ApplicationMapper
{
    internal static ApplicationDto ToDto(Application a) => new(
        a.Id, a.CompanyName, a.Role, a.DateApplied, a.Status,
        a.ContactName, a.ContactEmail, a.ContactPhone, a.JobPosting,
        a.CreatedAt, a.UpdatedAt,
        a.Notes.Select(NoteToDto).ToList(),
        a.Reminders.FirstOrDefault(r => r.CompletedAt == null) is { } activeReminder
            ? ReminderMapper.ToApplicationReminderDto(activeReminder)
            : null);

    internal static ApplicationSummaryDto ToSummaryDto(Application a) =>
        new(a.Id, a.CompanyName, a.Role, a.DateApplied, a.Status);

    internal static ApplicationNoteDto NoteToDto(ApplicationNote n) =>
        new(n.Id, n.Body, n.CreatedAt, n.UpdatedAt);
}
