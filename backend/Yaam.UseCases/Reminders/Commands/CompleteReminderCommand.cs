using MediatR;
using Yaam.Domain.Entities;
using Yaam.Domain.Errors;
using Yaam.Domain.Repositories;

namespace Yaam.UseCases.Reminders.Commands;

public record CompleteReminderCommand(Guid ReminderId) : IRequest;

public class CompleteReminderCommandHandler(IReminderRepository repository)
    : IRequestHandler<CompleteReminderCommand>
{
    public async Task Handle(CompleteReminderCommand command, CancellationToken cancellationToken)
    {
        var reminder = await repository.GetByIdAsync(command.ReminderId, cancellationToken)
            ?? throw new NotFoundException(nameof(Reminder), command.ReminderId);

        reminder.CompletedAt = DateTime.UtcNow;
        await repository.UpdateAsync(reminder, cancellationToken);
    }
}
