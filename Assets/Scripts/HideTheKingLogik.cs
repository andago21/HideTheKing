using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace HideTheKing.Core
{
    public sealed class HiddenTargetLogicGeneric
    {
        public event Action<bool, string> OnGameOver;

        private System.Random _rng = new System.Random();
        private Piece _hiddenTarget;
        private bool _hiddenIsWhite;
        private bool _initialized;

        public void Initialize(
            IReadOnlyList<Piece> pieces,
            bool? hiddenIsWhite = null,
            int? randomSeed = null)
        {
            if (pieces == null)
                throw new ArgumentNullException(nameof(pieces));

            _rng = randomSeed.HasValue ? new System.Random(randomSeed.Value) : new System.Random();
            _hiddenIsWhite = hiddenIsWhite ?? (_rng.Next(2) == 0);

            var pool = pieces
                .Where(p => p != null && p.gameObject != null)
                .Where(p => p.isWhite == _hiddenIsWhite)
                .Where(p => p.type != PieceType.Pawn)
                .ToList();

            if (pool.Count == 0)
                throw new InvalidOperationException("Hide The King Role not detected: No valid pieces available for selection.");

            _hiddenTarget = pool[_rng.Next(pool.Count)];
            _initialized = true;

#if UNITY_EDITOR
            Debug.Log($"[HideTheKing] Hide The King: {_hiddenTarget.name} ({_hiddenTarget.type}, {(_hiddenIsWhite ? "Weiß" : "Schwarz")})");
#endif
        }

        public bool ReportCapture(Piece captured, bool capturingIsWhite)
        {
            if (captured != _hiddenTarget)
                return false;

            OnGameOver?.Invoke(capturingIsWhite, "Check Mate!");
            return true;
        }

        public HiddenTargetStateGeneric Snapshot()
        {
            return new HiddenTargetStateGeneric
            {
                HiddenTarget = _hiddenTarget,
                HiddenIsWhite = _hiddenIsWhite
            };
        }
        
    }

    public sealed class HiddenTargetStateGeneric
    {
        public Piece HiddenTarget { get; set; }
        public bool HiddenIsWhite { get; set; }
    }
}
