using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class PlayerInput : MonoBehaviour
{
    public BoardManager boardManager;
    public GameObject highlightPrefab;
    public GameObject moveEffectPrefab; 
    public GameObject captureEffectPrefab; 

    private Piece selectedPiece;
    private List<GameObject> highlights = new List<GameObject>();

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                Piece hitPiece = hit.transform.GetComponent<Piece>();
                if (hitPiece != null && selectedPiece == null && hitPiece.isWhite == boardManager.isWhiteTurn)
                {
                    selectedPiece = hitPiece;
                    ShowPossibleMoves();
                    return;
                }

                if (selectedPiece != null)
                {
                    // Find target square (assuming hit is on square or piece; adjust if needed)
                    int targetIndex = GetSquareIndexFromHit(hit);
                    if (targetIndex != -1)
                    {
                        int targetRow = targetIndex / 8;
                        int targetCol = targetIndex % 8;
                        Vector2Int target = new Vector2Int(targetRow, targetCol);
                        if (selectedPiece.GetLegalMoves(boardManager.boardPieces).Contains(target))
                        {
                            MovePiece(target);
                            boardManager.isWhiteTurn = !boardManager.isWhiteTurn; // Switch turns
                        }
                    }
                    ClearSelection();
                }
            }
        }
    }

    private int GetSquareIndexFromHit(RaycastHit hit)
    {
        Transform hitTransform = hit.transform;
        for (int i = 0; i < boardManager.squares.Length; i++)
        {
            if (boardManager.squares[i] == hitTransform || boardManager.squares[i] == hitTransform.parent)
            {
                return i;
            }
        }
        return -1; // Not a square
    }

    private void ShowPossibleMoves()
    {
        ClearHighlights();
        List<Vector2Int> moves = selectedPiece.GetLegalMoves(boardManager.boardPieces);
        foreach (var move in moves)
        {
            int index = move.x * 8 + move.y;
            Vector3 pos = boardManager.squares[index].position + new Vector3(-0.5f, -0.08f, +0.5f);
            GameObject highlight = Instantiate(highlightPrefab, pos, Quaternion.Euler(0, 0, 0));
            highlights.Add(highlight);
        }
    }

    private void MovePiece(Vector2Int target)
    {
        Vector3 targetPos = boardManager.squares[target.x * 8 + target.y].position;

        // Capture
        Piece targetPiece = boardManager.boardPieces[target.x, target.y];
        if (targetPiece != null)
        {
            Destroy(targetPiece.gameObject);
            if (captureEffectPrefab != null) Instantiate(captureEffectPrefab, targetPos, Quaternion.identity);
        }
        else
        {
            if (moveEffectPrefab != null) Instantiate(moveEffectPrefab, targetPos, Quaternion.identity);
        }

        // Update board
        boardManager.boardPieces[selectedPiece.position.x, selectedPiece.position.y] = null;
        boardManager.boardPieces[target.x, target.y] = selectedPiece;
        selectedPiece.position = target;

        // Animate
        StartCoroutine(MoveAnimation(selectedPiece.transform, targetPos));
    }

    private IEnumerator MoveAnimation(Transform pieceTrans, Vector3 targetPos)
    {
        float duration = 0.5f;
        Vector3 start = pieceTrans.position;
        float elapsed = 0;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            pieceTrans.position = Vector3.Lerp(start, targetPos, elapsed / duration);
            yield return null;
        }
        pieceTrans.position = targetPos;
    }

    private void ClearSelection()
    {
        selectedPiece = null;
        ClearHighlights();
    }

    private void ClearHighlights()
    {
        foreach (var h in highlights) Destroy(h);
        highlights.Clear();
    }
}