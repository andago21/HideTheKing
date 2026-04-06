using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Mirror;

namespace HideTheKing.Core
{
    public class HideTheKingManager : NetworkBehaviour
    {
        public static HideTheKingManager Instance { get; private set; }

        private HiddenTargetLogicGeneric _whiteLogic;
        private HiddenTargetLogicGeneric _blackLogic;
        private bool _gameOverTriggered;
        private GameRules _gameRules;

        public static bool HideTheKingMode = true;

        private bool whiteHiddenInCheck = false;
        private bool blackHiddenInCheck = false;
        private HashSet<Piece> reportedCaptured = new HashSet<Piece>();

        [SyncVar(hook = nameof(OnWhiteHiddenIndexChanged))]
        private int _whiteHiddenIndex = -1;

        [SyncVar(hook = nameof(OnBlackHiddenIndexChanged))]
        private int _blackHiddenIndex = -1;

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

        private void DetectCapturedPieces()
        {
            var pieces = FindObjectsOfType<Piece>(true);
            foreach (var p in pieces)
            {
                if (!p.enabled && !reportedCaptured.Contains(p))
                {
                    reportedCaptured.Add(p);
                    bool capturingIsWhite = !p.isWhite;
                    ReportCapture(p, capturingIsWhite);
                }
            }
        }

        private void CheckHiddenTargetCheckState()
        {
            CheckHiddenCheckWarning(_whiteLogic?.Snapshot(), ref whiteHiddenInCheck, "White");
            CheckHiddenCheckWarning(_blackLogic?.Snapshot(), ref blackHiddenInCheck, "Black");
        }

