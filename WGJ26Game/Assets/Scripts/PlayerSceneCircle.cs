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

    [SerializeField, Min(0f), InspectorName("Diámetro llamada a vendedor de café")]
    private float coffeeNpcCallDiameter = 4f;

    [SerializeField, Min(0f), InspectorName("Diámetro interacción con NPC")]
    private float npcInteractionDiameter = 2.5f;

    [Header("Proyección visual")]
    [SerializeField, Min(0.01f), InspectorName("Ancho de la línea")]
    private float projectionLineWidth = 0.15f;

    [SerializeField, Range(0f, 1f), InspectorName("Transparencia")]
    private float projectionAlpha = 0.65f;

    [SerializeField, InspectorName("Color del Player")]
    private Color playerProjectionColor = Color.red;

    [SerializeField, InspectorName("Color de NPC interactuable")]
    private Color interactiveNpcProjectionColor = new(0f, 0.35f, 1f, 1f);

    [SerializeField, InspectorName("Color de vendedor de café")]
    private Color coffeeNpcProjectionColor = Color.green;

    public float NpcCallRadius => npcCallDiameter * 0.5f;
    public float CoffeeNpcCallRadius => coffeeNpcCallDiameter * 0.5f;
    public float NpcInteractionDiameter => npcInteractionDiameter;
    public float NpcInteractionRadius => npcInteractionDiameter * 0.5f;
    public float ProjectionLineWidth => projectionLineWidth;
    public float ProjectionAlpha => projectionAlpha;
    public Color PlayerProjectionColor => playerProjectionColor;
    public Color InteractiveNpcProjectionColor => interactiveNpcProjectionColor;
    public Color CoffeeNpcProjectionColor => coffeeNpcProjectionColor;

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        Color previousColor = Handles.color;
        CompareFunction previousZTest = Handles.zTest;

        Handles.zTest = CompareFunction.Always;

        Handles.color = Color.cyan;
        Handles.DrawWireDisc(transform.position, Vector3.up, npcCallDiameter * 0.5f);

        Handles.color = Color.green;
        Handles.DrawWireDisc(transform.position, Vector3.up, coffeeNpcCallDiameter * 0.5f);

        Handles.color = Color.yellow;
        Handles.DrawWireDisc(transform.position, Vector3.up, npcInteractionDiameter * 0.5f);

        Handles.color = previousColor;
        Handles.zTest = previousZTest;
    }
#endif

    private void OnValidate()
    {
        npcCallDiameter = Mathf.Max(0f, npcCallDiameter);
        coffeeNpcCallDiameter = Mathf.Max(0f, coffeeNpcCallDiameter);
        npcInteractionDiameter = Mathf.Max(0f, npcInteractionDiameter);
        projectionLineWidth = Mathf.Max(0.01f, projectionLineWidth);
        projectionAlpha = Mathf.Clamp01(projectionAlpha);
    }
}
