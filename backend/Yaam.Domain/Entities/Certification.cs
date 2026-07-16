namespace Yaam.Domain.Entities;

public class Certification : Entity
{
    public required Guid ProfileId { get; set; }
    public required string Name { get; set; }
    public string? Issuer { get; set; }
    public required DateOnly Date { get; set; }
}
