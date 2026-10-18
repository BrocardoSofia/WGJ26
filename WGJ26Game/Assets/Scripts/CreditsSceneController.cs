using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class CreditsSceneController : MonoBehaviour
{
    [Header("Datos que no cambian con el idioma")]
    [SerializeField, InspectorName("Programación y diseño de nivel")]
    private string programmerAndLevelDesigner = "Sofia Brocardo";

    [SerializeField, InspectorName("UI, ilustraciones y animaciones 1")]
    private string uiArtistOne = "Camila Boess";

    [SerializeField, InspectorName("UI, ilustraciones y animaciones 2")]
    private string uiArtistTwo = "Carla Rizzi";

    [SerializeField, InspectorName("SFX y música 1")]
    private string audioArtistOne = "Krishna Alin Shetty";

    [SerializeField, InspectorName("SFX y música 2")]
    private string audioArtistTwo = "Amelia Whelpdale";

    [Header("Referencias del Canvas")]
    [SerializeField] private Text titleText;
    [SerializeField] private Text messageText;
    [SerializeField] private Text teamText;
    [SerializeField] private Text musicCreditText;
    [SerializeField] private Text buttonText;
    [SerializeField] private Button returnButton;

    private void OnEnable()
    {
        if (returnButton != null)
        {
            returnButton.onClick.RemoveListener(ReturnToMainMenu);
            returnButton.onClick.AddListener(ReturnToMainMenu);
        }

        GameLocalization.LanguageChanged += RefreshTexts;
        RefreshTexts();
    }

    private void OnDisable()
    {
        GameLocalization.LanguageChanged -= RefreshTexts;
        if (returnButton != null)
        {
            returnButton.onClick.RemoveListener(ReturnToMainMenu);
        }
    }

    private void RefreshTexts()
    {
        if (titleText != null)
        {
            titleText.text = GameLocalization.Get("credits.title");
        }

        if (messageText != null)
        {
            messageText.text = GameLocalization.Get("credits.message");
        }

        if (teamText != null)
        {
            teamText.text = string.Format(
                GameLocalization.Get("credits.team"),
                programmerAndLevelDesigner,
                uiArtistOne,
                uiArtistTwo,
                audioArtistOne,
                audioArtistTwo);
        }

        if (musicCreditText != null)
        {
            musicCreditText.text = GameLocalization.Get("credits.music");
        }

        if (buttonText != null)
        {
            buttonText.text = GameLocalization.Get("pause.mainMenu");
        }
    }

    private static void ReturnToMainMenu()
    {
        SceneManager.LoadScene("0.Menu");
    }
}
