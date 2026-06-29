using Chess.Models;

namespace Chess.Services.Events;

public class KingThreatEventArgs : EventArgs
{
    public Player TargetPlayer { get; }
    public Position KingPosition { get; }
    public List<Piece> AttackingPieces { get; }

    public KingThreatEventArgs(Player targetPlayer, Position kingPosition, List<Piece> attackingPieces)
    {
        TargetPlayer = targetPlayer;
        KingPosition = kingPosition;
        AttackingPieces = attackingPieces;
    }
}