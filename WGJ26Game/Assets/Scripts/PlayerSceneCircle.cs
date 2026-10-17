using UnityEngine;
using UnityEngine.Serialization;

#if UNITY_EDITOR
using UnityEditor;
using UnityEngine.Rendering;
#endif

public sealed class PlayerSceneCircle : MonoBehaviour
{
    [FormerlySerializedAs("diameter")]
    [SerializeField, Min(0f), InspectorName("Diámetro llamada a NPC")]
    private float npcCallDiameter = 5f;

    [SerializeField, Min(0f), InspectorName("Diámetro interacción con NPC")]
    private float npcInteractionDiameter = 2.5f;

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        Color previousColor = Handles.color;
        CompareFunction previousZTest = Handles.zTest;

        Handles.zTest = CompareFunction.Always;

        Handles.color = Color.cyan;
        Handles.DrawWireDisc(transform.position, Vector3.up, npcCallDiameter * 0.5f);

        Handles.color = Color.yellow;
        Handles.DrawWireDisc(transform.position, Vector3.up, npcInteractionDiameter * 0.5f);

        Handles.color = previousColor;
        Handles.zTest = previousZTest;
    }
#endif

    private void OnValidate()
    {
        npcCallDiameter = Mathf.Max(0f, npcCallDiameter);
        npcInteractionDiameter = Mathf.Max(0f, npcInteractionDiameter);
    }
}
