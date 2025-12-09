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
        
        public static bool HideTheKingMode = true;

        // Track check warnings so they don’t spam
        private bool whiteHiddenInCheck = false;
        private bool blackHiddenInCheck = false;

        // Track captured pieces
        private HashSet<Piece> reportedCaptured = new HashSet<Piece>();

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


        private void Update()
        {
            DetectCapturedPieces();
            CheckHiddenTargetCheckState();
        }


        //  CAPTURE DETECTION (since pieces are never destroyed)
        private void DetectCapturedPieces()
        {
            var pieces = FindObjectsOfType<Piece>(true);

            foreach (var p in pieces)
            {
                // Disabled = captured by SendToSide()
                if (!p.enabled && !reportedCaptured.Contains(p))
                {
                    reportedCaptured.Add(p);
                    bool capturingIsWhite = !p.isWhite;
                    ReportCapture(p, capturingIsWhite);
                }
            }
        }


        //  DETECT IF THE HIDDEN PIECE IS IN CHECK
        private void CheckHiddenTargetCheckState()
        {
            // WHITE hidden king-role
            var whiteState = _whiteLogic?.Snapshot();
            if (whiteState != null && whiteState.HiddenTarget != null && whiteState.HiddenTarget.enabled)
            {
                bool nowInCheck = IsPieceInCheck(whiteState.HiddenTarget);

                if (nowInCheck && !whiteHiddenInCheck)
                {
                    whiteHiddenInCheck = true;
                    Debug.Log("[HideTheKing] WARNING! White's hidden figure is IN CHECK!");
                }
                else if (!nowInCheck)whiteHiddenInCheck = false;
            }

            // BLACK hidden king-role
            var blackState = _blackLogic?.Snapshot();
            if (blackState != null && blackState.HiddenTarget != null && blackState.HiddenTarget.enabled)
            {
                bool nowInCheck = IsPieceInCheck(blackState.HiddenTarget);
                if (nowInCheck && !blackHiddenInCheck)
                {
                    blackHiddenInCheck = true;
                    Debug.Log("[HideTheKing] WARNING! Black's hidden figure is IN CHECK!");
                }
                else if (!nowInCheck) blackHiddenInCheck = false;
            }
        }


        // Check if an enemy piece attacks the hidden role’s square
        public bool IsPieceInCheck(Piece target)
        {
            Piece[,] board = _gameRules.boardManager.boardPieces;
            Vector2Int targetPos = target.position;

            foreach (Piece p in board)
            {
                if (p != null && p.isWhite != target.isWhite)
                {
                    List<Vector2Int> moves = p.GetLegalMoves(board);

                    if (moves.Contains(targetPos))
                        return true;
                }
            }
            return false;
        }


        //  INITIALIZATION
        private IEnumerator InitializeWhenReady()
        {
            List<Piece> pieces = new List<Piece>();

            // Wait until pieces spawn
            while (pieces.Count == 0)
            {
                pieces = FindObjectsOfType<Piece>(true)
                    .Where(p => p != null)
                    .ToList();
                yield return null;
            }

            Debug.Log($"[HideTheKing] {pieces.Count} pieces found – initializing...");

            _whiteLogic = new HiddenTargetLogicGeneric();
            _whiteLogic.Initialize(pieces, hiddenIsWhite: true);
            _whiteLogic.OnGameOver += HandleGameOver;

            _blackLogic = new HiddenTargetLogicGeneric();
            _blackLogic.Initialize(pieces, hiddenIsWhite: false);
            _blackLogic.OnGameOver += HandleGameOver;

#if UNITY_EDITOR
            var whiteSecret = _whiteLogic.Snapshot();
            var blackSecret = _blackLogic.Snapshot();

            string whiteSide = HiddenTargetLogicGeneric.GetSideName(whiteSecret.HiddenTarget);
            string blackSide = HiddenTargetLogicGeneric.GetSideName(blackSecret.HiddenTarget);

            Debug.Log($"[HideTheKing] WHITE HIDDEN KING: {whiteSecret.HiddenTarget.type} ({whiteSide})");
            Debug.Log($"[HideTheKing] BLACK HIDDEN KING: {blackSecret.HiddenTarget.type} ({blackSide})");
#endif
        }


        //  CAPTURE REPORTING
        public void ReportCapture(Piece capturedPiece, bool capturingIsWhite)
        {
            if (_gameOverTriggered || capturedPiece == null)
                return;

            bool lostWasWhite = capturedPiece.isWhite;

            bool triggered =
                lostWasWhite
                    ? _whiteLogic.ReportCapture(capturedPiece, capturingIsWhite)
                    : _blackLogic.ReportCapture(capturedPiece, capturingIsWhite);

            if (triggered)
            {
                Debug.Log("[HideTheKing] GAME OVER – Hidden Target Captured!");
                Time.timeScale = 0f;
                HandleGameOver(capturingIsWhite, "Hidden Target Captured!");
            }
        }


        //  GAME OVER
        private void HandleGameOver(bool capturingIsWhite, string reason)
        {
            if (_gameOverTriggered) return;
            _gameOverTriggered = true;

            string winnerText = capturingIsWhite ? "White" : "Black";
            Debug.Log($"[HideTheKing] Checkmate! {winnerText} wins – {reason}");

            if (_gameRules != null && _gameRules.boardManager != null)
            {
                _gameRules.boardManager.gameState =
                    capturingIsWhite ? GameState.WhiteWins : GameState.BlackWins;
            }

            ChessTimer timer = FindObjectOfType<ChessTimer>();
            if (timer != null) timer.StopTimer();
            
            BoardManager board = FindObjectOfType<BoardManager>();
            if (board != null) board.enabled = false;
        }


        public HiddenTargetStateGeneric GetHiddenState(bool forWhite)
        {
            return forWhite ? _whiteLogic?.Snapshot() : _blackLogic?.Snapshot();
        }
    }
}