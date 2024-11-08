using Microsoft.AspNetCore.SignalR;

namespace api.Hubs;

public class BattleSpeakHub : Hub
{
    public override async Task OnConnectedAsync ()
    {
        Console.WriteLine ($"{Context.ConnectionId} connected");
    }

    public async Task JoinGameHub()
    {
        const string userId = "userId";
        await Clients.All
            .SendAsync("UserConnected", "admin", $"User with id: {userId} has connected");
    }

    public async Task RequestNewGame()
    {
        const string battleId = "XXXXX";
        await Groups
            .AddToGroupAsync(Context.ConnectionId, battleId);
        
        await Clients
            .Client(this.Context.ConnectionId)
            .SendAsync("ReceiveMessage", "admin", battleId);
        
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