namespace Yaam.Reminders.Reminders.Dtos;

public record ReminderDto(
    Guid Id,
    Guid ApplicationId,
    string CompanyName,
    string Role,
    DateOnly? DateApplied,
    string? ContactName,
    int DelayDays,
    DateOnly DueDate,
    string? Note,
    DateTime? NotifiedAt,
    DateTime? CompletedAt,
    DateTime CreatedAt);
