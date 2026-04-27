using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TutorialManager : MonoBehaviour
{
    public static TutorialManager Instance { get; private set; }

    public Transform[] squares;
    public BoardManager boardManager;

    public GameObject rookPrefab;
    public GameObject bishopPrefab;
    public GameObject queenPrefab;
    public GameObject kingPrefab;
    public GameObject blackKingPrefab;
    public GameObject knightPrefab;
    public GameObject pawnPrefab;
    public GameObject highlightPrefab;
    public GameObject highlightPrefabLayer2;
    
    public Transform rookSpawnTransform;
    public Transform[] rookHighlightTransforms;
    public Transform bishopSpawnTransform;
    public Transform[] bishopHighlightTransforms;
    public Transform queenSpawnTransform;
    public Transform[] queenHighlightTransforms;
    public Transform kingSpawnTransform;
    public Transform[] kingHighlightTransforms;
    public Transform knightSpawnTransform;
    public Transform[] knightHighlightTransforms;
    public Transform pawnSpawnTransform;
    public Transform[] pawnHighlightTransforms;

    [Header("Capture Tutorial")]
    public Transform captureRookSpawnTransform;
    public Transform capturePawnSpawnTransformA;
    public Transform capturePawnSpawnTransformB;
    public GameObject capturePawnPrefab;

    [Header("Stalemate Tutorial")]
    public Transform stalemateBishopSpawnTransform;
    public Transform stalemateRookSpawnTransform;
    public Transform stalemateKingSpawnTransform;
    public Transform[] stalemateHighlightTransforms;
        
    public float highlightYOffset = 0.01f;
    public bool clearEntireBoardOnCompletion = true;
    [SerializeField, Min(0f)] float clearBoardDelaySeconds = 1f;

    public Canvas sourceCanvas;
    public Canvas rookTargetCanvas;
    public Canvas bishopTargetCanvas;
    public Canvas queenTargetCanvas;
    public Canvas kingTargetCanvas;
    public Canvas knightTargetCanvas;
    public Canvas pawnTargetCanvas;
    public Canvas captureTargetCanvas;
    public Canvas stalemateTargetCanvas;
    public Canvas castlingTargetCanvas;
    public Canvas enPassantTargetCanvas;

    [Header("Promotion Panel")]
    public GameObject promotionPanel;

    [Header("Tutorial Piece Offsets")]
    [SerializeField] Vector3 rookPositionOffset = Vector3.zero;
    [SerializeField] Vector3 bishopPositionOffset = Vector3.zero;
    [SerializeField] Vector3 queenPositionOffset = Vector3.zero;
    [SerializeField] Vector3 kingPositionOffset = Vector3.zero;
    [SerializeField] Vector3 knightPositionOffset = Vector3.zero;
    [SerializeField] Vector3 pawnPositionOffset = Vector3.zero;

    GameObject _rookInstance;
    GameObject _bishopInstance;
    GameObject _queenInstance;
    GameObject _kingInstance;
    GameObject _knightInstance;
    GameObject _pawnInstance;
    readonly List<GameObject> _capturePawnInstances = new List<GameObject>();
    readonly List<GameObject> _stalemateSupportInstances = new List<GameObject>();
    readonly List<GameObject> _highlights = new List<GameObject>();
    readonly HashSet<int> _tutorialTargets = new HashSet<int>();
    readonly HashSet<int> _visitedTargets = new HashSet<int>();

    // Selection state — piece must be clicked first before a destination click moves it
    bool _pieceSelected;

    GameState _lastGameState = GameState.Playing;
    Coroutine _pendingClearRoutine;

    void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        Instance = this;
    }

    void OnDestroy() { if (Instance == this) Instance = null; }

    void Start()
    {
        if (boardManager == null) boardManager = FindObjectOfType<BoardManager>();
        if (boardManager != null) _lastGameState = boardManager.gameState;
        EnsureSquaresAttached();
    }

    void EnsureSquaresAttached()
    {
        // Starting another tutorial mode should cancel any queued auto-clear from the previous one.
        CancelPendingTutorialClear();

        if ((squares == null || squares.Length != 64) && boardManager != null)
            squares = boardManager.squares;
        if (squares == null || squares.Length != 64) return;

        for (int i = 0; i < 64; i++)
        {
            if (squares[i] == null) continue;
            var go = squares[i].gameObject;
            var router = go.GetComponent<TutorialSquareRouter>() ?? go.AddComponent<TutorialSquareRouter>();
            router.index = i;
            if (go.GetComponent<Collider>() == null)
            {
                var bc = go.AddComponent<BoxCollider>();
                bc.size = new Vector3(1f, 0.1f, 1f);
            }
        }
    }

    void Update()
    {
        if (boardManager == null) boardManager = FindObjectOfType<BoardManager>();
        if (boardManager == null) return;
        var state = boardManager.gameState;
        if (state == _lastGameState) return;
        if ((state == GameState.WhiteWins || state == GameState.BlackWins) && LocalPlayerWon(state))
        {
            SetCanvas(sourceCanvas, true);
            SetCanvas(rookTargetCanvas, false);
            SetCanvas(bishopTargetCanvas, false);
            SetCanvas(queenTargetCanvas, false);
            SetCanvas(kingTargetCanvas, false);
            SetCanvas(knightTargetCanvas, false);
            SetCanvas(pawnTargetCanvas, false);
            SetCanvas(captureTargetCanvas, false);
            SetCanvas(stalemateTargetCanvas, false);
            SetCanvas(castlingTargetCanvas, false);
            SetCanvas(enPassantTargetCanvas, false);
        }
        _lastGameState = state;
    }

    // ── Tutorial is only active when a piece instance exists ──────────────────
    public bool TutorialActive => _rookInstance != null || _bishopInstance != null || _queenInstance != null || _kingInstance != null || _knightInstance != null || _pawnInstance != null;

    // Called by TutorialSquareRouter when a square is clicked
    public void OnSquareClicked(int index)
    {
        if (!TutorialActive) return;

        // If nothing selected yet, check if user clicked the tutorial piece square
        if (!_pieceSelected)
        {
            int pieceIndex = GetTutorialPieceIndex();
            if (index == pieceIndex)
                SelectPiece();
            return;
        }

        // Piece already selected — try to move to clicked square
        MoveTutorialPieceToIndex(index);
    }

    int GetTutorialPieceIndex()
    {
        Piece p = GetActivePiece();
        if (p == null) return -1;
        return p.position.x * 8 + p.position.y;
    }

    Piece GetActivePiece()
    {
        if (_pawnInstance != null) return _pawnInstance.GetComponent<Piece>();
        if (_knightInstance != null) return _knightInstance.GetComponent<Piece>();
        if (_kingInstance != null) return _kingInstance.GetComponent<Piece>();
        if (_queenInstance != null) return _queenInstance.GetComponent<Piece>();
        if (_bishopInstance != null) return _bishopInstance.GetComponent<Piece>();
        if (_rookInstance != null)   return _rookInstance.GetComponent<Piece>();
        return null;
    }

    void SelectPiece()
    {
        _pieceSelected = true;

        // Visual feedback: highlight the piece or log selection
        Debug.Log("Tutorial piece selected. Click a highlighted square to move.");

        // Optional: Add a material highlight to the selected piece
        var activePieceInstance = _pawnInstance ?? _knightInstance ?? _kingInstance ?? _queenInstance ?? _rookInstance ?? _bishopInstance;
        switch (activePieceInstance)
        {
            case null:
                break;
            default:
                HighlightPiece(activePieceInstance);
                break;
        }

        // Always show legal move highlights when piece is selected.
        ClearLegalMoveHighlights();
        ShowLegalMovesForActivePiece();
    }

    void ShowLegalMovesForActivePiece()
    {
        Piece piece = GetActivePiece();
        if (piece == null) return;

        var legal = piece.GetLegalMovesWithCheckValidation(boardManager != null
            ? boardManager.boardPieces
            : new Piece[8, 8]);

        // Fallback if check validation fails
        if (legal == null || legal.Count == 0)
        {
            var raw = piece.GetLegalMoves(boardManager != null ? boardManager.boardPieces : new Piece[8, 8]);
            if (raw != null && raw.Count > 0)
            {
                legal = raw;
            }
            else
            {
                if (_rookInstance   != null) legal = GenerateRookMoves(piece.position, piece.isWhite);
                else if (_bishopInstance != null) legal = GenerateBishopMoves(piece.position, piece.isWhite);
                else if (_queenInstance  != null) legal = GenerateQueenMoves(piece.position, piece.isWhite);
                else if (_kingInstance   != null) legal = GenerateKingMoves(piece.position, piece.isWhite);
                else if (_knightInstance != null) legal = GenerateKnightMoves(piece.position, piece.isWhite);
                else if (_pawnInstance   != null) legal = GeneratePawnMoves(piece.position, piece.isWhite, piece.hasMoved);
                if (legal == null) legal = new List<Vector2Int>();
            }
        }

        // Show base highlightPrefab for every legal move square.
        // If that square is also a tutorial target the Layer2 star is already
        // there and both will coexist. If a target square is NOT a legal move
        // only the star stays (no base highlight).
        foreach (var move in legal)
        {
            int moveIndex = move.x * 8 + move.y;
            ShowLegalMoveHighlightAtIndex(moveIndex);
        }
    }

    void HighlightPiece(GameObject piece)
    {
        if (piece == null) return;
        var renderers = piece.GetComponentsInChildren<Renderer>();
        foreach (var rend in renderers)
        {
            // Store original materials and apply a highlight effect (optional)
            foreach (var material in rend.materials)
            {
                material.color = material.color * 1.2f; // Brighten the piece
            }
        }
    }

    public void DeselectPiece()
    {
        _pieceSelected = false;

        // Reset visual feedback
        var activePieceInstance = _pawnInstance ?? _knightInstance ?? _kingInstance ?? _queenInstance ?? _rookInstance ?? _bishopInstance;
        switch (activePieceInstance)
        {
            case null:
                break;
            default:
                RestorePieceAppearance(activePieceInstance);
                break;
        }
    }

    void RestorePieceAppearance(GameObject piece)
    {
        if (piece == null) return;
        var renderers = piece.GetComponentsInChildren<Renderer>();
        foreach (var rend in renderers)
        {
            foreach (var material in rend.materials)
            {
                material.color = material.color / 1.2f; // Restore original brightness
            }
        }
    }

    public void ShowPromotionPanel()
    {
        if (promotionPanel != null)
        {
            promotionPanel.SetActive(true);
            StartCoroutine(HidePromotionPanelAfterDelay(5f));
        }
    }

    public void HidePromotionPanel()
    {
        if (promotionPanel != null) promotionPanel.SetActive(false);
    }

    private IEnumerator HidePromotionPanelAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        HidePromotionPanel();
    }

    public void OnRookButton()
    {
        EnsureSquaresAttached();
        ResetEnPassantTutorialState();
        SetCanvas(sourceCanvas, false);
        SetCanvas(rookTargetCanvas, true);
        SetCanvas(captureTargetCanvas, false);
        SetCanvas(stalemateTargetCanvas, false);
        SetCanvas(enPassantTargetCanvas, false);

        if (boardManager == null) boardManager = FindObjectOfType<BoardManager>();
        if ((squares == null || squares.Length != 64) && boardManager != null) squares = boardManager.squares;

        if (rookSpawnTransform != null)
        {
            int spawnIndex = GetIndexFromTransform(rookSpawnTransform);
            InstantiateRookAtIndex(spawnIndex);
        }
        else
        {
            InstantiateRookAtAlgebraic("d4");
        }

        ClearHighlights();
        _pieceSelected = false;

        if (rookHighlightTransforms != null && rookHighlightTransforms.Length > 0)
        {
            CreateHighlightsLayer2FromTransforms(rookHighlightTransforms);
        }
    }

    public void OnBishopButton()
    {
        EnsureSquaresAttached();
        ResetEnPassantTutorialState();
        SetCanvas(sourceCanvas, false);
        SetCanvas(rookTargetCanvas, false);
        SetCanvas(bishopTargetCanvas, true);
        SetCanvas(captureTargetCanvas, false);
        SetCanvas(stalemateTargetCanvas, false);
        SetCanvas(enPassantTargetCanvas, false);

        if (boardManager == null) boardManager = FindObjectOfType<BoardManager>();
        if ((squares == null || squares.Length != 64) && boardManager != null) squares = boardManager.squares;

        if (bishopSpawnTransform != null)
        {
            int spawnIndex = GetIndexFromTransform(bishopSpawnTransform);
            InstantiateBishopAtIndex(spawnIndex);
        }
        else  InstantiateBishopAtAlgebraic("d4");
        

        ClearHighlights();
        _pieceSelected = false;

        if (bishopHighlightTransforms != null && bishopHighlightTransforms.Length > 0)
        {
            CreateHighlightsLayer2FromTransforms(bishopHighlightTransforms);
        }
    }

    public void OnQueenButton()
    {
        EnsureSquaresAttached();
        ResetEnPassantTutorialState();
        SetCanvas(sourceCanvas, false);
        SetCanvas(rookTargetCanvas, false);
        SetCanvas(bishopTargetCanvas, false);
        SetCanvas(queenTargetCanvas, true);
        SetCanvas(captureTargetCanvas, false);
        SetCanvas(stalemateTargetCanvas, false);
        SetCanvas(enPassantTargetCanvas, false);

        if (boardManager == null) boardManager = FindObjectOfType<BoardManager>();
        if ((squares == null || squares.Length != 64) && boardManager != null) squares = boardManager.squares;

        if (queenSpawnTransform != null)
        {
            int spawnIndex = GetIndexFromTransform(queenSpawnTransform);
            InstantiateQueenAtIndex(spawnIndex);
        }
        else
        {
            InstantiateQueenAtAlgebraic("d4");
        }

        ClearHighlights();
        _pieceSelected = false;

        if (queenHighlightTransforms != null && queenHighlightTransforms.Length > 0)
        {
            CreateHighlightsLayer2FromTransforms(queenHighlightTransforms);
        }
    }

    public void OnKingButton()
    {
        EnsureSquaresAttached();
        ResetEnPassantTutorialState();
        SetCanvas(sourceCanvas, false);
        SetCanvas(rookTargetCanvas, false);
        SetCanvas(bishopTargetCanvas, false);
        SetCanvas(queenTargetCanvas, false);
        SetCanvas(kingTargetCanvas, true);
        SetCanvas(captureTargetCanvas, false);
        SetCanvas(stalemateTargetCanvas, false);
        SetCanvas(enPassantTargetCanvas, false);

        if (boardManager == null) boardManager = FindObjectOfType<BoardManager>();
        if ((squares == null || squares.Length != 64) && boardManager != null) squares = boardManager.squares;

        if (kingSpawnTransform != null)
        {
            int spawnIndex = GetIndexFromTransform(kingSpawnTransform);
            InstantiateKingAtIndex(spawnIndex);
        }
        else
        {
            InstantiateKingAtAlgebraic("d4");
        }

        ClearHighlights();
        _pieceSelected = false;

        if (kingHighlightTransforms != null && kingHighlightTransforms.Length > 0)
        {
            CreateHighlightsLayer2FromTransforms(kingHighlightTransforms);
        }
    }

    public void OnCastlingButton()
    {
        EnsureSquaresAttached();
        ResetEnPassantTutorialState();

        if (boardManager == null) boardManager = FindObjectOfType<BoardManager>();
        if ((squares == null || squares.Length != 64) && boardManager != null) squares = boardManager.squares;

        if (boardManager != null)
        {
            ClearEntireBoard();
            boardManager.gameState = GameState.Playing;
            boardManager.isWhiteTurn = true;
            boardManager.enPassantTarget = new Vector2Int(-1, -1);
        }

        SetCanvas(sourceCanvas, false);
        SetCanvas(rookTargetCanvas, false);
        SetCanvas(bishopTargetCanvas, false);
        SetCanvas(queenTargetCanvas, false);
        SetCanvas(kingTargetCanvas, false);
        SetCanvas(knightTargetCanvas, false);
        SetCanvas(pawnTargetCanvas, false);
        SetCanvas(captureTargetCanvas, false);
        SetCanvas(stalemateTargetCanvas, false);
        SetCanvas(castlingTargetCanvas, true);
        SetCanvas(enPassantTargetCanvas, false);

        ClearHighlights();
        ClearCapturePawns();
        ClearStalemateSupportPieces();
        _pieceSelected = false;

        // White setup matching the castling lesson board.
        InstantiateKingAtAlgebraic("e1");
        AddCastlingSupportPiece("a1", true, PieceType.Rook);
        AddCastlingSupportPiece("h1", true, PieceType.Rook);
        AddCastlingSupportPiece("b1", true, PieceType.Knight);
        AddCastlingSupportPiece("c1", true, PieceType.Bishop);
        AddCastlingSupportPiece("d1", true, PieceType.Queen);
        AddCastlingSupportPiece("c4", true, PieceType.Bishop);
        AddCastlingSupportPiece("f3", true, PieceType.Knight);
        AddCastlingSupportPiece("a2", true, PieceType.Pawn);
        AddCastlingSupportPiece("b2", true, PieceType.Pawn);
        AddCastlingSupportPiece("c2", true, PieceType.Pawn);
        AddCastlingSupportPiece("d2", true, PieceType.Pawn);
        AddCastlingSupportPiece("e3", true, PieceType.Pawn);
        AddCastlingSupportPiece("f2", true, PieceType.Pawn);
        AddCastlingSupportPiece("g2", true, PieceType.Pawn);
        AddCastlingSupportPiece("h2", true, PieceType.Pawn);

        // Full black army included.
        AddCastlingSupportPiece("a8", false, PieceType.Rook);
        AddCastlingSupportPiece("b8", false, PieceType.Knight);
        AddCastlingSupportPiece("c8", false, PieceType.Bishop);
        AddCastlingSupportPiece("d8", false, PieceType.Queen);
        AddCastlingSupportPiece("e8", false, PieceType.King);
        AddCastlingSupportPiece("f8", false, PieceType.Bishop);
        AddCastlingSupportPiece("g8", false, PieceType.Knight);
        AddCastlingSupportPiece("h8", false, PieceType.Rook);
        AddCastlingSupportPiece("a7", false, PieceType.Pawn);
        AddCastlingSupportPiece("b7", false, PieceType.Pawn);
        AddCastlingSupportPiece("c7", false, PieceType.Pawn);
        AddCastlingSupportPiece("d7", false, PieceType.Pawn);
        AddCastlingSupportPiece("e7", false, PieceType.Pawn);
        AddCastlingSupportPiece("f7", false, PieceType.Pawn);
        AddCastlingSupportPiece("g7", false, PieceType.Pawn);
        AddCastlingSupportPiece("h7", false, PieceType.Pawn);

        _tutorialTargets.Clear();
        _visitedTargets.Clear();
        int kingsideCastlingTarget = AlgebraicToIndex("g1");
        if (IsValidIndex(kingsideCastlingTarget))
        {
            _tutorialTargets.Add(kingsideCastlingTarget);
            ShowHighlightAtIndexLayer2(kingsideCastlingTarget);
        }
    }

    public void OnKnightButton()
    {
        EnsureSquaresAttached();
        ResetEnPassantTutorialState();
        SetCanvas(sourceCanvas, false);
        SetCanvas(rookTargetCanvas, false);
        SetCanvas(bishopTargetCanvas, false);
        SetCanvas(queenTargetCanvas, false);
        SetCanvas(kingTargetCanvas, false);
        SetCanvas(knightTargetCanvas, true);
        SetCanvas(captureTargetCanvas, false);
        SetCanvas(stalemateTargetCanvas, false);
        SetCanvas(enPassantTargetCanvas, false);

        if (boardManager == null) boardManager = FindObjectOfType<BoardManager>();
        if ((squares == null || squares.Length != 64) && boardManager != null) squares = boardManager.squares;

        if (knightSpawnTransform != null)
        {
            int spawnIndex = GetIndexFromTransform(knightSpawnTransform);
            InstantiateKnightAtIndex(spawnIndex);
        }
        else  InstantiateKnightAtAlgebraic("d4");
        

        ClearHighlights();
        _pieceSelected = false;

        if (knightHighlightTransforms != null && knightHighlightTransforms.Length > 0)
        {
            CreateHighlightsLayer2FromTransforms(knightHighlightTransforms);
        }
    }

    public void OnPawnButton()
    {
        EnsureSquaresAttached();
        ResetEnPassantTutorialState();
        SetCanvas(sourceCanvas, false);
        SetCanvas(rookTargetCanvas, false);
        SetCanvas(bishopTargetCanvas, false);
        SetCanvas(queenTargetCanvas, false);
        SetCanvas(kingTargetCanvas, false);
        SetCanvas(knightTargetCanvas, false);
        SetCanvas(pawnTargetCanvas, true);
        SetCanvas(captureTargetCanvas, false);
        SetCanvas(stalemateTargetCanvas, false);
        SetCanvas(castlingTargetCanvas, false);
        SetCanvas(enPassantTargetCanvas, false);

        if (boardManager == null) boardManager = FindObjectOfType<BoardManager>();
        if ((squares == null || squares.Length != 64) && boardManager != null) squares = boardManager.squares;

        if (pawnSpawnTransform != null)
        {
            int spawnIndex = GetIndexFromTransform(pawnSpawnTransform);
            InstantiatePawnAtIndex(spawnIndex);
        }
        else  InstantiatePawnAtAlgebraic("d2");

        ClearHighlights();
        _pieceSelected = false;

        if (pawnHighlightTransforms != null && pawnHighlightTransforms.Length > 0)
        {
            CreateHighlightsLayer2FromTransforms(pawnHighlightTransforms);
        }
    }

    public void OnCaptureButton()
    {
        EnsureSquaresAttached();
        ResetEnPassantTutorialState();
        SetCanvas(sourceCanvas, false);
        SetCanvas(rookTargetCanvas, false);
        SetCanvas(bishopTargetCanvas, false);
        SetCanvas(queenTargetCanvas, false);
        SetCanvas(kingTargetCanvas, false);
        SetCanvas(knightTargetCanvas, false);
        SetCanvas(pawnTargetCanvas, false);
        SetCanvas(captureTargetCanvas, true);
        SetCanvas(stalemateTargetCanvas, false);
        SetCanvas(castlingTargetCanvas, false);
        SetCanvas(enPassantTargetCanvas, false);

        if (boardManager == null) boardManager = FindObjectOfType<BoardManager>();
        if ((squares == null || squares.Length != 64) && boardManager != null) squares = boardManager.squares;

        ClearHighlights();
        ClearCapturePawns();
        // Capture tutorial needs a clean board if coming from castling/stalemate setups.
        ClearStalemateSupportPieces();
        _pieceSelected = false;

        if (captureRookSpawnTransform != null)
        {
            int spawnIndex = GetIndexFromTransform(captureRookSpawnTransform);
            InstantiateRookAtIndex(spawnIndex);
        }
        else
        {
            InstantiateRookAtAlgebraic("d4");
        }

        if (capturePawnSpawnTransformA != null)
        {
            int pawnAIndex = GetIndexFromTransform(capturePawnSpawnTransformA);
            InstantiateCapturePawnAtIndex(pawnAIndex);
        }
        else
        {
            InstantiateCapturePawnAtAlgebraic("d6");
        }

        if (capturePawnSpawnTransformB != null)
        {
            int pawnBIndex = GetIndexFromTransform(capturePawnSpawnTransformB);
            InstantiateCapturePawnAtIndex(pawnBIndex);
        }
        else
        {
            InstantiateCapturePawnAtAlgebraic("g4");
        }
    }

    public void OnBackButton()
    {
        ResetEnPassantTutorialState();
        ClearHighlights();
        ClearRook();
        ClearBishop();
        ClearQueen();
        ClearKing();
        ClearKnight();
        ClearPawn();
        ClearCapturePawns();
        ClearStalemateSupportPieces();
        _tutorialTargets.Clear();
        _visitedTargets.Clear();
        _pieceSelected = false;

        ClearLooseHighlightClones();

        SetCanvas(rookTargetCanvas, false);
        SetCanvas(bishopTargetCanvas, false);
        SetCanvas(queenTargetCanvas, false);
        SetCanvas(kingTargetCanvas, false);
        SetCanvas(knightTargetCanvas, false);
        SetCanvas(pawnTargetCanvas, false);
        SetCanvas(captureTargetCanvas, false);
        SetCanvas(stalemateTargetCanvas, false);
        SetCanvas(castlingTargetCanvas, false);
        SetCanvas(enPassantTargetCanvas, false);
        SetCanvas(sourceCanvas, true);
    }

    public void OnStalemateButton()
    {
        EnsureSquaresAttached();
        ResetEnPassantTutorialState();

        if (boardManager == null) boardManager = FindObjectOfType<BoardManager>();
        if ((squares == null || squares.Length != 64) && boardManager != null) squares = boardManager.squares;

        if (boardManager != null)
        {
            ClearEntireBoard();
            boardManager.gameState = GameState.Playing;
            boardManager.isWhiteTurn = true;
            boardManager.enPassantTarget = new Vector2Int(-1, -1);
        }

        SetCanvas(sourceCanvas, false);
        SetCanvas(rookTargetCanvas, false);
        SetCanvas(bishopTargetCanvas, false);
        SetCanvas(queenTargetCanvas, false);
        SetCanvas(kingTargetCanvas, false);
        SetCanvas(knightTargetCanvas, false);
        SetCanvas(pawnTargetCanvas, false);
        SetCanvas(captureTargetCanvas, false);
        SetCanvas(stalemateTargetCanvas, true);
        SetCanvas(castlingTargetCanvas, false);
        SetCanvas(enPassantTargetCanvas, false);

        ClearHighlights();
        ClearCapturePawns();
        ClearStalemateSupportPieces();
        _pieceSelected = false;

        if (stalemateBishopSpawnTransform != null)
        {
            InstantiateBishopAtIndex(GetIndexFromTransform(stalemateBishopSpawnTransform));
        }
        else
        {
            InstantiateBishopAtAlgebraic("g5");
        }

        if (stalemateRookSpawnTransform != null)
        {
            InstantiateStalemateRookAtIndex(GetIndexFromTransform(stalemateRookSpawnTransform));
        }
        else
        {
            InstantiateStalemateRookAtAlgebraic("b3");
        }

        if (stalemateKingSpawnTransform != null)
        {
            InstantiateStalemateBlackKingAtIndex(GetIndexFromTransform(stalemateKingSpawnTransform));
        }
        else
        {
            InstantiateStalemateBlackKingAtAlgebraic("a8");
        }

        if (stalemateHighlightTransforms != null && stalemateHighlightTransforms.Length > 0)
        {
            CreateHighlightsLayer2FromTransforms(stalemateHighlightTransforms);
        }
        else
        {
            _tutorialTargets.Clear();
            _visitedTargets.Clear();
            int targetIndex = AlgebraicToIndex("e3");
            if (IsValidIndex(targetIndex))
            {
                _tutorialTargets.Add(targetIndex);
                ShowHighlightAtIndexLayer2(targetIndex);
            }
        }
    }

    public void OnEnPassantButton()
    {
        EnsureSquaresAttached();
        ResetEnPassantTutorialState();

        if (boardManager == null) boardManager = FindObjectOfType<BoardManager>();
        if ((squares == null || squares.Length != 64) && boardManager != null) squares = boardManager.squares;

        if (boardManager != null)
        {
            ClearEntireBoard();
            boardManager.gameState = GameState.Playing;
            boardManager.isWhiteTurn = true;
            // d6 is the en passant capture square (rank=5, file=3)
            boardManager.enPassantTarget = new Vector2Int(5, 3);
        }

        SetCanvas(sourceCanvas, false);
        SetCanvas(rookTargetCanvas, false);
        SetCanvas(bishopTargetCanvas, false);
        SetCanvas(queenTargetCanvas, false);
        SetCanvas(kingTargetCanvas, false);
        SetCanvas(knightTargetCanvas, false);
        SetCanvas(pawnTargetCanvas, false);
        SetCanvas(captureTargetCanvas, false);
        SetCanvas(stalemateTargetCanvas, false);
        SetCanvas(castlingTargetCanvas, false);
        SetCanvas(enPassantTargetCanvas, true);

        ClearHighlights();
        ClearCapturePawns();
        ClearStalemateSupportPieces();
        _pieceSelected = false;

        // ── Full black army ───────────────────────────────────────────────────
        AddCastlingSupportPiece("a8", false, PieceType.Rook);
        AddCastlingSupportPiece("b8", false, PieceType.Knight);
        AddCastlingSupportPiece("c8", false, PieceType.Bishop);
        AddCastlingSupportPiece("d8", false, PieceType.Queen);
        AddCastlingSupportPiece("e8", false, PieceType.King);
        AddCastlingSupportPiece("f8", false, PieceType.Bishop);
        AddCastlingSupportPiece("g8", false, PieceType.Knight);
        AddCastlingSupportPiece("h8", false, PieceType.Rook);
        // Black pawns on rank 7 — d7 and g7 missing (those pawns advanced)
        AddCastlingSupportPiece("a7", false, PieceType.Pawn);
        AddCastlingSupportPiece("b7", false, PieceType.Pawn);
        AddCastlingSupportPiece("c7", false, PieceType.Pawn);
        AddCastlingSupportPiece("e7", false, PieceType.Pawn);
        AddCastlingSupportPiece("f7", false, PieceType.Pawn);
        AddCastlingSupportPiece("h7", false, PieceType.Pawn);
        // Black pawn that double-moved to d5 (en passant target is d6)
        AddCastlingSupportPiece("d5", false, PieceType.Pawn);

        // ── Full white army ───────────────────────────────────────────────────
        AddCastlingSupportPiece("a1", true, PieceType.Rook);
        AddCastlingSupportPiece("b1", true, PieceType.Knight);
        AddCastlingSupportPiece("c1", true, PieceType.Bishop);
        AddCastlingSupportPiece("d1", true, PieceType.Queen);
        AddCastlingSupportPiece("e1", true, PieceType.King);
        AddCastlingSupportPiece("f1", true, PieceType.Bishop);
        AddCastlingSupportPiece("g1", true, PieceType.Knight);
        AddCastlingSupportPiece("h1", true, PieceType.Rook);
        // White pawns on rank 2 — c2 and h2 missing (those pawns advanced)
        AddCastlingSupportPiece("a2", true, PieceType.Pawn);
        AddCastlingSupportPiece("b2", true, PieceType.Pawn);
        AddCastlingSupportPiece("d2", true, PieceType.Pawn);
        AddCastlingSupportPiece("e2", true, PieceType.Pawn);
        AddCastlingSupportPiece("f2", true, PieceType.Pawn);
        AddCastlingSupportPiece("g2", true, PieceType.Pawn);

        // ── Interactive white pawn at c5 (captures en passant to d6) ─────────
        InstantiatePawnAtAlgebraic("c5");
        if (_pawnInstance != null)
        {
            var piece = _pawnInstance.GetComponent<Piece>();
            if (piece != null) piece.hasMoved = true; // pawn has already moved from c2
        }

        // Highlight d6 as the tutorial target
        _tutorialTargets.Clear();
        _visitedTargets.Clear();
        int epTargetIndex = AlgebraicToIndex("d6");
        if (IsValidIndex(epTargetIndex))
        {
            _tutorialTargets.Add(epTargetIndex);
            ShowHighlightAtIndexLayer2(epTargetIndex);
        }
    }

    void ResetEnPassantTutorialState()
    {
        // Retained for button flow symmetry; no state to reset in single-pawn en passant mode.
    }

    void ClearLooseHighlightClones()    {
        string baseHighlightName = highlightPrefab != null ? highlightPrefab.name + "(Clone)" : null;
        string layer2HighlightName = highlightPrefabLayer2 != null ? highlightPrefabLayer2.name + "(Clone)" : null;

        var all = FindObjectsOfType<GameObject>();
        foreach (var go in all)
        {
            if (go == null) continue;
            if ((baseHighlightName != null && go.name == baseHighlightName) ||
                (layer2HighlightName != null && go.name == layer2HighlightName))
            {
                Destroy(go);
            }
        }
    }

    public void ShowHighlightAtIndex(int index)
    {
        if (!IsValidIndex(index) || highlightPrefab == null) return;
        var pos = squares[index].position + new Vector3(-0.5f, highlightYOffset, +0.5f);
        var h = Instantiate(highlightPrefab, pos, highlightPrefab.transform.rotation, squares[index]);
        h.name = "Tutorial_Highlight_" + IndexToAlgebraic(index);

        var th = h.GetComponent<TutorialHighlight>() ?? h.AddComponent<TutorialHighlight>();
        th.index = index;
        th.manager = this;
        _tutorialTargets.Add(index);

        var col = h.GetComponent<Collider>();
        if (col == null)
        {
            var bc = h.AddComponent<BoxCollider>();
            bc.isTrigger = false;
            bc.center = Vector3.zero;
            bc.size = new Vector3(1f, 0.1f, 1f);
        }
        else col.isTrigger = false;

        SetLayerRecursively(h.gameObject, squares[index].gameObject.layer);
        _highlights.Add(h);
    }

    public void ShowLegalMoveHighlightAtIndex(int index)
    {
        if (!IsValidIndex(index) || highlightPrefab == null) return;
        var pos = squares[index].position + new Vector3(-0.5f, highlightYOffset, +0.5f);
        var h = Instantiate(highlightPrefab, pos, highlightPrefab.transform.rotation, squares[index]);
        h.name = "Tutorial_LegalMove_Highlight_" + IndexToAlgebraic(index);

        var th = h.GetComponent<TutorialHighlight>() ?? h.AddComponent<TutorialHighlight>();
        th.index = index;
        th.manager = this;
        // NOTE: Do NOT add to _tutorialTargets - this is for legal moves, not tutorial targets

        var col = h.GetComponent<Collider>();
        if (col == null)
        {
            var bc = h.AddComponent<BoxCollider>();
            bc.isTrigger = false;
            bc.center = Vector3.zero;
            bc.size = new Vector3(1f, 0.1f, 1f);
        }
        else col.isTrigger = false;

        SetLayerRecursively(h.gameObject, squares[index].gameObject.layer);
        _highlights.Add(h);
    }

    public void ShowHighlightAtIndexLayer2(int index)
    {
        if (!IsValidIndex(index) || highlightPrefabLayer2 == null) return;
        var pos = squares[index].position + new Vector3(-0.5f, highlightYOffset, +0.5f);
        var h = Instantiate(highlightPrefabLayer2, pos, highlightPrefabLayer2.transform.rotation, squares[index]);
        h.name = "Tutorial_Highlight_Layer2_" + IndexToAlgebraic(index);

        var th = h.GetComponent<TutorialHighlight>() ?? h.AddComponent<TutorialHighlight>();
        th.index = index;
        th.manager = this;

        var col = h.GetComponent<Collider>();
        if (col == null)
        {
            var bc = h.AddComponent<BoxCollider>();
            bc.isTrigger = false;
            bc.center = Vector3.zero;
            bc.size = new Vector3(1f, 0.1f, 1f);
        }
        else col.isTrigger = false;

        SetLayerRecursively(h.gameObject, squares[index].gameObject.layer);
        _highlights.Add(h);
    }

    public void MoveTutorialRookToIndex(int index)
    {
        Debug.Log($"MoveTutorialRookToIndex called with index={index}");
        if (_rookInstance == null || !IsValidIndex(index))
        {
            Debug.Log("Invalid move attempt.");
            return;
        }

        var piece = _rookInstance.GetComponent<Piece>();
        if (piece == null) return;

        var legal = piece.GetLegalMovesWithCheckValidation(boardManager != null
            ? boardManager.boardPieces
            : new Piece[8, 8]);
        // Fallbacks: try raw Piece moves, then simple generator if prefab doesn't provide move logic
        if (legal == null || legal.Count == 0)
        {
            var raw = piece.GetLegalMoves(boardManager != null ? boardManager.boardPieces : new Piece[8,8]);
            if (raw != null && raw.Count > 0) legal = raw;
            else
            {
                legal = GenerateRookMoves(piece.position, piece.isWhite);
                Debug.LogWarning("Using generated rook moves as fallback for tutorial piece.");
            }
        }
        var target = new Vector2Int(index / 8, index % 8);
        if (!legal.Contains(target))
        {
            // Diagnostic logging: show why move considered illegal
            string legalStr = "";
            foreach (var m in legal) legalStr += IndexToAlgebraic(m.x * 8 + m.y) + ",";
            Debug.LogWarning($"Move not allowed to {IndexToAlgebraic(index)}. Piece at {IndexToAlgebraic(piece.position.x*8 + piece.position.y)}. Legal moves: {legalStr}");

            if (boardManager?.boardPieces != null)
            {
                var targetPiece = boardManager.boardPieces[target.x, target.y];
                var targetPieceType = targetPiece != null ? targetPiece.type.ToString() : "null";
                Debug.Log("Board at target " + IndexToAlgebraic(index) + " contains: " + targetPieceType);
            }
            return;
        }

        bool clearedAllCapturePawns = false;

        if (boardManager?.boardPieces != null)
        {
            var old = piece.position;
            boardManager.boardPieces[old.x, old.y] = null;
            var dest = boardManager.boardPieces[target.x, target.y];
            if (dest != null && dest != piece) boardManager.SendToSide(dest);
            boardManager.boardPieces[target.x, target.y] = piece;

            clearedAllCapturePawns = HandleCaptureTutorialCapture(dest);
        }

        var p = squares[index].position + rookPositionOffset;
        p.y = 0f;
        _rookInstance.transform.position = p;
        piece.position = target;
        piece.hasMoved = true;

        if (clearedAllCapturePawns)
        {
            ClearTutorial();
        }

        if (_tutorialTargets.Contains(index))
        {
            _visitedTargets.Add(index);
            if (_visitedTargets.Count == _tutorialTargets.Count) ClearTutorial();
        }

        Debug.Log($"Tutorial rook moved to {IndexToAlgebraic(index)}");
    }

    // Move tutorial bishop (same validation as rook)
    public void MoveTutorialBishopToIndex(int index)
    {
        Debug.Log($"MoveTutorialBishopToIndex called with index={index}");
        if (_bishopInstance == null || !IsValidIndex(index))
        {
            Debug.Log("Invalid move attempt.");
            return;
        }

        var piece = _bishopInstance.GetComponent<Piece>();
        if (piece == null) return;

        var legal = piece.GetLegalMovesWithCheckValidation(boardManager != null
            ? boardManager.boardPieces
            : new Piece[8, 8]);
        if ((legal == null || legal.Count == 0))
        {
            var raw = piece.GetLegalMoves(boardManager != null ? boardManager.boardPieces : new Piece[8,8]);
            if (raw != null && raw.Count > 0)
            {
                legal = raw;
                Debug.LogWarning("Using raw GetLegalMoves fallback for tutorial piece (check-validation returned empty).");
            }
            else
            {
                legal = GenerateBishopMoves(piece.position, piece.isWhite);
                Debug.LogWarning("Using generated bishop moves as fallback for tutorial piece.");
            }
        }
        var target = new Vector2Int(index / 8, index % 8);
        if (!legal.Contains(target))
        {
            // Diagnostic logging: show why move considered illegal
            string legalStr = "";
            foreach (var m in legal) legalStr += IndexToAlgebraic(m.x * 8 + m.y) + ",";
            Debug.LogWarning($"Move not allowed to {IndexToAlgebraic(index)}. Piece at {IndexToAlgebraic(piece.position.x*8 + piece.position.y)}. Legal moves: {legalStr}");

            if (boardManager?.boardPieces != null)
            {
                var targetPiece = boardManager.boardPieces[target.x, target.y];
                var targetPieceType = targetPiece != null ? targetPiece.type.ToString() : "null";
                Debug.Log("Board at target " + IndexToAlgebraic(index) + " contains: " + targetPieceType);
            }
            return;
        }

        if (boardManager?.boardPieces != null)
        {
            var old = piece.position;
            boardManager.boardPieces[old.x, old.y] = null;
            var dest = boardManager.boardPieces[target.x, target.y];
            if (dest != null && dest != piece) boardManager.SendToSide(dest);
            boardManager.boardPieces[target.x, target.y] = piece;
        }

        var p = squares[index].position + bishopPositionOffset;
        p.y = 0f;
        _bishopInstance.transform.position = p;
        piece.position = target;
        piece.hasMoved = true;

        if (_tutorialTargets.Contains(index))
        {
            _visitedTargets.Add(index);
            if (_visitedTargets.Count == _tutorialTargets.Count) ClearTutorial();
        }

        DeselectPiece(); // Reset selection after move
        Debug.Log($"Tutorial bishop moved to {IndexToAlgebraic(index)}");
    }

    public void MoveTutorialQueenToIndex(int index)
    {
        Debug.Log($"MoveTutorialQueenToIndex called with index={index}");
        if (_queenInstance == null || !IsValidIndex(index))
        {
            Debug.Log("Invalid move attempt.");
            return;
        }

        var piece = _queenInstance.GetComponent<Piece>();
        if (piece == null) return;

        var legal = piece.GetLegalMovesWithCheckValidation(boardManager != null
            ? boardManager.boardPieces
            : new Piece[8, 8]);
        if (legal == null || legal.Count == 0)
        {
            var raw = piece.GetLegalMoves(boardManager != null ? boardManager.boardPieces : new Piece[8,8]);
            if (raw != null && raw.Count > 0)
            {
                legal = raw;
                Debug.LogWarning("Using raw GetLegalMoves fallback for tutorial piece (check-validation returned empty).");
            }
            else
            {
                legal = GenerateQueenMoves(piece.position, piece.isWhite);
                Debug.LogWarning("Using generated queen moves as fallback for tutorial piece.");
            }
        }
        var target = new Vector2Int(index / 8, index % 8);
        if (!legal.Contains(target))
        {
            string legalStr = "";
            foreach (var m in legal) legalStr += IndexToAlgebraic(m.x * 8 + m.y) + ",";
            Debug.LogWarning($"Move not allowed to {IndexToAlgebraic(index)}. Piece at {IndexToAlgebraic(piece.position.x*8 + piece.position.y)}. Legal moves: {legalStr}");

            if (boardManager?.boardPieces != null)
            {
                var targetPiece = boardManager.boardPieces[target.x, target.y];
                var targetPieceType = targetPiece != null ? targetPiece.type.ToString() : "null";
                Debug.Log("Board at target " + IndexToAlgebraic(index) + " contains: " + targetPieceType);
            }
            return;
        }

        if (boardManager?.boardPieces != null)
        {
            var old = piece.position;
            boardManager.boardPieces[old.x, old.y] = null;
            var dest = boardManager.boardPieces[target.x, target.y];
            if (dest != null && dest != piece) boardManager.SendToSide(dest);
            boardManager.boardPieces[target.x, target.y] = piece;
        }

        var p = squares[index].position + queenPositionOffset;
        p.y = 0f;
        _queenInstance.transform.position = p;
        piece.position = target;
        piece.hasMoved = true;

        if (_tutorialTargets.Contains(index))
        {
            _visitedTargets.Add(index);
            if (_visitedTargets.Count == _tutorialTargets.Count) ClearTutorial();
        }

        DeselectPiece();
        Debug.Log($"Tutorial queen moved to {IndexToAlgebraic(index)}");
    }

    public void MoveTutorialKingToIndex(int index)
    {
        Debug.Log($"MoveTutorialKingToIndex called with index={index}");
        if (_kingInstance == null || !IsValidIndex(index))
        {
            Debug.Log("Invalid move attempt.");
            return;
        }

        var piece = _kingInstance.GetComponent<Piece>();
        if (piece == null) return;

        var old = piece.position;
        var target = new Vector2Int(index / 8, index % 8);
        bool isCastlingAttempt = old.x == target.x && Mathf.Abs(target.y - old.y) == 2;

        var legal = piece.GetLegalMovesWithCheckValidation(boardManager != null
            ? boardManager.boardPieces
            : new Piece[8, 8]);
        if (legal == null || legal.Count == 0)
        {
            var raw = piece.GetLegalMoves(boardManager != null ? boardManager.boardPieces : new Piece[8,8]);
            if (raw != null && raw.Count > 0)
            {
                legal = raw;
                Debug.LogWarning("Using raw GetLegalMoves fallback for tutorial piece (check-validation returned empty).");
            }
            else
            {
                legal = GenerateKingMoves(piece.position, piece.isWhite);
                Debug.LogWarning("Using generated king moves as fallback for tutorial piece.");
            }
        }

        // Keep castling available in tutorial mode even if the piece script omits it.
        if (isCastlingAttempt && CanCastleInTutorial(old, target, piece.isWhite) && !legal.Contains(target))
            legal.Add(target);

        if (!legal.Contains(target))
        {
            string legalStr = "";
            foreach (var m in legal) legalStr += IndexToAlgebraic(m.x * 8 + m.y) + ",";
            Debug.LogWarning($"Move not allowed to {IndexToAlgebraic(index)}. Piece at {IndexToAlgebraic(piece.position.x*8 + piece.position.y)}. Legal moves: {legalStr}");

            if (boardManager?.boardPieces != null)
            {
                var targetPiece = boardManager.boardPieces[target.x, target.y];
                var targetPieceType = targetPiece != null ? targetPiece.type.ToString() : "null";
                Debug.Log("Board at target " + IndexToAlgebraic(index) + " contains: " + targetPieceType);
            }
            return;
        }

        if (boardManager?.boardPieces != null)
        {
            boardManager.boardPieces[old.x, old.y] = null;
            var dest = boardManager.boardPieces[target.x, target.y];
            if (dest != null && dest != piece) boardManager.SendToSide(dest);
            boardManager.boardPieces[target.x, target.y] = piece;
        }

        var p = squares[index].position + kingPositionOffset;
        p.y = 0f;
        _kingInstance.transform.position = p;
        piece.position = target;
        piece.hasMoved = true;

        if (isCastlingAttempt)
        {
            MoveCastlingRook(old, target, piece.isWhite);
        }

        if (_tutorialTargets.Contains(index))
        {
            _visitedTargets.Add(index);
            if (_visitedTargets.Count == _tutorialTargets.Count) ClearTutorial();
        }

        DeselectPiece();
        Debug.Log($"Tutorial king moved to {IndexToAlgebraic(index)}");
    }

    public void MoveTutorialKnightToIndex(int index)
    {
        Debug.Log($"MoveTutorialKnightToIndex called with index={index}");
        if (_knightInstance == null || !IsValidIndex(index))
        {
            Debug.Log("Invalid move attempt.");
            return;
        }

        var piece = _knightInstance.GetComponent<Piece>();
        if (piece == null) return;

        var legal = piece.GetLegalMovesWithCheckValidation(boardManager != null
            ? boardManager.boardPieces
            : new Piece[8, 8]);
        if (legal == null || legal.Count == 0)
        {
            var raw = piece.GetLegalMoves(boardManager != null ? boardManager.boardPieces : new Piece[8,8]);
            if (raw != null && raw.Count > 0)
            {
                legal = raw;
                Debug.LogWarning("Using raw GetLegalMoves fallback for tutorial piece (check-validation returned empty).");
            }
            else
            {
                legal = GenerateKnightMoves(piece.position, piece.isWhite);
                Debug.LogWarning("Using generated knight moves as fallback for tutorial piece.");
            }
        }
        var target = new Vector2Int(index / 8, index % 8);
        if (!legal.Contains(target))
        {
            string legalStr = "";
            foreach (var m in legal) legalStr += IndexToAlgebraic(m.x * 8 + m.y) + ",";
            Debug.LogWarning($"Move not allowed to {IndexToAlgebraic(index)}. Piece at {IndexToAlgebraic(piece.position.x*8 + piece.position.y)}. Legal moves: {legalStr}");

            if (boardManager?.boardPieces != null)
            {
                var targetPiece = boardManager.boardPieces[target.x, target.y];
                var targetPieceType = targetPiece != null ? targetPiece.type.ToString() : "null";
                Debug.Log("Board at target " + IndexToAlgebraic(index) + " contains: " + targetPieceType);
            }
            return;
        }

        if (boardManager?.boardPieces != null)
        {
            var old = piece.position;
            boardManager.boardPieces[old.x, old.y] = null;
            var dest = boardManager.boardPieces[target.x, target.y];
            if (dest != null && dest != piece) boardManager.SendToSide(dest);
            boardManager.boardPieces[target.x, target.y] = piece;
        }

        var p = squares[index].position + knightPositionOffset;
        p.y = 0f;
        _knightInstance.transform.position = p;
        piece.position = target;
        piece.hasMoved = true;

        if (_tutorialTargets.Contains(index))
        {
            _visitedTargets.Add(index);
            if (_visitedTargets.Count == _tutorialTargets.Count) ClearTutorial();
        }

        DeselectPiece();
        Debug.Log($"Tutorial knight moved to {IndexToAlgebraic(index)}");
    }

    public void MoveTutorialPawnToIndex(int index)
    {
        Debug.Log($"MoveTutorialPawnToIndex called with index={index}");
        if (_pawnInstance == null || !IsValidIndex(index))
        {
            Debug.Log("Invalid move attempt.");
            return;
        }

        var piece = _pawnInstance.GetComponent<Piece>();
        if (piece == null) return;

        var legal = piece.GetLegalMovesWithCheckValidation(boardManager != null
            ? boardManager.boardPieces
            : new Piece[8, 8]);
        if (legal == null || legal.Count == 0)
        {
            var raw = piece.GetLegalMoves(boardManager != null ? boardManager.boardPieces : new Piece[8,8]);
            if (raw != null && raw.Count > 0)
            {
                legal = raw;
                Debug.LogWarning("Using raw GetLegalMoves fallback for tutorial piece (check-validation returned empty).");
            }
            else
            {
                legal = GeneratePawnMoves(piece.position, piece.isWhite, piece.hasMoved);
                Debug.LogWarning("Using generated pawn moves as fallback for tutorial piece.");
            }
        }
        var target = new Vector2Int(index / 8, index % 8);
        if (!legal.Contains(target))
        {
            string legalStr = "";
            foreach (var m in legal) legalStr += IndexToAlgebraic(m.x * 8 + m.y) + ",";
            Debug.LogWarning($"Move not allowed to {IndexToAlgebraic(index)}. Piece at {IndexToAlgebraic(piece.position.x*8 + piece.position.y)}. Legal moves: {legalStr}");

            if (boardManager?.boardPieces != null)
            {
                var targetPiece = boardManager.boardPieces[target.x, target.y];
                var targetPieceType = targetPiece != null ? targetPiece.type.ToString() : "null";
                Debug.Log("Board at target " + IndexToAlgebraic(index) + " contains: " + targetPieceType);
            }
            return;
        }

        if (boardManager?.boardPieces != null)
        {
            var old = piece.position;
            boardManager.boardPieces[old.x, old.y] = null;
            var dest = boardManager.boardPieces[target.x, target.y];
            if (dest != null && dest != piece) boardManager.SendToSide(dest);
            boardManager.boardPieces[target.x, target.y] = piece;

            // En passant capture: remove the captured pawn one rank behind the target
            if (boardManager.enPassantTarget.x >= 0 && target == boardManager.enPassantTarget)
            {
                int capturedRank = piece.isWhite ? target.x - 1 : target.x + 1;
                if (capturedRank >= 0 && capturedRank < 8)
                {
                    var capturedPawn = boardManager.boardPieces[capturedRank, target.y];
                    if (capturedPawn != null && capturedPawn.isWhite != piece.isWhite)
                    {
                        boardManager.boardPieces[capturedRank, target.y] = null;
                        boardManager.SendToSide(capturedPawn);
                    }
                }
            }
        }

        var p = squares[index].position + pawnPositionOffset;
        p.y = 0f;
        _pawnInstance.transform.position = p;

        piece.position = target;
        piece.hasMoved = true;

        // Check for pawn promotion (pawn reaches a8 - rank 8 for white)
        // a8 = file 0, rank 7 = position (7, 0)
        if (piece.type == PieceType.Pawn && target.x == 7 && target.y == 0)
        {
            // Pawn reached a8 - promote to queen
            Debug.Log("Pawn promoted to Queen at a8!");
            
            if (queenPrefab != null)
            {
                // Store the position where the pawn is
                var pawnPos = _pawnInstance.transform.position;
                var pawnRot = _pawnInstance.transform.rotation;
                
                // Destroy the pawn GameObject
                Destroy(_pawnInstance);
                
                // Instantiate the queen prefab at the same position
                _queenInstance = Instantiate(queenPrefab, pawnPos, pawnRot);
                _pawnInstance = null;
                
                // Set up the queen piece component
                var queenPiece = _queenInstance.GetComponent<Piece>();
                if (queenPiece == null)
                {
                    queenPiece = _queenInstance.AddComponent<Piece>();
                }
                
                // Ensure QueenPiece script is present
                var queenPieceScript = _queenInstance.GetComponent<QueenPiece>();
                if (queenPieceScript == null)
                {
                    queenPieceScript = _queenInstance.AddComponent<QueenPiece>();
                }
                
                if (queenPiece != null)
                {
                    queenPiece.position = target;
                    queenPiece.isWhite = true;
                    queenPiece.hasMoved = true;
                    queenPiece.type = PieceType.Queen;
                }
                
                // Add collider
                if (_queenInstance.GetComponent<Collider>() == null)
                {
                    var bc = _queenInstance.AddComponent<SphereCollider>();
                    bc.radius = 0.3f;
                    bc.isTrigger = false;
                }
                
                // Add click handler
                if (_queenInstance.GetComponent<TutorialPieceClickHandler>() == null)
                {
                    var handler = _queenInstance.AddComponent<TutorialPieceClickHandler>();
                    handler.manager = this;
                    handler.pieceIndex = index;
                }
                
                // Update board
                if (boardManager?.boardPieces != null)
                {
                    boardManager.boardPieces[target.x, target.y] = queenPiece;
                }
                
                // Show promotion panel
                ShowPromotionPanel();
            }
        }

        if (_tutorialTargets.Contains(index))
        {
            _visitedTargets.Add(index);
            if (_visitedTargets.Count == _tutorialTargets.Count) ClearTutorial();
        }

        // Remove all legal move highlights (keep only tutorial target highlights)
        var highlightsToRemove = new List<GameObject>();
        foreach (var h in _highlights)
        {
            if (h != null)
            {
                var th = h.GetComponent<TutorialHighlight>();
                if (th != null && !_tutorialTargets.Contains(th.index))
                {
                    // This is a legal move highlight, not a tutorial target - remove it
                    highlightsToRemove.Add(h);
                }
            }
        }
        
        foreach (var h in highlightsToRemove)
        {
            _highlights.Remove(h);
            Destroy(h);
        }

        DeselectPiece();
        Debug.Log($"Tutorial pawn moved to {IndexToAlgebraic(index)}");
    }

    // Generic entrypoint: move whichever tutorial piece is active (rook preferred)
    public void MoveTutorialPieceToIndex(int index)
    {
        // Clear legal move highlights before moving the piece
        ClearLegalMoveHighlights();
        
        // Deselect the piece
        DeselectPiece();
        
        if (_pawnInstance != null) MoveTutorialPawnToIndex(index);
        else if (_knightInstance != null) MoveTutorialKnightToIndex(index);
        else if (_kingInstance != null) MoveTutorialKingToIndex(index);
        else if (_queenInstance != null) MoveTutorialQueenToIndex(index);
        else if (_rookInstance != null) MoveTutorialRookToIndex(index);
        else if (_bishopInstance != null) MoveTutorialBishopToIndex(index);
        else Debug.Log("No tutorial piece present to move.");
    }

    void ClearTutorial()
    {
        if (_pendingClearRoutine != null) return;

        if (clearBoardDelaySeconds <= 0f)
        {
            ClearTutorialNow();
            return;
        }

        _pendingClearRoutine = StartCoroutine(ClearTutorialAfterDelay());
    }

    IEnumerator ClearTutorialAfterDelay()
    {
        yield return new WaitForSeconds(clearBoardDelaySeconds);
        _pendingClearRoutine = null;
        ClearTutorialNow();
    }

    void CancelPendingTutorialClear()
    {
        if (_pendingClearRoutine == null) return;
        StopCoroutine(_pendingClearRoutine);
        _pendingClearRoutine = null;
    }

    void ClearTutorialNow()
    {
        if (clearEntireBoardOnCompletion && boardManager != null) ClearEntireBoard();
        ClearHighlights();
        ClearRook();
        ClearBishop();
        ClearQueen();
        ClearKing();
        ClearKnight();
        ClearPawn();
        ClearCapturePawns();
        ClearStalemateSupportPieces();
        _tutorialTargets.Clear();
        _visitedTargets.Clear();
        _pieceSelected = false;

        SetCanvas(rookTargetCanvas, false);
        SetCanvas(bishopTargetCanvas, false);
        SetCanvas(queenTargetCanvas, false);
        SetCanvas(kingTargetCanvas, false);
        SetCanvas(knightTargetCanvas, false);
        SetCanvas(pawnTargetCanvas, false);
        SetCanvas(captureTargetCanvas, false);
        SetCanvas(stalemateTargetCanvas, false);
        SetCanvas(sourceCanvas, true);
        SetCanvas(castlingTargetCanvas, false);
        SetCanvas(enPassantTargetCanvas, false);
    }

    bool HandleCaptureTutorialCapture(Piece capturedPiece)
    {
        if (capturedPiece == null) return false;

        CleanupMissingCapturePawns();

        if (!_capturePawnInstances.Remove(capturedPiece.gameObject)) return false;

        CleanupMissingCapturePawns();

        return _capturePawnInstances.Count == 0;
    }

    void CleanupMissingCapturePawns()
    {
        for (int i = _capturePawnInstances.Count - 1; i >= 0; i--)
        {
            if (_capturePawnInstances[i] == null)
                _capturePawnInstances.RemoveAt(i);
        }
    }

    void ClearEntireBoard()
    {
        if (boardManager?.boardPieces == null) return;

        var rookPiece = _rookInstance != null ? _rookInstance.GetComponent<Piece>() : null;
        var bishopPiece = _bishopInstance != null ? _bishopInstance.GetComponent<Piece>() : null;
        var queenPiece = _queenInstance != null ? _queenInstance.GetComponent<Piece>() : null;
        var kingPiece = _kingInstance != null ? _kingInstance.GetComponent<Piece>() : null;
        var knightPiece = _knightInstance != null ? _knightInstance.GetComponent<Piece>() : null;
        var pawnPiece = _pawnInstance != null ? _pawnInstance.GetComponent<Piece>() : null;
        var stalemateSupportPieces = new HashSet<Piece>();
        foreach (var supportInstance in _stalemateSupportInstances)
        {
            if (supportInstance == null) continue;
            var supportPiece = supportInstance.GetComponent<Piece>();
            if (supportPiece != null) stalemateSupportPieces.Add(supportPiece);
        }

        for (int r = 0; r < 8; r++)
        for (int c = 0; c < 8; c++)
        {
            var p = boardManager.boardPieces[r, c];
            if (p == null) continue;

            if (p == rookPiece || p == bishopPiece || p == queenPiece || p == kingPiece || p == knightPiece || p == pawnPiece || stalemateSupportPieces.Contains(p))
            {
                boardManager.boardPieces[r, c] = null;
                if (p == rookPiece) Destroy(_rookInstance);
                if (p == bishopPiece) Destroy(_bishopInstance);
                if (p == queenPiece) Destroy(_queenInstance);
                if (p == kingPiece) Destroy(_kingInstance);
                if (p == knightPiece) Destroy(_knightInstance);
                if (p == pawnPiece) Destroy(_pawnInstance);
                if (stalemateSupportPieces.Contains(p)) Destroy(p.gameObject);
                continue;
            }

            boardManager.SendToSide(p);
            boardManager.boardPieces[r, c] = null;
        }

        _rookInstance = null;
        _bishopInstance = null;
        _queenInstance = null;
        _kingInstance = null;
        _knightInstance = null;
        _pawnInstance = null;
        _stalemateSupportInstances.Clear();

        SetCanvas(sourceCanvas, true);
        SetCanvas(rookTargetCanvas, false);
        SetCanvas(bishopTargetCanvas, false);
        SetCanvas(queenTargetCanvas, false);
        SetCanvas(kingTargetCanvas, false);
        SetCanvas(knightTargetCanvas, false);
        SetCanvas(pawnTargetCanvas, false);
        SetCanvas(captureTargetCanvas, false);
        SetCanvas(stalemateTargetCanvas, false);
        SetCanvas(castlingTargetCanvas, false);
        SetCanvas(enPassantTargetCanvas, false);
    }

    void InstantiateRookAtAlgebraic(string alg)
    {
        int idx = AlgebraicToIndex(alg);
        if (IsValidIndex(idx)) InstantiateRookAtIndex(idx);
    }

    void InstantiateRookAtIndex(int index)
    {
        if (!IsValidIndex(index) || rookPrefab == null) return;
        ClearRook();
        var pos = squares[index].position + rookPositionOffset;
        pos.y = 0f;
        _rookInstance = Instantiate(rookPrefab, pos, rookPrefab.transform.rotation);
        var piece = _rookInstance.GetComponent<Piece>();
        if (piece == null)
        {
            piece = _rookInstance.AddComponent<Piece>();
        }
        // Ensure RookPiece script is present for proper move generation
        var rookPiece = _rookInstance.GetComponent<RookPiece>();
        if (rookPiece == null)
        {
            rookPiece = _rookInstance.AddComponent<RookPiece>();
        }
        if (piece != null)
        {
            piece.position = new Vector2Int(index / 8, index % 8);
            piece.isWhite = true;
            piece.hasMoved = false;
        }
        if (boardManager?.boardPieces != null)
        {
            boardManager.boardPieces[index / 8, index % 8] = piece;
        }

        if (_rookInstance.GetComponent<Collider>() == null)
        {
            var bc = _rookInstance.AddComponent<SphereCollider>();
            bc.radius = 0.3f;
            bc.isTrigger = false;
        }

        if (_rookInstance.GetComponent<TutorialPieceClickHandler>() == null)
        {
            var handler = _rookInstance.AddComponent<TutorialPieceClickHandler>();
            handler.manager = this;
            handler.pieceIndex = index;
        }
    }

    void InstantiateBishopAtAlgebraic(string alg)
    {
        int idx = AlgebraicToIndex(alg);
        if (IsValidIndex(idx)) InstantiateBishopAtIndex(idx);
    }

    void InstantiateBishopAtIndex(int index)
    {
        if (!IsValidIndex(index) || bishopPrefab == null) return;
        ClearBishop();
        var pos = squares[index].position + bishopPositionOffset;
        pos.y = 0f;
        _bishopInstance = Instantiate(bishopPrefab, pos, bishopPrefab.transform.rotation);
        var piece = _bishopInstance.GetComponent<Piece>();
        if (piece != null)
        {
            piece.position = new Vector2Int(index / 8, index % 8);
            piece.isWhite = true;
            piece.hasMoved = false;
        }
        if (boardManager?.boardPieces != null)
        {
            boardManager.boardPieces[index / 8, index % 8] = piece;
        }

        if (_bishopInstance.GetComponent<Collider>() == null)
        {
            var bc = _bishopInstance.AddComponent<SphereCollider>();
            bc.radius = 0.3f;
            bc.isTrigger = false;
        }

        if (_bishopInstance.GetComponent<TutorialPieceClickHandler>() == null)
        {
            var handler = _bishopInstance.AddComponent<TutorialPieceClickHandler>();
            handler.manager = this;
            handler.pieceIndex = index;
        }
    }

    void InstantiateQueenAtAlgebraic(string alg)
    {
        int idx = AlgebraicToIndex(alg);
        if (IsValidIndex(idx)) InstantiateQueenAtIndex(idx);
    }

    void InstantiateQueenAtIndex(int index)
    {
        if (!IsValidIndex(index) || queenPrefab == null) return;
        ClearQueen();
        var pos = squares[index].position + queenPositionOffset;
        pos.y = 0f;
        _queenInstance = Instantiate(queenPrefab, pos, queenPrefab.transform.rotation);
        var piece = _queenInstance.GetComponent<Piece>();
        if (piece != null)
        {
            piece.position = new Vector2Int(index / 8, index % 8);
            piece.isWhite = true;
            piece.hasMoved = false;
        }
        if (boardManager?.boardPieces != null)
        {
            boardManager.boardPieces[index / 8, index % 8] = piece;
        }

        if (_queenInstance.GetComponent<Collider>() == null)
        {
            var bc = _queenInstance.AddComponent<SphereCollider>();
            bc.radius = 0.3f;
            bc.isTrigger = false;
        }

        if (_queenInstance.GetComponent<TutorialPieceClickHandler>() == null)
        {
            var handler = _queenInstance.AddComponent<TutorialPieceClickHandler>();
            handler.manager = this;
            handler.pieceIndex = index;
        }
    }

    void InstantiateKingAtAlgebraic(string alg)
    {
        int idx = AlgebraicToIndex(alg);
        if (IsValidIndex(idx)) InstantiateKingAtIndex(idx);
    }

    void InstantiateKingAtIndex(int index)
    {
        if (!IsValidIndex(index) || kingPrefab == null) return;
        ClearKing();
        var pos = squares[index].position + kingPositionOffset;
        pos.y = 0f;
        _kingInstance = Instantiate(kingPrefab, pos, kingPrefab.transform.rotation);
        var piece = _kingInstance.GetComponent<Piece>();
        if (piece != null)
        {
            piece.position = new Vector2Int(index / 8, index % 8);
            piece.isWhite = true;
            piece.hasMoved = false;
        }
        if (boardManager?.boardPieces != null)
        {
            boardManager.boardPieces[index / 8, index % 8] = piece;
        }

        if (_kingInstance.GetComponent<Collider>() == null)
        {
            var bc = _kingInstance.AddComponent<SphereCollider>();
            bc.radius = 0.3f;
            bc.isTrigger = false;
        }

        if (_kingInstance.GetComponent<TutorialPieceClickHandler>() == null)
        {
            var handler = _kingInstance.AddComponent<TutorialPieceClickHandler>();
            handler.manager = this;
            handler.pieceIndex = index;
        }
    }

    void InstantiateKnightAtAlgebraic(string alg)
    {
        int idx = AlgebraicToIndex(alg);
        if (IsValidIndex(idx)) InstantiateKnightAtIndex(idx);
    }

    void InstantiateKnightAtIndex(int index)
    {
        if (!IsValidIndex(index) || knightPrefab == null) return;
        ClearKnight();
        var pos = squares[index].position + knightPositionOffset;
        pos.y = 0f;
        _knightInstance = Instantiate(knightPrefab, pos, knightPrefab.transform.rotation);
        var piece = _knightInstance.GetComponent<Piece>();
        if (piece != null)
        {
            piece.position = new Vector2Int(index / 8, index % 8);
            piece.isWhite = true;
            piece.hasMoved = false;
        }
        if (boardManager?.boardPieces != null)
        {
            boardManager.boardPieces[index / 8, index % 8] = piece;
        }

        if (_knightInstance.GetComponent<Collider>() == null)
        {
            var bc = _knightInstance.AddComponent<SphereCollider>();
            bc.radius = 0.3f;
            bc.isTrigger = false;
        }

        if (_knightInstance.GetComponent<TutorialPieceClickHandler>() == null)
        {
            var handler = _knightInstance.AddComponent<TutorialPieceClickHandler>();
            handler.manager = this;
            handler.pieceIndex = index;
        }
    }

    void InstantiatePawnAtAlgebraic(string alg)
    {
        int idx = AlgebraicToIndex(alg);
        if (IsValidIndex(idx)) InstantiatePawnAtIndex(idx);
    }

    void InstantiatePawnAtIndex(int index)
    {
        if (!IsValidIndex(index) || pawnPrefab == null) return;
        ClearPawn();
        var pos = squares[index].position + pawnPositionOffset;
        pos.y = 0f;
        _pawnInstance = Instantiate(pawnPrefab, pos, pawnPrefab.transform.rotation);
        _pawnInstance.transform.localScale = new Vector3(32f, 32f, 32f);
        foreach (Transform child in _pawnInstance.transform)
        {
            child.localScale = new Vector3(32f, 32f, 32f);
        }

        var piece = _pawnInstance.GetComponent<Piece>();
        if (piece == null) piece = _pawnInstance.AddComponent<Piece>();

        if (_pawnInstance.GetComponent<PawnPiece>() == null)
            _pawnInstance.AddComponent<PawnPiece>();

        if (piece != null)
        {
            piece.position = new Vector2Int(index / 8, index % 8);
            piece.isWhite = true;
            piece.hasMoved = false;
            piece.type = PieceType.Pawn;
        }

        if (boardManager?.boardPieces != null)
        {
            boardManager.boardPieces[index / 8, index % 8] = piece;
        }

        if (_pawnInstance.GetComponent<Collider>() == null)
        {
            var bc = _pawnInstance.AddComponent<SphereCollider>();
            bc.radius = 0.3f;
            bc.isTrigger = false;
        }

        if (_pawnInstance.GetComponent<TutorialPieceClickHandler>() == null)
        {
            var handler = _pawnInstance.AddComponent<TutorialPieceClickHandler>();
            handler.manager = this;
            handler.pieceIndex = index;
        }
    }

    void InstantiateCapturePawnAtAlgebraic(string alg)
    {
        int idx = AlgebraicToIndex(alg);
        if (IsValidIndex(idx)) InstantiateCapturePawnAtIndex(idx);
    }

    void InstantiateStalemateRookAtAlgebraic(string alg)
    {
        int idx = AlgebraicToIndex(alg);
        if (IsValidIndex(idx)) InstantiateStalemateRookAtIndex(idx);
    }

    void InstantiateStalemateRookAtIndex(int index)
    {
        var prefab = boardManager != null && boardManager.whiteRook != null ? boardManager.whiteRook : rookPrefab;
        var rookInstance = InstantiateSupportPieceAtIndex(prefab, index, true, PieceType.Rook, rookPositionOffset);
        if (rookInstance != null) _stalemateSupportInstances.Add(rookInstance);
    }

    void InstantiateStalemateBlackKingAtAlgebraic(string alg)
    {
        int idx = AlgebraicToIndex(alg);
        if (IsValidIndex(idx)) InstantiateStalemateBlackKingAtIndex(idx);
    }

    void InstantiateStalemateBlackKingAtIndex(int index)
    {
        var prefab = blackKingPrefab != null ? blackKingPrefab : kingPrefab;
        var kingInstance = InstantiateSupportPieceAtIndex(prefab, index, false, PieceType.King, kingPositionOffset);
        if (kingInstance != null) _stalemateSupportInstances.Add(kingInstance);
    }

    GameObject InstantiateSupportPieceAtIndex(GameObject prefab, int index, bool isWhite, PieceType pieceType, Vector3 positionOffset)
    {
        if (!IsValidIndex(index) || prefab == null) return null;

        var pos = squares[index].position + positionOffset;
        pos.y = 0f;

        var instance = Instantiate(prefab, pos, prefab.transform.rotation);
        var piece = instance.GetComponent<Piece>();
        if (piece == null) piece = instance.AddComponent<Piece>();

        EnsurePieceBehavior(instance, pieceType);

        piece.position = new Vector2Int(index / 8, index % 8);
        piece.isWhite = isWhite;
        piece.hasMoved = pieceType != PieceType.King ? false : true;
        piece.type = pieceType;

        if (boardManager?.boardPieces != null)
        {
            var existing = boardManager.boardPieces[index / 8, index % 8];
            if (existing != null && existing != piece) boardManager.SendToSide(existing);
            boardManager.boardPieces[index / 8, index % 8] = piece;
        }

        if (instance.GetComponent<Collider>() == null)
        {
            var bc = instance.AddComponent<SphereCollider>();
            bc.radius = 0.3f;
            bc.isTrigger = false;
        }

        return instance;
    }

    void AddCastlingSupportPiece(string algebraic, bool isWhite, PieceType pieceType)
    {
        int index = AlgebraicToIndex(algebraic);
        if (!IsValidIndex(index)) return;

        var prefab = GetSupportPrefab(isWhite, pieceType);
        var instance = InstantiateSupportPieceAtIndex(prefab, index, isWhite, pieceType, GetPieceOffset(pieceType));
        if (instance != null) _stalemateSupportInstances.Add(instance);
    }

    GameObject GetSupportPrefab(bool isWhite, PieceType pieceType)
    {
        if (boardManager == null) return null;

        if (isWhite)
        {
            switch (pieceType)
            {
                case PieceType.Pawn: return boardManager.whitePawn != null ? boardManager.whitePawn : pawnPrefab;
                case PieceType.Rook: return boardManager.whiteRook != null ? boardManager.whiteRook : rookPrefab;
                case PieceType.Knight: return boardManager.whiteKnight != null ? boardManager.whiteKnight : knightPrefab;
                case PieceType.Bishop: return boardManager.whiteBishop != null ? boardManager.whiteBishop : bishopPrefab;
                case PieceType.Queen: return boardManager.whiteQueen != null ? boardManager.whiteQueen : queenPrefab;
                case PieceType.King: return boardManager.whiteKing != null ? boardManager.whiteKing : kingPrefab;
            }
        }
        else
        {
            switch (pieceType)
            {
                case PieceType.Pawn: return boardManager.blackPawn != null ? boardManager.blackPawn : capturePawnPrefab;
                case PieceType.Rook: return boardManager.blackRook;
                case PieceType.Knight: return boardManager.blackKnight;
                case PieceType.Bishop: return boardManager.blackBishop;
                case PieceType.Queen: return boardManager.blackQueen;
                case PieceType.King: return boardManager.blackKing != null ? boardManager.blackKing : blackKingPrefab;
            }
        }

        return null;
    }

    Vector3 GetPieceOffset(PieceType pieceType)
    {
        switch (pieceType)
        {
            case PieceType.Rook: return rookPositionOffset;
            case PieceType.Bishop: return bishopPositionOffset;
            case PieceType.Queen: return queenPositionOffset;
            case PieceType.King: return kingPositionOffset;
            case PieceType.Knight: return knightPositionOffset;
            case PieceType.Pawn: return pawnPositionOffset;
            default: return Vector3.zero;
        }
    }

    bool CanCastleInTutorial(Vector2Int kingFrom, Vector2Int kingTo, bool isWhite)
    {
        if (boardManager?.boardPieces == null) return false;
        if (kingFrom.x != kingTo.x || Mathf.Abs(kingTo.y - kingFrom.y) != 2) return false;

        int row = kingFrom.x;
        int rookCol = kingTo.y > kingFrom.y ? 7 : 0;
        int step = kingTo.y > kingFrom.y ? 1 : -1;

        var rook = boardManager.boardPieces[row, rookCol];
        if (rook == null || rook.type != PieceType.Rook || rook.isWhite != isWhite || rook.hasMoved) return false;

        for (int c = kingFrom.y + step; c != rookCol; c += step)
        {
            if (boardManager.boardPieces[row, c] != null) return false;
        }

        return true;
    }

    void MoveCastlingRook(Vector2Int kingFrom, Vector2Int kingTo, bool isWhite)
    {
        if (boardManager?.boardPieces == null || kingFrom.x != kingTo.x || Mathf.Abs(kingTo.y - kingFrom.y) != 2) return;

        int row = kingFrom.x;
        int rookFromCol = kingTo.y > kingFrom.y ? 7 : 0;
        int rookToCol = kingTo.y > kingFrom.y ? kingTo.y - 1 : kingTo.y + 1;

        var rookPiece = boardManager.boardPieces[row, rookFromCol];
        if (rookPiece == null || rookPiece.type != PieceType.Rook || rookPiece.isWhite != isWhite) return;

        boardManager.boardPieces[row, rookFromCol] = null;
        boardManager.boardPieces[row, rookToCol] = rookPiece;
        rookPiece.position = new Vector2Int(row, rookToCol);
        rookPiece.hasMoved = true;

        if (rookPiece.gameObject == null) return;

        int rookIndex = row * 8 + rookToCol;
        if (!IsValidIndex(rookIndex)) return;
        var rookPos = squares[rookIndex].position + rookPositionOffset;
        rookPos.y = 0f;
        rookPiece.transform.position = rookPos;
    }

    void EnsurePieceBehavior(GameObject pieceObject, PieceType pieceType)
    {
        if (pieceObject == null) return;

        switch (pieceType)
        {
            case PieceType.Rook:
                if (pieceObject.GetComponent<RookPiece>() == null) pieceObject.AddComponent<RookPiece>();
                break;
            case PieceType.Bishop:
                if (pieceObject.GetComponent<BishopPiece>() == null) pieceObject.AddComponent<BishopPiece>();
                break;
            case PieceType.Queen:
                if (pieceObject.GetComponent<QueenPiece>() == null) pieceObject.AddComponent<QueenPiece>();
                break;
            case PieceType.King:
                if (pieceObject.GetComponent<KingPiece>() == null) pieceObject.AddComponent<KingPiece>();
                break;
            case PieceType.Knight:
                if (pieceObject.GetComponent<KnightPiece>() == null) pieceObject.AddComponent<KnightPiece>();
                break;
            case PieceType.Pawn:
                if (pieceObject.GetComponent<PawnPiece>() == null) pieceObject.AddComponent<PawnPiece>();
                break;
        }
    }

    void InstantiateCapturePawnAtIndex(int index)
    {
        var capturePrefab = capturePawnPrefab != null ? capturePawnPrefab : pawnPrefab;
        if (!IsValidIndex(index) || capturePrefab == null) return;

        var pos = squares[index].position + pawnPositionOffset;
        pos.y = 0f;
        var pawnInstance = Instantiate(capturePrefab, pos, capturePrefab.transform.rotation);
        pawnInstance.transform.localScale = new Vector3(32f, 32f, 32f);
        foreach (Transform child in pawnInstance.transform)
        {
            child.localScale = new Vector3(32f, 32f, 32f);
        }

        var piece = pawnInstance.GetComponent<Piece>();
        if (piece == null) piece = pawnInstance.AddComponent<Piece>();

        if (pawnInstance.GetComponent<PawnPiece>() == null)
            pawnInstance.AddComponent<PawnPiece>();

        if (piece != null)
        {
            piece.position = new Vector2Int(index / 8, index % 8);
            piece.isWhite = false;
            piece.hasMoved = true;
            piece.type = PieceType.Pawn;
        }

        if (boardManager?.boardPieces != null)
        {
            var existing = boardManager.boardPieces[index / 8, index % 8];
            if (existing != null && existing != piece) boardManager.SendToSide(existing);
            boardManager.boardPieces[index / 8, index % 8] = piece;
        }

        if (pawnInstance.GetComponent<Collider>() == null)
        {
            var bc = pawnInstance.AddComponent<SphereCollider>();
            bc.radius = 0.3f;
            bc.isTrigger = false;
        }

        _capturePawnInstances.Add(pawnInstance);
    }

    int GetIndexFromTransform(Transform t)
    {
        if (t == null || squares == null || squares.Length != 64) return -1;
        for (int i = 0; i < 64; i++)
            if (squares[i] == t) return i;
        return -1;
    }

    void CreateHighlightsLayer2FromTransforms(Transform[] targets)
    {
        if (targets == null) return;
        _tutorialTargets.Clear();
        _visitedTargets.Clear();
        foreach (var t in targets)
        {
            int idx = GetIndexFromTransform(t);
            if (!IsValidIndex(idx)) continue;
            _tutorialTargets.Add(idx);
            ShowHighlightAtIndexLayer2(idx);
        }
    }

    int AlgebraicToIndex(string alg)
    {
        if (string.IsNullOrEmpty(alg) || alg.Length < 2) return -1;
        int file = char.ToLower(alg[0]) - 'a';
        int rank = alg[1] - '1';
        if (file < 0 || file > 7 || rank < 0 || rank > 7) return -1;
        return rank * 8 + file;
    }

    public void ClearHighlights()
    {
        foreach (var h in _highlights)
            if (h != null)
                Destroy(h);
        _highlights.Clear();
        _tutorialTargets.Clear();
        _visitedTargets.Clear();
    }

    public void ClearLegalMoveHighlights()
    {
        // Remove only the legal move highlights (those that are not tutorial targets)
        List<GameObject> toRemove = new List<GameObject>();
        foreach (var h in _highlights)
        {
            if (h != null)
            {
                var th = h.GetComponent<TutorialHighlight>();
                if (th != null && !_tutorialTargets.Contains(th.index))
                {
                    toRemove.Add(h);
                    Destroy(h);
                }
            }
        }
        
        // Remove destroyed highlights from the list
        foreach (var h in toRemove)  _highlights.Remove(h);
    }

    public void ClearRook()
    {
        if (_rookInstance == null) return;
        var piece = _rookInstance.GetComponent<Piece>();
        if (piece != null && boardManager?.boardPieces != null)
        {
            var r = piece.position.x;
            var c = piece.position.y;
            if (r >= 0 && r < 8 && c >= 0 && c < 8 && boardManager.boardPieces[r, c] == piece)
                boardManager.boardPieces[r, c] = null;
        }

        Destroy(_rookInstance);
        _rookInstance = null;
    }

    public void ClearBishop()
    {
        if (_bishopInstance == null) return;
        var piece = _bishopInstance.GetComponent<Piece>();
        if (piece != null && boardManager?.boardPieces != null)
        {
            var r = piece.position.x;
            var c = piece.position.y;
            if (r >= 0 && r < 8 && c >= 0 && c < 8 && boardManager.boardPieces[r, c] == piece)
                boardManager.boardPieces[r, c] = null;
        }

        Destroy(_bishopInstance);
        _bishopInstance = null;
    }

    public void ClearQueen()
    {
        if (_queenInstance == null) return;
        var piece = _queenInstance.GetComponent<Piece>();
        if (piece != null && boardManager?.boardPieces != null)
        {
            var r = piece.position.x;
            var c = piece.position.y;
            if (r >= 0 && r < 8 && c >= 0 && c < 8 && boardManager.boardPieces[r, c] == piece)
                boardManager.boardPieces[r, c] = null;
        }

        Destroy(_queenInstance);
        _queenInstance = null;
    }

    public void ClearKing()
    {
        if (_kingInstance == null) return;
        var piece = _kingInstance.GetComponent<Piece>();
        if (piece != null && boardManager?.boardPieces != null)
        {
            var r = piece.position.x;
            var c = piece.position.y;
            if (r >= 0 && r < 8 && c >= 0 && c < 8 && boardManager.boardPieces[r, c] == piece)
                boardManager.boardPieces[r, c] = null;
        }

        Destroy(_kingInstance);
        _kingInstance = null;
    }

    public void ClearKnight()
    {
        if (_knightInstance == null) return;
        var piece = _knightInstance.GetComponent<Piece>();
        if (piece != null && boardManager?.boardPieces != null)
        {
            var r = piece.position.x;
            var c = piece.position.y;
            if (r >= 0 && r < 8 && c >= 0 && c < 8 && boardManager.boardPieces[r, c] == piece)
                boardManager.boardPieces[r, c] = null;
        }

        Destroy(_knightInstance);
        _knightInstance = null;
    }

    public void ClearPawn()
    {
        if (_pawnInstance == null) return;
        var piece = _pawnInstance.GetComponent<Piece>();
        if (piece != null && boardManager?.boardPieces != null)
        {
            var r = piece.position.x;
            var c = piece.position.y;
            if (r >= 0 && r < 8 && c >= 0 && c < 8 && boardManager.boardPieces[r, c] == piece)
                boardManager.boardPieces[r, c] = null;
        }

        Destroy(_pawnInstance);
        _pawnInstance = null;
    }

    void ClearCapturePawns()
    {
        if (_capturePawnInstances.Count == 0) return;

        CleanupMissingCapturePawns();

        foreach (var pawn in _capturePawnInstances)
        {
            if (pawn == null) continue;

            var piece = pawn.GetComponent<Piece>();
            if (piece != null && boardManager?.boardPieces != null)
            {
                var r = piece.position.x;
                var c = piece.position.y;
                if (r >= 0 && r < 8 && c >= 0 && c < 8 && boardManager.boardPieces[r, c] == piece)
                    boardManager.boardPieces[r, c] = null;
            }

            Destroy(pawn);
        }

        _capturePawnInstances.Clear();
    }

    void ClearStalemateSupportPieces()
    {
        if (_stalemateSupportInstances.Count == 0) return;

        foreach (var pieceObject in _stalemateSupportInstances)
        {
            if (pieceObject == null) continue;

            var piece = pieceObject.GetComponent<Piece>();
            if (piece != null && boardManager?.boardPieces != null)
            {
                var r = piece.position.x;
                var c = piece.position.y;
                if (r >= 0 && r < 8 && c >= 0 && c < 8 && boardManager.boardPieces[r, c] == piece)
                    boardManager.boardPieces[r, c] = null;
            }

            Destroy(pieceObject);
        }

        _stalemateSupportInstances.Clear();
    }

    string IndexToAlgebraic(int index)
    {
        if (!IsValidIndex(index)) return string.Empty;
        int file = index % 8, rank = index / 8;
        return string.Concat((char)('a' + file), (char)('1' + rank));
    }

    bool IsValidIndex(int idx) =>
        squares != null && squares.Length == 64 && idx >= 0 && idx < 64 && squares[idx] != null;

    bool LocalPlayerWon(GameState state)
    {
        bool localIsWhite = ChessNetworkManager.LocalInstance != null
            ? ChessNetworkManager.LocalInstance.isWhitePlayer
            : true;
        return (state == GameState.WhiteWins && localIsWhite) || (state == GameState.BlackWins && !localIsWhite);
    }

    void SetLayerRecursively(GameObject go, int layer)
    {
        if (go == null) return;
        go.layer = layer;
        foreach (Transform t in go.transform) SetLayerRecursively(t.gameObject, layer);
    }

    void SetCanvas(Canvas c, bool active)
    {
        if (c == null) return;
        c.gameObject.SetActive(active);
    }

    List<Vector2Int> GenerateRookMoves(Vector2Int pos, bool isWhite)
    {
        var moves = new List<Vector2Int>();
        if (boardManager?.boardPieces == null) return moves;
        Vector2Int[] dirs = { new Vector2Int(1,0), new Vector2Int(-1,0), new Vector2Int(0,1), new Vector2Int(0,-1)};
        foreach (var d in dirs)
        {
            Vector2Int cur = pos + d;
            while (Piece.IsInBounds(cur))
            {
                var p = boardManager.boardPieces[cur.x, cur.y];
                if (p == null)  moves.Add(cur);
                else
                {
                    if (p.isWhite != isWhite) moves.Add(cur);
                    break;
                }
                cur += d;
            }
        }
        return moves;
    }

    List<Vector2Int> GenerateBishopMoves(Vector2Int pos, bool isWhite)
    {
        var moves = new List<Vector2Int>();
        if (boardManager?.boardPieces == null) return moves;
        Vector2Int[] dirs = { new Vector2Int(1,1), new Vector2Int(1,-1), new Vector2Int(-1,1), new Vector2Int(-1,-1)};
        foreach (var d in dirs)
        {
            Vector2Int cur = pos + d;
            while (Piece.IsInBounds(cur))
            {
                var p = boardManager.boardPieces[cur.x, cur.y];
                if (p == null)  moves.Add(cur);
                else
                {
                    if (p.isWhite != isWhite) moves.Add(cur);
                    break;
                }
                cur += d;
            }
        }
        return moves;
    }

    List<Vector2Int> GenerateQueenMoves(Vector2Int pos, bool isWhite)
    {
        var set = new HashSet<Vector2Int>();
        foreach (var m in GenerateRookMoves(pos, isWhite)) set.Add(m);
        foreach (var m in GenerateBishopMoves(pos, isWhite)) set.Add(m);
        return new List<Vector2Int>(set);
    }

    List<Vector2Int> GenerateKingMoves(Vector2Int pos, bool isWhite)
    {
        var moves = new List<Vector2Int>();
        if (boardManager?.boardPieces == null) return moves;
        
        // King moves one square in any direction
        for (int dx = -1; dx <= 1; dx++)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                if (dx == 0 && dy == 0) continue;
                
                Vector2Int target = new Vector2Int(pos.x + dx, pos.y + dy);
                if (Piece.IsInBounds(target))
                {
                    var p = boardManager.boardPieces[target.x, target.y];
                    if (p == null || p.isWhite != isWhite) moves.Add(target);
                }
            }
        }
        return moves;
    }

    List<Vector2Int> GenerateKnightMoves(Vector2Int pos, bool isWhite)
    {
        var moves = new List<Vector2Int>();
        if (boardManager?.boardPieces == null) return moves;
        
        // Knight moves in L-shape: 2 squares in one direction, 1 square perpendicular
        int[] dx = { 2, 2, -2, -2, 1, 1, -1, -1 };
        int[] dy = { 1, -1, 1, -1, 2, -2, 2, -2 };
        
        for (int i = 0; i < 8; i++)
        {
            Vector2Int target = new Vector2Int(pos.x + dx[i], pos.y + dy[i]);
            if (Piece.IsInBounds(target))
            {
                var p = boardManager.boardPieces[target.x, target.y];
                if (p == null || p.isWhite != isWhite)  moves.Add(target);
            }
        }
        return moves;
    }

    List<Vector2Int> GeneratePawnMoves(Vector2Int pos, bool isWhite, bool hasMoved)
    {
        var moves = new List<Vector2Int>();
        if (boardManager?.boardPieces == null) return moves;
        
        // Pawn moves forward (white goes up, black goes down)
        int direction = isWhite ? 1 : -1;
        
        // One square forward
        Vector2Int forward = new Vector2Int(pos.x + direction, pos.y);
        if (Piece.IsInBounds(forward) && boardManager.boardPieces[forward.x, forward.y] == null)
        {
            moves.Add(forward);
            
            // Two squares forward from starting position
            if (!hasMoved)
            {
                Vector2Int doubleForward = new Vector2Int(pos.x + 2 * direction, pos.y);
                if (Piece.IsInBounds(doubleForward) && boardManager.boardPieces[doubleForward.x, doubleForward.y] == null)
                {
                    moves.Add(doubleForward);
                }
            }
        }
        
        // Diagonal captures
        Vector2Int leftCapture = new Vector2Int(pos.x + direction, pos.y - 1);
        if (Piece.IsInBounds(leftCapture))
        {
            var p = boardManager.boardPieces[leftCapture.x, leftCapture.y];
            if (p != null && p.isWhite != isWhite) moves.Add(leftCapture);
        }
        
        Vector2Int rightCapture = new Vector2Int(pos.x + direction, pos.y + 1);
        if (Piece.IsInBounds(rightCapture))
        {
            var p = boardManager.boardPieces[rightCapture.x, rightCapture.y];
            if (p != null && p.isWhite != isWhite) moves.Add(rightCapture);
        }

        // En passant
        if (boardManager.enPassantTarget.x >= 0)
        {
            Vector2Int epLeft  = new Vector2Int(pos.x + direction, pos.y - 1);
            Vector2Int epRight = new Vector2Int(pos.x + direction, pos.y + 1);
            if (boardManager.enPassantTarget == epLeft)  moves.Add(epLeft);
            if (boardManager.enPassantTarget == epRight) moves.Add(epRight);
        }

        return moves;
    }
}

