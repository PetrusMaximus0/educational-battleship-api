using System.Collections.Concurrent;
using api.Game;

namespace api.Interfaces;

public interface IGameSessionManager
{
    // Obtain a game session. Could be new or from a pool.
    GameSession CreateSession(string hostId, string[] rowTags, string[] colTags);
    
    // Return an existing Game Session by ID.
    GameSession? GetSessionById(string sessionId);
    
    // Return an existing Session by HostId or GuestId
    GameSession? GetSessionByClientId(string clientId);
    
    // Delete an existing Game Session by ID.
    GameSession? RemoveSession(string sessionId);
    
    // Leave a session.
    void LeaveSession(string sessionId);
    
    // Return the number of active sessions
    int GetSessionCount();
}