using Yaam.Domain.Entities;
using Yaam.Domain.Enums;

namespace Yaam.Domain.Repositories;

public interface IApplicationRepository
{
    Task<List<Application>> GetAllAsync(ApplicationStatus? status, string sort, string order, CancellationToken ct);
    Task<Application?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<Application> AddAsync(Application application, CancellationToken ct);
    Task UpdateAsync(Application application, CancellationToken ct);
    Task DeleteAsync(Guid id, CancellationToken ct);
    Task<ApplicationNote?> GetNoteByIdAsync(Guid applicationId, Guid noteId, CancellationToken ct);
    Task<ApplicationNote> AddNoteAsync(ApplicationNote note, CancellationToken ct);
    Task UpdateNoteAsync(ApplicationNote note, CancellationToken ct);
    Task DeleteNoteAsync(Guid applicationId, Guid noteId, CancellationToken ct);
}
