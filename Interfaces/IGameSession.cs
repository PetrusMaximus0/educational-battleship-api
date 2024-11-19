using api.Models;

namespace api.Interfaces;

public interface IGameSession
{
    string Id { get; }
    GameState CurrentGameState {get; }
    ShipData[]? ShipPool {get; }
    bool SetFleet(string playerId, ShipData[] ships);
    void ReInitializeSession(string[] rowTags, string[] colTags);
}