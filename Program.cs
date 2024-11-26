
using api.Hubs;
using api.Interfaces;
using api.Singletons;

var builder = WebApplication.CreateBuilder(args);

// Load configuration from settings.

var clientUrl = builder.Configuration.GetSection("ClientUrl").Get<string[]>();

if (clientUrl is null || clientUrl.Length == 0)
{
    Console.WriteLine($"\n \n ClientOrigin was not configured. \n \n");
    return;
}

// Add services
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSignalR();

builder.Services.AddCors(opt =>
{
    opt.AddPolicy("ReactApp", policyBuilder =>
    {
        policyBuilder
            .WithOrigins(clientUrl)
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });
});

// Service for managing game sessions.
builder.Services.AddSingleton<IGameSessionManager, GameSessionManager>();

var app = builder.Build();

app.UseHttpsRedirection();
app.UseCors("ReactApp");
app.MapHub<GameHub>("/battlespeak").RequireCors("ReactApp");

//
app.Run();
