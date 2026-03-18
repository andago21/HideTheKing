using UnityEngine;

public class ChessCameraController : MonoBehaviour
{
    public Camera mainCamera;
    public Transform boardCenter;

    private bool cameraAdjusted = false;

    // Saved chess camera state — restored after FPS battle
    private Vector3 _savedPosition;
    private Quaternion _savedRotation;

    private void Update()
    {
        if (cameraAdjusted) return;

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
        }
        else
        {
            Debug.Log("Black player - rotating camera 180 degrees");
            Vector3 pivotPoint = boardCenter != null ? boardCenter.position : Vector3.zero;
            mainCamera.transform.RotateAround(pivotPoint, Vector3.up, 180f);
        }
    }

    // ── Called by BattleChessManager when FPS battle starts ──
    public void SaveAndDisableForFPS()
    {
        _savedPosition = mainCamera.transform.position;
        _savedRotation = mainCamera.transform.rotation;

        // Disable this script so it doesn't interfere during FPS
        enabled = false;
    }

    // ── Called by BattleChessManager when FPS battle ends ──
    public void RestoreFromFPS()
    {
        mainCamera.transform.position = _savedPosition;
        mainCamera.transform.rotation = _savedRotation;

        enabled = true;
    }

    public Camera GetMainCamera()
    {
        return mainCamera;
    }
}