using System;
using UnityEngine;
using UnityEngine.UI;
using DiagnosticsProcess = System.Diagnostics.Process;
using DiagnosticsProcessStartInfo = System.Diagnostics.ProcessStartInfo;

[DisallowMultipleComponent]
public sealed class MenuSettingsPanel : MonoBehaviour
{
    private readonly Vector2Int[] resolutions =
    {
        new(1280, 720),
        new(1600, 900),
        new(1920, 1080)
    };

    private Font font;
    private GameObject panel;
    private Toggle voiceToggle;
    private Toggle keyboardToggle;
    private Toggle fullscreenToggle;
    private Dropdown resolutionDropdown;
    private Dropdown languageDropdown;
    private Slider musicSlider;
    private Slider effectsSlider;

    public bool IsOpen => panel != null && panel.activeSelf;

    private void Awake()
    {
        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        BuildPanel();
    }

    public void Open()
    {
        RefreshSavedValues();
        RefreshMicrophoneName();
        RefreshLocalizedTexts();
        panel.SetActive(true);
        panel.transform.SetAsLastSibling();
    }

    public void Close()
    {
        if (panel != null)
        {
            SaveCurrentValues();
            panel.SetActive(false);
        }
    }

    private void BuildPanel()
    {
        panel = CreateUiObject("SettingsPanel", transform);
        RectTransform panelRect = panel.AddComponent<RectTransform>();
        panelRect.anchorMin = panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(920f, 1000f);
        Image background = panel.AddComponent<Image>();
        background.color = new Color(0.1f, 0.1f, 0.1f, 0.96f);

        CreateText("Title", panelRect, GameLocalization.Get("settings.title"), new Vector2(0f, 450f),
            new Vector2(820f, 55f), 34, FontStyle.Bold);

        CreateText("MicrophoneLabel", panelRect, GetMicrophoneText(), new Vector2(0f, 325f),
            new Vector2(820f, 45f), 22);
        Button microphoneButton = CreateButton("ChangeMicrophoneButton", panelRect,
            GameLocalization.Get("settings.changeMicrophone"), new Vector2(0f, 270f), new Vector2(520f, 55f));
        microphoneButton.onClick.AddListener(OpenWindowsMicrophoneSettings);

        CreateText("ControlLabel", panelRect, GameLocalization.Get("settings.control"), new Vector2(0f, 195f),
            new Vector2(820f, 40f), 24, FontStyle.Bold);
        ToggleGroup controlGroup = panel.AddComponent<ToggleGroup>();
        controlGroup.allowSwitchOff = false;
        voiceToggle = CreateToggle("VoiceControl", panelRect, GameLocalization.Get("settings.voice"),
            new Vector2(-190f, 145f));
        keyboardToggle = CreateToggle("KeyboardControl", panelRect, GameLocalization.Get("settings.keyboard"),
            new Vector2(190f, 145f));
        voiceToggle.group = controlGroup;
        keyboardToggle.group = controlGroup;
        voiceToggle.SetIsOnWithoutNotify(GameSettings.UseVoiceControl);
        keyboardToggle.SetIsOnWithoutNotify(!GameSettings.UseVoiceControl);
        UpdateControlVisuals();
        voiceToggle.onValueChanged.AddListener(enabled =>
        {
            if (enabled)
                GameSettings.UseVoiceControl = true;
            UpdateControlVisuals();
        });
        keyboardToggle.onValueChanged.AddListener(enabled =>
        {
            if (enabled)
                GameSettings.UseVoiceControl = false;
            UpdateControlVisuals();
        });

        CreateText("MusicLabel", panelRect, GameLocalization.Get("settings.music"), new Vector2(0f, 75f),
            new Vector2(820f, 35f), 22);
        musicSlider = CreateSlider("MusicVolume", panelRect, new Vector2(0f, 35f));
        musicSlider.SetValueWithoutNotify(GameSettings.MusicVolume);
        musicSlider.onValueChanged.AddListener(value => GameSettings.MusicVolume = value);

        CreateText("EffectsLabel", panelRect, GameLocalization.Get("settings.effects"), new Vector2(0f, -35f),
            new Vector2(820f, 35f), 22);
        effectsSlider = CreateSlider("EffectsVolume", panelRect, new Vector2(0f, -75f));
        effectsSlider.SetValueWithoutNotify(GameSettings.EffectsVolume);
        effectsSlider.onValueChanged.AddListener(value => GameSettings.EffectsVolume = value);

        CreateText("ResolutionLabel", panelRect, GameLocalization.Get("settings.resolution"), new Vector2(0f, -145f),
            new Vector2(820f, 35f), 22);
        resolutionDropdown = CreateDropdown("ResolutionDropdown", panelRect,
            new Vector2(0f, -195f));
        resolutionDropdown.ClearOptions();
        resolutionDropdown.AddOptions(new System.Collections.Generic.List<string>
        {
            "1280 × 720",
            "1600 × 900",
            "1920 × 1080"
        });
        int resolutionIndex = GetSavedResolutionIndex();
        resolutionDropdown.SetValueWithoutNotify(resolutionIndex);
        resolutionDropdown.RefreshShownValue();
        resolutionDropdown.onValueChanged.AddListener(OnResolutionChanged);

        fullscreenToggle = CreateToggle("FullscreenToggle", panelRect, GameLocalization.Get("settings.fullscreen"),
            new Vector2(0f, -275f));
        bool supportsFullscreen = resolutionIndex == resolutions.Length - 1;
        fullscreenToggle.interactable = supportsFullscreen;
        fullscreenToggle.SetIsOnWithoutNotify(supportsFullscreen && GameSettings.Fullscreen);
        fullscreenToggle.onValueChanged.AddListener(OnFullscreenChanged);

        CreateText("LanguageLabel", panelRect, GameLocalization.Get("settings.language"),
            new Vector2(0f, -335f), new Vector2(820f, 35f), 22);
        languageDropdown = CreateDropdown("LanguageDropdown", panelRect,
            new Vector2(0f, -385f));
        RefreshLanguageOptions();
        languageDropdown.SetValueWithoutNotify((int)GameLocalization.CurrentLanguage);
        languageDropdown.RefreshShownValue();
        languageDropdown.onValueChanged.AddListener(index =>
        {
            GameLocalization.SetLanguage((GameLanguage)Mathf.Clamp(index, 0, 1));
            RefreshLocalizedTexts();
        });

        Button closeButton = CreateButton("CloseSettingsButton", panelRect, "X",
            new Vector2(420f, 460f), new Vector2(55f, 55f));
        Text closeText = closeButton.GetComponentInChildren<Text>();
        if (closeText != null)
            closeText.fontSize = 26;
        closeButton.onClick.AddListener(Close);

        panel.SetActive(false);
    }

