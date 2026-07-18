using FluentValidation;
using MediatR;
using Yaam.Domain.Entities;
using Yaam.Domain.Repositories;
using Yaam.UseCases.Profile.Dtos;

namespace Yaam.UseCases.Profile.Commands.Certifications;

public record AddCertificationCommand(string Name, string? Issuer, DateOnly Date) : IRequest<CertificationDto>;

public class AddCertificationCommandValidator : AbstractValidator<AddCertificationCommand>
{
    public AddCertificationCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Issuer).MaximumLength(200).When(x => x.Issuer is not null);
        RuleFor(x => x.Date).NotEmpty();
    }
}

public class AddCertificationCommandHandler(IProfileRepository repository)
    : IRequestHandler<AddCertificationCommand, CertificationDto>
{
    public async Task<CertificationDto> Handle(AddCertificationCommand command, CancellationToken cancellationToken)
    {
        var profile = await repository.GetOrCreateAsync(cancellationToken);
        var entry = new Certification
        {
            ProfileId = profile.Id,
            Name = command.Name,
            Issuer = command.Issuer,
            Date = command.Date,
        };
        profile.Certifications.Add(entry);
        await repository.UpdateAsync(cancellationToken);
        return ProfileMapper.ToDto(entry);
    }
}
