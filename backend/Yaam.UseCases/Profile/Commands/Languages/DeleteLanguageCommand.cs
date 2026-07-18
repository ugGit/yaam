using MediatR;
using Yaam.Domain.Entities;
using Yaam.Domain.Errors;
using Yaam.Domain.Repositories;

namespace Yaam.UseCases.Profile.Commands.Languages;

public record DeleteLanguageCommand(Guid Id) : IRequest;

public class DeleteLanguageCommandHandler(IProfileRepository repository)
    : IRequestHandler<DeleteLanguageCommand>
{
    public async Task Handle(DeleteLanguageCommand command, CancellationToken cancellationToken)
    {
        var profile = await repository.GetOrCreateAsync(cancellationToken);
        var entry = profile.Languages.FirstOrDefault(l => l.Id == command.Id)
            ?? throw new NotFoundException(nameof(Language), command.Id);
        profile.Languages.Remove(entry);
        await repository.UpdateAsync(cancellationToken);
    }
}
