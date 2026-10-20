using UnityEngine;

/// <summary>
/// Keeps a flat character visual parallel to the camera image plane.
/// The character remains in 3D space and retains normal depth sorting.
/// </summary>
public sealed class CameraFacingBillboard : MonoBehaviour
{
    private const float HorizontalMovementEpsilon = 0.00001f;

    [SerializeField] private Camera targetCamera;
    [SerializeField] private bool copyCameraRoll;

    private Transform player;
    private float initialScaleX;
    private bool isNpc;
    private bool isPlayer;
    private bool forceFacingLeft;
    private bool playerFacingOverrideActive;
    private bool playerOverrideFacesLeft;
    private float previousPositionX;
    private SpriteRenderer playerVisualRenderer;
    private SpriteRenderer npcVisualRenderer;
    private SpriteRenderer trackedPlayerVisualRenderer;

    private void Awake()
    {
        initialScaleX = transform.localScale.x;
        isNpc = GetComponent<NpcInteractionCircle>() != null ||
            GetComponent<CoffeeNpc>() != null;
        isPlayer = GetComponent<ConstantPlaneMover>() != null;
        if (isPlayer)
        {
            Transform playerVisual = transform.Find("PlayerVisual");
            if (playerVisual != null)
                playerVisualRenderer = playerVisual.GetComponent<SpriteRenderer>();
        }
        else if (isNpc)
        {
            Transform npcVisual = transform.Find("NPCVisual");
            if (npcVisual != null)
                npcVisualRenderer = npcVisual.GetComponent<SpriteRenderer>();
            if (npcVisualRenderer == null)
                npcVisualRenderer = GetComponentInChildren<SpriteRenderer>(true);
        }
        previousPositionX = transform.position.x;
    }

    private void LateUpdate()
    {
        if (targetCamera == null)
            targetCamera = Camera.main;
        if (targetCamera == null)
            return;

        if (copyCameraRoll)
        {
            transform.rotation = targetCamera.transform.rotation;
        }
        else
        {
            Vector3 cameraForward = targetCamera.transform.forward;
            transform.rotation = Quaternion.LookRotation(cameraForward, Vector3.up);
        }

        UpdatePlayerFacing();
        UpdateNpcFacing();
    }

    public void FaceLeftForDeparture()
    {
        if (!isNpc)
            return;

        forceFacingLeft = true;
        SetFlippedOnX(false);
    }

    public void SetPlayerInteractionFacing(bool faceLeft)
    {
        if (!isPlayer)
            return;

        playerFacingOverrideActive = true;
        playerOverrideFacesLeft = faceLeft;
        SetFlippedOnX(faceLeft);
    }

    public void ClearPlayerInteractionFacing()
    {
        playerFacingOverrideActive = false;
        previousPositionX = transform.position.x;
    }

    private void UpdateNpcFacing()
    {
        if (!isNpc)
            return;

        if (player == null)
        {
            ConstantPlaneMover playerMover = FindAnyObjectByType<ConstantPlaneMover>();
            if (playerMover == null)
                return;

            player = playerMover.transform;
            Transform playerVisual = player.Find("PlayerVisual");
            if (playerVisual != null)
                trackedPlayerVisualRenderer = playerVisual.GetComponent<SpriteRenderer>();
        }

        UpdateNpcDepthOrder();

        if (forceFacingLeft)
            return;

        // Los NPC están dibujados mirando a la izquierda. Solo se invierten
        // después de que el centro del Player supera el centro del NPC.
        SetFlippedOnX(player.position.x > transform.position.x);
    }

    private void UpdateNpcDepthOrder()
    {
        if (npcVisualRenderer == null || player == null)
            return;

        int playerOrder = trackedPlayerVisualRenderer != null
            ? trackedPlayerVisualRenderer.sortingOrder
            : 0;

        // La cámara mira hacia Z positivo. Si el Player tiene menor Z está
        // delante del NPC; si tiene mayor Z, está detrás.
        bool playerIsInFront = player.position.z < transform.position.z;
        npcVisualRenderer.sortingOrder = playerIsInFront
            ? playerOrder - 1
            : playerOrder + 1;
    }

    private void UpdatePlayerFacing()
    {
        if (!isPlayer)
            return;

        if (playerFacingOverrideActive)
        {
            SetFlippedOnX(playerOverrideFacesLeft);
            previousPositionX = transform.position.x;
            return;
        }

        float horizontalMovement = transform.position.x - previousPositionX;
        if (horizontalMovement < -HorizontalMovementEpsilon)
            SetFlippedOnX(true);
        else if (horizontalMovement > HorizontalMovementEpsilon)
            SetFlippedOnX(false);

        previousPositionX = transform.position.x;
    }

    private void SetFlippedOnX(bool flipped)
    {
        if (isPlayer && playerVisualRenderer != null)
        {
            playerVisualRenderer.flipX = flipped;
            return;
        }

        Vector3 scale = transform.localScale;
        scale.x = flipped ? -initialScaleX : initialScaleX;
        transform.localScale = scale;
    }
}
