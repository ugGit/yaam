using Yaam.Domain.Entities;
using Yaam.UseCases.Profile.Dtos;

namespace Yaam.UseCases.Profile;

public static class ProfileMapper
{
    public static ProfileDto ToDto(Yaam.Domain.Entities.Profile profile) => new(
        profile.Id,
        profile.FirstName,
        profile.LastName,
        profile.Email,
        profile.Phone,
        profile.Location,
        profile.Summary,
        profile.WorkExperiences.Select(ToDto).ToList(),
        profile.Educations.Select(ToDto).ToList(),
        profile.Skills,
        profile.Languages.Select(ToDto).ToList(),
        profile.Certifications.Select(ToDto).ToList(),
        profile.Links.Select(ToDto).ToList(),
        profile.CustomFields.Select(ToDto).ToList());

    public static WorkExperienceDto ToDto(WorkExperience w) =>
        new(w.Id, w.Company, w.Title, w.StartDate, w.EndDate, w.Description);

    public static EducationDto ToDto(Education e) =>
        new(e.Id, e.Institution, e.Degree, e.FieldOfStudy, e.StartDate, e.EndDate);

    public static LanguageDto ToDto(Language l) =>
        new(l.Id, l.Name, l.Proficiency);

    public static CertificationDto ToDto(Certification c) =>
        new(c.Id, c.Name, c.Issuer, c.Date);

    public static ProfileLinkDto ToDto(ProfileLink l) =>
        new(l.Id, l.Label, l.Url);

    public static CustomFieldDto ToDto(CustomField cf) =>
        new(cf.Id, cf.Label, cf.Value);
}
