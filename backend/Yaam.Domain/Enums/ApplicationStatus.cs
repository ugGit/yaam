using System.Text.Json.Serialization;

namespace Yaam.Domain.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ApplicationStatus
{
    Draft,
    Applied,
    InterviewScheduled,
    Interviewed,
    OfferReceived,
    Accepted,
    Rejected,
    Withdrawn
}
