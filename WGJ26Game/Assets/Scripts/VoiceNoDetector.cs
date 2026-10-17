using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using DiagnosticsProcess = System.Diagnostics.Process;
using DiagnosticsProcessStartInfo = System.Diagnostics.ProcessStartInfo;

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
using UnityEngine.Windows.Speech;
#endif

/// <summary>
/// Lightweight local detection of the word "no" using the microphone selected
/// as the Windows default input. It performs no continuous transcription.
/// </summary>
public sealed class VoiceNoDetector : MonoBehaviour
{
    private static readonly string[] NoPhrases =
    {
        "no",
        "no no",
        "no no no",
        "no no no no",
        "no no no no no"
    };

    [SerializeField] private Text statusText;
    [SerializeField] private Button openWindowsSettingsButton;
    [SerializeField] private Toggle voiceControlToggle;
    [SerializeField] private bool startListeningOnEnable = true;
    [SerializeField, Min(0f)] private float inputDelaySeconds;

    private bool useVoiceControl = true;

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
    private KeywordRecognizer recognizer;
#endif

    private void OnEnable()
    {
        EnsureControlToggle();
        MakeInformationTextReadable();

        if (openWindowsSettingsButton != null)
            openWindowsSettingsButton.onClick.AddListener(OpenWindowsMicrophoneSettings);
        if (voiceControlToggle != null)
        {
            voiceControlToggle.onValueChanged.AddListener(SetVoiceControl);
            useVoiceControl = voiceControlToggle.isOn;
        }

        UpdateModeStatus();
        if (startListeningOnEnable && useVoiceControl)
            StartListening();
    }

    private void Update()
    {
        if (useVoiceControl || Keyboard.current == null)
            return;

        if (Keyboard.current.spaceKey.wasPressedThisFrame)
            QueueNo(1);
    }

    private void OnDisable()
    {
        if (openWindowsSettingsButton != null)
            openWindowsSettingsButton.onClick.RemoveListener(OpenWindowsMicrophoneSettings);
        if (voiceControlToggle != null)
            voiceControlToggle.onValueChanged.RemoveListener(SetVoiceControl);
        StopListening();
    }

    public void SetVoiceControl(bool enabled)
    {
        StopAllCoroutines();
        useVoiceControl = enabled;

        if (useVoiceControl)
            StartListening();
        else
            StopListening();

        UpdateModeStatus();
    }

    public void StartListening()
    {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        if (recognizer != null)
            return;

        try
        {
            recognizer = new KeywordRecognizer(NoPhrases, ConfidenceLevel.Low);
            recognizer.OnPhraseRecognized += OnPhraseRecognized;
            recognizer.Start();
            Debug.Log("Detector de 'No' activo. Usa el micrófono de entrada predeterminado de Windows.");
        }
        catch (Exception exception)
        {
            Debug.LogError($"No se pudo iniciar el detector de 'No': {exception.Message}");
            SetStatus("No se pudo iniciar Windows Speech.");
        }
#else
        SetStatus("Reconocimiento disponible solamente en Windows.");
#endif
    }

    public void StopListening()
    {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        if (recognizer == null)
            return;

        if (recognizer.IsRunning)
            recognizer.Stop();
        recognizer.OnPhraseRecognized -= OnPhraseRecognized;
        recognizer.Dispose();
        recognizer = null;
#endif
    }

    public void OpenWindowsMicrophoneSettings()
    {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        try
        {
            DiagnosticsProcess.Start(new DiagnosticsProcessStartInfo("ms-settings:sound") { UseShellExecute = true });
        }
        catch (Exception exception)
        {
            Debug.LogError($"No se pudo abrir la configuración de sonido: {exception.Message}");
        }
#endif
    }

    private void ShowDefaultMicrophoneMessage()
    {
        SetStatus("Micrófono: entrada predeterminada de Windows");
    }

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
    private void OnPhraseRecognized(PhraseRecognizedEventArgs args)
    {
        if (!useVoiceControl)
            return;

        int detectedCount = CountNoWords(args.text);
        if (detectedCount == 0)
            return;

        QueueNo(detectedCount);
    }

