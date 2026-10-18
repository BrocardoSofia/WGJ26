using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class IntroController : MonoBehaviour
{
    [SerializeField] private Button nextButton;

    private void Awake()
    {
        if (nextButton != null)
        {
            nextButton.onClick.AddListener(GoToLevel);

            Text nextButtonText = nextButton.GetComponentInChildren<Text>(true);
            if (nextButtonText != null)
                nextButtonText.text = GameLocalization.Get("intro.next");
        }
    }

    private static void GoToLevel()
    {
        SceneManager.LoadScene("2.Nivel");
    }
}
