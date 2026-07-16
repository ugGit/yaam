namespace Yaam.Domain.Entities;

public class CustomField : Entity
{
    public required Guid ProfileId { get; set; }
    public required string Label { get; set; }
    public required string Value { get; set; }
}
