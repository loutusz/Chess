using Chess.Interfaces;
using Chess.Models;
using Chess.Models.Enums;
using Chess.Services;

namespace Chess;

public static class Program
{
    public static void Main()
    {
        Console.WriteLine("========= Console Chess =========");
        Console.Write("Enter name for White Player: ");

        var whiteName = Console.ReadLine();
        if (string.IsNullOrWhiteSpace(whiteName))
        {
            whiteName = "White Player";
        }
        
        Console.Write("Enter name for Black Player: ");

        var blackName = Console.ReadLine();
        if (string.IsNullOrWhiteSpace(blackName))
        {
            blackName = "Black Player";
        }

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
        Console.WriteLine("============= RULES =============");
        Console.WriteLine("Enter moves like: e2 e4");
        Console.WriteLine("Type 'resign' to resign.");
        Console.WriteLine("Type 'quit' to exit.");
        Console.WriteLine("=================================");

        RunGameLoop(game);
    }

    private static void RunGameLoop(ChessGameService game)
    {
        while (true)
        {
            BoardRenderer.Print(game.GetBoard());
            PrintStatusLine(game);

            if (IsGameOver(game.Status))
            {
                Console.WriteLine("Game over. Thanks for playing!");
                return;
            }

            Console.Write($"{game.CurrentPlayer.Name} ({game.CurrentPlayer.Color}), enter your move: ");
            var input = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(input))
            {
                continue;
            }

            if (input.Trim().Equals("quit", StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine("Exiting.");
                return;
            }

            if (input.Trim().Equals("resign", StringComparison.OrdinalIgnoreCase))
            {
                game.ResignGame(game.CurrentPlayer);
                continue;
            }

            HandleMoveInput(game, input);
        }
    }

    private static void HandleMoveInput(ChessGameService game, string input)
    {
        var parts = input.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length != 2)
        {
            Console.WriteLine("Please enter a move as two squares, e.g. 'e2 e4'.");
            return;
        }

        var from = AlgebraicNotation.Parse(parts[0]);
        var to = AlgebraicNotation.Parse(parts[1]);

        if (from == null || to == null)
        {
            Console.WriteLine("Invalid square. Use file a-h and rank 1-8, e.g. 'e2'.");
            return;
        }

        try
        {
            game.MakeMove(from.Value, to.Value);

            if (IsPromotionPending(game, to.Value))
            {
                PromptForPromotion(game, to.Value);
            }
        }
        catch (InvalidOperationException ex)
        {
            Console.WriteLine($"Illegal move: {ex.Message}");
        }
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

    private static void PromptForPromotion(ChessGameService game, Position pawnPosition)
    {
        while (true)
        {
            Console.Write("Pawn promotion! Choose Queen, Rook, Bishop, or Knight: ");
            var choice = Console.ReadLine();

            if (Enum.TryParse<PieceType>(choice?.Trim(), ignoreCase: true, out var newType)
                && newType is PieceType.Queen or PieceType.Rook or PieceType.Bishop or PieceType.Knight)
            {
                game.PromotePawn(pawnPosition, newType);
                return;
            }

            Console.WriteLine("Please type one of: Queen, Rook, Bishop, Knight.");
        }
    }

    private static void PrintStatusLine(ChessGameService game)
    {
        switch (game.Status)
        {
            case GameStatus.Check:
                Console.WriteLine($">>> {game.CurrentPlayer.Name} is in CHECK! <<<");
                break;
            case GameStatus.Checkmate:
                Console.WriteLine($">>> CHECKMATE - {game.CurrentPlayer.Name} has no legal moves. <<<");
                break;
            case GameStatus.Draw:
                Console.WriteLine(">>> DRAW (stalemate). <<<");
                break;
            case GameStatus.Resigned:
                Console.WriteLine(">>> Game ended by resignation. <<<");
                break;
        }
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