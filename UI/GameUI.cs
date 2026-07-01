using Chess.Models.Enums;
using Chess.Services;
using Spectre.Console;
using SpectreColor = Spectre.Console.Color;

namespace Chess.UI;

// Handles all console display and user input for the chess game
public static class GameUI
{
    // ─── Setup ────────────────────────────────────────────────────────────────

    // Show the title screen and ask both players for their names
    public static (string whiteName, string blackName) AskPlayerNames()
    {
        AnsiConsole.Clear();

        // Large title banner
        AnsiConsole.Write(
            new FigletText("CHESS")
                .Centered()
                .Color(SpectreColor.Gold1));

        AnsiConsole.Write(
            new Rule("[grey]A Console Chess Game[/]")
                .RuleStyle(Style.Parse("grey"))
                .Centered());

        AnsiConsole.WriteLine();

        string whiteName = AnsiConsole.Ask<string>("[white]♟  Enter name for[/] [bold white]White[/] [white]Player:[/]");
        if (string.IsNullOrWhiteSpace(whiteName))
        {
            whiteName = "White Player";
        }

        string blackName = AnsiConsole.Ask<string>("[grey]♟  Enter name for[/] [bold grey]Black[/] [grey]Player:[/]");
        if (string.IsNullOrWhiteSpace(blackName))
        {
            blackName = "Black Player";
        }

        return (whiteName, blackName);
    }

    // ─── Rules ────────────────────────────────────────────────────────────────

    // Show the game rules inside a bordered panel
    public static void ShowRules()
    {
        AnsiConsole.WriteLine();

        var rulesPanel = new Panel(
            "[yellow]Enter moves like:[/] [green bold]e2 e4[/]\n" +
            "Type [red]resign[/] to resign the game.\n" +
            "Type [red]quit[/]   to exit the program.")
            .Header("[bold yellow] ⚙  How to Play [/]", Justify.Center)
            .BorderColor(SpectreColor.Yellow)
            .Border(BoxBorder.Rounded)
            .Padding(1, 0);

        AnsiConsole.Write(new Padder(rulesPanel).PadLeft(2));
        AnsiConsole.WriteLine();
    }

    // ─── Game Status ──────────────────────────────────────────────────────────

    // Show status messages (check, checkmate, draw, resign) inside a bordered panel
    public static void ShowStatus(ChessGameService game)
    {
        if (game.Status == GameStatus.Check)
        {
            ShowStatusPanel(
                $"⚠  {Escape(game.CurrentPlayer.Name)} is in [bold]CHECK![/]",
                SpectreColor.Red, BoxBorder.Heavy);
        }
        else if (game.Status == GameStatus.Checkmate)
        {
            ShowStatusPanel(
                $"♚  CHECKMATE — [bold]{Escape(game.CurrentPlayer.Name)}[/] has no legal moves.",
                SpectreColor.DarkRed, BoxBorder.Double);
        }
        else if (game.Status == GameStatus.Draw)
        {
            ShowStatusPanel(
                "⚖  DRAW — No legal moves left (stalemate).",
                SpectreColor.Yellow, BoxBorder.Double);
        }
        else if (game.Status == GameStatus.Resigned)
        {
            ShowStatusPanel(
                "🏳  Game ended by resignation.",
                SpectreColor.Yellow, BoxBorder.Rounded);
        }
    }

    // Show the game over message inside a green bordered panel
    public static void ShowGameOver()
    {
        AnsiConsole.WriteLine();

        var panel = new Panel("[bold green]  Game over. Thanks for playing!  [/]")
            .BorderColor(SpectreColor.Green)
            .Border(BoxBorder.Double)
            .Padding(1, 0);

        AnsiConsole.Write(new Padder(panel).PadLeft(2));
        AnsiConsole.WriteLine();
    }

    // ─── Move Input ───────────────────────────────────────────────────────────

