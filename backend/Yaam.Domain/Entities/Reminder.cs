namespace Yaam.Domain.Entities;

public class Reminder : Entity
{
    public Guid ApplicationId { get; set; }
    public int DelayDays { get; set; }
    public DateOnly DueDate { get; set; }
    public string? Note { get; set; }
    public DateTime? NotifiedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public Application Application { get; set; } = null!;
}
