
using api.Hubs;
using api.Interfaces;
using api.Singletons;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSignalR();

builder.Services.AddCors(opt =>
{
    opt.AddPolicy("ReactApp", policyBuilder =>
    {
        policyBuilder
            .WithOrigins("http://localhost:5173")
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });
});

builder.Services.AddSingleton<IGameSessionManager, GameSessionManager>();

// Service for managing game sessions.

var app = builder.Build();

app.UseHttpsRedirection();
app.MapHub<GameHub>("/battlespeak");
app.UseCors("ReactApp");

//
app.Run();
