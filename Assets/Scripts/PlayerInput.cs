using UnityEngine;
using System.Collections.Generic;
using System.Collections;

// Vetem une edhe zoti e dim si funksionon ky kod, as ChatGPT as Claude asilloj AI nuk e zgjidh dot.
// Duhet me e transferu ne Clean Code
// Nqs e kupton ca nodh ktu ke aspirime per jeten
// Ragequit counter: 14

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
        {
            Debug.LogError("GameRules component not found on BoardManager!");
        }

        moveNotation = GetComponent<MoveNotation>();
        if (moveNotation == null)
        {
            Debug.LogError("MoveNotation component not found on BoardManager!");
        }
    }


    void Update()
    {
        // Don't allow input if game is over
        if (boardManager.gameState != GameState.Playing)
        {
            return;
        }

        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                Piece hitPiece = hit.transform.GetComponent<Piece>();
                if (hitPiece != null && selectedPiece == null && hitPiece.isWhite == boardManager.isWhiteTurn)
                {
                    selectedPiece = hitPiece;
                    //Debug.Log(selectedPiece.type + " has a Y of: " + selectedPiece.transform.position.y);
                    ShowPossibleMoves();
                    return;
                }

                if (selectedPiece != null)
                {
                    // Find target square (assuming hit is on square or piece; adjust if needed)
                    int targetIndex = GetSquareIndexFromHit(hit);
                    if (targetIndex != -1)
                    {
                        int targetRow = targetIndex / 8;
                        int targetCol = targetIndex % 8;
                        Vector2Int target = new Vector2Int(targetRow, targetCol);
                        if (selectedPiece.GetLegalMovesWithCheckValidation(boardManager.boardPieces).Contains(target))
                        {
                            MovePiece(target);
                            boardManager.isWhiteTurn = !boardManager.isWhiteTurn; // Switch turns
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
            {
                return i;
            }
        }
        return -1; // Not a square
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

    private void MovePiece(Vector2Int target)
    {
        Vector3 targetPos = boardManager.squares[target.x * 8 + target.y].position;
        Vector2Int originalPosition = selectedPiece.position;

        // Reset en passant from previous turn
        boardManager.enPassantTarget = new Vector2Int(-1, -1);

        // Check if en passant capture
        bool isEnPassant = false;
        if (selectedPiece.type == PieceType.Pawn)
        {
            int direction = selectedPiece.isWhite ? 1 : -1;
            // If pawn moves diagonally to an empty square, it's en passant
            if (target.y != selectedPiece.position.y && boardManager.boardPieces[target.x, target.y] == null)
            {
                isEnPassant = true;
                // The captured pawn is one row back
                Vector2Int capturedPawnPos = new Vector2Int(target.x - direction, target.y);
                Piece capturedPawn = boardManager.boardPieces[capturedPawnPos.x, capturedPawnPos.y];
                if (capturedPawn != null)
                {
                    // Move captured pawn to graveyard instead of destroying
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


        // Check if is castling
        bool isCastling = false;
        Piece rook = null;
        Vector2Int rookTarget = Vector2Int.zero;
        if (selectedPiece.type == PieceType.King && !selectedPiece.hasMoved)
        {
            int colDiff = target.y - selectedPiece.position.y;
            if (Mathf.Abs(colDiff) == 2)
            {
                isCastling = true;
                bool kingside = colDiff > 0;
                int rookCol = kingside ? 7 : 0;
                int rookTargetCol = kingside ? target.y - 1 : target.y + 1;
                
                rook = boardManager.boardPieces[selectedPiece.position.x, rookCol];
                rookTarget = new Vector2Int(selectedPiece.position.x, rookTargetCol);
                
                // Move the rook
                if (rook != null)
                {
                    boardManager.boardPieces[selectedPiece.position.x, rookCol] = null;
                    boardManager.boardPieces[rookTarget.x, rookTarget.y] = rook;
                    rook.position = rookTarget;
                    rook.hasMoved = true;
                    
                    Vector3 rookPos = boardManager.squares[rookTarget.x * 8 + rookTarget.y].position;
                    StartCoroutine(MoveAnimation(rook.transform, rookPos));
                }
                
                Debug.Log("Castling performed!");
            }
        }


        // Check if this move is a capture or pawn move (for fifty-move rule)
        Piece targetPiece = boardManager.boardPieces[target.x, target.y];
        bool isCapture = (targetPiece != null) || isEnPassant;
        bool isPawnMove = (selectedPiece.type == PieceType.Pawn);
        
        if (isCapture || isPawnMove)
        {
            gameRules.halfMoveClock = 0; // Reset counter
        }
        else
        {
            gameRules.halfMoveClock++; // Increment counter
        }


        // Capture
        if (targetPiece != null)
        {
            // Move captured target to graveyard
            if (captureEffectPrefab != null) Instantiate(captureEffectPrefab, targetPos, Quaternion.identity);
            boardManager.SendToSide(targetPiece);
        }
        else
        {
            if (moveEffectPrefab != null) Instantiate(moveEffectPrefab, targetPos, Quaternion.identity);
        }



        // Check if pawn moved two squares (enable en passant for next turn)
        if (selectedPiece.type == PieceType.Pawn)
        {
            int rowDiff = Mathf.Abs(target.x - selectedPiece.position.x);
            if (rowDiff == 2)
            {
                // Set en passant target square (the square the pawn "skipped over")
                int direction = selectedPiece.isWhite ? 1 : -1;
                boardManager.enPassantTarget = new Vector2Int(selectedPiece.position.x + direction, selectedPiece.position.y);
            }
        }


        // Update board
        boardManager.boardPieces[selectedPiece.position.x, selectedPiece.position.y] = null;
        boardManager.boardPieces[target.x, target.y] = selectedPiece;
        selectedPiece.position = target;
        selectedPiece.hasMoved = true; // Mark piece as moved


        // Check if this move will cause pawn promotion
        bool willPromote = false;
        if (selectedPiece.type == PieceType.Pawn)
        {
            int promotionRow = selectedPiece.isWhite ? 7 : 0;
            if (target.x == promotionRow)
            {
                willPromote = true;
            }
        }


        // Animate (or skip animation if promoting)
        if (willPromote)
        {
            // Move instantly without animation, then promote
            selectedPiece.transform.position = targetPos;
            PromotePawn(selectedPiece, target);
        }
        else
        {
            StartCoroutine(MoveAnimation(selectedPiece.transform, targetPos));
        }


        //--------- Record Move Notations --------------------

        bool isCheck = Piece.IsKingInCheck(boardManager.boardPieces, !boardManager.isWhiteTurn);
        bool isCheckmate = false;
        if (isCheck)
        {
            isCheckmate = Piece.IsCheckmate(boardManager.boardPieces, !boardManager.isWhiteTurn);
        }

        PieceType promotedTo = PieceType.Pawn;
        if (willPromote)
        {
            Piece promotedPiece = boardManager.boardPieces[target.x, target.y];
            if (promotedPiece != null)
            {
                promotedTo = promotedPiece.type;
            }
        }

        string notation = moveNotation.GenerateMoveNotation(
            selectedPiece,
            originalPosition,
            target,
            isCapture,
            isEnPassant,
            isCastling,
            isCheck,
            isCheckmate,
            promotedTo
        );

        moveNotation.RecordMove(notation, boardManager.isWhiteTurn);
        
        // Check all game-ending conditions
        gameRules.CheckGameEndConditions(boardManager.isWhiteTurn);
    }


    private IEnumerator MoveAnimation(Transform pieceTrans, Vector3 targetPos)
    {
        float duration = 0.5f;
        Vector3 start = pieceTrans.position;
        start.y = boardManager.transform.position.y; // Lock starting Y
        targetPos.y = boardManager.transform.position.y; // Lock target Y
        float elapsed = 0;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            Vector3 newPos = Vector3.Lerp(start, targetPos, elapsed / duration);
            newPos.y = boardManager.transform.position.y; // Enforce Y during lerp
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
        // Destroy the pawn
        Destroy(pawn.gameObject);

        // Randomly choose a piece type (Queen, Rook, Bishop, or Knight - no King or Pawn)
        PieceType[] promotionOptions = { PieceType.Queen, PieceType.Rook, PieceType.Bishop, PieceType.Knight };
        PieceType randomType = promotionOptions[Random.Range(0, promotionOptions.Length)];

        // Get the appropriate prefab based on color and random type
        GameObject promotionPrefab = null;
        switch (randomType)
        {
            case PieceType.Queen:
                promotionPrefab = pawn.isWhite ? boardManager.whiteQueen : boardManager.blackQueen;
                break;
            case PieceType.Rook:
                promotionPrefab = pawn.isWhite ? boardManager.whiteRook : boardManager.blackRook;
                break;
            case PieceType.Bishop:
                promotionPrefab = pawn.isWhite ? boardManager.whiteBishop : boardManager.blackBishop;
                break;
            case PieceType.Knight:
                promotionPrefab = pawn.isWhite ? boardManager.whiteKnight : boardManager.blackKnight;
                break;
        }

        // Create the new piece at the promotion position
        Vector3 pos = boardManager.squares[target.x * 8 + target.y].position;
        pos.y = promotionPrefab.transform.position.y;

        GameObject promotionObj = Instantiate(promotionPrefab, pos, promotionPrefab.transform.rotation);
        Piece promotionPiece = promotionObj.GetComponent<Piece>();

        if (promotionPiece != null)
        {
            promotionPiece.isWhite = pawn.isWhite;
            promotionPiece.type = randomType;
            promotionPiece.position = target;
            boardManager.boardPieces[target.x, target.y] = promotionPiece;
        }

        Debug.Log("Pawn promoted to " + randomType + "!");
    }
}