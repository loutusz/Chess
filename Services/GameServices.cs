using Chess.Interfaces;
using Chess.Models;
using Chess.Models.Enums;
using Chess.Services.Events;

namespace Chess.Services;

public class ChessGameService
{
    private IBoard _board;
    private readonly Dictionary<IPlayer, Color> _players;
    private GameStatus _status;

    public IPlayer CurrentPlayer { get; private set; }
    public GameStatus Status => _status;

    public event EventHandler<PieceMovedEventArgs>? OnPieceMoved;
    public event EventHandler<KingThreatEventArgs>? OnKingChecked;
    public event EventHandler<KingThreatEventArgs>? OnKingCheckmated;
    public event EventHandler<DrawEventArgs>? OnDraw;

    // Helper class to store initial piece positions
    private class StartingPieceConfig
    {
        public string Square { get; }
        public PieceType Type { get; }
        public Color Color { get; }

        public StartingPieceConfig(string square, PieceType type, Color color)
        {
            Square = square;
            Type = type;
            Color = color;
        }
    }

    // List of all 32 starting chess pieces
    private static readonly List<StartingPieceConfig> StartingPieces = new List<StartingPieceConfig>
    {
        // White Pieces
        new StartingPieceConfig("a1", PieceType.Rook, Color.White),
        new StartingPieceConfig("b1", PieceType.Knight, Color.White),
        new StartingPieceConfig("c1", PieceType.Bishop, Color.White),
        new StartingPieceConfig("d1", PieceType.Queen, Color.White),
        new StartingPieceConfig("e1", PieceType.King, Color.White),
        new StartingPieceConfig("f1", PieceType.Bishop, Color.White),
        new StartingPieceConfig("g1", PieceType.Knight, Color.White),
        new StartingPieceConfig("h1", PieceType.Rook, Color.White),
        new StartingPieceConfig("a2", PieceType.Pawn, Color.White),
        new StartingPieceConfig("b2", PieceType.Pawn, Color.White),
        new StartingPieceConfig("c2", PieceType.Pawn, Color.White),
        new StartingPieceConfig("d2", PieceType.Pawn, Color.White),
        new StartingPieceConfig("e2", PieceType.Pawn, Color.White),
        new StartingPieceConfig("f2", PieceType.Pawn, Color.White),
        new StartingPieceConfig("g2", PieceType.Pawn, Color.White),
        new StartingPieceConfig("h2", PieceType.Pawn, Color.White),

        // Black Pieces
        new StartingPieceConfig("a7", PieceType.Pawn, Color.Black),
        new StartingPieceConfig("b7", PieceType.Pawn, Color.Black),
        new StartingPieceConfig("c7", PieceType.Pawn, Color.Black),
        new StartingPieceConfig("d7", PieceType.Pawn, Color.Black),
        new StartingPieceConfig("e7", PieceType.Pawn, Color.Black),
        new StartingPieceConfig("f7", PieceType.Pawn, Color.Black),
        new StartingPieceConfig("g7", PieceType.Pawn, Color.Black),
        new StartingPieceConfig("h7", PieceType.Pawn, Color.Black),
        new StartingPieceConfig("a8", PieceType.Rook, Color.Black),
        new StartingPieceConfig("b8", PieceType.Knight, Color.Black),
        new StartingPieceConfig("c8", PieceType.Bishop, Color.Black),
        new StartingPieceConfig("d8", PieceType.Queen, Color.Black),
        new StartingPieceConfig("e8", PieceType.King, Color.Black),
        new StartingPieceConfig("f8", PieceType.Bishop, Color.Black),
        new StartingPieceConfig("g8", PieceType.Knight, Color.Black),
        new StartingPieceConfig("h8", PieceType.Rook, Color.Black)
    };

    public ChessGameService(IBoard board, Dictionary<IPlayer, Color> players, GameStatus status)
    {
        _board = board;
        _players = players;
        _status = status;

        CurrentPlayer = GetWhitePlayer();
    }

    public void StartGame()
    {
        _board = CreateStandardBoard();
        _status = GameStatus.InProgress;
        CurrentPlayer = GetWhitePlayer();
    }

    public void ResignGame(IPlayer player)
    {
        _status = GameStatus.Resigned;
    }

