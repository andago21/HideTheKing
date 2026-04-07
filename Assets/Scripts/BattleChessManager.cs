using UnityEngine;
using Mirror;
using System.Collections.Generic;

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
        if (attacker.type == PieceType.King || defender.type == PieceType.King)
        {
            Debug.Log("[BattleChess] König ist beteiligt — kein FPS Battle");
            return;
        }

        if (!isServer)
            CmdRequestBattle(attacker.position.x, attacker.position.y, defender.position.x, defender.position.y);
        else
            ServerStartBattle(attacker, defender);
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
        RpcSetupBattle(attacker.position.x, attacker.position.y, defender.position.x, defender.position.y);
    }

    [ClientRpc]
    private void RpcSetupBattle(int ax, int ay, int dx, int dy)
    {
        BoardManager board = FindObjectOfType<BoardManager>();
        if (board == null) return;

        Piece attacker = board.boardPieces[ax, ay];
        Piece defender = board.boardPieces[dx, dy];
        if (attacker == null || defender == null) return;

        // Store original Y before anything moves
        _attackerOriginalY = attacker.transform.position.y;
        _defenderOriginalY = defender.transform.position.y;

        ChessCameraController camCtrl = FindObjectOfType<ChessCameraController>();
        if (camCtrl == null) { Debug.LogError("[BattleChess] ChessCameraController not found!"); return; }
        camCtrl.SaveAndDisableForFPS();
        Camera mainCam = camCtrl.GetMainCamera();

        HideOtherPieces(attacker, defender);

        PlayerInput input = FindObjectOfType<PlayerInput>();
        if (input != null) input.enabled = false;

        Vector3 center    = (attacker.transform.position + defender.transform.position) / 2f;
        Vector3 direction = (defender.transform.position - attacker.transform.position).normalized;
        direction.y = 0;
        if (direction == Vector3.zero) direction = Vector3.forward;

        Vector3 attackerPos = center - direction * (battleStartDistance / 2f);
        Vector3 defenderPos = center + direction * (battleStartDistance / 2f);
        attackerPos.y = _attackerOriginalY;
        defenderPos.y = _defenderOriginalY;

        ChessNetworkManager localMgr = ChessNetworkManager.LocalInstance;
        if (localMgr == null) return;

        bool localIsWhite = localMgr.isWhitePlayer;
        Piece myFigure    = (attacker.isWhite == localIsWhite) ? attacker : defender;
        Piece enemyFigure = (myFigure == attacker) ? defender : attacker;

        Vector3 myPos    = (myFigure == attacker) ? attackerPos : defenderPos;
        Vector3 enemyPos = (myFigure == attacker) ? defenderPos : attackerPos;

        FigureStats stats = myFigure.GetComponent<FigureStats>();
        if (stats == null)
        {
            stats = myFigure.gameObject.AddComponent<FigureStats>();
            stats.ApplyDefaults(myFigure.type);
        }

        SetupHealth(attacker, ax, ay);
        SetupHealth(defender, dx, dy);

        _localFPSBody = CreateFPSBody(myPos, enemyPos, mainCam, stats, myFigure.transform);

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

    private GameObject CreateFPSBody(Vector3 position, Vector3 lookAt, Camera cam, FigureStats stats, Transform myFigure)
    {
        GameObject body = new GameObject("FPSBody_Local");
        body.layer = LayerMask.NameToLayer("Default");

        CharacterController cc = body.AddComponent<CharacterController>();
        cc.height = 0.5f;
        cc.radius = 0.2f;
        cc.center = new Vector3(0, 0.25f, 0);

        body.transform.position = position;

        Vector3 dir = lookAt - position;
        dir.y = 0;
        if (dir != Vector3.zero)
            body.transform.rotation = Quaternion.LookRotation(dir);

        cam.nearClipPlane = 0.3f;

        float headHeight = GetFigureHeadHeight(myFigure);
        FPSController ctrl = body.AddComponent<FPSController>();
        ctrl.Initialize(cam, stats.moveSpeed, stats.mouseSensitivity);
        ctrl.cameraHeightOffset = headHeight;
        ctrl.PlaceAtPosition(position, lookAt);
        ctrl.SetBattleActive(true);

        Piece myPiece = myFigure.GetComponent<Piece>();

        // Move own figure with FPS body, sync enemy figure via RPC
        ctrl.onPositionChanged = (newPos) =>
        {
            // Move own figure visually
            myFigure.position = new Vector3(newPos.x, myFigure.position.y, newPos.z);

            // Sync to other player
            if (NetworkServer.active)
                RpcSyncEnemyPosition(myPiece.position.x, myPiece.position.y, newPos.x, newPos.z);
            else
                CmdSyncEnemyPosition(myPiece.position.x, myPiece.position.y, newPos.x, newPos.z);
        };

        ctrl.onRotationChanged = (rotY) =>
        {
            if (NetworkServer.active)
                RpcSyncFigureRotation(myPiece.position.x, myPiece.position.y, rotY);
            else
                CmdSyncFigureRotation(myPiece.position.x, myPiece.position.y, rotY);
        };

        FPSWeapon weapon = body.AddComponent<FPSWeapon>();
        weapon.fpsCamera   = cam;
        weapon.damage      = stats.damage;
        weapon.fireRate    = stats.fireRate;
        weapon.bulletRange = stats.bulletRange;
        weapon.weaponType  = stats.weaponType;

        ThemeWeaponRegistry reg = ThemeWeaponRegistry.Instance;
        if (reg != null && reg.weaponPrefabFPS != null)
            weapon.AttachWeaponToCamera(reg.weaponPrefabFPS);

        weapon.SetBattleActive(true);

        if (NetworkServer.active)
            RpcShowEnemyWeapon(myPiece.position.x, myPiece.position.y);
        else
            CmdShowEnemyWeapon(myPiece.position.x, myPiece.position.y);

        return body;
    }

    // Sync only the ENEMY figure position on the other client
    [Command(requiresAuthority = false)]
    private void CmdSyncEnemyPosition(int row, int col, float x, float z)
    {
        RpcSyncEnemyPosition(row, col, x, z);
    }

    [ClientRpc]
    private void RpcSyncEnemyPosition(int row, int col, float x, float z)
    {
        BoardManager board = FindObjectOfType<BoardManager>();
        if (board == null) return;
        Piece figure = board.boardPieces[row, col];
        if (figure == null) return;

        // Only update the figure on the OTHER player's screen
        ChessNetworkManager localMgr = ChessNetworkManager.LocalInstance;
        if (localMgr != null && figure.isWhite == localMgr.isWhitePlayer) return;

        figure.transform.position = new Vector3(x, figure.transform.position.y, z);
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
    private void CmdShowEnemyWeapon(int row, int col) { RpcShowEnemyWeapon(row, col); }

    [ClientRpc]
    private void RpcShowEnemyWeapon(int row, int col)
    {
        ChessNetworkManager localMgr = ChessNetworkManager.LocalInstance;
        if (localMgr == null) return;
        BoardManager board = FindObjectOfType<BoardManager>();
        if (board == null) return;
        Piece figure = board.boardPieces[row, col];
        if (figure == null) return;
        if (figure.isWhite == localMgr.isWhitePlayer) return;

        Transform holder = figure.transform.Find("FigureWeaponHolder");
        ThemeWeaponRegistry reg = ThemeWeaponRegistry.Instance;
        if (holder == null || reg == null || reg.weaponPrefabFPS == null) return;

        foreach (Transform child in holder) Destroy(child.gameObject);

        GameObject enemyWeapon = Instantiate(reg.weaponPrefabFPS, holder);
        enemyWeapon.transform.localPosition = reg.enemyWeaponPosition;
        enemyWeapon.transform.localRotation = Quaternion.Euler(reg.enemyWeaponRotation);
        enemyWeapon.transform.localScale    = Vector3.one * reg.enemyWeaponScale;
        enemyWeapon.SetActive(true);
        _spawnedFigureWeapon = enemyWeapon;
    }

    private float GetFigureHeadHeight(Transform figure)
    {
        Renderer[] renderers = figure.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return 0.3f;
        Bounds bounds = renderers[0].bounds;
        foreach (var r in renderers) bounds.Encapsulate(r.bounds);
        return bounds.size.y * 0.85f;
    }

    [Command(requiresAuthority = false)]
    public void CmdApplyDamage(int row, int col, float amount)
    {
        BoardManager board = FindObjectOfType<BoardManager>();
        if (board == null) return;
        Piece figure = board.boardPieces[row, col];
        if (figure == null) return;
        FPSHealth health = figure.GetComponent<FPSHealth>();
        if (health != null) health.ApplyDamage(amount);
    }

    [Server]
    public void OnFigureDied(Piece deadPiece)
    {
        if (!_battleActive) return;
        bool attackerDied = (deadPiece == _attacker);
        _battleActive = false;
        Debug.Log($"[BattleChess] {deadPiece.type} died. AttackerDied={attackerDied}");
        RpcEndBattle(_attacker.position.x, _attacker.position.y, _defender.position.x, _defender.position.y, attackerDied);
    }

    [ClientRpc]
    private void RpcEndBattle(int ax, int ay, int dx, int dy, bool attackerDied)
    {
        BoardManager board = FindObjectOfType<BoardManager>();
        if (board == null) return;

        Piece attacker = board.boardPieces[ax, ay];
        Piece defender = board.boardPieces[dx, dy];

        // 1. FPS body und Waffe zerstören
        if (_localFPSBody != null)
        {
            FPSWeapon w = _localFPSBody.GetComponent<FPSWeapon>();
            if (w != null) w.DestroyWeaponModel();
            Destroy(_localFPSBody);
            _localFPSBody = null;
        }
        if (_spawnedFigureWeapon != null) { Destroy(_spawnedFigureWeapon); _spawnedFigureWeapon = null; }

        // 2. Health und Stats entfernen
        CleanupHealth(attacker);
        CleanupHealth(defender);
        CleanupStats(attacker);
        CleanupStats(defender);

        // 3. Kamera wiederherstellen
        ChessCameraController camCtrl = FindObjectOfType<ChessCameraController>();
        if (camCtrl != null)
        {
            camCtrl.RestoreFromFPS();
            Camera mainCam = camCtrl.GetMainCamera();
            if (mainCam != null) mainCam.nearClipPlane = 3f;
        }

        // 4. Andere Figuren wiederherstellen ZUERST
        RestoreHiddenPieces();

        // 5. Alle Figuren auf korrekte Positionen snappen
        for (int r = 0; r < 8; r++)
            for (int c = 0; c < 8; c++)
            {
                Piece p = board.boardPieces[r, c];
                if (p == null) continue;
                Vector3 pos = board.squares[r * 8 + c].position;
                pos.y = p.transform.position.y;
                p.transform.position = pos;
            }

        // 6. Schachbrett-Ergebnis anwenden
        if (!attackerDied)
        {
            Vector2Int newPos = new Vector2Int(dx, dy);
            if (defender != null) { board.boardPieces[dx, dy] = null; board.SendToSide(defender); }
            if (attacker != null)
            {
                board.boardPieces[ax, ay] = null;
                board.boardPieces[dx, dy] = attacker;
                attacker.position = newPos;
                attacker.hasMoved = true;
                Vector3 sq = board.squares[dx * 8 + dy].position;
                sq.y = _attackerOriginalY;
                attacker.transform.position = sq;
            }
            Debug.Log("[BattleChess] Attacker won");
        }
        else
        {
            if (attacker != null) { board.boardPieces[ax, ay] = null; board.SendToSide(attacker); }
            if (defender != null)
            {
                Vector3 sq = board.squares[dx * 8 + dy].position;
                sq.y = _defenderOriginalY;
                defender.transform.position = sq;
            }
            Debug.Log("[BattleChess] Defender won");
        }

        board.enPassantTarget = new Vector2Int(-1, -1);

        if (NetworkServer.active)
            board.isWhiteTurn = !board.isWhiteTurn;

        // 7. Checkmate prüfen (nur Server)
        if (NetworkServer.active)
        {
            try
            {
                GameRules gameRules = FindObjectOfType<GameRules>();
                if (gameRules != null)
                {
                    if (gameRules.boardManager == null) gameRules.boardManager = board;
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
                if (net != null) net.SendGameEnd(board.gameState);
                else RpcForceGameEnd((int)board.gameState);
            }
        }

        // 8. Input wiederherstellen
        if (board.gameState == GameState.Playing)
        {
            PlayerInput input = FindObjectOfType<PlayerInput>();
            if (input != null) input.enabled = true;
        }

        // 9. Cursor zurücksetzen
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
            if (!p.gameObject.activeSelf) continue;
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