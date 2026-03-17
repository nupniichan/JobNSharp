using System.Text.Json.Serialization;

namespace JobNSharp.Models.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum EDatePosted
{
    Anytime,
    Past24Hours,
    PastWeek,
    PastMonth
}
