using api.Game;

namespace api.Interfaces;

public interface IGameState
{
    public CellData[] GenerateEmptyBoard(int numRows, int numCols);
}