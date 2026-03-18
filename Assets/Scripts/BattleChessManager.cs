using UnityEngine;
using Mirror;
using System.Collections.Generic;

/// <summary>
/// Main coordinator for Battle Chess FPS mode.
/// </summary>
public class BattleChessManager : NetworkBehaviour
{
    public static BattleChessManager Instance;

    [Header("Battle Settings")]
    [Tooltip("How far apart the two figures are placed when battle starts")]
    public float battleStartDistance = 3f;

    [Tooltip("Camera height above figure base")]
    public float cameraHeightOffset = 1.5f;

    // Internal state
    private Piece _attacker;
    private Piece _defender;
    private bool  _battleActive = false;

    private List<GameObject> _hiddenObjects = new List<GameObject>();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    /// <summary>
    /// Called from PlayerInput.MovePiece() when a capture is detected in multiplayer.
    /// </summary>
    public void RequestBattle(Piece attacker, Piece defender)
    {
        if (!isServer)
        {
            CmdRequestBattle(
                attacker.GetComponent<NetworkIdentity>().netId,
                defender.GetComponent<NetworkIdentity>().netId
            );
        }
        else
        {
            ServerStartBattle(attacker, defender);
        }
    }

    [Command(requiresAuthority = false)]
    private void CmdRequestBattle(uint attackerNetId, uint defenderNetId)
    {
        if (NetworkServer.spawned.TryGetValue(attackerNetId, out NetworkIdentity aId) &&
            NetworkServer.spawned.TryGetValue(defenderNetId, out NetworkIdentity dId))
        {
            ServerStartBattle(aId.GetComponent<Piece>(), dId.GetComponent<Piece>());
        }
    }

    [Server]
    private void ServerStartBattle(Piece attacker, Piece defender)
    {
        if (_battleActive) return;

        _attacker     = attacker;
        _defender     = defender;
        _battleActive = true;

        Debug.Log($"[BattleChess] {attacker.type} vs {defender.type}");

        RpcSetupBattle(
            attacker.GetComponent<NetworkIdentity>().netId,
            defender.GetComponent<NetworkIdentity>().netId
        );
    }

    [ClientRpc]
    private void RpcSetupBattle(uint attackerNetId, uint defenderNetId)
    {
        if (!NetworkServer.spawned.TryGetValue(attackerNetId, out NetworkIdentity aId)) return;
        if (!NetworkServer.spawned.TryGetValue(defenderNetId, out NetworkIdentity dId)) return;

        Piece attacker = aId.GetComponent<Piece>();
        Piece defender = dId.GetComponent<Piece>();

        // ── 1. Get the shared camera and save its chess state ──
        ChessCameraController camCtrl = FindObjectOfType<ChessCameraController>();
        if (camCtrl == null)
        {
            Debug.LogError("[BattleChess] ChessCameraController not found!");
            return;
        }
        camCtrl.SaveAndDisableForFPS();
        Camera mainCam = camCtrl.GetMainCamera();

        // ── 2. Hide all other pieces ──
        HideOtherPieces(attacker, defender);

        // ── 3. Disable normal player input ──
        PlayerInput input = FindObjectOfType<PlayerInput>();
        if (input != null) input.enabled = false;

        // ── 4. Figure out which piece the local player controls ──
        ChessNetworkManager localMgr = ChessNetworkManager.LocalInstance;
        if (localMgr == null) return;

        bool localIsWhite   = localMgr.isWhitePlayer;
        Piece myFigure      = (attacker.isWhite == localIsWhite) ? attacker : defender;
        Piece enemyFigure   = (myFigure == attacker)             ? defender : attacker;

        // ── 5. Calculate positions ──
        Vector3 center      = (attacker.transform.position + defender.transform.position) / 2f;
        Vector3 direction   = (defender.transform.position - attacker.transform.position).normalized;
        direction.y         = 0;

        Vector3 attackerPos = center - direction * (battleStartDistance / 2f);
        Vector3 defenderPos = center + direction * (battleStartDistance / 2f);
        attackerPos.y       = attacker.transform.position.y;
        defenderPos.y       = defender.transform.position.y;

        Vector3 myPos       = (myFigure == attacker) ? attackerPos : defenderPos;
        Vector3 enemyPos    = (myFigure == attacker) ? defenderPos : attackerPos;

        // ── 6. Set up FPS on my figure only ──
        SetupFPSOnFigure(myFigure, mainCam, myPos, enemyPos);

        Debug.Log($"[BattleChess] Setup complete. I am {myFigure.type}, facing {enemyFigure.type}");
    }

