using Yaam.Domain.Enums;

namespace Yaam.Domain.Entities;

public class Application : Entity
{
    public required string CompanyName { get; set; }
    public required string Role { get; set; }
    public DateOnly? DateApplied { get; set; }
    public ApplicationStatus Status { get; set; } = ApplicationStatus.Draft;
    public string? ContactName { get; set; }
    public string? ContactEmail { get; set; }
    public string? ContactPhone { get; set; }
    public string? JobPosting { get; set; }
    public ICollection<ApplicationNote> Notes { get; set; } = new List<ApplicationNote>();
    public Reminder? Reminder { get; set; }
}
