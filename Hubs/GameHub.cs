using api.Enums;
using api.Interfaces;
using api.Models;
using Microsoft.AspNetCore.SignalR;

namespace api.Hubs;

// The game hub is responsible for handling all events related to connectivity between clients and server.
public class GameHub(IGameSessionManager gameSessionManager) : Hub
{
    //
    private readonly IGameSessionManager _gameSessionManager = gameSessionManager;
    
    // Store currently connected user Ids in a HashSet so that we can keep count of how many users are connected currently.
    private static readonly HashSet<string> ConnectedUsers = [];
    
    // Connections
    public override async Task OnConnectedAsync()
    {
        // Add the new connection to the list of connected users.
        ConnectedUsers.Add(Context.ConnectionId);
        
        // Log this connection
        Console.WriteLine($"User has connected to server: {Context.ConnectionId}");
        
        // Log number of users connected.
        Console.WriteLine($"We now have {ConnectedUsers.Count} users connected to the server");
        
        await base.OnConnectedAsync();
    }
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        // Remove the connection ID from the list of connected users.
        ConnectedUsers.Remove(Context.ConnectionId);
        
        // Log this connection
        Console.WriteLine($"User has disconnected from server: {Context.ConnectionId}");
        
        // Log number of users connected.
        Console.WriteLine($"We now have {ConnectedUsers.Count} users connected to the server");

        // Attempt to disconnect the client from any sessions they are connected to.
        _gameSessionManager.LeaveSession(Context.ConnectionId);        
        
