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
    
    // Leave a session.
    void LeaveSession(string clientId);
    
    // Join a session
    GameSession? JoinSession(string sessionId, string clientId);
    
    // Return the number of active sessions
    int GetSessionCount();
}