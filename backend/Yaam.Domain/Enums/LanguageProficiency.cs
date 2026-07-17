using System.Text.Json.Serialization;

namespace Yaam.Domain.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum LanguageProficiency
{
    Basic,
    BusinessProficiency,
    Fluent,
    Native
}
