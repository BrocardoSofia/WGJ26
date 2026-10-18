using UnityEngine;

[CreateAssetMenu(fileName = "MusicLibrary", menuName = "WGJ26/Music Library")]
public sealed class MusicLibrary : ScriptableObject
{
    [SerializeField, InspectorName("Música de Menú e Intro")]
    private AudioClip menuAndIntroMusic;

    [SerializeField, InspectorName("Música de Nivel")]
    private AudioClip levelMusic;

    [SerializeField, InspectorName("Música de Créditos")]
    private AudioClip creditsMusic;

    [Header("Ambiente del Nivel")]
    [SerializeField, InspectorName("Ambiente de ciudad")]
    private AudioClip levelCityAmbience;

    [SerializeField, Range(0f, 2f), InspectorName("Volumen individual del ambiente")]
    private float levelCityAmbienceVolume = 1f;

    public AudioClip MenuAndIntroMusic => menuAndIntroMusic;
    public AudioClip LevelMusic => levelMusic;
    public AudioClip CreditsMusic => creditsMusic;
    public AudioClip LevelCityAmbience => levelCityAmbience;
    public float LevelCityAmbienceVolume => levelCityAmbienceVolume;
}
