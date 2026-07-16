using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Yaam.Domain.Entities;

namespace Yaam.Infrastructure.Persistence.Configurations;

public class EducationConfiguration : IEntityTypeConfiguration<Education>
{
    public void Configure(EntityTypeBuilder<Education> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Institution).IsRequired().HasMaxLength(200);
        builder.Property(e => e.Degree).HasMaxLength(200);
        builder.Property(e => e.FieldOfStudy).HasMaxLength(200);
    }
}
