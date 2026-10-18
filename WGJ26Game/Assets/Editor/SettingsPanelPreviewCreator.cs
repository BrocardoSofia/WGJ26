using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class SettingsPanelPreviewCreator
{
    private const string MenuScenePath = "Assets/Scenes/0.Menu.unity";
    [MenuItem("Tools/WGJ26/Mostrar configuraciones editables en 0.Menu")]
    public static void CreatePreview()
    {
        if (EditorSceneManager.GetActiveScene().path != MenuScenePath)
            EditorSceneManager.OpenScene(MenuScenePath, OpenSceneMode.Single);
        MainMenuController menuController = Object.FindAnyObjectByType<MainMenuController>();
        if (menuController == null)
        {
            Debug.LogError("Abrí la escena 0.Menu antes de crear la ventana de configuraciones.");
            return;
        }

        MenuSettingsPanel settingsPanel = menuController.GetComponent<MenuSettingsPanel>();
        if (settingsPanel == null)
            settingsPanel = Undo.AddComponent<MenuSettingsPanel>(menuController.gameObject);

        settingsPanel.BuildEditorPreview();
        EditorUtility.SetDirty(settingsPanel);
        EditorSceneManager.MarkSceneDirty(settingsPanel.gameObject.scene);
        EditorSceneManager.SaveScene(settingsPanel.gameObject.scene);
        Selection.activeGameObject = settingsPanel.transform.Find("SettingsPanel")?.gameObject;
    }
}
