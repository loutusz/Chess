using Chess.Models;

namespace Chess.Services.Events;

public class PieceMovedEventArgs : EventArgs
{
    public Position FromPosition { get; }
    public Position ToPosition { get; }
    public Piece MovedPiece { get; }
    public Piece? CapturedPiece { get; }

    public PieceMovedEventArgs(Position fromPosition, Position toPosition, Piece movedPiece, Piece? capturedPiece)
    {
        FromPosition = fromPosition;
        ToPosition = toPosition;
        MovedPiece = movedPiece;
        CapturedPiece = capturedPiece;
    }
}