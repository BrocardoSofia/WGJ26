using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class IntroController : MonoBehaviour
{
    [SerializeField] private Button nextButton;

    private GameObject firstText;
    private GameObject secondText;

    private void Awake()
    {
        Transform firstTextTransform = transform.Find("PrimerTexto");
        Transform secondTextTransform = transform.Find("SegundoTexto");
        if (secondTextTransform == null)
            secondTextTransform = transform.Find("SeguntoTexto");

        firstText = firstTextTransform != null ? firstTextTransform.gameObject : null;
        secondText = secondTextTransform != null ? secondTextTransform.gameObject : null;

        LocalizeTextGroup(firstText);
        LocalizeTextGroup(secondText);

        if (firstText != null)
            firstText.SetActive(true);

        if (secondText != null)
            secondText.SetActive(false);

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

        GoToLevel();
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
}
