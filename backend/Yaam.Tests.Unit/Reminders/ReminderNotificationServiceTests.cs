using FluentAssertions;
using Yaam.API.Reminders;

namespace Yaam.Tests.Unit.Reminders;

public class ReminderNotificationServiceTests
{
    [Fact]
    public void TimeUntilNextRun_Before8Am_ReturnsDelayToSameDay8Am()
    {
        var now = DateTime.UtcNow.Date.AddHours(6);
        var expected = now.Date.AddHours(8) - now;

        var actual = ReminderNotificationService.TimeUntilNextRun(now);

        actual.Should().BeCloseTo(expected, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void TimeUntilNextRun_After8Am_ReturnsDelayToNextDay8Am()
    {
        var now = DateTime.UtcNow.Date.AddHours(10);
        var expected = now.Date.AddDays(1).AddHours(8) - now;

        var actual = ReminderNotificationService.TimeUntilNextRun(now);

        actual.Should().BeCloseTo(expected, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void TimeUntilNextRun_Exactly8Am_ReturnsDelayToNextDay8Am()
    {
        var now = DateTime.UtcNow.Date.AddHours(8);
        var expected = now.AddDays(1) - now;

        var actual = ReminderNotificationService.TimeUntilNextRun(now);

        actual.Should().BeCloseTo(expected, TimeSpan.FromSeconds(1));
    }
}
