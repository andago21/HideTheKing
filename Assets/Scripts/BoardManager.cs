using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public enum GameState
{
    Playing,
    WhiteWins,
    BlackWins,
    Draw
}

public class BoardManager : MonoBehaviour
{
    public Transform[] squares;
    public GameState gameState = GameState.Playing;

    public GameObject whitePawn;
    public GameObject whiteRook;
    public GameObject whiteKnight;
    public GameObject whiteBishop;
    public GameObject whiteQueen;
    public GameObject whiteKing;

    public GameObject blackPawn;
    public GameObject blackRook;
    public GameObject blackKnight;
    public GameObject blackBishop;
    public GameObject blackQueen;
    public GameObject blackKing;

    public Piece[,] boardPieces = new Piece[8, 8];
    public bool isWhiteTurn = true;
    public Vector2Int enPassantTarget = new Vector2Int(-1, -1);

    public Transform[] whiteCapturedSlots;
    public Transform[] blackCapturedSlots;

    public int whiteCapturedCount = 0;
    public int blackCapturedCount = 0;

    void Start()
    {
        // Align squares to board Y
        for (int i = 0; i < squares.Length; i++)
        {
            Vector3 pos = squares[i].position;
            pos.y = transform.position.y;
            squares[i].position = pos;
        }

        // Singleplayer only — multiplayer waits for RpcStartGame()
        if (!NetworkClient.active && !NetworkServer.active)
        {
            Debug.Log("Singleplayer: setting up board immediately");
            SetupBoard();
        }
        else
        {
            Debug.Log("Multiplayer: waiting for RpcStartGame to setup board");
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F))
        {
            string fen = FENConverter.Instance.BoardToFEN();
            Debug.Log("Current FEN: " + fen);
        }
    }

    public void SetupBoard()
    {
        Debug.Log("SetupBoard called - NetworkServer.active: " + NetworkServer.active);

        for (int i = 0; i < 8; i++) SetupPiece(whitePawn,   true,  PieceType.Pawn,   8  + i);
        for (int i = 0; i < 8; i++) SetupPiece(blackPawn,   false, PieceType.Pawn,   48 + i);

        SetupPiece(whiteRook,   true,  PieceType.Rook,   0);
        SetupPiece(whiteRook,   true,  PieceType.Rook,   7);
        SetupPiece(blackRook,   false, PieceType.Rook,   56);
        SetupPiece(blackRook,   false, PieceType.Rook,   63);

        SetupPiece(whiteKnight, true,  PieceType.Knight, 1);
        SetupPiece(whiteKnight, true,  PieceType.Knight, 6);
        SetupPiece(blackKnight, false, PieceType.Knight, 57);
        SetupPiece(blackKnight, false, PieceType.Knight, 62);

        SetupPiece(whiteBishop, true,  PieceType.Bishop, 2);
        SetupPiece(whiteBishop, true,  PieceType.Bishop, 5);
        SetupPiece(blackBishop, false, PieceType.Bishop, 58);
        SetupPiece(blackBishop, false, PieceType.Bishop, 61);

        SetupPiece(whiteQueen,  true,  PieceType.Queen,  3);
        SetupPiece(blackQueen,  false, PieceType.Queen,  59);

        SetupPiece(whiteKing,   true,  PieceType.King,   4);
        SetupPiece(blackKing,   false, PieceType.King,   60);
    }

    private void SetupPiece(GameObject prefab, bool isWhitePiece, PieceType pieceType, int index)
    {
        int row = index / 8;
        int col = index % 8;
        Vector3 pos = squares[index].position;
        pos.y = prefab.transform.position.y;

        GameObject pieceObj = Instantiate(prefab, pos, prefab.transform.rotation);
        Piece piece = pieceObj.GetComponent<Piece>();
        if (piece != null)
        {
            piece.isWhite  = isWhitePiece;
            piece.type     = pieceType;
            piece.position = new Vector2Int(row, col);
            boardPieces[row, col] = piece;
        }
        else
        {
            Debug.LogError($"Piece component missing on {prefab.name}");
            return;
        }

        if (NetworkServer.active)
            NetworkServer.Spawn(pieceObj);
    }

    public void SendToSide(Piece capturedPiece)
    {
        if (capturedPiece == null) return;

        Transform[] slots = capturedPiece.isWhite ? whiteCapturedSlots : blackCapturedSlots;
        int idx = capturedPiece.isWhite ? whiteCapturedCount++ : blackCapturedCount++;
        Vector3 targetPos;
        Quaternion targetRot = capturedPiece.transform.rotation;

        if (slots != null && slots.Length > 0)
        {
            int clamped         = Mathf.Clamp(idx, 0, slots.Length - 1);
            float overflow      = Mathf.Max(0, idx - (slots.Length - 1));
            Vector3 stackOffset = new Vector3(0f, 0f, 0.18f * overflow);
            targetPos           = slots[clamped].position + stackOffset;
        }
        else targetPos = transform.position + new Vector3(10f, 0f, 0f);

        var colls = capturedPiece.GetComponentsInChildren<Collider>(true);
        foreach (var c in colls) c.enabled = false;
        var rb = capturedPiece.GetComponent<Rigidbody>();
        if (rb) rb.isKinematic = true;

        capturedPiece.enabled = false;

        if (slots != null && slots.Length > 0)
        {
            int clamped = Mathf.Clamp(idx, 0, slots.Length - 1);
            capturedPiece.transform.SetParent(slots[clamped], true);
        }

        targetPos.y = transform.position.y;
        capturedPiece.transform.SetPositionAndRotation(targetPos, targetRot);
    }

    public void HandleGameEnd(GameState result)
    {
        if (result == GameState.Playing) return;
        gameState = result;
    }
}