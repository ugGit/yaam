using MediatR;
using Yaam.Application.Applications.Dtos;
using Yaam.Domain.Enums;
using Yaam.Domain.Repositories;

namespace Yaam.Application.Applications.Commands;

public record UpdateApplicationStatusCommand(
    Guid Id,
    ApplicationStatus Status) : IRequest<ApplicationDto?>;

public class UpdateApplicationStatusCommandHandler(IApplicationRepository repository)
    : IRequestHandler<UpdateApplicationStatusCommand, ApplicationDto?>
{
    public async Task<ApplicationDto?> Handle(
        UpdateApplicationStatusCommand request, CancellationToken cancellationToken)
    {
        var application = await repository.GetByIdAsync(request.Id, cancellationToken);
        if (application is null) return null;

        application.Status = request.Status;
        await repository.UpdateAsync(application, cancellationToken);

        return new ApplicationDto(
            application.Id, application.CompanyName, application.Role,
            application.DateApplied, application.Status,
            application.ContactName, application.ContactEmail, application.ContactPhone,
            application.JobPosting, application.CreatedAt, application.UpdatedAt,
            application.Notes.OrderByDescending(n => n.CreatedAt)
                .Select(n => new ApplicationNoteDto(n.Id, n.Body, n.CreatedAt, n.UpdatedAt))
                .ToList());
    }
}
