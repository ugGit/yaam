using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Yaam.Domain.Enums;
using Yaam.UseCases.Applications.Dtos;

namespace Yaam.Tests.Integration.Applications;

[Collection("Integration")]
public class ApplicationsEndpointsTests(ApiFactory factory)
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
    public async Task ApplicationCrudFlow_CreateUpdatePatchNotesReadDelete()
    {
        // Create
        var createPayload = new { companyName = "Acme Corp", role = "Software Engineer", status = "Draft" };
        var createResponse = await _client.PostAsJsonAsync("/api/applications", createPayload);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await createResponse.Content.ReadFromJsonAsync<ApplicationDto>();
        created!.CompanyName.Should().Be("Acme Corp");
        created.Status.Should().Be(ApplicationStatus.Draft);

        // Update
        var updatePayload = new
        {
            companyName = "Acme Corp",
            role = "Senior Software Engineer",
            status = "Applied",
            dateApplied = "2026-07-10"
        };
        var updateResponse = await _client.PutAsJsonAsync($"/api/applications/{created.Id}", updatePayload);
        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await updateResponse.Content.ReadFromJsonAsync<ApplicationDto>();
        updated!.Role.Should().Be("Senior Software Engineer");
        updated.Status.Should().Be(ApplicationStatus.Applied);

        // Patch status
        var patchResponse = await _client.PatchAsJsonAsync(
            $"/api/applications/{created.Id}/status", new { status = "Interviewed" });
        patchResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var patched = await patchResponse.Content.ReadFromJsonAsync<ApplicationDto>();
        patched!.Status.Should().Be(ApplicationStatus.Interviewed);

        // Add note
        var addNoteResponse = await _client.PostAsJsonAsync(
            $"/api/applications/{created.Id}/notes", new { body = "Had a great interview." });
        addNoteResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var note = await addNoteResponse.Content.ReadFromJsonAsync<ApplicationNoteDto>();
        note!.Body.Should().Be("Had a great interview.");

        // Update note
        var updateNoteResponse = await _client.PutAsJsonAsync(
            $"/api/applications/{created.Id}/notes/{note.Id}", new { body = "Updated note." });
        updateNoteResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var updatedNote = await updateNoteResponse.Content.ReadFromJsonAsync<ApplicationNoteDto>();
        updatedNote!.Body.Should().Be("Updated note.");

        // Read by id — note present after update
        var getResponse = await _client.GetAsync($"/api/applications/{created.Id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var fetched = await getResponse.Content.ReadFromJsonAsync<ApplicationDto>();
        fetched!.Notes.Should().Contain(n => n.Id == note.Id && n.Body == "Updated note.");

        // Delete note
        var deleteNoteResponse = await _client.DeleteAsync(
            $"/api/applications/{created.Id}/notes/{note.Id}");
        deleteNoteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
        var afterNoteDelete = await _client.GetAsync($"/api/applications/{created.Id}");
        var afterNoteDeleteBody = await afterNoteDelete.Content.ReadFromJsonAsync<ApplicationDto>();
        afterNoteDeleteBody!.Notes.Should().NotContain(n => n.Id == note.Id);

        // Delete application
        var deleteResponse = await _client.DeleteAsync($"/api/applications/{created.Id}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
        var afterDelete = await _client.GetAsync($"/api/applications/{created.Id}");
        afterDelete.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
