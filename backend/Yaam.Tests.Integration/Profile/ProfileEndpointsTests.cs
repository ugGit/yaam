using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Yaam.UseCases.Profile.Dtos;

namespace Yaam.Tests.Integration.Profile;

public class ProfileEndpointsTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task GET_Profile_ReturnsEmptyProfile_WhenNoneExists()
    {
        var response = await _client.GetAsync("/api/profile");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var profile = await response.Content.ReadFromJsonAsync<ProfileDto>();
        profile.Should().NotBeNull();
        profile!.FirstName.Should().BeNull();
        profile.Skills.Should().BeEmpty();
        profile.WorkExperiences.Should().BeEmpty();
        profile.CustomFields.Should().BeEmpty();
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
}
