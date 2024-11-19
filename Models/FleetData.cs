namespace api.Models;

public class FleetData
{
    public string CommanderId { get; set; }
    public ShipData[] Ships { get; set; }
}