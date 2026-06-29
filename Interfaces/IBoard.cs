namespace Chess.Interfaces;

public interface IBoard
{
    ICell[,] Cells { get; }
}