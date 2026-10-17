using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class CountdownTimer : MonoBehaviour
{
    [SerializeField, Min(0), InspectorName("Minutos iniciales")]
    private int initialMinutes = 2;

    [SerializeField, Range(0, 59), InspectorName("Segundos iniciales")]
    private int initialSeconds;

    [SerializeField, InspectorName("Texto del temporizador")]
    private Text timerText;

    [SerializeField, InspectorName("Estado del jugador")]
    private PlayerCoffeeInventory playerGameState;

    private float remainingSeconds;
    private bool expirationHandled;

    private void Awake()
    {
        remainingSeconds = initialMinutes * 60f + initialSeconds;
        RefreshText();
    }

    private void Start()
    {
        if (remainingSeconds <= 0f)
            HandleTimeExpired();
    }

    private void Update()
    {
        if (remainingSeconds <= 0f)
            return;

        remainingSeconds = Mathf.Max(0f, remainingSeconds - Time.deltaTime);
        RefreshText();
        if (remainingSeconds <= 0f)
            HandleTimeExpired();
    }

    private void HandleTimeExpired()
    {
        if (expirationHandled)
            return;

        expirationHandled = true;
        if (playerGameState != null)
            playerGameState.TimeExpiredGameOver();
    }

    private void RefreshText()
    {
        if (timerText == null)
            return;

        int displayedSeconds = Mathf.CeilToInt(remainingSeconds);
        int minutes = displayedSeconds / 60;
        int seconds = displayedSeconds % 60;
        timerText.text = $"Tiempo: {minutes:00}:{seconds:00}";
    }

    private void OnValidate()
    {
        initialMinutes = Mathf.Max(0, initialMinutes);
        initialSeconds = Mathf.Clamp(initialSeconds, 0, 59);
    }
}
