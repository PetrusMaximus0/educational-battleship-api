using Microsoft.AspNetCore.SignalR;

namespace api;

public class GameHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        Console.WriteLine("Someone Connected!");
        await Clients.All.SendAsync("ReceiveMessage", $"{Context.ConnectionId} connected");
    }
}