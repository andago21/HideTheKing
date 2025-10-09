using UnityEngine;
using System.Collections.Generic;

public class KingPiece : Piece
{
    public override List<Vector2Int> GetLegalMoves(Piece[,] board)
    {
        List<Vector2Int> moves = new List<Vector2Int>();
        Vector2Int[] offsets = {
            new Vector2Int(1, 0), new Vector2Int(-1, 0), new Vector2Int(0, 1), new Vector2Int(0, -1),
            new Vector2Int(1, 1), new Vector2Int(1, -1), new Vector2Int(-1, 1), new Vector2Int(-1, -1)
        };

        foreach (var offset in offsets)
        {
            Vector2Int target = position + offset;
            if (IsInBounds(target))
            {
                Piece targetPiece = board[target.x, target.y];
                if (targetPiece == null || targetPiece.isWhite != isWhite)
                {
                    moves.Add(target);
                }
            }
        }

        return moves;
    }
}