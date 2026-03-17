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

        // Castling
        if (!hasMoved)
        {
            int row = position.x;

            // Kingside castling (right)
            Piece kingsideRook = board[row, 7];
            if (kingsideRook != null && kingsideRook.type == PieceType.Rook && !kingsideRook.hasMoved)
            {
                // Check if squares between are empty
                if (board[row, 5] == null && board[row, 6] == null)
                {
                    moves.Add(new Vector2Int(row, 6)); // King moves to g-file
                }
            }

            // Queenside castling (left)
            Piece queensideRook = board[row, 0];
            if (queensideRook != null && queensideRook.type == PieceType.Rook && !queensideRook.hasMoved)
            {
                // Check if squares between are empty
                if (board[row, 1] == null && board[row, 2] == null && board[row, 3] == null)
                {
                    moves.Add(new Vector2Int(row, 2)); // King moves to c-file
                }
            }
        }

        return moves;
    }
}