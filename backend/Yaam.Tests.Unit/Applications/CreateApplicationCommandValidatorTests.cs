using FluentAssertions;
using FluentValidation.TestHelper;
using Yaam.Application.Applications.Commands;
using Yaam.Domain.Enums;

namespace Yaam.Tests.Unit.Applications;

public class CreateApplicationCommandValidatorTests
{
    private readonly CreateApplicationCommandValidator _validator = new();

    [Fact]
    public void Should_Fail_When_CompanyName_Empty()
    {
        var result = _validator.TestValidate(
            new CreateApplicationCommand("", "Dev", null, ApplicationStatus.Draft,
                null, null, null, null));
        result.ShouldHaveValidationErrorFor(x => x.CompanyName);
    }

    [Fact]
    public void Should_Fail_When_Role_Empty()
    {
        var result = _validator.TestValidate(
            new CreateApplicationCommand("Acme", "", null, ApplicationStatus.Draft,
                null, null, null, null));
        result.ShouldHaveValidationErrorFor(x => x.Role);
    }

    [Fact]
    public void Should_Fail_When_DateApplied_Null_And_Status_Not_Draft()
    {
        var result = _validator.TestValidate(
            new CreateApplicationCommand("Acme", "Dev", null, ApplicationStatus.Applied,
                null, null, null, null));
        result.ShouldHaveValidationErrorFor(x => x.DateApplied);
    }

    [Fact]
    public void Should_Pass_When_DateApplied_Null_And_Status_Draft()
    {
        var result = _validator.TestValidate(
            new CreateApplicationCommand("Acme", "Dev", null, ApplicationStatus.Draft,
                null, null, null, null));
        result.ShouldNotHaveValidationErrorFor(x => x.DateApplied);
    }

    [Fact]
    public void Should_Pass_With_All_Required_Fields_For_Applied_Status()
    {
        var result = _validator.TestValidate(
            new CreateApplicationCommand("Acme", "Dev", DateOnly.FromDateTime(DateTime.Today),
                ApplicationStatus.Applied, null, null, null, null));
        result.ShouldNotHaveAnyValidationErrors();
    }
}
