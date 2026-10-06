namespace Yaam.Contracts.Applications;

public static class ApplicationTopology
{
    public const string Exchange = "yaam.applications";

    public static string RoutingKeyFor(string type) => type switch
    {
        nameof(ApplicationUpserted) => "application.upserted",
        nameof(ApplicationDeleted) => "application.deleted",
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Unknown application event type."),
    };
}
