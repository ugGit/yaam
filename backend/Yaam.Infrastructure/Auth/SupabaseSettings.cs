namespace Yaam.Infrastructure.Auth;

public record SupabaseSettings
{
    public required string Url { get; init; }
}
