using System.Diagnostics;
using System.Text;

namespace Yaam.ServiceDefaults.Telemetry;

public static class MessagingTelemetry
{
    public const string SourceName = "Yaam.Messaging";
    public const string TraceParentHeader = "traceparent";

    private static readonly ActivitySource Source = new(SourceName);

    // The relay runs outside any request, so the parent is the trace context stored on the outbox row.
    public static Activity? StartPublishActivity(string? traceParent, string messageType)
    {
        ActivityContext.TryParse(traceParent, null, isRemote: true, out var parent);
        var activity = Source.StartActivity($"publish {messageType}", ActivityKind.Producer, parent);
        activity?.SetTag("messaging.system", "rabbitmq");
        activity?.SetTag("messaging.message.type", messageType);
        return activity;
    }

    public static Activity? StartConsumeActivity(IDictionary<string, object?>? headers, string? messageType)
    {
        string? traceParent = null;
        if (headers is not null && headers.TryGetValue(TraceParentHeader, out var value) && value is byte[] bytes)
            traceParent = Encoding.UTF8.GetString(bytes);

        ActivityContext.TryParse(traceParent, null, isRemote: true, out var parent);
        var activity = Source.StartActivity($"process {messageType}", ActivityKind.Consumer, parent);
        activity?.SetTag("messaging.system", "rabbitmq");
        activity?.SetTag("messaging.message.type", messageType);
        return activity;
    }
}
