using MediatR;
using Yaam.Domain.Entities;
using Yaam.Domain.Errors;
using Yaam.Domain.Repositories;

namespace Yaam.UseCases.Profile.Commands.Links;

public record DeleteProfileLinkCommand(Guid Id) : IRequest;

public class DeleteProfileLinkCommandHandler(IProfileRepository repository)
    : IRequestHandler<DeleteProfileLinkCommand>
{
    public async Task Handle(DeleteProfileLinkCommand command, CancellationToken cancellationToken)
    {
        var profile = await repository.GetOrCreateAsync(cancellationToken);
        var entry = profile.Links.FirstOrDefault(l => l.Id == command.Id)
            ?? throw new NotFoundException(nameof(ProfileLink), command.Id);
        profile.Links.Remove(entry);
        await repository.UpdateAsync(cancellationToken);
    }
}
