using Chess.Interfaces;
using Chess.Models;
using Chess.Models.Enums;
using Chess.Services.Events;

namespace Chess.Services;

public class ChessGameService
{
    private IBoard _board;

    private Dictionary<IPlayer, Color> _players;

    private GameStatus _status;

    private (Position From, Position To, PieceType Type)? _lastMove;

    public IPlayer CurrentPlayer { get; private set; }
    public GameStatus Status => _status;

    public event EventHandler<PieceMovedEventArgs>? OnPieceMoved;
    public event EventHandler<KingThreatEventArgs>? OnKingChecked;
    public event EventHandler<KingThreatEventArgs>? OnKingCheckmated;
    public event EventHandler<DrawEventArgs>? OnDraw;

    public ChessGameService(IBoard board, Dictionary<IPlayer, Color> players, GameStatus status)
    {
        _board = board;
        _players = players;
        _status = status;

        CurrentPlayer = _players.First(p => p.Value == Color.White).Key;
    }

    public void StartGame()
    {
        _board = CreateStandardBoard();
        _status = GameStatus.InProgress;
        CurrentPlayer = _players.First(p => p.Value == Color.White).Key;
        _lastMove = null;
    }

    public void ResignGame(IPlayer player)
    {
        _status = GameStatus.Resigned;
    }

    public IPlayer GetWhitePlayer() => _players.First(p => p.Value == Color.White).Key;

    public IPlayer GetBlackPlayer() => _players.First(p => p.Value == Color.Black).Key;

    public IBoard GetBoard() => _board;

    public ICell GetCell(Position position) => _board.Cells[position.Row, position.Column];

    public List<Position> GetLegalMoves(Position position)
    {
        var piece = GetCell(position).OccupiedPiece;
        if (piece == null)
        {
            return new List<Position>();
        }

        return GetPseudoLegalMoves(position, piece)
            .Where(to => !WouldLeaveKingInCheck(position, to, piece.Color))
            .ToList();
    }

    private static readonly (int, int)[] DiagonalDirections = { (-1, -1), (-1, 1), (1, -1), (1, 1) };
    private static readonly (int, int)[] StraightDirections = { (-1, 0), (1, 0), (0, -1), (0, 1) };

    private static readonly (int, int)[] KnightOffsets =
    {
        (-2, -1), (-2, 1), (-1, -2), (-1, 2),
        (1, -2), (1, 2), (2, -1), (2, 1)
    };

    private static readonly (int, int)[] KingOffsets =
    {
        (-1, -1), (-1, 0), (-1, 1),
        (0, -1), (0, 1),
        (1, -1), (1, 0), (1, 1)
    };

    private List<Position> GetPseudoLegalMoves(Position position, IPiece piece, bool includeCastling = true)
    {
        return piece.Type switch
        {
            PieceType.Pawn => GetPawnMoves(position),
            PieceType.Knight => GetStepMoves(position, KnightOffsets),
            PieceType.Bishop => GetSlidingMoves(position, DiagonalDirections),
            PieceType.Rook => GetSlidingMoves(position, StraightDirections),
            PieceType.Queen => GetSlidingMoves(position, DiagonalDirections.Concat(StraightDirections).ToArray()),
            PieceType.King => GetKingMoves(position, includeCastling),
            _ => new List<Position>()
        };
    }

    public List<Position> GetPawnMoves(Position position)
    {
        var moves = new List<Position>();
        var piece = GetCell(position).OccupiedPiece!;

        var direction = piece.Color == Color.White ? -1 : 1;
        var startRow = piece.Color == Color.White ? 6 : 1;

        var oneForward = new Position(position.Row + direction, position.Column);
        if (IsOnBoard(oneForward) && !GetCell(oneForward).IsOccupied)
        {
            moves.Add(oneForward);

            var twoForward = new Position(position.Row + (direction * 2), position.Column);
            if (position.Row == startRow && IsOnBoard(twoForward) && !GetCell(twoForward).IsOccupied)
            {
                moves.Add(twoForward);
            }
        }

        foreach (var columnOffset in new[] { -1, 1 })
        {
            var diagonal = new Position(position.Row + direction, position.Column + columnOffset);
            if (!IsOnBoard(diagonal))
            {
                continue;
            }

            var diagonalCell = GetCell(diagonal);
            if (diagonalCell.IsOccupied && diagonalCell.OccupiedPiece!.Color != piece.Color)
            {
                moves.Add(diagonal);
            }
            else if (IsEnPassantCapture(position, diagonal, piece))
            {
                moves.Add(diagonal);
            }
        }

        return moves;
    }