    private void SetupFPSOnFigure(Piece figure, Camera mainCam, Vector3 myPos, Vector3 enemyPos)
    {
        // Add CharacterController
        CharacterController cc = figure.GetComponent<CharacterController>();
        if (cc == null) cc = figure.gameObject.AddComponent<CharacterController>();
        cc.height = 2f;
        cc.radius = 0.3f;
        cc.center = new Vector3(0, 1f, 0);

        // Get or apply stats
        FigureStats stats = figure.GetComponent<FigureStats>();
        if (stats == null)
        {
            stats = figure.gameObject.AddComponent<FigureStats>();
            stats.ApplyDefaults(figure.type);
        }

        // Add FPSHealth
        FPSHealth health = figure.GetComponent<FPSHealth>();
        if (health == null) health = figure.gameObject.AddComponent<FPSHealth>();
        health.ownerPiece = figure;
        health.Initialize(stats.maxHealth);

        // Add FPSController
        FPSController ctrl = figure.GetComponent<FPSController>();
        if (ctrl == null) ctrl = figure.gameObject.AddComponent<FPSController>();
        ctrl.Initialize(mainCam, stats.moveSpeed, stats.mouseSensitivity);
        ctrl.PlaceAtPosition(myPos, enemyPos);
        ctrl.SetBattleActive(true);

        // Add FPSWeapon
        FPSWeapon weapon = figure.GetComponent<FPSWeapon>();
        if (weapon == null) weapon = figure.gameObject.AddComponent<FPSWeapon>();
        weapon.fpsCamera = mainCam;
        weapon.Initialize(stats, mainCam);
        weapon.SetBattleActive(true);
    }

    // ── Called by FPSHealth when HP reaches 0 ──
    [Server]
    public void OnFigureDied(Piece deadPiece)
    {
        if (!_battleActive) return;

        bool attackerDied = (deadPiece == _attacker);
        _battleActive     = false;

        Debug.Log($"[BattleChess] {deadPiece.type} died. AttackerDied={attackerDied}");

        RpcEndBattle(
            _attacker.GetComponent<NetworkIdentity>().netId,
            _defender.GetComponent<NetworkIdentity>().netId,
            attackerDied
        );
    }

    [ClientRpc]
    private void RpcEndBattle(uint attackerNetId, uint defenderNetId, bool attackerDied)
    {
        if (!NetworkServer.spawned.TryGetValue(attackerNetId, out NetworkIdentity aId)) return;
        if (!NetworkServer.spawned.TryGetValue(defenderNetId, out NetworkIdentity dId)) return;

        Piece attacker = aId.GetComponent<Piece>();
        Piece defender = dId.GetComponent<Piece>();

        // ── 1. Clean up FPS components from both figures ──
        CleanupFPS(attacker);
        CleanupFPS(defender);

        // ── 2. Restore chess camera ──
        ChessCameraController camCtrl = FindObjectOfType<ChessCameraController>();
        if (camCtrl != null) camCtrl.RestoreFromFPS();

        // ── 3. Apply chess board result ──
        BoardManager board = FindObjectOfType<BoardManager>();
        if (board != null)
        {
            if (!attackerDied)
            {
                // Attacker won — move attacker to defender's square, remove defender
                Vector2Int newPos = defender.position;

                board.boardPieces[defender.position.x, defender.position.y] = null;
                board.SendToSide(defender);

                board.boardPieces[attacker.position.x, attacker.position.y] = null;
                board.boardPieces[newPos.x, newPos.y] = attacker;
                attacker.position  = newPos;
                attacker.hasMoved  = true;

                Vector3 targetWorld = board.squares[newPos.x * 8 + newPos.y].position;
                attacker.transform.position = targetWorld;

                Debug.Log($"[BattleChess] Attacker won — {attacker.type} moved to {newPos}");
            }
            else
            {
                // Defender won — attacker is removed, defender stays
                board.boardPieces[attacker.position.x, attacker.position.y] = null;
                board.SendToSide(attacker);

                Debug.Log($"[BattleChess] Defender won — {defender.type} stays at {defender.position}");
            }

            // Switch turns
            board.isWhiteTurn = !board.isWhiteTurn;
        }

        // ── 4. Restore hidden pieces ──
        RestoreHiddenPieces();

        // ── 5. Re-enable player input ──
        PlayerInput input = FindObjectOfType<PlayerInput>();
        if (input != null) input.enabled = true;

        Debug.Log("[BattleChess] Board restored. Normal chess resumed.");
    }

    private void HideOtherPieces(Piece attacker, Piece defender)
    {
        _hiddenObjects.Clear();
        foreach (Piece p in FindObjectsOfType<Piece>())
        {
            if (p == attacker || p == defender) continue;
            if (!p.gameObject.activeSelf)       continue;
            p.gameObject.SetActive(false);
            _hiddenObjects.Add(p.gameObject);
        }
    }

    private void RestoreHiddenPieces()
    {
        foreach (GameObject obj in _hiddenObjects)
            if (obj != null) obj.SetActive(true);
        _hiddenObjects.Clear();
    }

    private void CleanupFPS(Piece figure)
    {
        if (figure == null) return;

        FPSController ctrl = figure.GetComponent<FPSController>();
        if (ctrl != null) { ctrl.SetBattleActive(false); Destroy(ctrl); }

        FPSWeapon weapon = figure.GetComponent<FPSWeapon>();
        if (weapon != null) { weapon.SetBattleActive(false); Destroy(weapon); }

        FPSHealth health = figure.GetComponent<FPSHealth>();
        if (health != null) Destroy(health);

        CharacterController cc = figure.GetComponent<CharacterController>();
        if (cc != null) Destroy(cc);
    }
}