using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Yaam.Reminders.Common;
using Yaam.Reminders.Domain;
using Yaam.Reminders.Persistence;
using Yaam.Reminders.Reminders.Dtos;

namespace Yaam.Reminders.Reminders.Commands;

public record CreateReminderCommand(
    Guid ApplicationId,
    int DelayDays,
    string? Note) : IRequest<ReminderDto>;

public class CreateReminderCommandValidator : AbstractValidator<CreateReminderCommand>
{
    public CreateReminderCommandValidator()
    {
        RuleFor(x => x.DelayDays).GreaterThan(0).WithMessage("Delay must be at least 1 day.");
        RuleFor(x => x.Note).MaximumLength(500).When(x => x.Note is not null);
    }
}

public class CreateReminderCommandHandler(RemindersDbContext db, TimeProvider timeProvider)
    : IRequestHandler<CreateReminderCommand, ReminderDto>
{
    public async Task<ReminderDto> Handle(CreateReminderCommand command, CancellationToken cancellationToken)
    {
        // Existence is checked against the local snapshot, not the monolith: no runtime dependency, but eventually consistent.
        var snapshot = await db.ApplicationSnapshots
            .FirstOrDefaultAsync(s => s.ApplicationId == command.ApplicationId && !s.IsDeleted, cancellationToken)
            ?? throw new NotFoundException("Application", command.ApplicationId);

        var today = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
        var reminder = new Reminder
        {
            ApplicationId = command.ApplicationId,
            Application = snapshot,
            DelayDays = command.DelayDays,
            DueDate = today.AddDays(command.DelayDays),
            Note = command.Note,
        };

        db.Reminders.Add(reminder);
        await db.SaveChangesAsync(cancellationToken);
        return ReminderMapper.ToDto(reminder);
    }
}
