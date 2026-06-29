using Chess.Interfaces;

namespace Chess.Models;
public class Cell : ICell
{
    public Position Position { get; }
    public IPiece? OccupiedPiece { get; set; }
    public bool IsOccupied => OccupiedPiece != null;

    public Cell(Position position, IPiece? occupiedPiece, bool isOccupied)
    {
        Position = position;
        OccupiedPiece = occupiedPiece;
    }
}