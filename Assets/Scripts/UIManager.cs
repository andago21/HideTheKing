using UnityEngine;
using UnityEngine.SceneManagement;
using Mirror;

public class UIManager : MonoBehaviour
{
    public BoardManager boardManager;

    [Header("Canvases")]
    public GameObject victoryCanvas;
    public GameObject drawCanvas;
    public GameObject gameOverCanvas;

    public bool disableBoardOnEnd = true;

    private GameState _lastState = GameState.Playing;
    private bool _eloGiven = false;
    private bool _localIsWhite = true;
    private bool _colorSet = false;

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
        _colorSet = false;

        // Try to set color immediately
        TryCacheColor();
    }

    private void TryCacheColor()
    {
        if (_colorSet) return;

        // Singleplayer — always White
        if (!NetworkClient.active && !NetworkServer.active)
        {
            _localIsWhite = true;
            _colorSet = true;
            Debug.Log("[UIManager] Singleplayer: color = White");
            return;
        }

        // Multiplayer — get from LocalInstance
        if (ChessNetworkManager.LocalInstance != null)
        {
            _localIsWhite = ChessNetworkManager.LocalInstance.isWhitePlayer;
            _colorSet = true;
            Debug.Log("[UIManager] Multiplayer: color = " + (_localIsWhite ? "White" : "Black"));
        }
    }

    void Update()
    {
        if (boardManager == null) return;

        // Keep trying to cache color until we have it
        if (!_colorSet) TryCacheColor();

        var state = boardManager.gameState;
        if (state != _lastState && state != GameState.Playing)
        {
            // Last attempt to get color before showing screen
            TryCacheColor();
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

        bool isClassic = SceneManager.GetActiveScene().name.Contains("Classic");
        // Use WasMultiplayer — NetworkClient.active may already be false when game ends
        bool isMultiplayer = ChessNetworkManager.WasMultiplayer;

        bool iWon  = (state == GameState.WhiteWins &&  _localIsWhite) ||
                     (state == GameState.BlackWins  && !_localIsWhite);
        bool iLost = (state == GameState.WhiteWins  && !_localIsWhite) ||
                     (state == GameState.BlackWins  &&  _localIsWhite);
        bool isDraw = state == GameState.Draw;

        Debug.Log($"[UIManager] state={state} localIsWhite={_localIsWhite} iWon={iWon} iLost={iLost} colorSet={_colorSet}");

        if (isDraw)
        {
            if (drawCanvas != null) drawCanvas.SetActive(true);
            if (isMultiplayer && isClassic && !_eloGiven)
            {
                _eloGiven = true;
                EloManager.Instance?.UpdateElo(0.5f, 1200);
            }
        }
        else if (iWon)
        {
            if (victoryCanvas != null) victoryCanvas.SetActive(true);
            MusicManager.Instance?.PlayVictory();
            if (isMultiplayer && isClassic && !_eloGiven)
            {
                _eloGiven = true;
                EloManager.Instance?.UpdateElo(1f, 1200);
                Debug.Log("[UIManager] +ELO given (winner)");
            }
        }
        else if (iLost)
        {
            if (gameOverCanvas != null) gameOverCanvas.SetActive(true);
            MusicManager.Instance?.PlayDefeat();
            Debug.Log($"[UIManager] iLost=true isMultiplayer={isMultiplayer} isClassic={isClassic} eloGiven={_eloGiven} EloInstance={EloManager.Instance != null}");
            if (isMultiplayer && isClassic && !_eloGiven)
            {
                _eloGiven = true;
                if (EloManager.Instance != null)
                {
                    EloManager.Instance.UpdateElo(0f, 1200);
                    Debug.Log("[UIManager] -ELO given (loser)");
                }
                else
                {
                    Debug.LogError("[UIManager] EloManager.Instance is NULL — cannot give ELO!");
                }
            }
        }
    }
}