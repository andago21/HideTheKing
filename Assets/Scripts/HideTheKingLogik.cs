using System;
using System.Collections.Generic;
using System.Linq;

namespace DefaultNamespace
{
    public interface IHideTheKingLogic
    {
        event Action<GameResult> OnGameOver;
        void Initialize(NewGameOptions options = null);
        IReadOnlyList<Move> GetLegalMoves(Square from);
        bool MakeMove(Move move);
        bool RevealGuess(Guid pieceId);
        BoardState Snapshot();
    }

    public sealed class NewGameOptions
    {
        public PieceColor? HiddenSide { get; set; }
        public int? RandomSeed { get; set; }
    }


    public interface IMoveEngine
    {
        void SetupClassic();
        IReadOnlyList<Move> GetLegalMoves(Square from);
        MoveResult ApplyMove(Move move);
        PieceColor SideToMove { get; }
        IReadOnlyList<PieceView> Pieces { get; }
        PieceView GetPieceAt(Square square);
    }
    
    public enum PieceColor { White, Black }
    public enum PieceType { Pawn, Knight, Bishop, Rook, Queen, King }
    public readonly struct Square
    {
        public int File { get; }
        public int Rank { get; }
        public Square(int file, int rank) { File = file; Rank = rank; }
        public override string ToString() => $"{(char)('a' + File)}{Rank + 1}";
    }

    public sealed class Move
    {
        public Square From { get; init; }
        public Square To { get; init; }
        public override string ToString() => $"{From} -> {To}";
    }

    public sealed class MoveResult
    {
        public bool Applied { get; init; }
        public Guid? CapturedPieceId { get; init; }
        public string Message { get; init; }
    }

    public sealed class PieceView
    {
        public Guid Id { get; init; }
        public PieceType Type { get; init; }
        public PieceColor Color { get; init; }
        public int File { get; init; }
        public int Rank { get; init; }
        public bool Captured { get; init; }
    }

    public sealed class BoardState
    {
        public PieceColor SideToMove { get; init; }
        public IReadOnlyList<PieceView> Pieces { get; init; }
        public Guid HiddenTargetPieceId { get; init; }
    }

    public sealed class GameResult
    {
        public PieceColor Winner { get; init; }
        public string Reason { get; init; } = "";
    }


    public class HideTheKingLogik : IHideTheKingLogic
    {
        public event Action<GameResult> OnGameOver;

        private readonly IMoveEngine _engine;
        private Guid _hiddenTargetPieceId;
        private Random _rng = new();

        public HideTheKingLogik(IMoveEngine engine)
        {
            _engine = engine ?? throw new ArgumentNullException(nameof(engine));
        }

        public void Initialize(NewGameOptions options = null)
        {
            options ??= new NewGameOptions();
            _rng = options.RandomSeed.HasValue ? new Random(options.RandomSeed.Value) : new Random();
            _engine.SetupClassic();

            var side = options.HiddenSide ?? (_rng.Next(2) == 0 ? PieceColor.White : PieceColor.Black);

            var pool = _engine.Pieces
                .Where(p => !p.Captured && p.Color == side && p.Type != PieceType.Pawn)
                .ToList();

            if (pool.Count == 0)
                throw new InvalidOperationException("Keine geeignete Ziel-Figur gefunden (Nicht-Bauer).");

            _hiddenTargetPieceId = pool[_rng.Next(pool.Count)].Id;
        }

        public IReadOnlyList<Move> GetLegalMoves(Square from)
        {
            return _engine.GetLegalMoves(from);
        }

        public bool MakeMove(Move move)
        {
            var preTarget = _engine.GetPieceAt(move.To);

            var result = _engine.ApplyMove(move);
            if (!result.Applied) return false;

            var capturedId = result.CapturedPieceId ?? preTarget?.Id;

            if (capturedId.HasValue && capturedId.Value == _hiddenTargetPieceId)
            {
                OnGameOver?.Invoke(new GameResult
                {
                    Winner = OpponentOf(_engine.SideToMove), // Gewinner ist der, der gerade gezogen hat
                    Reason = $"Geheime Ziel-Figur auf {move.To} wurde geschlagen."
                });
            }

            return true;
        }

        public bool RevealGuess(Guid pieceId) => pieceId == _hiddenTargetPieceId;

        public BoardState Snapshot() => new BoardState
        {
            SideToMove = _engine.SideToMove,
            Pieces = _engine.Pieces,
            HiddenTargetPieceId = _hiddenTargetPieceId
        };

        private static PieceColor OpponentOf(PieceColor c) => c == PieceColor.White ? PieceColor.Black : PieceColor.White;
    }
}
