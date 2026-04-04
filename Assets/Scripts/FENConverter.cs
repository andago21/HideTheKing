using UnityEngine;

public class FENConverter : MonoBehaviour
{
    public static FENConverter Instance;

    private BoardManager boardManager;
    private GameRules gameRules;
    private MoveNotation moveNotation;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        RefreshReferences();
    }

    private void OnEnable()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene,
                               UnityEngine.SceneManagement.LoadSceneMode mode)
    {
        // Refresh all references after every scene load
        RefreshReferences();
    }

    private void RefreshReferences()
    {
        boardManager = FindObjectOfType<BoardManager>();
        gameRules    = FindObjectOfType<GameRules>();
        moveNotation = FindObjectOfType<MoveNotation>();
    }

    public string BoardToFEN()
    {
        // Re-find if lost (e.g. scene reload)
        if (boardManager == null) boardManager = FindObjectOfType<BoardManager>();
        if (gameRules == null)    gameRules    = FindObjectOfType<GameRules>();
        if (moveNotation == null) moveNotation = FindObjectOfType<MoveNotation>();

        if (boardManager == null)
        {
            Debug.LogError("BoardManager reference missing - cannot generate FEN.");
            return "8/8/8/8/8/8/8/8 w - - 0 1";
        }

        string fen = "";

        for (int row = 7; row >= 0; row--)
        {
            int emptyCount = 0;
            for (int col = 0; col < 8; col++)
            {
                Piece piece = boardManager.boardPieces[row, col];
                if (piece == null) { emptyCount++; }
                else
                {
                    if (emptyCount > 0) { fen += emptyCount; emptyCount = 0; }
                    fen += GetFENPieceChar(piece);
                }
            }
            if (emptyCount > 0) fen += emptyCount;
            if (row > 0) fen += "/";
        }

        fen += " " + (boardManager.isWhiteTurn ? "w" : "b");

        string castling = GetCastlingRights();
        fen += " " + (string.IsNullOrEmpty(castling) ? "-" : castling);
        fen += " " + GetEnPassantSquare();

        int halfMoveClock = gameRules != null ? gameRules.halfMoveClock : 0;
        fen += " " + halfMoveClock;

        int fullMoveNumber = 1;
        if (moveNotation != null && moveNotation.moveHistory != null)
            fullMoveNumber = (moveNotation.moveHistory.Count / 2) + 1;
        fen += " " + fullMoveNumber;

        return fen;
    }

    private char GetFENPieceChar(Piece piece)
    {
        char c = piece.type switch
        {
            PieceType.Pawn   => 'p',
            PieceType.Rook   => 'r',
            PieceType.Knight => 'n',
            PieceType.Bishop => 'b',
            PieceType.Queen  => 'q',
            PieceType.King   => 'k',
            _                => '?'
        };
        return piece.isWhite ? char.ToUpper(c) : c;
    }

    private string GetCastlingRights()
    {
        if (boardManager == null) return "";
        string rights = "";
        Piece wKing = boardManager.boardPieces[0, 4];
        if (wKing != null && !wKing.hasMoved)
        {
            if (boardManager.boardPieces[0, 7] is { } wKRook && !wKRook.hasMoved) rights += "K";
            if (boardManager.boardPieces[0, 0] is { } wQRook && !wQRook.hasMoved) rights += "Q";
        }
        Piece bKing = boardManager.boardPieces[7, 4];
        if (bKing != null && !bKing.hasMoved)
        {
            if (boardManager.boardPieces[7, 7] is { } bKRook && !bKRook.hasMoved) rights += "k";
            if (boardManager.boardPieces[7, 0] is { } bQRook && !bQRook.hasMoved) rights += "q";
        }
        return rights;
    }

    private string GetEnPassantSquare()
    {
        if (boardManager == null ||
            boardManager.enPassantTarget.x < 0 ||
            boardManager.enPassantTarget.y < 0)
            return "-";
        char file = (char)('a' + boardManager.enPassantTarget.y);
        int rank = boardManager.enPassantTarget.x + 1;
        return $"{file}{rank}";
    }

    public (Vector2Int from, Vector2Int to) UCIToPosition(string uci)
    {
        if (string.IsNullOrEmpty(uci) || uci.Length < 4)
        {
            Debug.LogError($"Invalid UCI move: '{uci}'");
            return (new Vector2Int(-1,-1), new Vector2Int(-1,-1));
        }
        int fromFile = uci[0] - 'a';
        int fromRank = uci[1] - '1';
        int toFile   = uci[2] - 'a';
        int toRank   = uci[3] - '1';
        return (new Vector2Int(fromRank, fromFile), new Vector2Int(toRank, toFile));
    }
}