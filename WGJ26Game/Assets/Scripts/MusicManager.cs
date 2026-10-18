using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public sealed class MusicManager : MonoBehaviour
{
    private const string LibraryResourceName = "MusicLibrary";

    private static MusicManager instance;

    private MusicLibrary library;
    private AudioSource audioSource;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void CreatePersistentManager()
    {
        if (instance != null)
            return;

        GameObject managerObject = new("MusicManager");
        instance = managerObject.AddComponent<MusicManager>();
        DontDestroyOnLoad(managerObject);
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

        library = Resources.Load<MusicLibrary>(LibraryResourceName);
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.loop = true;
        audioSource.spatialBlend = 0f;
        audioSource.volume = AudioManager.MusicOutputVolume;

        SceneManager.sceneLoaded += OnSceneLoaded;
        GameSettings.SettingsChanged += ApplyVolume;
    }

    private void OnDestroy()
    {
        if (instance != this)
            return;

        SceneManager.sceneLoaded -= OnSceneLoaded;
        GameSettings.SettingsChanged -= ApplyVolume;
        instance = null;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        PlayMusicForScene(scene.name);
    }

    private void ApplyVolume()
    {
        if (audioSource != null)
            audioSource.volume = AudioManager.MusicOutputVolume;
    }

    private void PlayMusicForScene(string sceneName)
    {
        if (audioSource == null || library == null)
            return;

        AudioClip desiredClip = GetMusicForScene(sceneName);
        if (desiredClip == audioSource.clip)
        {
            if (desiredClip != null && !audioSource.isPlaying)
                audioSource.Play();
            return;
        }

        audioSource.Stop();
        audioSource.clip = desiredClip;

        if (desiredClip != null)
            audioSource.Play();
    }

    private AudioClip GetMusicForScene(string sceneName)
    {
        return sceneName switch
        {
            "0.Menu" => library.MenuAndIntroMusic,
            "1.Intro" => library.MenuAndIntroMusic,
            "2.Nivel" => library.LevelMusic,
            "3.Creditos" => library.CreditsMusic,
            _ => null
        };
    }
}
