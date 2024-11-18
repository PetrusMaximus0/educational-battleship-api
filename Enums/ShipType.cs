using System.Text.Json.Serialization;

namespace api.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ShipType
{
    destroyer,
    submarine,
    carrier,
    frigate,
    battleship,
}