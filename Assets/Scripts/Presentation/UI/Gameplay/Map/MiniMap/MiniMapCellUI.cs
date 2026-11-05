/*
 * Role hierarchy: Presentation > UI > Gameplay > Map > MiniMap
 * Primary responsibility: Presents MiniMapCellUI state and handles its user-facing UI interaction.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class MiniMapCellUI : MonoBehaviour
{
    [Header("Visibility Don't Ref Auto")]
    [SerializeField] private CanvasGroup _cellCanvasGroup;

    [Header("Parts")]
    [SerializeField] private Image _background;
    [SerializeField] private Image _icon;
    [SerializeField] private Image _pairedRoomIcon;
    [SerializeField] private Image _extractionIcon;

    [FormerlySerializedAs("_occupancyRatioText")]
    [SerializeField] private TextMeshProUGUI _playerOccupancyRatioText;

    [SerializeField] private TextMeshProUGUI _virusOccupancyRatioText;

    [SerializeField] private TextMeshProUGUI _sectorJudgeTimeText;
    [SerializeField] private TextMeshProUGUI _specialTimerText;

    // Purpose: Initializes Inspector defaults for this component.
    private void Reset()
    {
        _cellCanvasGroup = GetComponent<CanvasGroup>();
    }

    // Purpose: Caches the component references required before runtime initialization.
    private void Awake()
    {
        _cellCanvasGroup = UIVisibilityHelper.EnsureCanvasGroup(gameObject);

        UIVisibilityHelper.ForceActive(_background);
        UIVisibilityHelper.ForceActive(_icon);
        ResetIconPair();
        UIVisibilityHelper.ForceActive(_playerOccupancyRatioText);
        UIVisibilityHelper.ForceActive(_virusOccupancyRatioText);
        UIVisibilityHelper.ForceActive(_sectorJudgeTimeText);
        UIVisibilityHelper.ForceActive(_specialTimerText);
    }

    // Purpose: Updates the missing state.
    public void SetMissing()
    {
        SetVisible(false);
        SetSpecialTimer(string.Empty, false);
        ClearIcon();
        ClearRatios();
        ClearJudgeTime();
    }

    // Purpose: Updates the locked state.
    public void SetLocked(Color backgroundColor, Sprite lockedIcon)
    {
        SetVisible(true);
        SetSpecialTimer(string.Empty, false);
        SetBackground(backgroundColor);
        SetIcon(lockedIcon);
        ClearRatios();
        ClearJudgeTime();
    }

    // Purpose: Updates the start sector state.
    public void SetStartSector(Color backgroundColor, float visibleAlpha = 1f)
    {
        SetVisible(true, visibleAlpha);
        SetSpecialTimer(string.Empty, false);
        SetBackground(backgroundColor);
        ClearIcon();
        ClearRatios();
        ClearJudgeTime();
    }

    // Purpose: Updates the opened state.
    public void SetOpened(
        Color backgroundColor,
        Sprite iconSprite,
        string playerRatioText,
        string virusRatioText,
        bool showIcon,
        bool showRatios,
        string judgeTimeText,
        bool showJudgeTime,
        float visibleAlpha = 1f)
    {
        SetVisible(true, visibleAlpha);
        SetBackground(backgroundColor);

        if (showIcon)
            SetIcon(iconSprite);
        else
            ClearIcon();

        if (showRatios)
            SetRatios(playerRatioText, virusRatioText);
        else
            ClearRatios();

        if (showJudgeTime)
            SetJudgeTime(judgeTimeText);
        else
            ClearJudgeTime();
    }

    // Purpose: Updates the visible state.
    private void SetVisible(bool visible)
    {
        SetVisible(visible, 1f);
    }

    // Purpose: Updates the visible state.
    private void SetVisible(bool visible, float visibleAlpha)
    {
        UIVisibilityHelper.SetVisible(_cellCanvasGroup, visible, visibleAlpha);
    }

    // Purpose: Updates the background state.
    private void SetBackground(Color color)
    {
        if (_background == null)
            return;

        _background.color = color;
        UIVisibilityHelper.SetVisible(_background, true);
    }

    // Purpose: Updates the icon state.
    private void SetIcon(Sprite sprite)
    {
        ResetIconPair();
        if (_icon == null)
            return;

        _icon.sprite = sprite;
        _icon.preserveAspect = true;
        UIVisibilityHelper.SetVisible(_icon, sprite != null);
    }

    // Purpose: Clears the current icon state.
    private void ClearIcon()
    {
        ResetIconPair();
        if (_icon == null)
            return;

        _icon.sprite = null;
        UIVisibilityHelper.SetVisible(_icon, false);
    }

    // Separate authored Images leave the original icon's child labels and layout untouched.
    public void SetExtractionOverlay(Sprite roomIcon, Sprite extractionIcon)
    {
        ResetIconPair();
        if (extractionIcon == null) return;
        if (roomIcon == null || _pairedRoomIcon == null || _extractionIcon == null)
        {
            // Older, unlinked cell variants must at least retain the critical exit marker.
            SetIcon(extractionIcon);
            return;
        }
        if (_icon != null) _icon.enabled = false;
        _pairedRoomIcon.sprite = roomIcon;
        _extractionIcon.sprite = extractionIcon;
        _pairedRoomIcon.preserveAspect = _extractionIcon.preserveAspect = true;
        _pairedRoomIcon.enabled = _extractionIcon.enabled = true;
    }

    private void ResetIconPair()
    {
        if (_icon != null) _icon.enabled = true;
        if (_pairedRoomIcon != null) { _pairedRoomIcon.sprite = null; _pairedRoomIcon.enabled = false; }
        if (_extractionIcon != null) { _extractionIcon.sprite = null; _extractionIcon.enabled = false; }
    }

    // Purpose: Updates the ratios state.
    private void SetRatios(string playerText, string virusText)
    {
        if (_playerOccupancyRatioText != null)
        {
            _playerOccupancyRatioText.text = playerText;
            UIVisibilityHelper.SetVisible(_playerOccupancyRatioText, true);
        }

        if (_virusOccupancyRatioText != null)
        {
            _virusOccupancyRatioText.text = virusText;
            UIVisibilityHelper.SetVisible(_virusOccupancyRatioText, true);
        }
    }

    // Purpose: Clears the current ratios state.
    private void ClearRatios()
    {
        if (_playerOccupancyRatioText != null)
        {
            _playerOccupancyRatioText.text = string.Empty;
            UIVisibilityHelper.SetVisible(_playerOccupancyRatioText, false);
        }

        if (_virusOccupancyRatioText != null)
        {
            _virusOccupancyRatioText.text = string.Empty;
            UIVisibilityHelper.SetVisible(_virusOccupancyRatioText, false);
        }
    }

    // Purpose: Updates the judge time state.
    private void SetJudgeTime(string text)
    {
        if (_sectorJudgeTimeText == null)
            return;

        _sectorJudgeTimeText.text = text;
        UIVisibilityHelper.SetVisible(_sectorJudgeTimeText, true);
    }

    // Purpose: Clears the current judge time state.
    private void ClearJudgeTime()
    {
        if (_sectorJudgeTimeText == null)
            return;

        _sectorJudgeTimeText.text = string.Empty;
        UIVisibilityHelper.SetVisible(_sectorJudgeTimeText, false);
    }

    // Purpose: Updates the special timer state.
    public void SetSpecialTimer(string text, bool visible)
    {
        if (_specialTimerText == null)
            return;

        _specialTimerText.text = visible ? text : string.Empty;
        UIVisibilityHelper.SetVisible(_specialTimerText, visible);
    }

    // Purpose: Updates the root state.
    private void SetRoot(GameObject root, bool active)
    {
        UIVisibilityHelper.SetVisible(root, active);
    }
}
