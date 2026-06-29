using Chess.Interfaces;

namespace Chess.Models;

public class Board : IBoard
{
    public ICell[,] Cells { get; }

    public Board(ICell[,] cells)
    {
        Cells = cells;
    }
}