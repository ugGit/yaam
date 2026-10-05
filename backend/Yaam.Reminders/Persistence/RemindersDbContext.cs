using Microsoft.EntityFrameworkCore;
using Yaam.Reminders.Domain;

namespace Yaam.Reminders.Persistence;

public class RemindersDbContext(DbContextOptions<RemindersDbContext> options) : DbContext(options)
{
    public DbSet<Reminder> Reminders => Set<Reminder>();
    public DbSet<ApplicationSnapshot> ApplicationSnapshots => Set<ApplicationSnapshot>();
    public DbSet<ProcessedMessage> ProcessedMessages => Set<ProcessedMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Reminder>(builder =>
        {
            builder.HasKey(r => r.Id);
            builder.Property(r => r.Note).HasMaxLength(500);
            builder.HasIndex(r => r.ApplicationId);
        });

        modelBuilder.Entity<ApplicationSnapshot>(builder =>
        {
            builder.HasKey(s => s.ApplicationId);
            builder.Property(s => s.ApplicationId).ValueGeneratedNever();
            builder.Property(s => s.CompanyName).IsRequired().HasMaxLength(200);
            builder.Property(s => s.Role).IsRequired().HasMaxLength(350);
            builder.Property(s => s.ContactName).HasMaxLength(200);
        });

        modelBuilder.Entity<ProcessedMessage>(builder =>
        {
            builder.HasKey(m => m.MessageId);
            builder.Property(m => m.MessageId).ValueGeneratedNever();
        });
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        foreach (var entry in ChangeTracker.Entries<Reminder>())
        {
            if (entry.State == EntityState.Modified)
                entry.Entity.UpdatedAt = DateTime.UtcNow;
        }
        return base.SaveChangesAsync(cancellationToken);
    }
}
