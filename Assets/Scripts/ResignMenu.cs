using UnityEngine;
using UnityEngine.SceneManagement;
using Mirror;

public class ResignMenu : MonoBehaviour
{
    [Header("Assign in Inspector")]
    public GameObject resignMenuCanvas;

    private bool _menuOpen = false;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
            ToggleMenu();
    }

    public void ToggleMenu()
    {
        _menuOpen = !_menuOpen;
        if (resignMenuCanvas != null)
            resignMenuCanvas.SetActive(_menuOpen);
        Cursor.visible = true;
    }

    public void OnResignClicked()
    {
        Debug.Log("[Resign] Player resigned!");

        BoardManager board = FindObjectOfType<BoardManager>();
        if (board == null || board.gameState != GameState.Playing) return;

        ChessNetworkManager net = ChessNetworkManager.LocalInstance;

        if (net != null && net.IsMultiplayer())
        {
            // Multiplayer — Resignierender verliert
            GameState result = net.isWhitePlayer ? GameState.BlackWins : GameState.WhiteWins;
            net.SendGameEnd(result);
        }
        else
        {
            // Singleplayer / AI — Spieler ist immer Weiss, verliert
            board.HandleGameEnd(GameState.BlackWins);
        }

        if (resignMenuCanvas != null)
            resignMenuCanvas.SetActive(false);
        _menuOpen = false;
    }

    public void OnContinueClicked()
    {
        _menuOpen = false;
        if (resignMenuCanvas != null)
            resignMenuCanvas.SetActive(false);
    }
}