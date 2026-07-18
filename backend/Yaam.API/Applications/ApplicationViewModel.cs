using Yaam.Domain.Enums;

namespace Yaam.API.Applications;

public record ApplicationViewModel(
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
    List<ApplicationNoteViewModel> Notes);

public record ApplicationSummaryViewModel(
    Guid Id,
    string CompanyName,
    string Role,
    DateOnly? DateApplied,
    ApplicationStatus Status);

public record ApplicationNoteViewModel(
    Guid Id,
    string Body,
    DateTime CreatedAt,
    DateTime UpdatedAt);
