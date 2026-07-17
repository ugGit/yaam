using FluentValidation;
using MediatR;
using Yaam.Domain.Repositories;
using Yaam.UseCases.Profile.Dtos;

namespace Yaam.UseCases.Profile.Commands;

public record UpdateProfileSkillsCommand(List<string> Skills) : IRequest<ProfileDto>;

public class UpdateProfileSkillsCommandValidator : AbstractValidator<UpdateProfileSkillsCommand>
{
    public UpdateProfileSkillsCommandValidator()
    {
        RuleForEach(x => x.Skills).NotEmpty().MaximumLength(100);
    }
}

public class UpdateProfileSkillsCommandHandler(IProfileRepository repository)
    : IRequestHandler<UpdateProfileSkillsCommand, ProfileDto>
{
    public async Task<ProfileDto> Handle(UpdateProfileSkillsCommand command, CancellationToken cancellationToken)
    {
        var profile = await repository.GetOrCreateAsync(cancellationToken);
        profile.Skills = command.Skills;
        await repository.UpdateAsync(cancellationToken);
        return ProfileMapper.ToDto(profile);
    }
}
