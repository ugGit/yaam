using FluentValidation.TestHelper;
using Yaam.UseCases.Applications.Commands.Notes;

namespace Yaam.Tests.Unit.Applications;

public class ApplicationNoteCommandValidatorTests
{
    public class AddNoteTests
    {
        private readonly AddApplicationNoteCommandValidator _validator = new();

        [Fact]
        public void Should_Fail_When_Body_Empty()
        {
            var result = _validator.TestValidate(
                new AddApplicationNoteCommand(Guid.NewGuid(), ""));
            result.ShouldHaveValidationErrorFor(x => x.Body);
        }

        [Fact]
        public void Should_Pass_When_Body_Has_Content()
        {
            var result = _validator.TestValidate(
                new AddApplicationNoteCommand(Guid.NewGuid(), "Had a great interview."));
            result.ShouldNotHaveAnyValidationErrors();
        }
    }

    public class UpdateNoteTests
    {
        private readonly UpdateApplicationNoteCommandValidator _validator = new();

        [Fact]
        public void Should_Fail_When_Body_Empty()
        {
            var result = _validator.TestValidate(
                new UpdateApplicationNoteCommand(Guid.NewGuid(), Guid.NewGuid(), ""));
            result.ShouldHaveValidationErrorFor(x => x.Body);
        }

        [Fact]
        public void Should_Pass_When_Body_Has_Content()
        {
            var result = _validator.TestValidate(
                new UpdateApplicationNoteCommand(Guid.NewGuid(), Guid.NewGuid(), "Updated note."));
            result.ShouldNotHaveAnyValidationErrors();
        }
    }
}
