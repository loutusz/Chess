using Chess.Models.Enums;

namespace Chess.Interfaces;

public interface IPlayer
{
    string Name { get; }
    Color Color { get; }
}