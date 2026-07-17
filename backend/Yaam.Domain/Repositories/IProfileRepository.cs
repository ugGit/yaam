using Yaam.Domain.Entities;

namespace Yaam.Domain.Repositories;

public interface IProfileRepository
{
    Task<Profile?> GetAsync(CancellationToken cancellationToken);
    Task<Profile> GetOrCreateAsync(CancellationToken cancellationToken);
    Task UpdateAsync(CancellationToken cancellationToken);
}
