using Yaam.Domain.Enums;

namespace Yaam.Domain.Entities;

public class Language : Entity
{
    public required Guid ProfileId { get; set; }
    public required string Name { get; set; }
    public required LanguageProficiency Proficiency { get; set; }
}
