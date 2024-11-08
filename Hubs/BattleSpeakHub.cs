using api.Game;
using Microsoft.AspNetCore.SignalR;

namespace api.Hubs;

public static class UserHandler
{
    public static HashSet<string> ConnectedUsers = new HashSet<string>();
}

public class BattleSpeakHub : Hub
{
    public override async Task OnConnectedAsync ()
    {
        //
        await Groups.AddToGroupAsync(Context.ConnectionId, Context.ConnectionId);
        Console.WriteLine ($"{Context.ConnectionId} connected");
        
        UserHandler.ConnectedUsers.Add(Context.ConnectionId);
        
        Console.WriteLine($"There are currently {UserHandler.ConnectedUsers.Count} users");
        
        //
        await base.OnConnectedAsync ();
    }
    
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        // We don't need to call remove from groups. It is called automatically.
        Console.WriteLine ($"{Context.ConnectionId} disconnected");
        UserHandler.ConnectedUsers.Remove(Context.ConnectionId);
        
        Console.WriteLine($"There are currently {UserHandler.ConnectedUsers.Count} users");
        await base.OnDisconnectedAsync(exception);
    }

    public async Task NewGame()
    {
        // Temporary, must move to different event.
        var gameSession = new GameSession();
        string gameId = gameSession.GetSessionId();
        Console.WriteLine ($" Created new session with ID: {gameId}");
        
        //
        await Clients
            .Client(this.Context.ConnectionId)
            .SendAsync("onGameStart", gameId);
    }
    
    public async Task JoinGame(string gameId)
    {
        // Test if game exists.
        const bool gameExists = true;
        
        if (gameExists)
        // Add the player to the Group and Confirm joined game.
        {
            await Groups
                .AddToGroupAsync(Context.ConnectionId, gameId);
            
            await Clients
                .Client(this.Context.ConnectionId)
                .SendAsync("ReceiveMessage", "admin", $"Joined game with id: {gameId}");
        }
        //
    }
}