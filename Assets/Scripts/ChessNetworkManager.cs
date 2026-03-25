using UnityEngine;
using Mirror;

public class ChessNetworkManager : NetworkBehaviour
{
    private static ChessNetworkManager _localInstance;

    public static ChessNetworkManager LocalInstance
    {
        get
        {
            if (_localInstance == null)
            {
                ChessNetworkManager[] all = FindObjectsOfType<ChessNetworkManager>();
                foreach (var manager in all)
                {
                    if (manager.isLocalPlayer)
                    {
                        _localInstance = manager;
                        break;
                    }
                }
            }
            return _localInstance;
        }
    }

    public BoardManager boardManager;

    [SyncVar]
    public bool isWhitePlayer;

    private static int  _connectedPlayers = 0;
    private static bool _gameStarted      = false;

    private void Start()
    {
        if (boardManager == null)
            boardManager = FindObjectOfType<BoardManager>();

        if (!isLocalPlayer) return;

        _localInstance = this;

        if (isServer)
        {
            isWhitePlayer = true;
            Debug.Log("You are the HOST - Playing as WHITE");
        }
        else if (isClient)
        {
            isWhitePlayer = false;
            Debug.Log("You are the CLIENT - Playing as BLACK");
        }
    }

    public override void OnStartServer()
    {
        _connectedPlayers++;
        Debug.Log("Player spawned on server. Total: " + _connectedPlayers);

        if (_connectedPlayers >= 2 && !_gameStarted)
        {
            _gameStarted      = true;
            _connectedPlayers = 0;
            Debug.Log("Both players ready - starting game");
            Invoke(nameof(DelayedStart), 0.5f);
        }
    }

    private void DelayedStart()
    {
        RpcStartGame();
    }

    public override void OnStopServer()
    {
        _connectedPlayers = 0;
        _gameStarted      = false;
    }

    [ClientRpc]
    public void RpcStartGame()
    {
        Debug.Log("RpcStartGame received - setting up board and timer");

        BoardManager board = FindObjectOfType<BoardManager>();
        if (board != null)
            board.SetupBoard();
        else
            Debug.LogError("BoardManager not found!");

        ChessTimer timer = FindObjectOfType<ChessTimer>();
        if (timer != null)
            timer.StartTimer();
    }

    private void OnDestroy()
    {
        if (_localInstance == this)
            _localInstance = null;
    }

    // ── Züge ──
    public void SendMove(Vector2Int from, Vector2Int to)
    {
        if (!NetworkClient.active && !NetworkServer.active) return;

        if (isServer)
            RpcReceiveMove(from.x, from.y, to.x, to.y);
        else
            CmdSendMove(from.x, from.y, to.x, to.y);
    }

    [Command(requiresAuthority = false)]
    private void CmdSendMove(int fromX, int fromY, int toX, int toY)
    {
        RpcReceiveMove(fromX, fromY, toX, toY);
    }

    [ClientRpc]
    private void RpcReceiveMove(int fromX, int fromY, int toX, int toY)
    {
        if (isLocalPlayer) return;

        PlayerInput playerInput = FindObjectOfType<PlayerInput>();
        if (playerInput != null)
            playerInput.ExecuteNetworkMove(new Vector2Int(fromX, fromY), new Vector2Int(toX, toY));
    }

    // ── Spielende ──
    public void SendGameEnd(GameState result)
    {
        if (!NetworkClient.active && !NetworkServer.active) return;

        if (isServer)
            RpcReceiveGameEnd((int)result);
        else
            CmdSendGameEnd((int)result);
    }

    [Command(requiresAuthority = false)]
    private void CmdSendGameEnd(int result)
    {
        RpcReceiveGameEnd(result);
    }

    [ClientRpc]
    private void RpcReceiveGameEnd(int result)
    {
        Debug.Log("RpcReceiveGameEnd: " + (GameState)result);
        if (boardManager != null)
            boardManager.HandleGameEnd((GameState)result);
    }

    // ── ELO Synchronisation ──
    // Wird nach Spielende aufgerufen — Host sendet seine ELO zum Client
    // Beide berechnen dann ihre eigene ELO lokal
    public void SendEloSync(GameState result)
    {
        if (!IsMultiplayer())       return;
        if (EloManager.Instance == null) return;

        int myElo = EloManager.Instance.GetElo();

        if (isServer)
            RpcReceiveEloSync(myElo, (int)result);
        else
            CmdSendEloSync(myElo, (int)result);
    }

    [Command(requiresAuthority = false)]
    private void CmdSendEloSync(int clientElo, int result)
    {
        // Server (Host) empfängt Client-ELO und sendet seine eigene ELO zurück
        // Jetzt weiß der Host die ELO des Clients
        int hostElo = EloManager.Instance != null ? EloManager.Instance.GetElo() : 1200;
        RpcReceiveEloSync(hostElo, result);

        // Host berechnet seine eigene ELO mit der Client-ELO
        if (EloManager.Instance != null)
        {
            float hostResult = GetResultForWhite((GameState)result, true);
            EloManager.Instance.UpdateElo(hostResult, clientElo);
        }
    }

    [ClientRpc]
    private void RpcReceiveEloSync(int opponentElo, int result)
    {
        if (EloManager.Instance == null) return;

        // Jeder Spieler berechnet sein eigenes Ergebnis
        float myResult = isWhitePlayer
            ? GetResultForWhite((GameState)result, true)
            : GetResultForWhite((GameState)result, false);

        // Nur lokaler Spieler speichert
        if (isLocalPlayer)
            EloManager.Instance.UpdateElo(myResult, opponentElo);
    }

    // Gibt 1 (Gewinn), 0 (Verlust), 0.5 (Unentschieden) zurück
    private float GetResultForWhite(GameState state, bool forWhite)
    {
        switch (state)
        {
            case GameState.WhiteWins:
                return forWhite ? 1f : 0f;
            case GameState.BlackWins:
                return forWhite ? 0f : 1f;
            default: // Draw, Stalemate, etc.
                return 0.5f;
        }
    }

    public bool IsMyTurn()
    {
        if (!NetworkClient.active && !NetworkServer.active) return true;
        return (boardManager.isWhiteTurn && isWhitePlayer) ||
               (!boardManager.isWhiteTurn && !isWhitePlayer);
    }

    public bool IsMultiplayer()
    {
        return NetworkClient.active || NetworkServer.active;
    }
}