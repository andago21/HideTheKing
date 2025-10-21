using UnityEngine;
using System.Collections.Generic;

public class RookPiece : Piece
{
    public override List<Vector2Int> GetLegalMoves(Piece[,] board)
    {
        List<Vector2Int> moves = new List<Vector2Int>();
        Vector2Int[] directions = {
            new Vector2Int(1, 0),   // up
            new Vector2Int(-1, 0),  // down
            new Vector2Int(0, 1),   // right
            new Vector2Int(0, -1)   // left
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