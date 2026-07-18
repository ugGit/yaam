using FluentValidation;
using MediatR;
using Yaam.Domain.Entities;
using Yaam.Domain.Errors;
using Yaam.Domain.Repositories;
using Yaam.UseCases.Reminders.Dtos;

namespace Yaam.UseCases.Reminders.Commands;

public record SetReminderCommand(
    Guid ApplicationId,
    int DelayDays,
    string? Note) : IRequest<ApplicationReminderDto>;

public class SetReminderCommandValidator : AbstractValidator<SetReminderCommand>
{
    public SetReminderCommandValidator()
    {
        RuleFor(x => x.DelayDays).GreaterThan(0).WithMessage("Delay must be at least 1 day.");
        RuleFor(x => x.Note).MaximumLength(500).When(x => x.Note is not null);
    }
}

public class SetReminderCommandHandler(IReminderRepository reminderRepository, IApplicationRepository applicationRepository)
    : IRequestHandler<SetReminderCommand, ApplicationReminderDto>
{
    public async Task<ApplicationReminderDto> Handle(
        SetReminderCommand command, CancellationToken cancellationToken)
    {
        var application = await applicationRepository.GetByIdAsync(command.ApplicationId, cancellationToken)
            ?? throw new NotFoundException(nameof(Application), command.ApplicationId);

        var existing = await reminderRepository.GetByApplicationIdAsync(command.ApplicationId, cancellationToken);
        if (existing is not null)
            throw new ConflictException("An active reminder already exists. Complete or delete it before adding a new one.");

        var dueDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(command.DelayDays);
        var reminder = new Reminder
        {
            ApplicationId = command.ApplicationId,
            DelayDays = command.DelayDays,
            DueDate = dueDate,
            Note = command.Note,
        };

        await reminderRepository.AddAsync(reminder, cancellationToken);
        return ReminderMapper.ToApplicationReminderDto(reminder);
    }
}
