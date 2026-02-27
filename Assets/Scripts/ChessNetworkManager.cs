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
                // Find all instances and get the local one
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

    private void Start()
    {
        Debug.Log("===== ChessNetworkManager START =====");
        Debug.Log("isServer: " + isServer);
        Debug.Log("isClient: " + isClient);
        Debug.Log("isLocalPlayer: " + isLocalPlayer);
        
        // Find BoardManager in the scene
        if (boardManager == null)
        {
            boardManager = FindObjectOfType<BoardManager>();
            if (boardManager == null)
            {
                Debug.LogError("BoardManager not found in scene!");
            }
            else
            {
                Debug.Log("BoardManager found successfully!");
            }
        }
        
        // Only the local player should determine their color
        if (!isLocalPlayer)
        {
            Debug.Log("This is NOT the local player instance, skipping setup");
            return;
        }
        
        Debug.Log("This IS the local player!");
        
        // Set as local instance
        _localInstance = this;
        
        // Determine player color
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
        
        Debug.Log("===== END ChessNetworkManager START =====");
    }

    private void OnDestroy()
    {
        if (_localInstance == this)
        {
            _localInstance = null;
        }
    }

    // Called from PlayerInput when a move is made
    public void SendMove(Vector2Int from, Vector2Int to)
    {
        if (!NetworkClient.active && !NetworkServer.active)
        {
            // Single player mode - not networked
            return;
        }

        Debug.Log("Sending move: " + from + " -> " + to);

        // Send move to server (or execute locally if we are the server)
        if (isServer)
        {
            // We're the server, execute locally and tell clients
            RpcReceiveMove(from.x, from.y, to.x, to.y);
        }
        else
        {
            // We're a client, ask server to execute
            CmdSendMove(from.x, from.y, to.x, to.y);
        }
    }

    // Client sends move to server
    [Command(requiresAuthority = false)]
    private void CmdSendMove(int fromX, int fromY, int toX, int toY)
    {
        Debug.Log("Server received move from client: (" + fromX + "," + fromY + ") -> (" + toX + "," + toY + ")");
        
        // Server tells all clients about the move
        RpcReceiveMove(fromX, fromY, toX, toY);
    }

    [ClientRpc]
    private void RpcReceiveMove(int fromX, int fromY, int toX, int toY)
    {
        Vector2Int from = new Vector2Int(fromX, fromY);
        Vector2Int to = new Vector2Int(toX, toY);

        Debug.Log("RPC Received - isLocalPlayer: " + isLocalPlayer + ", Move: " + from + " -> " + to);

        // Only execute if this is NOT the local player who made the move
        // In Host+Client mode, the host's local player will receive this but should skip
        if (isLocalPlayer)
        {
            Debug.Log("Skipping - this is the local player who made the move");
            return;
        }

        // Also check if we even have authority to move pieces
        PlayerInput playerInput = FindObjectOfType<PlayerInput>();
        if (playerInput != null)
        {
            Debug.Log("Executing move for remote player");
            playerInput.ExecuteNetworkMove(from, to);
        }
    }

    // Tells client to run the same check so everyone sees the result.
    public void SendGameEnd(GameState result)
    {
        if (!NetworkClient.active && !NetworkServer.active)
            return;

        if (isServer)
        {
            // Host already has the result — just broadcast it to clients
            RpcReceiveGameEnd((int)result);
        }
        else
        {
            // Client sends the result up to the server, server broadcasts
            CmdSendGameEnd((int)result);
        }
    }

    [Command(requiresAuthority = false)]
    private void CmdSendGameEnd(int result)
    {
        RpcReceiveGameEnd(result);
    }

    [ClientRpc]
    private void RpcReceiveGameEnd(int result)
    {
        Debug.Log("RpcReceiveGameEnd received: " + (GameState)result);

        GameRules gameRules = FindObjectOfType<GameRules>();
        if (gameRules != null)
        {
            // Trigger the same game-end logic on every client
            boardManager.HandleGameEnd((GameState)result);
        }
    }

    // Check if it's this player's turn
    public bool IsMyTurn()
    {
        if (!NetworkClient.active && !NetworkServer.active)
        {
            // Single player mode - always your turn
            return true;
        }

        // White's turn and I'm white, OR Black's turn and I'm black
        return (boardManager.isWhiteTurn && isWhitePlayer) || 
               (!boardManager.isWhiteTurn && !isWhitePlayer);
    }

    // Check if we're in a multiplayer game
    public bool IsMultiplayer()
    {
        return NetworkClient.active || NetworkServer.active;
    }
}