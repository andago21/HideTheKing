using UnityEngine;
using Mirror;
using UnityEngine.SceneManagement;

public class ChessNetworkRoom : NetworkManager
{
    public override void OnServerDisconnect(NetworkConnectionToClient conn)
    {
        Debug.Log("[Disconnect] OnServerDisconnect gefeuert!");

        BoardManager board = FindObjectOfType<BoardManager>();
        if (board != null && board.gameState == GameState.Playing)
        {
            // Client hat getrennt — Host (Weiß) gewinnt
            board.HandleGameEnd(GameState.WhiteWins);

            // ELO für Host
            if (SceneManager.GetActiveScene().name.Contains("Classic"))
                if (EloManager.Instance != null)
                    EloManager.Instance.UpdateElo(1f, 1200);

            // Alle Clients informieren — aber Client ist bereits weg
            // Der verbleibende Host sieht den Screen durch HandleGameEnd
        }

        base.OnServerDisconnect(conn);
    }

    public override void OnClientDisconnect()
    {
        Debug.Log("[Disconnect] OnClientDisconnect gefeuert!");

        BoardManager board = FindObjectOfType<BoardManager>();
        if (board != null && board.gameState == GameState.Playing)
        {
            // Host hat getrennt — Client (Schwarz) gewinnt
            board.HandleGameEnd(GameState.BlackWins);

            if (SceneManager.GetActiveScene().name.Contains("Classic"))
                if (EloManager.Instance != null)
                    EloManager.Instance.UpdateElo(1f, 1200);
        }

        base.OnClientDisconnect();
    }
}