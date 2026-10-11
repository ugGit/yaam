using System.Diagnostics.Metrics;
using Yaam.ServiceDefaults.Telemetry;

namespace Yaam.Reminders.ApplicationEvents;

public sealed class ConsumerMetrics
{
    private readonly Counter<long> _messages;

    public ConsumerMetrics(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create(MessagingTelemetry.SourceName);
        _messages = meter.CreateCounter<long>("yaam.consumer.messages", unit: "{message}");
    }

    public void Record(ConsumeOutcome outcome)
        => _messages.Add(1, new KeyValuePair<string, object?>("outcome", outcome.ToString().ToLowerInvariant()));
}
