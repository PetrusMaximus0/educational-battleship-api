using System.Text.Json.Serialization;

namespace api.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CellState
{
    hidden,
    hit,
    ship,
    miss,
    sunk
}