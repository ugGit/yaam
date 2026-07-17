using FluentValidation;
using MediatR;
using Yaam.Domain.Entities;
using Yaam.Domain.Errors;
using Yaam.Domain.Repositories;
using Yaam.UseCases.Profile.Dtos;

namespace Yaam.UseCases.Profile.Commands.Links;

public record UpdateProfileLinkCommand(Guid Id, string Label, string Url) : IRequest<ProfileLinkDto>;

public class UpdateProfileLinkCommandValidator : AbstractValidator<UpdateProfileLinkCommand>
{
    public UpdateProfileLinkCommandValidator()
    {
        RuleFor(x => x.Label).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Url).NotEmpty().MaximumLength(500);
    }
}

public class UpdateProfileLinkCommandHandler(IProfileRepository repository)
    : IRequestHandler<UpdateProfileLinkCommand, ProfileLinkDto>
{
    public async Task<ProfileLinkDto> Handle(UpdateProfileLinkCommand command, CancellationToken cancellationToken)
    {
        var profile = await repository.GetOrCreateAsync(cancellationToken);
        var entry = profile.Links.FirstOrDefault(l => l.Id == command.Id)
            ?? throw new NotFoundException(nameof(ProfileLink), command.Id);
        entry.Label = command.Label;
        entry.Url = command.Url;
        await repository.UpdateAsync(cancellationToken);
        return ProfileMapper.ToDto(entry);
    }
}
