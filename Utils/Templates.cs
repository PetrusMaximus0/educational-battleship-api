using api.Enums;
using api.Models;

namespace api.Utils;

public struct Templates()
{
    public readonly ShipData[] Ships =
    [
        new ShipData()
        {
            NumberOfSections = 2,
            Orientation = [1, 0],
            Pos = new Position(0, 0),
            Type = ShipType.destroyer,
            SectionStatus = [ShipSectionStatus.ok, ShipSectionStatus.ok]
        },new ShipData()
        {
            NumberOfSections = 3,
            Orientation = [1, 0],
            Pos = new Position(0, 0),
            Type = ShipType.submarine,
            SectionStatus = [ShipSectionStatus.ok, ShipSectionStatus.ok, ShipSectionStatus.ok]
        },new ShipData()
        {
            NumberOfSections = 3,
            Orientation = [1, 0],
            Pos = new Position(0, 0),
            Type = ShipType.frigate,
            SectionStatus = [ShipSectionStatus.ok, ShipSectionStatus.ok, ShipSectionStatus.ok]
        },new ShipData()
        {
            NumberOfSections = 4,
            Orientation = [1, 0],
            Pos = new Position(0, 0),
            Type = ShipType.battleship,
            SectionStatus = [ShipSectionStatus.ok, ShipSectionStatus.ok, ShipSectionStatus.ok, ShipSectionStatus.ok]
        },new ShipData()
        {
            NumberOfSections = 5,
            Orientation = [1, 0],
            Pos = new Position(0, 0),
            Type = ShipType.carrier,
            SectionStatus = [ShipSectionStatus.ok, ShipSectionStatus.ok, ShipSectionStatus.ok, ShipSectionStatus.ok, ShipSectionStatus.ok]
        },
    ];
}