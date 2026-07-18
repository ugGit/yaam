using MediatR;
using Yaam.Domain.Entities;
using Yaam.Domain.Errors;
using Yaam.Domain.Repositories;

namespace Yaam.UseCases.Profile.Commands.CustomFields;

public record DeleteCustomFieldCommand(Guid Id) : IRequest;

public class DeleteCustomFieldCommandHandler(IProfileRepository repository)
    : IRequestHandler<DeleteCustomFieldCommand>
{
    public async Task Handle(DeleteCustomFieldCommand command, CancellationToken cancellationToken)
    {
        var profile = await repository.GetOrCreateAsync(cancellationToken);
        var entry = profile.CustomFields.FirstOrDefault(c => c.Id == command.Id)
            ?? throw new NotFoundException(nameof(CustomField), command.Id);
        profile.CustomFields.Remove(entry);
        await repository.UpdateAsync(cancellationToken);
    }
}
