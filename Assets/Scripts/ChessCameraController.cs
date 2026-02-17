using UnityEngine;

public class ChessCameraController : MonoBehaviour
{
    public Camera mainCamera;
    public Transform boardCenter; // Reference to your board's center
    
    private bool cameraAdjusted = false;

    private void Update()
    {
        if (cameraAdjusted) return;

        // Wait until we have a local player
        if (ChessNetworkManager.LocalInstance != null)
        {
            AdjustCamera();
            cameraAdjusted = true;
        }
    }

    private void AdjustCamera()
    {
        if (ChessNetworkManager.LocalInstance.isWhitePlayer)
        {
            Debug.Log("White player - camera stays in default position");
            // Camera stays as is for white
        }
        else
        {
            Debug.Log("Black player - rotating camera 180°");
            
            // Get the board center point (or use Vector3.zero if your board is at origin)
            Vector3 pivotPoint = boardCenter != null ? boardCenter.position : Vector3.zero;
            
            // Rotate camera 180 degrees around the board
            mainCamera.transform.RotateAround(pivotPoint, Vector3.up, 180f);
        }
    }
}