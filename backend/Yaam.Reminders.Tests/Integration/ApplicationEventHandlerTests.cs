using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Yaam.Contracts;
using Yaam.Contracts.Applications;
using Yaam.Reminders.ApplicationEvents;
using Yaam.Reminders.Domain;
using Yaam.Reminders.Persistence;

namespace Yaam.Reminders.Tests.Integration;

[Collection("Reminders")]
public class ApplicationEventHandlerTests(RemindersApiFactory factory)
{
    [Fact]
    public async Task Upserted_CreatesSnapshot()
    {
        var applicationId = Guid.NewGuid();

        var outcome = await HandleAsync(Envelope(Upserted(applicationId, 1, "Acme")));

        outcome.Should().Be(ConsumeOutcome.Processed);
        (await GetSnapshotAsync(applicationId))!.CompanyName.Should().Be("Acme");
    }

    [Fact]
    public async Task SameMessageTwice_IsAppliedOnce()
    {
        var applicationId = Guid.NewGuid();
        await HandleAsync(Envelope(Upserted(applicationId, 1, "Acme")));
        var update = Envelope(Upserted(applicationId, 2, "Acme v2"));

        var first = await HandleAsync(update);
        var second = await HandleAsync(update);

        first.Should().Be(ConsumeOutcome.Processed);
        second.Should().Be(ConsumeOutcome.Duplicate);
    }

    [Fact]
    public async Task OlderVersion_IsIgnoredAsStale()
    {
        var applicationId = Guid.NewGuid();
        await HandleAsync(Envelope(Upserted(applicationId, 3, "Newest")));

        var outcome = await HandleAsync(Envelope(Upserted(applicationId, 2, "Older")));

        outcome.Should().Be(ConsumeOutcome.Stale);
        (await GetSnapshotAsync(applicationId))!.CompanyName.Should().Be("Newest");
    }

    [Fact]
    public async Task Deleted_TombstonesSnapshot_AndDeletesItsReminders()
    {
        var applicationId = Guid.NewGuid();
        await HandleAsync(Envelope(Upserted(applicationId, 1, "Acme")));
        await SeedReminderAsync(applicationId);

        var outcome = await HandleAsync(Envelope(new ApplicationDeleted(applicationId, 2)));

        outcome.Should().Be(ConsumeOutcome.Processed);
        (await GetSnapshotAsync(applicationId))!.IsDeleted.Should().BeTrue();
        (await CountRemindersAsync(applicationId)).Should().Be(0);
    }

    [Fact]
    public async Task UpsertedAfterDeleted_StaysDeleted()
    {
        var applicationId = Guid.NewGuid();
        await HandleAsync(Envelope(Upserted(applicationId, 1, "Acme")));
        await HandleAsync(Envelope(new ApplicationDeleted(applicationId, 3)));

        var outcome = await HandleAsync(Envelope(Upserted(applicationId, 2, "Zombie")));

        outcome.Should().Be(ConsumeOutcome.Stale);
        (await GetSnapshotAsync(applicationId))!.IsDeleted.Should().BeTrue();
    }

    [Fact]
    public async Task DeletedBeforeAnyUpsert_InsertsTombstone()
    {
        var applicationId = Guid.NewGuid();

        await HandleAsync(Envelope(new ApplicationDeleted(applicationId, 2)));
        var late = await HandleAsync(Envelope(Upserted(applicationId, 1, "Late")));

        late.Should().Be(ConsumeOutcome.Stale);
        (await GetSnapshotAsync(applicationId))!.IsDeleted.Should().BeTrue();
    }

    [Fact]
    public async Task UnknownType_IsIgnored_AndRecordedAsProcessed()
    {
        var envelope = new MessageEnvelope(
            Guid.NewGuid(), "ApplicationArchived", DateTimeOffset.UtcNow, JsonDocument.Parse("{}").RootElement);

        var first = await HandleAsync(envelope);
        var second = await HandleAsync(envelope);

        first.Should().Be(ConsumeOutcome.Ignored);
        second.Should().Be(ConsumeOutcome.Duplicate);
    }

    [Fact]
    public async Task MalformedPayload_Throws_SoTheMessageIsNackedAndDeadLettered()
    {
        var envelope = new MessageEnvelope(
            Guid.NewGuid(), nameof(ApplicationUpserted), DateTimeOffset.UtcNow,
            JsonDocument.Parse("""{"applicationId":"not-a-guid"}""").RootElement);

        var act = () => HandleAsync(envelope);

        await act.Should().ThrowAsync<JsonException>();
    }

    private static ApplicationUpserted Upserted(Guid applicationId, long version, string companyName)
        => new(applicationId, version, companyName, "Developer", null, null);

    private static MessageEnvelope Envelope<TEvent>(TEvent integrationEvent) => new(
        Guid.NewGuid(), typeof(TEvent).Name, DateTimeOffset.UtcNow,
        JsonSerializer.SerializeToElement(integrationEvent, ContractSerializer.Options));

    private async Task<ConsumeOutcome> HandleAsync(MessageEnvelope envelope)
    {
        using var scope = factory.Services.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<ApplicationEventHandler>();
        return await handler.HandleAsync(envelope, CancellationToken.None);
    }

    private async Task<ApplicationSnapshot?> GetSnapshotAsync(Guid applicationId)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<RemindersDbContext>();
        return await db.ApplicationSnapshots.AsNoTracking().SingleOrDefaultAsync(s => s.ApplicationId == applicationId);
    }

    private async Task SeedReminderAsync(Guid applicationId)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<RemindersDbContext>();
        db.Reminders.Add(new Reminder { ApplicationId = applicationId, DelayDays = 7, DueDate = new DateOnly(2030, 1, 1) });
        await db.SaveChangesAsync();
    }

    private async Task<int> CountRemindersAsync(Guid applicationId)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<RemindersDbContext>();
        return await db.Reminders.CountAsync(r => r.ApplicationId == applicationId);
    }
}
