using Chess.Interfaces;
using Chess.Models;
using Chess.Models.Enums;
using Chess.Services;
using Chess.UI;

namespace Chess;

public static class Program
{
    public static void Main()
    {
        // Ask both players for their names
        var (whiteName, blackName) = GameUI.AskPlayerNames();

        var whitePlayer = new Player(whiteName, Color.White);
        var blackPlayer = new Player(blackName, Color.Black);

        var players = new Dictionary<IPlayer, Color>
        {
            { whitePlayer, Color.White },
            { blackPlayer, Color.Black }
        };

        var emptyBoard = CreateEmptyBoard();
        var game = new ChessGameService(emptyBoard, players, GameStatus.NotStarted);

        game.StartGame();

        GameUI.ShowRules();

        RunGameLoop(game, whiteName, blackName);
    }

    private static void RunGameLoop(ChessGameService game, string whiteName, string blackName)
    {
        while (true)
        {
            // Clear the screen before each turn so the layout stays clean
            Spectre.Console.AnsiConsole.Clear();

            // Draw the board and show any status messages
            BoardRenderer.Print(game.GetBoard(), whiteName, blackName);
            GameUI.ShowStatus(game);

            // Check if the game is over
            if (IsGameOver(game.Status))
            {
                GameUI.ShowGameOver();
                return;
            }

            // Ask the current player for their move
            string? input = GameUI.AskMove(game.CurrentPlayer.Name, game.CurrentPlayer.Color.ToString());

            if (string.IsNullOrWhiteSpace(input))
            {
                continue;
            }

            if (input.Trim().Equals("quit", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            if (input.Trim().Equals("resign", StringComparison.OrdinalIgnoreCase))
            {
                game.ResignGame(game.CurrentPlayer);
                continue;
            }

            bool hadError = HandleMoveInput(game, input);
            if (hadError)
            {
                GameUI.PauseForError();
            }
        }
    }

    private static bool HandleMoveInput(ChessGameService game, string input)
    {
        var parts = input.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length != 2)
        {
            GameUI.ShowError("Please enter a move as two squares, e.g. 'e2 e4'.");
            return true;
        }

        var from = AlgebraicNotation.Parse(parts[0]);
        var to   = AlgebraicNotation.Parse(parts[1]);

        if (from == null || to == null)
        {
            GameUI.ShowError("Invalid square. Use file a-h and rank 1-8, e.g. 'e2'.");
            return true;
        }

        (string Message, MoveErrorType Type)? error = game.MakeMove(from.Value, to.Value);
        if (error != null)
        {
            GameUI.ShowError(error.Value.Message, error.Value.Type);
            return true;
        }

        if (IsPromotionPending(game, to.Value))
        {
            return HandlePromotion(game, to.Value);
        }

        return false;
    }

    private static bool IsPromotionPending(ChessGameService game, Position to)
    {
        var piece = game.GetCell(to).OccupiedPiece;
        if (piece == null || piece.Type != PieceType.Pawn)
        {
            return false;
        }

        return piece.Color == Color.White ? to.Row == 0 : to.Row == 7;
    }

    private static bool HandlePromotion(ChessGameService game, Position pawnPosition)
    {
        PieceType newType = GameUI.AskPromotion();

        string? error = game.PromotePawn(pawnPosition, newType);
        if (error != null)
        {
            GameUI.ShowError(error);
            return true;
        }

        return false;
    }

    private static bool IsGameOver(GameStatus status)
    {
        return status is GameStatus.Checkmate or GameStatus.Draw or GameStatus.Resigned;
    }

    private static IBoard CreateEmptyBoard()
    {
        var cells = new ICell[8, 8];

        for (var row = 0; row < 8; row++)
        {
            for (var col = 0; col < 8; col++)
            {
                cells[row, col] = new Cell(new Position(row, col), null, false);
            }
        }

        return new Board(cells);
    }
}