    public List<Position> GetKnightMoves(Position position) => GetStepMoves(position, KnightOffsets);

    public List<Position> GetBishopMoves(Position position) => GetSlidingMoves(position, DiagonalDirections);

    public List<Position> GetRookMoves(Position position) => GetSlidingMoves(position, StraightDirections);

    public List<Position> GetQueenMoves(Position position) =>
        GetSlidingMoves(position, DiagonalDirections.Concat(StraightDirections).ToArray());

    public List<Position> GetKingMoves(Position position) => GetKingMoves(position, includeCastling: true);

    private List<Position> GetKingMoves(Position position, bool includeCastling)
    {
        var moves = GetStepMoves(position, KingOffsets);

        if (includeCastling)
        {
            var piece = GetCell(position).OccupiedPiece!;
            moves.AddRange(GetCastlingMoves(position, piece));
        }

        return moves;
    }

    private List<Position> GetStepMoves(Position position, (int RowOffset, int ColOffset)[] offsets)
    {
        var piece = GetCell(position).OccupiedPiece!;
        var moves = new List<Position>();

        foreach (var (rowOffset, colOffset) in offsets)
        {
            var target = new Position(position.Row + rowOffset, position.Column + colOffset);
            if (IsOnBoard(target) && CanLandOn(target, piece.Color))
            {
                moves.Add(target);
            }
        }

        return moves;
    }

    private List<Position> GetSlidingMoves(Position position, (int RowOffset, int ColOffset)[] directions)
    {
        var piece = GetCell(position).OccupiedPiece!;
        var moves = new List<Position>();

        foreach (var (rowOffset, colOffset) in directions)
        {
            var current = new Position(position.Row + rowOffset, position.Column + colOffset);

            while (IsOnBoard(current))
            {
                var cell = GetCell(current);
                if (!cell.IsOccupied)
                {
                    moves.Add(current);
                }
                else
                {
                    if (cell.OccupiedPiece!.Color != piece.Color)
                    {
                        moves.Add(current);
                    }
                    break;
                }

                current = new Position(current.Row + rowOffset, current.Column + colOffset);
            }
        }

        return moves;
    }

    private bool CanLandOn(Position position, Color movingColor)
    {
        var cell = GetCell(position);
        return !cell.IsOccupied || cell.OccupiedPiece!.Color != movingColor;
    }

    private bool IsEnPassantCapture(Position from, Position to, IPiece movingPawn)
    {
        if (_lastMove == null || _lastMove.Value.Type != PieceType.Pawn)
        {
            return false;
        }

        var (lastFrom, lastTo, _) = _lastMove.Value;

        if (Math.Abs(lastTo.Row - lastFrom.Row) != 2)
        {
            return false;
        }

        var passedOverRow = (lastFrom.Row + lastTo.Row) / 2;
        var expectedTarget = new Position(passedOverRow, lastTo.Column);

        if (!SamePosition(to, expectedTarget))
        {
            return false;
        }

        return from.Row == lastTo.Row && Math.Abs(from.Column - lastTo.Column) == 1;
    }

    private List<Position> GetCastlingMoves(Position kingPosition, IPiece king)
    {
        var moves = new List<Position>();

        if (king.HasMoved || IsSquareUnderAttack(kingPosition, OppositeColor(king.Color)))
        {
            return moves;
        }

        foreach (var rookColumn in new[] { 7, 0 })
        {
            var rookPosition = new Position(kingPosition.Row, rookColumn);
            var rookCell = GetCell(rookPosition);

            if (rookCell.OccupiedPiece is not { Type: PieceType.Rook, HasMoved: false } rook
                || rook.Color != king.Color)
            {
                continue;
            }

            var direction = rookColumn > kingPosition.Column ? 1 : -1;
            var pathIsClear = true;

            for (var col = kingPosition.Column + direction; col != rookColumn; col += direction)
            {
                if (GetCell(new Position(kingPosition.Row, col)).IsOccupied)
                {
                    pathIsClear = false;
                    break;
                }
            }

            if (!pathIsClear)
            {
                continue;
            }

            var oneStep = new Position(kingPosition.Row, kingPosition.Column + direction);
            var twoStep = new Position(kingPosition.Row, kingPosition.Column + (direction * 2));

            var enemyColor = OppositeColor(king.Color);
            if (!IsSquareUnderAttack(oneStep, enemyColor) && !IsSquareUnderAttack(twoStep, enemyColor))
            {
                moves.Add(twoStep);
            }
        }

        return moves;
    }

