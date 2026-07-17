using FluentValidation;
using MediatR;
using Yaam.Domain.Errors;
using Yaam.Domain.Repositories;
using Yaam.UseCases.Profile.Dtos;

namespace Yaam.UseCases.Profile.Commands.Education;

public record UpdateEducationCommand(
    Guid Id,
    string Institution,
    string? Degree,
    string? FieldOfStudy,
    DateOnly? StartDate,
    DateOnly? EndDate) : IRequest<EducationDto>;

public class UpdateEducationCommandValidator : AbstractValidator<UpdateEducationCommand>
{
    public UpdateEducationCommandValidator()
    {
        RuleFor(x => x.Institution).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Degree).MaximumLength(200).When(x => x.Degree is not null);
        RuleFor(x => x.FieldOfStudy).MaximumLength(200).When(x => x.FieldOfStudy is not null);
    }
}

public class UpdateEducationCommandHandler(IProfileRepository repository)
    : IRequestHandler<UpdateEducationCommand, EducationDto>
{
    public async Task<EducationDto> Handle(UpdateEducationCommand command, CancellationToken cancellationToken)
    {
        var profile = await repository.GetOrCreateAsync(cancellationToken);
        var entry = profile.Educations.FirstOrDefault(e => e.Id == command.Id)
            ?? throw new NotFoundException(nameof(Yaam.Domain.Entities.Education), command.Id);
        entry.Institution = command.Institution;
        entry.Degree = command.Degree;
        entry.FieldOfStudy = command.FieldOfStudy;
        entry.StartDate = command.StartDate;
        entry.EndDate = command.EndDate;
        await repository.UpdateAsync(cancellationToken);
        return ProfileMapper.ToDto(entry);
    }
}