        await base.OnDisconnectedAsync(exception);
    }
    
    // Sessions
    // Handle a client requesting a new session. Return the session ID and prepare empty boards.
    public async Task RequestNewSession(string[] rowTags, string[] colTags)
    {
        // Obtain a game session. Could be new or from a pool.
        var session = _gameSessionManager.CreateSession(rowTags, colTags);
        if(session == null)
        {
            await Clients.Caller.SendAsync("Error", "Error creating session, check if the number of column and row tags is larger than 0.");
            return;
        }
        
        // Return the session ID to the host client.
        Console.WriteLine($"Sending Session Id: {session.Id}");
        await Clients.Caller
            .SendAsync("ReceiveSessionId", session.Id);

        // 
        Console.WriteLine($"There are currently {_gameSessionManager.GetSessionCount()} sessions ongoing.");
    }

    // Handle a client request to join an existing session by session id.
    public async Task JoinSession(string sessionId)
    {
        // Try to join the session with sessionId
        var session = _gameSessionManager.JoinSession(sessionId, Context.ConnectionId);
        if (session == null )
        {
            // Send an error to the caller if the session doesn't exist.
            Console.WriteLine($"Couldn't join Session: {sessionId}");
            await Clients.Caller.SendAsync("SessionNotFound");
            return;
        }
        // Send game and player states to the client
        var player = session.GameData.Players.FirstOrDefault((p)=>p.Id == Context.ConnectionId);
        if (player == null) return; // This would be a very strange error.
        await Clients.Caller.SendAsync("ClientStateUpdate", player.ClientState);
        await Clients.Caller.SendAsync("GameStateUpdate", session.GameState);
        
        // Join the client to the session's group
        await Groups.AddToGroupAsync(Context.ConnectionId, session.Id);
        
        // Check if session is complete.
        if (session.GameData.Players.All(p => p.Id != null))
        {
            if(session.GameState==EGameState.GameOver)
            {
                // Set the states.
                foreach (var client in session.GameData.Players) client.ClientState = EClientState.FleetSetup;
                session.GameState = EGameState.FleetSetup;
                Console.WriteLine("Setting state to Fleet Setup");
            }
                                
            // Send the state.
            await Clients.Groups(sessionId).SendAsync("GameStateUpdate", session.GameState);
            foreach (var client in session.GameData.Players)
            {
                await Clients.Client(client.Id!).SendAsync("ClientStateUpdate", client.ClientState);
            }
        }
    }
    
    // Handle a client requesting to leave a session they are a part of.
    public void LeaveSession() => _gameSessionManager.LeaveSession(Context.ConnectionId);
    
    /* Handle a request for initializing the Fleet setup. */
    public async Task FleetSetup(string sessionId)
    {
        // Try getting the session by id.
        var session = _gameSessionManager.GetSessionById(sessionId);
        if (session == null )
        {
            // Send an error to the caller if the session doesn't exist.
            await Clients.Caller.SendAsync("Error", "Session Not Found");
            return;
        }
        
        //
        if (session.GameState != EGameState.FleetSetup)
        {
            Console.WriteLine($"Game state is not fleet setup. State: {session.GameState}");
            await Clients.Caller.SendAsync("Error", "Game state is not fleet setup.");
            return;
        }
        Console.WriteLine($"Fleet setup called for session {sessionId}");
        await Clients.Client(Context.ConnectionId)
            .SendAsync("BeginFleetSetup", 
                session.GameData.RowTags, 
                session.GameData.ColTags, 
                session.GetEmptyBoard(),
                session.ShipPool                     
            );
    }
    public async Task ValidateFleetPlacement(string sessionId, ShipData[] shipData)
    {
        var session = _gameSessionManager.GetSessionById(sessionId);

        // Send an error to the caller if the session doesn't exist.
        if (session == null)
        {
            await Clients.Caller.SendAsync("Error", "Session Not Found");
            return;
        }
        
        // Return feedback of placement to client.
        // When result is true, the placement is valid
        bool result = session.PlaceFleet(Context.ConnectionId, shipData);
        await Clients.Caller.SendAsync("ShipPlacementResult", result);
        Console.WriteLine($"Validating Ship Placement result: {result}");

        if (result && session.IsSetupComplete())
        {
            //
            Console.WriteLine($"Ship Placement Complete");
            await Clients.Groups(sessionId).SendAsync("StartGame");
        }
    }
    public async Task BeginGame(string sessionId)
    {
        Console.WriteLine($"Beginning Game Session: {sessionId}");
        //
        var session = _gameSessionManager.GetSessionById(sessionId);
        if(session == null)
        {
            await Clients.Caller.SendAsync("Error", "Session Not Found");
            return;
        }
        
        // 
        if (session.GameData.Players.All(player=>player.Id != Context.ConnectionId))
        {
            await Clients.Caller.SendAsync("Error", "Session Not Found");
            return;
        }
        
        // Null checks all the required fields for the next step.
        if (!session.IsSetupComplete())
        {
            await Clients.Groups(sessionId).SendAsync("Error", "Game Setup Not Complete!");
            return;
        }
        
        // Send The Respective ships, board and opponent board.
        var player = session.GameData.Players.FirstOrDefault(player => player.Id == Context.ConnectionId)!;
        await Clients.Caller.SendAsync("GameBoardsInit", session.GameData.RowTags, session.GameData.ColTags, player.Board, player.OpponentBoard);
        Console.WriteLine($"Sending initial game data to player: {player.Id}");
    }
    public async Task FireAtCell(string sessionId, int index)
    {
        var session = _gameSessionManager.GetSessionById(sessionId);
        // Early return if session couldn't be found.
        if (session == null)
        {
            await Clients.Caller.SendAsync("Error", "Session Not Found When trying to fire at cell");
            return;
        }

        // Is it this guy's turn ?
        var result = session.FireAtCell(Context.ConnectionId, index);
        if (!result)
        {
            // Not this client's turn. Refuse the command.
            await Clients.Caller.SendAsync("Error", "Not your turn. You shouldn't be able to fire this turn.");
            return;
        }
        
        // Get player data.
        var player = session.GameData.Players.FirstOrDefault(p => p.Id == Context.ConnectionId);
        var outOfTurnPlayer = session.GameData.Players.FirstOrDefault(p => p.Id != Context.ConnectionId);
        if (player?.Id == null || outOfTurnPlayer?.Id == null) 
        { 
            await Clients.Caller.SendAsync("Error", "Player not found");
            return;
        }
        
        // Update Players Data
        await Clients.Client(outOfTurnPlayer.Id).SendAsync("ClientStateUpdate", EClientState.OnTurn);
        await Clients.Client(outOfTurnPlayer.Id).SendAsync("UpdateBoards", outOfTurnPlayer.Board, outOfTurnPlayer.OpponentBoard);

        // Return updated states to active player
        await Clients.Caller.SendAsync("ClientStateUpdate", EClientState.WaitingForTurn);
        await Clients.Caller.SendAsync("UpdateBoards", player.Board, player.OpponentBoard );
        
        // Check for a winner
        var winnerId = session.IsGameOver();
        if (winnerId!=null)
        {
            // TODO: There is a winner. Advance to game over stage.
            await Clients.Groups(sessionId).SendAsync("GameStateUpdate", EGameState.GameOver);            
        }
    }
}