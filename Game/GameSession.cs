namespace api.Game;

public class GameSession
{
    private string _SessionId { get; }
    public GameSession()
    {
        _SessionId = Guid.NewGuid().ToString();
        Console.WriteLine("Session ID: " + _SessionId);
    }

    public string GetSessionId()
    {
        return _SessionId;
    }
    
}