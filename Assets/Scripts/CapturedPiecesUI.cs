using TMPro;
using UnityEngine;

namespace HideTheKing.Core
{
    public class CapturedPiecesUI : MonoBehaviour
    {
        public TMP_Text whiteCapturedText;
        public TMP_Text blackCapturedText;
        public BoardManager boardManager;
        private void Update()
        {
            if (boardManager != null)
            {
                // Update the UI with the current count of captured pieces
                if (whiteCapturedText != null)
                {
                    whiteCapturedText.text = $"White Pieces Captured: {boardManager.whiteCapturedCount}";
                }

                if (blackCapturedText != null)
                {
                    blackCapturedText.text = $"Black Pieces Captured: {boardManager.blackCapturedCount}";
                }
            }
        }
    }
}