    public IPlayer GetWhitePlayer()
    {
        foreach (KeyValuePair<IPlayer, Color> entry in _players)
        {
            if (entry.Value == Color.White)
            {
                return entry.Key;
            }
        }
        foreach (IPlayer player in _players.Keys)
        {
            return player;
        }
        return new Player("White Player", Color.White);
    }

    public IPlayer GetBlackPlayer()
    {
        foreach (KeyValuePair<IPlayer, Color> entry in _players)
        {
            if (entry.Value == Color.Black)
            {
                return entry.Key;
            }
        }
        foreach (IPlayer player in _players.Keys)
        {
            return player;
        }
        return new Player("Black Player", Color.Black);
    }

    public IBoard GetBoard()
    {
        return _board;
    }

    public ICell GetCell(Position position)
    {
        return _board.Cells[position.Row, position.Column];
    }

    public List<Position> GetLegalMoves(Position position)
    {
        ICell cell = GetCell(position);
        IPiece? piece = cell.OccupiedPiece;
        if (piece == null)
        {
            return new List<Position>();
        }

        List<Position> pseudoMoves = GetPseudoLegalMoves(position, piece);
        List<Position> legalMoves = new List<Position>();

        foreach (Position to in pseudoMoves)
        {
            if (!WouldLeaveKingInCheck(position, to, piece.Color))
            {
                legalMoves.Add(to);
            }
        }

        return legalMoves;
    }

    private List<Position> GetPseudoLegalMoves(Position position, IPiece piece)
    {
        if (piece.Type == PieceType.Pawn)
        {
            return GetPawnMoves(position);
        }
        if (piece.Type == PieceType.Knight)
        {
            return GetKnightMoves(position);
        }
        if (piece.Type == PieceType.Bishop)
        {
            return GetBishopMoves(position);
        }
        if (piece.Type == PieceType.Rook)
        {
            return GetRookMoves(position);
        }
        if (piece.Type == PieceType.Queen)
        {
            return GetQueenMoves(position);
        }
        if (piece.Type == PieceType.King)
        {
            return GetKingMoves(position);
        }
        return new List<Position>();
    }

    public List<Position> GetPawnMoves(Position position)
    {
        List<Position> moves = new List<Position>();
        ICell cell = GetCell(position);
        IPiece? piece = cell.OccupiedPiece;
        if (piece == null)
        {
            return moves;
        }

        int direction = (piece.Color == Color.White) ? -1 : 1;
        int startRow = (piece.Color == Color.White) ? 6 : 1;

        // Move 1 square forward
        Position oneForward = new Position(position.Row + direction, position.Column);
        if (IsOnBoard(oneForward))
        {
            ICell targetCell = GetCell(oneForward);
            if (!targetCell.IsOccupied)
            {
                moves.Add(oneForward);

                // Move 2 squares forward from the start row
                Position twoForward = new Position(position.Row + (direction * 2), position.Column);
                if (position.Row == startRow && IsOnBoard(twoForward))
                {
                    ICell targetCell2 = GetCell(twoForward);
                    if (!targetCell2.IsOccupied)
                    {
                        moves.Add(twoForward);
                    }
                }
            }
        }

        // Capture diagonally
        int[] colOffsets = { -1, 1 };
        foreach (int offset in colOffsets)
        {
            Position diagonal = new Position(position.Row + direction, position.Column + offset);
            if (IsOnBoard(diagonal))
            {
                ICell targetCell = GetCell(diagonal);
                if (targetCell.IsOccupied && targetCell.OccupiedPiece!.Color != piece.Color)
                {
                    moves.Add(diagonal);
                }
            }
        }

        return moves;
    }

    public List<Position> GetKnightMoves(Position position)
    {
        List<Position> moves = new List<Position>();
        ICell cell = GetCell(position);
        IPiece? piece = cell.OccupiedPiece;
        if (piece == null)
        {
            return moves;
        }

        int[] rowOffsets = { -2, -2, -1, -1, 1, 1, 2, 2 };
        int[] colOffsets = { -1, 1, -2, 2, -2, 2, -1, 1 };

        for (int i = 0; i < 8; i++)
        {
            Position target = new Position(position.Row + rowOffsets[i], position.Column + colOffsets[i]);
            if (IsOnBoard(target))
            {
                ICell targetCell = GetCell(target);
                if (!targetCell.IsOccupied || targetCell.OccupiedPiece!.Color != piece.Color)
                {
                    moves.Add(target);
                }
            }
        }

        return moves;
    }

