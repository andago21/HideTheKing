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

    // Static so all instances on the SERVER share the same counter
    private static int _connectedPlayers = 0;
    private static bool _gameStarted = false;

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
            _gameStarted = true;
            _connectedPlayers = 0;
            Debug.Log("Both players ready - starting game");
            Invoke(nameof(DelayedStart), 0.5f); // kurze Verzögerung
        }
    }

    private void DelayedStart()
    {
        RpcStartGame();
    }

    // Reset static state when server stops so next game starts clean
    public override void OnStopServer()
    {
        _connectedPlayers = 0;
        _gameStarted = false;
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
        Vector2Int from = new Vector2Int(fromX, fromY);
        Vector2Int to   = new Vector2Int(toX,   toY);

        if (isLocalPlayer) return;

        PlayerInput playerInput = FindObjectOfType<PlayerInput>();
        if (playerInput != null)
            playerInput.ExecuteNetworkMove(from, to);
    }

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