using MediatR;
using Yaam.Domain.Repositories;

namespace Yaam.Application.Applications.Commands;

public record DeleteApplicationCommand(Guid Id) : IRequest;

public class DeleteApplicationCommandHandler(IApplicationRepository repository)
    : IRequestHandler<DeleteApplicationCommand>
{
    public async Task Handle(DeleteApplicationCommand command, CancellationToken cancellationToken)
        => await repository.DeleteAsync(command.Id, cancellationToken);
}
