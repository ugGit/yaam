using MediatR;
using Yaam.Domain.Repositories;

namespace Yaam.Application.Applications.Commands.Notes;

public record DeleteApplicationNoteCommand(
    Guid ApplicationId,
    Guid NoteId) : IRequest;

public class DeleteApplicationNoteCommandHandler(IApplicationRepository repository)
    : IRequestHandler<DeleteApplicationNoteCommand>
{
    public async Task Handle(
        DeleteApplicationNoteCommand request, CancellationToken cancellationToken)
        => await repository.DeleteNoteAsync(request.ApplicationId, request.NoteId, cancellationToken);
}
