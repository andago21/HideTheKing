using UnityEngine;

public class UIManager : MonoBehaviour
{
    public BoardManager boardManager;
    
    [Header("Assign in-scene canvas GameObjects for end screens")]
    public GameObject victoryCanvas;
    public GameObject drawCanvas;
    public GameObject gameOverCanvas;
    
    public bool disableBoardOnEnd = true;
    
    private GameState _lastState = GameState.Playing;

    void Start()
    {
        if (boardManager == null)
        {
            boardManager = FindObjectOfType<BoardManager>();
            if (boardManager == null)
                Debug.LogWarning("UIManager: BoardManager not found in scene.");
        }

        if (boardManager != null)
            _lastState = boardManager.gameState;
        
        if (victoryCanvas != null)  victoryCanvas.SetActive(false);
        if (drawCanvas != null)     drawCanvas.SetActive(false);
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
        if (victoryCanvas != null)  victoryCanvas.SetActive(false);
        if (drawCanvas != null)     drawCanvas.SetActive(false);
        if (gameOverCanvas != null) gameOverCanvas.SetActive(false);

        if (disableBoardOnEnd && boardManager != null)
            boardManager.enabled = false;

        bool localIsWhite = GetLocalPlayerColor();

        if (state == GameState.Draw)
        {
            if (drawCanvas != null) drawCanvas.SetActive(true);
        }
        else if (state == GameState.WhiteWins)
        {
            if (localIsWhite)
            {
                if (victoryCanvas != null) victoryCanvas.SetActive(true);
                if (MusicManager.Instance != null) MusicManager.Instance.PlayVictory();
                Debug.Log("Victory screen shown (White won, you are White)");
            }
            else
            {
                if (gameOverCanvas != null) gameOverCanvas.SetActive(true);
                if (MusicManager.Instance != null) MusicManager.Instance.PlayDefeat();
                Debug.Log("Game Over screen shown (White won, you are Black)");
            }
        }
        else if (state == GameState.BlackWins)
        {
            if (!localIsWhite)
            {
                if (victoryCanvas != null) victoryCanvas.SetActive(true);
                if (MusicManager.Instance != null) MusicManager.Instance.PlayVictory();
                Debug.Log("Victory screen shown (Black won, you are Black)");
            }
            else
            {
                if (gameOverCanvas != null) gameOverCanvas.SetActive(true);
                if (MusicManager.Instance != null) MusicManager.Instance.PlayDefeat();
                Debug.Log("Game Over screen shown (Black won, you are White)");
            }
        }
    }

    private bool GetLocalPlayerColor()
    {
        if (ChessNetworkManager.LocalInstance != null)
        {
            bool isWhite = ChessNetworkManager.LocalInstance.isWhitePlayer;
            Debug.Log("UIManager: Local player is " + (isWhite ? "White" : "Black"));
            return isWhite;
        }

        Debug.Log("UIManager: Using static LocalIsWhite = " + ChessNetworkManager.LocalIsWhite);
        return ChessNetworkManager.LocalIsWhite;
    }
}