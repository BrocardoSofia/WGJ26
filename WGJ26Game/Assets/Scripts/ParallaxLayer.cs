using UnityEngine;

/// <summary>
/// Moves a decorative group by a fraction of the camera's horizontal motion.
/// This changes only its perceived depth; child positions remain untouched.
/// </summary>
[DefaultExecutionOrder(100)]
public sealed class ParallaxLayer : MonoBehaviour
{
    [SerializeField, Range(-1f, 1f)]
    [InspectorName("Seguimiento de cámara en X")]
    [Tooltip("0 = mundo normal. 1 = casi fijo en cámara. Un valor negativo se mueve más rápido como primer plano.")]
    private float cameraFollowX;

    [SerializeField, InspectorName("Cámara (opcional)")]
    private Camera targetCamera;

    private float previousCameraX;
    private bool initialized;

    private void OnEnable()
    {
        ResolveCamera();
        ResetReferencePosition();
    }

    private void LateUpdate()
    {
        if (targetCamera == null)
        {
            ResolveCamera();
            ResetReferencePosition();
            return;
        }

        float currentCameraX = targetCamera.transform.position.x;
        if (!initialized)
        {
            previousCameraX = currentCameraX;
            initialized = true;
            return;
        }

        float cameraDeltaX = currentCameraX - previousCameraX;
        if (!Mathf.Approximately(cameraDeltaX, 0f))
            transform.position += Vector3.right * (cameraDeltaX * cameraFollowX);

        previousCameraX = currentCameraX;
    }

    private void ResolveCamera()
    {
        if (targetCamera == null)
            targetCamera = Camera.main;
    }

    private void ResetReferencePosition()
    {
        initialized = targetCamera != null;
        if (initialized)
            previousCameraX = targetCamera.transform.position.x;
    }
}
