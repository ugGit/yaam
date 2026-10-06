namespace Yaam.Reminders.ApplicationEvents;

public enum ConsumeOutcome
{
    Processed,
    Duplicate,
    Stale,
    Ignored,
    Failed,
}
