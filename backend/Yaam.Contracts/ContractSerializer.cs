using System.Text.Json;

namespace Yaam.Contracts;

public static class ContractSerializer
{
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);
}
