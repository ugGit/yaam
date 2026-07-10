using FluentValidation;
using MediatR;
using Yaam.Application.Applications.Dtos;
using Yaam.Domain.Repositories;

namespace Yaam.Application.Applications.Commands.Notes;

public record UpdateApplicationNoteCommand(
    Guid ApplicationId,
    Guid NoteId,
    string Body) : IRequest<ApplicationNoteDto?>;

public class UpdateApplicationNoteCommandValidator : AbstractValidator<UpdateApplicationNoteCommand>
{
    public UpdateApplicationNoteCommandValidator()
    {
        RuleFor(x => x.Body).NotEmpty();
    }
}

public class UpdateApplicationNoteCommandHandler(IApplicationRepository repository)
    : IRequestHandler<UpdateApplicationNoteCommand, ApplicationNoteDto?>
{
    public async Task<ApplicationNoteDto?> Handle(
        UpdateApplicationNoteCommand request, CancellationToken cancellationToken)
    {
        var note = await repository.GetNoteByIdAsync(
            request.ApplicationId, request.NoteId, cancellationToken);
        if (note is null) return null;

        note.Body = request.Body;
        await repository.UpdateNoteAsync(note, cancellationToken);
        return new ApplicationNoteDto(note.Id, note.Body, note.CreatedAt, note.UpdatedAt);
    }
}
