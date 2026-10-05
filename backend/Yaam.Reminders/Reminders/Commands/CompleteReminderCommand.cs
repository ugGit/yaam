using MediatR;
using Yaam.Reminders.Common;
using Yaam.Reminders.Domain;
using Yaam.Reminders.Persistence;

namespace Yaam.Reminders.Reminders.Commands;

public record CompleteReminderCommand(Guid ReminderId) : IRequest;

public class CompleteReminderCommandHandler(RemindersDbContext db, TimeProvider timeProvider)
    : IRequestHandler<CompleteReminderCommand>
{
    public async Task Handle(CompleteReminderCommand command, CancellationToken cancellationToken)
    {
        var reminder = await db.Reminders.FindAsync([command.ReminderId], cancellationToken)
            ?? throw new NotFoundException(nameof(Reminder), command.ReminderId);

        reminder.CompletedAt = timeProvider.GetUtcNow().UtcDateTime;
        await db.SaveChangesAsync(cancellationToken);
    }
}