    public List<Position> GetBishopMoves(Position position)
    {
        List<Position> moves = new List<Position>();
        ICell cell = GetCell(position);
        IPiece? piece = cell.OccupiedPiece;
        if (piece == null)
        {
            return moves;
        }

        // Up-Left
        int row = position.Row - 1;
        int col = position.Column - 1;
        while (row >= 0 && col >= 0)
        {
            Position target = new Position(row, col);
            if (AddSlidingMove(moves, target, piece.Color))
            {
                break;
            }
            row--;
            col--;
        }

        // Up-Right
        row = position.Row - 1;
        col = position.Column + 1;
        while (row >= 0 && col < 8)
        {
            Position target = new Position(row, col);
            if (AddSlidingMove(moves, target, piece.Color))
            {
                break;
            }
            row--;
            col++;
        }

        // Down-Left
        row = position.Row + 1;
        col = position.Column - 1;
        while (row < 8 && col >= 0)
        {
            Position target = new Position(row, col);
            if (AddSlidingMove(moves, target, piece.Color))
            {
                break;
            }
            row++;
            col--;
        }

        // Down-Right
        row = position.Row + 1;
        col = position.Column + 1;
        while (row < 8 && col < 8)
        {
            Position target = new Position(row, col);
            if (AddSlidingMove(moves, target, piece.Color))
            {
                break;
            }
            row++;
            col++;
        }

        return moves;
    }

    public List<Position> GetRookMoves(Position position)
    {
        List<Position> moves = new List<Position>();
        ICell cell = GetCell(position);
        IPiece? piece = cell.OccupiedPiece;
        if (piece == null)
        {
            return moves;
        }

        // Up
        for (int r = position.Row - 1; r >= 0; r--)
        {
            Position target = new Position(r, position.Column);
            if (AddSlidingMove(moves, target, piece.Color))
            {
                break;
            }
        }
        // Down
        for (int r = position.Row + 1; r < 8; r++)
        {
            Position target = new Position(r, position.Column);
            if (AddSlidingMove(moves, target, piece.Color))
            {
                break;
            }
        }
        // Left
        for (int col = position.Column - 1; col >= 0; col--)
        {
            Position target = new Position(position.Row, col);
            if (AddSlidingMove(moves, target, piece.Color))
            {
                break;
            }
        }
        // Right
        for (int col = position.Column + 1; col < 8; col++)
        {
            Position target = new Position(position.Row, col);
            if (AddSlidingMove(moves, target, piece.Color))
            {
                break;
            }
        }

        return moves;
    }

    public List<Position> GetQueenMoves(Position position)
    {
        List<Position> moves = new List<Position>();
        
        List<Position> rookMoves = GetRookMoves(position);
        foreach (Position move in rookMoves)
        {
            moves.Add(move);
        }

        List<Position> bishopMoves = GetBishopMoves(position);
        foreach (Position move in bishopMoves)
        {
            moves.Add(move);
        }

        return moves;
    }

    public List<Position> GetKingMoves(Position position)
    {
        List<Position> moves = new List<Position>();
        ICell cell = GetCell(position);
        IPiece? piece = cell.OccupiedPiece;
        if (piece == null)
        {
            return moves;
        }

        int[] rowOffsets = { -1, -1, -1, 0, 0, 1, 1, 1 };
        int[] colOffsets = { -1, 0, 1, -1, 1, -1, 0, 1 };

        for (int i = 0; i < 8; i++)
        {
            Position target = new Position(position.Row + rowOffsets[i], position.Column + colOffsets[i]);
            if (IsOnBoard(target))
            {
                ICell targetCell = GetCell(target);
                if (!targetCell.IsOccupied || targetCell.OccupiedPiece!.Color != piece.Color)
                {
                    moves.Add(target);
                }
            }
        }

        return moves;
    }

    private bool AddSlidingMove(List<Position> moves, Position target, Color movingColor)
    {
        ICell cell = GetCell(target);
        if (!cell.IsOccupied)
        {
            moves.Add(target);
            return false; // Square is empty, keep checking in this direction
        }

        if (cell.OccupiedPiece!.Color != movingColor)
        {
            moves.Add(target);
        }
        return true; // Blocked by a piece, stop checking in this direction
    }

