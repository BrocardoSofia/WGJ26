using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class LevelUiPreviewCreator
{
    private const string LevelScenePath = "Assets/Scenes/2.Nivel.unity";
    private const string PreviewCreatedSessionKey = "WGJ26.LevelUiPreviewCreated";

    [InitializeOnLoadMethod]
    private static void CreatePreviewAfterCompilation()
    {
        EditorApplication.delayCall += () =>
        {
            if (SessionState.GetBool(PreviewCreatedSessionKey, false) ||
                EditorApplication.isPlayingOrWillChangePlaymode ||
                EditorSceneManager.GetActiveScene().path != LevelScenePath)
                return;

            CreatePreview();
            SessionState.SetBool(PreviewCreatedSessionKey, true);
        };
    }

    [MenuItem("Tools/WGJ26/Mostrar UI editable en 2.Nivel")]
    public static void CreatePreview()
    {
        if (EditorSceneManager.GetActiveScene().path != LevelScenePath)
            EditorSceneManager.OpenScene(LevelScenePath, OpenSceneMode.Single);

        PauseMenuController pauseController = Object.FindAnyObjectByType<PauseMenuController>();
        if (pauseController == null)
        {
            Debug.LogError("No se encontró PauseMenuController en 2.Nivel.");
            return;
        }

        MenuSettingsPanel settingsPanel = pauseController.GetComponent<MenuSettingsPanel>();
        if (settingsPanel == null)
            settingsPanel = Undo.AddComponent<MenuSettingsPanel>(pauseController.gameObject);

        settingsPanel.BuildEditorPreview();
        Transform settingsObject = pauseController.transform.Find("SettingsPanel");
        if (settingsObject != null)
            settingsObject.gameObject.SetActive(false);

        pauseController.BuildEditorPreview();
        EditorUtility.SetDirty(pauseController);
        EditorUtility.SetDirty(settingsPanel);
        EditorSceneManager.MarkSceneDirty(pauseController.gameObject.scene);
        EditorSceneManager.SaveScene(pauseController.gameObject.scene);
        Selection.activeGameObject = pauseController.transform.Find("PauseMenuPanel")?.gameObject;
    }
}
