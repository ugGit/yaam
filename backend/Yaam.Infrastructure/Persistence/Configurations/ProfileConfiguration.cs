using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Yaam.Domain.Entities;

namespace Yaam.Infrastructure.Persistence.Configurations;

public class ProfileConfiguration : IEntityTypeConfiguration<Profile>
{
    public void Configure(EntityTypeBuilder<Profile> builder)
    {
        builder.HasKey(p => p.Id);
        builder.Property(p => p.FirstName).HasMaxLength(100);
        builder.Property(p => p.LastName).HasMaxLength(100);
        builder.Property(p => p.Email).HasMaxLength(200);
        builder.Property(p => p.Phone).HasMaxLength(30);
        builder.Property(p => p.Location).HasMaxLength(200);
        builder.Property(p => p.Summary).HasMaxLength(2000);

        var skillsProp = builder.Property(p => p.Skills)
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null) ?? new())
            .HasColumnType("text");
        skillsProp.Metadata.SetValueComparer(new ValueComparer<List<string>>(
            (c1, c2) => c1 != null && c2 != null && c1.SequenceEqual(c2),
            c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
            c => c.ToList()));

        builder.HasMany(p => p.WorkExperiences)
            .WithOne()
            .HasForeignKey(w => w.ProfileId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(p => p.Educations)
            .WithOne()
            .HasForeignKey(e => e.ProfileId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(p => p.Languages)
            .WithOne()
            .HasForeignKey(l => l.ProfileId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(p => p.Certifications)
            .WithOne()
            .HasForeignKey(c => c.ProfileId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(p => p.Links)
            .WithOne()
            .HasForeignKey(l => l.ProfileId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(p => p.CustomFields)
            .WithOne()
            .HasForeignKey(cf => cf.ProfileId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
