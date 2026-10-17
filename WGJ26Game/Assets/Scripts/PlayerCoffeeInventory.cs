using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class PlayerCoffeeInventory : MonoBehaviour
{
    [SerializeField, Min(0), InspectorName("Cafés")]
    private int coffeeCount;

    [SerializeField, InspectorName("Espacios visuales de café")]
    private Image[] coffeeIcons;

    [SerializeField, Min(0f), InspectorName("Aumento de velocidad por café")]
    private float speedIncreasePerCoffee = 2f;

    [SerializeField, Min(1), InspectorName("Cafés para Game Over")]
    private int coffeesForGameOver = 4;

    [SerializeField, InspectorName("Panel de Game Over")]
    private GameObject gameOverPanel;

    [SerializeField, InspectorName("Texto de Game Over")]
    private Text gameOverText;

    [SerializeField, InspectorName("Botón volver a intentar")]
    private Button retryButton;

    [SerializeField, InspectorName("Botón volver al menú")]
    private Button returnToMenuButton;

    [SerializeField, InspectorName("Panel de victoria")]
    private GameObject victoryPanel;

    [SerializeField, InspectorName("Botón ir a créditos")]
    private Button creditsButton;

    [SerializeField, InspectorName("Botón volver a jugar")]
    private Button playAgainButton;

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
        if (creditsButton != null)
            creditsButton.onClick.AddListener(GoToCredits);
        if (playAgainButton != null)
            playAgainButton.onClick.AddListener(Retry);

        RefreshCounter();
        if (coffeeCount >= coffeesForGameOver)
            TriggerGameOver("GAME OVER\nPor sobredosis de café");
        else if (!gameOver && gameOverPanel != null)
            gameOverPanel.SetActive(false);
        if (victoryPanel != null)
            victoryPanel.SetActive(false);
    }

    public void AddCoffee(int amount)
    {
        if (amount <= 0 || gameOver)
            return;

        if (coffeeCount + amount >= coffeesForGameOver)
        {
            coffeeCount = coffeesForGameOver - 1;
            RefreshCounter();
            TriggerGameOver("GAME OVER\nPor sobredosis de café");
            return;
        }

        coffeeCount += amount;
        RefreshCounter();

        if (playerMovement == null)
            playerMovement = GetComponent<ConstantPlaneMover>();
        if (playerMovement != null)
            playerMovement.Speed += speedIncreasePerCoffee * amount;
    }

    public void ConsumeCoffee()
    {
        if (coffeeCount <= 0 || gameOver)
            return;

        coffeeCount--;
        RefreshCounter();

        if (playerMovement == null)
            playerMovement = GetComponent<ConstantPlaneMover>();
        if (playerMovement != null)
            playerMovement.Speed = Mathf.Max(0f, playerMovement.Speed - speedIncreasePerCoffee);
    }

    private void RefreshCounter()
    {
        if (coffeeIcons == null)
            return;

        for (int index = 0; index < coffeeIcons.Length; index++)
        {
            if (coffeeIcons[index] != null)
                coffeeIcons[index].color = index < coffeeCount ? Color.white : Color.black;
        }
    }

    private void TriggerGameOver(string message)
    {
        if (gameOver)
            return;

        gameOver = true;
        VoiceNoDetector.InputBlocked = true;
        if (gameOverText != null)
            gameOverText.text = message;
        if (gameOverPanel != null)
            gameOverPanel.SetActive(true);
        Time.timeScale = 0f;
    }

    public void TimeExpiredGameOver()
    {
        TriggerGameOver("GAME OVER\nNo llegaste a tiempo al trabajo");
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

    public void WinGame()
    {
        if (gameOver)
            return;

        gameOver = true;
        VoiceNoDetector.InputBlocked = true;
        if (victoryPanel != null)
            victoryPanel.SetActive(true);
        Time.timeScale = 0f;
    }

    public void GoToCredits()
    {
        ResumeBeforeChangingScene();
        SceneManager.LoadScene("3.Creditos");
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
