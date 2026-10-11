using MediatR;
using Microsoft.EntityFrameworkCore;
using Yaam.Reminders.Persistence;

namespace Yaam.Reminders.Reminders.Commands;

public record DeleteReminderCommand(Guid ReminderId) : IRequest;

public class DeleteReminderCommandHandler(RemindersDbContext db) : IRequestHandler<DeleteReminderCommand>
{
    public async Task Handle(DeleteReminderCommand command, CancellationToken cancellationToken)
        => await db.Reminders
            .Where(r => r.Id == command.ReminderId)
            .ExecuteDeleteAsync(cancellationToken);
}
