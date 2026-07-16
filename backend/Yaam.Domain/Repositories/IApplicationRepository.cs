using Yaam.Domain.Entities;
using Yaam.Domain.Enums;

namespace Yaam.Domain.Repositories;

public interface IApplicationRepository
{
    Task<List<Application>> GetAllAsync(ApplicationStatus? status, string sort, string order, CancellationToken cancellationToken);
    Task<Application?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<Application> AddAsync(Application application, CancellationToken cancellationToken);
    Task UpdateAsync(Application application, CancellationToken cancellationToken);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken);
    Task<ApplicationNote?> GetNoteByIdAsync(Guid applicationId, Guid noteId, CancellationToken cancellationToken);
    Task<ApplicationNote> AddNoteAsync(ApplicationNote note, CancellationToken cancellationToken);
    Task UpdateNoteAsync(ApplicationNote note, CancellationToken cancellationToken);
    Task DeleteNoteAsync(Guid applicationId, Guid noteId, CancellationToken cancellationToken);
}
