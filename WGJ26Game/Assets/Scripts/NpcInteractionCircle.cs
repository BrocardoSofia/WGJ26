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
    private const float GroundOffset = 0.04f;
    private const float VisibleAlpha = 0.45f;

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
    private Renderer npcRenderer;
    private Camera mainCamera;
    private Sprite circleSprite;
    private AudioSource npcAudioSource;
    private Coroutine callAudioCoroutine;
    private float currentAudioVolume;
    private readonly Plane[] cameraFrustumPlanes = new Plane[6];
    private bool callActive;
    private bool interactionAudioActive;

    public float Radius => diameter * 0.5f;
    public float RequiredOverlap => requiredOverlap;

    public void SetCircleSprite(Sprite sprite)
    {
        if (circleSprite == sprite)
            return;

        circleSprite = sprite;
        ApplySpriteToMaterial(runtimeMaterial, circleSprite);

        if (runtimeMesh != null)
            SetDiscUvs(runtimeMesh, circleSprite);
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
        SetDiscAlpha(callActive ? VisibleAlpha : 0f);
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
        MeshRenderer meshRenderer = runtimeDisc.AddComponent<MeshRenderer>();
        meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        meshRenderer.receiveShadows = false;

        runtimeMesh = CreateDiscMesh();
        runtimeMesh.hideFlags = HideFlags.HideAndDontSave;
        SetDiscUvs(runtimeMesh, circleSprite);
        meshFilter.sharedMesh = runtimeMesh;

        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null)
            shader = Shader.Find("Sprites/Default");
        runtimeMaterial = new Material(shader);
        runtimeMaterial.hideFlags = HideFlags.HideAndDontSave;
        ConfigureTransparentMaterial(runtimeMaterial);
        ApplySpriteToMaterial(runtimeMaterial, circleSprite);
        meshRenderer.sharedMaterial = runtimeMaterial;
        SetDiscAlpha(0f);
    }

    private bool IsVisibleToMainCamera()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;
        if (mainCamera == null)
            return false;

        if (npcRenderer == null)
            npcRenderer = GetComponent<Renderer>();
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

    private static void SetDiscUvs(Mesh mesh, Sprite sprite)
    {
        if (mesh == null)
            return;

        Rect uvRect = GetSpriteUvRect(sprite);
        Vector2[] uvs = new Vector2[DiscSegments + 1];
        uvs[0] = new Vector2(uvRect.center.x, uvRect.center.y);

        for (int index = 0; index < DiscSegments; index++)
        {
            float angle = index * Mathf.PI * 2f / DiscSegments;
            float normalizedX = Mathf.Cos(angle) * 0.5f + 0.5f;
            float normalizedY = Mathf.Sin(angle) * 0.5f + 0.5f;
            uvs[index + 1] = new Vector2(
                Mathf.Lerp(uvRect.xMin, uvRect.xMax, normalizedX),
                Mathf.Lerp(uvRect.yMin, uvRect.yMax, normalizedY));
        }

        mesh.uv = uvs;
    }

    private static Rect GetSpriteUvRect(Sprite sprite)
    {
        if (sprite == null || sprite.texture == null)
            return new Rect(0f, 0f, 1f, 1f);

        Rect textureRect = sprite.textureRect;
        return new Rect(
            textureRect.x / sprite.texture.width,
            textureRect.y / sprite.texture.height,
            textureRect.width / sprite.texture.width,
            textureRect.height / sprite.texture.height);
    }

    private static void ApplySpriteToMaterial(Material material, Sprite sprite)
    {
        if (material == null || sprite == null || sprite.texture == null)
            return;

        if (material.HasProperty("_BaseMap"))
            material.SetTexture("_BaseMap", sprite.texture);
        if (material.HasProperty("_MainTex"))
            material.SetTexture("_MainTex", sprite.texture);
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
