using UnityEngine;
using UnityEngine.SceneManagement;
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

    [SerializeField, InspectorName("Botón volver a intentar")]
    private Button retryButton;

    [SerializeField, InspectorName("Botón volver al menú")]
    private Button returnToMenuButton;

    private ConstantPlaneMover playerMovement;
    private bool gameOver;

    public int CoffeeCount => coffeeCount;

    private void Awake()
    {
        playerMovement = GetComponent<ConstantPlaneMover>();
        if (retryButton != null)
            retryButton.onClick.AddListener(Retry);
        if (returnToMenuButton != null)
            returnToMenuButton.onClick.AddListener(ReturnToMenu);

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

        if (coffeeCount + amount >= coffeesForGameOver)
        {
            coffeeCount = coffeesForGameOver - 1;
            RefreshCounter();
            TriggerGameOver();
            return;
        }

        coffeeCount += amount;
        RefreshCounter();

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

    public void Retry()
    {
        ResumeBeforeChangingScene();
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void ReturnToMenu()
    {
        ResumeBeforeChangingScene();
        SceneManager.LoadScene("0.Menu");
    }

    private void ResumeBeforeChangingScene()
    {
        Time.timeScale = 1f;
        VoiceNoDetector.InputBlocked = false;
    }

    private void OnDestroy()
    {
        if (!gameOver)
            return;

        ResumeBeforeChangingScene();
    }

    private void OnValidate()
    {
        coffeeCount = Mathf.Max(0, coffeeCount);
        speedIncreasePerCoffee = Mathf.Max(0f, speedIncreasePerCoffee);
        coffeesForGameOver = Mathf.Max(1, coffeesForGameOver);
    }
}
