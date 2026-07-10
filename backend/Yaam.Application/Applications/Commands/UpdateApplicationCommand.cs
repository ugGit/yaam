using FluentValidation;
using MediatR;
using Yaam.Application.Applications.Dtos;
using Yaam.Domain.Enums;
using Yaam.Domain.Repositories;

namespace Yaam.Application.Applications.Commands;

public record UpdateApplicationCommand(
    Guid Id,
    string CompanyName,
    string Role,
    DateOnly? DateApplied,
    ApplicationStatus Status,
    string? ContactName,
    string? ContactEmail,
    string? ContactPhone,
    string? JobPosting) : IRequest<ApplicationDto?>;

public class UpdateApplicationCommandValidator : AbstractValidator<UpdateApplicationCommand>
{
    public UpdateApplicationCommandValidator()
    {
        RuleFor(x => x.CompanyName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Role).NotEmpty().MaximumLength(200);
        RuleFor(x => x.DateApplied)
            .NotNull()
            .WithMessage("Date applied is required unless status is Draft.")
            .When(x => x.Status != ApplicationStatus.Draft);
    }
}

public class UpdateApplicationCommandHandler(IApplicationRepository repository)
    : IRequestHandler<UpdateApplicationCommand, ApplicationDto?>
{
    public async Task<ApplicationDto?> Handle(
        UpdateApplicationCommand request, CancellationToken cancellationToken)
    {
        var application = await repository.GetByIdAsync(request.Id, cancellationToken);
        if (application is null) return null;

        application.CompanyName = request.CompanyName;
        application.Role = request.Role;
        application.DateApplied = request.DateApplied;
        application.Status = request.Status;
        application.ContactName = request.ContactName;
        application.ContactEmail = request.ContactEmail;
        application.ContactPhone = request.ContactPhone;
        application.JobPosting = request.JobPosting;

        await repository.UpdateAsync(application, cancellationToken);

        return new ApplicationDto(
            application.Id, application.CompanyName, application.Role,
            application.DateApplied, application.Status,
            application.ContactName, application.ContactEmail, application.ContactPhone,
            application.JobPosting, application.CreatedAt, application.UpdatedAt,
            application.Notes.OrderByDescending(n => n.CreatedAt)
                .Select(n => new ApplicationNoteDto(n.Id, n.Body, n.CreatedAt, n.UpdatedAt))
                .ToList());
    }
}
