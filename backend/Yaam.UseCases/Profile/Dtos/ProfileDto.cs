namespace Yaam.UseCases.Profile.Dtos;

public record ProfileDto(
    Guid Id,
    string? FirstName,
    string? LastName,
    string? Email,
    string? Phone,
    string? Location,
    string? Summary,
    List<WorkExperienceDto> WorkExperiences,
    List<EducationDto> Educations,
    List<string> Skills,
    List<LanguageDto> Languages,
    List<CertificationDto> Certifications,
    List<ProfileLinkDto> Links,
    List<CustomFieldDto> CustomFields);
