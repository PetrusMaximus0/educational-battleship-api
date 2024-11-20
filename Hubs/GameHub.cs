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
    
    // Handle the host client requesting a new session. Return the session ID and prepare empty boards.
    public async Task RequestNewSession(string[] rowTags, string[] colTags)
    {
        // Obtain a game session. Could be new or from a pool.
        var session = _gameSessionManager.CreateSession(Context.ConnectionId, rowTags, colTags);
        if(session == null)
        {
            await Clients.Caller.SendAsync("Error", "Error creating session, check if the number of column and row tags is larger than 0.");
            return;
        }
        
        // Join the host to a SignalR Group with the session ID as the group name.
        await Groups.AddToGroupAsync(Context.ConnectionId, session.Id);

        Console.WriteLine($"Sending Session Id: {session.Id}");
        // Return the session ID to the host client.
        await Clients.Client(Context.ConnectionId)
            .SendAsync("ReceiveSessionId", session.Id);
        
        Console.WriteLine($"There are currently {_gameSessionManager.GetSessionCount()} sessions ongoing.");
    }

    // Handle a client request to join an existing session by session Id.
    public async Task JoinExistingSession(string sessionId)
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
        if (session.GameState.Players[0].Id == Context.ConnectionId)
        {
            // The caller is the Host. Log the host joining the session.
            Console.WriteLine($"Host joined Session Id: {session.Id}");
        } 
        else if (session.GameState.Players[1].Id == null )
        {
            // The Caller is a Guest.
            Console.WriteLine($"Guest joined Session Id: {session.Id}");
            
            // Update Game state to reflect the Guest has joined the session.
            session.GameState.Players[1].Id = Context.ConnectionId;
            
            // Join the guest to the same SignalR group as the host
            await Groups.AddToGroupAsync(Context.ConnectionId, session.Id);
        }

        //
        if (session.GameState.Players.All(player=>player.Id != null))
        {
            // Both clients have joined the session. Send empty boards and trigger the game setup stage on both clients.
            await Clients.Groups(session.Id)
                .SendAsync("BeginGameSetup", 
                    session.GameState.RowTags, 
                    session.GameState.ColTags, 
                    session.GetEmptyBoard(),
                    session.ShipPool                     
                    );
        }
    }
    
    public async Task CloseSession(string sessionId)
    {
        var session = _gameSessionManager.RemoveSession(sessionId);
        if(session==null) 
            await Clients.Caller.SendAsync("Error", "Session Not Found");
        else 
            await Clients.Group(sessionId).SendAsync("sessionClosed", session.GameState);
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
        if (session.GameState.Players.All(player=>player.Id != Context.ConnectionId))
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
        var player = session.GameState.Players.FirstOrDefault(player => player.Id == Context.ConnectionId)!;
        await Clients.Caller.SendAsync("UpdateGameState", player.Board, player.OpponentBoard, player.Fleet, session.GameState.RowTags, session.GameState.ColTags);
    }
    
    public void LeaveSession() => _gameSessionManager.LeaveSession(Context.ConnectionId);
    
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

        // Client leaves the session.
        _gameSessionManager.LeaveSession(Context.ConnectionId);        
        
        await base.OnDisconnectedAsync(exception);
    }
}