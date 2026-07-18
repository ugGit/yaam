using FluentValidation;
using MediatR;
using Yaam.Domain.Entities;
using Yaam.Domain.Errors;
using Yaam.Domain.Repositories;
using Yaam.UseCases.Profile.Dtos;

namespace Yaam.UseCases.Profile.Commands.WorkExperiences;

public record UpdateWorkExperienceCommand(
    Guid Id,
    string Company,
    string Title,
    DateOnly StartDate,
    DateOnly? EndDate,
    string? Description) : IRequest<WorkExperienceDto>;

public class UpdateWorkExperienceCommandValidator : AbstractValidator<UpdateWorkExperienceCommand>
{
    public UpdateWorkExperienceCommandValidator()
    {
        RuleFor(x => x.Company).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.StartDate).NotEmpty();
        RuleFor(x => x.Description).MaximumLength(2000).When(x => x.Description is not null);
    }
}

public class UpdateWorkExperienceCommandHandler(IProfileRepository repository)
    : IRequestHandler<UpdateWorkExperienceCommand, WorkExperienceDto>
{
    public async Task<WorkExperienceDto> Handle(UpdateWorkExperienceCommand command, CancellationToken cancellationToken)
    {
        var profile = await repository.GetOrCreateAsync(cancellationToken);
        var entry = profile.WorkExperiences.FirstOrDefault(w => w.Id == command.Id)
            ?? throw new NotFoundException(nameof(WorkExperience), command.Id);
        entry.Company = command.Company;
        entry.Title = command.Title;
        entry.StartDate = command.StartDate;
        entry.EndDate = command.EndDate;
        entry.Description = command.Description;
        await repository.UpdateAsync(cancellationToken);
        return ProfileMapper.ToDto(entry);
    }
}
