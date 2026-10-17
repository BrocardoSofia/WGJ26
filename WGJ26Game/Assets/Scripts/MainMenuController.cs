using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class MainMenuController : MonoBehaviour
{
    [SerializeField] private Button startGameButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button creditsButton;
    [SerializeField] private Button exitButton;

    private MenuSettingsPanel settingsPanel;

    private void Awake()
    {
        GameSettings.ApplyDisplay();
        settingsPanel = GetComponent<MenuSettingsPanel>();
        if (settingsPanel == null)
            settingsPanel = gameObject.AddComponent<MenuSettingsPanel>();

        if (startGameButton != null)
            startGameButton.onClick.AddListener(StartGame);
        if (settingsButton != null)
            settingsButton.onClick.AddListener(settingsPanel.Open);
        if (creditsButton != null)
            creditsButton.onClick.AddListener(OpenCredits);
        if (exitButton != null)
            exitButton.onClick.AddListener(ExitGame);
    }

    private static void StartGame()
    {
        SceneManager.LoadScene("1.Intro");
    }

    private static void OpenCredits()
    {
        SceneManager.LoadScene("3.Creditos");
    }

    private static void ExitGame()
    {
        Application.Quit();
    }
}
