using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class IntroController : MonoBehaviour
{
    [SerializeField] private Button nextButton;
    [SerializeField] private Button microphoneButton;
    [SerializeField] private Button keyboardButton;

    private GameObject firstText;
    private GameObject secondText;
    private GameObject thirdText;
    private ColorBlock microphoneOriginalColors;
    private ColorBlock keyboardOriginalColors;
    private Color microphoneOriginalGraphicColor;
    private Color keyboardOriginalGraphicColor;

    private void Awake()
    {
        Transform firstTextTransform = transform.Find("PrimerTexto");
        Transform secondTextTransform = transform.Find("SegundoTexto");
        if (secondTextTransform == null)
            secondTextTransform = transform.Find("SeguntoTexto");
        Transform thirdTextTransform = transform.Find("TercerTexto");

        firstText = firstTextTransform != null ? firstTextTransform.gameObject : null;
        secondText = secondTextTransform != null ? secondTextTransform.gameObject : null;
        thirdText = thirdTextTransform != null ? thirdTextTransform.gameObject : null;

        if (microphoneButton == null && thirdTextTransform != null)
            microphoneButton = thirdTextTransform.Find("Mic")?.GetComponent<Button>();
        if (keyboardButton == null && thirdTextTransform != null)
            keyboardButton = thirdTextTransform.Find("Teclado")?.GetComponent<Button>();

        if (microphoneButton != null)
        {
            microphoneOriginalColors = microphoneButton.colors;
            if (microphoneButton.targetGraphic != null)
                microphoneOriginalGraphicColor = microphoneButton.targetGraphic.color;
        }
        if (keyboardButton != null)
        {
            keyboardOriginalColors = keyboardButton.colors;
            if (keyboardButton.targetGraphic != null)
                keyboardOriginalGraphicColor = keyboardButton.targetGraphic.color;
        }

        LocalizeTextGroup(firstText);
        LocalizeTextGroup(secondText);
        LocalizeTextGroup(thirdText);

        if (firstText != null)
            firstText.SetActive(true);

        if (secondText != null)
            secondText.SetActive(false);

        if (thirdText != null)
            thirdText.SetActive(false);

        if (microphoneButton != null)
            microphoneButton.onClick.AddListener(SelectMicrophoneControl);
        if (keyboardButton != null)
            keyboardButton.onClick.AddListener(SelectKeyboardControl);

        if (nextButton != null)
        {
            nextButton.onClick.AddListener(OnNextPressed);

            Text nextButtonText = nextButton.GetComponentInChildren<Text>(true);
            if (nextButtonText != null)
                nextButtonText.text = GameLocalization.Get("intro.next");
        }
    }

    private void OnNextPressed()
    {
        if (firstText != null && firstText.activeSelf)
        {
            firstText.SetActive(false);

            if (secondText != null)
                secondText.SetActive(true);

            return;
        }

        if (secondText != null && secondText.activeSelf)
        {
            secondText.SetActive(false);

            if (thirdText != null)
            {
                thirdText.SetActive(true);
                SelectCurrentControlButton();
            }

            return;
        }

        GoToLevel();
    }

    private void SelectMicrophoneControl()
    {
        GameSettings.UseVoiceControl = true;
        UpdateControlSelectionVisuals();
    }

    private void SelectKeyboardControl()
    {
        GameSettings.UseVoiceControl = false;
        UpdateControlSelectionVisuals();
    }

    private void SelectCurrentControlButton()
    {
        UpdateControlSelectionVisuals();
    }

    private void UpdateControlSelectionVisuals()
    {
        bool microphoneSelected = GameSettings.UseVoiceControl;

        ApplyPersistentSelectionColor(
            microphoneButton,
            microphoneOriginalColors,
            microphoneOriginalGraphicColor,
            microphoneSelected);
        ApplyPersistentSelectionColor(
            keyboardButton,
            keyboardOriginalColors,
            keyboardOriginalGraphicColor,
            !microphoneSelected);
        SelectButton(microphoneSelected ? microphoneButton : keyboardButton);
    }

    private static void ApplyPersistentSelectionColor(
        Button button,
        ColorBlock originalColors,
        Color originalGraphicColor,
        bool selected)
    {
        if (button == null)
            return;

        Color selectedColor = originalColors.selectedColor;
        if (ApproximatelyEqual(selectedColor, originalColors.normalColor))
            selectedColor = originalColors.highlightedColor;

        ColorBlock colors = originalColors;
        colors.normalColor = selected ? selectedColor : originalColors.normalColor;
        colors.selectedColor = selected ? selectedColor : originalColors.normalColor;
        button.colors = colors;

        if (button.targetGraphic != null)
        {
            Color graphicColor = originalGraphicColor;
            graphicColor.a = selected ? 1f : originalGraphicColor.a;
            button.targetGraphic.color = graphicColor;

            Color visibleColor = selected ? selectedColor : originalColors.normalColor;
            button.targetGraphic.CrossFadeColor(
                visibleColor * originalColors.colorMultiplier,
                0f,
                true,
                true);
        }
    }

    private static bool ApproximatelyEqual(Color first, Color second)
    {
        const float tolerance = 0.001f;
        return Mathf.Abs(first.r - second.r) < tolerance
            && Mathf.Abs(first.g - second.g) < tolerance
            && Mathf.Abs(first.b - second.b) < tolerance
            && Mathf.Abs(first.a - second.a) < tolerance;
    }

    private static void SelectButton(Button button)
    {
        if (button == null)
            return;

        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(null);

        button.Select();
    }

    private static void GoToLevel()
    {
        SceneManager.LoadScene("2.Nivel");
    }

    private static void LocalizeTextGroup(GameObject textGroup)
    {
        if (textGroup == null)
            return;

        foreach (Text text in textGroup.GetComponentsInChildren<Text>(true))
            text.text = GameLocalization.TranslateText(text.text);
    }

    private void OnDestroy()
    {
        if (nextButton != null)
            nextButton.onClick.RemoveListener(OnNextPressed);
        if (microphoneButton != null)
            microphoneButton.onClick.RemoveListener(SelectMicrophoneControl);
        if (keyboardButton != null)
            keyboardButton.onClick.RemoveListener(SelectKeyboardControl);
    }
}
