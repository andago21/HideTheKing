// Save as: Assets/Editor/BulkFontReplacer.cs
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;

public class BulkFontReplacer : EditorWindow
{
    Font newFont;

    [MenuItem("Tools/Bulk Replace Font")]
    static void Open() => GetWindow<BulkFontReplacer>("Bulk Replace Font");

    void OnGUI()
    {
        newFont = (Font)EditorGUILayout.ObjectField("Replacement Font", newFont, typeof(Font), false);

        if (GUILayout.Button("Replace in ALL Scenes") && newFont != null)
            ReplaceInAllScenes();
    }

    void ReplaceInAllScenes()
    {
        string[] guids = AssetDatabase.FindAssets("t:Scene");
        int replaced = 0;
        int skipped = 0;

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);

            // ✅ Skip scenes that are in read-only packages
            if (!path.StartsWith("Assets/"))
            {
                Debug.Log($"Skipping read-only scene: {path}");
                skipped++;
                continue;
            }

            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);

            foreach (var text in Resources.FindObjectsOfTypeAll<Text>())
            {
                if (text.font != newFont)
                {
                    text.font = newFont;
                    EditorUtility.SetDirty(text);
                    replaced++;
                }
            }

            EditorSceneManager.SaveScene(scene);
        }

        Debug.Log($"Done! Replaced {replaced} Text components. Skipped {skipped} read-only scenes.");
    }
}