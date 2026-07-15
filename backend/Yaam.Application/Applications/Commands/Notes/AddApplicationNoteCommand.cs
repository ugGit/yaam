using FluentValidation;
using MediatR;
using Yaam.Application.Applications.Dtos;
using Yaam.Domain.Entities;
using Yaam.Domain.Repositories;

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
        AddApplicationNoteCommand command, CancellationToken cancellationToken)
    {
        var application = await repository.GetByIdAsync(command.ApplicationId, cancellationToken);
        if (application is null) return null;

        var note = new ApplicationNote
        {
            ApplicationId = command.ApplicationId,
            Body = command.Body,
        };

        await repository.AddNoteAsync(note, cancellationToken);
        return new ApplicationNoteDto(note.Id, note.Body, note.CreatedAt, note.UpdatedAt);
    }
}
