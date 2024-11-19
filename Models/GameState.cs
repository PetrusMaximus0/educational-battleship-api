namespace api.Models;

public class GameState(string hostId, string[] rowTags, string[] colTags)
{
    public int BoardWidth => ColTags.Length;
    public int BoardHeight => RowTags.Length;
    public required string[] RowTags { get; set; } = rowTags;
    public required string[] ColTags { get; set; } = colTags;
    public PlayerData Host { get; set; } = new PlayerData()
    {
        Id = hostId,
    };
    public PlayerData Guest { get; set; } = new PlayerData();
    
}