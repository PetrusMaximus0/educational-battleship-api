namespace api.Models;

public class GameData(string[] rowTags, string[] colTags)
{
    public int BoardWidth => ColTags.Length;
    public int BoardHeight => RowTags.Length;
    public required string[] RowTags { get; set; } = rowTags;
    public required string[] ColTags { get; set; } = colTags;
    public readonly PlayerData[] Players = [new PlayerData(), new PlayerData()];
}