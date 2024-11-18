using api.Enums;

namespace api.Game;

public class ShipData(ShipType shipType, int size, int[] orientation)
{
    //
    private const int MaxShipSize = 5;
    private const int MinShipSize = 1;

    //
    public string Id { get; } = Guid.NewGuid().ToString();
    public ShipType Type { get; } = shipType;
    public int[] Orientation { get; set; } = orientation;
    public int Size { get; } = Math.Clamp(size, MinShipSize, MaxShipSize);    
}