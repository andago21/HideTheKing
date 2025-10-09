using UnityEngine;
using System.Collections.Generic;

public class PawnPiece : Piece
{
    // Override to implement pawn-specific moves
    public override List<Vector2Int> GetLegalMoves(Piece[,] board)
    {
        List<Vector2Int> moves = new List<Vector2Int>();
        int direction = isWhite ? 1 : -1; // White moves up (positive row), black down
        int startRow = isWhite ? 1 : 6; // Starting row for double-move check

        // Single forward move (only if empty)
        Vector2Int forward = new Vector2Int(position.x + direction, position.y);
        if (IsInBounds(forward) && board[forward.x, forward.y] == null)
        {
            moves.Add(forward);

            // Double forward from start (if both squares empty)
            if (position.x == startRow)
            {
                Vector2Int doubleForward = new Vector2Int(position.x + direction * 2, position.y);
                if (IsInBounds(doubleForward) && board[doubleForward.x, doubleForward.y] == null)
                {
                    moves.Add(doubleForward);
                }
            }
        }

        // Diagonal captures (only if enemy piece there)
        Vector2Int[] captureOffsets = { new Vector2Int(direction, -1), new Vector2Int(direction, 1) };
        foreach (var offset in captureOffsets)
        {
            Vector2Int capturePos = position + offset;
            if (IsInBounds(capturePos) && board[capturePos.x, capturePos.y] != null &&
                board[capturePos.x, capturePos.y].isWhite != isWhite)
            {
                moves.Add(capturePos);
            }
        }

        return moves;
    }

    private bool IsInBounds(Vector2Int pos)
    {
        return pos.x >= 0 && pos.x < 8 && pos.y >= 0 && pos.y < 8;
    }
}