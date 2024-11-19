using api.Enums;
using api.Interfaces;
using api.Models;
using api.Utils;

namespace api.Controllers;

public class GameSession(string hostId, string[] rowTags, string[] colTags) : IGameSession
{
    // Game session ID.
    public string Id { get; } = Guid.NewGuid().ToString();

    // Store the session game state.
    public GameState GameState { get; private set; } = new GameState(hostId, rowTags, colTags)
    {
        RowTags = rowTags,
        ColTags = colTags,
    };

    // Store the ship pool.
    public ShipData[]? ShipPool { get; private set; } = GameSetup.GetNewShipPool(rowTags.Length, colTags.Length );

    //
    public void ReInitializeSession(string[] inRowTags, string[] inColTags)
    {
        // Reset tags
        GameState.ColTags = inColTags;
        GameState.RowTags = inRowTags;
        
        // Reset Boards and Fleet.
        foreach (var player in GameState.Players)
        {
            player.Board = GameSetup.GetEmptyBoard(GameState.BoardHeight, GameState.BoardWidth);
            player.Fleet = null;
        }
        
        // Reset the pool
        ShipPool = GameSetup.GetNewShipPool(GameState.BoardHeight, GameState.BoardWidth);
    }

    /**
     * Sets the fleet data for the player with id: @playerId 
     */
    public bool PlaceFleet(string playerId, ShipData[] ships)
    {
        // Verify placement
        bool isValidPlacement = GameSetup.IsValidFleetPlacement(ships, GameState.BoardHeight, GameState.BoardWidth);

        if(isValidPlacement)
        {
            // Store the Fleet object in the current player Data.
            GameState.Players.First(player => player.Id == playerId).Fleet = ships;
            
            // Set the player's own board.
            CellData[] newBoardData = GameSetup.GetEmptyBoard(GameState.BoardHeight, GameState.BoardWidth);
        
            // Initialize every cell state with miss so that the cell is discovered.
            foreach (var cell in newBoardData) cell.State = CellState.miss;
        
            // Use ship information to populate cells.
            foreach (var ship in ships)
            {
                var index = ship.Pos.X + ship.Pos.Y * GameState.BoardWidth;
                if (index >= GameState.BoardHeight * GameState.BoardWidth)
                {
                    Console.WriteLine($"Index out of bounds when setting board data: {index}");
                    continue;
                }
                newBoardData[index].State = CellState.ship;
            }
        
            // 
            GameState.Players.First(player=>player.Id == playerId).Board = newBoardData;
        }        
        else
        {
            GameState.Players.First(player => player.Id == playerId).Fleet = null;
            GameState.Players.First(player => player.Id == playerId).Board = GameSetup.GetEmptyBoard(rowTags.Length, colTags.Length);
        }
        return isValidPlacement;
    }
    public CellData[] GetEmptyBoard() => GameSetup.GetEmptyBoard(GameState.BoardHeight, GameState.BoardWidth);
    public bool IsSetupComplete()
    {
        bool allSet = GameState.Players.All(player => player is
        {
            Fleet: not null, 
            Board: not null, 
            OpponentBoard: not null
        });
        return allSet;
    }
}