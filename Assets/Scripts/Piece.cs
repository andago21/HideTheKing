using UnityEngine;
using System.Collections.Generic;
using HideTheKing.Core;

public class Piece : MonoBehaviour
{
    public bool isWhite; // true for white pieces, false for black
    public PieceType type; // Enum for pawn, rook, etc.
    public Vector2Int position; // Column (0-7 from bottom to top), Row (0-7 from a to h)
    public bool hasMoved; // Track if piece has moved (for castling)


    /* 
        Implementation at the bottom:
        Insufficient -> neither player has enough pieces to deliver checkmate. This results in an automatic draw. The main cases are:

        King vs King
        King + Bishop vs King
        King + Knight vs King
        King + Bishop vs King + Bishop (with both bishops on same color squares)
    */


    // This method will be overridden by specific piece scripts to calculate moves
    public virtual List<Vector2Int> GetLegalMoves(Piece[,] board)
    {
        return new List<Vector2Int>();
    }

    public List<Vector2Int> GetLegalMovesWithCheckValidation(Piece[,] board)
    {
        List<Vector2Int> potentialMoves = GetLegalMoves(board);
        List<Vector2Int> legalMoves = new List<Vector2Int>();
        
        foreach (Vector2Int move in potentialMoves)
        {
            // Simulate the move
            Piece capturedPiece = board[move.x, move.y];
            board[position.x, position.y] = null;
            board[move.x, move.y] = this;
            Vector2Int oldPos = position;
            position = move;
            
            // Check if this move leaves our king in check
            bool wouldBeInCheck = IsKingInCheck(board, isWhite);
            
            // Undo the move
            position = oldPos;
            board[oldPos.x, oldPos.y] = this;
            board[move.x, move.y] = capturedPiece;
            
            if (!wouldBeInCheck) legalMoves.Add(move);
        }
        
        return legalMoves;
    }
    
    public List<Vector2Int> GetLegalMovesHtk(Piece[,] board)
    {
        var htkManager = HideTheKingManager.Instance;
        if (htkManager != null)
        {
            return htkManager.GetLegalMovesHTK(this, board);
        }
        // Fallback: if manager is not available, return base moves
        return GetLegalMovesWithCheckValidation(board);
    }
    
    public static bool IsInBounds(Vector2Int pos)
    {
        return pos.x >= 0 && pos.x < 8 && pos.y >= 0 && pos.y < 8;
    }

    public static bool IsKingInCheck(Piece[,] board, bool isWhiteKing)
    {
        // In HideTheKing mode, only the hidden king is protected; other kings can be captured like normal pieces
        if (HideTheKingManager.HideTheKingMode)
        {
            var htkManager = HideTheKingManager.Instance;
            if (htkManager != null)
            {
                var state = htkManager.GetHiddenState(isWhiteKing);
                var hiddenTarget = state?.HiddenTarget;
                if (hiddenTarget == null || !hiddenTarget.enabled) return false;
                Vector2Int hiddenKingPos = hiddenTarget.position;
                foreach (Piece piece in board)
                {
                    if (piece != null && piece.isWhite != isWhiteKing)
                    {
                        List<Vector2Int> opponentMoves = piece.GetLegalMoves(board);
                        if (opponentMoves.Contains(hiddenKingPos))
                        {
                            return true;
                        }
                    }
                }
                return false;
            }
        }
        Vector2Int kingPos = FindKing(board, isWhiteKing);
        if (kingPos.x == -1) return false; // King not found (error)
        foreach (Piece piece in board)
        {
            if (piece != null && piece.isWhite != isWhiteKing)
            {
                List<Vector2Int> opponentMoves = piece.GetLegalMoves(board);
                if (opponentMoves.Contains(kingPos))
                {
                    return true; // King is attacked by this piece
                }
            }
        }
        return false; // No attacks found
    }

    private static Vector2Int FindKing(Piece[,] board, bool isWhite)
    {
        for (int row = 0; row < 8; row++)
        {
            for (int col = 0; col < 8; col++)
            {
                Piece piece = board[row, col];
                if (piece != null && piece.type == PieceType.King && piece.isWhite == isWhite)
                {
                    return new Vector2Int(row, col);
                }
            }
        }
        return new Vector2Int(-1, -1); // King not found (error)
    }

