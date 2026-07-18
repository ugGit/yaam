using Microsoft.Extensions.Options;
using NSubstitute;
using Yaam.Domain.Entities;
using Yaam.Domain.Repositories;
using Yaam.UseCases.Common.Email;
using Yaam.UseCases.Reminders.Commands;

namespace Yaam.Tests.Unit.Reminders;

public class SendDueRemindersCommandHandlerTests
{
    private readonly IReminderRepository _repo = Substitute.For<IReminderRepository>();
    private readonly IEmailSender _emailSender = Substitute.For<IEmailSender>();
    private readonly EmailSettings _settings = new()
    {
        NotificationEmail = "user@example.com",
        FromAddress = "noreply@yaam.local",
        FromName = "YAAM"
    };

    private SendDueRemindersCommandHandler CreateHandler() =>
        new(_repo, _emailSender, Options.Create(_settings));

    [Fact]
    public async Task Handle_SendsOneEmailPerDueReminder()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var reminder = new Reminder
        {
            ApplicationId = Guid.NewGuid(),
            DelayDays = 7,
            DueDate = today,
            Application = new Application { CompanyName = "Acme Corp", Role = "Dev" },
        };
        _repo.GetDueAsync(today, Arg.Any<CancellationToken>()).Returns([reminder]);

        await CreateHandler().Handle(new SendDueRemindersCommand(), CancellationToken.None);

        await _emailSender.Received(1).SendAsync(
            _settings.NotificationEmail,
            Arg.Is<string>(s => s.Contains("Acme Corp")),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_SetsNotifiedAt_AfterSending()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var reminder = new Reminder
        {
            ApplicationId = Guid.NewGuid(),
            DelayDays = 7,
            DueDate = today,
            Application = new Application { CompanyName = "Acme Corp", Role = "Dev" },
        };
        _repo.GetDueAsync(today, Arg.Any<CancellationToken>()).Returns([reminder]);

        await CreateHandler().Handle(new SendDueRemindersCommand(), CancellationToken.None);

        await _repo.Received(1).UpdateAsync(
            Arg.Is<Reminder>(r => r.NotifiedAt != null),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_DoesNothing_WhenNoDueReminders()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        _repo.GetDueAsync(today, Arg.Any<CancellationToken>()).Returns([]);

        await CreateHandler().Handle(new SendDueRemindersCommand(), CancellationToken.None);

        await _emailSender.DidNotReceive().SendAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_EmailBody_ContainsDraftWithContactName()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var reminder = new Reminder
        {
            ApplicationId = Guid.NewGuid(),
            DelayDays = 14,
            DueDate = today,
            Application = new Application
            {
                CompanyName = "Beta Ltd",
                Role = "QA Engineer",
                ContactName = "Alice Smith",
                DateApplied = today.AddDays(-14),
            },
        };
        _repo.GetDueAsync(today, Arg.Any<CancellationToken>()).Returns([reminder]);

        string? capturedBody = null;
        await _emailSender.SendAsync(
            Arg.Any<string>(), Arg.Any<string>(),
            Arg.Do<string>(b => capturedBody = b),
            Arg.Any<CancellationToken>());

        await CreateHandler().Handle(new SendDueRemindersCommand(), CancellationToken.None);

        Assert.NotNull(capturedBody);
        Assert.Contains("Dear Alice Smith,", capturedBody);
        Assert.Contains("Beta Ltd", capturedBody);
        Assert.Contains("QA Engineer", capturedBody);
    }

    [Fact]
    public async Task Handle_EmailBody_FallsBackToGenericSalutation_WhenNoContactName()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var reminder = new Reminder
        {
            ApplicationId = Guid.NewGuid(),
            DelayDays = 7,
            DueDate = today,
            Application = new Application { CompanyName = "Gamma Inc", Role = "PM" },
        };
        _repo.GetDueAsync(today, Arg.Any<CancellationToken>()).Returns([reminder]);

        string? capturedBody = null;
        await _emailSender.SendAsync(
            Arg.Any<string>(), Arg.Any<string>(),
            Arg.Do<string>(b => capturedBody = b),
            Arg.Any<CancellationToken>());

        await CreateHandler().Handle(new SendDueRemindersCommand(), CancellationToken.None);

        Assert.NotNull(capturedBody);
        Assert.Contains("Dear Hiring Team,", capturedBody);
    }
}
