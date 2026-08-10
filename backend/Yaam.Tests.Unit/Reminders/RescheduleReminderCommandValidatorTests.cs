using FluentValidation.TestHelper;
using Yaam.UseCases.Reminders.Commands;

namespace Yaam.Tests.Unit.Reminders;

public class RescheduleReminderCommandValidatorTests
{
    private readonly RescheduleReminderCommandValidator _validator = new();

    [Fact]
    public void Should_Fail_When_NewDueDate_Is_Today()
    {
        var result = _validator.TestValidate(
            new RescheduleReminderCommand(Guid.NewGuid(), DateOnly.FromDateTime(DateTime.UtcNow)));
        result.ShouldHaveValidationErrorFor(x => x.NewDueDate);
    }

    [Fact]
    public void Should_Fail_When_NewDueDate_Is_In_The_Past()
    {
        var result = _validator.TestValidate(
            new RescheduleReminderCommand(Guid.NewGuid(), DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1))));
        result.ShouldHaveValidationErrorFor(x => x.NewDueDate);
    }

    [Fact]
    public void Should_Pass_When_NewDueDate_Is_Tomorrow()
    {
        var result = _validator.TestValidate(
            new RescheduleReminderCommand(Guid.NewGuid(), DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1))));
        result.ShouldNotHaveAnyValidationErrors();
    }
}
