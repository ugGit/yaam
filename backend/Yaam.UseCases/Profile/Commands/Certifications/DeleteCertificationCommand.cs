using MediatR;
using Yaam.Domain.Entities;
using Yaam.Domain.Errors;
using Yaam.Domain.Repositories;

namespace Yaam.UseCases.Profile.Commands.Certifications;

public record DeleteCertificationCommand(Guid Id) : IRequest;

public class DeleteCertificationCommandHandler(IProfileRepository repository)
    : IRequestHandler<DeleteCertificationCommand>
{
    public async Task Handle(DeleteCertificationCommand command, CancellationToken cancellationToken)
    {
        var profile = await repository.GetOrCreateAsync(cancellationToken);
        var entry = profile.Certifications.FirstOrDefault(c => c.Id == command.Id)
            ?? throw new NotFoundException(nameof(Certification), command.Id);
        profile.Certifications.Remove(entry);
        await repository.UpdateAsync(cancellationToken);
    }
}
