using Chess.Models.Enums;

namespace Chess.Interfaces;

public interface IPiece
{
    PieceType Type { get; set; }
    Color Color { get; }
    bool HasMoved { get; set; }

}