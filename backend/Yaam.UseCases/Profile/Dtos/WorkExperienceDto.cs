namespace Yaam.UseCases.Profile.Dtos;

public record WorkExperienceDto(
    Guid Id,
    string Company,
    string Title,
    DateOnly StartDate,
    DateOnly? EndDate,
    string? Description);
