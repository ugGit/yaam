using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Yaam.Contracts;
using Yaam.Contracts.Applications;
using Yaam.Reminders.Domain;
using Yaam.Reminders.Persistence;

namespace Yaam.Reminders.ApplicationEvents;

public sealed class ApplicationEventHandler(RemindersDbContext db, TimeProvider timeProvider)
{
    public async Task<ConsumeOutcome> HandleAsync(MessageEnvelope envelope, CancellationToken cancellationToken)
    {
        // Inbox check, state change and inbox insert share one transaction: a redelivery after a crash
        // either finds the message recorded (skip) or finds nothing applied (redo).
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        if (await db.ProcessedMessages.AnyAsync(m => m.MessageId == envelope.MessageId, cancellationToken))
            return ConsumeOutcome.Duplicate;

        var outcome = envelope.Type switch
        {
            nameof(ApplicationUpserted) => await ApplyAsync(Read<ApplicationUpserted>(envelope), cancellationToken),
            nameof(ApplicationDeleted) => await ApplyAsync(Read<ApplicationDeleted>(envelope), cancellationToken),
            // Tolerant reader: a newer producer may emit types this consumer doesn't know yet.
            _ => ConsumeOutcome.Ignored,
        };

        db.ProcessedMessages.Add(new ProcessedMessage
        {
            MessageId = envelope.MessageId,
            ProcessedAt = timeProvider.GetUtcNow().UtcDateTime,
        });
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return outcome;
    }

    private async Task<ConsumeOutcome> ApplyAsync(ApplicationUpserted upserted, CancellationToken cancellationToken)
    {
        var snapshot = await db.ApplicationSnapshots.FindAsync([upserted.ApplicationId], cancellationToken);
        if (snapshot is null)
        {
            db.ApplicationSnapshots.Add(new ApplicationSnapshot
            {
                ApplicationId = upserted.ApplicationId,
                Version = upserted.Version,
                CompanyName = upserted.CompanyName,
                Role = upserted.Role,
                DateApplied = upserted.DateApplied,
                ContactName = upserted.ContactName,
            });
            return ConsumeOutcome.Processed;
        }

        if (upserted.Version <= snapshot.Version)
            return ConsumeOutcome.Stale;

        snapshot.Version = upserted.Version;
        snapshot.CompanyName = upserted.CompanyName;
        snapshot.Role = upserted.Role;
        snapshot.DateApplied = upserted.DateApplied;
        snapshot.ContactName = upserted.ContactName;
        return ConsumeOutcome.Processed;
    }

    private async Task<ConsumeOutcome> ApplyAsync(ApplicationDeleted deleted, CancellationToken cancellationToken)
    {
        var snapshot = await db.ApplicationSnapshots.FindAsync([deleted.ApplicationId], cancellationToken);
        if (snapshot is null)
        {
            // Tombstone even without prior state, so a late upsert can't resurrect the application.
            db.ApplicationSnapshots.Add(new ApplicationSnapshot
            {
                ApplicationId = deleted.ApplicationId,
                Version = deleted.Version,
                CompanyName = string.Empty,
                Role = string.Empty,
                IsDeleted = true,
            });
            return ConsumeOutcome.Processed;
        }

        if (deleted.Version <= snapshot.Version)
            return ConsumeOutcome.Stale;

        snapshot.Version = deleted.Version;
        snapshot.IsDeleted = true;
        await db.Reminders
            .Where(r => r.ApplicationId == deleted.ApplicationId)
            .ExecuteDeleteAsync(cancellationToken);
        return ConsumeOutcome.Processed;
    }

    private static TEvent Read<TEvent>(MessageEnvelope envelope)
        => envelope.Payload.Deserialize<TEvent>(ContractSerializer.Options)
           ?? throw new JsonException($"Empty {typeof(TEvent).Name} payload.");
}
