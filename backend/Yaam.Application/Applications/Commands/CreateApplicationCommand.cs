using FluentValidation;
using MediatR;
using Yaam.Application.Applications.Dtos;
using Yaam.Domain.Enums;
using Yaam.Domain.Repositories;
using DomainApp = Yaam.Domain.Entities.Application;

namespace Yaam.Application.Applications.Commands;

public record CreateApplicationCommand(
    string CompanyName,
    string Role,
    DateOnly? DateApplied,
    ApplicationStatus Status,
    string? ContactName,
    string? ContactEmail,
    string? ContactPhone,
    string? JobPosting) : IRequest<ApplicationDto>;

public class CreateApplicationCommandValidator : AbstractValidator<CreateApplicationCommand>
{
    public CreateApplicationCommandValidator()
    {
        RuleFor(x => x.CompanyName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Role).NotEmpty().MaximumLength(200);
        RuleFor(x => x.DateApplied)
            .NotNull()
            .WithMessage("Date applied is required unless status is Draft.")
            .When(x => x.Status != ApplicationStatus.Draft);
    }
}

public class CreateApplicationCommandHandler(IApplicationRepository repository)
    : IRequestHandler<CreateApplicationCommand, ApplicationDto>
{
    public async Task<ApplicationDto> Handle(
        CreateApplicationCommand request, CancellationToken cancellationToken)
    {
        var application = new DomainApp
        {
            CompanyName = request.CompanyName,
            Role = request.Role,
            DateApplied = request.DateApplied,
            Status = request.Status,
            ContactName = request.ContactName,
            ContactEmail = request.ContactEmail,
            ContactPhone = request.ContactPhone,
            JobPosting = request.JobPosting,
        };

        await repository.AddAsync(application, cancellationToken);

        return new ApplicationDto(
            application.Id, application.CompanyName, application.Role,
            application.DateApplied, application.Status,
            application.ContactName, application.ContactEmail, application.ContactPhone,
            application.JobPosting, application.CreatedAt, application.UpdatedAt, []);
    }
}
