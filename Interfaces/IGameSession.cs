using api.Game;

namespace api.Interfaces;

public interface IGameSession
{
    string Id { get; }
    string? HostId {get; set;}
    string? GuestId {get; set;}
    GameState CurrentGameState {get; }
    GameState InitializeGameState(string[] colTags, string[] rowTags);
    List<ShipData> GenerateFleet();
}