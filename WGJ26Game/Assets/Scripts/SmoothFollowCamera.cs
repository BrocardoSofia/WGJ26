using UnityEngine;

/// <summary>
/// Perspective 3D camera that follows a target while preserving depth.
/// Change Offset to tune height, distance and viewing angle.
/// </summary>
public sealed class SmoothFollowCamera : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 offset = new(0f, 12f, -12f);
    [SerializeField, Min(0f)] private float smoothTime = 0.2f;
    [SerializeField] private bool lookAtTarget = true;

    private Vector3 velocity;

    public Transform Target
    {
        get => target;
        set => target = value;
    }

    private void LateUpdate()
    {
        if (target == null)
            return;

        Vector3 desiredPosition = target.position + offset;
        transform.position = smoothTime <= 0f
            ? desiredPosition
            : Vector3.SmoothDamp(transform.position, desiredPosition, ref velocity, smoothTime);

        if (lookAtTarget)
            transform.LookAt(target.position);
    }

    private void OnValidate()
    {
        smoothTime = Mathf.Max(0f, smoothTime);
    }
}
