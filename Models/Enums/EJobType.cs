using System.Text.Json.Serialization;

namespace JobNSharp.Models.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum EJobType
{
    FullTime,
    PartTime,
    Contract,
    Internship,
    Temporary
}
