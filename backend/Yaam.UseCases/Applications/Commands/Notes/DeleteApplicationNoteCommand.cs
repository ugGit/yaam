using MediatR;
using Yaam.Domain.Repositories;

namespace Yaam.UseCases.Applications.Commands.Notes;

public record DeleteApplicationNoteCommand(
    Guid ApplicationId,
    Guid NoteId) : IRequest;

public class DeleteApplicationNoteCommandHandler(IApplicationRepository repository)
    : IRequestHandler<DeleteApplicationNoteCommand>
{
    public async Task Handle(
        DeleteApplicationNoteCommand command, CancellationToken cancellationToken)
        => await repository.DeleteNoteAsync(command.ApplicationId, command.NoteId, cancellationToken);
}
