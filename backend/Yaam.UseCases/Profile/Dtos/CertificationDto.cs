namespace Yaam.UseCases.Profile.Dtos;

public record CertificationDto(Guid Id, string Name, string? Issuer, DateOnly Date);
