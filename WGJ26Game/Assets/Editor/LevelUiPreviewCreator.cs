using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class LevelUiPreviewCreator
{
    private const string LevelScenePath = "Assets/Scenes/2.Nivel.unity";
    private const string MenuScenePath = "Assets/Scenes/0.Menu.unity";
    private const string PreviewCreatedSessionKey = "WGJ26.LevelSettingsCopiedV2";

    [InitializeOnLoadMethod]
    private static void CreatePreviewAfterCompilation()
    {
        EditorApplication.delayCall += () =>
        {
            if (SessionState.GetBool(PreviewCreatedSessionKey, false) ||
                EditorApplication.isPlayingOrWillChangePlaymode ||
                EditorSceneManager.GetActiveScene().path != LevelScenePath)
                return;

            CopySettingsPanelFromMenu();
            SessionState.SetBool(PreviewCreatedSessionKey, true);
        };
    }

    [MenuItem("Tools/WGJ26/Copiar configuraciones de 0.Menu a 2.Nivel")]
    public static void CopySettingsPanelFromMenu()
    {
        if (EditorSceneManager.GetActiveScene().path != LevelScenePath)
            EditorSceneManager.OpenScene(LevelScenePath, OpenSceneMode.Single);

        Scene levelScene = EditorSceneManager.GetActiveScene();
        PauseMenuController pauseController = Object.FindAnyObjectByType<PauseMenuController>();
        if (pauseController == null)
        {
            Debug.LogError("No se encontró PauseMenuController en 2.Nivel.");
            return;
        }

        Scene menuScene = EditorSceneManager.OpenScene(MenuScenePath, OpenSceneMode.Additive);
        MainMenuController menuController = null;
        foreach (GameObject root in menuScene.GetRootGameObjects())
        {
            menuController = root.GetComponentInChildren<MainMenuController>(true);
            if (menuController != null)
                break;
        }

        Transform sourcePanel = menuController != null
            ? menuController.transform.Find("SettingsPanel")
            : null;
        if (sourcePanel == null)
        {
            EditorSceneManager.CloseScene(menuScene, true);
            Debug.LogError("No se encontró SettingsPanel dentro de 0.Menu.");
            return;
        }

        Transform previousPanel = pauseController.transform.Find("SettingsPanel");
        if (previousPanel != null)
            Undo.DestroyObjectImmediate(previousPanel.gameObject);

        GameObject copiedPanel = Object.Instantiate(sourcePanel.gameObject);
        copiedPanel.name = "SettingsPanel";
        SceneManager.MoveGameObjectToScene(copiedPanel, levelScene);
        copiedPanel.transform.SetParent(pauseController.transform, false);
        copiedPanel.transform.SetAsLastSibling();
        copiedPanel.SetActive(true);

        MenuSettingsPanel settingsPanel = pauseController.GetComponent<MenuSettingsPanel>();
        if (settingsPanel == null)
            settingsPanel = Undo.AddComponent<MenuSettingsPanel>(pauseController.gameObject);

        EditorSceneManager.CloseScene(menuScene, true);
        EditorUtility.SetDirty(pauseController);
        EditorUtility.SetDirty(settingsPanel);
        EditorSceneManager.MarkSceneDirty(levelScene);
        EditorSceneManager.SaveScene(levelScene);
        Selection.activeGameObject = copiedPanel;
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
