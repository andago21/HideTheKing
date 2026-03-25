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

    private Piece _attacker;
    private Piece _defender;
    private bool  _battleActive = false;

    private List<GameObject> _hiddenObjects   = new List<GameObject>();
    private GameObject       _localFPSBody    = null; // unsichtbarer FPS-Koerper

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

        Piece attacker = board.boardPieces[ax, ay];
        Piece defender = board.boardPieces[dx, dy];
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

        // Kamera direkt auf Figur-Kopf positionieren
        // Figur-Höhe schätzen (Figur-Bounds oder feste Höhe)
        float headHeight = GetFigureHeadHeight(myFigure);
        Vector3 headPos  = position;
        headPos.y        = myFigure.position.y + headHeight;

        body.transform.position = headPos;

        // Richtung zum Gegner
        Vector3 dir = lookAt - headPos;
        dir.y = 0;
        if (dir != Vector3.zero)
            body.transform.rotation = Quaternion.LookRotation(dir);

        // WeaponHolder — Child des FPSBody, für lokale FPS-Ansicht
        GameObject weaponHolder = new GameObject("WeaponHolder");
        weaponHolder.transform.SetParent(body.transform);
        weaponHolder.transform.localPosition = new Vector3(0.3f, -0.2f, 0.5f);
        weaponHolder.transform.localRotation = Quaternion.identity;

        // FigureWeaponHolder — auf der Figur, für anderen Spieler sichtbar
        Transform figureWeaponHolder = myFigure.Find("FigureWeaponHolder");
        if (figureWeaponHolder != null)
        {
            // Weapon-Prefab auf Figur spawnen (für anderen Spieler)
            ThemeWeaponRegistry registry = ThemeWeaponRegistry.Instance;
            if (registry != null && registry.weaponPrefabFigure != null)
            {
                GameObject figWeapon = Instantiate(registry.weaponPrefabFigure, figureWeaponHolder);
                figWeapon.transform.localPosition = Vector3.zero;
                figWeapon.transform.localRotation = Quaternion.identity;
                figWeapon.SetActive(true);
                // Beim FPS Ende wird es mit der Figur-Cleanup entfernt
            }
        }

        // FPSController hinzufügen
        FPSController ctrl = body.AddComponent<FPSController>();
        ctrl.Initialize(cam, stats.moveSpeed, stats.mouseSensitivity);
        ctrl.PlaceAtPosition(headPos, lookAt);
        ctrl.SetBattleActive(true);

        // Position-Callback
        Piece myPiece = myFigure.GetComponent<Piece>();
        ctrl.onPositionChanged = (newPos) =>
        {
            if (NetworkServer.active)
                RpcSyncFigurePosition(myPiece.position.x, myPiece.position.y, newPos.x, newPos.y, newPos.z);
            else
                CmdSyncFigurePosition(myPiece.position.x, myPiece.position.y, newPos.x, newPos.y, newPos.z);
        };

        // FPSWeapon hinzufügen
        FPSWeapon weapon = body.AddComponent<FPSWeapon>();
        weapon.fpsCamera   = cam;
        weapon.damage      = stats.damage;
        weapon.fireRate    = stats.fireRate;
        weapon.bulletRange = stats.bulletRange;
        weapon.weaponType  = stats.weaponType;
        weapon.weaponHolder = weaponHolder.transform;

        // Weapon-Prefab für FPS-Ansicht spawnen
        ThemeWeaponRegistry reg = ThemeWeaponRegistry.Instance;
        if (reg != null && reg.weaponPrefabFPS != null)
        {
            GameObject fpsWeaponObj = Instantiate(reg.weaponPrefabFPS, weaponHolder.transform);
            fpsWeaponObj.transform.localPosition = Vector3.zero;
            fpsWeaponObj.transform.localRotation = Quaternion.identity;
        }

        weapon.SetBattleActive(true);

        return body;
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

        // 1. FPS-Körper zerstören
        if (_localFPSBody != null)
        {
            Destroy(_localFPSBody);
            _localFPSBody = null;
        }

        // 2. Health-Komponenten entfernen
        CleanupHealth(attacker);
        CleanupHealth(defender);

        // 3. Stats entfernen
        CleanupStats(attacker);
        CleanupStats(defender);

        // 4. Kamera wiederherstellen
        ChessCameraController camCtrl = FindObjectOfType<ChessCameraController>();
        if (camCtrl != null) camCtrl.RestoreFromFPS();

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
                    // Snap zur korrekten Brett-Position — FPSBody hat sie verschoben
                    Vector3 correctPos = board.squares[dx * 8 + dy].position;
                    correctPos.y = attacker.transform.position.y;
                    attacker.transform.position = correctPos;
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
                // Defender bleibt auf seinem Feld — snap zur korrekten Position
                if (defender != null)
                {
                    Vector3 correctPos = board.squares[dx * 8 + dy].position;
                    correctPos.y = defender.transform.position.y;
                    defender.transform.position = correctPos;
                }
                Debug.Log("[BattleChess] Defender won");
            }

            // En Passant Target zuruecksetzen — Figur kam durch FPS, nicht durch Zwei-Felder-Zug
            board.enPassantTarget = new Vector2Int(-1, -1);

            // Turn-Wechsel: nur der Host setzt isWhiteTurn
            // Der Angreifer hat den Zug gemacht — nach dem Kampf ist der Gegner dran
            // PlayerInput hat den Turn NICHT umgeschaltet (wegen early return)
            // Also schalten wir hier genau einmal um
            if (NetworkServer.active)
                board.isWhiteTurn = !board.isWhiteTurn;
        }

        // 6. Andere Figuren wiederherstellen
        RestoreHiddenPieces();

        // 7. Input wiederherstellen
        PlayerInput input = FindObjectOfType<PlayerInput>();
        if (input != null) input.enabled = true;

        // 8. Cursor zurücksetzen
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible   = true;

        Debug.Log("[BattleChess] Normal chess resumed.");
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