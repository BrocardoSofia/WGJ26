using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(Image))]
public sealed class ControlModeIndicator : MonoBehaviour
{
    [Header("Sprites de control")]
    [SerializeField] private Sprite microphoneSprite;
    [SerializeField] private Sprite keyboardSprite;
    [SerializeField] private Image targetImage;

    private void Awake()
    {
        if (targetImage == null)
            targetImage = GetComponent<Image>();
    }

    private void OnEnable()
    {
        GameSettings.SettingsChanged += RefreshSprite;
        RefreshSprite();
    }

    private void OnDisable()
    {
        GameSettings.SettingsChanged -= RefreshSprite;
    }

    private void RefreshSprite()
    {
        if (targetImage == null)
            return;

        Sprite selectedSprite = GameSettings.UseVoiceControl
            ? microphoneSprite
            : keyboardSprite;

        if (selectedSprite != null)
            targetImage.sprite = selectedSprite;
    }

    private void Reset()
    {
        targetImage = GetComponent<Image>();
    }
}
