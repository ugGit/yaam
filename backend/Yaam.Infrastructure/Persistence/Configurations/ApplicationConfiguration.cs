using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Yaam.Domain.Enums;
using Yaam.Domain.Entities;

namespace Yaam.Infrastructure.Persistence.Configurations;

public class ApplicationConfiguration : IEntityTypeConfiguration<global::Yaam.Domain.Entities.Application>
{
    public void Configure(EntityTypeBuilder<global::Yaam.Domain.Entities.Application> builder)
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
            .HasForeignKey((ApplicationNote n) => n.ApplicationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
