using FluentValidation;
using MediatR;
using Yaam.Domain.Repositories;
using Yaam.UseCases.Profile.Dtos;

namespace Yaam.UseCases.Profile.Commands.Education;

public record AddEducationCommand(
    string Institution,
    string? Degree,
    string? FieldOfStudy,
    DateOnly? StartDate,
    DateOnly? EndDate) : IRequest<EducationDto>;

public class AddEducationCommandValidator : AbstractValidator<AddEducationCommand>
{
    public AddEducationCommandValidator()
    {
        RuleFor(x => x.Institution).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Degree).MaximumLength(200).When(x => x.Degree is not null);
        RuleFor(x => x.FieldOfStudy).MaximumLength(200).When(x => x.FieldOfStudy is not null);
    }
}

public class AddEducationCommandHandler(IProfileRepository repository)
    : IRequestHandler<AddEducationCommand, EducationDto>
{
    public async Task<EducationDto> Handle(AddEducationCommand command, CancellationToken cancellationToken)
    {
        var profile = await repository.GetOrCreateAsync(cancellationToken);
        var entry = new Yaam.Domain.Entities.Education
        {
            ProfileId = profile.Id,
            Institution = command.Institution,
            Degree = command.Degree,
            FieldOfStudy = command.FieldOfStudy,
            StartDate = command.StartDate,
            EndDate = command.EndDate,
        };
        profile.Educations.Add(entry);
        await repository.UpdateAsync(cancellationToken);
        return ProfileMapper.ToDto(entry);
    }
}
