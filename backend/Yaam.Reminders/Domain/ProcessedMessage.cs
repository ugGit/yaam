namespace Yaam.Reminders.Domain;

public class ProcessedMessage
{
    public Guid MessageId { get; init; }
    public DateTime ProcessedAt { get; init; }
}
