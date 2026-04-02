using UnityEngine;
using TMPro;

public class ChessTimer : MonoBehaviour
{
    public BoardManager boardManager;
    private GameRules gameRules;

    [Header("UI")]
    public TMP_Text timerWhiteText;
    public TMP_Text timerBlackText;

    public float whiteTimeRemaining;
    public float blackTimeRemaining;
    
    private float gameDuration;
    private bool timerActive = false;

    private float displayInterval = 5f;
    private float nextDisplayTime = 0f;

    private void Awake()
    {
        gameRules = GetComponent<GameRules>();
        if (gameRules == null)
            Debug.LogError("GameRules component not found on BoardManager!");
    }

    private void Start()
    {
        if (!Mirror.NetworkClient.active && !Mirror.NetworkServer.active)
        {
            StartTimer();
        }
    }

    public void StartTimer()
    {
        // Read timer from PlayerPrefs (set by Host in LobbyUI)
        // Default 5 minutes if not set
        int minutes = PlayerPrefs.GetInt("SelectedTimerMinutes", 5);
        gameDuration = minutes * 60f;

        whiteTimeRemaining = gameDuration;
        blackTimeRemaining = gameDuration;

        timerActive = true;

        Debug.Log("Chess Timer started: " + minutes + " minutes per player");
    }

    private void Update()
    {
        if (!timerActive || boardManager.gameState != GameState.Playing)
            return;

        // Update UI
        if (timerWhiteText != null) timerWhiteText.text = GetFormattedTime(true);
        if (timerBlackText != null) timerBlackText.text = GetFormattedTime(false);

        if (Time.time >= nextDisplayTime)
        {
            Debug.Log("White: " + GetFormattedTime(true) + " | Black: " + GetFormattedTime(false));
            nextDisplayTime = Time.time + displayInterval;
        }

        if (boardManager.isWhiteTurn)
        {
            whiteTimeRemaining -= Time.deltaTime;
            if (whiteTimeRemaining <= 0)
            {
                whiteTimeRemaining = 0;
                OnTimeOut(true);
            }
        }
        else
        {
            blackTimeRemaining -= Time.deltaTime;
            if (blackTimeRemaining <= 0)
            {
                blackTimeRemaining = 0;
                OnTimeOut(false);
            }
        }
    }

    private void OnTimeOut(bool isWhiteTimeout)
    {
        timerActive = false;

        if (isWhiteTimeout)
        {
            Debug.Log("White ran out of time! Black wins by timeout.");
            boardManager.gameState = GameState.BlackWins;
        }
        else
        {
            Debug.Log("Black ran out of time! White wins by timeout.");
            boardManager.gameState = GameState.WhiteWins;
        }
    }

    public string GetFormattedTime(bool isWhite)
    {
        float timeRemaining = isWhite ? whiteTimeRemaining : blackTimeRemaining;
        int minutes = Mathf.FloorToInt(timeRemaining / 60);
        int seconds = Mathf.FloorToInt(timeRemaining % 60);
        return string.Format("{0:00}:{1:00}", minutes, seconds);
    }

    public void StopTimer()
    {
        timerActive = false;
    }

    public void ResumeTimer()
    {
        if (boardManager.gameState == GameState.Playing)
            timerActive = true;
    }
}