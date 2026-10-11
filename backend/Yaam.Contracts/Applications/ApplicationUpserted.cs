namespace Yaam.Contracts.Applications;

// Additive changes only. Renaming or removing a field breaks every consumer still on the old shape.
public sealed record ApplicationUpserted(
    Guid ApplicationId,
    long Version,
    string CompanyName,
    string Role,
    DateOnly? DateApplied,
    string? ContactName);
