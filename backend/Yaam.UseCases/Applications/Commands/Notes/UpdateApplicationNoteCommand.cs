using FluentValidation;
using MediatR;
using Yaam.UseCases.Applications.Dtos;
using Yaam.Domain.Entities;
using Yaam.Domain.Errors;
using Yaam.Domain.Repositories;

namespace Yaam.UseCases.Applications.Commands.Notes;

public record UpdateApplicationNoteCommand(
    Guid ApplicationId,
    Guid NoteId,
    string Body) : IRequest<ApplicationNoteDto>;

public class UpdateApplicationNoteCommandValidator : AbstractValidator<UpdateApplicationNoteCommand>
{
    public UpdateApplicationNoteCommandValidator()
    {
        RuleFor(x => x.Body).NotEmpty();
    }
}

public class UpdateApplicationNoteCommandHandler(IApplicationRepository repository)
    : IRequestHandler<UpdateApplicationNoteCommand, ApplicationNoteDto>
{
    public async Task<ApplicationNoteDto> Handle(
        UpdateApplicationNoteCommand command, CancellationToken cancellationToken)
    {
        var note = await repository.GetNoteByIdAsync(
            command.ApplicationId, command.NoteId, cancellationToken);
        if (note is null)
            throw new NotFoundException(nameof(ApplicationNote), command.NoteId);

        note.Body = command.Body;
        await repository.UpdateNoteAsync(note, cancellationToken);
        return new ApplicationNoteDto(note.Id, note.Body, note.CreatedAt, note.UpdatedAt);
    }
}
