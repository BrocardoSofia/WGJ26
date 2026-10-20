using System.Collections;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using UnityEngine.Rendering;
#endif

[DisallowMultipleComponent]
public sealed class NpcInteractionCircle : MonoBehaviour
{
    private const int DiscSegments = 64;
    private const int DiscSortingOffset = 10;
    private const float GroundOffset = 0.04f;

    [SerializeField, Min(0f), InspectorName("Diámetro de interacción NPC")]
    private float diameter = 2f;

    [SerializeField, Min(0f), InspectorName("Superposición para iniciar interacción")]
    private float requiredOverlap = 0.25f;

    [SerializeField, InspectorName("Color del círculo")]
    private Color circleColor = new(0f, 0.35f, 1f, 1f);

    [SerializeField, InspectorName("Imagen mientras llama al Player")]
    private GameObject callImage;

    [Header("Audio del NPC")]
    [SerializeField, InspectorName("Audio mientras llama")]
    private AudioClip callAudio;

    [SerializeField, Min(0f), InspectorName("Espera entre llamadas (segundos)")]
    private float callAudioDelay = 1f;

    [SerializeField, Range(0f, 2f), InspectorName("Volumen individual de llamada")]
    private float callAudioVolume = 1f;

    [SerializeField, InspectorName("Audio al interactuar")]
    private AudioClip interactionAudio;

    [SerializeField, Range(0f, 2f), InspectorName("Volumen individual de interacción")]
    private float interactionAudioVolume = 1f;

    private GameObject runtimeDisc;
    private Mesh runtimeMesh;
    private Material runtimeMaterial;
    private MeshRenderer runtimeDiscRenderer;
    private Renderer npcRenderer;
    private Camera mainCamera;
    private float lineWidth = 0.15f;
    private float visibleAlpha = 0.65f;
    private AudioSource npcAudioSource;
    private Coroutine callAudioCoroutine;
    private float currentAudioVolume;
    private readonly Plane[] cameraFrustumPlanes = new Plane[6];
    private bool callActive;
    private bool interactionAudioActive;

    public float Radius => diameter * 0.5f;
    public float RequiredOverlap => requiredOverlap;

    public void SetVisualSettings(float newLineWidth, float newAlpha, Color newColor)
    {
        newLineWidth = Mathf.Max(0.01f, newLineWidth);
        newAlpha = Mathf.Clamp01(newAlpha);
        bool meshNeedsRebuild = !Mathf.Approximately(lineWidth, newLineWidth);
        bool colorChanged = circleColor != newColor || !Mathf.Approximately(visibleAlpha, newAlpha);
        if (!meshNeedsRebuild && !colorChanged)
            return;

        lineWidth = newLineWidth;
        visibleAlpha = newAlpha;
        circleColor = newColor;

        if (meshNeedsRebuild && runtimeMesh != null)
            RebuildRingMesh(runtimeMesh, diameter, lineWidth);
        SetDiscAlpha(callActive ? visibleAlpha : 0f);
    }

    private void Awake()
    {
        if (callImage != null)
            callImage.SetActive(false);

        npcAudioSource = GetComponent<AudioSource>();
        if (npcAudioSource == null)
            npcAudioSource = gameObject.AddComponent<AudioSource>();
        npcAudioSource.playOnAwake = false;
        npcAudioSource.loop = false;
        npcAudioSource.spatialBlend = 0f;
    }

    private void OnEnable()
    {
        GameSettings.SettingsChanged += ApplyAudioVolume;
    }

    private void LateUpdate()
    {
        if (!callActive || !IsVisibleToMainCamera())
        {
            DestroyRuntimeDisc();
            return;
        }

        CreateRuntimeDisc();

        float groundY = transform.position.y - transform.localScale.y * 0.5f + GroundOffset;
        runtimeDisc.transform.SetPositionAndRotation(
            new Vector3(transform.position.x, groundY, transform.position.z),
            Quaternion.identity);
        runtimeDisc.transform.localScale = new Vector3(diameter, 1f, diameter);
        UpdateDiscSortingOrder();
        SetDiscAlpha(callActive ? visibleAlpha : 0f);
    }

    private void OnDestroy()
    {
        DestroyRuntimeDisc();
    }

    private void OnDisable()
    {
        GameSettings.SettingsChanged -= ApplyAudioVolume;
        if (callImage != null)
            callImage.SetActive(false);
        StopNpcAudio();
        DestroyRuntimeDisc();
    }

    public void SetCallActive(bool active)
    {
        if (callImage != null)
            callImage.SetActive(active);

        if (callActive == active && enabled == active)
            return;

        callActive = active;
        enabled = active;

        if (active)
            StartCallAudioCycle();
        else
            StopNpcAudio();
    }

    public void PlayInteractionAudio()
    {
        PlayPriorityAudio(interactionAudio, interactionAudioVolume);
    }

