using Yaam.Domain.Enums;

namespace Yaam.UseCases.Applications.Dtos;

public record ApplicationSummaryDto(
    Guid Id,
    string CompanyName,
    string Role,
    DateOnly? DateApplied,
    ApplicationStatus Status);
