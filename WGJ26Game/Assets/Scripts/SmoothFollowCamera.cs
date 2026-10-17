using UnityEngine;

/// <summary>
/// Perspective 3D camera that follows a target only on the X axis.
/// Its initial height, depth and rotation remain fixed.
/// </summary>
public sealed class SmoothFollowCamera : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField, Min(0f)] private float smoothTime = 0.2f;

    private Vector3 velocity;
    private float fixedXOffset;
    private float fixedY;
    private float fixedZ;
    private Quaternion fixedRotation;
    private bool hasInitialXOffset;

    public Transform Target
    {
        get => target;
        set => target = value;
    }

    private void Awake()
    {
        fixedY = transform.position.y;
        fixedZ = transform.position.z;
        fixedRotation = transform.rotation;
    }

    private void LateUpdate()
    {
        if (target == null)
            return;

        if (!hasInitialXOffset)
        {
            fixedXOffset = transform.position.x - target.position.x;
            hasInitialXOffset = true;
        }

        Vector3 desiredPosition = new(
            target.position.x + fixedXOffset,
            fixedY,
            fixedZ);

        transform.position = smoothTime <= 0f
            ? desiredPosition
            : Vector3.SmoothDamp(transform.position, desiredPosition, ref velocity, smoothTime);
        transform.rotation = fixedRotation;
    }

    private void OnValidate()
    {
        smoothTime = Mathf.Max(0f, smoothTime);
    }
}
