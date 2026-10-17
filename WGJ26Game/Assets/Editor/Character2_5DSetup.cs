#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class Character2_5DSetup
{
    private const string ScenePath = "Assets/Scenes/2.Nivel.unity";
    private const string MarkerName = "CameraFacingBillboard_Configured";

    [InitializeOnLoadMethod]
    private static void ConfigureAfterScriptReload()
    {
        EditorApplication.delayCall += () =>
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return;
            bool ready = File.Exists(ScenePath) && File.ReadAllText(ScenePath).Contains($"m_Name: {MarkerName}");
            if (!ready) ConfigureScene();
        };
    }

    [MenuItem("WGJ26/Configurar personaje 2.5D")]
    public static void ConfigureScene()
    {
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        GameObject character = GameObject.Find("Personaje_Placeholder");
        if (character == null)
        {
            Debug.LogError("No se encontró Personaje_Placeholder en 2.Nivel.");
            return;
        }

        Component addedMovement = character.GetComponent("PlanarCharacterMovement");
        if (addedMovement != null)
            Object.DestroyImmediate(addedMovement);
        GameObjectUtility.RemoveMonoBehavioursWithMissingScript(character);

        if (character.GetComponent<CameraFacingBillboard>() == null)
            character.AddComponent<CameraFacingBillboard>();

        GameObject previousMarker = GameObject.Find("Character2_5D_Configured");
        if (previousMarker != null)
            Object.DestroyImmediate(previousMarker);

        GameObject oldMarker = GameObject.Find(MarkerName);
        if (oldMarker != null)
            Object.DestroyImmediate(oldMarker);
        GameObject marker = new(MarkerName);
        marker.transform.SetParent(character.transform, false);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, ScenePath);
        Debug.Log("Personaje configurado como billboard 2.5D. El movimiento existente no fue modificado.");
    }
}
#endif
