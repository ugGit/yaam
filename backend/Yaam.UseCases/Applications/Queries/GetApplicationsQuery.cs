using FluentValidation;
using MediatR;
using Yaam.Domain.Common;
using Yaam.Domain.Enums;
using Yaam.Domain.Repositories;
using Yaam.UseCases.Applications.Dtos;

namespace Yaam.UseCases.Applications.Queries;

public record GetApplicationsQuery(
    ApplicationStatus? Status,
    ApplicationSortField Sort = ApplicationSortField.DateApplied,
    string Order = "desc") : IRequest<List<ApplicationSummaryDto>>;

public class GetApplicationsQueryValidator : AbstractValidator<GetApplicationsQuery>
{
    public GetApplicationsQueryValidator()
    {
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
        return applications.Select(ApplicationMapper.ToSummaryDto).ToList();
    }
}
