using Chess.Interfaces;
using Spectre.Console;
using SpectreColor = Spectre.Console.Color;
using PieceColor = Chess.Models.Enums.Color;
using Chess.Models.Enums;
using System.Text;

namespace Chess.UI;

// Renders the chess board inside a bordered panel using Spectre.Console
public static class BoardRenderer
{
    // Square background colors (classic chess board palette)
    private static readonly SpectreColor LightSquare = new SpectreColor(240, 217, 181); // Cream
    private static readonly SpectreColor DarkSquare  = new SpectreColor(181, 136, 99);  // Brown

    // Piece text colors
    private static readonly SpectreColor WhiteText = new SpectreColor(255, 255, 255);
    private static readonly SpectreColor BlackText  = new SpectreColor(20, 20, 20);

    // Border color for the board panel
    private static readonly SpectreColor BorderColor = new SpectreColor(101, 67, 33); // Dark wood

    public static void Print(IBoard board, string whiteName, string blackName)
    {
        // Build the entire board as a markup string first
        var boardMarkup = new StringBuilder();

        // Black player label at the top
        boardMarkup.AppendLine($"  [bold grey]{Escape(blackName)} (Black)[/]");
        boardMarkup.AppendLine();

        for (int row = 0; row < 8; row++)
        {
            int rank = 8 - row;

            // Rank number on the left
            boardMarkup.Append($"[grey]{rank}[/] ");

            for (int col = 0; col < 8; col++)
            {
                bool isLight = (row + col) % 2 == 0;
                SpectreColor bg = isLight ? LightSquare : DarkSquare;

                IPiece? piece = board.Cells[row, col].OccupiedPiece;

                if (piece == null)
                {
                    // Empty square — fill with spaces in the square color
                    boardMarkup.Append($"[{ToHex(bg)} on {ToHex(bg)}]   [/]");
                }
                else
                {
                    string symbol = ToSymbol(piece);
                    SpectreColor fg = piece.Color == PieceColor.White ? WhiteText : BlackText;
                    boardMarkup.Append($"[bold {ToHex(fg)} on {ToHex(bg)}] {symbol} [/]");
                }
            }

            boardMarkup.AppendLine();
        }

        // File letters at the bottom
        boardMarkup.AppendLine();
        boardMarkup.Append("[grey]   a  b  c  d  e  f  g  h [/]");
        boardMarkup.AppendLine();
        boardMarkup.AppendLine();

        // White player label at the bottom
        boardMarkup.AppendLine($"  [bold white]{Escape(whiteName)} (White)[/]");

        // Wrap everything in a panel with a clear border
        var panel = new Panel(boardMarkup.ToString())
            .Header("[bold yellow] ♟  Chess Board [/]", Justify.Center)
            .BorderColor(BorderColor)
            .Border(BoxBorder.Heavy)
            .Padding(1, 0);

        // Center the panel on screen
        AnsiConsole.Write(new Padder(panel).PadLeft(2));
        AnsiConsole.WriteLine();
    }

    // Converts a piece to its unicode chess symbol
    private static string ToSymbol(IPiece piece)
    {
        if (piece.Color == PieceColor.Black)
        {
            if (piece.Type == PieceType.King)   return "♚";
            if (piece.Type == PieceType.Queen)  return "♛";
            if (piece.Type == PieceType.Rook)   return "♜";
            if (piece.Type == PieceType.Bishop) return "♝";
            if (piece.Type == PieceType.Knight) return "♞";
            if (piece.Type == PieceType.Pawn)   return "♟";
        }
        else
        {
            if (piece.Type == PieceType.King)   return "♔";
            if (piece.Type == PieceType.Queen)  return "♕";
            if (piece.Type == PieceType.Rook)   return "♖";
            if (piece.Type == PieceType.Bishop) return "♗";
            if (piece.Type == PieceType.Knight) return "♘";
            if (piece.Type == PieceType.Pawn)   return "♙";
        }
        return "?";
    }

    // Converts a Spectre Color to a hex string for inline markup (e.g. #f0d9b5)
    private static string ToHex(SpectreColor color)
    {
        return $"#{color.R:X2}{color.G:X2}{color.B:X2}";
    }

    // Escapes player names so square brackets don't break Spectre markup
    private static string Escape(string text)
    {
        return text.Replace("[", "[[").Replace("]", "]]");
    }
}
