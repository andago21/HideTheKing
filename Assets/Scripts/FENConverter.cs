using UnityEngine;

public class FENConverter : MonoBehaviour
{
    public static FENConverter Instance;

    // Cache references (set in Awake or via inspector)
    private BoardManager boardManager;
    private GameRules gameRules;
    private MoveNotation moveNotation;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // optional
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        // Try to find dependencies automatically
        boardManager = FindObjectOfType<BoardManager>();
        gameRules    = FindObjectOfType<GameRules>();
        moveNotation = FindObjectOfType<MoveNotation>();

        if (boardManager == null)   Debug.LogError("FENConverter could not find BoardManager!");
        if (gameRules == null)      Debug.LogError("FENConverter could not find GameRules!");
        if (moveNotation == null)   Debug.LogWarning("FENConverter could not find MoveNotation – fullmove number will be approximate.");
    }

    /// <summary>
    /// Converts the current board state to standard FEN notation
    /// </summary>
    public string BoardToFEN()
    {
        if (boardManager == null)
        {
            Debug.LogError("BoardManager reference missing – cannot generate FEN.");
            return "8/8/8/8/8/8/8/8 w - - 0 1"; // fallback / error representation
        }

        string fen = "";

        // 1. Piece placement (rank 8 → rank 1)
        for (int row = 7; row >= 0; row--)
        {
            int emptyCount = 0;

            for (int col = 0; col < 8; col++)
            {
                Piece piece = boardManager.boardPieces[row, col];

                if (piece == null)
                {
                    emptyCount++;
                }
                else
                {
                    if (emptyCount > 0)
                    {
                        fen += emptyCount;
                        emptyCount = 0;
                    }
                    fen += GetFENPieceChar(piece);
                }
            }

            if (emptyCount > 0)
                fen += emptyCount;

            if (row > 0)
                fen += "/";
        }

        // 2. Active color
        fen += " " + (boardManager.isWhiteTurn ? "w" : "b");

        // 3. Castling availability
        string castling = GetCastlingRights();
        fen += " " + (string.IsNullOrEmpty(castling) ? "-" : castling);

        // 4. En passant target square
        fen += " " + GetEnPassantSquare();

        // 5. Halfmove clock (plies since last pawn move or capture)
        int halfMoveClock = gameRules != null ? gameRules.halfMoveClock : 0;
        fen += " " + halfMoveClock;

        // 6. Fullmove number
        int fullMoveNumber = 1;

        if (moveNotation != null && moveNotation.moveHistory != null)
        {
            // Rough approximation: every two moves (white+black) = one fullmove
            fullMoveNumber = (moveNotation.moveHistory.Count / 2) + 1;
        }
        // TODO: for better accuracy, maintain a fullMoveCounter in GameRules and increment it only after black's move

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

        // White
        Piece wKing = boardManager.boardPieces[0, 4];
        if (wKing != null && !wKing.hasMoved)
        {
            if (boardManager.boardPieces[0, 7] is { } wKRook && !wKRook.hasMoved)
                rights += "K";
            if (boardManager.boardPieces[0, 0] is { } wQRook && !wQRook.hasMoved)
                rights += "Q";
        }

        // Black
        Piece bKing = boardManager.boardPieces[7, 4];
        if (bKing != null && !bKing.hasMoved)
        {
            if (boardManager.boardPieces[7, 7] is { } bKRook && !bKRook.hasMoved)
                rights += "k";
            if (boardManager.boardPieces[7, 0] is { } bQRook && !bQRook.hasMoved)
                rights += "q";
        }

        return rights;
    }

    private string GetEnPassantSquare()
    {
        if (boardManager == null ||
            boardManager.enPassantTarget.x < 0 ||
            boardManager.enPassantTarget.y < 0)
        {
            return "-";
        }

        char file = (char)('a' + boardManager.enPassantTarget.y);
        int rank = boardManager.enPassantTarget.x + 1;
        return $"{file}{rank}";
    }

    /// <summary>
    /// Converts UCI move string (e.g. "e2e4", "b7b8q") → from/to positions
    /// </summary>
    public (Vector2Int from, Vector2Int to) UCIToPosition(string uci)
    {
        if (string.IsNullOrEmpty(uci) || uci.Length < 4)
        {
            Debug.LogError($"Invalid UCI move: '{uci}'");
            return (new Vector2Int(-1,-1), new Vector2Int(-1,-1));
        }

        int fromFile = uci[0] - 'a';
        int fromRank = uci[1] - '1';   // '1' → 0
        int toFile   = uci[2] - 'a';
        int toRank   = uci[3] - '1';

        return (
            new Vector2Int(fromRank, fromFile),
            new Vector2Int(toRank,   toFile)
        );
    }
}