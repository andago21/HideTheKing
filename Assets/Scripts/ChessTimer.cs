using UnityEngine;

public class ChessTimer : MonoBehaviour
{
    public BoardManager boardManager;
    private GameRules gameRules;

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
        // CHANGED: no longer auto-starts. 
        // In singleplayer, StartTimer() is called immediately.
        // In multiplayer, StartTimer() is called once both players are connected.
        if (!Mirror.NetworkClient.active && !Mirror.NetworkServer.active)
        {
            StartTimer(); // Singleplayer: start immediately
        }
        // Multiplayer: LobbyConnector will call StartTimer() via RPC when both players are in
    }

    public void StartTimer()
    {
        int randomMinutes = Random.Range(0, 2) == 0 ? 5 : 10;
        gameDuration = randomMinutes * 60f;

        whiteTimeRemaining = gameDuration;
        blackTimeRemaining = gameDuration;

        timerActive = true;

        Debug.Log("Chess Timer started: " + randomMinutes + " minutes per player");
    }

    private void Update()
    {
        if (!timerActive || boardManager.gameState != GameState.Playing)
            return;

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