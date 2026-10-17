using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using UnityEngine.Rendering;
#endif

[DisallowMultipleComponent]
public sealed class NpcInteractionCircle : MonoBehaviour
{
    private const int DiscSegments = 64;
    private const float GroundOffset = 0.04f;
    private const float VisibleAlpha = 0.45f;

    [SerializeField, Min(0f), InspectorName("Diámetro de interacción NPC")]
    private float diameter = 2f;

    [SerializeField, Min(0f), InspectorName("Superposición para iniciar interacción")]
    private float requiredOverlap = 0.25f;

    [SerializeField, InspectorName("Color del círculo")]
    private Color circleColor = new(0f, 0.35f, 1f, 1f);

    private GameObject runtimeDisc;
    private Mesh runtimeMesh;
    private Material runtimeMaterial;
    private bool callActive;

    public float Radius => diameter * 0.5f;
    public float RequiredOverlap => requiredOverlap;

    private void Awake()
    {
        CreateRuntimeDisc();
    }

    private void LateUpdate()
    {
        if (runtimeDisc == null)
            return;

        float groundY = transform.position.y - transform.localScale.y * 0.5f + GroundOffset;
        runtimeDisc.transform.SetPositionAndRotation(
            new Vector3(transform.position.x, groundY, transform.position.z),
            Quaternion.identity);
        runtimeDisc.transform.localScale = new Vector3(diameter, 1f, diameter);
        SetDiscAlpha(callActive ? VisibleAlpha : 0f);
    }

    private void OnDestroy()
    {
        if (runtimeDisc != null)
            Destroy(runtimeDisc);
        if (runtimeMaterial != null)
            Destroy(runtimeMaterial);
        if (runtimeMesh != null)
            Destroy(runtimeMesh);
    }

    public void SetCallActive(bool active)
    {
        callActive = active;
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        Color previousColor = Handles.color;
        CompareFunction previousZTest = Handles.zTest;
        Handles.color = circleColor;
        Handles.zTest = CompareFunction.Always;
        Handles.DrawWireDisc(transform.position, Vector3.up, Radius);
        Handles.color = previousColor;
        Handles.zTest = previousZTest;
    }
#endif

    private void CreateRuntimeDisc()
    {
        if (runtimeDisc != null)
            return;

        runtimeDisc = new GameObject("NpcInteractionCircle_Runtime");
        runtimeDisc.hideFlags = HideFlags.DontSave;

        MeshFilter meshFilter = runtimeDisc.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = runtimeDisc.AddComponent<MeshRenderer>();
        meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        meshRenderer.receiveShadows = false;

        runtimeMesh = CreateDiscMesh();
        meshFilter.sharedMesh = runtimeMesh;

        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null)
            shader = Shader.Find("Sprites/Default");
        runtimeMaterial = new Material(shader);
        ConfigureTransparentMaterial(runtimeMaterial);
        meshRenderer.sharedMaterial = runtimeMaterial;
        SetDiscAlpha(0f);
    }

    private void SetDiscAlpha(float alpha)
    {
        if (runtimeMaterial == null)
            return;

        Color color = new(circleColor.r, circleColor.g, circleColor.b,
            Mathf.Clamp01(alpha * circleColor.a));
        if (runtimeMaterial.HasProperty("_BaseColor"))
            runtimeMaterial.SetColor("_BaseColor", color);
        if (runtimeMaterial.HasProperty("_Color"))
            runtimeMaterial.SetColor("_Color", color);
    }

    private static Mesh CreateDiscMesh()
    {
        Vector3[] vertices = new Vector3[DiscSegments + 1];
        int[] triangles = new int[DiscSegments * 3];

        for (int index = 0; index < DiscSegments; index++)
        {
            float angle = index * Mathf.PI * 2f / DiscSegments;
            vertices[index + 1] = new Vector3(Mathf.Cos(angle) * 0.5f, 0f, Mathf.Sin(angle) * 0.5f);
            int triangleIndex = index * 3;
            triangles[triangleIndex] = 0;
            triangles[triangleIndex + 1] = index + 1;
            triangles[triangleIndex + 2] = (index + 1) % DiscSegments + 1;
        }

        Mesh mesh = new() { name = "NPC Interaction Disc" };
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    private static void ConfigureTransparentMaterial(Material material)
    {
        if (material.HasProperty("_Surface"))
            material.SetFloat("_Surface", 1f);
        if (material.HasProperty("_SrcBlend"))
            material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
        if (material.HasProperty("_DstBlend"))
            material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        if (material.HasProperty("_ZWrite"))
            material.SetFloat("_ZWrite", 0f);
        if (material.HasProperty("_Cull"))
            material.SetFloat("_Cull", (float)UnityEngine.Rendering.CullMode.Off);

        material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
    }

    private void OnValidate()
    {
        diameter = Mathf.Max(0f, diameter);
        requiredOverlap = Mathf.Clamp(requiredOverlap, 0f, diameter);
    }
}
