using MediatR;
using Yaam.Domain.Entities;
using Yaam.Domain.Enums;
using Yaam.Domain.Errors;
using Yaam.Domain.Repositories;
using Yaam.UseCases.Applications.Dtos;

namespace Yaam.UseCases.Applications.Commands;

public record UpdateApplicationStatusCommand(
    Guid Id,
    ApplicationStatus Status) : IRequest<ApplicationDto>;

public class UpdateApplicationStatusCommandHandler(IApplicationRepository repository)
    : IRequestHandler<UpdateApplicationStatusCommand, ApplicationDto>
{
    public async Task<ApplicationDto> Handle(
        UpdateApplicationStatusCommand command, CancellationToken cancellationToken)
    {
        var application = await repository.GetByIdAsync(command.Id, cancellationToken);
        if (application is null) throw new NotFoundException(nameof(Application), command.Id);

        application.Status = command.Status;
        await repository.UpdateAsync(application, cancellationToken);
        return ApplicationMapper.ToDto(application);
    }
}
