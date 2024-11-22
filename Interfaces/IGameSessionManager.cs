using api.Controllers;

namespace api.Interfaces;

public interface IGameSessionManager
{
    // Obtain a game session. Could be new or from a pool.
    GameSession? CreateSession(string[] rowTags, string[] colTags);
    
    // Return an existing Game Session by ID.
    GameSession? GetSessionById(string sessionId);
    
    // Return an existing Session by HostId or GuestId
    GameSession? GetSessionByClientId(string clientId);
    
    // Delete an existing Game Session by ID.
    GameSession? RemoveSession(string sessionId);
    
    // Leave a session.
    void LeaveSession(string clientId);
    
    // Return the number of active sessions
    int GetSessionCount();
}