using System.Text.Json.Serialization;

namespace api.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum EShipType
{
    destroyer,
    submarine,
    carrier,
    frigate,
    battleship,
}