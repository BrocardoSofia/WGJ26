#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class VoiceNoSceneSetup
{
    private const string ScenePath = "Assets/Scenes/2.Nivel.unity";
    private const string RootName = "VoiceNoSystem_Windows";

    [InitializeOnLoadMethod]
    private static void ConfigureAfterScriptReload()
    {
        EditorApplication.delayCall += () =>
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return;
            bool ready = File.Exists(ScenePath) && File.ReadAllText(ScenePath).Contains($"m_Name: {RootName}");
            if (!ready)
                ConfigureScene();
            else if (!File.ReadAllText(ScenePath).Contains("m_Name: ControlModeToggle"))
                AddControlToggleToExistingScene();
        };
    }

    [MenuItem("WGJ26/Configurar detector Windows de No")]
    public static void ConfigureScene()
    {
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        DeleteIfPresent("VoiceNoSystem");
        DeleteIfPresent(RootName);

        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        GameObject root = new(RootName);
        VoiceNoDetector detector = root.AddComponent<VoiceNoDetector>();

        GameObject canvasObject = new("MicrophoneInfoCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObject.transform.SetParent(root.transform, false);
        canvasObject.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        GameObject panel = DefaultControls.CreatePanel(new DefaultControls.Resources());
        panel.name = "MicrophonePanel";
        panel.transform.SetParent(canvasObject.transform, false);
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        SetRect(panelRect, new Vector2(20f, -20f), new Vector2(500f, 205f));
        panel.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.72f);

        Text title = CreateText(panel.transform, font, "Title", "Reconocimiento de voz", 22);
        title.color = Color.white;
        SetRect(title.rectTransform, new Vector2(15f, -8f), new Vector2(470f, 30f));

        Text status = CreateText(panel.transform, font, "Status", "Micrófono: entrada predeterminada de Windows", 17);
        status.color = Color.white;
        SetRect(status.rectTransform, new Vector2(15f, -43f), new Vector2(470f, 30f));

        Toggle controlToggle = CreateControlToggle(panel.transform, font);

        GameObject buttonObject = DefaultControls.CreateButton(new DefaultControls.Resources());
        buttonObject.name = "OpenWindowsSoundSettings";
        buttonObject.transform.SetParent(panel.transform, false);
        SetRect(buttonObject.GetComponent<RectTransform>(), new Vector2(15f, -82f), new Vector2(330f, 42f));
        Button button = buttonObject.GetComponent<Button>();
        Text buttonText = buttonObject.GetComponentInChildren<Text>();
        buttonText.font = font;
        buttonText.fontSize = 17;
        buttonText.text = "Cambiar micrófono en Windows";

        SerializedObject serializedDetector = new(detector);
        serializedDetector.FindProperty("statusText").objectReferenceValue = status;
        serializedDetector.FindProperty("openWindowsSettingsButton").objectReferenceValue = button;
        serializedDetector.FindProperty("voiceControlToggle").objectReferenceValue = controlToggle;
        serializedDetector.ApplyModifiedPropertiesWithoutUndo();

        if (Object.FindAnyObjectByType<EventSystem>() == null)
        {
            GameObject eventSystem = new("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            eventSystem.transform.SetParent(root.transform, false);
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, ScenePath);
        Debug.Log("Detector ligero de Windows y acceso a configuración de micrófono agregados a 2.Nivel.");
    }

    public static void AddControlToggleToExistingScene()
    {
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        VoiceNoDetector detector = Object.FindAnyObjectByType<VoiceNoDetector>();
        GameObject panel = GameObject.Find("MicrophonePanel");
        if (detector == null || panel == null)
            return;

        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        Toggle controlToggle = CreateControlToggle(panel.transform, font);

        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.sizeDelta = new Vector2(panelRect.sizeDelta.x, 205f);

        SerializedObject serializedDetector = new(detector);
        serializedDetector.FindProperty("voiceControlToggle").objectReferenceValue = controlToggle;
        serializedDetector.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, ScenePath);
        Debug.Log("Selector Voz/Teclado agregado al detector de No.");
    }

    private static Toggle CreateControlToggle(Transform parent, Font font)
    {
        GameObject toggleObject = DefaultControls.CreateToggle(new DefaultControls.Resources());
        toggleObject.name = "ControlModeToggle";
        toggleObject.transform.SetParent(parent, false);
        SetRect(toggleObject.GetComponent<RectTransform>(), new Vector2(15f, -135f), new Vector2(440f, 35f));

        Toggle toggle = toggleObject.GetComponent<Toggle>();
        toggle.isOn = true;
        Text label = toggleObject.GetComponentInChildren<Text>();
        label.font = font;
        label.fontSize = 17;
        label.color = Color.white;
        label.text = "MODO VOZ";
        return toggle;
    }

    private static Text CreateText(Transform parent, Font font, string name, string content, int fontSize)
    {
        GameObject textObject = DefaultControls.CreateText(new DefaultControls.Resources());
        textObject.name = name;
        textObject.transform.SetParent(parent, false);
        Text text = textObject.GetComponent<Text>();
        text.font = font;
        text.text = content;
        text.fontSize = fontSize;
        text.alignment = TextAnchor.MiddleLeft;
        return text;
    }

    private static void SetRect(RectTransform rect, Vector2 position, Vector2 size)
    {
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    private static void DeleteIfPresent(string objectName)
    {
        GameObject existing = GameObject.Find(objectName);
        if (existing != null) Object.DestroyImmediate(existing);
    }
}
#endif
