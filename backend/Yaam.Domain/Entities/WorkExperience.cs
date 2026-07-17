namespace Yaam.Domain.Entities;

public class WorkExperience : Entity
{
    public required Guid ProfileId { get; set; }
    public required string Company { get; set; }
    public required string Title { get; set; }
    public required DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public string? Description { get; set; }
}
