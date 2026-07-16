namespace Yaam.UseCases.Profile.Dtos;

public record EducationDto(
    Guid Id,
    string Institution,
    string? Degree,
    string? FieldOfStudy,
    DateOnly? StartDate,
    DateOnly? EndDate);
