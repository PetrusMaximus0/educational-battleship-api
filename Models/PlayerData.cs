namespace api.Models;

public class PlayerData
{
    public string? Id { get; set; }
    public FleetData? Fleet { get; set; }
    public CellData[]? Board { get; set; }
}