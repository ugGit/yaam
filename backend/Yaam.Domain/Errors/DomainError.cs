namespace Yaam.Domain.Errors;

public record DomainError(string Code, string Message)
{
    public static DomainError NotFound(string entity, Guid id) =>
        new($"{entity}.NotFound", $"{entity} with id '{id}' was not found.");
}
