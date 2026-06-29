using Chess.Models;

namespace Chess;

public static class AlgebraicNotation
{
    public static Position? Parse(string input)
    {
        if (string.IsNullOrWhiteSpace(input) || input.Length != 2)
        {
            return null;
        }

        var file = char.ToLowerInvariant(input[0]);
        var rankChar = input[1];

        if (file < 'a' || file > 'h' || rankChar < '1' || rankChar > '8')
        {
            return null;
        }

        var column = file - 'a';
        var rank = rankChar - '0';
        var row = 8 - rank;

        return new Position(row, column);
    }

    public static string ToAlgebraic(Position position)
    {
        var file = (char)('a' + position.Column);
        var rank = 8 - position.Row;
        return $"{file}{rank}";
    }
}