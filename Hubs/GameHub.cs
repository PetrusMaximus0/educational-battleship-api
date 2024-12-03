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
        // Obtain a new game session.
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
        if (player == null) 
        {
            Console.WriteLine($"Couldn't join Session: {sessionId}. Player not found.");
            return; // This would be a very strange error.
        }
        await Clients.Caller.SendAsync("ClientStateUpdate", player.ClientState, "Joined Session!");
        await Clients.Caller.SendAsync("GameStateUpdate", session.GameState, "Joined Session!");
        
        // Join the client to the session's group
        await Groups.AddToGroupAsync(Context.ConnectionId, session.Id);
        
        // Check if session is complete.
        if (session.GameData.Players.All(p => p.Id != null))
        {
            if(session.GameState==EGameState.Lobby)
            {
                // Set the states.
                foreach (var client in session.GameData.Players) client.ClientState = EClientState.FleetSetup;
                session.GameState = EGameState.FleetSetup;
                Console.WriteLine("Setting state to Fleet Setup");
            }
                                
            // Send the state.
            await Clients.Groups(sessionId).SendAsync("GameStateUpdate", session.GameState, "Starting Game...");
            foreach (var client in session.GameData.Players)
            {
                await Clients.Client(client.Id!).SendAsync("ClientStateUpdate", client.ClientState, "Starting Game...");
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
        
        Console.WriteLine($"Fleet setup called for session {sessionId} with state: {session.GameState}");
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
       
        // Obtain a reference to the client validating the fleet placement.
        var client = session.GameData.Players.FirstOrDefault((p) => p.Id == Context.ConnectionId && p.Id != null);
        if (client == null || client.Id == null)
        {
            await Clients.Caller.SendAsync("Error", "Fleet Setup Validation failed");
            return;
        }
        
        // Return feedback of placement to client.
        // When result is true, the placement is valid
        bool result = session.PlaceFleet(Context.ConnectionId, shipData);
        if (result == false)
        {
            await Clients.Caller.SendAsync("Error", "Fleet Setup Validation failed");
            return;
        }

        // Exit execution if the setup is not yet complete.
        if (!session.IsSetupComplete())
        {
            client.ClientState = EClientState.FleetReady;
            await Clients.Caller.SendAsync("ClientStateUpdate", client.ClientState, "Fleet Setup Complete!");
            return;
        }

        // The fleet set up is complete for both players.

        // Get a reference to the other player.
        var otherClient = session.GameData.Players.FirstOrDefault((p)=>p.Id != Context.ConnectionId);
        if (otherClient == null || otherClient.Id == null)
        {
            await Clients.Groups(sessionId).SendAsync("Error", "Player not found");
            return;
        }

        // Set the other client as onTurn since they finished first.
        otherClient.ClientState = EClientState.OnTurn;
        client.ClientState = EClientState.WaitingForTurn;
        // Set the game State to ongoing since the fleet setup is complete.
        session.GameState = EGameState.GameOnGoing;
        
        // Send the updated game and client states.
        await Clients.Client(otherClient.Id).SendAsync("ClientStateUpdate", otherClient.ClientState, "Game Ongoing!");
        await Clients.Client(client.Id).SendAsync("ClientStateUpdate", client.ClientState, "Game Ongoing!");
        await Clients.Groups(sessionId).SendAsync("GameStateUpdate", session.GameState, "Game Ongoing!");
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

        // Is it a valid result?
        var result = session.FireAtCell(Context.ConnectionId, index);
        if (!result)
        {
            // Not a valid move. Refuse the command.
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
            // There is a winner. Advance to game over stage.
            await Clients.Groups(sessionId).SendAsync("GameStateUpdate", EGameState.GameOver);            
        }
    }

    // Client Request for Redoing the Fleet Placement.
    public async Task CancelFleetPlacement(string sessionId)
    {
        // Return if session not found or not in Fleet Setup Phase
        var session = _gameSessionManager.GetSessionById(sessionId);
        if (session is not { GameState: EGameState.FleetSetup }) return;
        
        // 
        var player = session.GameData.Players.FirstOrDefault(p=>p.Id == Context.ConnectionId);
        if (player == null) return;
        
        Console.WriteLine($"User{Context.ConnectionId} Canceling Fleet Placement Session: {sessionId}");
        player.ClientState = EClientState.FleetSetup;
        await Clients.Caller.SendAsync("ClientStateUpdate", player.ClientState, "Setting up Fleet");
    }
    
    // FireAtCell with Permission
    public async Task RequestShot(string sessionId, int index)
    {
        var session = _gameSessionManager.GetSessionById(sessionId);
        // Early return if session couldn't be found.
        if (session == null)
        {
            await Clients.Caller.SendAsync("Error", "Session Not Found When trying to fire at cell");
            return;
        }
        
        // Get requester data.
        var requester = session.GameData.Players.FirstOrDefault(p => p.Id == Context.ConnectionId && p.Id != null);
        if (requester == null || requester.Id == null)
        {
            await Clients.Caller.SendAsync("Error", "Player not found");
            return;
        }
        
        // If it is the Host requesting the shot, do the shot.
        if (requester.Id == session.GameData.Players[0].Id)
        {
            await ShootAtBoard(sessionId, requester.Id, index);
            return;
        }
        
        // Send a request for approval to the Host.
        await RequestShotApproval(sessionId, requester.Id, index);
        
        // Store the pending shot coordinate for the confirmation.
        session.ShotIndex = index;
    }
    
    // Handle the Host Approving the Shot from the Guest.
    public async Task ApproveShot(string sessionId)
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
        var host = session.GameData.Players.FirstOrDefault(p => p.Id == Context.ConnectionId);
        var guest = session.GameData.Players.FirstOrDefault(p => p.Id != Context.ConnectionId && p.Id != null);

        if (host == null || guest is not { Id: not null })
        {
            await Clients.Caller.SendAsync("Error", "Player not found");
            return;
        }
        
        // Confirm the client is the Host and is currently in Approving Shot state.
        if(host.ClientState != EClientState.ApprovingShot || host.Id != session.GameData.Players[0].Id)
        {
            Console.WriteLine($"Approving shot. USER: {host.Id} with state: {host.ClientState}");
            await Clients.Caller.SendAsync("Error", "Forbidden Action");
            return;
        }
        
        // Set the Host state back to Waiting for Turn.
        // await Clients.Caller.SendAsync("ClientStateUpdate", EClientState.WaitingForTurn);
        // await Clients.Client(guest.Id).SendAsync("ClientStateUpdate", EClientState.ShotApproved);
        
        // Perform the shot using the pending shot index coordinate. Update the respective states.
        await ShootAtBoard(sessionId, guest.Id, session.ShotIndex);
    }

    // Handles a client requesting a shot approval
    private async Task RequestShotApproval(string sessionId, string requesterId, int index)
    {
        // Try getting the session by id.
        var session = _gameSessionManager.GetSessionById(sessionId);
        if (session == null )
        {
            // Send an error to the caller if the session doesn't exist.
            await Clients.Caller.SendAsync("Error", "Session Not Found");
            return;
        }

        // Get player requesting approval.
        var guest = session.GameData.Players.FirstOrDefault(p => p.Id == requesterId);
        
        // Get host player.
        var host = session.GameData.Players.FirstOrDefault(p => p.Id != requesterId && p.Id != null);
        
        // Make sure both exist.
        if (guest == null || host is not {Id: not null}) 
        {
            await Clients.Caller.SendAsync("Error", "Player not found");
            return;
        }
        
        // Confirm It is the Client's turn and the Client is the Guest.
        if (guest.Id != session.GameData.Players[1].Id || guest.ClientState != EClientState.OnTurn)
        {
            Console.WriteLine($"Requesting shot Approval. USER: {guest.Id} with state: {guest.ClientState}");
            await Clients.Caller.SendAsync("Error", "Forbidden Action");
            return;
        }

        // Set the Guest PlayerState as pending shot approval.
        guest.ClientState = EClientState.PendingShotApproval;
        await Clients.Caller.SendAsync("ClientStateUpdate", guest.ClientState);
        
        // Set the Host PlayerState as approving shot so that they get a shot approval prompt.
        host.ClientState = EClientState.ApprovingShot;
        await Clients.Client(host.Id).SendAsync("ClientStateUpdate", host.ClientState);
        await Clients.Client(host.Id).SendAsync("ReceiveIndexToApprove", index, session.GameData.ColTags.Length, session.GameData.RowTags.Length);
        
        
    }

    private async Task ShootAtBoard(string sessionId, string shooterId, int index)
    {
        var session = _gameSessionManager.GetSessionById(sessionId);
        // Early return if session couldn't be found.
        if (session == null)
        {
            await Clients.Caller.SendAsync("Error", "Session Not Found When trying to fire at cell");
            return;
        }
        
        // Is it a valid result?
        var result = session.FireAtCell(shooterId, index);
        if (!result)
        {
            // Not a valid move. Refuse the command.
            await Clients.Caller.SendAsync("Error", "Not your turn. You shouldn't be able to fire this turn.");
            return;
        }
        
        // Get the shooter client
        var shooterClient = session.GameData.Players.FirstOrDefault(p => p.Id == shooterId);
        var targetClient = session.GameData.Players.FirstOrDefault(p => p.Id != shooterId);
        if (shooterClient?.Id == null || targetClient?.Id == null) 
        { 
            await Clients.Caller.SendAsync("Error", "Player not found");
            return;
        }
        
        // Update Players Data
        targetClient.ClientState = EClientState.OnTurn;
        await Clients.Client(targetClient.Id).SendAsync("ClientStateUpdate", targetClient.ClientState);
        await Clients.Client(targetClient.Id).SendAsync("UpdateBoards", targetClient.Board, targetClient.OpponentBoard);

        // Return updated states to active player
        shooterClient.ClientState = EClientState.WaitingForTurn;
        await Clients.Client(shooterClient.Id).SendAsync("ClientStateUpdate", shooterClient.ClientState);
        await Clients.Client(shooterClient.Id).SendAsync("UpdateBoards", shooterClient.Board, shooterClient.OpponentBoard);
        
        // Check for a winner
        var winnerId = session.IsGameOver();
        if (winnerId!=null)
        {
            // There is a winner. Advance to game over stage.
            session.GameState = EGameState.GameOver;
            await Clients.Groups(sessionId).SendAsync("GameStateUpdate", session.GameState);            
        }
    }
}