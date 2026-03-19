using System.Text.Json.Serialization;

namespace JobNSharp.Models.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ESite
{
    LinkedIn,
    Indeed,
    Glassdoor
}
