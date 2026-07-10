using FluentValidation.TestHelper;
using Yaam.Application.Applications.Commands.Notes;

namespace Yaam.Tests.Unit.Applications;

public class ApplicationNoteCommandValidatorTests
{
    [Fact]
    public void AddNote_Should_Fail_When_Body_Empty()
    {
        var validator = new AddApplicationNoteCommandValidator();
        var result = validator.TestValidate(
            new AddApplicationNoteCommand(Guid.NewGuid(), ""));
        result.ShouldHaveValidationErrorFor(x => x.Body);
    }

    [Fact]
    public void AddNote_Should_Pass_With_Valid_Body()
    {
        var validator = new AddApplicationNoteCommandValidator();
        var result = validator.TestValidate(
            new AddApplicationNoteCommand(Guid.NewGuid(), "Had a call with recruiter."));
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void UpdateNote_Should_Fail_When_Body_Empty()
    {
        var validator = new UpdateApplicationNoteCommandValidator();
        var result = validator.TestValidate(
            new UpdateApplicationNoteCommand(Guid.NewGuid(), Guid.NewGuid(), ""));
        result.ShouldHaveValidationErrorFor(x => x.Body);
    }
}
