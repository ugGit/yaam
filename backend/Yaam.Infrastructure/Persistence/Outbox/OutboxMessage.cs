namespace Yaam.Infrastructure.Persistence.Outbox;

public class OutboxMessage
{
    public long Id { get; init; }
    public Guid MessageId { get; init; }
    public required string Type { get; init; }
    public Guid AggregateId { get; init; }
    public required string Payload { get; init; }
    public string? TraceParent { get; init; }
    public DateTime OccurredAt { get; init; }
    public DateTime? ProcessedAt { get; set; }
    public int Attempts { get; set; }
}
