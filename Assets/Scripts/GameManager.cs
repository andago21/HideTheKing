using TMPro;
using UnityEngine;

namespace DefaultNamespace
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance;

        [Header("UI References")]
        public TextMeshProUGUI whitePiecesText;
        public TextMeshProUGUI blackPiecesText;
        public TextMeshProUGUI gameStatusText;

        [Header("Game State")]
        private int whitePieces = 16;
        private int blackPieces = 16;
        private string gameStatus = "Ongoing";

        private void Awake()
        {
            if (Instance == null)
                Instance = this;
            else
                Destroy(gameObject);
        }

        private void Start()
        {
            UpdateUI();
        }

        public void PieceCaptured(bool isWhite)
        {
            if (isWhite)
                whitePieces--;
            else
                blackPieces--;

            UpdateUI();
        }

        private void UpdateUI()
        {
            whitePiecesText.text = "White Pieces: " + whitePieces;
            blackPiecesText.text = "Black Pieces: " + blackPieces;
            gameStatusText.text = "Status: " + gameStatus;
        }

        public void SetGameStatus(string status)
        {
            gameStatus = status;
            UpdateUI();
        }
    }
}