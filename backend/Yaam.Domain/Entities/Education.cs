namespace Yaam.Domain.Entities;

public class Education : Entity
{
    public required Guid ProfileId { get; set; }
    public required string Institution { get; set; }
    public string? Degree { get; set; }
    public string? FieldOfStudy { get; set; }
    public DateOnly? StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
}
