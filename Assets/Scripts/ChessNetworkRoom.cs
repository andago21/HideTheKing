using UnityEngine;
using Mirror;
using UnityEngine.SceneManagement;

public class ChessNetworkRoom : NetworkManager
{
    private bool _disconnectHandled = false;

    public override void OnServerDisconnect(NetworkConnectionToClient conn)
    {
        // Wenn der Server selbst schliesst, feuert OnServerDisconnect mit der eigenen
        // localConnection — in diesem Fall gibt es keine anderen Verbindungen mehr
        // NetworkServer.connections beinhaltet noch die trennende Verbindung,
        // also prüfen wir ob es mehr als 1 gibt (der Host selbst zählt auch)
        if (NetworkServer.connections.Count <= 1)
        {
            // Nur wir selbst — Server schliesst, kein echter Client hat disconnected
            base.OnServerDisconnect(conn);
            return;
        }

        if (!_disconnectHandled)
        {
            _disconnectHandled = true;
            BoardManager board = FindObjectOfType<BoardManager>();
            if (board != null && board.gameState == GameState.Playing)
            {
                board.HandleGameEnd(GameState.WhiteWins);
                if (SceneManager.GetActiveScene().name.Contains("Classic"))
                    if (EloManager.Instance != null)
                        EloManager.Instance.UpdateElo(1f, 1200);
            }
        }

        base.OnServerDisconnect(conn);
    }

    public override void OnClientDisconnect()
    {
        // Host feuert das auch — ignorieren
        if (NetworkServer.active) { base.OnClientDisconnect(); return; }

        if (!_disconnectHandled)
        {
            _disconnectHandled = true;
            BoardManager board = FindObjectOfType<BoardManager>();
            if (board != null && board.gameState == GameState.Playing)
            {
                board.HandleGameEnd(GameState.BlackWins);
                if (SceneManager.GetActiveScene().name.Contains("Classic"))
                    if (EloManager.Instance != null)
                        EloManager.Instance.UpdateElo(1f, 1200);
            }
        }

        base.OnClientDisconnect();
    }

    public override void OnStopServer() { _disconnectHandled = false; base.OnStopServer(); }
    public override void OnStopClient() { _disconnectHandled = false; base.OnStopClient(); }
}