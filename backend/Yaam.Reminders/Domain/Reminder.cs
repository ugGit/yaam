namespace Yaam.Reminders.Domain;

public class Reminder
{
    public Guid Id { get; init; }
    public Guid ApplicationId { get; init; }
    public int DelayDays { get; set; }
    public DateOnly DueDate { get; set; }
    public string? Note { get; set; }
    public DateTime? NotifiedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public ApplicationSnapshot Application { get; init; } = null!;
}
