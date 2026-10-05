using FluentAssertions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Yaam.Reminders.Domain;
using Yaam.Reminders.Notifications;
using Yaam.Reminders.Persistence;

namespace Yaam.Reminders.Tests.Integration;

[Collection("Reminders")]
public class SendDueRemindersTests(RemindersApiFactory factory)
{
    [Fact]
    public async Task SendsOneEmailPerDueReminder_AndNotAgainOnNextRun()
    {
        var snapshot = RemindersApiFactory.NewSnapshot("Due Co");
        await factory.SeedSnapshotAsync(snapshot);
        var reminderId = await SeedDueReminderAsync(snapshot.ApplicationId);
        factory.EmailSender.Clear();

        await RunJobAsync();
        await RunJobAsync();

        factory.EmailSender.Sent.Where(e => e.Subject.Contains("Due Co")).Should().ContainSingle()
            .Which.Body.Should().Contain("Dear Grace,");
        (await GetReminderAsync(reminderId)).NotifiedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task SkipsRemindersOfDeletedApplications()
    {
        var snapshot = RemindersApiFactory.NewSnapshot("Gone Co");
        snapshot.IsDeleted = true;
        await factory.SeedSnapshotAsync(snapshot);
        await SeedDueReminderAsync(snapshot.ApplicationId);
        factory.EmailSender.Clear();

        await RunJobAsync();

        factory.EmailSender.Sent.Should().NotContain(e => e.Subject.Contains("Gone Co"));
    }

    private async Task RunJobAsync()
    {
        using var scope = factory.Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<IMediator>().Send(
            new SendDueRemindersCommand(),
            CancellationToken.None);
    }

    private async Task<Guid> SeedDueReminderAsync(Guid applicationId)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<RemindersDbContext>();
        var reminder = new Reminder
        {
            ApplicationId = applicationId,
            DelayDays = 7,
            DueDate = DateOnly.FromDateTime(DateTime.UtcNow),
        };
        db.Reminders.Add(reminder);
        await db.SaveChangesAsync();
        return reminder.Id;
    }

    private async Task<Reminder> GetReminderAsync(Guid reminderId)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<RemindersDbContext>();
        return await db.Reminders.AsNoTracking().SingleAsync(r => r.Id == reminderId);
    }
}
