using FluentValidation;
using MediatR;
using Yaam.Domain.Entities;
using Yaam.Domain.Enums;
using Yaam.Domain.Repositories;
using Yaam.UseCases.Profile.Dtos;

namespace Yaam.UseCases.Profile.Commands.Languages;

public record AddLanguageCommand(string Name, LanguageProficiency Proficiency) : IRequest<LanguageDto>;

public class AddLanguageCommandValidator : AbstractValidator<AddLanguageCommand>
{
    public AddLanguageCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Proficiency).IsInEnum();
    }
}

public class AddLanguageCommandHandler(IProfileRepository repository)
    : IRequestHandler<AddLanguageCommand, LanguageDto>
{
    public async Task<LanguageDto> Handle(AddLanguageCommand command, CancellationToken cancellationToken)
    {
        var profile = await repository.GetOrCreateAsync(cancellationToken);
        var entry = new Language
        {
            ProfileId = profile.Id,
            Name = command.Name,
            Proficiency = command.Proficiency,
        };
        profile.Languages.Add(entry);
        await repository.UpdateAsync(cancellationToken);
        return ProfileMapper.ToDto(entry);
    }
}
