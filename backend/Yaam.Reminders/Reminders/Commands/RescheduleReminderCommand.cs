using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Yaam.Reminders.Common;
using Yaam.Reminders.Domain;
using Yaam.Reminders.Persistence;
using Yaam.Reminders.Reminders.Dtos;

namespace Yaam.Reminders.Reminders.Commands;

public record RescheduleReminderCommand(
    Guid ReminderId,
    DateOnly NewDueDate) : IRequest<ReminderDto>;

public class RescheduleReminderCommandValidator : AbstractValidator<RescheduleReminderCommand>
{
    public RescheduleReminderCommandValidator()
    {
        RuleFor(x => x.NewDueDate)
            .GreaterThan(DateOnly.FromDateTime(DateTime.UtcNow))
            .WithMessage("New due date must be in the future.");
    }
}

public class RescheduleReminderCommandHandler(RemindersDbContext db)
    : IRequestHandler<RescheduleReminderCommand, ReminderDto>
{
    public async Task<ReminderDto> Handle(RescheduleReminderCommand command, CancellationToken cancellationToken)
    {
        var reminder = await db.Reminders
            .Include(r => r.Application)
            .FirstOrDefaultAsync(r => r.Id == command.ReminderId, cancellationToken)
            ?? throw new NotFoundException(nameof(Reminder), command.ReminderId);

        reminder.DueDate = command.NewDueDate;
        reminder.NotifiedAt = null;
        await db.SaveChangesAsync(cancellationToken);
        return ReminderMapper.ToDto(reminder);
    }
}
