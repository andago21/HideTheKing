using UnityEngine;
using System.Collections.Generic;
using System;

public class MoveNotation : MonoBehaviour
{
    public BoardManager boardManager;

    public List<string> moveHistory = new List<string>();
    public event Action OnMoveAdded;

    private void Awake()
    {
        moveHistory = new List<string>();
    }

    // Convert position to algebraic notation (e.g., e4, a1)
    public string PositionToAlgebraic(Vector2Int pos)
    {
        char file = (char)('a' + pos.y); // Column: a-h
        int rank = pos.x + 1; // Row: 1-8
        return file.ToString() + rank.ToString();
    }

    // Get piece symbol for notation
    private string GetPieceSymbol(PieceType type)
    {
        switch (type)
        {
            case PieceType.King: return "K";
            case PieceType.Queen: return "Q";
            case PieceType.Rook: return "R";
            case PieceType.Bishop: return "B";
            case PieceType.Knight: return "N";
            case PieceType.Pawn: return ""; // Pawns have no symbol
            default: return "";
        }
    }

    // Generate algebraic notation for a move
    public string GenerateMoveNotation(
        Piece piece,
        Vector2Int from,
        Vector2Int to,
        bool isCapture,
        bool isEnPassant,
        bool isCastling,
        bool isCheck,
        bool isCheckmate,
        PieceType promotionType = PieceType.Pawn)
    {
        string notation = "";
        // Castling
        if (isCastling)
        {
            int colDiff = to.y - from.y;
            notation = colDiff > 0 ? "O-O" : "O-O-O"; // Kingside or Queenside
        }
        else
        {
            // Piece symbol (empty for pawns)
            notation += GetPieceSymbol(piece.type);

            // For pawn captures, add starting file
            if (piece.type == PieceType.Pawn && isCapture) notation += (char)('a' + from.y);

            // Capture symbol
            if (isCapture || isEnPassant) notation += "x";

            notation += PositionToAlgebraic(to);

            if (isEnPassant) notation += " e.p.";

            // Promotion
            if (piece.type == PieceType.Pawn && (to.x == 7 || to.x == 0)) notation += "=" + GetPieceSymbol(promotionType);
        }

        // Check and checkmate
        if (isCheckmate) notation += "#";
        else if (isCheck) notation += "+";

        // 🔹 Always include from → to information for clarity
        notation += $" ({PositionToAlgebraic(from)}→{PositionToAlgebraic(to)})";

        return notation;
    }

    // Record a move in the move history
    public void RecordMove(string moveNotation, bool isWhiteMove)
    {
        string playerColor = isWhiteMove ? "White" : "Black";
        moveHistory.Add(moveNotation);

        // Trigger UI update
        OnMoveAdded?.Invoke();

        Debug.Log(playerColor + ": " + moveNotation);
    }

    // For checking threefold repetition (considers castling rights and en passant availability)
    public string GetBoardHash()
    {
        string hash = "";

        // Board position
        for (int row = 0; row < 8; row++)
        {
            for (int col = 0; col < 8; col++)
            {
                Piece piece = boardManager.boardPieces[row, col];
                if (piece == null) hash += "-";
                else
                {
                    // Format: ColorType (e.g., WP = white pawn, BK = black king)
                    char color = piece.isWhite ? 'W' : 'B';
                    char type = piece.type.ToString()[0];
                    hash += color.ToString() + type.ToString();
                }
                hash += ",";
            }
        }

        hash += boardManager.isWhiteTurn ? "W" : "B";
        hash += "|";

        // Include castling rights
        Piece whiteKing = boardManager.boardPieces[0, 4];
        Piece whiteKingsideRook = boardManager.boardPieces[0, 7];
        if (whiteKing != null && !whiteKing.hasMoved && whiteKingsideRook != null && !whiteKingsideRook.hasMoved)
            hash += "WK";

        Piece whiteQueensideRook = boardManager.boardPieces[0, 0];
        if (whiteKing != null && !whiteKing.hasMoved && whiteQueensideRook != null && !whiteQueensideRook.hasMoved)
            hash += "WQ";

        Piece blackKing = boardManager.boardPieces[7, 4];
        Piece blackKingsideRook = boardManager.boardPieces[7, 7];
        if (blackKing != null && !blackKing.hasMoved && blackKingsideRook != null && !blackKingsideRook.hasMoved)
            hash += "BK";

        Piece blackQueensideRook = boardManager.boardPieces[7, 0];
        if (blackKing != null && !blackKing.hasMoved && blackQueensideRook != null && !blackQueensideRook.hasMoved)
            hash += "BQ";

        hash += "|";

        // Include en passant target
        if (boardManager.enPassantTarget.x != -1 && boardManager.enPassantTarget.y != -1)
            hash += "EP:" + boardManager.enPassantTarget.x + "," + boardManager.enPassantTarget.y;
        else
            hash += "EP:none";

        return hash;
    }
}
