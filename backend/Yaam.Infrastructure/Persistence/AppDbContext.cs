using Microsoft.EntityFrameworkCore;
using Yaam.Domain.Entities;
using DomainApplication = Yaam.Domain.Entities.Application;
using DomainApplicationNote = Yaam.Domain.Entities.ApplicationNote;

namespace Yaam.Infrastructure.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<DomainApplication> Applications => Set<DomainApplication>();
    public DbSet<DomainApplicationNote> ApplicationNotes => Set<DomainApplicationNote>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        foreach (var entry in ChangeTracker.Entries<Entity>())
        {
            if (entry.State == EntityState.Modified)
                entry.Entity.UpdatedAt = DateTime.UtcNow;
        }
        return base.SaveChangesAsync(cancellationToken);
    }
}
