using UnityEngine;

[DisallowMultipleComponent]
public sealed class FinishGoal : MonoBehaviour
{
    [SerializeField, InspectorName("Estado del jugador")]
    private PlayerCoffeeInventory playerGameState;

    private bool reached;

    private void Update()
    {
        if (reached || playerGameState == null)
            return;

        PlayerSceneCircle interactionCircle = playerGameState.GetComponent<PlayerSceneCircle>();
        if (interactionCircle == null)
            return;

        Vector3 playerPosition = playerGameState.transform.position;
        Vector3 localPlayerPosition = transform.InverseTransformPoint(playerPosition);
        Vector3 closestLocalPoint = new(
            Mathf.Clamp(localPlayerPosition.x, -0.5f, 0.5f),
            0f,
            Mathf.Clamp(localPlayerPosition.z, -0.5f, 0.5f));
        Vector3 closestWorldPoint = transform.TransformPoint(closestLocalPoint);
        Vector3 difference = playerPosition - closestWorldPoint;
        difference.y = 0f;

        float interactionRadius = interactionCircle.NpcInteractionRadius;
        if (difference.sqrMagnitude > interactionRadius * interactionRadius)
            return;

        reached = true;
        playerGameState.WinGame();
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        Matrix4x4 previousMatrix = Gizmos.matrix;
        Color previousColor = Gizmos.color;
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.color = new Color(0.15f, 1f, 0.2f, 0.22f);
        Gizmos.DrawCube(Vector3.zero, Vector3.one);
        Gizmos.color = new Color(0.15f, 1f, 0.2f, 1f);
        Gizmos.DrawWireCube(Vector3.zero, Vector3.one);
        Gizmos.matrix = previousMatrix;
        Gizmos.color = previousColor;
    }
#endif
}
