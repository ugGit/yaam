using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Yaam.Reminders.Domain;
using Yaam.Reminders.Persistence;

namespace Yaam.Reminders.Tests.Integration;

[Collection("Reminders")]
public class ReminderPersistenceTests(RemindersApiFactory factory)
{
    [Fact]
    public async Task Reminder_WithoutApplicationSnapshot_IsRejectedByTheDatabase()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<RemindersDbContext>();
        db.Reminders.Add(new Reminder
        {
            ApplicationId = Guid.NewGuid(),
            DelayDays = 7,
            DueDate = new DateOnly(2030, 1, 1),
        });

        var save = () => db.SaveChangesAsync();

        await save.Should().ThrowAsync<DbUpdateException>();
    }
}
