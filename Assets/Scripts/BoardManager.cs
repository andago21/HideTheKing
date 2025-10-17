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

    void Start()
    {
        // Ensure squares are aligned to X-Z plane
        for (int i = 0; i < squares.Length; i++)
        {
            Vector3 pos = squares[i].position;
            pos.y = transform.position.y; // Lock Y to board height
            squares[i].position = pos;
        }
        SetupBoard();
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
        int row = index / 8;
        int col = index % 8;
        Vector3 pos = squares[index].position;
        pos.y = prefab.transform.position.y;

        // Instantiate without parenting to squares[index]
        GameObject pieceObj = Instantiate(prefab, pos, prefab.transform.rotation); // No parent specified
        Piece piece = pieceObj.GetComponent<Piece>();
        if (piece != null)
        {
            piece.isWhite = isWhitePiece;
            piece.type = pieceType;
            piece.position = new Vector2Int(row, col);
            boardPieces[row, col] = piece;
        }
        else
        {
            Debug.LogError($"Piece component missing on {prefab.name}");
        }
    }
}