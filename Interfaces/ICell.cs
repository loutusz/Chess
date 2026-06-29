using Chess.Models;
using Chess.Models.Enums;

namespace Chess.Interfaces;

public interface ICell
{
    Position Position { get; }
    IPiece? OccupiedPiece { get; set; }
    bool IsOccupied { get; }
}