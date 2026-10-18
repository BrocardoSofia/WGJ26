using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(Button))]
public sealed class UIButtonSound : MonoBehaviour, IPointerEnterHandler, IPointerDownHandler, ISubmitHandler
{
    private Button button;

    private void Awake()
    {
        button = GetComponent<Button>();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (IsInteractable())
            AudioManager.PlayButtonHover();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left && IsInteractable())
            AudioManager.PlayButtonClick();
    }

    public void OnSubmit(BaseEventData eventData)
    {
        if (IsInteractable())
            AudioManager.PlayButtonClick();
    }

    private bool IsInteractable()
    {
        if (button == null)
            button = GetComponent<Button>();
        return button != null && button.IsInteractable();
    }
}
