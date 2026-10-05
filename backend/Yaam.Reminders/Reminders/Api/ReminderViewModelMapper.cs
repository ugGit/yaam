using Yaam.Reminders.Reminders.Dtos;

namespace Yaam.Reminders.Reminders.Api;

internal static class ReminderViewModelMapper
{
    internal static ReminderViewModel ToViewModel(ReminderDto dto) => new(
        dto.Id, dto.ApplicationId, dto.CompanyName, dto.Role, dto.DateApplied, dto.ContactName,
        dto.DelayDays, dto.DueDate, dto.Note, dto.NotifiedAt, dto.CompletedAt, dto.CreatedAt);
}
