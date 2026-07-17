using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Yaam.Domain.Entities;

namespace Yaam.Infrastructure.Persistence.Configurations;

public class ProfileLinkConfiguration : IEntityTypeConfiguration<ProfileLink>
{
    public void Configure(EntityTypeBuilder<ProfileLink> builder)
    {
        builder.HasKey(l => l.Id);
        builder.Property(l => l.Label).IsRequired().HasMaxLength(100);
        builder.Property(l => l.Url).IsRequired().HasMaxLength(500);
    }
}
