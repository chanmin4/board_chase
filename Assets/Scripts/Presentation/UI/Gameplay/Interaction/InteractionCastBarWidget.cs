/*
 * Role hierarchy: Presentation > UI > Gameplay > Interaction
 * Primary responsibility: Presents InteractionCastBarWidget state and handles its user-facing UI interaction.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class InteractionCastBarWidget : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private RectTransform _root;
    [SerializeField] private RectTransform _fillRect;
    [SerializeField] private Image _fillImage;
    [SerializeField] private UICanvasGroupOpacity _opacity;
    [SerializeField] private CanvasGroup _canvasGroupFallback;

    private float _fillFullWidth;

    public RectTransform Root => _root != null ? _root : (RectTransform)transform;

    // Purpose: Initializes Inspector defaults for this component.
    private void Reset()
    {
        _root = transform as RectTransform;
        _canvasGroupFallback = GetComponent<CanvasGroup>();
    }

    // Purpose: Caches the component references required before runtime initialization.
    private void Awake()
    {
        if (_root == null)
            _root = transform as RectTransform;

        if (_fillRect == null && _fillImage != null)
            _fillRect = _fillImage.rectTransform;

        if (_canvasGroupFallback == null)
            _canvasGroupFallback = GetComponent<CanvasGroup>();

        if (_fillRect != null)
        {
            _fillFullWidth = _fillRect.sizeDelta.x;
            _fillRect.pivot = new Vector2(0f, 0.5f);
            _fillRect.anchorMin = new Vector2(0f, 0.5f);
            _fillRect.anchorMax = new Vector2(0f, 0.5f);
        }

        EnsureFillImageMode();
        SetProgress(0f);
        SetVisible(false);
    }

    // Purpose: Updates the screen position state.
    public void SetScreenPosition(Vector2 anchoredPosition)
    {
        Root.anchoredPosition = anchoredPosition;
    }

    // Purpose: Updates the progress state.
    public void SetProgress(float progress01)
    {
        float normalized = Mathf.Clamp01(progress01);

        if (_fillImage != null)
        {
            EnsureFillImageMode();
            _fillImage.fillAmount = normalized;
        }

        if (_fillRect != null)
        {
            Vector2 size = _fillRect.sizeDelta;
            size.x = _fillFullWidth * normalized;
            _fillRect.sizeDelta = size;
        }
    }

    // Purpose: Updates the visible state.
    public void SetVisible(bool visible)
    {
        if (_opacity != null)
        {
            if (visible)
                _opacity.Show();
            else
                _opacity.Hide();

            return;
        }

        if (_canvasGroupFallback == null)
            return;

        _canvasGroupFallback.alpha = visible ? 1f : 0f;
        _canvasGroupFallback.interactable = false;
        _canvasGroupFallback.blocksRaycasts = false;
    }

    // Purpose: Ensures fill image mode is ready before the workflow continues.
    private void EnsureFillImageMode()
    {
        if (_fillImage == null)
            return;

        if (_fillImage.type != Image.Type.Filled)
            _fillImage.type = Image.Type.Filled;

        if (_fillImage.fillMethod != Image.FillMethod.Horizontal)
            _fillImage.fillMethod = Image.FillMethod.Horizontal;

        if (_fillImage.fillOrigin != (int)Image.OriginHorizontal.Left)
            _fillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
    }
}
