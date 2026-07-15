using FluentValidation;
using MediatR;
using Yaam.Application.Applications.Dtos;
using Yaam.Domain.Enums;
using Yaam.Domain.Repositories;

namespace Yaam.Application.Applications.Queries;

public record GetApplicationsQuery(
    ApplicationStatus? Status,
    string Sort = "dateApplied",
    string Order = "desc") : IRequest<List<ApplicationSummaryDto>>;

public static class ApplicationSortFields
{
    public const string DateApplied = "dateApplied";
    public const string CompanyName = "companyName";
    public static readonly IReadOnlySet<string> All = new HashSet<string>
        { DateApplied, CompanyName };
}

public class GetApplicationsQueryValidator : AbstractValidator<GetApplicationsQuery>
{
    public GetApplicationsQueryValidator()
    {
        RuleFor(x => x.Sort)
            .Must(ApplicationSortFields.All.Contains)
            .WithMessage($"Sort must be one of: {string.Join(", ", ApplicationSortFields.All)}.");
        RuleFor(x => x.Order)
            .Must(o => o == "asc" || o == "desc")
            .WithMessage("Order must be 'asc' or 'desc'.");
    }
}

public class GetApplicationsQueryHandler(IApplicationRepository repository)
    : IRequestHandler<GetApplicationsQuery, List<ApplicationSummaryDto>>
{
    public async Task<List<ApplicationSummaryDto>> Handle(
        GetApplicationsQuery query, CancellationToken cancellationToken)
    {
        var applications = await repository.GetAllAsync(
            query.Status, query.Sort, query.Order, cancellationToken);
        return applications.Select(ToDto).ToList();
    }

    private static ApplicationSummaryDto ToDto(global::Yaam.Domain.Entities.Application a) =>
        new(a.Id, a.CompanyName, a.Role, a.DateApplied, a.Status);
}
