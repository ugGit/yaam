using System.Diagnostics.Metrics;
using Yaam.ServiceDefaults.Telemetry;

namespace Yaam.Infrastructure.Messaging;

// The two numbers that tell you an outbox is healthy: how much is waiting, and for how long.
public sealed class OutboxMetrics
{
    private long _pending;
    private double _oldestPendingAgeSeconds;

    public OutboxMetrics(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create(MessagingTelemetry.SourceName);
        meter.CreateObservableGauge("yaam.outbox.pending", () => Interlocked.Read(ref _pending), unit: "{message}");
        meter.CreateObservableGauge("yaam.outbox.oldest_pending_age", () => Volatile.Read(ref _oldestPendingAgeSeconds), unit: "s");
    }

    public void Update(long pending, double oldestPendingAgeSeconds)
    {
        Interlocked.Exchange(ref _pending, pending);
        Volatile.Write(ref _oldestPendingAgeSeconds, oldestPendingAgeSeconds);
    }
}
