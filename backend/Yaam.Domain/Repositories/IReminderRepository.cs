using Yaam.Domain.Entities;

namespace Yaam.Domain.Repositories;

public interface IReminderRepository
{
    Task<Reminder?> GetByApplicationIdAsync(Guid applicationId, CancellationToken cancellationToken);
    Task<List<Reminder>> GetAllActiveAsync(CancellationToken cancellationToken);
    Task<List<Reminder>> GetDueAsync(DateOnly today, CancellationToken cancellationToken);
    Task<Reminder> AddAsync(Reminder reminder, CancellationToken cancellationToken);
    Task UpdateAsync(Reminder reminder, CancellationToken cancellationToken);
    Task DeleteByApplicationIdAsync(Guid applicationId, CancellationToken cancellationToken);
}
