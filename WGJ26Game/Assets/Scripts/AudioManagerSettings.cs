using UnityEngine;

[CreateAssetMenu(fileName = "AudioManagerSettings", menuName = "WGJ26/Audio Manager Settings")]
public sealed class AudioManagerSettings : ScriptableObject
{
    [SerializeField, Range(0f, 1f), InspectorName("Volumen máximo de música")]
    private float maximumMusicVolume = 0.5f;

    [SerializeField, Range(0f, 1f), InspectorName("Volumen máximo de SFX")]
    private float maximumSfxVolume = 1f;

    [Header("Sonidos de botones")]
    [SerializeField, InspectorName("Sonido al pasar el cursor")]
    private AudioClip buttonHoverSound;

    [SerializeField, Range(0f, 2f), InspectorName("Volumen individual del hover")]
    private float buttonHoverVolume = 1f;

    [SerializeField, InspectorName("Sonido al hacer click")]
    private AudioClip buttonClickSound;

    [SerializeField, Range(0f, 2f), InspectorName("Volumen individual del click")]
    private float buttonClickVolume = 1f;

    public float MaximumMusicVolume => maximumMusicVolume;
    public float MaximumSfxVolume => maximumSfxVolume;
    public AudioClip ButtonHoverSound => buttonHoverSound;
    public float ButtonHoverVolume => buttonHoverVolume;
    public AudioClip ButtonClickSound => buttonClickSound;
    public float ButtonClickVolume => buttonClickVolume;
}
