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

    [FormerlySerializedAs("_occupancyRatioText")]
    [SerializeField] private TextMeshProUGUI _playerOccupancyRatioText;

    [SerializeField] private TextMeshProUGUI _virusOccupancyRatioText;

    [SerializeField] private TextMeshProUGUI _sectorJudgeTimeText;
    [SerializeField] private TextMeshProUGUI _specialTimerText;

    private void Reset()
    {
        _cellCanvasGroup = GetComponent<CanvasGroup>();
    }

    private void Awake()
    {
        _cellCanvasGroup = UIVisibilityHelper.EnsureCanvasGroup(gameObject);

        UIVisibilityHelper.ForceActive(_background);
        UIVisibilityHelper.ForceActive(_icon);
        UIVisibilityHelper.ForceActive(_playerOccupancyRatioText);
        UIVisibilityHelper.ForceActive(_virusOccupancyRatioText);
        UIVisibilityHelper.ForceActive(_sectorJudgeTimeText);
        UIVisibilityHelper.ForceActive(_specialTimerText);
    }

    public void SetMissing()
    {
        SetVisible(false);
        SetSpecialTimer(string.Empty, false);
        ClearIcon();
        ClearRatios();
        ClearJudgeTime();
    }

    public void SetLocked(Color backgroundColor, Sprite lockedIcon)
    {
        SetVisible(true);
        SetSpecialTimer(string.Empty, false);
        SetBackground(backgroundColor);
        SetIcon(lockedIcon);
        ClearRatios();
        ClearJudgeTime();
    }

    public void SetStartSector(Color backgroundColor, float visibleAlpha = 1f)
    {
        SetVisible(true, visibleAlpha);
        SetSpecialTimer(string.Empty, false);
        SetBackground(backgroundColor);
        ClearIcon();
        ClearRatios();
        ClearJudgeTime();
    }

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

    private void SetVisible(bool visible)
    {
        SetVisible(visible, 1f);
    }

    private void SetVisible(bool visible, float visibleAlpha)
    {
        UIVisibilityHelper.SetVisible(_cellCanvasGroup, visible, visibleAlpha);
    }

    private void SetBackground(Color color)
    {
        if (_background == null)
            return;

        _background.color = color;
        UIVisibilityHelper.SetVisible(_background, true);
    }

    private void SetIcon(Sprite sprite)
    {
        if (_icon == null)
            return;

        _icon.sprite = sprite;
        UIVisibilityHelper.SetVisible(_icon, sprite != null);
    }

    private void ClearIcon()
    {
        if (_icon == null)
            return;

        _icon.sprite = null;
        UIVisibilityHelper.SetVisible(_icon, false);
    }

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

    private void SetJudgeTime(string text)
    {
        if (_sectorJudgeTimeText == null)
            return;

        _sectorJudgeTimeText.text = text;
        UIVisibilityHelper.SetVisible(_sectorJudgeTimeText, true);
    }

    private void ClearJudgeTime()
    {
        if (_sectorJudgeTimeText == null)
            return;

        _sectorJudgeTimeText.text = string.Empty;
        UIVisibilityHelper.SetVisible(_sectorJudgeTimeText, false);
    }

    public void SetSpecialTimer(string text, bool visible)
    {
        if (_specialTimerText == null)
            return;

        _specialTimerText.text = visible ? text : string.Empty;
        UIVisibilityHelper.SetVisible(_specialTimerText, visible);
    }

    private void SetRoot(GameObject root, bool active)
    {
        UIVisibilityHelper.SetVisible(root, active);
    }
}
