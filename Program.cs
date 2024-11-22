
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
            .WithOrigins("http://localhost:5173", "http://192.168.0.220:5173")
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
