using FluentAssertions;
using FluentValidation.TestHelper;
using Yaam.Application.Applications.Commands;
using Yaam.Domain.Enums;

namespace Yaam.Tests.Unit.Applications;

public class UpdateApplicationCommandValidatorTests
{
    private readonly UpdateApplicationCommandValidator _validator = new();

    [Fact]
    public void Should_Fail_When_CompanyName_Empty()
    {
        var result = _validator.TestValidate(
            new UpdateApplicationCommand(Guid.NewGuid(), "", "Dev", null,
                ApplicationStatus.Draft, null, null, null, null));
        result.ShouldHaveValidationErrorFor(x => x.CompanyName);
    }

    [Fact]
    public void Should_Fail_When_DateApplied_Null_And_Status_Not_Draft()
    {
        var result = _validator.TestValidate(
            new UpdateApplicationCommand(Guid.NewGuid(), "Acme", "Dev", null,
                ApplicationStatus.Interviewed, null, null, null, null));
        result.ShouldHaveValidationErrorFor(x => x.DateApplied);
    }
}
