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
    public GameData GameData { get; private set; } = new GameData(hostId, rowTags, colTags)
    {
        RowTags = rowTags,
        ColTags = colTags,
    };

    // Store the current Game State.
    public EGameState GameState { get; set; } = EGameState.Lobby;
    
    // Store the ship pool.
    public ShipData[]? ShipPool { get; private set; } = GameSetup.GetNewShipPool(rowTags.Length, colTags.Length );

    //
    public void ReInitializeSession(string[] inRowTags, string[] inColTags)
    {
        // Reset tags
        GameData.ColTags = inColTags;
        GameData.RowTags = inRowTags;
        
        // Reset Boards and Fleet.
        foreach (var player in GameData.Players)
        {
            player.Board = GameSetup.GetEmptyBoard(GameData.BoardHeight, GameData.BoardWidth);
            player.Fleet = null;
        }
        
        // Reset the pool
        ShipPool = GameSetup.GetNewShipPool(GameData.BoardHeight, GameData.BoardWidth);
    }

    /**
     * Sets the fleet data for the player with id: @playerId 
     */
    public bool PlaceFleet(string playerId, ShipData[] ships)
    {
        // Verify placement
        bool isValidPlacement = GameSetup.IsValidFleetPlacement(ships, GameData.BoardHeight, GameData.BoardWidth);

        if(isValidPlacement)
        {
            // Store the Fleet object in the current player Data.
            GameData.Players.First(player => player.Id == playerId).Fleet = ships;
            
            // Set the player's own board.
            CellData[] newBoardData = GameSetup.GetEmptyBoard(GameData.BoardHeight, GameData.BoardWidth);

            // Initialize every cell state with miss so that the cell is discovered.
            foreach (var cell in newBoardData) cell.State = CellState.miss;
        
            // Use ship information to populate cells.
            foreach (var ship in ships)
            {
                for (var i = 0; i < ship.NumberOfSections; i++)
                {
                    var sectionPos = new Position(ship.Pos.X + i * ship.Orientation[0], ship.Pos.Y + i * ship.Orientation[1]);
                    var index = sectionPos.X + sectionPos.Y * GameData.BoardWidth;
                    if (index >= GameData.BoardHeight * GameData.BoardWidth || index < 0)
                    {
                        Console.WriteLine($"Index out of bounds when setting board data: {index}");
                        continue;
                    }
                    newBoardData[index].State = CellState.ship;
                }
            }
        
            // 
            GameData.Players.First(player=>player.Id == playerId).Board = newBoardData;
            GameData.Players.First(player=>player.Id == playerId).OpponentBoard = GameSetup.GetEmptyBoard(GameData.BoardHeight, GameData.BoardWidth);
        }        
        else
        {
            GameData.Players.First(player => player.Id == playerId).Fleet = null;
            GameData.Players.First(player => player.Id == playerId).Board = GameSetup.GetEmptyBoard(rowTags.Length, colTags.Length);
        }
        return isValidPlacement;
    }
    
    /**/
    public CellData[] GetEmptyBoard() => GameSetup.GetEmptyBoard(GameData.BoardHeight, GameData.BoardWidth);
    
    /**/
    public bool IsSetupComplete()
    {
        bool fleetSet = GameData.Players.All(player => player.Fleet != null);
        bool boardSet = GameData.Players.All(player => player.Board != null);
        bool opBoardSet = GameData.Players.All(player => player.OpponentBoard != null);
        Console.WriteLine($"fleet: {fleetSet},  board: {boardSet},  opBoard: {opBoardSet}");
        return fleetSet && boardSet && opBoardSet;
    }
}