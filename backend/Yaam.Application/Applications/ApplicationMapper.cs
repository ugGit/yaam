using Yaam.Application.Applications.Dtos;
using Yaam.Domain.Entities;

namespace Yaam.Application.Applications;

internal static class ApplicationMapper
{
    internal static ApplicationDto ToDto(global::Yaam.Domain.Entities.Application a) => new(
        a.Id, a.CompanyName, a.Role, a.DateApplied, a.Status,
        a.ContactName, a.ContactEmail, a.ContactPhone, a.JobPosting,
        a.CreatedAt, a.UpdatedAt,
        a.Notes.OrderByDescending(n => n.CreatedAt).Select(NoteToDto).ToList());

    internal static ApplicationNoteDto NoteToDto(ApplicationNote n) =>
        new(n.Id, n.Body, n.CreatedAt, n.UpdatedAt);
}
