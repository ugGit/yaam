using FluentValidation.TestHelper;
using Yaam.UseCases.Reminders.Commands;

namespace Yaam.Tests.Unit.Reminders;

public class SetReminderCommandValidatorTests
{
    private readonly SetReminderCommandValidator _validator = new();

    [Fact]
    public void Should_Fail_When_DelayDays_Zero()
    {
        var result = _validator.TestValidate(new SetReminderCommand(Guid.NewGuid(), 0, null));
        result.ShouldHaveValidationErrorFor(x => x.DelayDays);
    }

    [Fact]
    public void Should_Fail_When_DelayDays_Negative()
    {
        var result = _validator.TestValidate(new SetReminderCommand(Guid.NewGuid(), -1, null));
        result.ShouldHaveValidationErrorFor(x => x.DelayDays);
    }

    [Fact]
    public void Should_Pass_With_Valid_Delay()
    {
        var result = _validator.TestValidate(new SetReminderCommand(Guid.NewGuid(), 7, null));
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Should_Fail_When_Note_Exceeds_500_Chars()
    {
        var result = _validator.TestValidate(new SetReminderCommand(Guid.NewGuid(), 7, new string('x', 501)));
        result.ShouldHaveValidationErrorFor(x => x.Note);
    }
}
