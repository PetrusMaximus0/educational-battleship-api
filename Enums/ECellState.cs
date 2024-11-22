using System.Text.Json.Serialization;

namespace api.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ECellState
{
    hidden,
    hit,
    ship,
    miss,
    sunk
}