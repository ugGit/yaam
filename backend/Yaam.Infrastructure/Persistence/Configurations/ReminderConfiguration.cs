using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Yaam.Domain.Entities;

namespace Yaam.Infrastructure.Persistence.Configurations;

public class ReminderConfiguration : IEntityTypeConfiguration<Reminder>
{
    public void Configure(EntityTypeBuilder<Reminder> builder)
    {
        builder.HasKey(r => r.Id);
        builder.Property(r => r.DelayDays).IsRequired();
        builder.Property(r => r.DueDate).IsRequired();
        builder.Property(r => r.Note).HasMaxLength(500);

        builder.HasOne(r => r.Application)
            .WithOne(a => a.Reminder)
            .HasForeignKey<Reminder>(r => r.ApplicationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(r => r.ApplicationId)
            .IsUnique()
            .HasFilter("\"CompletedAt\" IS NULL");
    }
}
