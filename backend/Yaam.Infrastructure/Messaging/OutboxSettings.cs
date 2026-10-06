namespace Yaam.Infrastructure.Messaging;

public class OutboxSettings
{
    public int PollingIntervalMilliseconds { get; init; } = 1000;
    public int BatchSize { get; init; } = 50;
    public bool SimulateCrashAfterPublish { get; init; }
}
