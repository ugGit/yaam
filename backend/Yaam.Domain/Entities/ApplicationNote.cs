namespace Yaam.Domain.Entities;

public class ApplicationNote : Entity
{
    public Guid ApplicationId { get; set; }
    public string Body { get; set; } = string.Empty;
}
