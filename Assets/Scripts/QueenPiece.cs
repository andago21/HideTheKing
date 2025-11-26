using UnityEngine;
using System.Collections.Generic;

public class QueenPiece : Piece
{
    public override List<Vector2Int> GetLegalMoves(Piece[,] board)
    {
        List<Vector2Int> moves = new List<Vector2Int>();

        // Combine rook and bishop directions
        Vector2Int[] directions = {
            new Vector2Int(1, 0), new Vector2Int(-1, 0), new Vector2Int(0, 1), new Vector2Int(0, -1), // Rook
            new Vector2Int(1, 1), new Vector2Int(1, -1), new Vector2Int(-1, 1), new Vector2Int(-1, -1) // Bishop
        };

        foreach (var dir in directions)
        {
            Vector2Int current = position + dir;
            while (IsInBounds(current))
            {
                if (board[current.x, current.y] == null)
                {
                    moves.Add(current);
                }
                else
                {
                    if (board[current.x, current.y].isWhite != isWhite)
                    {
                        moves.Add(current); // Capture
                    }
                    break; // Blocked
                }
                current += dir;
            }
        }

        return moves;
    }
}