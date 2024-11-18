using api.Game;

namespace api.Interfaces;

public interface IGameSession
{
    string Id { get; }
    string? HostId {get; set;}
    string? GuestId {get; set;}
    GameState CurrentGameState {get; }
    List<ShipData> Fleet {get; }
}