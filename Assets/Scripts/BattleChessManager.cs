using UnityEngine;
using Mirror;
using System.Collections.Generic;

/// <summary>
/// Koordiniert den Battle Chess FPS Modus.
/// Spawnt unsichtbare FPS-Koerper neben den Figuren statt die Figuren selbst zu bewegen.
/// </summary>
public class BattleChessManager : NetworkBehaviour
{
    public static BattleChessManager Instance;

    [Header("Battle Settings")]
    public float battleStartDistance = 3f;
    public float cameraHeightOffset  = 1.6f;

    [Header("UI")]
    public GameObject crosshair;



    private Piece _attacker;
    private Piece _defender;
    private bool  _battleActive = false;

    private List<GameObject> _hiddenObjects      = new List<GameObject>();
    private GameObject       _localFPSBody        = null;
    private float            _attackerOriginalY   = 0f;
    private float            _defenderOriginalY   = 0f;
    private GameObject       _spawnedFigureWeapon = null;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void RequestBattle(Piece attacker, Piece defender)
    {
        // König niemals im FPS — normale Capture-Logik stattdessen
        if (attacker.type == PieceType.King || defender.type == PieceType.King)
        {
            Debug.Log("[BattleChess] König ist beteiligt — kein FPS Battle");
            return;
        }

        if (!isServer)
        {
            CmdRequestBattle(
                attacker.position.x, attacker.position.y,
                defender.position.x, defender.position.y
            );
        }
        else
        {
            ServerStartBattle(attacker, defender);
        }
    }

