namespace Yaam.UseCases.Applications.Dtos;

public record ApplicationNoteDto(
    Guid Id,
    string Body,
    DateTime CreatedAt,
    DateTime UpdatedAt);
