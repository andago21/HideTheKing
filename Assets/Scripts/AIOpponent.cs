using UnityEngine;

public class AIOpponent : MonoBehaviour
{
    [Header("Dependencies")]
    public BoardManager boardManager;
    public PlayerInput playerInput;           // Must have ExecuteNetworkMove(from, to)

    [Header("AI Settings")]
    public bool aiEnabled = false;
    public bool aiPlaysBlack = true;
    public float aiMoveDelay = 0.5f;
    private bool isThinking = false;

    private void Update()
    {
        if (!aiEnabled) return;
        if (isThinking) return;
        if (boardManager == null || boardManager.gameState != GameState.Playing) return;

        bool isAITurn = (aiPlaysBlack && !boardManager.isWhiteTurn) ||
                        (!aiPlaysBlack && boardManager.isWhiteTurn);

        if (isAITurn)
        {
            isThinking = true;
            Invoke(nameof(RequestAIMove), aiMoveDelay);
        }
    }

    private void RequestAIMove()
    {
        string currentFEN = FENConverter.Instance.BoardToFEN();
        Debug.Log($"Thinking... FEN = {currentFEN}");

        StockfishManager.Instance.GetBestMove(currentFEN, OnAIMoveReceived);
    }

    private void OnAIMoveReceived(string uciMove)
    {
        isThinking = false;  // reset thinking

        if (string.IsNullOrWhiteSpace(uciMove))
        {
            Debug.LogError("Stockfish returned empty/invalid move.");
            return;
        }

        Debug.Log($"AI Chosen move (UCI): {uciMove}");

        var (from, to) = FENConverter.Instance.UCIToPosition(uciMove);

        if (from.x < 0 || from.y < 0 || to.x < 0 || to.y < 0)
        {
            Debug.LogError($"[AI] Invalid position from UCI '{uciMove}' → from={from}, to={to}");
            return;
        }

        Debug.Log($"[AI] Converted: {from} → {to}");

        Piece piece = boardManager.boardPieces[from.x, from.y];
        if (piece == null)
        {
            Debug.LogError($"[AI] No piece found at from position {from} (UCI: {uciMove})");
            return;
        }

        // Optional: verify it's actually AI's piece
        bool isAIPiece = (aiPlaysBlack && !piece.isWhite) || (!aiPlaysBlack && piece.isWhite);
        if (!isAIPiece)
        {
            Debug.LogWarning($"[AI] Piece at {from} is not AI's color! (expected {(aiPlaysBlack ? "Black" : "White")})");
            // still try to move – maybe engine is confused, but don't block
        }

        // Tell the input system to perform the move (should handle validation, animation, capture, etc.)
        playerInput.ExecuteNetworkMove(from, to);

        // Note: Do **not** flip isWhiteTurn manually here.
        // The move execution (in PlayerInput / your move logic) should already flip the turn.
        // If it doesn't, you have a bug in ExecuteNetworkMove / GameRules.

        Debug.Log("[AI] Move sent for execution.");
    }

    public void SetAIEnabled(bool enabled)
    {
        aiEnabled = enabled;
        isThinking = false;
        CancelInvoke(nameof(RequestAIMove)); // prevent pending moves
        Debug.Log($"AI opponent {(enabled ? "enabled" : "disabled")}");
    }

    public void SetAIColor(bool playsBlack)
    {
        aiPlaysBlack = playsBlack;
        Debug.Log($"AI now playing as {(playsBlack ? "Black" : "White")}");
    }

    // Optional: call this when game resets / new game starts
    public void ResetAI()
    {
        isThinking = false;
        CancelInvoke(nameof(RequestAIMove));
    }
}