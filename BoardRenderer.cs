using Chess.Interfaces;
using Chess.Models.Enums;

namespace Chess;

public static class BoardRenderer
{
    public static void Print(IBoard board)
    {
        Console.WriteLine();

        for (var row = 0; row < 8; row++)
        {
            var rank = 8 - row;
            Console.Write($"{rank} ");

            for (var col = 0; col < 8; col++)
            {
                var piece = board.Cells[row, col].OccupiedPiece;
                Console.Write(piece == null ? ". " : $"{ToSymbol(piece)} ");
            }

            Console.WriteLine();
        }

        Console.WriteLine("  a b c d e f g h");
        Console.WriteLine();
    }

    private static string ToSymbol(IPiece piece)
    {
        return (piece.Type, piece.Color) switch
        {
            (PieceType.King, Color.White) => "♚",
            (PieceType.Queen, Color.White) => "♛",
            (PieceType.Rook, Color.White) => "♜",
            (PieceType.Bishop, Color.White) => "♝",
            (PieceType.Knight, Color.White) => "♞",
            (PieceType.Pawn, Color.White) => "♟",
            (PieceType.King, Color.Black) => "♔",
            (PieceType.Queen, Color.Black) => "♕",
            (PieceType.Rook, Color.Black) => "♖",
            (PieceType.Bishop, Color.Black) => "♗",
            (PieceType.Knight, Color.Black) => "♘",
            (PieceType.Pawn, Color.Black) => "♙",
            _ => "?"
        };
    }
}