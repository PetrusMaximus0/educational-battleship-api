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
    
    // Store the fleet template
    public List<ShipData> Fleet { get; } = [];
    
    // Constructor
    public GameSession(string hostId, string[] rowTags, string[] colTags)
    {
        HostId = hostId;
        InitializeGameState(rowTags, colTags);
        GenerateFleet(rowTags.Length, colTags.Length );
    }
    
    private void InitializeGameState(string[] rowTags, string[] colTags)
    {
        CurrentGameState.ColTags = colTags;
        CurrentGameState.RowTags = rowTags;
        CurrentGameState.GuestBoardData = GenerateEmptyBoard(rowTags.Length, colTags.Length );
        CurrentGameState.HostBoardData = GenerateEmptyBoard(rowTags.Length, colTags.Length);
    }

    // A number obtain from the ratio between the number of cells in a 8x8 board
    // and the classic number of cell occupying ship parts in battleship.
    private const double ShipToAreaRatio = 17.0 / 64.0 ;
    
    // Generate a ship pool based 
    private void GenerateFleet(int boardHeight, int boardWidth)
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
        availableShips.Sort((ship1,ship2)=> ship1.Size.CompareTo(ship2.Size));
        
        // Obtain available points to generate ships.

        var shipPoints = boardWidth * boardHeight * ShipToAreaRatio;  // We are okay with truncating the remainder.
       
        // Allocate the fleet from the available ships.
        // Reset fleet if current.
        Fleet.Clear();
        foreach (var ship in availableShips)
        {
            if (shipPoints >= ship.Size)
            {
                shipPoints -= ship.Size;
                Fleet.Add(ship);
            }
            else
            {
                break;
            }
        }
    }

    public void ReInitializeSession(string[] rowTags, string[] colTags)
    {
        InitializeGameState(rowTags, colTags);
        GenerateFleet(rowTags.Length, colTags.Length);
    }
    
    private static CellData[] GenerateEmptyBoard(int height, int width)
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