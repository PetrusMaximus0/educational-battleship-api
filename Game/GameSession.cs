using api.Enums;
using api.Interfaces;
using api.Utils;

namespace api.Game;

public class GameSession : IGameSession
{
    // Game session ID.
    public string Id { get; } = Guid.NewGuid().ToString();
    // Store the id of the host
    public string? HostId { get; set; }
    
    // Store the id of the guest
    public string? GuestId { get; set; }
    
    // Store the session game state.
    public GameState CurrentGameState { get; } = new GameState();
    
    public GameState InitializeGameState(string[] rowTags, string[] colTags)
    {
        CurrentGameState.ColTags = colTags;
        CurrentGameState.RowTags = rowTags;
        CurrentGameState.GuestBoardData = GenerateEmptyBoard(colTags.Length, rowTags.Length);
        CurrentGameState.HostBoardData = GenerateEmptyBoard(colTags.Length, rowTags.Length);
        
        return CurrentGameState;
    }

    // Generate a ship pool based 
    public List<ShipData> GenerateFleet()
    {   
        // Create a default ship list.
        List<ShipData> availableShips =
        [
            new ShipData(ShipType.destroyer, 2,[1,0]),
            new ShipData(ShipType.submarine, 3,[1,0]),
            new ShipData(ShipType.frigate, 3, [1,0]),
            new ShipData(ShipType.battleship, 4,[1,0]),
            new ShipData(ShipType.carrier, 5, [1,0])
        ];

        // Sort default ship list.
        availableShips.Sort(CompareShips);
        
        // Obtain available points to generate ships.
        const double shipToAreaRatio = 17.0 / 64.0 ;
        var shipPoints = CurrentGameState.ColTags.Length * CurrentGameState.RowTags.Length * shipToAreaRatio;  // We are okay with truncating the remainder.
       
        // Allocate the fleet from the available ships.
        List<ShipData> fleet = [];
        foreach (var ship in availableShips)
        {
            if (shipPoints >= ship.Size)
            {
                shipPoints -= ship.Size;
                fleet.Add(ship);
            }
            else
            {
                break;
            }
        }
        
        // Return allocated fleet.
        return fleet;
    }

    private static int CompareShips(ShipData ship1, ShipData ship2)
    {
        return ship1.Size > ship2.Size 
            ? 1 
            : ship1.Size < ship2.Size 
                ? -1 
                : 0;
    }
    
    private static CellData[] GenerateEmptyBoard(int width, int height)
    {
        var tempBoardData = new CellData[width * height];
        for (var i = 0; i < width * height ; i++)
        {
            var newPosition = new Position(i % width, i / width);
            var cellData = new CellData(i, newPosition, CellState.hidden);
            tempBoardData[i] = cellData;
        }
        return tempBoardData;
    }
}