    // Returns null if the move is legal, otherwise the reason + a category
    // the UI can use to pick a matching panel header/icon.
    public (string Message, MoveErrorType Type)? ValidateMove(Position from, Position to)
    {
        if (!IsOnBoard(from) || !IsOnBoard(to))
        {
            return ("That square is off the board.", MoveErrorType.OffBoard);
        }

        ICell cell = GetCell(from);
        IPiece? piece = cell.OccupiedPiece;

        if (piece == null)
        {
            return ("There's no piece on that square.", MoveErrorType.NoPiece);
        }

        if (piece.Color != CurrentPlayer.Color)
        {
            return ("You can't move your opponent's piece.", MoveErrorType.OpponentPiece);
        }

        List<Position> legalMoves = GetLegalMoves(from);
        foreach (Position move in legalMoves)
        {
            if (move.Row == to.Row && move.Column == to.Column)
            {
                return null;
            }
        }

        return ($"That {piece.Type} can't move there.", MoveErrorType.IllegalMove);
    }

    public void ExecuteMove(Position from, Position to)
    {
        ICell fromCell = GetCell(from);
        ICell toCell = GetCell(to);

        IPiece movingPiece = fromCell.OccupiedPiece!;
        IPiece? capturedPiece = toCell.OccupiedPiece;

        toCell.OccupiedPiece = movingPiece;
        fromCell.OccupiedPiece = null;
        movingPiece.HasMoved = true;

        OnPieceMoved?.Invoke(this, new PieceMovedEventArgs(
            from, to, (Piece)movingPiece, (Piece?)capturedPiece));
    }

    private void SwitchTurn()
    {
        if (CurrentPlayer.Color == Color.White)
        {
            CurrentPlayer = GetBlackPlayer();
        }
        else
        {
            CurrentPlayer = GetWhitePlayer();
        }
    }

    public (string Message, MoveErrorType Type)? MakeMove(Position from, Position to)
    {
        if (_status != GameStatus.InProgress && _status != GameStatus.Check)
        {
            return ("Cannot make a move - the game is not in progress.", MoveErrorType.GameNotInProgress);
        }

        (string Message, MoveErrorType Type)? validationError = ValidateMove(from, to);
        if (validationError != null)
        {
            return validationError;
        }

        ExecuteMove(from, to);
        SwitchTurn();
        UpdateGameStatus();
        return null;
    }

    public string? PromotePawn(Position position, PieceType newType)
    {
        ICell cell = GetCell(position);
        IPiece? piece = cell.OccupiedPiece;
        if (piece == null)
        {
            return "No piece at this position to promote.";
        }

        if (piece.Type != PieceType.Pawn)
        {
            return "Only pawns can be promoted.";
        }

        bool isPromotionSquare = (piece.Color == Color.White) ? (position.Row == 0) : (position.Row == 7);
        if (!isPromotionSquare)
        {
            return "Pawn is not on a promotion square.";
        }

        if (newType == PieceType.Pawn || newType == PieceType.King)
        {
            return "A pawn cannot be promoted to a Pawn or King.";
        }

        piece.Type = newType;

        UpdateGameStatus();
        return null;
    }

    private void UpdateGameStatus()
    {
        if (_status == GameStatus.Resigned)
        {
            return;
        }

        bool inCheck = IsKingInCheck(CurrentPlayer.Color);

        if (inCheck && IsCheckmate(CurrentPlayer.Color))
        {
            _status = GameStatus.Checkmate;
            RaiseKingThreatEvent(OnKingCheckmated);
        }
        else if (inCheck)
        {
            _status = GameStatus.Check;
            RaiseKingThreatEvent(OnKingChecked);
        }
        else if (IsDraw())
        {
            _status = GameStatus.Draw;
            OnDraw?.Invoke(this, new DrawEventArgs(GameStatus.Draw));
        }
        else
        {
            _status = GameStatus.InProgress;
        }
    }

    private void RaiseKingThreatEvent(EventHandler<KingThreatEventArgs>? handler)
    {
        Position kingPosition = FindKing(CurrentPlayer.Color);
        List<Piece> attackers = FindAttackingPieces(kingPosition, OppositeColor(CurrentPlayer.Color));
        handler?.Invoke(this, new KingThreatEventArgs((Player)CurrentPlayer, kingPosition, attackers));
    }

