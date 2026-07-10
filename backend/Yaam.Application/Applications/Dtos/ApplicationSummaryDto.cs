using Yaam.Domain.Enums;

namespace Yaam.Application.Applications.Dtos;

public record ApplicationSummaryDto(
    Guid Id,
    string CompanyName,
    string Role,
    DateOnly? DateApplied,
    ApplicationStatus Status);
