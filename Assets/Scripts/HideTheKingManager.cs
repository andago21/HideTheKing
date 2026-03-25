using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Mirror;
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

        // Track check warnings so they don't spam
        private bool whiteHiddenInCheck = false;
        private bool blackHiddenInCheck = false;

        // Track captured pieces
        private HashSet<Piece> reportedCaptured = new HashSet<Piece>();

        // The local player's hidden target (for UI highlight)
        private Piece _localHiddenTarget;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Start()
        {
            _gameRules = FindObjectOfType<GameRules>();

            // In single-player mode, initialize immediately as before.
            // In multiplayer, HideTheKingNetworkSync will call SetNetworkLogic instead.
            if (!IsMultiplayer())
            {
                StartCoroutine(InitializeWhenReady());
            }
        }

        private bool IsMultiplayer() =>
            NetworkClient.active || NetworkServer.active;

        private void Update()
        {
            if (_whiteLogic == null || _blackLogic == null) return;
            DetectCapturedPieces();
            CheckHiddenTargetCheckState();
        }

        // ---------------------------------------------------------------
        // Called by HideTheKingNetworkSync once seeds are resolved
        // ---------------------------------------------------------------
        public void SetNetworkLogic(HiddenTargetLogicGeneric whiteLogic,
                                    HiddenTargetLogicGeneric blackLogic)
        {
            _whiteLogic = whiteLogic;
            _blackLogic = blackLogic;

            // In multiplayer the server handles game-over via RPC,
            // so we don't subscribe to OnGameOver here to avoid double-firing.
            Debug.Log("[HideTheKing] Network logic set.");
        }

        // Called by HideTheKingNetworkSync to reveal which piece is YOUR hidden target
        public void OnLocalHiddenTargetRevealed(Piece target)
        {
            _localHiddenTarget = target;
            Debug.Log($"[HideTheKing] Local hidden target revealed: {target?.type} at {target?.position}");
            // TODO: Trigger your UI highlight here, e.g.:
            // HideTheKingUI.Instance?.HighlightHiddenTarget(target);
        }

        // ---------------------------------------------------------------
        // Server-only: check if the captured piece is a hidden target.
        // Called from HideTheKingNetworkSync.CmdReportCapture.
        // ---------------------------------------------------------------
        public bool ServerCheckCapture(Piece captured, bool capturingIsWhite)
        {
            if (_gameOverTriggered || captured == null) return false;

            bool lostWasWhite = captured.isWhite;
            bool triggered = lostWasWhite
                ? _whiteLogic?.ReportCapture(captured, capturingIsWhite) ?? false
                : _blackLogic?.ReportCapture(captured, capturingIsWhite) ?? false;

            return triggered;
        }

        // ---------------------------------------------------------------
        // Called on ALL clients via RpcGameOver
        // ---------------------------------------------------------------
        public void HandleNetworkGameOver(bool capturingIsWhite, string reason)
        {
            if (_gameOverTriggered) return;
            _gameOverTriggered = true;

            string winnerText = capturingIsWhite ? "White" : "Black";
            Debug.Log($"[HideTheKing] {winnerText} wins – {reason}");

            ApplyGameOver(capturingIsWhite);
        }

        // ---------------------------------------------------------------
        // CAPTURE DETECTION
        // ---------------------------------------------------------------
        private void DetectCapturedPieces()
        {
            var pieces = FindObjectsOfType<Piece>(true);

            foreach (var p in pieces)
            {
                if (!p.enabled && !reportedCaptured.Contains(p))
                {
                    reportedCaptured.Add(p);
                    bool capturingIsWhite = !p.isWhite;

                    if (IsMultiplayer())
                    {
                        // In multiplayer, route through the server for authority
                        NetworkIdentity ni = p.GetComponent<NetworkIdentity>();
                        if (ni != null && HideTheKingNetworkSync.Instance != null)
                        {
                            HideTheKingNetworkSync.Instance.CmdReportCapture(ni.netId, capturingIsWhite);
                        }
                    }
                    else
                    {
                        // Single-player: handle locally as before
                        ReportCapture(p, capturingIsWhite);
                    }
                }
            }
        }

        // ---------------------------------------------------------------
        // CHECK STATE DETECTION
        // ---------------------------------------------------------------
        private void CheckHiddenTargetCheckState()
        {
            CheckHiddenCheckWarning(_whiteLogic?.Snapshot(), ref whiteHiddenInCheck, "White");
            CheckHiddenCheckWarning(_blackLogic?.Snapshot(), ref blackHiddenInCheck, "Black");
        }

        private void CheckHiddenCheckWarning(HiddenTargetStateGeneric state,
                                              ref bool wasInCheck, string colorName)
        {
            if (state?.HiddenTarget == null || !state.HiddenTarget.enabled) return;

            bool nowInCheck = IsPieceInCheck(state.HiddenTarget);
            if (nowInCheck && !wasInCheck)
            {
                wasInCheck = true;
                Debug.Log($"[HideTheKing] {colorName}'s hidden figure is IN CHECK!");
                // In multiplayer, only warn the LOCAL player whose piece is in check.
                // The opponent should NOT see this warning.
                if (IsMultiplayer())
                {
                    ChessNetworkManager localNet = ChessNetworkManager.LocalInstance;
                    bool iAmWhite = localNet != null && localNet.isWhitePlayer;
                    bool warningIsForMe = (colorName == "White" && iAmWhite) ||
                                         (colorName == "Black" && !iAmWhite);
                    if (!warningIsForMe) { wasInCheck = false; return; }
                }
                // TODO: Show UI warning to the correct player
            }
            else if (!nowInCheck) wasInCheck = false;
        }

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

        // ---------------------------------------------------------------
        // SINGLE-PLAYER initialization (unchanged)
        // ---------------------------------------------------------------
        private IEnumerator InitializeWhenReady()
        {
            List<Piece> pieces = new List<Piece>();
            while (pieces.Count == 0)
            {
                pieces = FindObjectsOfType<Piece>(true)
                    .Where(p => p != null).ToList();
                yield return null;
            }

            Debug.Log($"[HideTheKing] {pieces.Count} pieces found – initializing (single-player)...");

            _whiteLogic = new HiddenTargetLogicGeneric();
            _whiteLogic.Initialize(pieces, hiddenIsWhite: true);
            _whiteLogic.OnGameOver += HandleSinglePlayerGameOver;

            _blackLogic = new HiddenTargetLogicGeneric();
            _blackLogic.Initialize(pieces, hiddenIsWhite: false);
            _blackLogic.OnGameOver += HandleSinglePlayerGameOver;

#if UNITY_EDITOR
            var ws = _whiteLogic.Snapshot();
            var bs = _blackLogic.Snapshot();
            Debug.Log($"[HideTheKing] WHITE HIDDEN: {ws.HiddenTarget.type} " +
                      $"({HiddenTargetLogicGeneric.GetSideName(ws.HiddenTarget)})");
            Debug.Log($"[HideTheKing] BLACK HIDDEN: {bs.HiddenTarget.type} " +
                      $"({HiddenTargetLogicGeneric.GetSideName(bs.HiddenTarget)})");
#endif
        }

        // ---------------------------------------------------------------
        // SINGLE-PLAYER capture reporting (unchanged)
        // ---------------------------------------------------------------
        public void ReportCapture(Piece capturedPiece, bool capturingIsWhite)
        {
            if (_gameOverTriggered || capturedPiece == null) return;

            bool lostWasWhite = capturedPiece.isWhite;
            bool triggered = lostWasWhite
                ? _whiteLogic.ReportCapture(capturedPiece, capturingIsWhite)
                : _blackLogic.ReportCapture(capturedPiece, capturingIsWhite);

            if (triggered)
            {
                Debug.Log("[HideTheKing] GAME OVER – Hidden Target Captured!");
                Time.timeScale = 0f;
            }
        }

        private void HandleSinglePlayerGameOver(bool capturingIsWhite, string reason)
        {
            if (_gameOverTriggered) return;
            _gameOverTriggered = true;
            Debug.Log($"[HideTheKing] {(capturingIsWhite ? "White" : "Black")} wins – {reason}");
            ApplyGameOver(capturingIsWhite);
        }

        // ---------------------------------------------------------------
        // Shared game-over application
        // ---------------------------------------------------------------
        private void ApplyGameOver(bool capturingIsWhite)
        {
            Time.timeScale = 0f;

            if (_gameRules?.boardManager != null)
            {
                _gameRules.boardManager.gameState =
                    capturingIsWhite ? GameState.WhiteWins : GameState.BlackWins;
            }

            ChessTimer timer = FindObjectOfType<ChessTimer>();
            if (timer != null) timer.StopTimer();

            BoardManager board = FindObjectOfType<BoardManager>();
            if (board != null) board.enabled = false;
        }

        // ---------------------------------------------------------------
        // PUBLIC ACCESSORS
        // ---------------------------------------------------------------
        public HiddenTargetStateGeneric GetHiddenState(bool forWhite) =>
            forWhite ? _whiteLogic?.Snapshot() : _blackLogic?.Snapshot();

        public List<Vector2Int> GetLegalMovesHTK(Piece piece, Piece[,] board = null)
        {
            if (piece == null) return new List<Vector2Int>();

            board = board ?? _gameRules?.boardManager?.boardPieces;
            if (board == null) return new List<Vector2Int>();

            bool prevHideMode = HideTheKingMode;
            HideTheKingMode = false;
            List<Vector2Int> baseMoves = piece.GetLegalMoves(board) ?? new List<Vector2Int>();
            HideTheKingMode = prevHideMode;

            if (!prevHideMode) return baseMoves;

            var state = piece.isWhite ? _whiteLogic?.Snapshot() : _blackLogic?.Snapshot();
            Piece hiddenTarget = state?.HiddenTarget;
            if (hiddenTarget == null || !hiddenTarget.enabled) return baseMoves;

            HideTheKingMode = false;
            bool isHiddenInCheck = IsPieceInCheck(hiddenTarget);
            HideTheKingMode = prevHideMode;

            var validMoves = new List<Vector2Int>();
            foreach (var move in baseMoves)
            {
                if (IsMoveValid(piece, piece.position, move, board,
                                isHiddenInCheck ? hiddenTarget : null))
                    validMoves.Add(move);
            }
            return validMoves;
        }

        private bool IsMoveValid(Piece piece, Vector2Int from, Vector2Int to,
                                 Piece[,] board, Piece optionalHiddenTarget = null)
        {
            Piece captured = board[to.x, to.y];
            bool wasCapturedEnabled = captured?.enabled ?? false;

            board[from.x, from.y] = null;
            board[to.x, to.y] = piece;
            piece.position = to;
            if (captured != null) captured.enabled = false;

            bool ownKingInCheck = Piece.IsKingInCheck(board, piece.isWhite);
            bool hiddenInCheck = false;
            if (optionalHiddenTarget != null)
            {
                HideTheKingMode = false;
                hiddenInCheck = IsPieceInCheck(optionalHiddenTarget);
                HideTheKingMode = true;
            }

            piece.position = from;
            board[from.x, from.y] = piece;
            board[to.x, to.y] = captured;
            if (captured != null) captured.enabled = wasCapturedEnabled;

            return !ownKingInCheck && !hiddenInCheck;
        }
    }
}