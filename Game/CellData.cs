using api.Enums;
using api.Utils;

namespace api.Game;
public class CellData(int index, Position position, CellState cellState)
{
    public int Index { get; } = index;
    public Position Pos { get; set; } = position;
    public CellState State { get; set; } = cellState;
}