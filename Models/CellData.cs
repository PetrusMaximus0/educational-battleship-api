using api.Enums;
using api.Utils;

namespace api.Models;
public class CellData(int index, Position position, ECellState eCellState)
{
    public int Index { get; } = index;
    public Position Pos { get; set; } = position;
    public ECellState State { get; set; } = eCellState;
}