    // Show the move prompt inside a styled bordered panel and ask for input
    public static string? AskMove(string playerName, string playerColor)
    {
        AnsiConsole.Write(
            new Rule($"[bold]{Escape(playerName)}[/] [grey]({playerColor})[/]")
                .RuleStyle(Style.Parse("grey dim"))
                .LeftJustified());

        return AnsiConsole.Ask<string>("[grey]  >[/] Your move:");
    }

    // Show a move error inside a panel styled to match the failure type
    public static void ShowError(string message, MoveErrorType type)
    {
        var (header, borderColor) = type switch
        {
            MoveErrorType.NoPiece         => ("[bold red] ⬚  No Piece There [/]", SpectreColor.Red),
            MoveErrorType.OpponentPiece   => ("[bold red] 🚫  Not Your Piece [/]", SpectreColor.Red),
            MoveErrorType.IllegalMove     => ("[bold red] ✗  Illegal Move [/]", SpectreColor.Red),
            MoveErrorType.OffBoard        => ("[bold red] ⚠  Off The Board [/]", SpectreColor.Red),
            MoveErrorType.GameNotInProgress => ("[bold red] ⏸  Game Not In Progress [/]", SpectreColor.Red),
            _ => ("[bold red] ✗  Invalid Move [/]", SpectreColor.Red),
        };

        ShowErrorPanel(message, header, borderColor);
    }

    // Fallback for plain-string errors (e.g. pawn promotion failures)
    public static void ShowError(string message)
    {
        ShowErrorPanel(message, "[bold red] ✗  Invalid Move [/]", SpectreColor.Red);
    }

    // Pause after an error panel so the next loop's screen Clear() doesn't wipe it instantly
    public static void PauseForError()
    {
        AnsiConsole.MarkupLine("[grey]  Press any key to continue...[/]");
        Console.ReadKey(true);
    }

    private static void ShowErrorPanel(string message, string header, SpectreColor borderColor)
    {
        AnsiConsole.WriteLine();

        var panel = new Panel($"[red]{Escape(message)}[/]")
            .Header(header, Justify.Left)
            .BorderColor(borderColor)
            .Border(BoxBorder.Rounded)
            .Padding(1, 0);

        AnsiConsole.Write(new Padder(panel).PadLeft(2));
        AnsiConsole.WriteLine();
    }

    // ─── Promotion ────────────────────────────────────────────────────────────

    // Show a pawn promotion selection menu using arrow keys
    public static PieceType AskPromotion()
    {
        AnsiConsole.WriteLine();

        var panel = new Panel(
            "[yellow]Your pawn has reached the other end of the board!\n[/]" +
            "[grey]Use arrow keys to select a piece.[/]")
            .Header("[bold yellow] ♛  Pawn Promotion [/]", Justify.Center)
            .BorderColor(SpectreColor.Yellow)
            .Border(BoxBorder.Heavy)
            .Padding(1, 0);

        AnsiConsole.Write(new Padder(panel).PadLeft(2));

        string choice = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("[grey]  Choose your piece:[/]")
                .HighlightStyle(Style.Parse("bold yellow"))
                .AddChoices("♛ Queen", "♜ Rook", "♝ Bishop", "♞ Knight"));

        // Extract the piece name from the displayed choice (e.g. "♛ Queen" → "Queen")
        string pieceName = choice.Split(' ')[1];

        if (Enum.TryParse<PieceType>(pieceName, out PieceType newType))
        {
            return newType;
        }

        return PieceType.Queen;
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    // Show a single-line status inside a panel with a given border color
    private static void ShowStatusPanel(string message, SpectreColor borderColor, BoxBorder border)
    {
        AnsiConsole.WriteLine();

        var panel = new Panel(message)
            .BorderColor(borderColor)
            .Border(border)
            .Padding(1, 0);

        AnsiConsole.Write(new Padder(panel).PadLeft(2));
        AnsiConsole.WriteLine();
    }

    // Escapes player names so square brackets in names don't break Spectre markup
    private static string Escape(string text)
    {
        return text.Replace("[", "[[").Replace("]", "]]");
    }
}