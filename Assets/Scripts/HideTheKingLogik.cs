using System;
using System.Collections.Generic;
using System.Linq;

namespace HideTheKing.Core
{
    public sealed class HiddenTargetLogicGeneric
    {
        public event Action<bool, string> OnGameOver;

        private Random _rng = new Random();
        private ChessPiece _hiddenTarget;
        private bool _hiddenIsWhite;
        private bool _initialized;

        public void Initialize(IReadOnlyList<ChessPiece> pieces, bool? hiddenIsWhite = null, int? randomSeed = null)
        {
            if (pieces == null) throw new ArgumentNullException(nameof(pieces));
            _rng = randomSeed.HasValue ? new Random(randomSeed.Value) : new Random();

            _hiddenIsWhite = hiddenIsWhite ?? (_rng.Next(2) == 0);

            var pool = pieces.Where(p => p.isWhite == _hiddenIsWhite && p.gameObject != null).ToList();
            if (pool.Count == 0)
                throw new InvalidOperationException("Keine geeignete Ziel-Figur gefunden.");

            _hiddenTarget = pool[_rng.Next(pool.Count)];
            _initialized = true;
        }

        public bool RevealGuess(ChessPiece piece)
        {
            EnsureInitialized();
            return piece == _hiddenTarget;
        }

        public bool ReportCapture(ChessPiece captured, bool capturingIsWhite)
        {
            EnsureInitialized();
            if (captured != _hiddenTarget) return false;

            OnGameOver?.Invoke(capturingIsWhite, "Geheime Ziel-Figur wurde geschlagen!");
            return true;
        }

        public HiddenTargetStateGeneric Snapshot()
        {
            EnsureInitialized();
            return new HiddenTargetStateGeneric
            {
                HiddenTarget = _hiddenTarget,
                HiddenIsWhite = _hiddenIsWhite
            };
        }

        private void EnsureInitialized()
        {
            if (!_initialized)
                throw new InvalidOperationException("HiddenTargetLogicGeneric ist nicht initialisiert.");
        }
    }

    public sealed class HiddenTargetStateGeneric
    {
        public ChessPiece HiddenTarget { get; set; }
        public bool HiddenIsWhite { get; set; }
    }
}
