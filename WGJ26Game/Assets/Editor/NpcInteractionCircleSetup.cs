#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class NpcInteractionCircleSetup
{
    private const string ScenePath = "Assets/Scenes/2.Nivel.unity";

    [InitializeOnLoadMethod]
    private static void AddAfterScriptReload()
    {
        EditorApplication.delayCall += AddToInteractiveNpcs;
    }

    [MenuItem("WGJ26/Agregar círculos de interacción a NPCs")]
    public static void AddToInteractiveNpcs()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        Scene scene = SceneManager.GetActiveScene();
        if (scene.path != ScenePath)
            return;

        bool changed = false;
        foreach (GameObject npc in GameObject.FindGameObjectsWithTag("InteractiveNPC"))
        {
            if (npc.GetComponent<NpcInteractionCircle>() != null)
                continue;

            Undo.AddComponent<NpcInteractionCircle>(npc);
            changed = true;
        }

        if (!changed)
            return;

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("Círculos de interacción agregados a los NPCs interactuables.");
    }
}
#endif
