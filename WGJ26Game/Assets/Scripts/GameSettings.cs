using UnityEngine;

public static class GameSettings
{
    private const string VoiceControlKey = "settings.voiceControl";
    private const string MusicVolumeKey = "settings.musicVolume";
    private const string EffectsVolumeKey = "settings.effectsVolume";
    private const string ResolutionWidthKey = "settings.resolutionWidth";
    private const string ResolutionHeightKey = "settings.resolutionHeight";
    private const string FullscreenKey = "settings.fullscreen";

    public static bool UseVoiceControl
    {
        get => PlayerPrefs.GetInt(VoiceControlKey, 1) == 1;
        set => SaveInt(VoiceControlKey, value ? 1 : 0);
    }

    public static float MusicVolume
    {
        get => PlayerPrefs.GetFloat(MusicVolumeKey, 1f);
        set => SaveFloat(MusicVolumeKey, Mathf.Clamp01(value));
    }

    public static float EffectsVolume
    {
        get => PlayerPrefs.GetFloat(EffectsVolumeKey, 1f);
        set => SaveFloat(EffectsVolumeKey, Mathf.Clamp01(value));
    }

    public static int ResolutionWidth => PlayerPrefs.GetInt(ResolutionWidthKey, 1920);
    public static int ResolutionHeight => PlayerPrefs.GetInt(ResolutionHeightKey, 1080);
    public static bool Fullscreen => PlayerPrefs.GetInt(FullscreenKey, 1) == 1;

    public static void SetDisplay(int width, int height, bool fullscreen)
    {
        bool canUseFullscreen = width == 1920 && height == 1080;
        PlayerPrefs.SetInt(ResolutionWidthKey, width);
        PlayerPrefs.SetInt(ResolutionHeightKey, height);
        PlayerPrefs.SetInt(FullscreenKey, fullscreen && canUseFullscreen ? 1 : 0);
        PlayerPrefs.Save();
        ApplyDisplay();
    }

    public static void ApplyDisplay()
    {
        FullScreenMode mode = Fullscreen
            ? FullScreenMode.FullScreenWindow
            : FullScreenMode.Windowed;
        Screen.SetResolution(ResolutionWidth, ResolutionHeight, mode);
    }

    private static void SaveInt(string key, int value)
    {
        PlayerPrefs.SetInt(key, value);
        PlayerPrefs.Save();
    }

    private static void SaveFloat(string key, float value)
    {
        PlayerPrefs.SetFloat(key, value);
        PlayerPrefs.Save();
    }
}
