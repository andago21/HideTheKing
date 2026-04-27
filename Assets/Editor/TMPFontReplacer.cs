using UnityEngine;
using UnityEditor;
using TMPro;

public class TMPFontReplacer : EditorWindow
{
    public TMP_FontAsset oldFont;
    public TMP_FontAsset newFont;

    [MenuItem("Tools/TMP Replace Font")]
    public static void ShowWindow()
    {
        GetWindow<TMPFontReplacer>("TMP Replace Font");
    }

    private void OnGUI()
    {
        GUILayout.Label("Replace TMP Font Assets", EditorStyles.boldLabel);

        oldFont = (TMP_FontAsset)EditorGUILayout.ObjectField("Old Font", oldFont, typeof(TMP_FontAsset), false);
        newFont = (TMP_FontAsset)EditorGUILayout.ObjectField("New Font", newFont, typeof(TMP_FontAsset), false);

        if (GUILayout.Button("Replace In Open Scenes"))
        {
            ReplaceFontsInOpenScenes();
        }
    }

    private void ReplaceFontsInOpenScenes()
    {
        int count = 0;

        // UI TMP text
        var uiTexts = Resources.FindObjectsOfTypeAll<TextMeshProUGUI>();
        foreach (var txt in uiTexts)
        {
            if (EditorUtility.IsPersistent(txt)) continue; // skip prefabs/assets
            if (txt.font == oldFont || txt.font == null)
            {
                Undo.RecordObject(txt, "Replace TMP Font");
                txt.font = newFont;
                EditorUtility.SetDirty(txt);
                count++;
            }
        }

        // 3D TMP text
        var worldTexts = Resources.FindObjectsOfTypeAll<TextMeshPro>();
        foreach (var txt in worldTexts)
        {
            if (EditorUtility.IsPersistent(txt)) continue; // skip prefabs/assets
            if (txt.font == oldFont || txt.font == null)
            {
                Undo.RecordObject(txt, "Replace TMP Font");
                txt.font = newFont;
                EditorUtility.SetDirty(txt);
                count++;
            }
        }

        Debug.Log($"Replaced font on {count} TMP text objects.");
    }
}