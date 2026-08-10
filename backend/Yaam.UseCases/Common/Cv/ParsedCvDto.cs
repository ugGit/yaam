namespace Yaam.UseCases.Common.Cv;

public record ParsedCvDto(
    ParsedInfoDto? Info,
    List<ParsedWorkExperienceDto> WorkExperiences,
    List<ParsedEducationDto> Educations,
    List<string> Skills,
    List<ParsedLanguageDto> Languages,
    List<ParsedCertificationDto> Certifications);

public record ParsedInfoDto(
    string? FirstName,
    string? LastName,
    string? Email,
    string? Phone,
    string? Location,
    string? Summary);

// Dates are ISO strings ("2023-05" or "2023-05-01") — the AI returns them as text.
// DateOnly parsing happens in ApplyParsedCvCommand.
public record ParsedWorkExperienceDto(
    string Company,
    string Title,
    string? StartDate,
    string? EndDate,
    string? Description);

public record ParsedEducationDto(
    string Institution,
    string? Degree,
    string? FieldOfStudy,
    string? StartDate,
    string? EndDate);

public record ParsedLanguageDto(
    string Name,
    string Proficiency);

public record ParsedCertificationDto(
    string Name,
    string? Issuer,
    string? Date);
