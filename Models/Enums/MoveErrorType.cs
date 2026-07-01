namespace Chess.Models.Enums;

// Categorizes why a move was rejected, so the UI can show a matching panel/icon
public enum MoveErrorType
{
    GameNotInProgress,
    OffBoard,
    NoPiece,
    OpponentPiece,
    IllegalMove,
}