namespace Yaam.Domain.Common;

public static class ApplicationSortFields
{
    public const string DateApplied = "dateApplied";
    public const string CompanyName = "companyName";
    public static readonly IReadOnlySet<string> All =
        new HashSet<string>([DateApplied, CompanyName]);
}
