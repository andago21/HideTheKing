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

        ChessNetworkManager net = ChessNetworkManager.LocalInstance;
        BoardManager board = FindObjectOfType<BoardManager>();

        if (net != null && net.IsMultiplayer() && board != null && board.gameState == GameState.Playing)
        {
            // Resignierender verliert
            GameState result = net.isWhitePlayer ? GameState.BlackWins : GameState.WhiteWins;

            // SendEloSync VOR SendGameEnd — damit PlayerInput.Update nicht nochmal ELO gibt
            net.SendEloSync(result);
            net.SendGameEnd(result);
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