    public static bool IsCheckmate(Piece[,] board, bool isWhiteKing)
    {
        // In HideTheKing mode, only the hidden king is protected; other kings can be captured like normal pieces
        if (HideTheKingManager.HideTheKingMode)
        {
            var htkManager = HideTheKingManager.Instance;
            if (htkManager != null)
            {
                var state = htkManager.GetHiddenState(isWhiteKing);
                var hiddenTarget = state?.HiddenTarget;
                if (hiddenTarget == null || !hiddenTarget.enabled) return false;
                // Check if hidden king is in check
                if (!IsKingInCheck(board, isWhiteKing)) return false;
                // Check if any piece for this player has any legal moves (under HTK rules) that would avoid check
                for (int row = 0; row < 8; row++)
                {
                    for (int col = 0; col < 8; col++)
                    {
                        Piece p = board[row, col];
                        if (p != null && p.isWhite == isWhiteKing && p.enabled)
                        {
                            List<Vector2Int> legalMoves = htkManager.GetLegalMovesHTK(p, board);
                            if (legalMoves != null && legalMoves.Count > 0)
                            {
                                return false; // Found a legal move, not checkmate
                            }
                        }
                    }
                }
                 return true;
            }
        }
        if (!IsKingInCheck(board, isWhiteKing)) return false; // Not in check, so can't be checkmate
        for (int row = 0; row < 8; row++)
        {
            for (int col = 0; col < 8; col++)
            {
                Piece piece = board[row, col];
                if (piece != null && piece.isWhite == isWhiteKing)
                {
                    List<Vector2Int> legalMoves = piece.GetLegalMovesWithCheckValidation(board);
                    if (legalMoves.Count > 0) return false; // Found a legal move, not checkmate
                }
            }
        }
        return true; // No legal moves available, it's checkmate
    }

    public static bool IsStalemate(Piece[,] board, bool isWhitePlayer)
    {
        // In HideTheKing mode, only the hidden king matters for stalemate; other kings can be captured like normal pieces
        if (HideTheKingManager.HideTheKingMode)
        {
            var htkManager = HideTheKingManager.Instance;
            if (htkManager != null)
            {
                var state = htkManager.GetHiddenState(isWhitePlayer);
                var hiddenTarget = state?.HiddenTarget;
                if (hiddenTarget == null || !hiddenTarget.enabled)
                    return false;
                // If hidden king is in check, not stalemate
                if (IsKingInCheck(board, isWhitePlayer))
                    return false;
                // If ANY piece for this player has any legal moves (under HTK rules), it's not stalemate
                for (int row = 0; row < 8; row++)
                {
                    for (int col = 0; col < 8; col++)
                    {
                        Piece p = board[row, col];
                        if (p != null && p.isWhite == isWhitePlayer && p.enabled)
                        {
                            List<Vector2Int> legalMoves = htkManager.GetLegalMovesHTK(p, board);
                            if (legalMoves != null && legalMoves.Count > 0)
                                return false;
                        }
                    }
                }
                 return true; // No legal moves and not in check
            }
        }
        // First, check if the king is NOT in check
        if (IsKingInCheck(board, isWhitePlayer))
        {
            return false; // In check, so can't be stalemate
        }
        // King is safe - now check if there are ANY legal moves for this player
        for (int row = 0; row < 8; row++)
        {
            for (int col = 0; col < 8; col++)
            {
                Piece piece = board[row, col];
                if (piece != null && piece.isWhite == isWhitePlayer)
                {
                    List<Vector2Int> legalMoves = piece.GetLegalMovesWithCheckValidation(board);
                    if (legalMoves.Count > 0)
                    {
                        return false; // Found a legal move, not stalemate
                    }
                }
            }
        }
        return true; // No legal moves available and not in check meaning stalemate
    }
    
    public static bool IsInsufficientMaterial(Piece[,] board)
    {
        List<Piece> whitePieces = new List<Piece>();
        List<Piece> blackPieces = new List<Piece>();
        
        // Collect all pieces
        for (int row = 0; row < 8; row++)
        {
            for (int col = 0; col < 8; col++)
            {
                Piece piece = board[row, col];
                if (piece != null)
                {
                    if (piece.isWhite)
                        whitePieces.Add(piece);
                    else
                        blackPieces.Add(piece);
                }
            }
        }
        
        // King vs King
        if (whitePieces.Count == 1 && blackPieces.Count == 1)
        {
            return true;
        }
        
        // King + Bishop vs King or King + Knight vs King
        if ((whitePieces.Count == 2 && blackPieces.Count == 1) || 
            (whitePieces.Count == 1 && blackPieces.Count == 2))
        {
            List<Piece> twoSide = whitePieces.Count == 2 ? whitePieces : blackPieces;
            
            foreach (Piece p in twoSide)
            {
                if (p.type == PieceType.Bishop || p.type == PieceType.Knight)
                {
                    return true;
                }
            }
        }
        
        // King + Bishop vs King + Bishop (same color squares)
        if (whitePieces.Count == 2 && blackPieces.Count == 2)
        {
            Piece whiteBishop = null;
            Piece blackBishop = null;
            
            foreach (Piece p in whitePieces)
            {
                if (p.type == PieceType.Bishop) whiteBishop = p;
            }
            
            foreach (Piece p in blackPieces)
            {
                if (p.type == PieceType.Bishop) blackBishop = p;
            }
            
            if (whiteBishop != null && blackBishop != null)
            {
                // Check if bishops are on same color squares
                bool whiteBishopOnWhite = (whiteBishop.position.x + whiteBishop.position.y) % 2 == 0;
                bool blackBishopOnWhite = (blackBishop.position.x + blackBishop.position.y) % 2 == 0;
                
                if (whiteBishopOnWhite == blackBishopOnWhite)
                {
                    return true;
                }
            }
        }
        
        return false;
    }
}

// Enum for piece types (Cleaner and faster than strings for comparisons)
public enum PieceType
{
    Pawn, Rook, Knight, Bishop, Queen, King
}
