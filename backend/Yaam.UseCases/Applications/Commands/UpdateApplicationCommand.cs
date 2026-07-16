using FluentValidation;
using MediatR;
using Yaam.UseCases.Applications.Dtos;
using Yaam.Domain.Entities;
using Yaam.Domain.Enums;
using Yaam.Domain.Errors;
using Yaam.Domain.Repositories;

namespace Yaam.UseCases.Applications.Commands;

public record UpdateApplicationCommand(
    Guid Id,
    string CompanyName,
    string Role,
    DateOnly? DateApplied,
    ApplicationStatus Status,
    string? ContactName,
    string? ContactEmail,
    string? ContactPhone,
    string? JobPosting) : IRequest<ApplicationDto>;

public class UpdateApplicationCommandValidator : AbstractValidator<UpdateApplicationCommand>
{
    public UpdateApplicationCommandValidator()
    {
        RuleFor(x => x.CompanyName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Role).NotEmpty().MaximumLength(350);
        RuleFor(x => x.ContactPhone).MaximumLength(20).When(x => x.ContactPhone is not null);
        RuleFor(x => x.DateApplied)
            .NotNull()
            .WithMessage("Date applied is required unless status is Draft.")
            .When(x => x.Status != ApplicationStatus.Draft);
    }
}

public class UpdateApplicationCommandHandler(IApplicationRepository repository)
    : IRequestHandler<UpdateApplicationCommand, ApplicationDto>
{
    public async Task<ApplicationDto> Handle(
        UpdateApplicationCommand command, CancellationToken cancellationToken)
    {
        var application = await repository.GetByIdAsync(command.Id, cancellationToken);
        if (application is null) throw new NotFoundException(nameof(Application), command.Id);

        application.CompanyName = command.CompanyName;
        application.Role = command.Role;
        application.DateApplied = command.DateApplied;
        application.Status = command.Status;
        application.ContactName = command.ContactName;
        application.ContactEmail = command.ContactEmail;
        application.ContactPhone = command.ContactPhone;
        application.JobPosting = command.JobPosting;

        await repository.UpdateAsync(application, cancellationToken);
        return ApplicationMapper.ToDto(application);
    }
}
