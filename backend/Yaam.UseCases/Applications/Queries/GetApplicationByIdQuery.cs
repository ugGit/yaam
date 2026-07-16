using MediatR;
using Yaam.UseCases.Applications.Dtos;
using Yaam.Domain.Entities;
using Yaam.Domain.Errors;
using Yaam.Domain.Repositories;

namespace Yaam.UseCases.Applications.Queries;

public record GetApplicationByIdQuery(Guid Id) : IRequest<ApplicationDto>;

public class GetApplicationByIdQueryHandler(IApplicationRepository repository)
    : IRequestHandler<GetApplicationByIdQuery, ApplicationDto>
{
    public async Task<ApplicationDto> Handle(
        GetApplicationByIdQuery query, CancellationToken cancellationToken)
    {
        var application = await repository.GetByIdAsync(query.Id, cancellationToken);
        if (application is null) throw new NotFoundException(nameof(Application), query.Id);
        return ApplicationMapper.ToDto(application);
    }
}
