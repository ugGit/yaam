using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Yaam.Reminders.Reminders.Api;

namespace Yaam.Reminders.Tests.Integration;

[Collection("Reminders")]
public class RemindersEndpointsTests(RemindersApiFactory factory)
{
    private readonly HttpClient _client = factory.CreateAuthenticatedClient();

    [Fact]
    public async Task GET_Reminders_Returns401_WithoutToken()
    {
        var response = await factory.CreateClient().GetAsync("/api/reminders");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task POST_Reminder_Returns404_WhenApplicationSnapshotIsUnknown()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/reminders", new { applicationId = Guid.NewGuid(), delayDays = 7 });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task POST_Reminder_Returns404_WhenApplicationIsDeleted()
    {
        var snapshot = RemindersApiFactory.NewSnapshot("Deleted Co");
        snapshot.IsDeleted = true;
        await factory.SeedSnapshotAsync(snapshot);

        var response = await _client.PostAsJsonAsync(
            "/api/reminders", new { applicationId = snapshot.ApplicationId, delayDays = 7 });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ReminderCrudFlow_CreateRescheduleCompleteDelete()
    {
        var snapshot = RemindersApiFactory.NewSnapshot("Flow Co");
        await factory.SeedSnapshotAsync(snapshot);

        var createResponse = await _client.PostAsJsonAsync(
            "/api/reminders",
            new { applicationId = snapshot.ApplicationId, delayDays = 7, note = "Check in on status" });
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var first = (await createResponse.Content.ReadFromJsonAsync<ReminderViewModel>())!;
        first.CompanyName.Should().Be("Flow Co");
        first.DueDate.Should().Be(DateOnly.FromDateTime(DateTime.UtcNow).AddDays(7));
        first.Note.Should().Be("Check in on status");

        var second = (await (await _client.PostAsJsonAsync(
            "/api/reminders", new { applicationId = snapshot.ApplicationId, delayDays = 14 }))
            .Content.ReadFromJsonAsync<ReminderViewModel>())!;

        (await ListForAsync(snapshot.ApplicationId)).Should().HaveCount(2);

        var tomorrow = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1));
        var rescheduleResponse = await _client.PatchAsJsonAsync(
            $"/api/reminders/{first.Id}/reschedule", new { newDueDate = tomorrow.ToString("yyyy-MM-dd") });
        rescheduleResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        (await rescheduleResponse.Content.ReadFromJsonAsync<ReminderViewModel>())!
            .DueDate.Should().Be(tomorrow);

        var completeResponse = await _client.PatchAsJsonAsync($"/api/reminders/{first.Id}/complete", new { });
        completeResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await ListForAsync(snapshot.ApplicationId)).Should().ContainSingle(r => r.Id == second.Id);

        var deleteResponse = await _client.DeleteAsync($"/api/reminders/{second.Id}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await ListForAsync(snapshot.ApplicationId)).Should().BeEmpty();
    }

    [Fact]
    public async Task GET_Reminders_WithoutFilter_ReturnsActiveRemindersAcrossApplications()
    {
        var snapshotA = RemindersApiFactory.NewSnapshot("A Co");
        var snapshotB = RemindersApiFactory.NewSnapshot("B Co");
        await factory.SeedSnapshotAsync(snapshotA);
        await factory.SeedSnapshotAsync(snapshotB);
        await _client.PostAsJsonAsync("/api/reminders", new { applicationId = snapshotA.ApplicationId, delayDays = 3 });
        await _client.PostAsJsonAsync("/api/reminders", new { applicationId = snapshotB.ApplicationId, delayDays = 1 });

        var all = await _client.GetFromJsonAsync<List<ReminderViewModel>>("/api/reminders");

        all!.Should().Contain(r => r.ApplicationId == snapshotA.ApplicationId)
            .And.Contain(r => r.ApplicationId == snapshotB.ApplicationId);
        all.Select(r => r.DueDate).Should().BeInAscendingOrder();
    }

    [Fact]
    public async Task POST_Reminder_Returns400_WhenDelayDaysIsZero()
    {
        var snapshot = RemindersApiFactory.NewSnapshot("Validation Co");
        await factory.SeedSnapshotAsync(snapshot);

        var response = await _client.PostAsJsonAsync(
            "/api/reminders", new { applicationId = snapshot.ApplicationId, delayDays = 0 });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task PATCH_Reschedule_Returns400_WhenNewDueDateIsToday()
    {
        var snapshot = RemindersApiFactory.NewSnapshot("Reschedule Co");
        await factory.SeedSnapshotAsync(snapshot);
        var reminder = (await (await _client.PostAsJsonAsync(
            "/api/reminders", new { applicationId = snapshot.ApplicationId, delayDays = 7 }))
            .Content.ReadFromJsonAsync<ReminderViewModel>())!;

        var today = DateOnly.FromDateTime(DateTime.UtcNow).ToString("yyyy-MM-dd");
        var response = await _client.PatchAsJsonAsync(
            $"/api/reminders/{reminder.Id}/reschedule", new { newDueDate = today });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    private async Task<List<ReminderViewModel>> ListForAsync(Guid applicationId)
        => (await _client.GetFromJsonAsync<List<ReminderViewModel>>($"/api/reminders?applicationId={applicationId}"))!;
}
