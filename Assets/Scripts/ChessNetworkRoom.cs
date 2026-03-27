using UnityEngine;
using Mirror;
using UnityEngine.SceneManagement;

public class ChessNetworkRoom : NetworkManager
{
    public override void OnServerDisconnect(NetworkConnectionToClient conn)
    {
        Debug.Log("[Disconnect] OnServerDisconnect fired!");

        BoardManager board = FindObjectOfType<BoardManager>();
        if (board != null && board.gameState == GameState.Playing)
        {
            // Client disconnected — Host (White) wins
            board.HandleGameEnd(GameState.WhiteWins);

            if (SceneManager.GetActiveScene().name.Contains("Classic"))
                if (EloManager.Instance != null)
                    EloManager.Instance.UpdateElo(1f, 1200);
        }

        base.OnServerDisconnect(conn);
    }

    public override void OnClientDisconnect()
    {
        // Skip if we are the host — OnServerDisconnect already handled it
        if (NetworkServer.active)
        {
            base.OnClientDisconnect();
            return;
        }

        Debug.Log("[Disconnect] OnClientDisconnect fired!");

        BoardManager board = FindObjectOfType<BoardManager>();
        if (board != null && board.gameState == GameState.Playing)
        {
            // Server disconnected — Client (Black) wins
            board.HandleGameEnd(GameState.BlackWins);

            if (SceneManager.GetActiveScene().name.Contains("Classic"))
                if (EloManager.Instance != null)
                    EloManager.Instance.UpdateElo(1f, 1200);
        }

        base.OnClientDisconnect();
    }
}