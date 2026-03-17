using UnityEngine;

public class ChessTimer : MonoBehaviour
{
    public BoardManager boardManager;
    private GameRules gameRules;

    public float whiteTimeRemaining;
    public float blackTimeRemaining;
    
    private float gameDuration; // Total time for each player in seconds
    private bool timerActive = false;

    //Displaying the timer in console (Will be replaced with UI later)
    private float displayInterval = 5f; // Display every 5 seconds
    private float nextDisplayTime = 0f;


    private void Awake()
    {
        gameRules = GetComponent<GameRules>();
        if (gameRules == null)
        {
            Debug.LogError("GameRules component not found on BoardManager!");
        }
    }

    private void Start()
    {
        InitializeTimer();
    }

    private void InitializeTimer()
    {
        // Randomly choose between 3/5 minute game
        int randomMinutes = Random.Range(0, 2) == 0 ? 3 : 5;
        gameDuration = randomMinutes * 60f; // Convert to seconds
        
        whiteTimeRemaining = gameDuration;
        blackTimeRemaining = gameDuration;
        
        timerActive = true;
        
        Debug.Log("Chess Timer initialized: " + randomMinutes + " minutes per player");
    }

    private void Update()
    {
        if (!timerActive || boardManager.gameState != GameState.Playing)
        {
            return;
        }

        // Display timer periodically (optional, for debugging)
        if (Time.time >= nextDisplayTime)
        {
            //Debug.Log("White: " + GetFormattedTime(true) + " | Black: " + GetFormattedTime(false));
            nextDisplayTime = Time.time + displayInterval;
        }


        // Decrease time for the current player
        if (boardManager.isWhiteTurn)
        {
            whiteTimeRemaining -= Time.deltaTime;
            if (whiteTimeRemaining <= 0)
            {
                whiteTimeRemaining = 0;
                OnTimeOut(true); // White ran out of time
            }
        }
        else
        {
            blackTimeRemaining -= Time.deltaTime;
            if (blackTimeRemaining <= 0)
            {
                blackTimeRemaining = 0;
                OnTimeOut(false); // Black ran out of time
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

    // Get formatted time string (MM:SS)
    public string GetFormattedTime(bool isWhite)
    {
        float timeRemaining = isWhite ? whiteTimeRemaining : blackTimeRemaining;
        int minutes = Mathf.FloorToInt(timeRemaining / 60);
        int seconds = Mathf.FloorToInt(timeRemaining % 60);
        return string.Format("{0:00}:{1:00}", minutes, seconds);
    }

    // Stop the timer (useful when game ends)
    public void StopTimer()
    {
        timerActive = false;
    }

    // Resume the timer
    public void ResumeTimer()
    {
        if (boardManager.gameState == GameState.Playing)
        {
            timerActive = true;
        }
    }
}