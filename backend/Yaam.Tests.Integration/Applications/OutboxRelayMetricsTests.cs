using System.Diagnostics.Metrics;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Yaam.Infrastructure.Messaging;
using Yaam.ServiceDefaults.Messaging;
using Yaam.ServiceDefaults.Telemetry;

namespace Yaam.Tests.Integration.Applications;

[Collection("Integration")]
public class OutboxRelayMetricsTests(ApiFactory factory)
{
    [Fact]
    public async Task PendingGauge_ReportsBacklog_WhileBrokerIsUnreachable()
    {
        var client = factory.CreateAuthenticatedClient();
        (await client.PostAsJsonAsync("/api/applications",
            new { companyName = "Backlog Co", role = "Dev", status = "Draft" })).EnsureSuccessStatusCode();

        long observedPending = 0;
        using var listener = new MeterListener();
        listener.InstrumentPublished = (instrument, meterListener) =>
        {
            if (instrument.Meter.Name == MessagingTelemetry.SourceName && instrument.Name == "yaam.outbox.pending")
                meterListener.EnableMeasurementEvents(instrument);
        };
        listener.SetMeasurementEventCallback<long>((_, value, _, _) => observedPending = value);
        listener.Start();

        await using var unreachableBroker = new RabbitMqConnectionProvider(
            Options.Create(new RabbitMqSettings { HostName = "localhost", Port = 1 }));
        var relay = new OutboxRelay(
            factory.Services.GetRequiredService<IServiceScopeFactory>(),
            unreachableBroker,
            Options.Create(new OutboxSettings { PollingIntervalMilliseconds = 50 }),
            new OutboxMetrics(factory.Services.GetRequiredService<IMeterFactory>()),
            NullLogger<OutboxRelay>.Instance);

        await relay.StartAsync(CancellationToken.None);
        try
        {
            var deadline = DateTime.UtcNow.AddSeconds(5);
            while (observedPending < 1 && DateTime.UtcNow < deadline)
            {
                listener.RecordObservableInstruments();
                await Task.Delay(100);
            }
        }
        finally
        {
            await relay.StopAsync(CancellationToken.None);
        }

        observedPending.Should().BeGreaterThanOrEqualTo(1);
    }
}
