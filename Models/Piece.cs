using Chess.Interfaces;
using Chess.Models.Enums;

namespace Chess.Models;

public class Piece : IPiece
{
    public PieceType Type { get; set; }
    public Color Color { get; set; }
    public bool HasMoved { get; set; }

    public Piece(PieceType type, Color color, bool hasMoved)
    {
        Type = type;
        Color = color;
        HasMoved = hasMoved;
    }
}