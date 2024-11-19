using api.Models;

namespace api.Interfaces;

public interface IGameSession
{
    string Id { get; }
    GameState GameState {get; }
    ShipData[]? ShipPool {get; }
    bool PlaceFleet(string playerId, ShipData[] ships);
    void ReInitializeSession(string[] rowTags, string[] colTags);
}