using UnityEngine;

[DisallowMultipleComponent]
public sealed class CoffeeNpc : MonoBehaviour
{
    [SerializeField, Min(0), InspectorName("Cafés entregados")]
    private int coffeeAmount = 1;

    [SerializeField, InspectorName("Destino al rechazar")]
    private Vector3 rejectionDestination;

    [SerializeField, Min(0f), InspectorName("Velocidad al retirarse")]
    private float rejectionMoveSpeed = 1f;

    private bool movingToRejectionDestination;

    public void CompleteInteraction(PlayerCoffeeInventory inventory)
    {
        if (inventory != null)
            inventory.AddCoffee(coffeeAmount);
    }

    public void Reject()
    {
        movingToRejectionDestination = true;
    }

    private void Update()
    {
        if (!movingToRejectionDestination || rejectionMoveSpeed <= 0f)
            return;

        transform.position = Vector3.MoveTowards(
            transform.position,
            rejectionDestination,
            rejectionMoveSpeed * Time.deltaTime);

        if ((transform.position - rejectionDestination).sqrMagnitude <= 0.000001f)
            movingToRejectionDestination = false;
    }

    private void OnValidate()
    {
        coffeeAmount = Mathf.Max(0, coffeeAmount);
        rejectionMoveSpeed = Mathf.Max(0f, rejectionMoveSpeed);
    }
}
