using System.Collections.Generic;
using UnityEngine;

public class TutorialManager : MonoBehaviour
{
    public Transform[] squares;
    public BoardManager boardManager;

    public GameObject rookPrefab;
    public GameObject highlightPrefab;

    public float highlightYOffset = 0.01f;

    public bool clearEntireBoardOnCompletion = true;

    public Canvas sourceCanvas;
    public Canvas targetCanvas;

    GameObject _rookInstance;
    readonly List<GameObject> _highlights = new List<GameObject>();
    readonly HashSet<int> _tutorialTargets = new HashSet<int>();
    readonly HashSet<int> _visitedTargets = new HashSet<int>();

    GameState _lastGameState = GameState.Playing;

    void Start()
    {
        if (boardManager == null) boardManager = FindObjectOfType<BoardManager>();
        if (boardManager != null) _lastGameState = boardManager.gameState;
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
            SetCanvas(targetCanvas, false);
        }
        _lastGameState = state;
    }

    public void OnRookButton()
    {
        SetCanvas(sourceCanvas, false);
        SetCanvas(targetCanvas, true);

        if (boardManager == null) boardManager = FindObjectOfType<BoardManager>();
        if ((squares == null || squares.Length != 64) && boardManager != null) squares = boardManager.squares;

        InstantiateRookAtAlgebraic("d4");
        ClearHighlights();

        var targets = new[] { "a4", "g4", "g8" };
        if (boardManager != null)
        {
            int from = AlgebraicToIndex("d4");
            foreach (var t in targets)
            {
                int to = AlgebraicToIndex(t);
                if (IsValidIndex(to)) ClearPathBetweenIndices(from, to);
            }
        }
        CreateHighlightsFromAlgebraic(targets);
    }

    void ClearPathBetweenIndices(int fromIndex, int toIndex)
    {
        if (boardManager?.boardPieces == null) return;
        int fr = fromIndex / 8, fc = fromIndex % 8, tr = toIndex / 8, tc = toIndex % 8;
        if (fr != tr && fc != tc) return; // not straight
        int dr = tr == fr ? 0 : (tr > fr ? 1 : -1);
        int dc = tc == fc ? 0 : (tc > fc ? 1 : -1);
        int r = fr + dr, c = fc + dc;
        while (r != tr || c != tc)
        {
            var p = boardManager.boardPieces[r, c];
            if (p != null)
            {
                boardManager.SendToSide(p);
                boardManager.boardPieces[r, c] = null;
            }

            r += dr;
            c += dc;
        }

        var dest = boardManager.boardPieces[tr, tc];
        if (dest != null)
        {
            boardManager.SendToSide(dest);
            boardManager.boardPieces[tr, tc] = null;
        }
    }

    public void InstantiateRookAtAlgebraic(string algebraic) => InstantiateRookAtIndex(AlgebraicToIndex(algebraic));

    public void InstantiateRookAtIndex(int index)
    {
        if (!IsValidIndex(index))
        {
            Debug.LogError($"TutorialManager: invalid index {index}.");
            return;
        }

        if (rookPrefab == null)
        {
            Debug.LogError("TutorialManager: rookPrefab is not assigned.");
            return;
        }

        ClearRook();

        var pos = squares[index].position;
        pos.y = rookPrefab.transform.position.y;
        _rookInstance = Instantiate(rookPrefab, pos, rookPrefab.transform.rotation);
        _rookInstance.name = "Tutorial_Rook";

        var piece = _rookInstance.GetComponent<Piece>();
        if (piece == null)
        {
            Debug.LogWarning("TutorialManager: rook prefab missing Piece component.");
            return;
        }

        piece.position = new Vector2Int(index / 8, index % 8);
        piece.type = PieceType.Rook;
        piece.isWhite = boardManager != null ? boardManager.isWhiteTurn : true;
        piece.hasMoved = false;
        piece.enabled = true;

        if (_rookInstance.GetComponent<Collider>() == null)
        {
            var rend = _rookInstance.GetComponentInChildren<Renderer>();
            var bc = _rookInstance.AddComponent<BoxCollider>();
            if (rend != null)
            {
                bc.center = _rookInstance.transform.InverseTransformPoint(rend.bounds.center);
                bc.size = rend.bounds.size;
            }
            else
            {
                bc.center = Vector3.zero;
                bc.size = Vector3.one * 0.5f;
            }

            bc.isTrigger = false;
        }

        if (boardManager?.boardPieces != null)
        {
            var r = piece.position.x;
            var c = piece.position.y;
            var existing = boardManager.boardPieces[r, c];
            if (existing != null && existing != piece)
            {
                boardManager.SendToSide(existing);
                boardManager.boardPieces[r, c] = null;
            }

            boardManager.boardPieces[r, c] = piece;
        }
    }

    public void CreateHighlightsFromAlgebraic(string[] algebraics)
    {
        if (algebraics == null) return;
        _tutorialTargets.Clear();
        _visitedTargets.Clear();
        foreach (var a in algebraics)
        {
            int idx = AlgebraicToIndex(a);
            if (IsValidIndex(idx)) ShowHighlightAtIndex(idx);
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
        var target = new Vector2Int(index / 8, index % 8);
        if (!legal.Contains(target))
        {
            Debug.Log("Move not allowed");
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

        var p = squares[index].position;
        p.y = _rookInstance.transform.position.y;
        _rookInstance.transform.position = p;
        piece.position = target;
        piece.hasMoved = true;

        if (_tutorialTargets.Contains(index))
        {
            _visitedTargets.Add(index);
            if (_visitedTargets.Count == _tutorialTargets.Count) ClearTutorial();
        }

        Debug.Log($"Tutorial rook moved to {IndexToAlgebraic(index)}");
    }

    void ClearTutorial()
    {
        if (clearEntireBoardOnCompletion && boardManager != null) ClearEntireBoard();
        ClearHighlights();
        ClearRook();
        _tutorialTargets.Clear();
        _visitedTargets.Clear();
    }

    void ClearEntireBoard()
    {
        if (boardManager?.boardPieces == null) return;
        for (int r = 0; r < 8; r++)
        for (int c = 0; c < 8; c++)
        {
            var p = boardManager.boardPieces[r, c];
            if (p != null)
            {
                boardManager.SendToSide(p);
                boardManager.boardPieces[r, c] = null;
            }
        }

        SetCanvas(sourceCanvas, true);
        SetCanvas(targetCanvas, false);
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

    int AlgebraicToIndex(string alg)
    {
        if (string.IsNullOrEmpty(alg) || alg.Length < 2) return -1;
        int file = char.ToLower(alg[0]) - 'a';
        int rank = alg[1] - '1';
        if (file < 0 || file > 7 || rank < 0 || rank > 7) return -1;
        return rank * 8 + file;
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
}