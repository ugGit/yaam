using System.Diagnostics;
using System.Diagnostics.Metrics;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Npgsql;
using OpenTelemetry;
using OpenTelemetry.Trace;
using Yaam.Infrastructure.Messaging;
using Yaam.ServiceDefaults.Messaging;

namespace Yaam.Tests.Integration.Applications;

[Collection("Integration")]
public class OutboxRelayTracingTests(ApiFactory factory)
{
    [Fact]
    public async Task RelayPolling_ProducesNoDatabaseSpans()
    {
        var exported = new List<Activity>();
        using var tracerProvider = Sdk.CreateTracerProviderBuilder()
            .AddNpgsql()
            .AddInMemoryExporter(exported)
            .Build();

        await using var unreachableBroker = new RabbitMqConnectionProvider(
            Options.Create(new RabbitMqSettings { HostName = "localhost", Port = 1 }));
        var relay = new OutboxRelay(
            factory.Services.GetRequiredService<IServiceScopeFactory>(),
            unreachableBroker,
            Options.Create(new OutboxSettings { PollingIntervalMilliseconds = 50 }),
            new OutboxMetrics(factory.Services.GetRequiredService<IMeterFactory>()),
            NullLogger<OutboxRelay>.Instance);

        await relay.StartAsync(CancellationToken.None);
        await Task.Delay(500);
        await relay.StopAsync(CancellationToken.None);
        tracerProvider.ForceFlush();

        exported.Select(activity => activity.DisplayName).Should().BeEmpty();
    }
}
