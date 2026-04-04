using UnityEngine;

public class AIOpponent : MonoBehaviour
{
    [Header("Dependencies")]
    public BoardManager boardManager;
    public PlayerInput playerInput;

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

        if (IsAITurn())
        {
            isThinking = true;
            Invoke(nameof(RequestAIMove), aiMoveDelay);
        }
    }

    // Returns true when it is the AI's turn to move
    public bool IsAITurn()
    {
        if (boardManager == null) return false;
        return (aiPlaysBlack && !boardManager.isWhiteTurn) ||
               (!aiPlaysBlack && boardManager.isWhiteTurn);
    }

    private void RequestAIMove()
    {
        string currentFEN = FENConverter.Instance.BoardToFEN();
        Debug.Log($"Thinking... FEN = {currentFEN}");
        StockfishManager.Instance.GetBestMove(currentFEN, OnAIMoveReceived);
    }

    private void OnAIMoveReceived(string uciMove)
    {
        isThinking = false;

        if (string.IsNullOrWhiteSpace(uciMove))
        {
            Debug.LogError("Stockfish returned empty/invalid move.");
            return;
        }

        Debug.Log($"AI Chosen move (UCI): {uciMove}");

        var (from, to) = FENConverter.Instance.UCIToPosition(uciMove);

        if (from.x < 0 || from.y < 0 || to.x < 0 || to.y < 0)
        {
            Debug.LogError($"[AI] Invalid position from UCI '{uciMove}' from={from}, to={to}");
            return;
        }

        Debug.Log($"[AI] Converted: {from} to {to}");

        Piece piece = boardManager.boardPieces[from.x, from.y];
        if (piece == null)
        {
            Debug.LogError($"[AI] No piece found at {from} (UCI: {uciMove})");
            return;
        }

        playerInput.ExecuteNetworkMove(from, to);
        Debug.Log("[AI] Move sent for execution.");
    }

    public void SetAIEnabled(bool enabled)
    {
        aiEnabled = enabled;
        isThinking = false;
        CancelInvoke(nameof(RequestAIMove));
        Debug.Log($"AI opponent {(enabled ? "enabled" : "disabled")}");
    }

    public void SetAIColor(bool playsBlack)
    {
        aiPlaysBlack = playsBlack;
        Debug.Log($"AI now playing as {(playsBlack ? "Black" : "White")}");
    }

    public void ResetAI()
    {
        isThinking = false;
        CancelInvoke(nameof(RequestAIMove));
    }
}