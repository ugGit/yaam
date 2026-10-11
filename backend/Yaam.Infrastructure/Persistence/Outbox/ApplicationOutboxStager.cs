using System.Diagnostics;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Yaam.Contracts;
using Yaam.Contracts.Applications;
using Yaam.Domain.Entities;

namespace Yaam.Infrastructure.Persistence.Outbox;

// Transitional (ADR 008): derives integration events from the ChangeTracker diff.
// Extracted services raise domain events instead.
internal static class ApplicationOutboxStager
{
    internal static void Stage(AppDbContext db)
    {
        var occurredAt = DateTime.UtcNow;
        var traceParent = Activity.Current?.Id;
        var messages = new List<OutboxMessage>();

        foreach (var entry in db.ChangeTracker.Entries<Application>())
        {
            var message = ToOutboxMessage(entry, occurredAt, traceParent);
            if (message is not null)
                messages.Add(message);
        }

        db.OutboxMessages.AddRange(messages);
    }

    private static OutboxMessage? ToOutboxMessage(
        EntityEntry<Application> entry, DateTime occurredAt, string? traceParent)
    {
        var application = entry.Entity;
        switch (entry.State)
        {
            case EntityState.Added:
                application.Version = 1;
                return Create(application.Id, ToUpserted(application), occurredAt, traceParent);
            case EntityState.Modified:
                application.Version++;
                return Create(application.Id, ToUpserted(application), occurredAt, traceParent);
            case EntityState.Deleted:
                return Create(
                    application.Id,
                    new ApplicationDeleted(application.Id, application.Version + 1),
                    occurredAt,
                    traceParent);
            default:
                return null;
        }
    }

    private static ApplicationUpserted ToUpserted(Application application) => new(
        application.Id, application.Version, application.CompanyName,
        application.Role, application.DateApplied, application.ContactName);

    private static OutboxMessage Create<TEvent>(
        Guid aggregateId, TEvent integrationEvent, DateTime occurredAt, string? traceParent) => new()
        {
            MessageId = Guid.NewGuid(),
            Type = typeof(TEvent).Name,
            AggregateId = aggregateId,
            Payload = JsonSerializer.Serialize(integrationEvent, ContractSerializer.Options),
            TraceParent = traceParent,
            OccurredAt = occurredAt,
        };
}
