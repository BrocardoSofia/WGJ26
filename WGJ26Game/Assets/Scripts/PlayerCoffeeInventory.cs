using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class PlayerCoffeeInventory : MonoBehaviour
{
    [SerializeField, Min(0), InspectorName("Cafés")]
    private int coffeeCount;

    [SerializeField, InspectorName("Texto contador de cafés")]
    private Text coffeeCountText;

    [SerializeField, Min(0f), InspectorName("Aumento de velocidad por café")]
    private float speedIncreasePerCoffee = 2f;

    [SerializeField, Min(1), InspectorName("Cafés para Game Over")]
    private int coffeesForGameOver = 4;

    [SerializeField, InspectorName("Panel de Game Over")]
    private GameObject gameOverPanel;

    private ConstantPlaneMover playerMovement;
    private bool gameOver;

    public int CoffeeCount => coffeeCount;

    private void Awake()
    {
        playerMovement = GetComponent<ConstantPlaneMover>();
        RefreshCounter();
        if (coffeeCount >= coffeesForGameOver)
            TriggerGameOver();
        else if (gameOverPanel != null)
            gameOverPanel.SetActive(false);
    }

    public void AddCoffee(int amount)
    {
        if (amount <= 0 || gameOver)
            return;

        coffeeCount += amount;
        RefreshCounter();

        if (coffeeCount >= coffeesForGameOver)
        {
            TriggerGameOver();
            return;
        }

        if (playerMovement == null)
            playerMovement = GetComponent<ConstantPlaneMover>();
        if (playerMovement != null)
            playerMovement.Speed += speedIncreasePerCoffee * amount;
    }

    private void RefreshCounter()
    {
        if (coffeeCountText != null)
            coffeeCountText.text = $"Cafés: {coffeeCount}";
    }

    private void TriggerGameOver()
    {
        gameOver = true;
        VoiceNoDetector.InputBlocked = true;
        if (gameOverPanel != null)
            gameOverPanel.SetActive(true);
        Time.timeScale = 0f;
    }

    private void OnDestroy()
    {
        if (!gameOver)
            return;

        Time.timeScale = 1f;
        VoiceNoDetector.InputBlocked = false;
    }

    private void OnValidate()
    {
        coffeeCount = Mathf.Max(0, coffeeCount);
        speedIncreasePerCoffee = Mathf.Max(0f, speedIncreasePerCoffee);
        coffeesForGameOver = Mathf.Max(1, coffeesForGameOver);
    }
}
