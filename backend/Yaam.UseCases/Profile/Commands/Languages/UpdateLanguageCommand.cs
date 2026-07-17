using FluentValidation;
using MediatR;
using Yaam.Domain.Entities;
using Yaam.Domain.Enums;
using Yaam.Domain.Errors;
using Yaam.Domain.Repositories;
using Yaam.UseCases.Profile.Dtos;

namespace Yaam.UseCases.Profile.Commands.Languages;

public record UpdateLanguageCommand(Guid Id, string Name, LanguageProficiency Proficiency) : IRequest<LanguageDto>;

public class UpdateLanguageCommandValidator : AbstractValidator<UpdateLanguageCommand>
{
    public UpdateLanguageCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Proficiency).IsInEnum();
    }
}

public class UpdateLanguageCommandHandler(IProfileRepository repository)
    : IRequestHandler<UpdateLanguageCommand, LanguageDto>
{
    public async Task<LanguageDto> Handle(UpdateLanguageCommand command, CancellationToken cancellationToken)
    {
        var profile = await repository.GetOrCreateAsync(cancellationToken);
        var entry = profile.Languages.FirstOrDefault(l => l.Id == command.Id)
            ?? throw new NotFoundException(nameof(Language), command.Id);
        entry.Name = command.Name;
        entry.Proficiency = command.Proficiency;
        await repository.UpdateAsync(cancellationToken);
        return ProfileMapper.ToDto(entry);
    }
}
