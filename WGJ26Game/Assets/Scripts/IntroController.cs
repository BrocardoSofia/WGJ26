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
            nextButton.onClick.AddListener(GoToLevel);
    }

    private static void GoToLevel()
    {
        SceneManager.LoadScene("2.Nivel");
    }
}
