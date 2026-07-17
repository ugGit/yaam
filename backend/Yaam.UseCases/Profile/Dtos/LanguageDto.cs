using Yaam.Domain.Enums;

namespace Yaam.UseCases.Profile.Dtos;

public record LanguageDto(Guid Id, string Name, LanguageProficiency Proficiency);
