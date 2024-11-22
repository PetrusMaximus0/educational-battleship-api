using api.Enums;

namespace api.Models;

public class PlayerData
{
    public string? Id { get; set; }
    public ShipData[]? Fleet { get; set; }
    public CellData[]? Board { get; set; }
    public CellData[]? OpponentBoard { get; set; }
    public EClientState ClientState { get; set; } = EClientState.FleetSetup;
}