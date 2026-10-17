using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class CreditsSceneController : MonoBehaviour
{
    private const string CreditsSceneName = "3.Creditos";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void CreateCreditsInterface()
    {
        if (SceneManager.GetActiveScene().name != CreditsSceneName ||
            FindAnyObjectByType<CreditsSceneController>() != null)
            return;

        GameObject controllerObject = new("CreditsSceneController");
        controllerObject.AddComponent<CreditsSceneController>().BuildInterface();
    }

    private void BuildInterface()
    {
        GameObject canvasObject = new("CreditsCanvas", typeof(RectTransform), typeof(Canvas),
            typeof(CanvasScaler), typeof(GraphicRaycaster));

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        GameObject buttonObject = DefaultControls.CreateButton(new DefaultControls.Resources());
        buttonObject.name = "ReturnToMainMenuButton";
        buttonObject.transform.SetParent(canvasObject.transform, false);

        RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(0.5f, 0f);
        buttonRect.anchorMax = new Vector2(0.5f, 0f);
        buttonRect.pivot = new Vector2(0.5f, 0f);
        buttonRect.anchoredPosition = new Vector2(0f, 70f);
        buttonRect.sizeDelta = new Vector2(420f, 80f);

        Text label = buttonObject.GetComponentInChildren<Text>();
        label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        label.fontSize = 28;
        label.text = GameLocalization.Get("pause.mainMenu");

        buttonObject.GetComponent<Button>().onClick.AddListener(ReturnToMainMenu);

        if (FindAnyObjectByType<EventSystem>() == null)
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
    }

    private static void ReturnToMainMenu()
    {
        SceneManager.LoadScene("0.Menu");
    }
}
