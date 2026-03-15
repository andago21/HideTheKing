using Mirror;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using HideTheKing.Core;

/// <summary>
/// Attached to the same NetworkObject as ChessNetworkManager (or a dedicated one).
/// The server picks HTK seeds, broadcasts them, and authoritatively resolves captures.
/// </summary>
public class HideTheKingNetworkSync : NetworkBehaviour
{
    public static HideTheKingNetworkSync Instance { get; private set; }

    // Seeds used to reproduce the same random selection on all clients
    [SyncVar] private int _whiteHiddenSeed = -1;
    [SyncVar] private int _blackHiddenSeed = -1;

    // Server-only: tracks whether game-over was already fired
    private bool _gameOverFired;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        // Generate seeds server-side
        var rng = new System.Random();
        _whiteHiddenSeed = rng.Next(100000);
        _blackHiddenSeed = rng.Next(100000);
        Debug.Log($"[HTK Network] Seeds generated – white:{_whiteHiddenSeed} black:{_blackHiddenSeed}");
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        // SyncVars are populated before this fires on clients, so we can initialize
        StartCoroutine(InitializeWhenPiecesReady());
    }

    private IEnumerator InitializeWhenPiecesReady()
    {
        // Wait until seeds are synced (non-host clients may need a frame)
        yield return new WaitUntil(() => _whiteHiddenSeed >= 0 && _blackHiddenSeed >= 0);

        // Wait until pieces are spawned in the scene
        List<Piece> pieces = new List<Piece>();
        while (pieces.Count == 0)
        {
            pieces = Object.FindObjectsOfType<Piece>(true)
                           .Where(p => p != null)
                           .ToList();
            yield return null;
        }

        Debug.Log($"[HTK Network] Initializing with {pieces.Count} pieces");

        // Initialize using deterministic seeds – same result on every client
        var whiteLogic = new HiddenTargetLogicGeneric();
        whiteLogic.Initialize(pieces, hiddenIsWhite: true, randomSeed: _whiteHiddenSeed);

        var blackLogic = new HiddenTargetLogicGeneric();
        blackLogic.Initialize(pieces, hiddenIsWhite: false, randomSeed: _blackHiddenSeed);

        // Hand the logic objects to HideTheKingManager
        HideTheKingManager.Instance?.SetNetworkLogic(whiteLogic, blackLogic);

        // Tell each local player which piece is *their* hidden target (for local UI highlight)
        ChessNetworkManager localNet = ChessNetworkManager.LocalInstance;
        if (localNet != null)
        {
            bool iAmWhite = localNet.isWhitePlayer;
            var myState = iAmWhite ? whiteLogic.Snapshot() : blackLogic.Snapshot();
            Debug.Log($"[HTK Network] YOUR hidden target: {myState.HiddenTarget?.type} " +
                      $"at {myState.HiddenTarget?.position}");
            HideTheKingManager.Instance?.OnLocalHiddenTargetRevealed(myState.HiddenTarget);
        }
    }

    // ---------------------------------------------------------------
    // Server-authoritative capture validation
    // Called by HideTheKingManager when a capture is detected locally.
    // Only the server version actually triggers game-over.
    // ---------------------------------------------------------------
    [Command(requiresAuthority = false)]
    public void CmdReportCapture(uint capturedNetId, bool capturingIsWhite)
    {
        if (!isServer || _gameOverFired) return;

        if (NetworkClient.spawned.TryGetValue(capturedNetId, out NetworkIdentity ni))
        {
            Piece captured = ni.GetComponent<Piece>();
            if (captured == null) return;

            bool triggered = HideTheKingManager.Instance != null &&
                             HideTheKingManager.Instance.ServerCheckCapture(captured, capturingIsWhite);

            if (triggered)
            {
                _gameOverFired = true;
                RpcGameOver(capturingIsWhite, "Hidden Target Captured!");
            }
        }
    }

    [ClientRpc]
    private void RpcGameOver(bool capturingIsWhite, string reason)
    {
        HideTheKingManager.Instance?.HandleNetworkGameOver(capturingIsWhite, reason);
    }
}