    public void SetPiece(Position position, IPiece? piece)
    {
        GetCell(position).OccupiedPiece = piece;
    }

    public void RemovePiece(Position position) => SetPiece(position, null);

    public bool IsPromotionSquare(IPiece piece, Position position) =>
        piece.Color == Color.White ? position.Row == 0 : position.Row == 7;

    public bool ValidateMove(Position from, Position to)
    {
        if (!IsOnBoard(from) || !IsOnBoard(to))
        {
            return false;
        }

        var piece = GetCell(from).OccupiedPiece;
        if (piece == null || piece.Color != CurrentPlayer.Color)
        {
            return false;
        }

        return GetLegalMoves(from).Any(p => SamePosition(p, to));
    }

    public void ExecuteMove(Position from, Position to)
    {
        var movingPiece = GetCell(from).OccupiedPiece!;
        IPiece? capturedPiece = GetCell(to).OccupiedPiece;

        var isEnPassant = movingPiece.Type == PieceType.Pawn
            && capturedPiece == null
            && Math.Abs(to.Column - from.Column) == 1;

        if (isEnPassant)
        {
            var capturedPawnPosition = new Position(from.Row, to.Column);
            capturedPiece = GetCell(capturedPawnPosition).OccupiedPiece;
            RemovePiece(capturedPawnPosition);
        }

        var isCastling = movingPiece.Type == PieceType.King && Math.Abs(to.Column - from.Column) == 2;
        if (isCastling)
        {
            var rookColumn = to.Column > from.Column ? 7 : 0;
            var newRookColumn = to.Column > from.Column ? to.Column - 1 : to.Column + 1;

            var rookFrom = new Position(from.Row, rookColumn);
            var rookTo = new Position(from.Row, newRookColumn);
            var rook = GetCell(rookFrom).OccupiedPiece!;

            RemovePiece(rookFrom);
            SetPiece(rookTo, rook);
            rook.HasMoved = true;
        }

        RemovePiece(from);
        SetPiece(to, movingPiece);
        movingPiece.HasMoved = true;

        _lastMove = (from, to, movingPiece.Type);

        OnPieceMoved?.Invoke(this, new PieceMovedEventArgs(
            from, to, (Piece)movingPiece, capturedPiece as Piece));
    }

    private void SwitchTurn()
    {
        CurrentPlayer = CurrentPlayer.Color == Color.White ? GetBlackPlayer() : GetWhitePlayer();
    }

    public void MakeMove(Position from, Position to)
    {
        if (_status != GameStatus.InProgress && _status != GameStatus.Check)
        {
            throw new InvalidOperationException("Cannot make a move - the game is not in progress.");
        }

        if (!ValidateMove(from, to))
        {
            throw new InvalidOperationException($"Move from {from} to {to} is not legal.");
        }

        ExecuteMove(from, to);
        SwitchTurn();
        UpdateGameStatus();
    }

    public void PromotePawn(Position position, PieceType newType)
    {
        var piece = GetCell(position).OccupiedPiece
            ?? throw new InvalidOperationException("No piece at this position to promote.");

        if (piece.Type != PieceType.Pawn)
        {
            throw new InvalidOperationException("Only pawns can be promoted.");
        }

        if (!IsPromotionSquare(piece, position))
        {
            throw new InvalidOperationException("Pawn is not on a promotion square.");
        }

        if (newType is PieceType.Pawn or PieceType.King)
        {
            throw new ArgumentException("A pawn cannot be promoted to a Pawn or King.");
        }

        piece.Type = newType;

        UpdateGameStatus();
    }

    private void UpdateGameStatus()
    {
        if (_status == GameStatus.Resigned)
        {
            return;
        }

        var inCheck = IsKingInCheck(CurrentPlayer.Color);

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
        var kingPosition = FindKing(CurrentPlayer.Color);
        var attackers = FindAttackingPieces(kingPosition, OppositeColor(CurrentPlayer.Color));
        handler?.Invoke(this, new KingThreatEventArgs((Player)CurrentPlayer, kingPosition, attackers));
    }

    public bool IsKingInCheck(Color color) => IsSquareUnderAttack(FindKing(color), OppositeColor(color));

    public bool IsCheckmate(Color color) => !PlayerHasAnyLegalMove(color);

