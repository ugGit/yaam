using FluentValidation;
using MediatR;
using Yaam.Domain.Entities;
using Yaam.Domain.Repositories;
using Yaam.UseCases.Profile.Dtos;

namespace Yaam.UseCases.Profile.Commands.CustomFields;

public record AddCustomFieldCommand(string Label, string Value) : IRequest<CustomFieldDto>;

public class AddCustomFieldCommandValidator : AbstractValidator<AddCustomFieldCommand>
{
    public AddCustomFieldCommandValidator()
    {
        RuleFor(x => x.Label).NotEmpty().MaximumLength(250);
        RuleFor(x => x.Value).NotEmpty().MaximumLength(1000);
    }
}

public class AddCustomFieldCommandHandler(IProfileRepository repository)
    : IRequestHandler<AddCustomFieldCommand, CustomFieldDto>
{
    public async Task<CustomFieldDto> Handle(AddCustomFieldCommand command, CancellationToken cancellationToken)
    {
        var profile = await repository.GetOrCreateAsync(cancellationToken);
        var entry = new CustomField
        {
            ProfileId = profile.Id,
            Label = command.Label,
            Value = command.Value,
        };
        profile.CustomFields.Add(entry);
        await repository.UpdateAsync(cancellationToken);
        return ProfileMapper.ToDto(entry);
    }
}
