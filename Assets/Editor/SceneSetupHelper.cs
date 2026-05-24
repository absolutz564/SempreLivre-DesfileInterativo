#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class SceneSetupHelper
{
    [MenuItem("SempreLivre/Configurar Cenas no Build Settings")]
    static void SetupBuildScenes()
    {
        string[] scenePaths = new[]
        {
            "Assets/Scenes/Scene_01_Welcome.unity",
            "Assets/Scenes/Scene_02_FrameSelection.unity",
            "Assets/Scenes/Scene_03_Recording.unity",
            "Assets/Scenes/Scene_04_Final.unity"
        };

        var buildScenes = new EditorBuildSettingsScene[scenePaths.Length];
        for (int i = 0; i < scenePaths.Length; i++)
            buildScenes[i] = new EditorBuildSettingsScene(scenePaths[i], true);

        EditorBuildSettings.scenes = buildScenes;
        Debug.Log("✓ Build Settings configuradas com as 4 cenas.");
        EditorUtility.DisplayDialog("SempreLivre", "Build Settings configuradas!\nCrie as cenas na pasta Assets/Scenes/ com os nomes corretos.", "OK");
    }
}
#endif
