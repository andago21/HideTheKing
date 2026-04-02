using UnityEngine;
using Mirror;
using UnityEngine.SceneManagement;

public class ChessNetworkRoom : NetworkManager
{
    private bool _disconnectHandled = false;

    public override void OnServerDisconnect(NetworkConnectionToClient conn)
    {
        if (NetworkServer.connections.Count <= 1)
        {
            base.OnServerDisconnect(conn);
            return;
        }

        if (!_disconnectHandled)
        {
            _disconnectHandled = true;
            BoardManager board = FindObjectOfType<BoardManager>();
            if (board != null && board.gameState == GameState.Playing)
                board.HandleGameEnd(GameState.WhiteWins);
                // Kein ELO bei Disconnect
        }

        base.OnServerDisconnect(conn);
    }

    public override void OnClientDisconnect()
    {
        if (NetworkServer.active) { base.OnClientDisconnect(); return; }

        if (!_disconnectHandled)
        {
            _disconnectHandled = true;
            BoardManager board = FindObjectOfType<BoardManager>();
            if (board != null && board.gameState == GameState.Playing)
                board.HandleGameEnd(GameState.BlackWins);
                // Kein ELO bei Disconnect
        }

        base.OnClientDisconnect();
    }

    public override void OnStopServer() { _disconnectHandled = false; base.OnStopServer(); }
    public override void OnStopClient() { _disconnectHandled = false; base.OnStopClient(); }
}