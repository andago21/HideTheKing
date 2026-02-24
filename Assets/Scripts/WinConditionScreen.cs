using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

public class WinConditionScreen : MonoBehaviour
{
    // Reloads the currently active scene
    public void ReloadScene()
    {
        Scene currentScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(currentScene.name);
    }

    // Exits the game
    public void QuitGame()
    {
        Debug.Log("Game is exiting...");

        Application.Quit();

        // Stops play mode in the Unity Editor
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #endif
    }
}