using UnityEngine;

/// <summary>
/// Keeps a flat character visual parallel to the camera image plane.
/// The character remains in 3D space and retains normal depth sorting.
/// </summary>
public sealed class CameraFacingBillboard : MonoBehaviour
{
    [SerializeField] private Camera targetCamera;
    [SerializeField] private bool copyCameraRoll;

    private void LateUpdate()
    {
        if (targetCamera == null)
            targetCamera = Camera.main;
        if (targetCamera == null)
            return;

        if (copyCameraRoll)
        {
            transform.rotation = targetCamera.transform.rotation;
            return;
        }

        Vector3 cameraForward = targetCamera.transform.forward;
        transform.rotation = Quaternion.LookRotation(cameraForward, Vector3.up);
    }
}
