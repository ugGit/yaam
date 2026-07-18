using Yaam.UseCases.Reminders.Dtos;

namespace Yaam.API.Reminders;

internal static class ReminderViewModelMapper
{
    internal static ApplicationReminderViewModel ToViewModel(ApplicationReminderDto dto) => new(
        dto.Id, dto.DelayDays, dto.DueDate, dto.Note, dto.NotifiedAt, dto.CompletedAt, dto.CreatedAt);

    internal static ReminderViewModel ToViewModel(ReminderSummaryDto dto) => new(
        dto.Id, dto.ApplicationId, dto.CompanyName, dto.Role, dto.DateApplied, dto.ContactName,
        dto.DelayDays, dto.DueDate, dto.Note, dto.NotifiedAt, dto.CompletedAt, dto.CreatedAt);
}
