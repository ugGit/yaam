using MediatR;
using Yaam.Application.Applications.Dtos;
using Yaam.Domain.Enums;
using Yaam.Domain.Repositories;
using DomainApp = Yaam.Domain.Entities.Application;

namespace Yaam.Application.Applications.Queries;

public record GetApplicationsQuery(
    ApplicationStatus? Status,
    string Sort = "dateApplied",
    string Order = "desc") : IRequest<List<ApplicationSummaryDto>>;

public class GetApplicationsQueryHandler(IApplicationRepository repository)
    : IRequestHandler<GetApplicationsQuery, List<ApplicationSummaryDto>>
{
    public async Task<List<ApplicationSummaryDto>> Handle(
        GetApplicationsQuery request, CancellationToken cancellationToken)
    {
        var applications = await repository.GetAllAsync(
            request.Status, request.Sort, request.Order, cancellationToken);
        return applications.Select(ToDto).ToList();
    }

    private static ApplicationSummaryDto ToDto(DomainApp a) =>
        new(a.Id, a.CompanyName, a.Role, a.DateApplied, a.Status);
}
