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

        // Application GET now includes reminder
        var appResponse = await _client.GetAsync($"/api/applications/{app.Id}");
        var appWithReminder = await appResponse.Content.ReadFromJsonAsync<ApplicationViewModel>();
        appWithReminder!.Reminder.Should().NotBeNull();
        appWithReminder.Reminder!.DelayDays.Should().Be(7);

        // Second set is rejected
        var conflictResponse = await _client.PostAsJsonAsync(
            $"/api/applications/{app.Id}/reminder", setPayload);
        conflictResponse.StatusCode.Should().Be(HttpStatusCode.Conflict);

        // Reschedule
        var tomorrow = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)).ToString("yyyy-MM-dd");
        var rescheduleResponse = await _client.PatchAsJsonAsync(
            $"/api/applications/{app.Id}/reminder/reschedule", new { newDueDate = tomorrow });
        rescheduleResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var rescheduled = await rescheduleResponse.Content.ReadFromJsonAsync<ApplicationReminderViewModel>();
        rescheduled!.NotifiedAt.Should().BeNull();

        // Complete
        var completeResponse = await _client.PatchAsJsonAsync(
            $"/api/applications/{app.Id}/reminder/complete", new { });
        completeResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // After complete, app GET shows no active reminder
        var appAfterComplete = await _client.GetAsync($"/api/applications/{app.Id}");
        var appBody = await appAfterComplete.Content.ReadFromJsonAsync<ApplicationViewModel>();
        appBody!.Reminder.Should().BeNull();

        // Set a new reminder after completing old one
        var setAgainResponse = await _client.PostAsJsonAsync(
            $"/api/applications/{app.Id}/reminder", new { delayDays = 14 });
        setAgainResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        // Delete
        var deleteResponse = await _client.DeleteAsync($"/api/applications/{app.Id}/reminder");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Reminders list is empty
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
        await _client.PostAsJsonAsync($"/api/applications/{app.Id}/reminder", new { delayDays = 7 });

        var today = DateOnly.FromDateTime(DateTime.UtcNow).ToString("yyyy-MM-dd");
        var response = await _client.PatchAsJsonAsync(
            $"/api/applications/{app.Id}/reminder/reschedule", new { newDueDate = today });
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    private async Task<ApplicationViewModel> CreateApplicationAsync()
    {
        var payload = new { companyName = "Test Co", role = "Dev", status = "Draft" };
        var response = await _client.PostAsJsonAsync("/api/applications", payload);
        return (await response.Content.ReadFromJsonAsync<ApplicationViewModel>())!;
    }
}
