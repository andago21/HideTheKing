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

    [SyncVar] public bool isWhitePlayer;
    public static bool LocalIsWhite = false;

    private static int  _connectedPlayers = 0;
    private static bool _gameStarted      = false;
    private static bool _eloGiven         = false; // Verhindert doppeltes ELO

    private void Start()
    {
        if (boardManager == null)
            boardManager = FindObjectOfType<BoardManager>();

        if (!isLocalPlayer) return;

        _localInstance = this;
        _eloGiven      = false;

        if (isServer)      { isWhitePlayer = true;  Debug.Log("You are the HOST - Playing as WHITE"); }
        else if (isClient) { isWhitePlayer = false; Debug.Log("You are the CLIENT - Playing as BLACK"); }

        LocalIsWhite = isWhitePlayer;
    }

    public override void OnStartServer()
    {
        _connectedPlayers++;
        Debug.Log($"[CNM] OnStartServer fired. Total={_connectedPlayers}");

        if (_connectedPlayers >= 2 && !_gameStarted)
        {
            _gameStarted      = true;
            _connectedPlayers = 0;
            _eloGiven         = false;
            Invoke(nameof(DelayedStart), 0.5f);
        }
    }

    private void DelayedStart() { RpcStartGame(); }

    public override void OnStopServer()
    {
        _connectedPlayers = 0;
        _gameStarted      = false;
        _eloGiven         = false;
    }

    [ClientRpc]
    public void RpcStartGame()
    {
        BoardManager board = FindObjectOfType<BoardManager>();
        if (board != null) board.SetupBoard();
        ChessTimer timer = FindObjectOfType<ChessTimer>();
        if (timer != null) timer.StartTimer();

        if (MusicManager.Instance != null)
            MusicManager.Instance.StartThemeMusic();
    }

    private void OnDestroy()
    {
        if (_localInstance == this) _localInstance = null;
    }

    // ── Züge ──
    public void SendMove(Vector2Int from, Vector2Int to)
    {
        if (!NetworkClient.active && !NetworkServer.active) return;
        if (isServer) RpcReceiveMove(from.x, from.y, to.x, to.y);
        else          CmdSendMove(from.x, from.y, to.x, to.y);
    }

    [Command(requiresAuthority = false)]
    private void CmdSendMove(int fx, int fy, int tx, int ty) { RpcReceiveMove(fx, fy, tx, ty); }

    [ClientRpc]
    private void RpcReceiveMove(int fx, int fy, int tx, int ty)
    {
        if (isLocalPlayer) return;
        PlayerInput pi = FindObjectOfType<PlayerInput>();
        if (pi != null) pi.ExecuteNetworkMove(new Vector2Int(fx, fy), new Vector2Int(tx, ty));
    }

    // ── Spielende ──
    public void SendGameEnd(GameState result)
    {
        if (!NetworkClient.active && !NetworkServer.active) return;
        if (isServer) RpcReceiveGameEnd((int)result);
        else          CmdSendGameEnd((int)result);
    }

    [Command(requiresAuthority = false)]
    private void CmdSendGameEnd(int result) { RpcReceiveGameEnd(result); }

    [ClientRpc]
    private void RpcReceiveGameEnd(int result)
    {
        if (boardManager != null) boardManager.HandleGameEnd((GameState)result);
    }

    // ── ELO Sync ──
    public void SendEloSync(GameState result)
    {
        if (!IsMultiplayer()) return;
        if (_eloGiven)        return; // Verhindert doppeltes ELO

        if (isServer) CmdDoEloSync((int)result);
        else          CmdDoEloSync((int)result);
    }

    [Command(requiresAuthority = false)]
    private void CmdDoEloSync(int result)
    {
        if (_eloGiven) return;
        _eloGiven = true;

        if (!UnityEngine.SceneManagement.SceneManager.GetActiveScene().name.Contains("Classic")) return;

        // Beide Spieler berechnen ELO lokal via RPC — kein Server-only
        float whiteResult = result == (int)GameState.WhiteWins ? 1f : (result == (int)GameState.BlackWins ? 0f : 0.5f);
        float blackResult = 1f - whiteResult;
        if (result == (int)GameState.Draw) blackResult = 0.5f;

        RpcDoEloLocally(whiteResult, blackResult);
    }

    [ClientRpc]
    private void RpcDoEloLocally(float whiteResult, float blackResult)
    {
        if (!isLocalPlayer) return;
        if (EloManager.Instance == null) return;

        float myResult = isWhitePlayer ? whiteResult : blackResult;
        EloManager.Instance.UpdateElo(myResult, 1200);
        Debug.Log($"[ELO] Local result={myResult} isWhite={isWhitePlayer}");
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