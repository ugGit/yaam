using FluentAssertions;
using FluentValidation;
using MediatR;
using NSubstitute;
using Yaam.Application.Common.Behaviors;

namespace Yaam.Tests.Unit.Common;

public class ValidationBehaviorTests
{
    private record TestCommand(string Value) : IRequest<string>;

    private class TestCommandValidator : AbstractValidator<TestCommand>
    {
        public TestCommandValidator()
        {
            RuleFor(x => x.Value).NotEmpty().WithMessage("Value is required.");
        }
    }

    [Fact]
    public async Task Handle_WhenValidationPasses_CallsNext()
    {
        var validators = new[] { new TestCommandValidator() };
        var behavior = new ValidationBehavior<TestCommand, string>(validators);
        var next = Substitute.For<RequestHandlerDelegate<string>>();
        next().Returns("ok");

        var result = await behavior.Handle(new TestCommand("hello"), next, CancellationToken.None);

        result.Should().Be("ok");
        await next.Received(1)();
    }

    [Fact]
    public async Task Handle_WhenValidationFails_ThrowsValidationException()
    {
        var validators = new[] { new TestCommandValidator() };
        var behavior = new ValidationBehavior<TestCommand, string>(validators);
        var next = Substitute.For<RequestHandlerDelegate<string>>();

        var act = () => behavior.Handle(new TestCommand(""), next, CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>()
            .Where(e => e.Errors.Any(f => f.PropertyName == "Value"));
        await next.DidNotReceive()();
    }
}
