#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class NivelSceneSetup
{
    private const string ScenePath = "Assets/Scenes/2.Nivel.unity";

    [InitializeOnLoadMethod]
    private static void ConfigureAfterScriptReload()
    {
        // Delay until Unity has finished importing the new scripts. The object
        // name check makes this a one-time setup and preserves later edits.
        EditorApplication.delayCall += () =>
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return;

            bool alreadyConfigured = File.Exists(ScenePath)
                && File.ReadAllText(ScenePath).Contains("m_Name: Personaje_Placeholder");
            if (!alreadyConfigured)
                ConfigureScene();
        };
    }

    [MenuItem("WGJ26/Configurar escena 2.Nivel")]
    public static void ConfigureFromMenu() => ConfigureScene();

    // Entry point used by Unity batch mode.
    public static void ConfigureScene()
    {
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        DeleteIfPresent("Piso_310x310");
        DeleteIfPresent("Personaje_Placeholder");

        GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
        floor.name = "Piso_310x310";
        floor.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        // A Unity Plane is 10 x 10 units, therefore scale 31 produces 310 x 310.
        floor.transform.localScale = new Vector3(31f, 1f, 31f);

        GameObject character = GameObject.CreatePrimitive(PrimitiveType.Quad);
        character.name = "Personaje_Placeholder";
        character.transform.position = new Vector3(0f, 1.55f, 0f);
        // 310 px at the common 100 pixels-per-unit import setting = 3.1 units.
        character.transform.localScale = new Vector3(3.1f, 3.1f, 1f);
        character.AddComponent<ConstantPlaneMover>();

        Camera camera = Camera.main;
        if (camera == null)
        {
            GameObject cameraObject = new("Main Camera");
            cameraObject.tag = "MainCamera";
            camera = cameraObject.AddComponent<Camera>();
            cameraObject.AddComponent<AudioListener>();
        }

        camera.orthographic = false;
        camera.fieldOfView = 50f;
        camera.nearClipPlane = 0.1f;
        camera.farClipPlane = 1000f;
        camera.transform.position = character.transform.position + new Vector3(0f, 12f, -12f);
        camera.transform.LookAt(character.transform.position);

        SmoothFollowCamera follow = camera.GetComponent<SmoothFollowCamera>();
        if (follow == null)
            follow = camera.gameObject.AddComponent<SmoothFollowCamera>();
        follow.Target = character.transform;

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.SaveAssets();
        Debug.Log("2.Nivel configured: 310x310 floor, moving placeholder and perspective follow camera.");
    }

    private static void DeleteIfPresent(string objectName)
    {
        GameObject existing = GameObject.Find(objectName);
        if (existing != null)
            Object.DestroyImmediate(existing);
    }
}
#endif
