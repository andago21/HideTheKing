using TMPro;
using UnityEngine;
using System;

namespace HideTheKing.Core
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
        
        public event Action<bool> OnPieceCaptured;
        public event Action<string> OnGameStatusChanged;

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
            
            OnPieceCaptured?.Invoke(isWhite);
            UpdateUI();
        }
        
        public int GetRemainingPieces(bool isWhite)
        {
            return isWhite ? whitePieces : blackPieces;
        }

        public string GetGameStatus()
        {
            return gameStatus;
        }

        public void SetGameStatus(string status)
        {
            gameStatus = status;

            OnGameStatusChanged?.Invoke(status);

            UpdateUI();
        }

        private void UpdateUI()
        {
            whitePiecesText.text = "White Pieces: " + whitePieces;
            blackPiecesText.text = "Black Pieces: " + blackPieces;
            gameStatusText.text = "Status: " + gameStatus;
        }
    }
}
