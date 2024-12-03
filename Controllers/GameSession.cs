using api.Enums;
using api.Interfaces;
using api.Models;
using api.Utils;

namespace api.Controllers;

public class GameSession(string[] rowTags, string[] colTags) : IGameSession
{
    // Game session ID.
    public string Id { get; } = Guid.NewGuid().ToString();

    // Store the session game state.
    public GameData GameData { get; private set; } = new GameData(rowTags, colTags)
    {
        RowTags = rowTags,
        ColTags = colTags,
    };

    // Store the current Game State.
    public EGameState GameState { get; set; } = EGameState.Lobby;
    
    // Store a pending shot from the Guest.
    public int ShotIndex { get; set; } = -1;
    
    // Store the ship pool.
    public ShipData[]? ShipPool { get; private set; } = GameSetup.GetNewShipPool(rowTags.Length, colTags.Length );

    //
    public void ResetGame(string[] inRowTags, string[] inColTags)
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

    public string? IsGameOver()
    {
        var loser = GameData.Players.FirstOrDefault(
            p => p.Board!.All(cell=>cell.State!=ECellState.ship));
        if (loser == null) return null;
        
        // Return the winner's id or null if for some reason we can't find a valid winner.
        var winner = GameData.Players.FirstOrDefault(p => p.Id != loser.Id && p.Id != null);
        return winner?.Id;
    }

    public bool FireAtCell(string clientId, int index)
    {
        // Get our player's data.
        var player = GameData.Players.FirstOrDefault((player) => player.Id == clientId);
        // Get the client's opponent.
        var opponent = GameData.Players.FirstOrDefault((p)=>p.Id != clientId && p.Id != null);
        if (player==null || opponent == null) return false;
        
        // Compute the result.
        var result = opponent.Board![index].State == ECellState.ship;
        
        if (result) // The shot is a HIT.
        {
            player.OpponentBoard![index].State = ECellState.hit;
            opponent.Board![index].State = ECellState.hit;
        }
        else // The shot is a MISS.
        {
            player.OpponentBoard![index].State = ECellState.miss;
        }

        return true;
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
            foreach (var cell in newBoardData) cell.State = ECellState.miss;
        
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
                    newBoardData[index].State = ECellState.ship;
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
        return fleetSet && boardSet && opBoardSet;
    }
}