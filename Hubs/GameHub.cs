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
        
        if (session.CurrentGameState.Host.Id == Context.ConnectionId)
        {
            // The caller is the Host. Log the host joining the session.
            Console.WriteLine($"Host joined Session Id: {session.Id}");
        }
        else if (session.CurrentGameState.Guest.Id == null )
        {
            // The Caller is a Guest.
            Console.WriteLine($"Guest joined Session Id: {session.Id}");
            
            // Update Game state to reflect the Guest has joined the session.
            session.CurrentGameState.Guest.Id = Context.ConnectionId;
            
            // Join the guest to the same SignalR group as the host
            await Groups.AddToGroupAsync(Context.ConnectionId, session.Id);
        }

        
        if (session.CurrentGameState.Host.Id != null && session.CurrentGameState.Guest.Id != null)
        {
            // Both clients have joined the session. Send empty boards and trigger the game setup stage on both clients.
            await Clients.Groups(session.Id)
                .SendAsync("BeginGameSetup", 
                    session.CurrentGameState.RowTags, 
                    session.CurrentGameState.ColTags, 
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
            await Clients.Group(sessionId).SendAsync("sessionClosed", session.CurrentGameState);
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
        bool result = session.SetFleet(Context.ConnectionId, shipData);
        await Clients.Caller.SendAsync("ShipPlacementResult", result);
        Console.WriteLine($"Validating Ship Placement result: {result}");

        if (result) 
        {
            // Placement is valid, check if all players have valid placement.
            // Todo: Check and Handle all players are validated. 
        }

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