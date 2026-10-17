#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class PlayerSceneCircleSetup
{
    private const string ScenePath = "Assets/Scenes/2.Nivel.unity";

    [InitializeOnLoadMethod]
    private static void AttachAfterScriptReload()
    {
        EditorApplication.delayCall += AttachToPlayer;
    }

    [MenuItem("WGJ26/Agregar círculo visual a Player")]
    private static void AttachFromMenu()
    {
        AttachToPlayer();
    }

    private static void AttachToPlayer()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        Scene activeScene = SceneManager.GetActiveScene();
        if (activeScene.path != ScenePath)
            return;

        GameObject player = GameObject.Find("Player");
        if (player == null || player.GetComponent<PlayerSceneCircle>() != null)
            return;

        Undo.AddComponent<PlayerSceneCircle>(player);
        EditorUtility.SetDirty(player);
        Debug.Log("Círculo visual agregado a Player. Ajusta Diameter desde el Inspector.");
    }
}
#endif
