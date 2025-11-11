using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace HideTheKing.Core
{
    public class HideTheKingManager : MonoBehaviour
    {
        public static HideTheKingManager Instance { get; private set; }

        private HiddenTargetLogicGeneric _whiteLogic;
        private HiddenTargetLogicGeneric _blackLogic;
        private bool _gameOverTriggered;
        private GameRules _gameRules;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        private void Start()
        {
            _gameRules = FindObjectOfType<GameRules>();
            StartCoroutine(InitializeWhenReady());
        }

        private IEnumerator InitializeWhenReady()
        {
            List<Piece> pieces = new List<Piece>();

            // Warten, bis Figuren gespawnt sind
            while (pieces.Count == 0)
            {
                pieces = FindObjectsOfType<Piece>(true)
                    .Where(p => p != null)
                    .ToList();
                yield return null;
            }

            Debug.Log($"[HideTheKing] {pieces.Count} Figuren gefunden – Initialisierung läuft...");

            _whiteLogic = new HiddenTargetLogicGeneric();
            _whiteLogic.Initialize(pieces, hiddenIsWhite: true);
            _whiteLogic.OnGameOver += HandleGameOver;

            _blackLogic = new HiddenTargetLogicGeneric();
            _blackLogic.Initialize(pieces, hiddenIsWhite: false);
            _blackLogic.OnGameOver += HandleGameOver;

#if UNITY_EDITOR
            var whiteSecret = _whiteLogic.Snapshot();
            var blackSecret = _blackLogic.Snapshot();
            Debug.Log($"[HideTheKing] White hidden King: {whiteSecret.HiddenTarget.type}");
            Debug.Log($"[HideTheKing] Black hidden King: {blackSecret.HiddenTarget.type}");
#endif
        }

        public void ReportCapture(ChessPiece capturedPiece)
        {
            if (_gameOverTriggered || capturedPiece == null)
                return;

            bool capturingIsWhite = !capturedPiece.isWhite;
            Piece pieceData = capturedPiece.GetComponent<Piece>();

            if (pieceData == null)
            {
                Debug.LogWarning("[HideTheKing] Captured piece had no Piece component attached!");
                return;
            }

            if (pieceData.isWhite)
            {
                _whiteLogic?.ReportCapture(pieceData, capturingIsWhite);
            }
            else
            {
                _blackLogic?.ReportCapture(pieceData, capturingIsWhite);
            }
        }

        private void HandleGameOver(bool capturingIsWhite, string reason)
        {
            if (_gameOverTriggered)
                return;

            _gameOverTriggered = true;

            string winnerText = capturingIsWhite ? "White" : "Black";
            string status = $"{winnerText} wins – {reason}";

            Debug.Log($"[HideTheKing] Checkmate! {status}");

            if (_gameRules != null && _gameRules.boardManager != null)
            {
                _gameRules.boardManager.gameState =
                    capturingIsWhite ? GameState.WhiteWins : GameState.BlackWins;
            }

            ChessTimer timer = FindObjectOfType<ChessTimer>();
            if (timer != null)
                timer.StopTimer();

            BoardManager board = FindObjectOfType<BoardManager>();
            if (board != null)
                board.enabled = false;
        }

        public HiddenTargetStateGeneric GetHiddenState(bool forWhite)
        {
            return forWhite ? _whiteLogic?.Snapshot() : _blackLogic?.Snapshot();
        }
    }
}
