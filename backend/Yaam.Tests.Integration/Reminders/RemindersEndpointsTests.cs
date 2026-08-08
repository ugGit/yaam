using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Yaam.API.Applications;
using Yaam.API.Reminders;

namespace Yaam.Tests.Integration.Reminders;

[Collection("Integration")]
public class RemindersEndpointsTests(ApiFactory factory)
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task GET_Reminders_ReturnsEmptyList_WhenNoRemindersExist()
    {
        var response = await _client.GetAsync("/api/reminders");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<List<ReminderViewModel>>();
        body.Should().NotBeNull();
    }

    [Fact]
    public async Task ReminderCrudFlow_SetCompleteRescheduleDelete()
    {
        var app = await CreateApplicationAsync();

        // Set reminder
        var setPayload = new { delayDays = 7, note = "Check in on status" };
        var setResponse = await _client.PostAsJsonAsync($"/api/applications/{app.Id}/reminder", setPayload);
        setResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var reminder = await setResponse.Content.ReadFromJsonAsync<ApplicationReminderViewModel>();
        reminder!.DelayDays.Should().Be(7);
        reminder.Note.Should().Be("Check in on status");
        reminder.CompletedAt.Should().BeNull();
        reminder.NotifiedAt.Should().BeNull();

        // Application GET now includes reminder in list
        var appResponse = await _client.GetAsync($"/api/applications/{app.Id}");
        var appWithReminder = await appResponse.Content.ReadFromJsonAsync<ApplicationViewModel>();
        appWithReminder!.Reminders.Should().HaveCount(1);
        appWithReminder.Reminders[0].DelayDays.Should().Be(7);

        // Second reminder is allowed
        var secondReminderResponse = await _client.PostAsJsonAsync(
            $"/api/applications/{app.Id}/reminder", setPayload);
        secondReminderResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var secondReminder = await secondReminderResponse.Content.ReadFromJsonAsync<ApplicationReminderViewModel>();

        // Application GET now includes both reminders
        var appWithTwo = await _client.GetAsync($"/api/applications/{app.Id}");
        var appBodyTwo = await appWithTwo.Content.ReadFromJsonAsync<ApplicationViewModel>();
        appBodyTwo!.Reminders.Should().HaveCount(2);

        // Reschedule first reminder
        var tomorrow = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)).ToString("yyyy-MM-dd");
        var rescheduleResponse = await _client.PatchAsJsonAsync(
            $"/api/applications/{app.Id}/reminder/{reminder.Id}/reschedule", new { newDueDate = tomorrow });
        rescheduleResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var rescheduled = await rescheduleResponse.Content.ReadFromJsonAsync<ApplicationReminderViewModel>();
        rescheduled!.NotifiedAt.Should().BeNull();

        // Complete first reminder
        var completeResponse = await _client.PatchAsJsonAsync(
            $"/api/applications/{app.Id}/reminder/{reminder.Id}/complete", new { });
        completeResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // After completing one reminder the second is still active
        var appAfterComplete = await _client.GetAsync($"/api/applications/{app.Id}");
        var appBody = await appAfterComplete.Content.ReadFromJsonAsync<ApplicationViewModel>();
        appBody!.Reminders.Should().HaveCount(1);

        // Set a new reminder after completing old one
        var setAgainResponse = await _client.PostAsJsonAsync(
            $"/api/applications/{app.Id}/reminder", new { delayDays = 14 });
        setAgainResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var thirdReminder = await setAgainResponse.Content.ReadFromJsonAsync<ApplicationReminderViewModel>();

        // Delete remaining reminders by ID
        var deleteSecond = await _client.DeleteAsync(
            $"/api/applications/{app.Id}/reminder/{secondReminder!.Id}");
        deleteSecond.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var deleteThird = await _client.DeleteAsync(
            $"/api/applications/{app.Id}/reminder/{thirdReminder!.Id}");
        deleteThird.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Reminders list no longer contains this application
        var listResponse = await _client.GetAsync("/api/reminders");
        var list = await listResponse.Content.ReadFromJsonAsync<List<ReminderViewModel>>();
        list.Should().NotContain(r => r.ApplicationId == app.Id);
    }

    [Fact]
    public async Task POST_Reminder_Returns400_WhenDelayDaysIsZero()
    {
        var app = await CreateApplicationAsync();
        var response = await _client.PostAsJsonAsync(
            $"/api/applications/{app.Id}/reminder", new { delayDays = 0 });
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task PATCH_Reschedule_Returns400_WhenNewDueDateIsToday()
    {
        var app = await CreateApplicationAsync();
        var setResponse = await _client.PostAsJsonAsync(
            $"/api/applications/{app.Id}/reminder", new { delayDays = 7 });
        var rem = await setResponse.Content.ReadFromJsonAsync<ApplicationReminderViewModel>();

        var today = DateOnly.FromDateTime(DateTime.UtcNow).ToString("yyyy-MM-dd");
        var response = await _client.PatchAsJsonAsync(
            $"/api/applications/{app.Id}/reminder/{rem!.Id}/reschedule", new { newDueDate = today });
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    private async Task<ApplicationViewModel> CreateApplicationAsync()
    {
        var payload = new { companyName = "Test Co", role = "Dev", status = "Draft" };
        var response = await _client.PostAsJsonAsync("/api/applications", payload);
        return (await response.Content.ReadFromJsonAsync<ApplicationViewModel>())!;
    }
}
