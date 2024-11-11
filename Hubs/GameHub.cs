using api.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace api.Hubs;

public class GameHub(IGameSessionManager gameSessionManager) : Hub
{
    private readonly IGameSessionManager _gameSessionManager = gameSessionManager;

    // Store currently connected user Ids in a HashSet so that we can keep count of how many users are connected currently.
    private static readonly HashSet<string> ConnectedUsers = [];
    
    // Handle the host client requesting a new session. Return the session ID and prepare empty boards.
    public async Task RequestNewSession(string[] rowTags, string[] colTags)
    {
        // Obtain a game session. Could be new or from a pool.
        var session = _gameSessionManager.CreateSession(Context.ConnectionId, rowTags, colTags);
        
        // Join the host to a SignalR Group with the session ID as the group name.
        await Groups.AddToGroupAsync(Context.ConnectionId, session.Id);

        // Return the session ID to the host client.
        await Clients.Client(Context.ConnectionId)
            .SendAsync("ReceiveSessionId", session.Id);
        Console.WriteLine($"Sending Session Id: {session.Id}");
        Console.WriteLine($"There are currently {_gameSessionManager.GetSessionCount()} sessions ongoing.");
    }

    public async Task JoinExistingSession(string sessionId)
    {
        // Check if the session exists
        var session = _gameSessionManager.GetSessionById(sessionId);
        if (session != null )
        {
            // Set the session guest id.
            if (session.HostId == Context.ConnectionId) // host can not join as client.
            {
                await Clients.Caller.SendAsync("Error", "The host can not join as a client.");
                throw new HubException("The host is already connected.");
            }
            //
            session.GuestId = Context.ConnectionId;
            
            // Join the guest to the same SignalR group as the host
            await Groups.AddToGroupAsync(Context.ConnectionId, session.Id);
            
            // Sends empty boards and trigger the game setup stage on both clients.
            await Clients.Groups(session.Id)
                .SendAsync("BeginGameSetup", session.CurrentGameState.HostBoardData);
        }
        else
        {
            // Send an error to the caller if the session doesn't exist.
            await Clients.Caller.SendAsync("Error", "Session Not Found");
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

    public override async Task OnDisconnectedAsync(Exception exception)
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