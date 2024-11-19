using api.Enums;
using api.Models;

namespace api.Utils;

public static class GameSetup
{
    private const double ShipSectionsToAreaRatio = 17.0 / 64.0;
    public static CellData[] GetEmptyBoard(int height, int width)
    {
        var newBoard = new CellData[width * height];
        for (var i = 0; i < width * height ; i++)
        {
            var newPosition = new Position(i % width, i / width);
            var cellData = new CellData(i, newPosition, CellState.hidden);
            newBoard[i] = cellData;
        }
        return newBoard;
    }
    public static ShipData[] GetNewShipPool(int boardHeight, int boardWidth)
    {
        // Get the default ship list.
        ShipData[] availableShips = new Templates().Ships;
        
        // 
        List<ShipData> shipPool = new List<ShipData>();
        
        // Obtain available points to select ships.
        var shipPoints = boardWidth * boardHeight * ShipSectionsToAreaRatio;  // We are okay with truncating the remainder.
       
        // Allocate ships from the template to the ship pool
        // Reset ship pool if current.
        foreach (var ship in availableShips)
        {
            if (shipPoints >= ship.NumberOfSections)
            {
                shipPoints -= ship.NumberOfSections;
                shipPool.Add(ship);
            }
            else
            {
                break;
            }
        }
        return shipPool.ToArray();
    }
    private static double MinDistanceBetweenShips(ShipData ship1, ShipData ship2)
    {
        double minDistance = double.MaxValue;
        for (var i = 0; i < ship1.NumberOfSections; i++)
        {
            for (var j = 0; j < ship2.NumberOfSections; j++)
            {
                double distX = Math.Abs(ship1.Pos.X + i * ship1.Orientation[0] - ship2.Pos.X + j * ship2.Orientation[0]);
                double distY = Math.Abs(ship1.Pos.Y + i * ship1.Orientation[1] - ship2.Pos.Y + j * ship2.Orientation[1]);
                var dist = Math.Sqrt(distX * distX + distY * distY);
                if(dist < minDistance) minDistance = dist;
            }
        }        
        return minDistance;
    }
    private static bool IsShipInBounds(ShipData ship, int boardHeight, int boardWidth)
    {
        // Check the X coordinate
        bool xOk = ship.Pos.X >= 0
                   && ship.Pos.X < boardWidth
                   && ship.Pos.X + ship.Orientation[0] * ship.NumberOfSections <= boardWidth
                   && ship.Pos.X + ship.Orientation[0] * (ship.NumberOfSections - 1) >= 0;
        // Check the Y coordinate
        bool yOk = ship.Pos.Y >= 0 
                && ship.Pos.Y < boardHeight 
                && ship.Pos.Y + ship.Orientation[1] * ship.NumberOfSections <= boardHeight
                && ship.Pos.Y + ship.Orientation[1] * (ship.NumberOfSections - 1) >= 0;
        
        return xOk && yOk;
    }
    
    // Ship placement validation.
    public static bool IsValidFleetPlacement(ShipData[] ships, int boardHeight, int boardWidth)
    {
        for (var i = 0; i < ships.Length; i++)
        {
            // Check if the ship is within the bounds of the board.
            if(!IsShipInBounds(ships[i], boardHeight, boardWidth)) return false;
            
            // Check the ship has an appropriate distance to other ships.
            for (var j = i + 1; j < ships.Length; j++)
            {
                var shipDistance = MinDistanceBetweenShips(ships[i], ships[j]);
                if (shipDistance <= 2) return false;
            }
        }
        //
        return true;
    }
}