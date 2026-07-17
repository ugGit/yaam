using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Yaam.UseCases.Profile.Dtos;

namespace Yaam.Tests.Integration.Profile;

[Collection("Integration")]
public class ProfileEndpointsTests(ApiFactory factory)
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task GET_Profile_ReturnsProfile()
    {
        var response = await _client.GetAsync("/api/profile");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var profile = await response.Content.ReadFromJsonAsync<ProfileDto>();
        profile.Should().NotBeNull();
        profile!.Id.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GET_Profile_ReturnsSameId_OnSubsequentCalls()
    {
        var r1 = await _client.GetAsync("/api/profile");
        var p1 = await r1.Content.ReadFromJsonAsync<ProfileDto>();

        var r2 = await _client.GetAsync("/api/profile");
        var p2 = await r2.Content.ReadFromJsonAsync<ProfileDto>();

        p2!.Id.Should().Be(p1!.Id);
    }

    [Fact]
    public async Task PUT_ProfileInfo_UpdatesScalarFields()
    {
        var payload = new
        {
            firstName = "Ada",
            lastName = "Lovelace",
            email = "ada@example.com",
            phone = "+41 79 000 00 00",
            location = "Zurich, Switzerland",
            summary = "Pioneer of computing."
        };

        var response = await _client.PutAsJsonAsync("/api/profile/info", payload);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var profile = await response.Content.ReadFromJsonAsync<ProfileDto>();
        profile!.FirstName.Should().Be("Ada");
        profile.LastName.Should().Be("Lovelace");
        profile.Summary.Should().Be("Pioneer of computing.");
    }

    [Fact]
    public async Task PUT_ProfileInfo_Returns400_WhenRequiredFieldsMissing()
    {
        var payload = new
        {
            firstName = "",
            lastName = "Lovelace",
            email = "ada@example.com",
            phone = "+41 79 000 00 00",
            location = (string?)null,
            summary = (string?)null
        };

        var response = await _client.PutAsJsonAsync("/api/profile/info", payload);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task PUT_ProfileSkills_ReplacesSkillsList()
    {
        var payload = new { skills = new[] { "C#", "Angular", "PostgreSQL" } };

        var response = await _client.PutAsJsonAsync("/api/profile/skills", payload);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var profile = await response.Content.ReadFromJsonAsync<ProfileDto>();
        profile!.Skills.Should().BeEquivalentTo(new[] { "C#", "Angular", "PostgreSQL" });
    }

    [Fact]
    public async Task WorkExperienceCrudFlow()
    {
        // Add
        var addPayload = new
        {
            company = "ACME Corp",
            title = "Software Engineer",
            startDate = "2022-01-01",
            endDate = (string?)null,
            description = "Built things."
        };
        var addResponse = await _client.PostAsJsonAsync("/api/profile/work-experiences", addPayload);
        addResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var added = await addResponse.Content.ReadFromJsonAsync<WorkExperienceDto>();
        added!.Company.Should().Be("ACME Corp");

        // Update
        var updatePayload = new
        {
            company = "ACME Corp",
            title = "Senior Software Engineer",
            startDate = "2022-01-01",
            endDate = "2024-12-31",
            description = "Built more things."
        };
        var updateResponse = await _client.PutAsJsonAsync(
            $"/api/profile/work-experiences/{added.Id}", updatePayload);
        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await updateResponse.Content.ReadFromJsonAsync<WorkExperienceDto>();
        updated!.Title.Should().Be("Senior Software Engineer");

        // Delete
        var deleteResponse = await _client.DeleteAsync($"/api/profile/work-experiences/{added.Id}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify gone
        var profile = await (await _client.GetAsync("/api/profile"))
            .Content.ReadFromJsonAsync<ProfileDto>();
        profile!.WorkExperiences.Should().BeEmpty();
    }

    [Fact]
    public async Task POST_WorkExperience_Returns400_WhenRequiredFieldsMissing()
    {
        var payload = new { description = "No company or title" };
        var response = await _client.PostAsJsonAsync("/api/profile/work-experiences", payload);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task EducationCrudFlow()
    {
        var addPayload = new
        {
            institution = "ETH Zurich",
            degree = "MSc",
            fieldOfStudy = "Computer Science",
            startDate = "2018-09-01",
            endDate = "2020-06-30"
        };
        var addResponse = await _client.PostAsJsonAsync("/api/profile/education", addPayload);
        addResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var added = await addResponse.Content.ReadFromJsonAsync<EducationDto>();
        added!.Institution.Should().Be("ETH Zurich");

        var updatePayload = new { institution = "ETH Zurich", degree = "PhD", fieldOfStudy = "Computer Science", startDate = "2020-09-01", endDate = (string?)null };
        var updateResponse = await _client.PutAsJsonAsync($"/api/profile/education/{added.Id}", updatePayload);
        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await updateResponse.Content.ReadFromJsonAsync<EducationDto>();
        updated!.Degree.Should().Be("PhD");

        var deleteResponse = await _client.DeleteAsync($"/api/profile/education/{added.Id}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }
}
