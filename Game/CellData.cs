using api.Enums;
using api.Utils;

namespace api.Game;
public class CellData(int index, Vector2I position, CellState cellState)
{
    public int Index { get; } = index;
    public Vector2I Pos { get; set; } = position;
    public CellState State { get; set; } = cellState;
}