using UnityEngine;

[DisallowMultipleComponent]
public sealed class SfxPlayer : MonoBehaviour
{
    [SerializeField, InspectorName("Efecto de sonido")]
    private AudioClip clip;

    [SerializeField, Range(0f, 1f), InspectorName("Volumen individual")]
    private float individualVolume = 1f;

    public void Play()
    {
        AudioManager.PlaySfx(clip, individualVolume);
    }
}
