using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class PlayerInput : MonoBehaviour
{
    public BoardManager boardManager;
    public GameObject highlightPrefab;
    public GameObject moveEffectPrefab;
    public GameObject captureEffectPrefab;

    private Piece selectedPiece;
    private List<GameObject> highlights = new List<GameObject>();

    private GameRules gameRules;
    private MoveNotation moveNotation;

    void Start()
    {
        gameRules = GetComponent<GameRules>();
        if (gameRules == null)
            Debug.LogError("GameRules component not found on BoardManager!");

        moveNotation = GetComponent<MoveNotation>();
        if (moveNotation == null)
            Debug.LogError("MoveNotation component not found on BoardManager!");
    }

    void Update()
    {
        if (boardManager.gameState != GameState.Playing) return;

        if (TutorialManager.Instance != null && TutorialManager.Instance.TutorialActive) return;

        if (ChessNetworkManager.LocalInstance != null)
            if (!ChessNetworkManager.LocalInstance.IsMyTurn()) return;

        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                Piece hitPiece = hit.transform.GetComponent<Piece>();
                if (hitPiece != null && selectedPiece == null && hitPiece.isWhite == boardManager.isWhiteTurn)
                {
                    selectedPiece = hitPiece;
                    ShowPossibleMoves();
                    return;
                }

                if (selectedPiece != null)
                {
                    int targetIndex = GetSquareIndexFromHit(hit);
                    if (targetIndex != -1)
                    {
                        int targetRow = targetIndex / 8;
                        int targetCol = targetIndex % 8;
                        Vector2Int target = new Vector2Int(targetRow, targetCol);

                        if (selectedPiece.GetLegalMovesWithCheckValidation(boardManager.boardPieces).Contains(target))
                        {
                            bool battleStarted = MovePiece(selectedPiece.position, target);

                            // Turn nur umschalten wenn kein Battle Chess gestartet wurde
                            if (!battleStarted)
                                boardManager.isWhiteTurn = !boardManager.isWhiteTurn;
                        }
                    }
                    ClearSelection();
                }
            }
        }
    }

    private int GetSquareIndexFromHit(RaycastHit hit)
    {
        Transform hitTransform = hit.transform;
        for (int i = 0; i < boardManager.squares.Length; i++)
        {
            if (boardManager.squares[i] == hitTransform || boardManager.squares[i] == hitTransform.parent)
                return i;
        }
        return -1;
    }

    private void ShowPossibleMoves()
    {
        ClearHighlights();
        List<Vector2Int> moves = selectedPiece.GetLegalMovesWithCheckValidation(boardManager.boardPieces);
        foreach (var move in moves)
        {
            int index = move.x * 8 + move.y;
            Vector3 pos = boardManager.squares[index].position + new Vector3(-0.5f, highlightPrefab.transform.position.y, +0.5f);
            GameObject highlight = Instantiate(highlightPrefab, pos, Quaternion.Euler(0, 0, 0));
            highlights.Add(highlight);
        }
    }

    // Returns true if Battle Chess was started (turn switch handled by BattleChessManager)
    // Returns false if normal move (turn switch handled by Update)
    private bool MovePiece(Vector2Int from, Vector2Int to)
    {
        Piece piece = boardManager.boardPieces[from.x, from.y];
        if (piece == null)
        {
            Debug.LogWarning($"MovePiece called but no piece found at {from}");
            return false;
        }

        Vector3 targetPos = boardManager.squares[to.x * 8 + to.y].position;
        Vector2Int originalPosition = from;

        boardManager.enPassantTarget = new Vector2Int(-1, -1);

        bool isEnPassant = false;
        if (piece.type == PieceType.Pawn)
        {
            int direction = piece.isWhite ? 1 : -1;
            if (to.y != piece.position.y && boardManager.boardPieces[to.x, to.y] == null)
            {
                isEnPassant = true;
                Vector2Int capturedPawnPos = new Vector2Int(to.x - direction, to.y);
                Piece capturedPawn = boardManager.boardPieces[capturedPawnPos.x, capturedPawnPos.y];
                if (capturedPawn != null)
                {
                    boardManager.boardPieces[capturedPawnPos.x, capturedPawnPos.y] = null;
                    if (captureEffectPrefab != null)
                    {
                        Vector3 capturePos = boardManager.squares[capturedPawnPos.x * 8 + capturedPawnPos.y].position;
                        Instantiate(captureEffectPrefab, capturePos, Quaternion.identity);
                    }
                    boardManager.SendToSide(capturedPawn);
                }
            }
        }

        bool isCastling = false;
        Piece rook = null;
        Vector2Int rookTarget = Vector2Int.zero;
        if (piece.type == PieceType.King && !piece.hasMoved)
        {
            int colDiff = to.y - piece.position.y;
            if (Mathf.Abs(colDiff) == 2)
            {
                isCastling = true;
                bool kingside = colDiff > 0;
                int rookCol = kingside ? 7 : 0;
                int rookTargetCol = kingside ? to.y - 1 : to.y + 1;

                rook = boardManager.boardPieces[piece.position.x, rookCol];
                rookTarget = new Vector2Int(piece.position.x, rookTargetCol);

                if (rook != null)
                {
                    boardManager.boardPieces[piece.position.x, rookCol] = null;
                    boardManager.boardPieces[rookTarget.x, rookTarget.y] = rook;
                    rook.position = rookTarget;
                    rook.hasMoved = true;
                    Vector3 rookPos = boardManager.squares[rookTarget.x * 8 + rookTarget.y].position;
                    StartCoroutine(MoveAnimation(rook.transform, rookPos));
                }
                Debug.Log("Castling performed!");
            }
        }

        Piece targetPiece = boardManager.boardPieces[to.x, to.y];
        bool isCapture = (targetPiece != null) || isEnPassant;
        bool isPawnMove = (piece.type == PieceType.Pawn);

        if (isCapture || isPawnMove)
            gameRules.halfMoveClock = 0;
        else
            gameRules.halfMoveClock++;

        if (targetPiece != null)
        {
            bool useBattleChess = BattleChessManager.Instance != null &&
                                  ChessNetworkManager.LocalInstance != null &&
                                  ChessNetworkManager.LocalInstance.IsMultiplayer();

            if (useBattleChess)
            {
                // König nie im FPS — normale Capture
                bool kingInvolved = (piece.type == PieceType.King || targetPiece.type == PieceType.King);

                if (!kingInvolved)
                {
                    boardManager.enPassantTarget = new Vector2Int(-1, -1);
                    BattleChessManager.Instance.RequestBattle(piece, targetPiece);
                    ClearSelection();
                    return true; // Battle gestartet — Turn-Wechsel kommt von RpcEndBattle
                }
                // König beteiligt — normale Capture-Logik weiterlaufen lassen
            }

            if (captureEffectPrefab != null) Instantiate(captureEffectPrefab, targetPos, Quaternion.identity);
            boardManager.SendToSide(targetPiece);
        }
        else
        {
            if (moveEffectPrefab != null) Instantiate(moveEffectPrefab, targetPos, Quaternion.identity);
        }

        if (piece.type == PieceType.Pawn)
        {
            int rowDiff = Mathf.Abs(to.x - piece.position.x);
            if (rowDiff == 2)
            {
                int direction = piece.isWhite ? 1 : -1;
                boardManager.enPassantTarget = new Vector2Int(piece.position.x + direction, piece.position.y);
            }
        }

        boardManager.boardPieces[piece.position.x, piece.position.y] = null;
        boardManager.boardPieces[to.x, to.y] = piece;
        piece.position = to;
        piece.hasMoved = true;

        bool willPromote = false;
        if (piece.type == PieceType.Pawn)
        {
            int promotionRow = piece.isWhite ? 7 : 0;
            if (to.x == promotionRow)
                willPromote = true;
        }

        if (willPromote)
        {
            piece.transform.position = targetPos;
            PromotePawn(piece, to);
        }
        else
        {
            StartCoroutine(MoveAnimation(piece.transform, targetPos));
        }

        bool isCheck = Piece.IsKingInCheck(boardManager.boardPieces, !boardManager.isWhiteTurn);
        bool isCheckmate = false;
        if (isCheck)
            isCheckmate = Piece.IsCheckmate(boardManager.boardPieces, !boardManager.isWhiteTurn);

        PieceType promotedTo = PieceType.Pawn;
        if (willPromote)
        {
            Piece promotedPiece = boardManager.boardPieces[to.x, to.y];
            if (promotedPiece != null)
                promotedTo = promotedPiece.type;
        }

        string notation = moveNotation.GenerateMoveNotation(
            piece, originalPosition, to,
            isCapture, isEnPassant, isCastling,
            isCheck, isCheckmate, promotedTo
        );
        moveNotation.RecordMove(notation, boardManager.isWhiteTurn);

        if (ChessNetworkManager.LocalInstance != null && ChessNetworkManager.LocalInstance.IsMultiplayer())
        {
            Debug.Log("Sending move to network: " + originalPosition + " -> " + to);
            ChessNetworkManager.LocalInstance.SendMove(originalPosition, to);
        }

        gameRules.CheckGameEndConditions(boardManager.isWhiteTurn);

        if (ChessNetworkManager.LocalInstance != null && ChessNetworkManager.LocalInstance.IsMultiplayer())
        {
            if (boardManager.gameState != GameState.Playing)
                ChessNetworkManager.LocalInstance.SendGameEnd(boardManager.gameState);
        }

        return false;
    }

    private IEnumerator MoveAnimation(Transform pieceTrans, Vector3 targetPos)
    {
        float duration = 0.5f;
        Vector3 start = pieceTrans.position;
        start.y = boardManager.transform.position.y;
        targetPos.y = boardManager.transform.position.y;
        float elapsed = 0;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            Vector3 newPos = Vector3.Lerp(start, targetPos, elapsed / duration);
            newPos.y = boardManager.transform.position.y;
            pieceTrans.position = Vector3.Lerp(start, targetPos, elapsed / duration);
            yield return null;
        }
        pieceTrans.position = targetPos;
    }

    private void ClearSelection()
    {
        selectedPiece = null;
        ClearHighlights();
    }

    private void ClearHighlights()
    {
        foreach (var h in highlights) Destroy(h);
        highlights.Clear();
    }

    private void PromotePawn(Piece pawn, Vector2Int target)
    {
        if (Mirror.NetworkServer.active)
            Mirror.NetworkServer.Destroy(pawn.gameObject);
        else
            Destroy(pawn.gameObject);

        PieceType[] promotionOptions = { PieceType.Queen, PieceType.Rook, PieceType.Bishop, PieceType.Knight };
        PieceType randomType = promotionOptions[Random.Range(0, promotionOptions.Length)];

        GameObject promotionPrefab = null;
        switch (randomType)
        {
            case PieceType.Queen:  promotionPrefab = pawn.isWhite ? boardManager.whiteQueen  : boardManager.blackQueen;  break;
            case PieceType.Rook:   promotionPrefab = pawn.isWhite ? boardManager.whiteRook   : boardManager.blackRook;   break;
            case PieceType.Bishop: promotionPrefab = pawn.isWhite ? boardManager.whiteBishop : boardManager.blackBishop; break;
            case PieceType.Knight: promotionPrefab = pawn.isWhite ? boardManager.whiteKnight : boardManager.blackKnight; break;
        }

        Vector3 pos = boardManager.squares[target.x * 8 + target.y].position;
        pos.y = promotionPrefab.transform.position.y;

        GameObject promotionObj = Instantiate(promotionPrefab, pos, promotionPrefab.transform.rotation);
        Piece promotionPiece = promotionObj.GetComponent<Piece>();

        if (promotionPiece != null)
        {
            promotionPiece.isWhite  = pawn.isWhite;
            promotionPiece.type     = randomType;
            promotionPiece.position = target;
            boardManager.boardPieces[target.x, target.y] = promotionPiece;
        }

        if (Mirror.NetworkServer.active)
            Mirror.NetworkServer.Spawn(promotionObj);

        Debug.Log("Pawn promoted to " + randomType + "!");
    }

    public void ExecuteNetworkMove(Vector2Int from, Vector2Int to)
    {
        Debug.Log("Executing network move: " + from + " -> " + to);

        Piece pieceToMove = boardManager.boardPieces[from.x, from.y];
        if (pieceToMove == null)
        {
            Debug.Log("Piece already moved - this is normal in Host+Client mode");
            return;
        }

        MovePiece(from, to);
        boardManager.isWhiteTurn = !boardManager.isWhiteTurn;
        ClearSelection();
    }
}