    public bool IsDraw() => !IsKingInCheck(CurrentPlayer.Color) && !PlayerHasAnyLegalMove(CurrentPlayer.Color);

    public bool IsSquareUnderAttack(Position position, Color attacker)
    {
        for (var row = 0; row < 8; row++)
        {
            for (var col = 0; col < 8; col++)
            {
                var fromPosition = new Position(row, col);
                var piece = GetCell(fromPosition).OccupiedPiece;

                if (piece == null || piece.Color != attacker)
                {
                    continue;
                }

                if (piece.Type == PieceType.Pawn)
                {
                    var direction = piece.Color == Color.White ? -1 : 1;
                    if (fromPosition.Row + direction == position.Row
                        && Math.Abs(fromPosition.Column - position.Column) == 1)
                    {
                        return true;
                    }
                    continue;
                }

                if (GetPseudoLegalMoves(fromPosition, piece, includeCastling: false).Any(p => SamePosition(p, position)))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private bool PlayerHasAnyLegalMove(Color color)
    {
        for (var row = 0; row < 8; row++)
        {
            for (var col = 0; col < 8; col++)
            {
                var position = new Position(row, col);
                var piece = GetCell(position).OccupiedPiece;

                if (piece != null && piece.Color == color && GetLegalMoves(position).Count > 0)
                {
                    return true;
                }
            }
        }

        return false;
    }

    private bool WouldLeaveKingInCheck(Position from, Position to, Color movingColor)
    {
        var originalFromPiece = GetCell(from).OccupiedPiece;
        var originalToPiece = GetCell(to).OccupiedPiece;

        SetPiece(to, originalFromPiece);
        RemovePiece(from);

        var kingPosition = originalFromPiece!.Type == PieceType.King ? to : FindKing(movingColor);
        var leavesKingInCheck = IsSquareUnderAttack(kingPosition, OppositeColor(movingColor));

        SetPiece(from, originalFromPiece);
        SetPiece(to, originalToPiece);

        return leavesKingInCheck;
    }

    private Position FindKing(Color color)
    {
        for (var row = 0; row < 8; row++)
        {
            for (var col = 0; col < 8; col++)
            {
                var cell = _board.Cells[row, col];
                if (cell.OccupiedPiece is { Type: PieceType.King } piece && piece.Color == color)
                {
                    return cell.Position;
                }
            }
        }

        throw new InvalidOperationException($"No {color} king found on the board.");
    }

    private List<Piece> FindAttackingPieces(Position kingPosition, Color attackerColor)
    {
        var attackers = new List<Piece>();

        for (var row = 0; row < 8; row++)
        {
            for (var col = 0; col < 8; col++)
            {
                var fromPosition = new Position(row, col);
                var piece = GetCell(fromPosition).OccupiedPiece;

                if (piece != null && piece.Color == attackerColor
                    && GetPseudoLegalMoves(fromPosition, piece, includeCastling: false)
                        .Any(p => SamePosition(p, kingPosition)))
                {
                    attackers.Add((Piece)piece);
                }
            }
        }

        return attackers;
    }

    private static bool IsOnBoard(Position position) =>
        position.Row is >= 0 and <= 7 && position.Column is >= 0 and <= 7;

    private static bool SamePosition(Position a, Position b) => a.Row == b.Row && a.Column == b.Column;

    private static Color OppositeColor(Color color) => color == Color.White ? Color.Black : Color.White;

    private static IBoard CreateStandardBoard()
    {
        var cells = new ICell[8, 8];

        for (var row = 0; row < 8; row++)
        {
            for (var col = 0; col < 8; col++)
            {
                var piece = CreateStartingPiece(row, col);
                cells[row, col] = new Cell(new Position(row, col), piece, piece != null);
            }
        }

        return new Board(cells);
    }

    private static readonly PieceType[] BackRank =
    {
        PieceType.Rook, PieceType.Knight, PieceType.Bishop, PieceType.Queen,
        PieceType.King, PieceType.Bishop, PieceType.Knight, PieceType.Rook
    };

    private static IPiece? CreateStartingPiece(int row, int col)
    {
        return row switch
        {
            1 => new Piece(PieceType.Pawn, Color.Black, false),
            6 => new Piece(PieceType.Pawn, Color.White, false),
            0 => new Piece(BackRank[col], Color.Black, false),
            7 => new Piece(BackRank[col], Color.White, false),
            _ => null
        };
    }
}