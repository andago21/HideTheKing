using UnityEngine;
using UnityEngine.SceneManagement;
using Mirror;
using System.Collections;

public class WinConditionScreen : MonoBehaviour
{
    public void GoToLobby()
    {
        string scene = SceneManager.GetActiveScene().name;
        string lobby = "ClassicLobby";
        if      (scene.Contains("BattleChess"))       lobby = "BattleChessLobby";
        else if (scene.Contains("HideTheKing"))        lobby = "HideTheKingLobby";
        else if (scene.Contains("CrownOfConfussions")) lobby = "CrownOfConfusionsLobby";

        StartCoroutine(StopAndLoad(lobby));
    }

    public void GoToMainMenu()
    {
        StartCoroutine(StopAndLoad("StartScene"));
    }

    private IEnumerator StopAndLoad(string sceneName)
    {
        if (NetworkServer.active && NetworkClient.isConnected)
            NetworkManager.singleton.StopHost();
        else if (NetworkServer.active)
            NetworkManager.singleton.StopServer();
        else if (NetworkClient.isConnected)
            NetworkManager.singleton.StopClient();

        // Reset multiplayer flag so next game starts fresh
        ChessNetworkManager.WasMultiplayer = false;

        // Wait for Mirror to fully stop
        yield return new WaitForSeconds(0.5f);

        SceneManager.LoadScene(sceneName);
    }

    public void QuitGame()
    {
        Debug.Log("Game is exiting...");
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}