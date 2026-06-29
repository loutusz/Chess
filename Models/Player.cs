using Chess.Interfaces;
using Chess.Models.Enums;

namespace Chess.Models;

public class Player : IPlayer
{
    public string Name { get; set; }
    public Color Color { get; set; }

    public Player(string name, Color color)
    {
        Name = name;
        Color = color;
    }
}