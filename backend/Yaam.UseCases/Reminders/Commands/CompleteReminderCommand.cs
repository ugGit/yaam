using MediatR;
using Yaam.Domain.Entities;
using Yaam.Domain.Errors;
using Yaam.Domain.Repositories;

namespace Yaam.UseCases.Reminders.Commands;

public record CompleteReminderCommand(Guid ApplicationId) : IRequest;

public class CompleteReminderCommandHandler(IReminderRepository repository)
    : IRequestHandler<CompleteReminderCommand>
{
    public async Task Handle(CompleteReminderCommand command, CancellationToken cancellationToken)
    {
        var reminder = await repository.GetByApplicationIdAsync(command.ApplicationId, cancellationToken)
            ?? throw new NotFoundException(nameof(Reminder), command.ApplicationId);

        reminder.CompletedAt = DateTime.UtcNow;
        await repository.UpdateAsync(reminder, cancellationToken);
    }
}
