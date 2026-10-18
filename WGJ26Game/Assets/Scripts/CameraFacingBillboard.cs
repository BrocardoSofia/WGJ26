using UnityEngine;

/// <summary>
/// Keeps a flat character visual parallel to the camera image plane.
/// The character remains in 3D space and retains normal depth sorting.
/// </summary>
public sealed class CameraFacingBillboard : MonoBehaviour
{
    [SerializeField] private Camera targetCamera;
    [SerializeField] private bool copyCameraRoll;

    private Transform player;
    private float initialScaleX;
    private bool isNpc;
    private bool forceFacingLeft;

    private void Awake()
    {
        initialScaleX = transform.localScale.x;
        isNpc = GetComponent<NpcInteractionCircle>() != null ||
            GetComponent<CoffeeNpc>() != null;
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

        UpdateNpcFacing();
    }

    public void FaceLeftForDeparture()
    {
        if (!isNpc)
            return;

        forceFacingLeft = true;
        SetFlippedOnX(false);
    }

    private void UpdateNpcFacing()
    {
        if (!isNpc || forceFacingLeft)
            return;

        if (player == null)
        {
            ConstantPlaneMover playerMover = FindAnyObjectByType<ConstantPlaneMover>();
            if (playerMover == null)
                return;

            player = playerMover.transform;
        }

        // Los NPC están dibujados mirando a la izquierda. Solo se invierten
        // después de que el centro del Player supera el centro del NPC.
        SetFlippedOnX(player.position.x > transform.position.x);
    }

    private void SetFlippedOnX(bool flipped)
    {
        Vector3 scale = transform.localScale;
        scale.x = flipped ? -initialScaleX : initialScaleX;
        transform.localScale = scale;
    }
}
