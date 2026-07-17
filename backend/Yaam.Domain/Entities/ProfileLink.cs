namespace Yaam.Domain.Entities;

public class ProfileLink : Entity
{
    public required Guid ProfileId { get; set; }
    public required string Label { get; set; }
    public required string Url { get; set; }
}
