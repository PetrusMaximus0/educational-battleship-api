using api.Models;

namespace api.Interfaces;

public interface IGameSession
{
    string Id { get; }
    GameData GameData {get; }
    ShipData[]? ShipPool {get; }
    bool PlaceFleet(string playerId, ShipData[] ships);
    void ResetGame(string[] rowTags, string[] colTags);
    string? IsGameOver();
    bool FireAtCell(string clientId, int index);
}