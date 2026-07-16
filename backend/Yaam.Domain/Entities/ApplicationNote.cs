namespace Yaam.Domain.Entities;

public class ApplicationNote : Entity
{
    public Guid ApplicationId { get; set; }
    public required string Body { get; set; }
}
