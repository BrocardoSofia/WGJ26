using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(Button))]
public sealed class UIButtonSound : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler,
    IPointerDownHandler, ISubmitHandler
{
    private const float HoverOutlineScale = 1.045f;

    private Button button;
    private Image sourceImage;
    private Image hoverOutlineImage;
    private RectTransform sourceRect;
    private RectTransform hoverOutlineRect;
    private Material hoverOutlineMaterial;

    private void Awake()
    {
        button = GetComponent<Button>();
        CreateHoverOutlineImage();
        KeepHoverColorUnchanged();
    }

    private void LateUpdate()
    {
        if (hoverOutlineImage != null && hoverOutlineImage.gameObject.activeSelf)
            MatchOutlineToSource();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!IsInteractable())
            return;

        KeepHoverColorUnchanged();
        SetHoverOutlineVisible(true);
        AudioManager.PlayButtonHover();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        SetHoverOutlineVisible(false);
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

    private void CreateHoverOutlineImage()
    {
        sourceImage = button != null ? button.targetGraphic as Image : null;
        if (sourceImage == null)
            sourceImage = GetComponent<Image>();
        if (sourceImage == null || sourceImage.transform.parent == null)
            return;

        Shader outlineShader = Resources.Load<Shader>("ButtonWhiteSilhouette");
        if (outlineShader == null)
            return;

        GameObject outlineObject = new(
            "ButtonHoverOutline_Runtime",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(LayoutElement));
        outlineObject.hideFlags = HideFlags.DontSave;
        outlineObject.transform.SetParent(sourceImage.transform.parent, false);
        outlineObject.transform.SetSiblingIndex(sourceImage.transform.GetSiblingIndex());

        sourceRect = sourceImage.rectTransform;
        hoverOutlineRect = outlineObject.GetComponent<RectTransform>();
        hoverOutlineImage = outlineObject.GetComponent<Image>();
        outlineObject.GetComponent<LayoutElement>().ignoreLayout = true;
        hoverOutlineMaterial = new Material(outlineShader)
        {
            hideFlags = HideFlags.HideAndDontSave
        };

        hoverOutlineImage.sprite = sourceImage.sprite;
        hoverOutlineImage.overrideSprite = sourceImage.overrideSprite;
        hoverOutlineImage.type = sourceImage.type;
        hoverOutlineImage.preserveAspect = sourceImage.preserveAspect;
        hoverOutlineImage.fillCenter = sourceImage.fillCenter;
        hoverOutlineImage.fillMethod = sourceImage.fillMethod;
        hoverOutlineImage.fillAmount = sourceImage.fillAmount;
        hoverOutlineImage.fillClockwise = sourceImage.fillClockwise;
        hoverOutlineImage.fillOrigin = sourceImage.fillOrigin;
        hoverOutlineImage.pixelsPerUnitMultiplier = sourceImage.pixelsPerUnitMultiplier;
        hoverOutlineImage.color = Color.white;
        hoverOutlineImage.raycastTarget = false;
        hoverOutlineImage.maskable = sourceImage.maskable;
        hoverOutlineImage.material = hoverOutlineMaterial;

        MatchOutlineToSource();
        outlineObject.SetActive(false);
    }

    private void MatchOutlineToSource()
    {
        if (sourceRect == null || hoverOutlineRect == null)
            return;

        hoverOutlineRect.anchorMin = sourceRect.anchorMin;
        hoverOutlineRect.anchorMax = sourceRect.anchorMax;
        hoverOutlineRect.anchoredPosition = sourceRect.anchoredPosition;
        hoverOutlineRect.sizeDelta = sourceRect.sizeDelta;
        hoverOutlineRect.pivot = sourceRect.pivot;
        hoverOutlineRect.localRotation = sourceRect.localRotation;
        hoverOutlineRect.localScale = sourceRect.localScale * HoverOutlineScale;
    }

    private void KeepHoverColorUnchanged()
    {
        if (button == null)
            return;

        ColorBlock colors = button.colors;
        colors.highlightedColor = colors.normalColor;
        button.colors = colors;
    }

    private void SetHoverOutlineVisible(bool visible)
    {
        if (hoverOutlineImage != null)
            hoverOutlineImage.gameObject.SetActive(visible);
    }

    private void OnDisable()
    {
        SetHoverOutlineVisible(false);
    }

    private void OnDestroy()
    {
        if (hoverOutlineImage != null)
            Destroy(hoverOutlineImage.gameObject);
        if (hoverOutlineMaterial != null)
            Destroy(hoverOutlineMaterial);
    }
}
