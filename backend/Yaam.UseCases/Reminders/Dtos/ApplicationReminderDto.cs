namespace Yaam.UseCases.Reminders.Dtos;

public record ApplicationReminderDto(
    Guid Id,
    int DelayDays,
    DateOnly DueDate,
    string? Note,
    DateTime? NotifiedAt,
    DateTime? CompletedAt,
    DateTime CreatedAt);
