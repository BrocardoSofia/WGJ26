using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class PauseMenuController : MonoBehaviour
{
    private Font font;
    private GameObject pausePanel;
    private Button menuButton;
    private MenuSettingsPanel settingsPanel;
    private bool pausedByThisMenu;
    private bool inputWasBlocked;

    private void Awake()
    {
        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        settingsPanel = GetComponent<MenuSettingsPanel>();
        if (settingsPanel == null)
            settingsPanel = gameObject.AddComponent<MenuSettingsPanel>();
        BuildInterface();
    }

    private void Update()
    {
        if (Keyboard.current == null || !Keyboard.current.escapeKey.wasPressedThisFrame)
            return;

        if (pausedByThisMenu && settingsPanel.IsOpen)
            settingsPanel.Close();
        else if (pausedByThisMenu)
            ResumeGame();
        else if (Time.timeScale > 0f)
            PauseGame();
    }

    public void PauseGame()
    {
        if (pausedByThisMenu || Time.timeScale <= 0f)
            return;

        inputWasBlocked = VoiceNoDetector.InputBlocked;
        pausedByThisMenu = true;
        VoiceNoDetector.InputBlocked = true;
        Time.timeScale = 0f;
        pausePanel.SetActive(true);
    }

    public void ResumeGame()
    {
        if (!pausedByThisMenu)
            return;

        settingsPanel.Close();
        pausePanel.SetActive(false);
        pausedByThisMenu = false;
        VoiceNoDetector.InputBlocked = inputWasBlocked;
        Time.timeScale = 1f;
    }

    private void OpenSettings()
    {
        settingsPanel.Open();
    }

    private void ReturnToMainMenu()
    {
        pausedByThisMenu = false;
        Time.timeScale = 1f;
        VoiceNoDetector.InputBlocked = false;
        SceneManager.LoadScene("0.Menu");
    }

    private void OnDestroy()
    {
        if (!pausedByThisMenu)
            return;

        Time.timeScale = 1f;
        VoiceNoDetector.InputBlocked = false;
    }

    private void BuildInterface()
    {
        menuButton = CreateButton("PauseMenuButton", transform, GameLocalization.Get("pause.menu"),
            new Vector2(110f, -50f), new Vector2(180f, 60f), new Vector2(0f, 1f));
        menuButton.onClick.AddListener(PauseGame);

        pausePanel = CreateUiObject("PauseMenuPanel", transform);
        RectTransform panelRect = pausePanel.AddComponent<RectTransform>();
        panelRect.anchorMin = panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(700f, 500f);
        Image panelBackground = pausePanel.AddComponent<Image>();
        panelBackground.color = new Color(0.08f, 0.08f, 0.08f, 0.96f);

        CreateText("PauseTitle", panelRect, GameLocalization.Get("pause.title"),
            new Vector2(0f, 180f), new Vector2(600f, 55f), 36, FontStyle.Bold);

        Button resumeButton = CreateButton("ResumeButton", panelRect,
            GameLocalization.Get("pause.resume"), new Vector2(0f, 75f),
            new Vector2(500f, 70f), new Vector2(0.5f, 0.5f));
        resumeButton.onClick.AddListener(ResumeGame);

        Button settingsButton = CreateButton("PauseSettingsButton", panelRect,
            GameLocalization.Get("menu.settings"), new Vector2(0f, -15f),
            new Vector2(500f, 70f), new Vector2(0.5f, 0.5f));
        settingsButton.onClick.AddListener(OpenSettings);

        Button mainMenuButton = CreateButton("MainMenuButton", panelRect,
            GameLocalization.Get("pause.mainMenu"), new Vector2(0f, -105f),
            new Vector2(500f, 70f), new Vector2(0.5f, 0.5f));
        mainMenuButton.onClick.AddListener(ReturnToMainMenu);

        pausePanel.SetActive(false);
    }

    private Button CreateButton(string objectName, Transform parent, string label,
        Vector2 position, Vector2 size, Vector2 anchor)
    {
        GameObject buttonObject = DefaultControls.CreateButton(new DefaultControls.Resources());
        buttonObject.name = objectName;
        buttonObject.transform.SetParent(parent, false);
        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = anchor;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        Text text = buttonObject.GetComponentInChildren<Text>();
        text.font = font;
        text.fontSize = 26;
        text.fontStyle = FontStyle.Bold;
        text.text = label;
        return buttonObject.GetComponent<Button>();
    }

    private void CreateText(string objectName, Transform parent, string content,
        Vector2 position, Vector2 size, int fontSize, FontStyle style)
    {
        GameObject textObject = CreateUiObject(objectName, parent);
        RectTransform rect = textObject.AddComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        Text text = textObject.AddComponent<Text>();
        text.font = font;
        text.fontSize = fontSize;
        text.fontStyle = style;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;
        text.text = content;
    }

    private static GameObject CreateUiObject(string objectName, Transform parent)
    {
        GameObject uiObject = new(objectName);
        uiObject.layer = 5;
        uiObject.transform.SetParent(parent, false);
        return uiObject;
    }
}
