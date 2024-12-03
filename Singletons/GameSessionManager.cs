using System.Collections.Concurrent;
using api.Controllers;
using api.Interfaces;

namespace api.Singletons;

// This class manages the lifetime of sessions. It creates, removes, stores and accesses game sessions.
public class GameSessionManager : IGameSessionManager
{
    // The concurrent dictionary is good for multi threading access of the dictionary.
    private readonly ConcurrentDictionary<string, GameSession> _gameSessions = new();
        
    //
    private void RemoveSession(string sessionId)
    {
        if (_gameSessions.TryRemove(sessionId, out var removedSession))
        {
            Console.WriteLine($"Session with id: {sessionId} removed.");
            Console.WriteLine($"There are currently {_gameSessions.Count} sessions ongoing.");
            return;
        } 
        Console.WriteLine("Couldn't remove the session.");
        Console.WriteLine($"There are currently {_gameSessions.Count} sessions ongoing.");
    }
    
    /*
     * Creates a new session using hostId and returns it.
     * If a session with a matching hostId exists, then attempt to return this it.
     */
    public GameSession? CreateSession(string[] rowTags, string[] colTags)
    {
        // Check if the inputs are valid.
        if (rowTags.Length <= 0 || colTags.Length <= 0) return null; 
        
        // Create a new session.
        var session = new GameSession(rowTags, colTags);
        _gameSessions[session.Id] = session;
        
        Console.WriteLine("Returning new session");
        return session;
    }
    public GameSession? GetSessionById(string sessionId) => _gameSessions.Values.FirstOrDefault(session => session.Id == sessionId);
    public GameSession? GetSessionByClientId(string clientId) => _gameSessions.Values.FirstOrDefault(sess => sess.GameData.Players.Any(player=>player.Id == clientId));
    public void LeaveSession(string clientId)
    {
        // Get a reference to the session containing a client with this id. Stop execution if no session is found.
        var session = _gameSessions.Values.FirstOrDefault(sess=>sess.GameData.Players.Any(player=>player.Id == clientId));
        if (session == null) return;
        
        // Set this client ID null in the session, to signal the client is no longer connected to the session.
        var player = session.GameData.Players.FirstOrDefault(p => p.Id == clientId);
        if (player == null) return;
        player.Id = null;
        Console.WriteLine($"Client with id: {clientId} left session with id: {session.Id}");
       
        // Clean up session if it is empty.
        if (session.GameData.Players.All(p => p.Id == null)) RemoveSession(session.Id);
    }
    
    public GameSession? JoinSession(string sessionId, string clientId)
    {
        // Attempt to get reference to session.
        var session = GetSessionById(sessionId);
        if (session == null) return null;
        
        // Check if client id is already in the session
        var player = session.GameData.Players.FirstOrDefault(p => p.Id == clientId);
        if (player != null)
        {
            // Client tried to re-enter the session.
            Console.WriteLine($"Client with id: {clientId} has already joined the session with id: {session.Id}");
            return session;
        }
        
        // Client hasn't joined yet, obtain an empty player slot.
        player = session.GameData.Players.FirstOrDefault(p => p.Id == null);
        if (player != null) // Session has a free slot.
        {
            player.Id = clientId;
            Console.WriteLine($"Client with id: {clientId} joined session with id: {session.Id}");
            return session;
        }
        
        // Session is Full.    
        Console.WriteLine($"Client with id: {clientId} tried to join a full session with id: {session.Id}");
        return null;
    }
    public int GetSessionCount () => _gameSessions.Count;
}