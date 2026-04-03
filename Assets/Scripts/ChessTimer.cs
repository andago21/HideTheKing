using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using Mirror;

public class ChessTimer : NetworkBehaviour
{
    public BoardManager boardManager;

    [Header("UI")]
    public TMP_Text timerWhiteText;
    public TMP_Text timerBlackText;

    [SyncVar] public float whiteTimeRemaining;
    [SyncVar] public float blackTimeRemaining;

    private bool timerActive = false;
    private float displayInterval = 5f;
    private float nextDisplayTime = 0f;

    private bool IsOffline => SceneManager.GetActiveScene().name.Contains("Offline");

    private void Awake()
    {
        if (boardManager == null)
            boardManager = FindObjectOfType<BoardManager>();
    }

    private void Start()
    {
        if (IsOffline)
            StartTimer();
        else if (!NetworkClient.active && !NetworkServer.active)
            StartTimer();
    }

    public void StartTimer()
    {
        int minutes;

        if (IsOffline)
        {
            // Random 5, 10 or 15 minutes for offline/AI games
            int[] options = { 5, 10, 15 };
            minutes = options[Random.Range(0, options.Length)];
        }
        else
        {
            // Multiplayer: use host's selection from LobbyUI
            minutes = PlayerPrefs.GetInt("SelectedTimerMinutes", 5);
        }

        whiteTimeRemaining = minutes * 60f;
        blackTimeRemaining = minutes * 60f;
        timerActive = true;

        Debug.Log("[Timer] Scene: " + SceneManager.GetActiveScene().name + " | IsOffline: " + IsOffline + " | Minutes: " + minutes);
    }

    private void Update()
    {
        if (boardManager == null)
        {
            boardManager = FindObjectOfType<BoardManager>();
            return;
        }

        // Update UI on all clients (SyncVar keeps values in sync for multiplayer)
        if (timerWhiteText != null) timerWhiteText.text = GetFormattedTime(true);
        if (timerBlackText != null) timerBlackText.text = GetFormattedTime(false);

        // Offline: run locally; Multiplayer: only server runs the logic
        if (!IsOffline && !isServer) return;
        if (!timerActive || boardManager.gameState != GameState.Playing) return;

        if (Time.time >= nextDisplayTime)
        {
            Debug.Log("White: " + GetFormattedTime(true) + " | Black: " + GetFormattedTime(false));
            nextDisplayTime = Time.time + displayInterval;
        }

        if (boardManager.isWhiteTurn)
        {
            whiteTimeRemaining -= Time.deltaTime;
            if (whiteTimeRemaining <= 0) { whiteTimeRemaining = 0; OnTimeOut(true); }
        }
        else
        {
            blackTimeRemaining -= Time.deltaTime;
            if (blackTimeRemaining <= 0) { blackTimeRemaining = 0; OnTimeOut(false); }
        }
    }

    private void OnTimeOut(bool isWhiteTimeout)
    {
        timerActive = false;
        if (IsOffline || isServer)
        {
            if (isWhiteTimeout)
            {
                Debug.Log("White ran out of time! Black wins by timeout.");
                boardManager.HandleGameEnd(GameState.BlackWins);
            }
            else
            {
                Debug.Log("Black ran out of time! White wins by timeout.");
                boardManager.HandleGameEnd(GameState.WhiteWins);
            }
        }
    }

    public string GetFormattedTime(bool isWhite)
    {
        float t = isWhite ? whiteTimeRemaining : blackTimeRemaining;
        return string.Format("{0:00}:{1:00}", Mathf.FloorToInt(t / 60), Mathf.FloorToInt(t % 60));
    }

    public void StopTimer()   { timerActive = false; }
    public void ResumeTimer() { if (boardManager != null && boardManager.gameState == GameState.Playing) timerActive = true; }
}