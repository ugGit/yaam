using Yaam.UseCases.Profile.Dtos;

namespace Yaam.API.Profile;

internal static class ProfileViewModelMapper
{
    internal static ProfileViewModel ToViewModel(ProfileDto dto) => new(
        dto.Id,
        dto.FirstName,
        dto.LastName,
        dto.Email,
        dto.Phone,
        dto.Location,
        dto.Summary,
        dto.WorkExperiences.Select(ToViewModel).ToList(),
        dto.Educations.Select(ToViewModel).ToList(),
        dto.Skills,
        dto.Languages.Select(ToViewModel).ToList(),
        dto.Certifications.Select(ToViewModel).ToList(),
        dto.Links.Select(ToViewModel).ToList(),
        dto.CustomFields.Select(ToViewModel).ToList());

    internal static WorkExperienceViewModel ToViewModel(WorkExperienceDto dto) =>
        new(dto.Id, dto.Company, dto.Title, dto.StartDate, dto.EndDate, dto.Description);

    internal static EducationViewModel ToViewModel(EducationDto dto) =>
        new(dto.Id, dto.Institution, dto.Degree, dto.FieldOfStudy, dto.StartDate, dto.EndDate);

    internal static LanguageViewModel ToViewModel(LanguageDto dto) =>
        new(dto.Id, dto.Name, dto.Proficiency);

    internal static CertificationViewModel ToViewModel(CertificationDto dto) =>
        new(dto.Id, dto.Name, dto.Issuer, dto.Date);

    internal static ProfileLinkViewModel ToViewModel(ProfileLinkDto dto) =>
        new(dto.Id, dto.Label, dto.Url);

    internal static CustomFieldViewModel ToViewModel(CustomFieldDto dto) =>
        new(dto.Id, dto.Label, dto.Value);
}
