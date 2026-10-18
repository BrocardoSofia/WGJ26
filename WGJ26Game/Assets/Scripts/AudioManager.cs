using UnityEngine;

[DisallowMultipleComponent]
public sealed class AudioManager : MonoBehaviour
{
    private const string SettingsResourceName = "AudioManagerSettings";
    private const float DefaultMaximumMusicVolume = 0.5f;
    private const float DefaultMaximumSfxVolume = 1f;

    private static AudioManager instance;
    private static AudioManagerSettings settings;

    private AudioSource sfxSource;

    public static float MusicOutputVolume =>
        GameSettings.MusicVolume * MaximumMusicVolume;

    public static float SfxOutputVolume =>
        GameSettings.EffectsVolume * MaximumSfxVolume;

    private static float MaximumMusicVolume =>
        Settings != null ? Settings.MaximumMusicVolume : DefaultMaximumMusicVolume;

    private static float MaximumSfxVolume =>
        Settings != null ? Settings.MaximumSfxVolume : DefaultMaximumSfxVolume;

    private static AudioManagerSettings Settings
    {
        get
        {
            if (settings == null)
                settings = Resources.Load<AudioManagerSettings>(SettingsResourceName);
            return settings;
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void CreatePersistentManager()
    {
        if (instance != null)
            return;

        GameObject managerObject = new("AudioManager");
        instance = managerObject.AddComponent<AudioManager>();
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

        sfxSource = gameObject.AddComponent<AudioSource>();
        sfxSource.playOnAwake = false;
        sfxSource.loop = false;
        sfxSource.spatialBlend = 0f;

        PreloadUiSounds();
    }

    private void OnDestroy()
    {
        if (instance == this)
            instance = null;
    }

    public static void PlaySfx(AudioClip clip, float individualVolume = 1f)
    {
        if (clip == null)
            return;

        EnsureInstance();
        if (instance == null || instance.sfxSource == null)
            return;

        float finalVolume = Mathf.Max(0f, individualVolume) * SfxOutputVolume;
        instance.sfxSource.PlayOneShot(clip, finalVolume);
    }

    public static void PlayButtonHover()
    {
        if (Settings != null)
            PlaySfx(Settings.ButtonHoverSound, Settings.ButtonHoverVolume);
    }

    public static void PlayButtonClick()
    {
        if (Settings != null)
            PlaySfx(Settings.ButtonClickSound, Settings.ButtonClickVolume);
    }

    private static void EnsureInstance()
    {
        if (instance != null)
            return;

        CreatePersistentManager();
    }

    private static void PreloadUiSounds()
    {
        if (Settings == null)
            return;

        if (Settings.ButtonHoverSound != null)
            Settings.ButtonHoverSound.LoadAudioData();
        if (Settings.ButtonClickSound != null)
            Settings.ButtonClickSound.LoadAudioData();
    }
}
