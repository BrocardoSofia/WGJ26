using System.Collections.Generic;
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

    [SerializeField, Min(0.1f), InspectorName("Duración de cada café (segundos)")]
    private float coffeeDurationSeconds = 10f;

    [SerializeField, Min(0f), InspectorName("Aumento de velocidad por café")]
    private float speedIncreasePerCoffee = 2f;

    [SerializeField, Min(1), InspectorName("Cafés para Game Over")]
    private int coffeesForGameOver = 4;

    [Header("Audio del Player")]
    [SerializeField, InspectorName("Audio al tomar café")]
    private AudioClip coffeeReceivedAudio;

    [SerializeField, Range(0f, 2f), InspectorName("Volumen individual al tomar café")]
    private float coffeeReceivedAudioVolume = 1f;

    [SerializeField, InspectorName("Panel de Game Over")]
    private GameObject gameOverPanel;

    [SerializeField, InspectorName("Texto de Game Over")]
    private Text gameOverText;

    [SerializeField, InspectorName("Texto de la razón del Game Over")]
    private Text gameOverReasonText;

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
    private readonly List<float> coffeeRemainingTimes = new();
    private bool gameOver;

    public int CoffeeCount => coffeeCount;

    private void Awake()
    {
        playerMovement = GetComponent<ConstantPlaneMover>();
        int initialCoffeeCount = Mathf.Min(coffeeCount, GetCoffeeCapacity());
        coffeeRemainingTimes.Clear();
        for (int index = 0; index < initialCoffeeCount; index++)
            coffeeRemainingTimes.Add(coffeeDurationSeconds);
        coffeeCount = coffeeRemainingTimes.Count;

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
            TriggerGameOver(GameLocalization.Get("game.coffeeOverReason"));
        else if (!gameOver && gameOverPanel != null)
            gameOverPanel.SetActive(false);
        if (victoryPanel != null)
            victoryPanel.SetActive(false);
    }

    private void Update()
    {
        if (gameOver || coffeeRemainingTimes.Count == 0)
            return;

        float elapsedTime = Time.deltaTime;
        while (elapsedTime > 0f && coffeeRemainingTimes.Count > 0)
        {
            int oldestCoffeeIndex = coffeeRemainingTimes.Count - 1;
            float oldestRemainingTime = coffeeRemainingTimes[oldestCoffeeIndex];
            if (oldestRemainingTime > elapsedTime)
            {
                coffeeRemainingTimes[oldestCoffeeIndex] = oldestRemainingTime - elapsedTime;
                elapsedTime = 0f;
            }
            else
            {
                elapsedTime -= oldestRemainingTime;
                RemoveOldestCoffee();
            }
        }

        RefreshCounter();
    }

    public void AddCoffee(int amount)
    {
        if (amount <= 0 || gameOver)
            return;

        AudioManager.PlaySfx(coffeeReceivedAudio, coffeeReceivedAudioVolume);

        for (int index = 0; index < amount; index++)
        {
            if (coffeeRemainingTimes.Count >= GetCoffeeCapacity())
            {
                TriggerGameOver(GameLocalization.Get("game.coffeeOverReason"));
                return;
            }

            coffeeRemainingTimes.Insert(0, coffeeDurationSeconds);
            IncreaseSpeedForOneCoffee();
        }

        coffeeCount = coffeeRemainingTimes.Count;
        RefreshCounter();
    }

    public void ConsumeCoffee()
    {
        ConsumeCoffeeTime(coffeeDurationSeconds);
    }

    public void ConsumeHalfCoffee()
    {
        ConsumeCoffeeTime(coffeeDurationSeconds * 0.5f);
    }

    private void ConsumeCoffeeTime(float timeToConsume)
    {
        if (coffeeRemainingTimes.Count == 0 || gameOver)
            return;

        float remainingTimeToConsume = Mathf.Max(0f, timeToConsume);
        while (remainingTimeToConsume > 0f && coffeeRemainingTimes.Count > 0)
        {
            int oldestCoffeeIndex = coffeeRemainingTimes.Count - 1;
            float oldestRemainingTime = coffeeRemainingTimes[oldestCoffeeIndex];

            if (oldestRemainingTime > remainingTimeToConsume)
            {
                coffeeRemainingTimes[oldestCoffeeIndex] =
                    oldestRemainingTime - remainingTimeToConsume;
                remainingTimeToConsume = 0f;
            }
            else
            {
                remainingTimeToConsume -= oldestRemainingTime;
                RemoveOldestCoffee();
            }
        }

        RefreshCounter();
    }

    private void IncreaseSpeedForOneCoffee()
    {
        if (playerMovement == null)
            playerMovement = GetComponent<ConstantPlaneMover>();
        if (playerMovement != null)
            playerMovement.Speed += speedIncreasePerCoffee;
    }

    private void RemoveOldestCoffee()
    {
        if (coffeeRemainingTimes.Count == 0)
            return;

        coffeeRemainingTimes.RemoveAt(coffeeRemainingTimes.Count - 1);
        coffeeCount = coffeeRemainingTimes.Count;

        if (playerMovement == null)
            playerMovement = GetComponent<ConstantPlaneMover>();
        if (playerMovement != null)
            playerMovement.Speed = Mathf.Max(0f, playerMovement.Speed - speedIncreasePerCoffee);
    }

    private int GetCoffeeCapacity()
    {
        int visualCapacity = coffeeIcons != null && coffeeIcons.Length > 0
            ? coffeeIcons.Length
            : coffeesForGameOver - 1;
        return Mathf.Max(0, Mathf.Min(visualCapacity, coffeesForGameOver - 1));
    }

    private void RefreshCounter()
    {
        if (coffeeIcons == null)
            return;

        for (int index = 0; index < coffeeIcons.Length; index++)
        {
            if (coffeeIcons[index] != null)
            {
                float fillProgress;
                if (index >= coffeeRemainingTimes.Count)
                {
                    fillProgress = 0f;
                }
                else if (index == coffeeRemainingTimes.Count - 1)
                {
                    fillProgress = Mathf.Clamp01(
                        coffeeRemainingTimes[index] / coffeeDurationSeconds);
                }
                else
                {
                    fillProgress = 1f;
                }

                Image coffeeIcon = coffeeIcons[index];
                coffeeIcon.type = Image.Type.Filled;
                coffeeIcon.fillMethod = Image.FillMethod.Horizontal;
                coffeeIcon.fillOrigin = (int)Image.OriginHorizontal.Left;
                coffeeIcon.fillAmount = fillProgress;
            }
        }
    }

    private void TriggerGameOver(string reason)
    {
        if (gameOver)
            return;

        gameOver = true;
        VoiceNoDetector.InputBlocked = true;
        if (gameOverText != null)
            gameOverText.text = GameLocalization.Get("game.gameOverTitle");
        if (gameOverReasonText != null)
            gameOverReasonText.text = reason;
        if (gameOverPanel != null)
            gameOverPanel.SetActive(true);
        Time.timeScale = 0f;
    }

    public void TimeExpiredGameOver()
    {
        TriggerGameOver(GameLocalization.Get("game.timeOverReason"));
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
        coffeeDurationSeconds = Mathf.Max(0.1f, coffeeDurationSeconds);
        coffeesForGameOver = Mathf.Max(1, coffeesForGameOver);
        coffeeReceivedAudioVolume = Mathf.Clamp(coffeeReceivedAudioVolume, 0f, 2f);
    }
}
