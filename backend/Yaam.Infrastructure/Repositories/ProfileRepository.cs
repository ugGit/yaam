using Microsoft.EntityFrameworkCore;
using Yaam.Domain.Entities;
using Yaam.Domain.Repositories;
using Yaam.Infrastructure.Persistence;

namespace Yaam.Infrastructure.Repositories;

public class ProfileRepository(AppDbContext context) : IProfileRepository
{
    public async Task<Profile?> GetAsync(CancellationToken cancellationToken)
    {
        return await context.Profiles
            .Include(p => p.WorkExperiences)
            .Include(p => p.Educations)
            .Include(p => p.Languages)
            .Include(p => p.Certifications)
            .Include(p => p.Links)
            .Include(p => p.CustomFields)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<Profile> GetOrCreateAsync(CancellationToken cancellationToken)
    {
        var profile = await GetAsync(cancellationToken);
        if (profile is not null) return profile;

        profile = new Profile();
        context.Profiles.Add(profile);
        await context.SaveChangesAsync(cancellationToken);
        return profile;
    }

    public void Add<TEntity>(TEntity entity) where TEntity : Entity
    {
        context.Add(entity);
    }

    public async Task UpdateAsync(CancellationToken cancellationToken)
    {
        await context.SaveChangesAsync(cancellationToken);
    }
}