    [Command(requiresAuthority = false)]
    private void CmdRequestBattle(int ax, int ay, int dx, int dy)
    {
        BoardManager board = FindObjectOfType<BoardManager>();
        if (board == null) return;

        // Find pieces by stored position — they may have moved visually during FPS
        // but their logical board position (ax,ay) and (dx,dy) never changed
        Piece attacker = board.boardPieces[ax, ay];
        Piece defender = board.boardPieces[dx, dy];

        // Snap visual positions back to board squares immediately
        // so they are in a known correct position before we apply results
        if (attacker != null)
        {
            Vector3 snapA = board.squares[ax * 8 + ay].position;
            snapA.y = attacker.transform.position.y;
            attacker.transform.position = snapA;
        }
        if (defender != null)
        {
            Vector3 snapD = board.squares[dx * 8 + dy].position;
            snapD.y = defender.transform.position.y;
            defender.transform.position = snapD;
        }
        if (attacker == null || defender == null) return;

        ServerStartBattle(attacker, defender);
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
            attacker.position.x, attacker.position.y,
            defender.position.x, defender.position.y
        );
    }

    [ClientRpc]
    private void RpcSetupBattle(int ax, int ay, int dx, int dy)
    {
        BoardManager board = FindObjectOfType<BoardManager>();
        if (board == null) return;

        Piece attacker = board.boardPieces[ax, ay];
        Piece defender = board.boardPieces[dx, dy];
        if (attacker != null) _attackerOriginalY = attacker.transform.position.y;
        if (defender != null) _defenderOriginalY = defender.transform.position.y;
        if (attacker == null || defender == null) return;

        // 1. Kamera speichern
        ChessCameraController camCtrl = FindObjectOfType<ChessCameraController>();
        if (camCtrl == null) { Debug.LogError("[BattleChess] ChessCameraController not found!"); return; }
        camCtrl.SaveAndDisableForFPS();
        Camera mainCam = camCtrl.GetMainCamera();

        // 2. Andere Figuren ausblenden
        HideOtherPieces(attacker, defender);

        // 3. Input deaktivieren
        PlayerInput input = FindObjectOfType<PlayerInput>();
        if (input != null) input.enabled = false;

        // 4. Positionen berechnen
        Vector3 center      = (attacker.transform.position + defender.transform.position) / 2f;
        Vector3 direction   = (defender.transform.position - attacker.transform.position).normalized;
        direction.y         = 0;
        if (direction == Vector3.zero) direction = Vector3.forward;

        Vector3 attackerPos = center - direction * (battleStartDistance / 2f);
        Vector3 defenderPos = center + direction * (battleStartDistance / 2f);
        // Y auf Bretthöhe setzen
        attackerPos.y = attacker.transform.position.y;
        defenderPos.y = defender.transform.position.y;

        // 5. Lokalen Spieler bestimmen
        ChessNetworkManager localMgr = ChessNetworkManager.LocalInstance;
        if (localMgr == null) return;

        bool localIsWhite = localMgr.isWhitePlayer;
        Piece myFigure    = (attacker.isWhite == localIsWhite) ? attacker : defender;
        Piece enemyFigure = (myFigure == attacker)             ? defender : attacker;

        Vector3 myPos    = (myFigure == attacker) ? attackerPos : defenderPos;
        Vector3 enemyPos = (myFigure == attacker) ? defenderPos : attackerPos;

        // 6. Stats für meine Figur
        FigureStats stats = myFigure.GetComponent<FigureStats>();
        if (stats == null)
        {
            stats = myFigure.gameObject.AddComponent<FigureStats>();
            stats.ApplyDefaults(myFigure.type);
        }

        // 7. HP auf beiden Figuren initialisieren
        SetupHealth(attacker, ax, ay);
        SetupHealth(defender, dx, dy);

        // 8. Unsichtbaren FPS-Körper erstellen
        _localFPSBody = CreateFPSBody(myPos, enemyPos, mainCam, stats, myFigure.transform);

        // Crosshair aktivieren
        if (crosshair != null) crosshair.SetActive(true);

        Debug.Log($"[BattleChess] Setup complete. I am {myFigure.type}, facing {enemyFigure.type}");
    }

    private void SetupHealth(Piece figure, int row, int col)
    {
        FigureStats stats = figure.GetComponent<FigureStats>();
        if (stats == null)
        {
            stats = figure.gameObject.AddComponent<FigureStats>();
            stats.ApplyDefaults(figure.type);
        }

        FPSHealth health = figure.GetComponent<FPSHealth>();
        if (health == null) health = figure.gameObject.AddComponent<FPSHealth>();
        health.ownerPiece = figure;
        health.Initialize(stats.maxHealth);
    }

    /// <summary>
    /// Erstellt einen unsichtbaren Capsule-Körper als FPS-Charakter.
    /// Die Schachfigur selbst wird nicht bewegt.
    /// </summary>
    private GameObject CreateFPSBody(Vector3 position, Vector3 lookAt, Camera cam, FigureStats stats, Transform myFigure)
    {
        // Unsichtbarer Körper
        GameObject body = new GameObject("FPSBody_Local");
        body.layer      = LayerMask.NameToLayer("Default");

        // CharacterController — klein weil Figur klein ist
        CharacterController cc = body.AddComponent<CharacterController>();
        cc.height = 0.5f;
        cc.radius = 0.2f;
        cc.center = new Vector3(0, 0.25f, 0);

        // FPSBody auf Brett-Höhe positionieren (nicht Kopfhöhe)
        // Kamera wird separat auf Kopfhöhe gesetzt in PlaceAtPosition
        body.transform.position = position;

        // Richtung zum Gegner
        Vector3 dir = lookAt - position;
        dir.y = 0;
        if (dir != Vector3.zero)
            body.transform.rotation = Quaternion.LookRotation(dir);

        // No WeaponHolder needed — weapon attaches directly to camera

        // Reduce near clip plane to avoid clipping through figures
        cam.nearClipPlane = 0.3f;

        // FPSController hinzufügen
        float headHeight = GetFigureHeadHeight(myFigure);
        FPSController ctrl = body.AddComponent<FPSController>();
        ctrl.Initialize(cam, stats.moveSpeed, stats.mouseSensitivity);
        ctrl.cameraHeightOffset = headHeight;
        ctrl.PlaceAtPosition(position, lookAt);
        ctrl.SetBattleActive(true);

        // Position Callback
        Piece myPiece = myFigure.GetComponent<Piece>();
        ctrl.onPositionChanged = (newPos) =>
        {
            if (NetworkServer.active)
                RpcSyncFigurePosition(myPiece.position.x, myPiece.position.y, newPos.x, newPos.y, newPos.z);
            else
                CmdSyncFigurePosition(myPiece.position.x, myPiece.position.y, newPos.x, newPos.y, newPos.z);
        };

        // Rotation Callback
        ctrl.onRotationChanged = (rotY) =>
        {
            if (NetworkServer.active)
                RpcSyncFigureRotation(myPiece.position.x, myPiece.position.y, rotY);
            else
                CmdSyncFigureRotation(myPiece.position.x, myPiece.position.y, rotY);
        };

        // FPSWeapon — weapon attaches directly to camera
        FPSWeapon weapon   = body.AddComponent<FPSWeapon>();
        weapon.fpsCamera   = cam;
        weapon.damage      = stats.damage;
        weapon.fireRate    = stats.fireRate;
        weapon.bulletRange = stats.bulletRange;
        weapon.weaponType  = stats.weaponType;

        ThemeWeaponRegistry reg = ThemeWeaponRegistry.Instance;
        if (reg != null && reg.weaponPrefabFPS != null)
            weapon.AttachWeaponToCamera(reg.weaponPrefabFPS);

        weapon.SetBattleActive(true);

        // Tell other player to show weapon on enemy figure
        Piece myPieceRef = myFigure.GetComponent<Piece>();
        if (NetworkServer.active)
            RpcShowEnemyWeapon(myPieceRef.position.x, myPieceRef.position.y);
        else
            CmdShowEnemyWeapon(myPieceRef.position.x, myPieceRef.position.y);

        return body;
    }

    [Command(requiresAuthority = false)]
    private void CmdShowEnemyWeapon(int row, int col)
    {
        RpcShowEnemyWeapon(row, col);
    }

    [ClientRpc]
    private void RpcShowEnemyWeapon(int row, int col)
    {
        // Only show on the OTHER player's screen
        ChessNetworkManager localMgr = ChessNetworkManager.LocalInstance;
        if (localMgr == null) return;

        BoardManager board = FindObjectOfType<BoardManager>();
        if (board == null) return;

        Piece figure = board.boardPieces[row, col];
        if (figure == null) return;

        // Skip if this is MY figure — I already have weapon in hand
        bool localIsWhite = localMgr.isWhitePlayer;
        if (figure.isWhite == localIsWhite) return;

        // Show weapon on enemy figure
        Transform holder = figure.transform.Find("FigureWeaponHolder");
        ThemeWeaponRegistry reg = ThemeWeaponRegistry.Instance;
        if (holder == null || reg == null || reg.weaponPrefabFPS == null) return;

        // Remove existing weapons first
        foreach (Transform child in holder)
            Destroy(child.gameObject);

        GameObject enemyWeapon = Instantiate(reg.weaponPrefabFPS, holder);
        ThemeWeaponRegistry r = ThemeWeaponRegistry.Instance;
        enemyWeapon.transform.localPosition = r != null ? r.enemyWeaponPosition : new Vector3(0f, 0f, 0.01f);
        enemyWeapon.transform.localRotation = Quaternion.Euler(r != null ? r.enemyWeaponRotation : new Vector3(0f, 90f, 0f));
        enemyWeapon.transform.localScale    = Vector3.one * (r != null ? r.enemyWeaponScale : 0.0015f);
        enemyWeapon.SetActive(true);
        _spawnedFigureWeapon = enemyWeapon;
    }

    /// <summary>
    /// Schätzt die Kopfhöhe der Figur basierend auf ihrem Renderer.
    /// </summary>
    private float GetFigureHeadHeight(Transform figure)
    {
        Renderer[] renderers = figure.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return 0.3f;

        Bounds bounds = renderers[0].bounds;
        foreach (var r in renderers)
            bounds.Encapsulate(r.bounds);

        // Kopf = obere Hälfte der Figur
        return bounds.size.y * 0.85f;
    }





    [Command(requiresAuthority = false)]
    private void CmdSyncFigureRotation(int row, int col, float rotY)
    {
        RpcSyncFigureRotation(row, col, rotY);
    }

    [ClientRpc]
    private void RpcSyncFigureRotation(int row, int col, float rotY)
    {
        BoardManager board = FindObjectOfType<BoardManager>();
        if (board == null) return;

        Piece figure = board.boardPieces[row, col];
        if (figure == null) return;

        ChessNetworkManager localMgr = ChessNetworkManager.LocalInstance;
        if (localMgr == null) return;
        if (figure.isWhite == localMgr.isWhitePlayer) return;

        Vector3 euler = figure.transform.eulerAngles;
        euler.y = rotY;
        figure.transform.eulerAngles = euler;
    }

    [Command(requiresAuthority = false)]
    private void CmdSyncFigurePosition(int row, int col, float x, float y, float z)
    {
        RpcSyncFigurePosition(row, col, x, y, z);
    }

        /// <summary>
    /// Synchronisiert die Figur-Position waehrend FPS-Kampf auf alle Clients.
    /// Wird aufgerufen wenn der lokale Spieler sich bewegt.
    /// </summary>
    [ClientRpc]
    private void RpcSyncFigurePosition(int row, int col, float x, float y, float z)
    {
        BoardManager board = FindObjectOfType<BoardManager>();
        if (board == null) return;

        Piece figure = board.boardPieces[row, col];
        if (figure == null) return;

        // Only move the ENEMY figure — own figure is controlled locally
        ChessNetworkManager localMgr = ChessNetworkManager.LocalInstance;
        if (localMgr != null && figure.isWhite == localMgr.isWhitePlayer) return;

        figure.transform.position = new Vector3(x, y, z);
    }

    /// <summary>
    /// Wird lokal von FPSHealth aufgerufen — sendet Schaden zum Server.
    /// BattleChessManager hat eine NetworkIdentity, kann also Commands senden.
    /// </summary>
    [Command(requiresAuthority = false)]
    public void CmdApplyDamage(int row, int col, float amount)
    {
        BoardManager board = FindObjectOfType<BoardManager>();
        if (board == null) return;

        Piece figure = board.boardPieces[row, col];
        if (figure == null) return;

        FPSHealth health = figure.GetComponent<FPSHealth>();
        if (health != null)
            health.ApplyDamage(amount);
    }

    [Server]
    public void OnFigureDied(Piece deadPiece)
    {
        if (!_battleActive) return;

        bool attackerDied = (deadPiece == _attacker);
        _battleActive     = false;

        Debug.Log($"[BattleChess] {deadPiece.type} died. AttackerDied={attackerDied}");

        RpcEndBattle(
            _attacker.position.x, _attacker.position.y,
            _defender.position.x, _defender.position.y,
            attackerDied
        );
    }

    [ClientRpc]
    private void RpcEndBattle(int ax, int ay, int dx, int dy, bool attackerDied)
    {
        BoardManager board = FindObjectOfType<BoardManager>();
        if (board == null) return;

        Piece attacker = board.boardPieces[ax, ay];
        Piece defender = board.boardPieces[dx, dy];

        // 1. FPS-Körper und Waffe zerstören
        if (_localFPSBody != null)
        {
            FPSWeapon w = _localFPSBody.GetComponent<FPSWeapon>();
            if (w != null) w.DestroyWeaponModel();
            Destroy(_localFPSBody);
            _localFPSBody = null;
        }

// Cleanup enemy weapon visible on figure
        if (_spawnedFigureWeapon != null)
        {
            Destroy(_spawnedFigureWeapon);
            _spawnedFigureWeapon = null;
        }

        // 2. Health-Komponenten entfernen
        CleanupHealth(attacker);
        CleanupHealth(defender);

        // 3. Stats entfernen
        CleanupStats(attacker);
        CleanupStats(defender);

        // 4. Kamera wiederherstellen
        ChessCameraController camCtrl = FindObjectOfType<ChessCameraController>();
        if (camCtrl != null)
        {
            camCtrl.RestoreFromFPS();
            // Restore default near clip plane
            Camera mainCam = camCtrl.GetMainCamera();
            if (mainCam != null) mainCam.nearClipPlane = 3f;
        }

        // 5. Schachbrett-Ergebnis anwenden
        if (board != null)
        {
            if (!attackerDied)
            {
                Vector2Int newPos = new Vector2Int(dx, dy);
                if (defender != null)
                {
                    board.boardPieces[dx, dy] = null;
                    board.SendToSide(defender);
                }
                if (attacker != null)
                {
                    board.boardPieces[ax, ay] = null;
                    board.boardPieces[dx, dy] = attacker;
                    attacker.position         = newPos;
                    attacker.hasMoved         = true;
                    // Use the square's exact position, only keep the piece's own Y offset
                    Vector3 squarePos = board.squares[dx * 8 + dy].position;
                    squarePos.y = _attackerOriginalY;
                    attacker.transform.position = squarePos;
                }
                Debug.Log("[BattleChess] Attacker won");
            }
            else
            {
                if (attacker != null)
                {
                    board.boardPieces[ax, ay] = null;
                    board.SendToSide(attacker);
                }
                // Defender stays on its field — snap to exact square position
                if (defender != null)
                {
                    Vector3 squarePos = board.squares[dx * 8 + dy].position;
                    squarePos.y = _defenderOriginalY;
                    defender.transform.position = squarePos;
                }
                Debug.Log("[BattleChess] Defender won");
            }

            // En Passant Target zuruecksetzen — Figur kam durch FPS, nicht durch Zwei-Felder-Zug
            board.enPassantTarget = new Vector2Int(-1, -1);

            // Turn-Wechsel: nur der Host setzt isWhiteTurn
            if (NetworkServer.active)
                board.isWhiteTurn = !board.isWhiteTurn;

            // Checkmate/Stalemate — nur Server prüft und sendet Ergebnis
            if (NetworkServer.active)
            {
                try
                {
                    GameRules gameRules = FindObjectOfType<GameRules>();
                    if (gameRules != null)
                    {
                        if (gameRules.boardManager == null)
                            gameRules.boardManager = board;
                        // board.isWhiteTurn is already switched — check conditions for the NEW current player
                        gameRules.CheckGameEndConditions(!board.isWhiteTurn);
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogError("[BattleChess] CheckGameEndConditions error: " + e.Message);
                }

                if (board.gameState != GameState.Playing)
                {
                    ChessNetworkManager net = ChessNetworkManager.LocalInstance;
                    if (net != null)
                        net.SendGameEnd(board.gameState);
                    else
                        RpcForceGameEnd((int)board.gameState);
                }
            }
        }

        // 6. Andere Figuren wiederherstellen
        RestoreHiddenPieces();

        // 7. Input wiederherstellen — nur wenn Spiel noch läuft
        if (board == null || board.gameState == GameState.Playing)
        {
            PlayerInput input = FindObjectOfType<PlayerInput>();
            if (input != null) input.enabled = true;
        }

        // 8. Cursor IMMER zurücksetzen
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible   = true;
        if (crosshair != null) crosshair.SetActive(false);

        Debug.Log("[BattleChess] Normal chess resumed.");
    }

    [ClientRpc]
    private void RpcForceGameEnd(int result)
    {
        BoardManager board = FindObjectOfType<BoardManager>();
        if (board != null) board.HandleGameEnd((GameState)result);
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

    private void CleanupHealth(Piece figure)
    {
        if (figure == null) return;
        FPSHealth h = figure.GetComponent<FPSHealth>();
        if (h != null) Destroy(h);
    }

    private void CleanupStats(Piece figure)
    {
        if (figure == null) return;
        FigureStats s = figure.GetComponent<FigureStats>();
        if (s != null) Destroy(s);
    }
}