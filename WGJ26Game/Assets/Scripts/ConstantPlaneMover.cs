using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Moves the character placeholder at a constant speed on the XZ ground plane.
/// All gameplay values are exposed in the Inspector so the placeholder can later
/// be replaced by a sprite without changing the movement logic.
/// </summary>
public sealed class ConstantPlaneMover : MonoBehaviour
{
    private const float NpcWaitSeconds = 2f;
    private const float IndicatorGroundOffset = 0.02f;
    private const int IndicatorSegments = 64;

    private enum MovementState
    {
        Normal,
        ApproachingNpc,
        PositioningAtNpc,
        WaitingAtNpc,
        ReturningToOriginalZ
    }

    [Header("Movement")]
    [SerializeField, Min(0f)] private float speed = 5f;
    [SerializeField] private Vector3 direction = Vector3.right;

    [Header("Ground limits")]
    [SerializeField] private bool wrapAtLimits = true;
    [SerializeField] private Vector2 xLimits = new(-150f, 150f);
    [SerializeField] private Vector2 zLimits = new(-150f, 150f);

    [Header("Resistencia al NPC")]
    [SerializeField, Min(0f)] private float noCorrectionDuration = 0.45f;
    [SerializeField, Range(0f, 1f)] private float noCorrectionStrength = 0.75f;

    [Header("Indicador de llamada NPC")]
    [SerializeField, Range(0f, 1f)] private float npcCallIndicatorMaxAlpha = 0.65f;
    [SerializeField, Min(0f)] private float npcInteractionVisualInset = 0.15f;

    private PlayerSceneCircle interactionRanges;
    private Transform activeNpc;
    private MovementState movementState;
    private float originalZ;
    private float waitTimer;
    private float npcInteractionSide;
    private float noCorrectionTimer;
    private readonly List<Transform> waitingNpcs = new();
    private GameObject npcCallIndicator;
    private Mesh npcCallIndicatorMesh;
    private Material npcCallIndicatorMaterial;

    public float Speed
    {
        get => speed;
        set => speed = Mathf.Max(0f, value);
    }

    private void Start()
    {
        interactionRanges = GetComponent<PlayerSceneCircle>();
        originalZ = transform.position.z;
        CreateNpcCallIndicator();
    }

    private void OnEnable()
    {
        VoiceNoDetector.NoDetected += OnNoDetected;
    }

    private void OnDisable()
    {
        VoiceNoDetector.NoDetected -= OnNoDetected;
        VoiceNoDetector.InputBlocked = false;
    }

    private void OnDestroy()
    {
        if (npcCallIndicator != null)
            Destroy(npcCallIndicator);
        if (npcCallIndicatorMaterial != null)
            Destroy(npcCallIndicatorMaterial);
        if (npcCallIndicatorMesh != null)
            Destroy(npcCallIndicatorMesh);
    }

    private void LateUpdate()
    {
        UpdateNpcCallIndicator();
    }

    private void Update()
    {
        UpdateNpcCallQueue();

        if (movementState == MovementState.WaitingAtNpc)
        {
            UpdateNpcWait();
            return;
        }

        if (movementState == MovementState.ReturningToOriginalZ)
        {
            ReturnToOriginalZ();
            return;
        }

        if (movementState == MovementState.ApproachingNpc)
        {
            ApproachActiveNpc();
            return;
        }

        if (movementState == MovementState.PositioningAtNpc)
        {
            MoveToNpcInteractionPosition();
            return;
        }

        if (TryStartNpcApproach())
        {
            ApproachActiveNpc();
            return;
        }

        Vector3 planarDirection = new(direction.x, 0f, direction.z);
        if (planarDirection.sqrMagnitude < Mathf.Epsilon || speed <= 0f)
            return;

        transform.position += planarDirection.normalized * (speed * Time.deltaTime);

        if (wrapAtLimits)
            WrapPosition();
    }

