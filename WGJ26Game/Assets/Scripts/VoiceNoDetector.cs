using System;
using UnityEngine;
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
    [SerializeField] private bool startListeningOnEnable = true;

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
    private KeywordRecognizer recognizer;
#endif

    private void OnEnable()
    {
        if (openWindowsSettingsButton != null)
            openWindowsSettingsButton.onClick.AddListener(OpenWindowsMicrophoneSettings);

        ShowDefaultMicrophoneMessage();
        if (startListeningOnEnable)
            StartListening();
    }

    private void OnDisable()
    {
        if (openWindowsSettingsButton != null)
            openWindowsSettingsButton.onClick.RemoveListener(OpenWindowsMicrophoneSettings);
        StopListening();
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
        int detectedCount = CountNoWords(args.text);
        if (detectedCount == 0)
            return;

        for (int index = 0; index < detectedCount; index++)
            Debug.Log("No");

        SetStatus(detectedCount == 1
            ? "Detectado: No"
            : $"Detectados: {detectedCount} × No");
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

    private void SetStatus(string message)
    {
        if (statusText != null)
            statusText.text = message;
    }
}
