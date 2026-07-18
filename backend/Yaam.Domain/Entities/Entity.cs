namespace Yaam.Domain.Entities;

public abstract class Entity
{
    public Guid Id { get; init; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