    private bool TryStartNpcApproach()
    {
        if (interactionRanges == null || interactionRanges.NpcCallRadius <= 0f)
            return false;

        while (waitingNpcs.Count > 0)
        {
            Transform nextNpc = waitingNpcs[0];
            waitingNpcs.RemoveAt(0);
            if (nextNpc == null || !nextNpc.CompareTag("InteractiveNPC"))
                continue;
            if (!IsInsideNpcCallRange(nextNpc))
                continue;

            activeNpc = nextNpc;
            movementState = MovementState.ApproachingNpc;
            return true;
        }

        return false;
    }

    private void UpdateNpcCallQueue()
    {
        if (interactionRanges == null)
            return;

        for (int index = waitingNpcs.Count - 1; index >= 0; index--)
        {
            Transform queuedNpc = waitingNpcs[index];
            if (queuedNpc == null || !queuedNpc.CompareTag("InteractiveNPC") ||
                !IsInsideNpcCallRange(queuedNpc))
            {
                waitingNpcs.RemoveAt(index);
            }
        }

        List<Transform> newCalls = new();
        foreach (GameObject npcObject in GameObject.FindGameObjectsWithTag("InteractiveNPC"))
        {
            Transform npc = npcObject.transform;
            if (npc == activeNpc || waitingNpcs.Contains(npc))
                continue;

            if (HasPassedNpcByInteractionDiameter(npc))
            {
                npc.tag = "NPC";
                continue;
            }

            if (IsInsideNpcCallRange(npc))
                newCalls.Add(npc);
        }

        newCalls.Sort((first, second) =>
            PlanarSqrDistanceToNpcEdge(first).CompareTo(PlanarSqrDistanceToNpcEdge(second)));
        waitingNpcs.AddRange(newCalls);
    }

    private bool IsInsideNpcCallRange(Transform npc)
    {
        float callRadius = interactionRanges.NpcCallRadius;
        return PlanarSqrDistanceToNpcEdge(npc) <= callRadius * callRadius;
    }

    private void ApproachActiveNpc()
    {
        if (activeNpc == null || !activeNpc.CompareTag("InteractiveNPC"))
        {
            activeNpc = null;
            if (!TryStartNpcApproach())
                movementState = MovementState.ReturningToOriginalZ;
            return;
        }

        if (HasPassedNpcByInteractionDiameter(activeNpc))
        {
            activeNpc.tag = "NPC";
            activeNpc = null;
            noCorrectionTimer = 0f;
            if (!TryStartNpcApproach())
                movementState = MovementState.ReturningToOriginalZ;
            return;
        }

        float interactionRadius = GetAdjustedInteractionRadius();
        if (PlanarSqrDistanceToNpcEdge(activeNpc) <= interactionRadius * interactionRadius)
        {
            BeginPositioningAtNpc();
            return;
        }

        if (speed <= 0f)
            return;

        Vector3 targetPosition = new(activeNpc.position.x, transform.position.y, activeNpc.position.z);
        if (noCorrectionTimer > 0f)
        {
            noCorrectionTimer -= Time.deltaTime;
            Vector3 npcDirection = (targetPosition - transform.position).normalized;
            float zDifference = originalZ - transform.position.z;
            Vector3 correctionDirection = new Vector3(
                1f,
                0f,
                Mathf.Abs(zDifference) > 0.001f ? Mathf.Sign(zDifference) : 0f).normalized;
            Vector3 resistedDirection = Vector3.Lerp(
                npcDirection,
                correctionDirection,
                noCorrectionStrength).normalized;
            transform.position += resistedDirection * (speed * Time.deltaTime);
        }
        else
        {
            transform.position = Vector3.MoveTowards(
                transform.position,
                targetPosition,
                speed * Time.deltaTime);
        }

        if (PlanarSqrDistanceToNpcEdge(activeNpc) <= interactionRadius * interactionRadius)
            BeginPositioningAtNpc();
    }

    private void OnNoDetected()
    {
        if (movementState != MovementState.ApproachingNpc)
            return;

        noCorrectionTimer = noCorrectionDuration;
    }

    private void BeginPositioningAtNpc()
    {
        noCorrectionTimer = 0f;
        VoiceNoDetector.InputBlocked = true;
        npcInteractionSide = transform.position.x < activeNpc.position.x ? -1f : 1f;
        movementState = MovementState.PositioningAtNpc;
    }

