using System.Collections.Concurrent;
using api.Interfaces;

namespace api.Game;

public class GameSessionManager : IGameSessionManager
{
    // The concurrent dictionary is good for multi threading access of the dictionary.
    private readonly ConcurrentDictionary<string, GameSession> _gameSessions = new();
    public GameSession CreateSession(string hostId, string[] rowTags, string[] colTags)
    {
        // Attempt to reuse old session.
        foreach (var sess in _gameSessions.Values)
        {
            if (sess.HostId != hostId) continue;
            sess.InitializeGameState(rowTags, colTags);
            return sess;
        }
        
        // Create a new session.
        var session = new GameSession
        {
            HostId = hostId
        };
        session.InitializeGameState(rowTags, colTags);
        _gameSessions[session.Id] = session;
        
        Console.WriteLine("Returning new session");
        return session;
    }
    public GameSession? GetSessionById(string sessionId)
    {
        _gameSessions.TryGetValue(sessionId, out var session);
        return session;
    }
    public GameSession? GetSessionByClientId(string clientId)
    {
        return _gameSessions.Values.FirstOrDefault(
                sess => sess.HostId == clientId || sess.GuestId == clientId
            );
    }
    public GameSession? RemoveSession(string sessionId)
    {
        if (_gameSessions.TryRemove(sessionId, out var removedSession))
        {
            Console.WriteLine($"Session with id: {sessionId} removed.");
            Console.WriteLine($"There are currently {_gameSessions.Count} sessions ongoing.");
            return removedSession;
        } 
        Console.WriteLine("Couldn't remove the session.");
        Console.WriteLine($"There are currently {_gameSessions.Count} sessions ongoing.");
        return null;
    }

    public void LeaveSession(string clientId)
    {
        // Get reference to session.
        var session = _gameSessions.Values.FirstOrDefault(sess=>sess.HostId == clientId || sess.GuestId == clientId);
        
        // Return null if a session containing this client ID doesn't exist.
        if (session == null) return;
        
        // Set this client ID null in the session, to signal the client is no longer connected to the session.
        if(session.HostId == clientId) session.HostId = null;
        else if(session.GuestId == clientId) session.GuestId = null;

        // If both the Host ID and Guest ID are null, there is no client connected to this game session.
        // Clear the session if no client is connected. 
        if(session.HostId == null && session.GuestId == null)
        {
            _gameSessions.TryRemove(session.Id, out _);
            Console.WriteLine($"Session with id: {session.Id} removed.");
        };
        
        Console.WriteLine($"Client with id: {clientId} left session with id: {session.Id}");
    }
    public int GetSessionCount ()
    {
        return _gameSessions.Count;
    }
}