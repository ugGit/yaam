using MediatR;
using Yaam.Domain.Repositories;
using Yaam.UseCases.Profile.Dtos;

namespace Yaam.UseCases.Profile.Queries;

public record GetProfileQuery : IRequest<ProfileDto>;

public class GetProfileQueryHandler(IProfileRepository repository)
    : IRequestHandler<GetProfileQuery, ProfileDto>
{
    public async Task<ProfileDto> Handle(GetProfileQuery query, CancellationToken cancellationToken)
    {
        var profile = await repository.GetOrCreateAsync(cancellationToken);
        return ProfileMapper.ToDto(profile);
    }
}
