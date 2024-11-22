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
            Type = EShipType.destroyer,
            SectionStatus = [EShipSectionStatus.ok, EShipSectionStatus.ok]
        },new ShipData()
        {
            NumberOfSections = 3,
            Orientation = [1, 0],
            Pos = new Position(0, 0),
            Type = EShipType.submarine,
            SectionStatus = [EShipSectionStatus.ok, EShipSectionStatus.ok, EShipSectionStatus.ok]
        },new ShipData()
        {
            NumberOfSections = 3,
            Orientation = [1, 0],
            Pos = new Position(0, 0),
            Type = EShipType.frigate,
            SectionStatus = [EShipSectionStatus.ok, EShipSectionStatus.ok, EShipSectionStatus.ok]
        },new ShipData()
        {
            NumberOfSections = 4,
            Orientation = [1, 0],
            Pos = new Position(0, 0),
            Type = EShipType.battleship,
            SectionStatus = [EShipSectionStatus.ok, EShipSectionStatus.ok, EShipSectionStatus.ok, EShipSectionStatus.ok]
        },new ShipData()
        {
            NumberOfSections = 5,
            Orientation = [1, 0],
            Pos = new Position(0, 0),
            Type = EShipType.carrier,
            SectionStatus = [EShipSectionStatus.ok, EShipSectionStatus.ok, EShipSectionStatus.ok, EShipSectionStatus.ok, EShipSectionStatus.ok]
        },
    ];
}