namespace Yaam.Reminders.Domain;

// Local, eventually consistent copy of the monolith's Application, written only by the event consumer.
public class ApplicationSnapshot
{
    public Guid ApplicationId { get; init; }
    public long Version { get; set; }
    public required string CompanyName { get; set; }
    public required string Role { get; set; }
    public DateOnly? DateApplied { get; set; }
    public string? ContactName { get; set; }
    public bool IsDeleted { get; set; }
}
