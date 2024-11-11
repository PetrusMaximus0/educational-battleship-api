namespace api.Game;

public class GameState
{
    public string[] RowTags { get; set; }
    public string[] ColTags { get; set; }
    public CellData[] HostBoardData { get; set; }
    public CellData[] GuestBoardData { get; set; }    
}