    private void RefreshSavedValues()
    {
        bool useVoice = GameSettings.UseVoiceControl;
        voiceToggle.SetIsOnWithoutNotify(useVoice);
        keyboardToggle.SetIsOnWithoutNotify(!useVoice);
        musicSlider.SetValueWithoutNotify(GameSettings.MusicVolume);
        effectsSlider.SetValueWithoutNotify(GameSettings.EffectsVolume);

        int resolutionIndex = GetSavedResolutionIndex();
        resolutionDropdown.SetValueWithoutNotify(resolutionIndex);
        resolutionDropdown.RefreshShownValue();
        bool supportsFullscreen = resolutionIndex == resolutions.Length - 1;
        fullscreenToggle.interactable = supportsFullscreen;
        fullscreenToggle.SetIsOnWithoutNotify(supportsFullscreen && GameSettings.Fullscreen);

        languageDropdown.SetValueWithoutNotify((int)GameLocalization.CurrentLanguage);
        languageDropdown.RefreshShownValue();
        UpdateControlVisuals();
    }

    private void SaveCurrentValues()
    {
        GameSettings.UseVoiceControl = voiceToggle.isOn;
        GameSettings.MusicVolume = musicSlider.value;
        GameSettings.EffectsVolume = effectsSlider.value;

        Vector2Int resolution = resolutions[Mathf.Clamp(resolutionDropdown.value, 0, resolutions.Length - 1)];
        GameSettings.SetDisplay(resolution.x, resolution.y, fullscreenToggle.isOn);
        PlayerPrefs.Save();
    }

    private int GetSavedResolutionIndex()
    {
        for (int index = 0; index < resolutions.Length; index++)
        {
            if (resolutions[index].x == GameSettings.ResolutionWidth &&
                resolutions[index].y == GameSettings.ResolutionHeight)
                return index;
        }

        return resolutions.Length - 1;
    }

