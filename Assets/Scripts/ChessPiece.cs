using UnityEngine;

namespace HideTheKing.Core
{
    public class ChessPiece : MonoBehaviour
    {
        public bool isWhite;

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.PieceCaptured(isWhite);
            }
        }
    }
}