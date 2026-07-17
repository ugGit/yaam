using MediatR;
using Yaam.Domain.Errors;
using Yaam.Domain.Repositories;

namespace Yaam.UseCases.Profile.Commands.Education;

public record DeleteEducationCommand(Guid Id) : IRequest;

public class DeleteEducationCommandHandler(IProfileRepository repository)
    : IRequestHandler<DeleteEducationCommand>
{
    public async Task Handle(DeleteEducationCommand command, CancellationToken cancellationToken)
    {
        var profile = await repository.GetOrCreateAsync(cancellationToken);
        var entry = profile.Educations.FirstOrDefault(e => e.Id == command.Id)
            ?? throw new NotFoundException(nameof(Yaam.Domain.Entities.Education), command.Id);
        profile.Educations.Remove(entry);
        await repository.UpdateAsync(cancellationToken);
    }
}