    private void OnResolutionChanged(int index)
    {
        Vector2Int resolution = resolutions[Mathf.Clamp(index, 0, resolutions.Length - 1)];
        bool supportsFullscreen = resolution.x == 1920 && resolution.y == 1080;
        if (!supportsFullscreen)
            fullscreenToggle.SetIsOnWithoutNotify(false);
        fullscreenToggle.interactable = supportsFullscreen;
        GameSettings.SetDisplay(resolution.x, resolution.y,
            supportsFullscreen && fullscreenToggle.isOn);
    }

    private void OnFullscreenChanged(bool enabled)
    {
        Vector2Int resolution = resolutions[resolutionDropdown.value];
        if (resolution.x != 1920 || resolution.y != 1080)
        {
            fullscreenToggle.SetIsOnWithoutNotify(false);
            return;
        }

        GameSettings.SetDisplay(resolution.x, resolution.y, enabled);
    }

    private void UpdateControlVisuals()
    {
        UpdateControlToggleVisual(voiceToggle, GameLocalization.Get("settings.voice"));
        UpdateControlToggleVisual(keyboardToggle, GameLocalization.Get("settings.keyboard"));
    }

    private static void UpdateControlToggleVisual(Toggle toggle, string label)
    {
        if (toggle == null)
            return;

        Text text = toggle.GetComponentInChildren<Text>(true);
        if (text != null)
        {
            text.text = toggle.isOn ? $"●  {label}" : $"○  {label}";
            text.color = toggle.isOn
                ? new Color(0.35f, 1f, 0.5f, 1f)
                : Color.white;
        }

        if (toggle.targetGraphic != null)
            toggle.targetGraphic.color = toggle.isOn
                ? new Color(0.15f, 0.55f, 0.25f, 1f)
                : new Color(0.3f, 0.3f, 0.3f, 1f);
    }

    private void RefreshMicrophoneName()
    {
        Transform labelTransform = panel.transform.Find("MicrophoneLabel");
        Text label = labelTransform != null ? labelTransform.GetComponent<Text>() : null;
        if (label != null)
            label.text = GetMicrophoneText();
    }

    private static string GetMicrophoneText()
    {
        string[] devices = Microphone.devices;
        string microphoneName = devices.Length > 0
            ? devices[0]
            : GameLocalization.Get("settings.noMicrophone");
        return string.Format(GameLocalization.Get("settings.microphone"), microphoneName);
    }

    private void RefreshLocalizedTexts()
    {
        SetText("Title", GameLocalization.Get("settings.title"));
        SetText("ControlLabel", GameLocalization.Get("settings.control"));
        SetText("MusicLabel", GameLocalization.Get("settings.music"));
        SetText("EffectsLabel", GameLocalization.Get("settings.effects"));
        SetText("ResolutionLabel", GameLocalization.Get("settings.resolution"));
        SetText("LanguageLabel", GameLocalization.Get("settings.language"));
        SetButtonText("ChangeMicrophoneButton", GameLocalization.Get("settings.changeMicrophone"));
        SetToggleText(fullscreenToggle, GameLocalization.Get("settings.fullscreen"));
        RefreshMicrophoneName();
        RefreshLanguageOptions();
        UpdateControlVisuals();
    }

    private void RefreshLanguageOptions()
    {
        if (languageDropdown == null)
            return;

        int selectedLanguage = (int)GameLocalization.CurrentLanguage;
        languageDropdown.ClearOptions();
        languageDropdown.AddOptions(new System.Collections.Generic.List<string>
        {
            GameLocalization.Get("settings.spanish"),
            GameLocalization.Get("settings.english")
        });
        languageDropdown.SetValueWithoutNotify(selectedLanguage);
        languageDropdown.RefreshShownValue();
    }

    private void SetText(string objectName, string value)
    {
        Transform child = panel.transform.Find(objectName);
        Text text = child != null ? child.GetComponent<Text>() : null;
        if (text != null)
            text.text = value;
    }

    private void SetButtonText(string objectName, string value)
    {
        Transform child = panel.transform.Find(objectName);
        Text text = child != null ? child.GetComponentInChildren<Text>(true) : null;
        if (text != null)
            text.text = value;
    }

