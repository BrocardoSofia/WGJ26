using UnityEngine;

/// <summary>
/// Moves the character placeholder at a constant speed on the XZ ground plane.
/// All gameplay values are exposed in the Inspector so the placeholder can later
/// be replaced by a sprite without changing the movement logic.
/// </summary>
public sealed class ConstantPlaneMover : MonoBehaviour
{
    private const float NpcWaitSeconds = 2f;

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

    private PlayerSceneCircle interactionRanges;
    private Transform activeNpc;
    private MovementState movementState;
    private float originalZ;
    private float waitTimer;
    private float npcInteractionSide;

    public float Speed
    {
        get => speed;
        set => speed = Mathf.Max(0f, value);
    }

    private void Start()
    {
        interactionRanges = GetComponent<PlayerSceneCircle>();
        originalZ = transform.position.z;
    }

    private void Update()
    {
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

        GameObject[] interactiveNpcs = GameObject.FindGameObjectsWithTag("InteractiveNPC");
        Transform nearestNpc = null;
        float nearestSqrDistance = float.PositiveInfinity;
        float callRadiusSqr = interactionRanges.NpcCallRadius * interactionRanges.NpcCallRadius;

        foreach (GameObject npc in interactiveNpcs)
        {
            float passedDistance = transform.position.x - npc.transform.position.x;
            if (passedDistance > interactionRanges.NpcInteractionDiameter)
            {
                npc.tag = "NPC";
                continue;
            }

            float sqrDistance = PlanarSqrDistanceToNpcEdge(npc.transform);
            if (sqrDistance <= callRadiusSqr && sqrDistance < nearestSqrDistance)
            {
                nearestNpc = npc.transform;
                nearestSqrDistance = sqrDistance;
            }
        }

        if (nearestNpc == null)
            return false;

        activeNpc = nearestNpc;
        movementState = MovementState.ApproachingNpc;
        return true;
    }

    private void ApproachActiveNpc()
    {
        if (activeNpc == null || !activeNpc.CompareTag("InteractiveNPC"))
        {
            movementState = MovementState.ReturningToOriginalZ;
            return;
        }

        float interactionRadius = interactionRanges.NpcInteractionRadius;
        if (PlanarSqrDistanceToNpcEdge(activeNpc) <= interactionRadius * interactionRadius)
        {
            BeginPositioningAtNpc();
            return;
        }

        if (speed <= 0f)
            return;

        Vector3 targetPosition = new(activeNpc.position.x, transform.position.y, activeNpc.position.z);
        transform.position = Vector3.MoveTowards(
            transform.position,
            targetPosition,
            speed * Time.deltaTime);

        if (PlanarSqrDistanceToNpcEdge(activeNpc) <= interactionRadius * interactionRadius)
            BeginPositioningAtNpc();
    }

    private void BeginPositioningAtNpc()
    {
        npcInteractionSide = transform.position.x < activeNpc.position.x ? -1f : 1f;
        movementState = MovementState.PositioningAtNpc;
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

    private void UpdateNpcWait()
    {
        waitTimer -= Time.deltaTime;
        if (waitTimer > 0f)
            return;

        if (activeNpc != null && activeNpc.CompareTag("InteractiveNPC"))
            activeNpc.tag = "NPC";

        activeNpc = null;
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
        if (xLimits.x > xLimits.y)
            xLimits = new Vector2(xLimits.y, xLimits.x);
        if (zLimits.x > zLimits.y)
            zLimits = new Vector2(zLimits.y, zLimits.x);
    }
}
