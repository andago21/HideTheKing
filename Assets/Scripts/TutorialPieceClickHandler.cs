using UnityEngine;

public class TutorialPieceClickHandler : MonoBehaviour
{
    public TutorialManager manager;
    public int pieceIndex = -1;

    void Update()
    {
        if (!Input.GetMouseButtonDown(0)) return;
        if (Camera.main == null) return;

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (!Physics.Raycast(ray, out RaycastHit hit, 100f)) return;

        // Check if we hit this piece or its children
        bool hitPiece = false;
        for (Transform t = hit.transform; t != null; t = t.parent)
        {
            if (t == this.transform) 
            { 
                hitPiece = true;
                break; 
            }
        }

        if (hitPiece && manager != null)
        {
            // Block TutorialSquareRouter from also processing this same click
            TutorialSquareRouter.ConsumeClick();

            var piece = GetComponent<Piece>();
            if (piece != null)
            {
                int currentIndex = piece.position.x * 8 + piece.position.y;
                manager.OnSquareClicked(currentIndex);
            }
            else
            {
                manager.OnSquareClicked(pieceIndex);
            }
        }
    }
}
