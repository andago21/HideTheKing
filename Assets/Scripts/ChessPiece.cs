using UnityEngine;

namespace HideTheKing.Core
{
    public class ChessPiece : Piece
    {
        public bool isWhite;

        private void OnDestroy()
        {
            // Notify normal game logic
            if (GameManager.Instance != null)
            {
                GameManager.Instance.PieceCaptured(isWhite);
            }

            // Notify Hide-the-King logic (important!)
            if (HideTheKingManager.Instance != null)
            {
                HideTheKingManager.Instance.ReportCapture(this);
            }
        }
    }
}