using MediatR;
using Yaam.Application.Applications.Dtos;
using Yaam.Domain.Entities;
using Yaam.Domain.Errors;
using Yaam.Domain.Repositories;

namespace Yaam.Application.Applications.Queries;

public record GetApplicationByIdQuery(Guid Id) : IRequest<ApplicationDto>;

public class GetApplicationByIdQueryHandler(IApplicationRepository repository)
    : IRequestHandler<GetApplicationByIdQuery, ApplicationDto>
{
    public async Task<ApplicationDto> Handle(
        GetApplicationByIdQuery query, CancellationToken cancellationToken)
    {
        var application = await repository.GetByIdAsync(query.Id, cancellationToken);
        if (application is null) throw new NotFoundException(nameof(Application), query.Id);
        return ToDto(application);
    }

    private static ApplicationDto ToDto(global::Yaam.Domain.Entities.Application a) => new(
        a.Id, a.CompanyName, a.Role, a.DateApplied, a.Status,
        a.ContactName, a.ContactEmail, a.ContactPhone, a.JobPosting,
        a.CreatedAt, a.UpdatedAt,
        a.Notes.OrderByDescending(n => n.CreatedAt).Select(ToNoteDto).ToList());

    private static ApplicationNoteDto ToNoteDto(ApplicationNote n) =>
        new(n.Id, n.Body, n.CreatedAt, n.UpdatedAt);
}
