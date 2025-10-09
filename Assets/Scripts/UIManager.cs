using UnityEngine;

namespace HideTheKing.Core
{
    public class UIManager : MonoBehaviour
    {
        private void OnEnable()
        {
            GameManager.Instance.OnPieceCaptured += HandlePieceCaptured;
            GameManager.Instance.OnGameStatusChanged += HandleStatusChanged;
        }

        private void OnDisable()
        {
            GameManager.Instance.OnPieceCaptured -= HandlePieceCaptured;
            GameManager.Instance.OnGameStatusChanged -= HandleStatusChanged;
        }

        private void HandlePieceCaptured(bool isWhite)
        {
            Debug.Log("Captured piece: " + (isWhite ? "White" : "Black"));
        }

        private void HandleStatusChanged(string newStatus)
        {
            Debug.Log("Game status changed to: " + newStatus);
        }
    }

}