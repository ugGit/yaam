using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Yaam.UseCases.Applications.Dtos;
using Yaam.Domain.Enums;

namespace Yaam.Tests.Integration.Applications;

public class ApplicationsEndpointsTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task GET_Applications_ReturnsEmptyList_WhenNoApplicationsExist()
    {
        var response = await _client.GetAsync("/api/applications");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<List<ApplicationSummaryDto>>();
        body.Should().NotBeNull();
    }

    [Fact]
    public async Task POST_Applications_Returns201_WithValidDraftApplication()
    {
        var payload = new
        {
            companyName = "Acme Corp",
            role = "Software Engineer",
            status = "Draft"
        };

        var response = await _client.PostAsJsonAsync("/api/applications", payload);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<ApplicationDto>();
        body!.CompanyName.Should().Be("Acme Corp");
        body.Status.Should().Be(ApplicationStatus.Draft);
        body.DateApplied.Should().BeNull();
    }

    [Fact]
    public async Task POST_Applications_Returns400_WhenDateAppliedMissingForNonDraft()
    {
        var payload = new
        {
            companyName = "Acme Corp",
            role = "Engineer",
            status = "Applied"
        };

        var response = await _client.PostAsJsonAsync("/api/applications", payload);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GET_ApplicationById_Returns404_ForUnknownId()
    {
        var response = await _client.GetAsync($"/api/applications/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task PUT_Application_UpdatesAndReturns200()
    {
        var created = await CreateApplicationAsync("Update Test Co", "Dev");

        var update = new
        {
            companyName = "Updated Co",
            role = "Dev",
            status = "Applied",
            dateApplied = "2026-07-10"
        };

        var response = await _client.PutAsJsonAsync($"/api/applications/{created.Id}", update);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApplicationDto>();
        body!.CompanyName.Should().Be("Updated Co");
    }

    [Fact]
    public async Task PATCH_ApplicationStatus_UpdatesStatusImmediately()
    {
        var created = await CreateApplicationAsync("Status Test Co", "QA");

        var patch = new { status = "Applied" };
        var response = await _client.PatchAsJsonAsync($"/api/applications/{created.Id}/status", patch);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApplicationDto>();
        body!.Status.Should().Be(ApplicationStatus.Applied);
    }

    [Fact]
    public async Task DELETE_Application_Returns204_AndRemovesEntity()
    {
        var created = await CreateApplicationAsync("Delete Test Co", "PM");

        var deleteResponse = await _client.DeleteAsync($"/api/applications/{created.Id}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var getResponse = await _client.GetAsync($"/api/applications/{created.Id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task POST_Note_Returns201_WithValidBody()
    {
        var app = await CreateApplicationAsync("Note Test Co", "Dev");

        var payload = new { body = "Had a great call with the recruiter." };
        var response = await _client.PostAsJsonAsync(
            $"/api/applications/{app.Id}/notes", payload);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var note = await response.Content.ReadFromJsonAsync<ApplicationNoteDto>();
        note!.Body.Should().Be("Had a great call with the recruiter.");
    }

    [Fact]
    public async Task PUT_Note_UpdatesBody()
    {
        var app = await CreateApplicationAsync("Note Update Co", "Dev");
        var note = await CreateNoteAsync(app.Id, "Original note.");

        var update = new { body = "Updated note." };
        var response = await _client.PutAsJsonAsync(
            $"/api/applications/{app.Id}/notes/{note.Id}", update);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApplicationNoteDto>();
        body!.Body.Should().Be("Updated note.");
    }

    [Fact]
    public async Task DELETE_Note_Returns204_AndRemovesNote()
    {
        var app = await CreateApplicationAsync("Note Delete Co", "Dev");
        var note = await CreateNoteAsync(app.Id, "To be deleted.");

        var deleteResponse = await _client.DeleteAsync(
            $"/api/applications/{app.Id}/notes/{note.Id}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var getResponse = await _client.GetAsync($"/api/applications/{app.Id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await getResponse.Content.ReadFromJsonAsync<ApplicationDto>();
        body!.Notes.Should().NotContain(n => n.Id == note.Id);
    }

    private async Task<ApplicationDto> CreateApplicationAsync(string company, string role)
    {
        var payload = new { companyName = company, role, status = "Draft" };
        var response = await _client.PostAsJsonAsync("/api/applications", payload);
        return (await response.Content.ReadFromJsonAsync<ApplicationDto>())!;
    }

    private async Task<ApplicationNoteDto> CreateNoteAsync(Guid applicationId, string body)
    {
        var response = await _client.PostAsJsonAsync(
            $"/api/applications/{applicationId}/notes", new { body });
        return (await response.Content.ReadFromJsonAsync<ApplicationNoteDto>())!;
    }
}
