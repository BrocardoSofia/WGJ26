using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class UIButtonSoundInstaller : MonoBehaviour
{
    private const float ScanIntervalSeconds = 1f;

    private static UIButtonSoundInstaller instance;
    private float nextScanTime;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void CreatePersistentInstaller()
    {
        if (instance != null)
            return;

        GameObject installerObject = new("UIButtonSoundInstaller");
        instance = installerObject.AddComponent<UIButtonSoundInstaller>();
        DontDestroyOnLoad(installerObject);
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        if (instance != this)
            return;

        SceneManager.sceneLoaded -= OnSceneLoaded;
        instance = null;
    }

    private void Update()
    {
        if (Time.unscaledTime < nextScanTime)
            return;

        AddSoundsToLoadedButtons();
        nextScanTime = Time.unscaledTime + ScanIntervalSeconds;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        AddSoundsToScene(scene);
        nextScanTime = 0f;
    }

    private static void AddSoundsToLoadedButtons()
    {
        for (int index = 0; index < SceneManager.sceneCount; index++)
        {
            Scene scene = SceneManager.GetSceneAt(index);
            if (scene.isLoaded)
                AddSoundsToScene(scene);
        }
    }

    private static void AddSoundsToScene(Scene scene)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (Button button in root.GetComponentsInChildren<Button>(true))
            {
                if (button.GetComponent<UIButtonSound>() == null)
                    button.gameObject.AddComponent<UIButtonSound>();
            }
        }
    }
}