    public bool IsKingInCheck(Color color)
    {
        Position kingPos = FindKing(color);
        Color enemyColor = OppositeColor(color);

        for (var row = 0; row < 8; row++)
        {
            for (var col = 0; col < 8; col++)
            {
                Position pos = new Position(row, col);
                ICell cell = GetCell(pos);
                if (cell.IsOccupied && cell.OccupiedPiece!.Color == enemyColor)
                {
                    List<Position> enemyMoves = GetPseudoLegalMoves(pos, cell.OccupiedPiece);
                    foreach (Position move in enemyMoves)
                    {
                        if (move.Row == kingPos.Row && move.Column == kingPos.Column)
                        {
                            return true;
                        }
                    }
                }
            }
        }
        return false;
    }

    public bool IsCheckmate(Color color)
    {
        return IsKingInCheck(color) && !PlayerHasAnyLegalMove(color);
    }

    public bool IsDraw()
    {
        return !IsKingInCheck(CurrentPlayer.Color) && !PlayerHasAnyLegalMove(CurrentPlayer.Color);
    }

    private bool PlayerHasAnyLegalMove(Color color)
    {
        for (var row = 0; row < 8; row++)
        {
            for (var col = 0; col < 8; col++)
            {
                Position pos = new Position(row, col);
                ICell cell = GetCell(pos);
                if (cell.IsOccupied && cell.OccupiedPiece!.Color == color)
                {
                    List<Position> legalMoves = GetLegalMoves(pos);
                    if (legalMoves.Count > 0)
                    {
                        return true;
                    }
                }
            }
        }
        return false;
    }

    private bool WouldLeaveKingInCheck(Position from, Position to, Color movingColor)
    {
        ICell fromCell = GetCell(from);
        ICell toCell = GetCell(to);

        IPiece? originalFromPiece = fromCell.OccupiedPiece;
        IPiece? originalToPiece = toCell.OccupiedPiece;

        toCell.OccupiedPiece = originalFromPiece;
        fromCell.OccupiedPiece = null;

        bool leavesKingInCheck = IsKingInCheck(movingColor);

        fromCell.OccupiedPiece = originalFromPiece;
        toCell.OccupiedPiece = originalToPiece;

        return leavesKingInCheck;
    }

    private Position FindKing(Color color)
    {
        for (var row = 0; row < 8; row++)
        {
            for (var col = 0; col < 8; col++)
            {
                ICell cell = _board.Cells[row, col];
                if (cell.OccupiedPiece != null && cell.OccupiedPiece.Type == PieceType.King && cell.OccupiedPiece.Color == color)
                {
                    return cell.Position;
                }
            }
        }

        return new Position(0, 0);
    }

    private List<Piece> FindAttackingPieces(Position kingPosition, Color attackerColor)
    {
        List<Piece> attackers = new List<Piece>();

        for (var row = 0; row < 8; row++)
        {
            for (var col = 0; col < 8; col++)
            {
                Position pos = new Position(row, col);
                ICell cell = GetCell(pos);
                if (cell.OccupiedPiece != null && cell.OccupiedPiece.Color == attackerColor)
                {
                    List<Position> enemyMoves = GetPseudoLegalMoves(pos, cell.OccupiedPiece);
                    foreach (Position move in enemyMoves)
                    {
                        if (move.Row == kingPosition.Row && move.Column == kingPosition.Column)
                        {
                            attackers.Add((Piece)cell.OccupiedPiece);
                            break;
                        }
                    }
                }
            }
        }

        return attackers;
    }

    private static bool IsOnBoard(Position position)
    {
        return position.Row >= 0 && position.Row <= 7 && position.Column >= 0 && position.Column <= 7;
    }

    private static Color OppositeColor(Color color)
    {
        return (color == Color.White) ? Color.Black : Color.White;
    }

    private static IBoard CreateStandardBoard()
    {
        ICell[,] cells = new ICell[8, 8];

        // Initialize empty cells
        for (var row = 0; row < 8; row++)
        {
            for (var col = 0; col < 8; col++)
            {
                cells[row, col] = new Cell(new Position(row, col), null, false);
            }
        }

        // Place the 32 pieces using the flat list configuration
        foreach (StartingPieceConfig config in StartingPieces)
        {
            Position? pos = AlgebraicNotation.Parse(config.Square);
            if (pos.HasValue)
            {
                cells[pos.Value.Row, pos.Value.Column].OccupiedPiece = new Piece(config.Type, config.Color, false);
            }
        }

        return new Board(cells);
    }
}