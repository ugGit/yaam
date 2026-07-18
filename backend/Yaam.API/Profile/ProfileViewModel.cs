using Yaam.Domain.Enums;

namespace Yaam.API.Profile;

public record ProfileViewModel(
    Guid Id,
    string? FirstName,
    string? LastName,
    string? Email,
    string? Phone,
    string? Location,
    string? Summary,
    List<WorkExperienceViewModel> WorkExperiences,
    List<EducationViewModel> Educations,
    List<string> Skills,
    List<LanguageViewModel> Languages,
    List<CertificationViewModel> Certifications,
    List<ProfileLinkViewModel> Links,
    List<CustomFieldViewModel> CustomFields);

public record WorkExperienceViewModel(
    Guid Id,
    string Company,
    string Title,
    DateOnly StartDate,
    DateOnly? EndDate,
    string? Description);

public record EducationViewModel(
    Guid Id,
    string Institution,
    string? Degree,
    string? FieldOfStudy,
    DateOnly? StartDate,
    DateOnly? EndDate);

public record LanguageViewModel(Guid Id, string Name, LanguageProficiency Proficiency);

public record CertificationViewModel(Guid Id, string Name, string? Issuer, DateOnly Date);

public record ProfileLinkViewModel(Guid Id, string Label, string Url);

public record CustomFieldViewModel(Guid Id, string Label, string Value);
