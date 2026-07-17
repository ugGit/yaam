using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Yaam.Domain.Entities;

namespace Yaam.Infrastructure.Persistence.Configurations;

public class CustomFieldConfiguration : IEntityTypeConfiguration<CustomField>
{
    public void Configure(EntityTypeBuilder<CustomField> builder)
    {
        builder.HasKey(cf => cf.Id);
        builder.Property(cf => cf.Label).IsRequired().HasMaxLength(250);
        builder.Property(cf => cf.Value).IsRequired().HasMaxLength(1000);
    }
}
