namespace Yaam.API.Reminders;

public record ApplicationReminderViewModel(
    Guid Id,
    int DelayDays,
    DateOnly DueDate,
    string? Note,
    DateTime? NotifiedAt,
    DateTime? CompletedAt,
    DateTime CreatedAt);

public record ReminderViewModel(
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
