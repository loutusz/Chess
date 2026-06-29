using Chess.Models.Enums;

namespace Chess.Services.Events;

public class DrawEventArgs : EventArgs
{
    public GameStatus Reason { get; }
    public DrawEventArgs(GameStatus reason)
    {
        Reason = reason;
    }


}