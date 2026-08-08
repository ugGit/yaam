using FluentValidation;
using MediatR;
using Yaam.Domain.Entities;
using Yaam.Domain.Errors;
using Yaam.Domain.Repositories;
using Yaam.UseCases.Reminders.Dtos;

namespace Yaam.UseCases.Reminders.Commands;

public record RescheduleReminderCommand(
    Guid ReminderId,
    DateOnly NewDueDate) : IRequest<ApplicationReminderDto>;

public class RescheduleReminderCommandValidator : AbstractValidator<RescheduleReminderCommand>
{
    public RescheduleReminderCommandValidator()
    {
        RuleFor(x => x.NewDueDate)
            .GreaterThan(DateOnly.FromDateTime(DateTime.UtcNow))
            .WithMessage("New due date must be in the future.");
    }
}

public class RescheduleReminderCommandHandler(IReminderRepository repository)
    : IRequestHandler<RescheduleReminderCommand, ApplicationReminderDto>
{
    public async Task<ApplicationReminderDto> Handle(
        RescheduleReminderCommand command, CancellationToken cancellationToken)
    {
        var reminder = await repository.GetByIdAsync(command.ReminderId, cancellationToken)
            ?? throw new NotFoundException(nameof(Reminder), command.ReminderId);

        reminder.DueDate = command.NewDueDate;
        reminder.NotifiedAt = null;
        await repository.UpdateAsync(reminder, cancellationToken);
        return ReminderMapper.ToApplicationReminderDto(reminder);
    }
}
