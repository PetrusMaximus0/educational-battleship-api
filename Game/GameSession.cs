using api.Enums;
using api.Interfaces;
using api.Utils;

namespace api.Game;

public class GameSession: IGameSession
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