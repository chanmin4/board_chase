using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class FullMapCellUI : MonoBehaviour
{
    [Header("Visibility")]
    [SerializeField] private CanvasGroup _cellCanvasGroup;

    [Header("Refs")]
    [SerializeField] private RectTransform _rectTransform;

    [Tooltip("Cell base image. FullMapUI swaps this sprite by state: current player sector, normal active sector, or inactive sector.")]
    [SerializeField] private Image _background;

    [Tooltip("Optional outline/frame image drawn over Background. Use this only for a shared border/glow layer; leave null if the BG sprite already contains the frame.")]
    [SerializeField] private Image _border;

    [Tooltip("Room type icon image. FullMapUI swaps this sprite by StageRoomType, or uses the undiscovered icon.")]
    [SerializeField] private Image _roomIcon;

    [Tooltip("Coordinate label for this room, such as A2 or B3.")]
    [SerializeField] private TextMeshProUGUI _roomLabelText;

    private Sprite _initialBorderSprite;

    private void Reset()
    {
        _rectTransform = transform as RectTransform;
        _cellCanvasGroup = GetComponent<CanvasGroup>();
    }

    private void Awake()
    {
        if (_rectTransform == null)
            _rectTransform = transform as RectTransform;

        if (_border != null)
            _initialBorderSprite = _border.sprite;

        _cellCanvasGroup = UIVisibilityHelper.EnsureCanvasGroup(gameObject);

        UIVisibilityHelper.ForceActive(_background);
        UIVisibilityHelper.ForceActive(_border);
        UIVisibilityHelper.ForceActive(_roomIcon);
        UIVisibilityHelper.ForceActive(_roomLabelText);
    }

    public void ApplyLayout(
        Vector2 anchoredPosition,
        float scale,
        float rotationZ)
    {
        if (_rectTransform == null)
            return;

        _rectTransform.anchoredPosition = anchoredPosition;
        _rectTransform.localEulerAngles = new Vector3(0f, 0f, rotationZ);
        _rectTransform.localScale = Vector3.one * Mathf.Max(0.01f, scale);
    }

    public void SetCell(
        Sprite backgroundSprite,
        Sprite roomIconSprite,
        Color labelColor,
        string roomLabel,
        bool showRoomLabel,
        bool showBorder,
        float alpha)
    {
        SetVisible(true, alpha);
        SetImage(_background, backgroundSprite, backgroundSprite != null);
        SetBorderVisible(showBorder);
        SetImage(_roomIcon, roomIconSprite, roomIconSprite != null);
        SetText(_roomLabelText, roomLabel, showRoomLabel, labelColor);
    }

    public void SetMissing()
    {
        SetVisible(false, 0f);
        SetImage(_background, null, false);
        SetBorderVisible(false);
        SetImage(_roomIcon, null, false);
        SetText(_roomLabelText, string.Empty, false, Color.white);
    }

    private void SetVisible(bool visible, float alpha)
    {
        UIVisibilityHelper.SetVisible(_cellCanvasGroup, visible, Mathf.Clamp01(alpha));
    }

    private static void SetImage(Image image, Sprite sprite, bool visible)
    {
        if (image == null)
            return;

        image.sprite = sprite;
        image.preserveAspect = true;
        UIVisibilityHelper.SetVisible(image, visible && sprite != null);
    }

    private void SetBorderVisible(bool visible)
    {
        if (_border == null)
            return;

        if (_border.sprite == null && _initialBorderSprite != null)
            _border.sprite = _initialBorderSprite;

        UIVisibilityHelper.SetVisible(_border, visible && _border.sprite != null);
    }

    private static void SetText(TextMeshProUGUI text, string value, bool visible, Color color)
    {
        if (text == null)
            return;

        text.color = color;
        text.text = visible ? value : string.Empty;
        UIVisibilityHelper.SetVisible(text, visible && !string.IsNullOrWhiteSpace(value));
    }
}