    private static void SetToggleText(Toggle toggle, string value)
    {
        Text text = toggle != null ? toggle.GetComponentInChildren<Text>(true) : null;
        if (text != null)
            text.text = value;
    }

    private static void OpenWindowsMicrophoneSettings()
    {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        try
        {
            DiagnosticsProcess.Start(new DiagnosticsProcessStartInfo("ms-settings:sound")
            {
                UseShellExecute = true
            });
        }
        catch (Exception exception)
        {
            Debug.LogError($"No se pudo abrir la configuración de sonido: {exception.Message}");
        }
#endif
    }

    private Text CreateText(string objectName, Transform parent, string content,
        Vector2 position, Vector2 size, int fontSize, FontStyle style = FontStyle.Normal)
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
        return text;
    }

    private Button CreateButton(string objectName, Transform parent, string label,
        Vector2 position, Vector2 size)
    {
        GameObject buttonObject = DefaultControls.CreateButton(new DefaultControls.Resources());
        buttonObject.name = objectName;
        buttonObject.transform.SetParent(parent, false);
        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        Text text = buttonObject.GetComponentInChildren<Text>();
        text.font = font;
        text.fontSize = 22;
        text.fontStyle = FontStyle.Bold;
        text.text = label;
        return buttonObject.GetComponent<Button>();
    }

    private Toggle CreateToggle(string objectName, Transform parent, string label,
        Vector2 position)
    {
        GameObject toggleObject = DefaultControls.CreateToggle(new DefaultControls.Resources());
        toggleObject.name = objectName;
        toggleObject.transform.SetParent(parent, false);
        RectTransform rect = toggleObject.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = new Vector2(330f, 45f);
        Text text = toggleObject.GetComponentInChildren<Text>();
        text.font = font;
        text.fontSize = 21;
        text.color = Color.white;
        text.text = label;
        Toggle toggle = toggleObject.GetComponent<Toggle>();
        if (toggle.targetGraphic != null)
            toggle.targetGraphic.color = new Color(0.3f, 0.3f, 0.3f, 1f);
        if (toggle.graphic != null)
            toggle.graphic.color = new Color(0.25f, 0.9f, 0.4f, 1f);
        return toggle;
    }

    private Slider CreateSlider(string objectName, Transform parent, Vector2 position)
    {
        GameObject sliderObject = DefaultControls.CreateSlider(new DefaultControls.Resources());
        sliderObject.name = objectName;
        sliderObject.transform.SetParent(parent, false);
        RectTransform rect = sliderObject.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = new Vector2(600f, 35f);
        Slider slider = sliderObject.GetComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 1f;
        return slider;
    }

    private Dropdown CreateDropdown(string objectName, Transform parent, Vector2 position)
    {
        GameObject dropdownObject = DefaultControls.CreateDropdown(new DefaultControls.Resources());
        dropdownObject.name = objectName;
        dropdownObject.transform.SetParent(parent, false);
        RectTransform rect = dropdownObject.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = new Vector2(420f, 55f);
        Image dropdownBackground = dropdownObject.GetComponent<Image>();
        if (dropdownBackground != null)
            dropdownBackground.color = new Color(0.92f, 0.92f, 0.92f, 1f);

        Transform template = dropdownObject.transform.Find("Template");
        RectTransform templateRect = template != null ? template.GetComponent<RectTransform>() : null;
        if (templateRect != null)
            templateRect.sizeDelta = new Vector2(0f, 180f);
        Image templateBackground = template != null ? template.GetComponent<Image>() : null;
        if (templateBackground != null)
            templateBackground.color = new Color(0.96f, 0.96f, 0.96f, 1f);

        Transform item = template != null ? template.Find("Viewport/Content/Item") : null;
        RectTransform itemRect = item != null ? item.GetComponent<RectTransform>() : null;
        if (itemRect != null)
            itemRect.sizeDelta = new Vector2(0f, 48f);
        foreach (Text text in dropdownObject.GetComponentsInChildren<Text>(true))
        {
            text.font = font;
            text.fontSize = 20;
            text.color = new Color(0.08f, 0.08f, 0.08f, 1f);
        }
        return dropdownObject.GetComponent<Dropdown>();
    }

    private static GameObject CreateUiObject(string objectName, Transform parent)
    {
        GameObject uiObject = new(objectName);
        uiObject.layer = 5;
        uiObject.transform.SetParent(parent, false);
        return uiObject;
    }
}
