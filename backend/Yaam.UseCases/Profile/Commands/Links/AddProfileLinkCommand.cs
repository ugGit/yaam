using FluentValidation;
using MediatR;
using Yaam.Domain.Entities;
using Yaam.Domain.Repositories;
using Yaam.UseCases.Profile.Dtos;

namespace Yaam.UseCases.Profile.Commands.Links;

public record AddProfileLinkCommand(string Label, string Url) : IRequest<ProfileLinkDto>;

public class AddProfileLinkCommandValidator : AbstractValidator<AddProfileLinkCommand>
{
    public AddProfileLinkCommandValidator()
    {
        RuleFor(x => x.Label).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Url).NotEmpty().MaximumLength(500);
    }
}

public class AddProfileLinkCommandHandler(IProfileRepository repository)
    : IRequestHandler<AddProfileLinkCommand, ProfileLinkDto>
{
    public async Task<ProfileLinkDto> Handle(AddProfileLinkCommand command, CancellationToken cancellationToken)
    {
        var profile = await repository.GetOrCreateAsync(cancellationToken);
        var entry = new ProfileLink
        {
            ProfileId = profile.Id,
            Label = command.Label,
            Url = command.Url,
        };
        profile.Links.Add(entry);
        await repository.UpdateAsync(cancellationToken);
        return ProfileMapper.ToDto(entry);
    }
}
