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
    private bool _eloGiven = false;
    private bool _localColorCached = false;
    private bool _cachedLocalIsWhite = true;

    void Start()
    {
        if (boardManager == null)
            boardManager = FindObjectOfType<BoardManager>();

        if (boardManager != null)
            _lastState = boardManager.gameState;
        
        if (victoryCanvas != null)  victoryCanvas.SetActive(false);
        if (drawCanvas != null)     drawCanvas.SetActive(false);
        if (gameOverCanvas != null) gameOverCanvas.SetActive(false);

        _eloGiven = false;
    }

    void Update()
    {
        if (boardManager == null) return;

        // Cache local player color as early as possible while LocalInstance is still alive
        if (!_localColorCached && ChessNetworkManager.LocalInstance != null)
        {
            _cachedLocalIsWhite = ChessNetworkManager.LocalInstance.isWhitePlayer;
            _localColorCached = true;
            Debug.Log("UIManager: Cached local color = " + (_cachedLocalIsWhite ? "White" : "Black"));
        }

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
        bool isMultiplayer = ChessNetworkManager.LocalInstance != null && 
                             ChessNetworkManager.LocalInstance.IsMultiplayer();
        bool isClassic = UnityEngine.SceneManagement.SceneManager
                            .GetActiveScene().name.Contains("Classic");

        if (state == GameState.Draw)
        {
            if (drawCanvas != null) drawCanvas.SetActive(true);
            if (isMultiplayer && isClassic && !_eloGiven)
            {
                _eloGiven = true;
                if (EloManager.Instance != null)
                    EloManager.Instance.UpdateElo(0.5f, 1200);
            }
        }
        else if (state == GameState.WhiteWins)
        {
            if (localIsWhite)
            {
                if (victoryCanvas != null) victoryCanvas.SetActive(true);
                if (MusicManager.Instance != null) MusicManager.Instance.PlayVictory();
                if (isMultiplayer && isClassic && !_eloGiven)
                {
                    _eloGiven = true;
                    if (EloManager.Instance != null)
                        EloManager.Instance.UpdateElo(1f, 1200); // Gewinner +ELO
                }
            }
            else
            {
                if (gameOverCanvas != null) gameOverCanvas.SetActive(true);
                if (MusicManager.Instance != null) MusicManager.Instance.PlayDefeat();
                if (isMultiplayer && isClassic && !_eloGiven)
                {
                    _eloGiven = true;
                    if (EloManager.Instance != null)
                        EloManager.Instance.UpdateElo(0f, 1200); // Verlierer -ELO
                }
            }
        }
        else if (state == GameState.BlackWins)
        {
            if (!localIsWhite)
            {
                if (victoryCanvas != null) victoryCanvas.SetActive(true);
                if (MusicManager.Instance != null) MusicManager.Instance.PlayVictory();
                if (isMultiplayer && isClassic && !_eloGiven)
                {
                    _eloGiven = true;
                    if (EloManager.Instance != null)
                        EloManager.Instance.UpdateElo(1f, 1200); // Gewinner +ELO
                }
            }
            else
            {
                if (gameOverCanvas != null) gameOverCanvas.SetActive(true);
                if (MusicManager.Instance != null) MusicManager.Instance.PlayDefeat();
                if (isMultiplayer && isClassic && !_eloGiven)
                {
                    _eloGiven = true;
                    if (EloManager.Instance != null)
                        EloManager.Instance.UpdateElo(0f, 1200); // Verlierer -ELO
                }
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

        // Singleplayer — kein Netzwerk, Spieler ist immer Weiss
        if (!Mirror.NetworkClient.active && !Mirror.NetworkServer.active)
        {
            Debug.Log("UIManager: Singleplayer — defaulting to White");
            return true;
        }

        Debug.Log("UIManager: Using static LocalIsWhite = " + ChessNetworkManager.LocalIsWhite);
        return ChessNetworkManager.LocalIsWhite;
    }
}