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
}
