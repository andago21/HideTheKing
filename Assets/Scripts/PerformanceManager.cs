using UnityEngine;
using UnityEngine.SceneManagement;

public class PerformanceManager : MonoBehaviour
{
    [Header("Framerate")]
    public int targetFrameRate = 70;

    private void Awake()
    {
        Application.targetFrameRate = targetFrameRate;
        QualitySettings.vSyncCount = 0;

        // BattleChess needs fast physics for FPS movement
        // Classic/Offline chess can use slower physics to save CPU
        bool isBattleChess = SceneManager.GetActiveScene().name.Contains("BattleChess");
        Time.fixedDeltaTime = isBattleChess ? 0.02f : 0.04f;
    }
}