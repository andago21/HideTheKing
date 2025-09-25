using UnityEngine;
using System.Collections.Generic;

public class Piece : MonoBehaviour
{
    public bool isWhite; // true for white pieces, false for black
    public PieceType type; // Enum for pawn, rook, etc.
    public Vector2Int position; // Column (0-7 from bottom to top), Row (0-7 from a to h)

    // This method will be overridden by specific piece scripts to calculate moves
    public virtual List<Vector2Int> GetLegalMoves(Piece[,] board)
    {
        return new List<Vector2Int>();
    }
    
    protected bool IsInBounds(Vector2Int pos)
    {
        return pos.x >= 0 && pos.x < 8 && pos.y >= 0 && pos.y < 8;
    }
}

// Enum for piece types (Cleaner and faster than strings for comparisons)
public enum PieceType
{
    Pawn, Rook, Knight, Bishop, Queen, King
}