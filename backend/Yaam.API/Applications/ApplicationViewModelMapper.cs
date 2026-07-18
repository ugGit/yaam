using Yaam.UseCases.Applications.Dtos;

namespace Yaam.API.Applications;

internal static class ApplicationViewModelMapper
{
    internal static ApplicationViewModel ToViewModel(ApplicationDto dto) => new(
        dto.Id,
        dto.CompanyName,
        dto.Role,
        dto.DateApplied,
        dto.Status,
        dto.ContactName,
        dto.ContactEmail,
        dto.ContactPhone,
        dto.JobPosting,
        dto.CreatedAt,
        dto.UpdatedAt,
        dto.Notes.Select(ToViewModel).ToList());

    internal static ApplicationSummaryViewModel ToViewModel(ApplicationSummaryDto dto) =>
        new(dto.Id, dto.CompanyName, dto.Role, dto.DateApplied, dto.Status);

    internal static ApplicationNoteViewModel ToViewModel(ApplicationNoteDto dto) =>
        new(dto.Id, dto.Body, dto.CreatedAt, dto.UpdatedAt);
}
