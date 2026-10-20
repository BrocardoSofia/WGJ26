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

    public bool IsInteractingWithNpc => movementState == MovementState.WaitingAtNpc;

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
    [SerializeField, Min(0f), InspectorName("Aumento de atracción por café")]
    private float npcAttractionIncreasePerCoffee = 0.25f;

    [Header("Indicador visual de No")]
    [SerializeField] private GameObject noFeedbackImage;
    [SerializeField, Min(0f), InspectorName("Segundos visible después de decir No")]
    private float noFeedbackDuration = 1f;

    [Header("Audio del Player")]
    [SerializeField, InspectorName("Audio al terminar interacción con NPC")]
    private AudioClip npcInteractionFinishedAudio;

    [SerializeField, Range(0f, 2f), InspectorName("Volumen individual de respuesta")]
    private float npcInteractionFinishedAudioVolume = 1f;

    private PlayerSceneCircle interactionRanges;
    private PlayerCoffeeInventory coffeeInventory;
    private CameraFacingBillboard playerBillboard;
    private Transform activeNpc;
    private MovementState movementState;
    private float originalZ;
    private float waitTimer;
    private float npcInteractionSide;
    private float noCorrectionTimer;
    private float noFeedbackRemaining;
    private readonly List<Transform> waitingNpcs = new();
    private readonly HashSet<NpcInteractionCircle> trackedNpcCircles = new();
    private readonly HashSet<NpcInteractionCircle> activeNpcCirclesThisFrame = new();
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
        coffeeInventory = GetComponent<PlayerCoffeeInventory>();
        playerBillboard = GetComponent<CameraFacingBillboard>();
        originalZ = transform.position.z;
        if (noFeedbackImage != null)
            noFeedbackImage.SetActive(false);
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
        ClearInteractionFacing();
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
        UpdateNoFeedbackImage();
        UpdateNpcCallQueue();

        if (movementState == MovementState.WaitingAtNpc)
        {
            UpdateNpcWait();
            return;
        }

        if (movementState == MovementState.ReturningToOriginalZ)
        {
            if (TryStartNpcApproach())
            {
                ApproachActiveNpc();
                return;
            }

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
        if (interactionRanges == null ||
            (interactionRanges.NpcCallRadius <= 0f && interactionRanges.CoffeeNpcCallRadius <= 0f))
            return false;

        while (waitingNpcs.Count > 0)
        {
            Transform nextNpc = waitingNpcs[0];
            waitingNpcs.RemoveAt(0);
            if (!IsCallableNpc(nextNpc))
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

        activeNpcCirclesThisFrame.Clear();

        for (int index = waitingNpcs.Count - 1; index >= 0; index--)
        {
            Transform queuedNpc = waitingNpcs[index];
            if (!IsCallableNpc(queuedNpc) ||
                !IsInsideNpcCallRange(queuedNpc))
            {
                waitingNpcs.RemoveAt(index);
            }
        }

        List<Transform> newCalls = new();
        AddNpcCallsWithTag("InteractiveNPC", newCalls);
        AddNpcCallsWithTag("CoffeeNPC", newCalls);

        // El círculo del NPC activo debe seguir visible durante el acercamiento,
        // el posicionamiento y los dos segundos de interacción. Se apaga recién
        // cuando la interacción termina y el NPC deja de ser interactuable.
        if (IsCallableNpc(activeNpc) &&
            (movementState == MovementState.ApproachingNpc ||
             movementState == MovementState.PositioningAtNpc ||
             movementState == MovementState.WaitingAtNpc))
        {
            NpcInteractionCircle activeCircle = GetOrCreateNpcCircle(activeNpc);
            trackedNpcCircles.Add(activeCircle);
            activeNpcCirclesThisFrame.Add(activeCircle);
        }

        foreach (NpcInteractionCircle circle in trackedNpcCircles)
        {
            if (circle != null)
                circle.SetCallActive(activeNpcCirclesThisFrame.Contains(circle));
        }

        newCalls.Sort((first, second) =>
            PlanarSqrDistanceToNpcEdge(first).CompareTo(PlanarSqrDistanceToNpcEdge(second)));
        waitingNpcs.AddRange(newCalls);
    }

    private void AddNpcCallsWithTag(string npcTag, List<Transform> newCalls)
    {
        foreach (GameObject npcObject in GameObject.FindGameObjectsWithTag(npcTag))
        {
            Transform npc = npcObject.transform;
            NpcInteractionCircle npcCircle = GetOrCreateNpcCircle(npc);
            trackedNpcCircles.Add(npcCircle);
            bool isInsideCallRange = IsInsideNpcCallRange(npc);
            if (isInsideCallRange)
                activeNpcCirclesThisFrame.Add(npcCircle);

            if (npc.CompareTag("CoffeeNPC"))
            {
                CoffeeNpc coffeeBehaviour = npc.GetComponent<CoffeeNpc>();
                if (coffeeBehaviour != null)
                    coffeeBehaviour.SetInsidePlayerCallRange(isInsideCallRange);
            }

            if (npc == activeNpc || waitingNpcs.Contains(npc))
                continue;

            if (HasPassedNpcByInteractionDiameter(npc))
            {
                npc.tag = "NPC";
                continue;
            }

            if (isInsideCallRange)
                newCalls.Add(npc);
        }
    }

    private bool IsInsideNpcCallRange(Transform npc)
    {
        if (npc != null && npc.CompareTag("CoffeeNPC"))
        {
            NpcInteractionCircle npcCircle = GetOrCreateNpcCircle(npc);
            float contactDistance = interactionRanges.CoffeeNpcCallRadius + npcCircle.Radius;
            return PlanarCenterSqrDistance(npc) <= contactDistance * contactDistance;
        }

        float callRadius = GetCallRadiusForNpc(npc);
        return PlanarSqrDistanceToNpcEdge(npc) <= callRadius * callRadius;
    }

    private float GetCallRadiusForNpc(Transform npc)
    {
        return npc != null && npc.CompareTag("CoffeeNPC")
            ? interactionRanges.CoffeeNpcCallRadius
            : interactionRanges.NpcCallRadius;
    }

    private void ApproachActiveNpc()
    {
        if (!IsCallableNpc(activeNpc))
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

        float interactionDistance = GetNpcInteractionDistance(activeNpc);
        if (PlanarCenterSqrDistance(activeNpc) <= interactionDistance * interactionDistance)
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

        if (PlanarCenterSqrDistance(activeNpc) <= interactionDistance * interactionDistance)
            BeginPositioningAtNpc();
    }

    private void OnNoDetected()
    {
        ShowNoFeedbackImage();

        if (RejectCoffeeNpcsInsideCallRange())
            return;

        if (movementState != MovementState.ApproachingNpc)
            return;

        noCorrectionTimer = GetEffectiveNoCorrectionDuration();
    }

    private void ShowNoFeedbackImage()
    {
        if (noFeedbackImage == null)
            return;

        noFeedbackRemaining = noFeedbackDuration;
        noFeedbackImage.SetActive(noFeedbackDuration > 0f);
    }

    private void UpdateNoFeedbackImage()
    {
        if (noFeedbackImage == null || !noFeedbackImage.activeSelf)
            return;

        noFeedbackRemaining -= Time.deltaTime;
        if (noFeedbackRemaining <= 0f)
            noFeedbackImage.SetActive(false);
    }

    private float GetEffectiveNoCorrectionDuration()
    {
        if (coffeeInventory == null)
            coffeeInventory = GetComponent<PlayerCoffeeInventory>();

        int activeCoffees = coffeeInventory != null ? coffeeInventory.CoffeeCount : 0;
        float attractionMultiplier = 1f + activeCoffees * npcAttractionIncreasePerCoffee;
        return noCorrectionDuration / attractionMultiplier;
    }

    private bool RejectCoffeeNpcsInsideCallRange()
    {
        if (interactionRanges == null)
            return false;

        bool rejectedAnyCoffeeNpc = false;
        bool rejectedActiveNpc = false;
        foreach (GameObject npcObject in GameObject.FindGameObjectsWithTag("CoffeeNPC"))
        {
            Transform coffeeNpc = npcObject.transform;
            if (!IsInsideNpcCallRange(coffeeNpc))
                continue;

            CoffeeNpc coffeeBehaviour = coffeeNpc.GetComponent<CoffeeNpc>();
            if (coffeeBehaviour == null || !coffeeBehaviour.CanHearRejection)
                continue;

            rejectedAnyCoffeeNpc = true;
            rejectedActiveNpc |= activeNpc == coffeeNpc;
            waitingNpcs.Remove(coffeeNpc);

            NpcInteractionCircle npcCircle = coffeeNpc.GetComponent<NpcInteractionCircle>();
            if (npcCircle != null)
                npcCircle.SetCallActive(false);

            coffeeNpc.tag = "NPC";
            coffeeBehaviour.Reject();
        }

        if (!rejectedActiveNpc)
            return rejectedAnyCoffeeNpc;

        activeNpc = null;
        noCorrectionTimer = 0f;
        if (!TryStartNpcApproach())
            movementState = MovementState.ReturningToOriginalZ;

        return true;
    }

    private void BeginPositioningAtNpc()
    {
        noCorrectionTimer = 0f;
        VoiceNoDetector.InputBlocked = true;
        npcInteractionSide = transform.position.x < activeNpc.position.x ? -1f : 1f;
        if (playerBillboard != null)
            playerBillboard.SetPlayerInteractionFacing(npcInteractionSide > 0f);
        movementState = MovementState.PositioningAtNpc;
    }

    private void MoveToNpcInteractionPosition()
    {
        if (!IsCallableNpc(activeNpc))
        {
            ClearInteractionFacing();
            movementState = MovementState.ReturningToOriginalZ;
            return;
        }

        float interactionDistance = GetNpcInteractionDistance(activeNpc);
        Vector3 destination = new(
            activeNpc.position.x + npcInteractionSide * interactionDistance,
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

        NpcInteractionCircle npcCircle = activeNpc.GetComponent<NpcInteractionCircle>();
        if (npcCircle != null)
            npcCircle.PlayInteractionAudio();
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

    private float PlanarCenterSqrDistance(Transform npc)
    {
        Vector3 difference = npc.position - transform.position;
        difference.y = 0f;
        return difference.sqrMagnitude;
    }

    private NpcInteractionCircle GetOrCreateNpcCircle(Transform npc)
    {
        NpcInteractionCircle circle = npc.GetComponent<NpcInteractionCircle>();
        if (circle == null)
            circle = npc.gameObject.AddComponent<NpcInteractionCircle>();

        if (interactionRanges != null)
        {
            Color circleColor = npc.CompareTag("CoffeeNPC")
                ? interactionRanges.CoffeeNpcProjectionColor
                : interactionRanges.InteractiveNpcProjectionColor;
            circle.SetVisualSettings(
                interactionRanges.ProjectionLineWidth,
                interactionRanges.ProjectionAlpha,
                circleColor);
        }

        return circle;
    }

    private float GetNpcInteractionDistance(Transform npc)
    {
        NpcInteractionCircle npcCircle = GetOrCreateNpcCircle(npc);
        float contactDistance = interactionRanges.NpcInteractionRadius + npcCircle.Radius;
        return Mathf.Max(0f, contactDistance - npcCircle.RequiredOverlap);
    }

    private bool HasPassedNpcByInteractionDiameter(Transform npc)
    {
        float horizontalDistanceAfterPassing = transform.position.x - npc.position.x;
        return horizontalDistanceAfterPassing >= interactionRanges.NpcInteractionDiameter;
    }

    private static bool IsCallableNpc(Transform npc)
    {
        return npc != null &&
            (npc.CompareTag("InteractiveNPC") || npc.CompareTag("CoffeeNPC"));
    }

    private void CreateNpcCallIndicator()
    {
        if (interactionRanges == null || npcCallIndicator != null)
            return;

        npcCallIndicator = new GameObject("NpcCallIndicator_Runtime");
        npcCallIndicator.hideFlags = HideFlags.HideAndDontSave;

        MeshFilter meshFilter = npcCallIndicator.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = npcCallIndicator.AddComponent<MeshRenderer>();
        meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        meshRenderer.receiveShadows = false;

        npcCallIndicatorMesh = CreateRingMesh(
            diameter: interactionRanges.NpcInteractionDiameter,
            width: interactionRanges.ProjectionLineWidth);
        npcCallIndicatorMesh.hideFlags = HideFlags.HideAndDontSave;
        meshFilter.sharedMesh = npcCallIndicatorMesh;

        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null)
            shader = Shader.Find("Sprites/Default");
        npcCallIndicatorMaterial = new Material(shader);
        npcCallIndicatorMaterial.hideFlags = HideFlags.HideAndDontSave;
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
            IsCallableNpc(activeNpc))
        {
            float centerDistance = Mathf.Sqrt(PlanarCenterSqrDistance(activeNpc));
            NpcInteractionCircle npcCircle = GetOrCreateNpcCircle(activeNpc);
            float callDistance = GetCallRadiusForNpc(activeNpc) + GetNpcCallExtent(activeNpc);
            float interactionTouchDistance = interactionRanges.NpcInteractionRadius + npcCircle.Radius;
            float proximity = callDistance > interactionTouchDistance
                ? Mathf.InverseLerp(callDistance, interactionTouchDistance, centerDistance)
                : 1f;
            float earlyVisibleProximity = Mathf.Sqrt(proximity);
            alpha = interactionRanges.ProjectionAlpha * earlyVisibleProximity;
        }

        SetNpcCallIndicatorAlpha(alpha);
    }

    private static float GetNpcCallExtent(Transform npc)
    {
        Renderer npcRenderer = npc.GetComponent<Renderer>();
        if (npcRenderer == null)
            return 0f;

        return Mathf.Max(npcRenderer.bounds.extents.x, npcRenderer.bounds.extents.z);
    }

    private void SetNpcCallIndicatorAlpha(float alpha)
    {
        if (npcCallIndicatorMaterial == null)
            return;

        Color configuredColor = interactionRanges.PlayerProjectionColor;
        Color color = new(
            configuredColor.r,
            configuredColor.g,
            configuredColor.b,
            Mathf.Clamp01(alpha));
        if (npcCallIndicatorMaterial.HasProperty("_BaseColor"))
            npcCallIndicatorMaterial.SetColor("_BaseColor", color);
        if (npcCallIndicatorMaterial.HasProperty("_Color"))
            npcCallIndicatorMaterial.SetColor("_Color", color);
    }

    private static Mesh CreateRingMesh(float diameter, float width)
    {
        float normalizedWidth = diameter > Mathf.Epsilon
            ? Mathf.Clamp(width / diameter, 0.001f, 0.5f)
            : 0.5f;
        float innerRadius = 0.5f - normalizedWidth;
        Vector3[] vertices = new Vector3[IndicatorSegments * 2];
        int[] triangles = new int[IndicatorSegments * 6];

        for (int index = 0; index < IndicatorSegments; index++)
        {
            float angle = index * Mathf.PI * 2f / IndicatorSegments;
            float x = Mathf.Cos(angle);
            float z = Mathf.Sin(angle);
            int vertexIndex = index * 2;
            vertices[vertexIndex] = new Vector3(x * 0.5f, 0f, z * 0.5f);
            vertices[vertexIndex + 1] = new Vector3(x * innerRadius, 0f, z * innerRadius);

            int nextVertexIndex = ((index + 1) % IndicatorSegments) * 2;
            int triangleIndex = index * 6;
            triangles[triangleIndex] = vertexIndex;
            triangles[triangleIndex + 1] = nextVertexIndex;
            triangles[triangleIndex + 2] = vertexIndex + 1;
            triangles[triangleIndex + 3] = vertexIndex + 1;
            triangles[triangleIndex + 4] = nextVertexIndex;
            triangles[triangleIndex + 5] = nextVertexIndex + 1;
        }

        Mesh mesh = new() { name = "NPC Call Indicator Ring" };
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

        if (IsCallableNpc(activeNpc))
        {
            NpcInteractionCircle npcCircle = activeNpc.GetComponent<NpcInteractionCircle>();
            if (npcCircle != null)
                npcCircle.SetCallActive(false);

            if (activeNpc.CompareTag("CoffeeNPC"))
            {
                CoffeeNpc coffeeNpc = activeNpc.GetComponent<CoffeeNpc>();
                if (coffeeNpc != null)
                    coffeeNpc.CompleteInteraction(GetComponent<PlayerCoffeeInventory>());
            }
            else if (activeNpc.CompareTag("InteractiveNPC"))
            {
                PlayerCoffeeInventory coffeeInventory = GetComponent<PlayerCoffeeInventory>();
                if (coffeeInventory != null)
                    coffeeInventory.ConsumeHalfCoffee();

                AudioManager.PlaySfx(
                    npcInteractionFinishedAudio,
                    npcInteractionFinishedAudioVolume);
            }

            activeNpc.tag = "NPC";
        }

        VoiceNoDetector.InputBlocked = false;
        ClearInteractionFacing();
        activeNpc = null;
        if (!TryStartNpcApproach())
            movementState = MovementState.ReturningToOriginalZ;
    }

    private void ClearInteractionFacing()
    {
        if (playerBillboard != null)
            playerBillboard.ClearPlayerInteractionFacing();
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
        npcAttractionIncreasePerCoffee = Mathf.Max(0f, npcAttractionIncreasePerCoffee);
        noFeedbackDuration = Mathf.Max(0f, noFeedbackDuration);
        if (xLimits.x > xLimits.y)
            xLimits = new Vector2(xLimits.y, xLimits.x);
        if (zLimits.x > zLimits.y)
            zLimits = new Vector2(zLimits.y, zLimits.x);
    }
}