        private void CheckHiddenCheckWarning(HiddenTargetStateGeneric state, ref bool wasInCheck, string colorName)
        {
            if (state?.HiddenTarget == null || !state.HiddenTarget.enabled) return;
            bool nowInCheck = IsPieceInCheck(state.HiddenTarget);
            if (nowInCheck && !wasInCheck)
            {
                wasInCheck = true;
                Debug.Log($"[HideTheKing] {colorName}'s hidden figure is IN CHECK!");
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

        private IEnumerator InitializeWhenReady()
        {
            List<Piece> pieces = new List<Piece>();
            while (pieces.Count == 0)
            {
                pieces = FindObjectsOfType<Piece>(true)
                    .Where(p => p != null)
                    .ToList();
                yield return null;
            }

            Debug.Log($"[HideTheKing] {pieces.Count} pieces found – initializing...");

            if (NetworkServer.active)
            {
                _whiteLogic = new HiddenTargetLogicGeneric();
                _whiteLogic.Initialize(pieces, hiddenIsWhite: true);
                _whiteLogic.OnGameOver += HandleGameOver;

                _blackLogic = new HiddenTargetLogicGeneric();
                _blackLogic.Initialize(pieces, hiddenIsWhite: false);
                _blackLogic.OnGameOver += HandleGameOver;

                var whiteState = _whiteLogic.Snapshot();
                var blackState = _blackLogic.Snapshot();

                _whiteHiddenIndex = whiteState.HiddenTarget.position.x * 8 + whiteState.HiddenTarget.position.y;
                _blackHiddenIndex = blackState.HiddenTarget.position.x * 8 + blackState.HiddenTarget.position.y;

                Debug.Log($"[HideTheKing] Host: White hidden index={_whiteHiddenIndex}, Black hidden index={_blackHiddenIndex}");
            }
            else
            {
                Debug.Log("[HideTheKing] Client: Warte auf SyncVar vom Host...");

                if (_whiteHiddenIndex != -1 && _blackHiddenIndex != -1)
                    InitializeFromSyncVars(pieces);
            }
        }

        private void OnWhiteHiddenIndexChanged(int oldVal, int newVal)
        {
            Debug.Log($"[HideTheKing] Client: White hidden index empfangen: {newVal}");
            TryInitializeClientLogic();
        }

        private void OnBlackHiddenIndexChanged(int oldVal, int newVal)
        {
            Debug.Log($"[HideTheKing] Client: Black hidden index empfangen: {newVal}");
            TryInitializeClientLogic();
        }

        private void TryInitializeClientLogic()
        {
            if (_whiteHiddenIndex == -1 || _blackHiddenIndex == -1) return;
            if (NetworkServer.active) return;

            var pieces = FindObjectsOfType<Piece>(true)
                .Where(p => p != null)
                .ToList();

            if (pieces.Count == 0)
            {
                StartCoroutine(WaitAndInitClient());
                return;
            }

            InitializeFromSyncVars(pieces);
        }

        private IEnumerator WaitAndInitClient()
        {
            List<Piece> pieces = new List<Piece>();
            while (pieces.Count == 0)
            {
                pieces = FindObjectsOfType<Piece>(true).Where(p => p != null).ToList();
                yield return null;
            }
            InitializeFromSyncVars(pieces);
        }

        private void InitializeFromSyncVars(List<Piece> pieces)
        {
            int whiteRow = _whiteHiddenIndex / 8;
            int whiteCol = _whiteHiddenIndex % 8;
            int blackRow = _blackHiddenIndex / 8;
            int blackCol = _blackHiddenIndex % 8;

            Piece whiteHidden = pieces.FirstOrDefault(p => p.isWhite && p.position.x == whiteRow && p.position.y == whiteCol);
            Piece blackHidden = pieces.FirstOrDefault(p => !p.isWhite && p.position.x == blackRow && p.position.y == blackCol);

            if (whiteHidden == null || blackHidden == null)
            {
                Debug.LogError("[HideTheKing] Client: Konnte versteckte Figuren nicht finden!");
                return;
            }

            _whiteLogic = new HiddenTargetLogicGeneric();
            _whiteLogic.InitializeWithPiece(whiteHidden, hiddenIsWhite: true);
            _whiteLogic.OnGameOver += HandleGameOver;

            _blackLogic = new HiddenTargetLogicGeneric();
            _blackLogic.InitializeWithPiece(blackHidden, hiddenIsWhite: false);
            _blackLogic.OnGameOver += HandleGameOver;

            Debug.Log($"[HideTheKing] Client: Initialisiert — White={whiteHidden.type}, Black={blackHidden.type}");
        }

        public void ReportCapture(Piece capturedPiece, bool capturingIsWhite)
        {
            if (_gameOverTriggered || capturedPiece == null) return;

            bool lostWasWhite = capturedPiece.isWhite;
            bool triggered = lostWasWhite
                ? _whiteLogic?.ReportCapture(capturedPiece, capturingIsWhite) ?? false
                : _blackLogic?.ReportCapture(capturedPiece, capturingIsWhite) ?? false;

            if (triggered)
            {
                Debug.Log("[HideTheKing] GAME OVER – Hidden Target Captured!");
                Time.timeScale = 0f;
                HandleGameOver(capturingIsWhite, "Hidden Target Captured!");
            }
        }

        private void HandleGameOver(bool capturingIsWhite, string reason)
        {
            if (_gameOverTriggered) return;
            _gameOverTriggered = true;

            string winnerText = capturingIsWhite ? "White" : "Black";
            Debug.Log($"[HideTheKing] {winnerText} wins – {reason}");

            GameState result = capturingIsWhite ? GameState.WhiteWins : GameState.BlackWins;

            // Client: only stop timer — UIManager handles display via RpcReceiveGameEnd from server
            if (!NetworkServer.active)
            {
                ChessTimer timer = FindObjectOfType<ChessTimer>();
                if (timer != null) timer.StopTimer();
                return;
            }

            // SERVER only from here:
            if (_gameRules != null && _gameRules.boardManager != null)
                _gameRules.boardManager.gameState = result;

            ChessNetworkManager netManager = ChessNetworkManager.LocalInstance;
            if (netManager != null)
                netManager.SendGameEnd(result);

            ChessTimer timerServer = FindObjectOfType<ChessTimer>();
            if (timerServer != null) timerServer.StopTimer();

            BoardManager board = FindObjectOfType<BoardManager>();
            if (board != null) board.enabled = false;
        }

        public HiddenTargetStateGeneric GetHiddenState(bool forWhite)
        {
            return forWhite ? _whiteLogic?.Snapshot() : _blackLogic?.Snapshot();
        }

        private bool IsMoveValid(Piece piece, Vector2Int from, Vector2Int to, Piece[,] board, Piece optionalHiddenTarget = null)
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
                if (IsMoveValid(piece, piece.position, move, board, isHiddenInCheck ? hiddenTarget : null))
                    validMoves.Add(move);
            }

            return validMoves;
        }
    }
}