    public void PlayPriorityAudio(AudioClip clip, float individualVolume)
    {
        interactionAudioActive = true;
        StopCallAudioCycle();

        if (npcAudioSource == null || clip == null)
            return;

        currentAudioVolume = Mathf.Max(0f, individualVolume);
        ApplyAudioVolume();
        npcAudioSource.Stop();
        npcAudioSource.clip = clip;
        npcAudioSource.Play();
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
        runtimeDisc.hideFlags = HideFlags.HideAndDontSave;

        MeshFilter meshFilter = runtimeDisc.AddComponent<MeshFilter>();
        runtimeDiscRenderer = runtimeDisc.AddComponent<MeshRenderer>();
        runtimeDiscRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        runtimeDiscRenderer.receiveShadows = false;

        runtimeMesh = CreateRingMesh(diameter, lineWidth);
        runtimeMesh.hideFlags = HideFlags.HideAndDontSave;
        meshFilter.sharedMesh = runtimeMesh;

        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null)
            shader = Shader.Find("Sprites/Default");
        runtimeMaterial = new Material(shader);
        runtimeMaterial.hideFlags = HideFlags.HideAndDontSave;
        ConfigureTransparentMaterial(runtimeMaterial);
        runtimeDiscRenderer.sharedMaterial = runtimeMaterial;
        UpdateDiscSortingOrder();
        SetDiscAlpha(0f);
    }

    private void UpdateDiscSortingOrder()
    {
        if (runtimeDiscRenderer == null || npcRenderer == null)
            return;

        runtimeDiscRenderer.sortingLayerID = npcRenderer.sortingLayerID;
        runtimeDiscRenderer.sortingOrder = npcRenderer.sortingOrder - DiscSortingOffset;
    }

    private bool IsVisibleToMainCamera()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;
        if (mainCamera == null)
            return false;

        if (npcRenderer == null || !npcRenderer.enabled)
        {
            Transform npcVisual = transform.Find("NPCVisual");
            SpriteRenderer visibleSprite = npcVisual != null
                ? npcVisual.GetComponent<SpriteRenderer>()
                : GetComponentInChildren<SpriteRenderer>(true);
            npcRenderer = visibleSprite != null
                ? visibleSprite
                : GetComponent<Renderer>();
        }
        if (npcRenderer == null)
            return false;

        GeometryUtility.CalculateFrustumPlanes(mainCamera, cameraFrustumPlanes);
        return GeometryUtility.TestPlanesAABB(cameraFrustumPlanes, npcRenderer.bounds);
    }

    private void DestroyRuntimeDisc()
    {
        if (runtimeDisc != null)
            Destroy(runtimeDisc);
        if (runtimeMaterial != null)
            Destroy(runtimeMaterial);
        if (runtimeMesh != null)
            Destroy(runtimeMesh);

        runtimeDisc = null;
        runtimeMaterial = null;
        runtimeMesh = null;
        runtimeDiscRenderer = null;
    }

    private void StartCallAudioCycle()
    {
        interactionAudioActive = false;
        StopCallAudioCycle();

        if (callAudio == null || !isActiveAndEnabled)
            return;

        callAudioCoroutine = StartCoroutine(CallAudioCycle());
    }

    private IEnumerator CallAudioCycle()
    {
        while (callActive && !interactionAudioActive)
        {
            if (npcAudioSource != null)
            {
                currentAudioVolume = callAudioVolume;
                ApplyAudioVolume();
                npcAudioSource.Stop();
                npcAudioSource.clip = callAudio;
                npcAudioSource.Play();

                while (npcAudioSource.isPlaying && callActive && !interactionAudioActive)
                    yield return null;
            }

            if (!callActive || interactionAudioActive)
                break;

            yield return new WaitForSeconds(callAudioDelay);
        }

        callAudioCoroutine = null;
    }

    private void StopCallAudioCycle()
    {
        if (callAudioCoroutine == null)
            return;

        StopCoroutine(callAudioCoroutine);
        callAudioCoroutine = null;
    }

    private void StopNpcAudio()
    {
        StopCallAudioCycle();
        interactionAudioActive = false;

        if (npcAudioSource != null)
        {
            npcAudioSource.Stop();
            npcAudioSource.clip = null;
        }
    }

    private void ApplyAudioVolume()
    {
        if (npcAudioSource != null)
            npcAudioSource.volume = currentAudioVolume * AudioManager.SfxOutputVolume;
    }

    private void SetDiscAlpha(float alpha)
    {
        if (runtimeMaterial == null)
            return;

        Color color = new(circleColor.r, circleColor.g, circleColor.b,
            Mathf.Clamp01(alpha));
        if (runtimeMaterial.HasProperty("_BaseColor"))
            runtimeMaterial.SetColor("_BaseColor", color);
        if (runtimeMaterial.HasProperty("_Color"))
            runtimeMaterial.SetColor("_Color", color);
    }

    private static Mesh CreateRingMesh(float diameter, float width)
    {
        Mesh mesh = new() { name = "NPC Interaction Ring" };
        RebuildRingMesh(mesh, diameter, width);
        return mesh;
    }

    private static void RebuildRingMesh(Mesh mesh, float diameter, float width)
    {
        float normalizedWidth = diameter > Mathf.Epsilon
            ? Mathf.Clamp(width / diameter, 0.001f, 0.5f)
            : 0.5f;
        float innerRadius = 0.5f - normalizedWidth;
        Vector3[] vertices = new Vector3[DiscSegments * 2];
        int[] triangles = new int[DiscSegments * 6];

        for (int index = 0; index < DiscSegments; index++)
        {
            float angle = index * Mathf.PI * 2f / DiscSegments;
            float x = Mathf.Cos(angle);
            float z = Mathf.Sin(angle);
            int vertexIndex = index * 2;
            vertices[vertexIndex] = new Vector3(x * 0.5f, 0f, z * 0.5f);
            vertices[vertexIndex + 1] = new Vector3(x * innerRadius, 0f, z * innerRadius);

            int nextVertexIndex = ((index + 1) % DiscSegments) * 2;
            int triangleIndex = index * 6;
            triangles[triangleIndex] = vertexIndex;
            triangles[triangleIndex + 1] = nextVertexIndex;
            triangles[triangleIndex + 2] = vertexIndex + 1;
            triangles[triangleIndex + 3] = vertexIndex + 1;
            triangles[triangleIndex + 4] = nextVertexIndex;
            triangles[triangleIndex + 5] = nextVertexIndex + 1;
        }

        mesh.Clear();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
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
        callAudioDelay = Mathf.Max(0f, callAudioDelay);
    }
}
