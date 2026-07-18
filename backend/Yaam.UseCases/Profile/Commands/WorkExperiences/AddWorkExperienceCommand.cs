using FluentValidation;
using MediatR;
using Yaam.Domain.Entities;
using Yaam.Domain.Repositories;
using Yaam.UseCases.Profile.Dtos;

namespace Yaam.UseCases.Profile.Commands.WorkExperiences;

public record AddWorkExperienceCommand(
    string Company,
    string Title,
    DateOnly StartDate,
    DateOnly? EndDate,
    string? Description) : IRequest<WorkExperienceDto>;

public class AddWorkExperienceCommandValidator : AbstractValidator<AddWorkExperienceCommand>
{
    public AddWorkExperienceCommandValidator()
    {
        RuleFor(x => x.Company).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.StartDate).NotEmpty();
        RuleFor(x => x.Description).MaximumLength(2000).When(x => x.Description is not null);
    }
}

public class AddWorkExperienceCommandHandler(IProfileRepository repository)
    : IRequestHandler<AddWorkExperienceCommand, WorkExperienceDto>
{
    public async Task<WorkExperienceDto> Handle(AddWorkExperienceCommand command, CancellationToken cancellationToken)
    {
        var profile = await repository.GetOrCreateAsync(cancellationToken);
        var entry = new WorkExperience
        {
            ProfileId = profile.Id,
            Company = command.Company,
            Title = command.Title,
            StartDate = command.StartDate,
            EndDate = command.EndDate,
            Description = command.Description,
        };
        profile.WorkExperiences.Add(entry);
        await repository.UpdateAsync(cancellationToken);
        return ProfileMapper.ToDto(entry);
    }
}
