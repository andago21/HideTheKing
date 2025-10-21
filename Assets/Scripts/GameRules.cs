using UnityEngine;
using System.Collections.Generic;

public class GameRules : MonoBehaviour
{
    public BoardManager boardManager;
    private MoveNotation moveNotation;
    private Dictionary<string, int> positionHistory = new Dictionary<string, int>();


    // The fifty-move rule states that a player can claim a draw if no pawn has been moved and no capture has been made in the last fifty moves (100 half-moves/plies).
    public int halfMoveClock = 0; // Counts half-moves since last pawn move or capture

    private ChessTimer chessTimer;

    private void Awake()
    {
        moveNotation = GetComponent<MoveNotation>();
        if (moveNotation == null)
        {
            Debug.LogError("GameRules requires MoveNotation on the same GameObject!");
        }

        chessTimer = FindObjectOfType<ChessTimer>();
        if (chessTimer == null)
        {
            Debug.LogWarning("ChessTimer not found - game will run without time limit.");
        }
    }

    // Check if the game has ended and update game state accordingly
    public void CheckGameEndConditions(bool currentPlayerIsWhite)
    {
        // Check for fifty-move rule (100 half-moves = 50 full moves)
        if (CheckFiftyMoveRule())
        {
            Debug.Log("DRAW! Fifty-move rule - no pawn move or capture in 50 moves.");
            boardManager.gameState = GameState.Draw;
            chessTimer.StopTimer();
            return;
        }

        // Check for insufficient material
        if (CheckInsufficientMaterial())
        {
            Debug.Log("DRAW! Insufficient material - neither player can checkmate.");
            boardManager.gameState = GameState.Draw;
            chessTimer.StopTimer();
            return;
        }

        // Check for threefold repetition
        if (CheckThreefoldRepetition())
        {
            Debug.Log("DRAW! Threefold repetition - the same position has occurred three times.");
            boardManager.gameState = GameState.Draw;
            chessTimer.StopTimer();
            return;
        }

        // Check for stalemate (must check before checkmate)
        if (CheckStalemate(!currentPlayerIsWhite))
        {
            Debug.Log("STALEMATE! The game is a draw.");
            boardManager.gameState = GameState.Draw;
            chessTimer.StopTimer();
            return;
        }

        // Check for check and checkmate
        if (CheckForCheck(!currentPlayerIsWhite))
        {
            if (CheckForCheckmate(!currentPlayerIsWhite))
            {
                Debug.Log("CHECKMATE! " + (currentPlayerIsWhite ? "White" : "Black") + " wins!");
                boardManager.gameState = currentPlayerIsWhite ? GameState.WhiteWins : GameState.BlackWins;
                chessTimer.StopTimer();
            }
            else
            {
                Debug.Log("Check! The opponent's king is under attack.");
            }
        }
    }

    // Check if a king is in check
    public bool CheckForCheck(bool isWhiteKing)
    {
        return Piece.IsKingInCheck(boardManager.boardPieces, isWhiteKing);
    }

    // Check if a king is in checkmate
    public bool CheckForCheckmate(bool isWhiteKing)
    {
        return Piece.IsCheckmate(boardManager.boardPieces, isWhiteKing);
    }

    // Check if the current player is in stalemate
    public bool CheckStalemate(bool isWhitePlayer)
    {
        return Piece.IsStalemate(boardManager.boardPieces, isWhitePlayer);
    }

    // Check fifty-move rule
    public bool CheckFiftyMoveRule()
    {
        return halfMoveClock >= 100;
    }

    // Check insufficient material
    public bool CheckInsufficientMaterial()
    {
        return Piece.IsInsufficientMaterial(boardManager.boardPieces);
    }

    // Check threefold repetition
    public bool CheckThreefoldRepetition()
    {
        string currentHash = moveNotation.GetBoardHash();

        if (positionHistory.ContainsKey(currentHash))
        {
            positionHistory[currentHash]++;
            if (positionHistory[currentHash] >= 3)
            {
                return true; // Threefold repetition detected
            }
        }
        else
        {
            positionHistory[currentHash] = 1;
        }

        return false;
    }
}