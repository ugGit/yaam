using Yaam.Domain.Entities;
using Yaam.UseCases.Applications.Dtos;

namespace Yaam.UseCases.Applications;

internal static class ApplicationMapper
{
    internal static ApplicationDto ToDto(Application a) => new(
        a.Id, a.CompanyName, a.Role, a.DateApplied, a.Status,
        a.ContactName, a.ContactEmail, a.ContactPhone, a.JobPosting,
        a.CreatedAt, a.UpdatedAt,
        a.Notes.OrderByDescending(n => n.CreatedAt).Select(NoteToDto).ToList());

    internal static ApplicationNoteDto NoteToDto(ApplicationNote n) =>
        new(n.Id, n.Body, n.CreatedAt, n.UpdatedAt);
}
