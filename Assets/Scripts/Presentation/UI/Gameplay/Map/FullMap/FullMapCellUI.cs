/*
 * Role hierarchy: Presentation > UI > Gameplay > Map > FullMap
 * Primary responsibility: Presents FullMapCellUI state and handles its user-facing UI interaction.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

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

    [Tooltip("Label drawn directly below the cell while the player is currently in this room.")]
    [SerializeField] private TextMeshProUGUI _playerLabelText;

    private Sprite _initialBorderSprite;

    // Purpose: Initializes Inspector defaults for this component.
    private void Reset()
    {
        _rectTransform = transform as RectTransform;
        _cellCanvasGroup = GetComponent<CanvasGroup>();
    }

    // Purpose: Caches the component references required before runtime initialization.
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
        UIVisibilityHelper.ForceActive(_playerLabelText);
    }

    // Purpose: Applies the requested layout change.
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

    // Purpose: Updates the cell state.
    public void SetCell(
        Sprite backgroundSprite,
        Sprite roomIconSprite,
        Color labelColor,
        string roomLabel,
        bool showRoomLabel,
        string playerLabel,
        bool showPlayerLabel,
        Color playerLabelColor,
        bool showBorder,
        float alpha)
    {
        SetVisible(true, alpha);
        SetImage(_background, backgroundSprite, backgroundSprite != null);
        SetBorderVisible(showBorder);
        SetImage(_roomIcon, roomIconSprite, roomIconSprite != null);
        SetText(_roomLabelText, roomLabel, showRoomLabel, labelColor);
        SetText(_playerLabelText, playerLabel, showPlayerLabel, playerLabelColor);
    }

    // Purpose: Updates the missing state.
    public void SetMissing()
    {
        SetVisible(false, 0f);
        SetImage(_background, null, false);
        SetBorderVisible(false);
        SetImage(_roomIcon, null, false);
        SetText(_roomLabelText, string.Empty, false, Color.white);
        SetText(_playerLabelText, string.Empty, false, Color.white);
    }

    // Purpose: Updates the visible state.
    private void SetVisible(bool visible, float alpha)
    {
        UIVisibilityHelper.SetVisible(_cellCanvasGroup, visible, Mathf.Clamp01(alpha));
    }

    // Purpose: Updates the image state.
    private static void SetImage(Image image, Sprite sprite, bool visible)
    {
        if (image == null)
            return;

        image.sprite = sprite;
        image.preserveAspect = true;
        UIVisibilityHelper.SetVisible(image, visible && sprite != null);
    }

    // Purpose: Updates the border visible state.
    private void SetBorderVisible(bool visible)
    {
        if (_border == null)
            return;

        if (_border.sprite == null && _initialBorderSprite != null)
            _border.sprite = _initialBorderSprite;

        UIVisibilityHelper.SetVisible(_border, visible && _border.sprite != null);
    }

    // Purpose: Updates the text state.
    private static void SetText(TextMeshProUGUI text, string value, bool visible, Color color)
    {
        if (text == null)
            return;

        text.color = color;
        text.text = visible ? value : string.Empty;
        UIVisibilityHelper.SetVisible(text, visible && !string.IsNullOrWhiteSpace(value));
    }
}
