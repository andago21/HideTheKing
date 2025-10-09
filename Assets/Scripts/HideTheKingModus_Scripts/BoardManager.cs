using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoardManager : MonoBehaviour
{
    public Transform[] squares; // 0-63, a1 to h8
    
    public GameObject whitePawn;
    public GameObject whiteRook;
    public GameObject whiteKnight;
    public GameObject whiteBishop;
    public GameObject whiteQueen;
    public GameObject whiteKing;
    
    public GameObject blackPawn;
    public GameObject blackRook;
    public GameObject blackKnight;
    public GameObject blackBishop;
    public GameObject blackQueen;
    public GameObject blackKing;

    public Piece[,] boardPieces = new Piece[8, 8];
    public bool isWhiteTurn = true; // White starts
    
    public Transform boardOrigin;   // bottom-left corner of a1
    public float tileSize = 1.0f;   // world units per square
    public bool xLeftToRight = true;   // file a->h along +X if true, else -X
    public bool zBottomToTop = true;   // rank 1->8 along +Z if true, else -Z
    public Transform squaresParent;    // optional; created if null

    private void Awake()
{
    Debug.Log("[BoardManager] Awake");

    // Ensure a BoardOrigin exists
    if (boardOrigin == null)
    {
        var originGO = GameObject.Find("BoardOrigin");
        if (originGO == null)
        {
            originGO = new GameObject("BoardOrigin");
            originGO.transform.position = Vector3.zero; // bottom-left of a1; move in scene later if needed
            Debug.LogWarning("[BoardManager] Created default BoardOrigin at (0,0,0). Assign/move it in the scene for exact placement.");
        }
        boardOrigin = originGO.transform;
    }

    // Build the grid if it isn't already valid
    if (!SquaresArrayIsValid())
    {
        BuildSquaresGrid();
    }
}

private void Start()
{
    // Double-check at Start (some objects can be toggled active between Awake and Start)
    if (!SquaresArrayIsValid())
    {
        Debug.LogWarning("[BoardManager] Squares invalid at Start. Rebuilding grid now.");
        BuildSquaresGrid();

        if (!SquaresArrayIsValid())
        {
            Debug.LogError("[BoardManager] Squares still invalid after rebuild. Aborting SetupBoard().");
            return;
        }
    }

    SetupBoard(); // now safe
}

private bool SquaresArrayIsValid()
{
    if (squares == null || squares.Length != 64) return false;
    for (int i = 0; i < squares.Length; i++)
    {
        if (squares[i] == null) return false;
    }
    return true;
}

private void BuildSquaresGrid()
{
    if (boardOrigin == null)
    {
        Debug.LogError("[BoardManager] boardOrigin is NULL. Cannot build squares.");
        return;
    }

    if (squaresParent == null)
    {
        squaresParent = new GameObject("Squares").transform;
        squaresParent.SetParent(transform, worldPositionStays: true);
    }
    if (!squaresParent.gameObject.activeInHierarchy)
        squaresParent.gameObject.SetActive(true);

    if (tileSize <= 0f)
    {
        tileSize = 1f;
        Debug.LogWarning("[BoardManager] tileSize was <= 0. Reset to 1.");
    }

    squares = new Transform[64];

    Vector3 xStep = (xLeftToRight ? Vector3.right : -Vector3.right) * tileSize;
    Vector3 zStep = (zBottomToTop ? Vector3.forward : -Vector3.forward) * tileSize;

    for (int row = 0; row < 8; row++)
    for (int col = 0; col < 8; col++)
    {
        int idx = row * 8 + col;

        var go = new GameObject($"Square_{row}_{col}");
        go.transform.SetParent(squaresParent, worldPositionStays: false);
        go.transform.position = boardOrigin.position + (xStep * col) + (zStep * row);

        var bc = go.AddComponent<BoxCollider>();
        bc.size = new Vector3(tileSize, 0.05f, tileSize);

        squares[idx] = go.transform;
    }

    Debug.Log("[BoardManager] Squares grid generated (8x8).");
}


    void SetupBoard()
    {
        // White Pawns (row 1)
        for (int i = 0; i < 8; i++)
        {
            int index = 8 + i; // 8-15
            SetupPiece(whitePawn, true, PieceType.Pawn, index);
        }

        // Black Pawns (row 6)
        for (int i = 0; i < 8; i++)
        {
            int index = 48 + i; // 48-55
            SetupPiece(blackPawn, false, PieceType.Pawn, index);
        }

        // White Rooks
        SetupPiece(whiteRook, true, PieceType.Rook, 0); // a1
        SetupPiece(whiteRook, true, PieceType.Rook, 7); // h1

        // Black Rooks
        SetupPiece(blackRook, false, PieceType.Rook, 56); // a8
        SetupPiece(blackRook, false, PieceType.Rook, 63); // h8

        // White Knights
        SetupPiece(whiteKnight, true, PieceType.Knight, 1); // b1
        SetupPiece(whiteKnight, true, PieceType.Knight, 6); // g1

        // Black Knights
        SetupPiece(blackKnight, false, PieceType.Knight, 57); // b8
        SetupPiece(blackKnight, false, PieceType.Knight, 62); // g8

        // White Bishops
        SetupPiece(whiteBishop, true, PieceType.Bishop, 2); // c1
        SetupPiece(whiteBishop, true, PieceType.Bishop, 5); // f1

        // Black Bishops
        SetupPiece(blackBishop, false, PieceType.Bishop, 58); // c8
        SetupPiece(blackBishop, false, PieceType.Bishop, 61); // f8

        // White Queen
        SetupPiece(whiteQueen, true, PieceType.Queen, 3); // d1

        // Black Queen
        SetupPiece(blackQueen, false, PieceType.Queen, 59); // d8

        // White King
        SetupPiece(whiteKing, true, PieceType.King, 4); // e1

        // Black King
        SetupPiece(blackKing, false, PieceType.King, 60); // e8
    }


    private void SetupPiece(GameObject prefab, bool isWhitePiece, PieceType pieceType, int index)
    {
        int row = index / 8;   // 0–7 internally
        int col = index % 8;   // 0–7 internally

        Vector3 basePos = squares[index].position;

        // Remove any pawn offset (we keep all at same Y height)
        GameObject pieceObj = Instantiate(prefab, basePos, Quaternion.identity, squares[index]);
        Vector3 p = pieceObj.transform.position;
        pieceObj.transform.position = new Vector3(p.x, basePos.y, p.z);

        Piece piece = pieceObj.GetComponent<Piece>();
        if (piece != null)
        {
            piece.isWhite = isWhitePiece;
            piece.type = pieceType;
            piece.position = new Vector2Int(row, col);
            boardPieces[row, col] = piece;

            // 🟢 Print board coordinates in 1–8 system
            int xBoard = row + 1; // human-readable (1–8)
            int zBoard = col + 1;
            string color = isWhitePiece ? "White" : "Black";

            Debug.Log($"{color} {pieceType} → (x = {xBoard}, z = {zBoard})");
        }
        else
        {
            Debug.LogError($"Piece component missing on {prefab.name}");
        }
    }



}