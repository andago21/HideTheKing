using UnityEngine;

/// <summary>
/// Manages the user interface elements of the game, including the end game screens for victory and draw states.
/// </summary>
public class UIManager : MonoBehaviour
{
    public BoardManager boardManager;
    [Tooltip("Set true if the local player is White (used to decide which screen to show on win/loss)")]
    public bool localIsWhite = true;

    [Header("Assign in-scene canvas GameObjects for end screens")]
    [Tooltip("Canvas (or root GameObject) to activate when the local player wins")]
    public GameObject victoryCanvas;

    [Tooltip("Canvas (or root GameObject) to activate when the game is a draw or the local player loses")]
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
        // Hide both then selectively enable the correct canvas
        if (victoryCanvas != null) victoryCanvas.SetActive(false);
        if (drawCanvas != null) drawCanvas.SetActive(false);
        if (gameOverCanvas != null) gameOverCanvas.SetActive(false);

        if (disableBoardOnEnd && boardManager != null)
        {
            boardManager.enabled = false;
        }

        if (state == GameState.Draw)
        {
            if (drawCanvas != null) drawCanvas.SetActive(true);
            else Debug.Log("UIManager: Draw occured but drawCanvas is not assigned.");
        }
        else if (state == GameState.WhiteWins)
        {
            if (localIsWhite)
            {
                if (victoryCanvas != null) victoryCanvas.SetActive(true);
                else Debug.Log("UIManager: White won but victoryCanvas is not assigned.");
            }
            else
            {
                if (gameOverCanvas != null) gameOverCanvas.SetActive(true);
                else Debug.Log("UIManager: White won but gameOverCanvas is not assigned for loss display.");
            }
        }
        else if (state == GameState.BlackWins)
        {
            if (!localIsWhite)
            {
                if (victoryCanvas != null) victoryCanvas.SetActive(true);
                else Debug.Log("UIManager: Black won but victoryCanvas is not assigned.");
            }
            else
            {
                if (gameOverCanvas != null) gameOverCanvas.SetActive(true);
                else Debug.Log("UIManager: Black won but gameOverCanvas is not assigned for loss display.");
            }
        }
    }
}
