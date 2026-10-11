using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Yaam.API.Applications;
using Yaam.Contracts;
using Yaam.Contracts.Applications;
using Yaam.Domain.Errors;
using Yaam.Infrastructure.Persistence;
using Yaam.Infrastructure.Persistence.Outbox;

namespace Yaam.Tests.Integration.Applications;

[Collection("Integration")]
public class ApplicationOutboxTests(ApiFactory factory)
{
    private readonly HttpClient _client = factory.CreateAuthenticatedClient();

    [Fact]
    public async Task Create_WritesUpsertedMessage_WithVersion1()
    {
        var application = await CreateApplicationAsync("Outbox Co");

        var message = (await GetOutboxMessagesAsync(application.Id)).Should().ContainSingle().Subject;
        message.Type.Should().Be(nameof(ApplicationUpserted));
        message.ProcessedAt.Should().BeNull();
        var payload = JsonSerializer.Deserialize<ApplicationUpserted>(message.Payload, ContractSerializer.Options)!;
        payload.Should().BeEquivalentTo(new ApplicationUpserted(application.Id, 1, "Outbox Co", "Dev", null, null));
    }

    [Fact]
    public async Task Update_WritesUpsertedMessage_WithIncrementedVersion()
    {
        var application = await CreateApplicationAsync("Before Co");

        var response = await _client.PutAsJsonAsync($"/api/applications/{application.Id}",
            new { companyName = "After Co", role = "Dev", status = "Draft" });
        response.EnsureSuccessStatusCode();

        var messages = await GetOutboxMessagesAsync(application.Id);
        messages.Should().HaveCount(2);
        var payload = JsonSerializer.Deserialize<ApplicationUpserted>(messages[1].Payload, ContractSerializer.Options)!;
        payload.Version.Should().Be(2);
        payload.CompanyName.Should().Be("After Co");
    }

    [Fact]
    public async Task Delete_WritesDeletedMessage_WithNextVersion()
    {
        var application = await CreateApplicationAsync("Doomed Co");

        (await _client.DeleteAsync($"/api/applications/{application.Id}")).EnsureSuccessStatusCode();

        var last = (await GetOutboxMessagesAsync(application.Id)).Last();
        last.Type.Should().Be(nameof(ApplicationDeleted));
        JsonSerializer.Deserialize<ApplicationDeleted>(last.Payload, ContractSerializer.Options)!
            .Should().Be(new ApplicationDeleted(application.Id, 2));
    }

    [Fact]
    public async Task ConcurrentUpdate_Returns409Conflict_AndWritesNoOutboxMessage()
    {
        var application = await CreateApplicationAsync("Race Co");
        using var firstScope = factory.Services.CreateScope();
        using var secondScope = factory.Services.CreateScope();
        var firstDb = firstScope.ServiceProvider.GetRequiredService<AppDbContext>();
        var secondDb = secondScope.ServiceProvider.GetRequiredService<AppDbContext>();
        var firstCopy = await firstDb.Applications.SingleAsync(a => a.Id == application.Id);
        var secondCopy = await secondDb.Applications.SingleAsync(a => a.Id == application.Id);

        firstCopy.Role = "First writer";
        await firstDb.SaveChangesAsync();
        secondCopy.Role = "Second writer";
        var secondSave = () => secondDb.SaveChangesAsync();

        await secondSave.Should().ThrowAsync<ConflictException>();
        (await GetOutboxMessagesAsync(application.Id)).Should().HaveCount(2);
    }

    private async Task<ApplicationViewModel> CreateApplicationAsync(string companyName)
    {
        var response = await _client.PostAsJsonAsync("/api/applications",
            new { companyName, role = "Dev", status = "Draft" });
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return (await response.Content.ReadFromJsonAsync<ApplicationViewModel>())!;
    }

    private async Task<List<OutboxMessage>> GetOutboxMessagesAsync(Guid applicationId)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await db.OutboxMessages.AsNoTracking()
            .Where(m => m.AggregateId == applicationId)
            .OrderBy(m => m.Id)
            .ToListAsync();
    }
}
