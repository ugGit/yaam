using System.Text.Json;

namespace Yaam.Contracts;

public sealed record MessageEnvelope(
    Guid MessageId,
    string Type,
    DateTimeOffset OccurredAt,
    JsonElement Payload);
