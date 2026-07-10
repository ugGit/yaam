using FluentValidation;
using MediatR;
using Yaam.Application.Applications.Dtos;
using Yaam.Domain.Repositories;
using DomainNote = Yaam.Domain.Entities.ApplicationNote;

namespace Yaam.Application.Applications.Commands.Notes;

public record AddApplicationNoteCommand(
    Guid ApplicationId,
    string Body) : IRequest<ApplicationNoteDto?>;

public class AddApplicationNoteCommandValidator : AbstractValidator<AddApplicationNoteCommand>
{
    public AddApplicationNoteCommandValidator()
    {
        RuleFor(x => x.Body).NotEmpty();
    }
}

public class AddApplicationNoteCommandHandler(IApplicationRepository repository)
    : IRequestHandler<AddApplicationNoteCommand, ApplicationNoteDto?>
{
    public async Task<ApplicationNoteDto?> Handle(
        AddApplicationNoteCommand request, CancellationToken cancellationToken)
    {
        var application = await repository.GetByIdAsync(request.ApplicationId, cancellationToken);
        if (application is null) return null;

        var note = new DomainNote
        {
            ApplicationId = request.ApplicationId,
            Body = request.Body,
        };

        await repository.AddNoteAsync(note, cancellationToken);
        return new ApplicationNoteDto(note.Id, note.Body, note.CreatedAt, note.UpdatedAt);
    }
}
