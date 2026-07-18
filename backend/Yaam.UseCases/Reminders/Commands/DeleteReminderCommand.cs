using MediatR;
using Yaam.Domain.Repositories;

namespace Yaam.UseCases.Reminders.Commands;

public record DeleteReminderCommand(Guid ApplicationId) : IRequest;

public class DeleteReminderCommandHandler(IReminderRepository repository)
    : IRequestHandler<DeleteReminderCommand>
{
    public async Task Handle(DeleteReminderCommand command, CancellationToken cancellationToken)
        => await repository.DeleteByApplicationIdAsync(command.ApplicationId, cancellationToken);
}
