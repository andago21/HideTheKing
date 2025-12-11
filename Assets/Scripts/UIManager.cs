using UnityEngine;

/// <summary>
/// Manages the user interface elements of the game, including the end game screens for victory and draw states.
/// </summary>
public class UIManager : MonoBehaviour
{
    public BoardManager boardManager;
    
    [Header("Assign in-scene canvas GameObjects for end screens")]
    [Tooltip("Canvas (or root GameObject) to activate when the local player wins")]
    public GameObject victoryCanvas;
    
    [Tooltip("Canvas (or root GameObject) to activate when the game is a draw")]
    public GameObject drawCanvas;
    
    [Tooltip("Canvas (or root GameObject) to activate when the local player loses the game (Game Over)")]
    public GameObject gameOverCanvas;
    
    [Tooltip("When true, this manager will disable the BoardManager component when the game ends")]
    public bool disableBoardOnEnd = true;
    
    private GameState _lastState = GameState.Playing;

    void Start()
    {
        if (boardManager == null)
        {
            boardManager = FindObjectOfType<BoardManager>();
            if (boardManager == null)
            {
                Debug.LogWarning("UIManager: BoardManager not found in scene. Please assign one in the inspector.");
            }
        }

        if (boardManager != null)
            _lastState = boardManager.gameState;
        
        // Make sure canvases are hidden initially
        if (victoryCanvas != null) victoryCanvas.SetActive(false);
        if (drawCanvas != null) drawCanvas.SetActive(false);
        if (gameOverCanvas != null) gameOverCanvas.SetActive(false);
    }

    void Update()
    {
        if (boardManager == null) return;

        var state = boardManager.gameState;
        if (state != _lastState && state != GameState.Playing)
        {
            HandleGameStateChange(state);
            _lastState = state;
        }
    }

    private void HandleGameStateChange(GameState state)
    {
        // Hide all canvases first
        if (victoryCanvas != null) victoryCanvas.SetActive(false);
        if (drawCanvas != null) drawCanvas.SetActive(false);
        if (gameOverCanvas != null) gameOverCanvas.SetActive(false);

        if (disableBoardOnEnd && boardManager != null)
        {
            boardManager.enabled = false;
        }

        // Get the local player's color from ChessNetworkManager
        bool localIsWhite = GetLocalPlayerColor();

        if (state == GameState.Draw)
        {
            if (drawCanvas != null) 
            {
                drawCanvas.SetActive(true);
                Debug.Log("Draw screen shown");
            }
            else 
            {
                Debug.Log("UIManager: Draw occurred but drawCanvas is not assigned.");
            }
        }
        else if (state == GameState.WhiteWins)
        {
            if (localIsWhite)
            {
                // Local player is White and White won - VICTORY
                if (victoryCanvas != null) 
                {
                    victoryCanvas.SetActive(true);
                    Debug.Log("Victory screen shown (White won, you are White)");
                }
                else 
                {
                    Debug.Log("UIManager: White won but victoryCanvas is not assigned.");
                }
            }
            else
            {
                // Local player is Black and White won - GAME OVER (loss)
                if (gameOverCanvas != null) 
                {
                    gameOverCanvas.SetActive(true);
                    Debug.Log("Game Over screen shown (White won, you are Black)");
                }
                else 
                {
                    Debug.Log("UIManager: White won but gameOverCanvas is not assigned for loss display.");
                }
            }
        }
        else if (state == GameState.BlackWins)
        {
            if (!localIsWhite)
            {
                // Local player is Black and Black won - VICTORY
                if (victoryCanvas != null) 
                {
                    victoryCanvas.SetActive(true);
                    Debug.Log("Victory screen shown (Black won, you are Black)");
                }
                else 
                {
                    Debug.Log("UIManager: Black won but victoryCanvas is not assigned.");
                }
            }
            else
            {
                // Local player is White and Black won - GAME OVER (loss)
                if (gameOverCanvas != null) 
                {
                    gameOverCanvas.SetActive(true);
                    Debug.Log("Game Over screen shown (Black won, you are White)");
                }
                else 
                {
                    Debug.Log("UIManager: Black won but gameOverCanvas is not assigned for loss display.");
                }
            }
        }
    }

    /// <summary>
    /// Gets the local player's color from ChessNetworkManager.
    /// Returns true if White, false if Black.
    /// In single-player mode, defaults to White.
    /// </summary>
    private bool GetLocalPlayerColor()
    {
        // Try to get the local player's color from the network manager
        if (ChessNetworkManager.LocalInstance != null)
        {
            bool isWhite = ChessNetworkManager.LocalInstance.isWhitePlayer;
            Debug.Log("UIManager: Local player is " + (isWhite ? "White" : "Black"));
            return isWhite;
        }

        // If no network manager (single player), default to White
        Debug.Log("UIManager: No ChessNetworkManager found, defaulting to White for single player");
        return true;
    }
}