    private float GetAdjustedInteractionRadius()
    {
        return Mathf.Max(0f,
            interactionRanges.NpcInteractionRadius - npcInteractionVisualInset);
    }

    private void MoveToNpcInteractionPosition()
    {
        if (activeNpc == null || !activeNpc.CompareTag("InteractiveNPC"))
        {
            movementState = MovementState.ReturningToOriginalZ;
            return;
        }

        float npcHalfWidth = GetNpcHalfWidth(activeNpc);
        Vector3 destination = new(
            activeNpc.position.x + npcInteractionSide *
                (interactionRanges.NpcInteractionRadius + npcHalfWidth),
            transform.position.y,
            activeNpc.position.z);

        if (speed > 0f)
        {
            transform.position = Vector3.MoveTowards(
                transform.position,
                destination,
                speed * Time.deltaTime);
        }

        if ((transform.position - destination).sqrMagnitude > 0.000001f)
            return;

        waitTimer = NpcWaitSeconds;
        movementState = MovementState.WaitingAtNpc;
    }

    private float PlanarSqrDistanceToNpcEdge(Transform npc)
    {
        Renderer npcRenderer = npc.GetComponent<Renderer>();
        if (npcRenderer == null)
        {
            Vector3 centerDifference = npc.position - transform.position;
            centerDifference.y = 0f;
            return centerDifference.sqrMagnitude;
        }

        Vector3 closestPoint = npcRenderer.bounds.ClosestPoint(transform.position);
        Vector3 difference = closestPoint - transform.position;
        difference.y = 0f;
        return difference.sqrMagnitude;
    }

    private static float GetNpcHalfWidth(Transform npc)
    {
        Renderer npcRenderer = npc.GetComponent<Renderer>();
        return npcRenderer != null ? npcRenderer.bounds.extents.x : 0f;
    }

    private bool HasPassedNpcByInteractionDiameter(Transform npc)
    {
        float horizontalDistanceAfterPassing = transform.position.x - npc.position.x;
        return horizontalDistanceAfterPassing >= interactionRanges.NpcInteractionDiameter;
    }

    private void CreateNpcCallIndicator()
    {
        if (interactionRanges == null || npcCallIndicator != null)
            return;

        npcCallIndicator = new GameObject("NpcCallIndicator_Runtime");
        npcCallIndicator.hideFlags = HideFlags.DontSave;

        MeshFilter meshFilter = npcCallIndicator.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = npcCallIndicator.AddComponent<MeshRenderer>();
        meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        meshRenderer.receiveShadows = false;

        npcCallIndicatorMesh = CreateDiscMesh();
        meshFilter.sharedMesh = npcCallIndicatorMesh;

        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null)
            shader = Shader.Find("Sprites/Default");
        npcCallIndicatorMaterial = new Material(shader);
        ConfigureTransparentMaterial(npcCallIndicatorMaterial);
        meshRenderer.sharedMaterial = npcCallIndicatorMaterial;

