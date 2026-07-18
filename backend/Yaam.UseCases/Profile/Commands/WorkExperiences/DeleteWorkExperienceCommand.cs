using MediatR;
using Yaam.Domain.Entities;
using Yaam.Domain.Errors;
using Yaam.Domain.Repositories;

namespace Yaam.UseCases.Profile.Commands.WorkExperiences;

public record DeleteWorkExperienceCommand(Guid Id) : IRequest;

public class DeleteWorkExperienceCommandHandler(IProfileRepository repository)
    : IRequestHandler<DeleteWorkExperienceCommand>
{
    public async Task Handle(DeleteWorkExperienceCommand command, CancellationToken cancellationToken)
    {
        var profile = await repository.GetOrCreateAsync(cancellationToken);
        var entry = profile.WorkExperiences.FirstOrDefault(w => w.Id == command.Id)
            ?? throw new NotFoundException(nameof(WorkExperience), command.Id);
        profile.WorkExperiences.Remove(entry);
        await repository.UpdateAsync(cancellationToken);
    }
}
