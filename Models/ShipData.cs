using api.Enums;
using api.Utils;

namespace api.Models;

public class ShipData
{
    // Ship id, unique to each ship.
    public string Id { get; private set; } = Guid.NewGuid().ToString();
    
    // The ship's class.
    public EShipType Type { get; set; } = EShipType.submarine;
    
    // The ship's position.
    public Position Pos { get; set; } = new Position();
    
    // The ship's direction vector.
    public int[] Orientation { get; set; } = [1,0];
    
    // The length of the ship in cells.
    public int NumberOfSections { get; set; } = 0;
    
    // Ship sections holding the ship status inside.
    public List<EShipSectionStatus> SectionStatus { get; set; } = [];
    
}
