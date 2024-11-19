using api.Interfaces;
using api.Models;
using api.Utils;

namespace api.Controllers;

public class GameSession(string hostId, string[] rowTags, string[] colTags) : IGameSession
{
    // Game session ID.
    public string Id { get; } = Guid.NewGuid().ToString();

    // Store the session game state.
    public GameState CurrentGameState { get; private set; } = new GameState(hostId, rowTags, colTags)
    {
        RowTags = rowTags,
        ColTags = colTags,
    };

    // Store the ship pool.
    public ShipData[]? ShipPool { get; private set; } = GameSetup.GetNewShipPool(rowTags.Length, colTags.Length );

    //
    public void ReInitializeSession(string[] rowTags, string[] colTags)
    {
        // Reset tags
        CurrentGameState.ColTags = colTags;
        CurrentGameState.RowTags = rowTags;
        
        // Reset Boards and Fleet.
        CurrentGameState.Guest.Board = GameSetup.GetEmptyBoard(rowTags.Length, colTags.Length);
        CurrentGameState.Guest.Fleet = null;
        CurrentGameState.Host.Board = GameSetup.GetEmptyBoard(rowTags.Length, colTags.Length);
        CurrentGameState.Host.Fleet = null;
        
        // Reset the pool
        ShipPool = GameSetup.GetNewShipPool(rowTags.Length, colTags.Length);
    }

    /**
     * Sets the fleet data for the player with id: @playerId 
     */
    public bool SetFleet(string playerId, ShipData[] ships)
    {
        // Verify placement
        bool isValidPlacement = GameSetup.IsValidFleetPlacement(ships, CurrentGameState.BoardHeight, CurrentGameState.BoardWidth);
        
        // If the placement is valid
        if (isValidPlacement)
        {
            // Store the fleet.
            StoreFleet(playerId, ships);
            
            // TODO: Update the correct board based on player Id
            
        }
        //
        return isValidPlacement;
    }
    
    private void StoreFleet(string playerId, ShipData[] shipData)
    {
        // Create the Fleet object.
        FleetData fleetData = new FleetData
        {
            CommanderId = playerId,
            Ships = shipData,
        };

        // Store the Fleet object in the current player Data.
        if (playerId == CurrentGameState.Host.Id) CurrentGameState.Host.Fleet = fleetData;
        else if (playerId == CurrentGameState.Guest.Id) CurrentGameState.Guest.Fleet = fleetData;
    }

    public CellData[] GetEmptyBoard() => GameSetup.GetEmptyBoard(CurrentGameState.BoardHeight, CurrentGameState.BoardWidth);
}