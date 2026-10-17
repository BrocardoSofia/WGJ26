using UnityEngine;

[DisallowMultipleComponent]
public sealed class PlayerCoffeeInventory : MonoBehaviour
{
    [SerializeField, Min(0), InspectorName("Cafés")]
    private int coffeeCount;

    public int CoffeeCount => coffeeCount;

    public void AddCoffee(int amount)
    {
        if (amount > 0)
            coffeeCount += amount;
    }
}
