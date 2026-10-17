using UnityEngine;

/// <summary>
/// Moves the character placeholder at a constant speed on the XZ ground plane.
/// All gameplay values are exposed in the Inspector so the placeholder can later
/// be replaced by a sprite without changing the movement logic.
/// </summary>
public sealed class ConstantPlaneMover : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField, Min(0f)] private float speed = 5f;
    [SerializeField] private Vector3 direction = Vector3.right;

    [Header("Ground limits")]
    [SerializeField] private bool wrapAtLimits = true;
    [SerializeField] private Vector2 xLimits = new(-150f, 150f);
    [SerializeField] private Vector2 zLimits = new(-150f, 150f);

    public float Speed
    {
        get => speed;
        set => speed = Mathf.Max(0f, value);
    }

    private void Update()
    {
        Vector3 planarDirection = new(direction.x, 0f, direction.z);
        if (planarDirection.sqrMagnitude < Mathf.Epsilon || speed <= 0f)
            return;

        transform.position += planarDirection.normalized * (speed * Time.deltaTime);

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
