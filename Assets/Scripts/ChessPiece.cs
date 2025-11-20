using UnityEngine;

namespace HideTheKing.Core
{
    public class ChessPiece : Piece
    {
        private void OnDestroy()
        {
            // Notify normal chess logic
            if (GameManager.Instance != null)
            {
                GameManager.Instance.PieceCaptured(isWhite);
            }
        }
    }
}