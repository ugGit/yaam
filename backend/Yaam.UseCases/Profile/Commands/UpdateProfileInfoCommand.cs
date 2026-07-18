using FluentValidation;
using MediatR;
using Yaam.Domain.Repositories;
using Yaam.UseCases.Profile.Dtos;

namespace Yaam.UseCases.Profile.Commands;

public record UpdateProfileInfoCommand(
    string FirstName,
    string LastName,
    string Email,
    string Phone,
    string? Location,
    string? Summary) : IRequest<ProfileDto>;

public class UpdateProfileInfoCommandValidator : AbstractValidator<UpdateProfileInfoCommand>
{
    public UpdateProfileInfoCommandValidator()
    {
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Email).NotEmpty().MaximumLength(200).EmailAddress();
        RuleFor(x => x.Phone).NotEmpty().MaximumLength(30);
        RuleFor(x => x.Location).MaximumLength(200).When(x => x.Location is not null);
        RuleFor(x => x.Summary).MaximumLength(2000).When(x => x.Summary is not null);
    }
}

public class UpdateProfileInfoCommandHandler(IProfileRepository repository)
    : IRequestHandler<UpdateProfileInfoCommand, ProfileDto>
{
    public async Task<ProfileDto> Handle(UpdateProfileInfoCommand command, CancellationToken cancellationToken)
    {
        var profile = await repository.GetOrCreateAsync(cancellationToken);
        profile.FirstName = command.FirstName;
        profile.LastName = command.LastName;
        profile.Email = command.Email;
        profile.Phone = command.Phone;
        profile.Location = command.Location;
        profile.Summary = command.Summary;
        await repository.UpdateAsync(cancellationToken);
        return ProfileMapper.ToDto(profile);
    }
}
