using FluentValidation;
using MediatR;
using Yaam.Domain.Entities;
using Yaam.Domain.Errors;
using Yaam.Domain.Repositories;
using Yaam.UseCases.Profile.Dtos;

namespace Yaam.UseCases.Profile.Commands.Certifications;

public record UpdateCertificationCommand(Guid Id, string Name, string? Issuer, DateOnly Date) : IRequest<CertificationDto>;

public class UpdateCertificationCommandValidator : AbstractValidator<UpdateCertificationCommand>
{
    public UpdateCertificationCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Issuer).MaximumLength(200).When(x => x.Issuer is not null);
        RuleFor(x => x.Date).NotEmpty();
    }
}

public class UpdateCertificationCommandHandler(IProfileRepository repository)
    : IRequestHandler<UpdateCertificationCommand, CertificationDto>
{
    public async Task<CertificationDto> Handle(UpdateCertificationCommand command, CancellationToken cancellationToken)
    {
        var profile = await repository.GetOrCreateAsync(cancellationToken);
        var entry = profile.Certifications.FirstOrDefault(c => c.Id == command.Id)
            ?? throw new NotFoundException(nameof(Certification), command.Id);
        entry.Name = command.Name;
        entry.Issuer = command.Issuer;
        entry.Date = command.Date;
        await repository.UpdateAsync(cancellationToken);
        return ProfileMapper.ToDto(entry);
    }
}
