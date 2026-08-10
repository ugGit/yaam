using Yaam.Domain.Enums;
using Yaam.UseCases.Reminders.Dtos;

namespace Yaam.UseCases.Applications.Dtos;

public record ApplicationDto(
    Guid Id,
    string CompanyName,
    string Role,
    DateOnly? DateApplied,
    ApplicationStatus Status,
    string? ContactName,
    string? ContactEmail,
    string? ContactPhone,
    string? JobPosting,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    List<ApplicationNoteDto> Notes,
    List<ApplicationReminderDto> Reminders);