        float diameter = interactionRanges.NpcInteractionDiameter;
        npcCallIndicator.transform.localScale = new Vector3(diameter, 1f, diameter);
        SetNpcCallIndicatorAlpha(0f);
    }

    private void UpdateNpcCallIndicator()
    {
        if (npcCallIndicator == null || interactionRanges == null)
            return;

        float groundY = transform.position.y - transform.localScale.y * 0.5f + IndicatorGroundOffset;
        npcCallIndicator.transform.SetPositionAndRotation(
            new Vector3(transform.position.x, groundY, transform.position.z),
            Quaternion.identity);

        float diameter = interactionRanges.NpcInteractionDiameter;
        npcCallIndicator.transform.localScale = new Vector3(diameter, 1f, diameter);

        float alpha = 0f;
        if (movementState == MovementState.ApproachingNpc &&
            activeNpc != null && activeNpc.CompareTag("InteractiveNPC"))
        {
            float edgeDistance = Mathf.Sqrt(PlanarSqrDistanceToNpcEdge(activeNpc));
            float callRadius = interactionRanges.NpcCallRadius;
            float interactionRadius = interactionRanges.NpcInteractionRadius;
            float proximity = callRadius > interactionRadius
                ? Mathf.InverseLerp(callRadius, interactionRadius, edgeDistance)
                : 1f;
            alpha = npcCallIndicatorMaxAlpha * proximity;
        }

        SetNpcCallIndicatorAlpha(alpha);
    }

    private void SetNpcCallIndicatorAlpha(float alpha)
    {
        if (npcCallIndicatorMaterial == null)
            return;

        Color color = new(1f, 0f, 0f, Mathf.Clamp01(alpha));
        if (npcCallIndicatorMaterial.HasProperty("_BaseColor"))
            npcCallIndicatorMaterial.SetColor("_BaseColor", color);
        if (npcCallIndicatorMaterial.HasProperty("_Color"))
            npcCallIndicatorMaterial.SetColor("_Color", color);
    }

    private static Mesh CreateDiscMesh()
    {
        Vector3[] vertices = new Vector3[IndicatorSegments + 1];
        int[] triangles = new int[IndicatorSegments * 3];
        vertices[0] = Vector3.zero;

        for (int index = 0; index < IndicatorSegments; index++)
        {
            float angle = index * Mathf.PI * 2f / IndicatorSegments;
            vertices[index + 1] = new Vector3(Mathf.Cos(angle) * 0.5f, 0f, Mathf.Sin(angle) * 0.5f);

            int triangleIndex = index * 3;
            triangles[triangleIndex] = 0;
            triangles[triangleIndex + 1] = index + 1;
            triangles[triangleIndex + 2] = (index + 1) % IndicatorSegments + 1;
        }

        Mesh mesh = new() { name = "NPC Call Indicator Disc" };
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

    private void UpdateNpcWait()
    {
        waitTimer -= Time.deltaTime;
        if (waitTimer > 0f)
            return;

        if (activeNpc != null && activeNpc.CompareTag("InteractiveNPC"))
            activeNpc.tag = "NPC";

        VoiceNoDetector.InputBlocked = false;
        activeNpc = null;
        if (!TryStartNpcApproach())
            movementState = MovementState.ReturningToOriginalZ;
    }

    private void ReturnToOriginalZ()
    {
        if (speed <= 0f)
            return;

        float zDifference = originalZ - transform.position.z;
        if (Mathf.Abs(zDifference) <= 0.001f)
        {
            Vector3 position = transform.position;
            position.z = originalZ;
            transform.position = position + Vector3.right * (speed * Time.deltaTime);
            movementState = MovementState.Normal;
            if (wrapAtLimits)
                WrapPosition();
            return;
        }

        Vector3 returnDirection = new Vector3(1f, 0f, Mathf.Sign(zDifference)).normalized;
        Vector3 nextPosition = transform.position + returnDirection * (speed * Time.deltaTime);
        if (Mathf.Sign(originalZ - nextPosition.z) != Mathf.Sign(zDifference))
            nextPosition.z = originalZ;

        transform.position = nextPosition;
        if (Mathf.Approximately(transform.position.z, originalZ))
            movementState = MovementState.Normal;

        if (wrapAtLimits)
            WrapPosition();
    }

    private void WrapPosition()
    {
        Vector3 position = transform.position;
        position.x = Wrap(position.x, xLimits.x, xLimits.y);
        position.z = Wrap(position.z, zLimits.x, zLimits.y);
        transform.position = position;
    }

    private static float Wrap(float value, float min, float max)
    {
        if (max <= min)
            return value;

        return min + Mathf.Repeat(value - min, max - min);
    }

    private void OnValidate()
    {
        speed = Mathf.Max(0f, speed);
        noCorrectionDuration = Mathf.Max(0f, noCorrectionDuration);
        npcCallIndicatorMaxAlpha = Mathf.Clamp01(npcCallIndicatorMaxAlpha);
        npcInteractionVisualInset = Mathf.Max(0f, npcInteractionVisualInset);
        if (xLimits.x > xLimits.y)
            xLimits = new Vector2(xLimits.y, xLimits.x);
        if (zLimits.x > zLimits.y)
            zLimits = new Vector2(zLimits.y, zLimits.x);
    }
}
