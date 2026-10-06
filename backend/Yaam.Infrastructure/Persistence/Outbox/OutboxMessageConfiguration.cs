using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Yaam.Infrastructure.Persistence.Outbox;

public class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.HasKey(m => m.Id);
        builder.Property(m => m.Type).IsRequired().HasMaxLength(200);
        builder.Property(m => m.Payload).IsRequired().HasColumnType("jsonb");
        builder.Property(m => m.TraceParent).HasMaxLength(55);
        builder.HasIndex(m => m.MessageId).IsUnique();
        // Partial index: the relay only ever scans pending rows.
        builder.HasIndex(m => m.Id).HasFilter("\"ProcessedAt\" IS NULL").HasDatabaseName("IX_OutboxMessages_Pending");
    }
}
