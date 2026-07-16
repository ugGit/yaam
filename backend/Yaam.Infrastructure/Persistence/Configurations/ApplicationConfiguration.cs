using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Yaam.Domain.Entities;

namespace Yaam.Infrastructure.Persistence.Configurations;

public class ApplicationConfiguration : IEntityTypeConfiguration<Application>
{
    public void Configure(EntityTypeBuilder<Application> builder)
    {
        builder.HasKey(a => a.Id);
        builder.Property(a => a.CompanyName).IsRequired().HasMaxLength(200);
        builder.Property(a => a.Role).IsRequired().HasMaxLength(350);
        builder.Property(a => a.Status).IsRequired().HasConversion<string>();
        builder.Property(a => a.ContactName).HasMaxLength(200);
        builder.Property(a => a.ContactEmail).HasMaxLength(200);
        builder.Property(a => a.ContactPhone).HasMaxLength(20);

        builder.HasMany(a => a.Notes)
            .WithOne()
            .HasForeignKey(n => n.ApplicationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
