namespace Yaam.Infrastructure.Auth;

public record SupabaseSettings
{
    public required string Url { get; init; }
    public required string JwtSecret { get; init; }
}
