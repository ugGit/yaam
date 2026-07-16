namespace Yaam.Domain.Entities;

public class Profile : Entity
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Location { get; set; }
    public string? Summary { get; set; }
    public List<string> Skills { get; set; } = [];
    public ICollection<WorkExperience> WorkExperiences { get; set; } = new List<WorkExperience>();
    public ICollection<Education> Educations { get; set; } = new List<Education>();
    public ICollection<Language> Languages { get; set; } = new List<Language>();
    public ICollection<Certification> Certifications { get; set; } = new List<Certification>();
    public ICollection<ProfileLink> Links { get; set; } = new List<ProfileLink>();
    public ICollection<CustomField> CustomFields { get; set; } = new List<CustomField>();
}