    private static int CountNoWords(string phrase)
    {
        if (string.IsNullOrWhiteSpace(phrase))
            return 0;

        int count = 0;
        string[] words = phrase.Split(new[] { ' ', ',', '.', ';', ':', '!', '?' },
            StringSplitOptions.RemoveEmptyEntries);
        foreach (string word in words)
        {
            if (string.Equals(word, "no", StringComparison.OrdinalIgnoreCase))
                count++;
        }

        return count;
    }
#endif

    private void QueueNo(int count)
    {
        if (inputDelaySeconds <= 0f)
        {
            LogNo(count);
            return;
        }

        StartCoroutine(LogNoAfterDelay(count));
    }

    private IEnumerator LogNoAfterDelay(int count)
    {
        yield return new WaitForSecondsRealtime(inputDelaySeconds);
        LogNo(count);
    }

    private void LogNo(int count)
    {
        for (int index = 0; index < count; index++)
            Debug.Log("No");

        string detection = count == 1 ? "Detectado: No" : $"Detectados: {count} × No";
        SetStatus(useVoiceControl
            ? $"Micrófono: predeterminado de Windows | {detection}"
            : $"Micrófono: predeterminado de Windows (pausado) | {detection}");
    }

    private void UpdateModeStatus()
    {
        if (useVoiceControl)
            ShowDefaultMicrophoneMessage();
        else
            SetStatus("Micrófono: predeterminado de Windows (pausado)");

        UpdateToggleVisual();
    }

    private void UpdateToggleVisual()
    {
        if (voiceControlToggle == null)
            return;

        Text label = voiceControlToggle.GetComponentInChildren<Text>(true);
        if (label != null)
        {
            label.text = useVoiceControl ? "MODO VOZ" : "MODO TECLADO";
            label.color = Color.white;
        }

        Color modeColor = useVoiceControl
            ? new Color(0.12f, 0.55f, 0.22f, 1f)
            : new Color(0.18f, 0.38f, 0.75f, 1f);
        ColorBlock colors = voiceControlToggle.colors;
        colors.normalColor = modeColor;
        colors.highlightedColor = modeColor * 1.15f;
        colors.selectedColor = modeColor;
        colors.pressedColor = modeColor * 0.8f;
        colors.disabledColor = new Color(modeColor.r, modeColor.g, modeColor.b, 0.5f);
        voiceControlToggle.colors = colors;

        if (voiceControlToggle.targetGraphic != null)
            voiceControlToggle.targetGraphic.color = modeColor;
    }

    private void EnsureControlToggle()
    {
        if (voiceControlToggle != null || statusText == null)
            return;

        Transform panel = statusText.transform.parent;
        Transform existingToggle = panel.Find("ControlModeToggle");
        if (existingToggle != null)
        {
            voiceControlToggle = existingToggle.GetComponent<Toggle>();
            return;
        }

        GameObject toggleObject = DefaultControls.CreateToggle(new DefaultControls.Resources());
        toggleObject.name = "ControlModeToggle";
        toggleObject.transform.SetParent(panel, false);

        RectTransform toggleRect = toggleObject.GetComponent<RectTransform>();
        toggleRect.anchorMin = new Vector2(0f, 1f);
        toggleRect.anchorMax = new Vector2(0f, 1f);
        toggleRect.pivot = new Vector2(0f, 1f);
        toggleRect.anchoredPosition = new Vector2(15f, -135f);
        toggleRect.sizeDelta = new Vector2(440f, 35f);

        voiceControlToggle = toggleObject.GetComponent<Toggle>();
        voiceControlToggle.isOn = true;

        Text label = toggleObject.GetComponentInChildren<Text>();
        label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        label.fontSize = 17;
        label.color = Color.white;

        RectTransform panelRect = panel.GetComponent<RectTransform>();
        if (panelRect != null)
            panelRect.sizeDelta = new Vector2(panelRect.sizeDelta.x, 205f);
    }

    private void MakeInformationTextReadable()
    {
        if (statusText == null)
            return;

        statusText.color = Color.white;
        Transform title = statusText.transform.parent.Find("Title");
        Text titleText = title != null ? title.GetComponent<Text>() : null;
        if (titleText != null)
            titleText.color = Color.white;
    }

    private void SetStatus(string message)
    {
        if (statusText != null)
            statusText.text = message;
    }
}
