using FluentValidation;
using MediatR;
using Yaam.Domain.Entities;
using Yaam.Domain.Errors;
using Yaam.Domain.Repositories;
using Yaam.UseCases.Profile.Dtos;

namespace Yaam.UseCases.Profile.Commands.CustomFields;

public record UpdateCustomFieldCommand(Guid Id, string Label, string Value) : IRequest<CustomFieldDto>;

public class UpdateCustomFieldCommandValidator : AbstractValidator<UpdateCustomFieldCommand>
{
    public UpdateCustomFieldCommandValidator()
    {
        RuleFor(x => x.Label).NotEmpty().MaximumLength(250);
        RuleFor(x => x.Value).NotEmpty().MaximumLength(1000);
    }
}

public class UpdateCustomFieldCommandHandler(IProfileRepository repository)
    : IRequestHandler<UpdateCustomFieldCommand, CustomFieldDto>
{
    public async Task<CustomFieldDto> Handle(UpdateCustomFieldCommand command, CancellationToken cancellationToken)
    {
        var profile = await repository.GetOrCreateAsync(cancellationToken);
        var entry = profile.CustomFields.FirstOrDefault(c => c.Id == command.Id)
            ?? throw new NotFoundException(nameof(CustomField), command.Id);
        entry.Label = command.Label;
        entry.Value = command.Value;
        await repository.UpdateAsync(cancellationToken);
        return ProfileMapper.ToDto(entry);
    }
}
