using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class MiniMapCellUI : MonoBehaviour
{
    [Header("Visibility")]
    [SerializeField] private CanvasGroup _cellCanvasGroup;

    [Header("Parts")]
    [SerializeField] private Image _background;
    [SerializeField] private Image _icon;
    [SerializeField] private TextMeshProUGUI _occupancyRatioText;
    [SerializeField] private TextMeshProUGUI _sectorJudgeTimeText;


    private void Reset()
    {
        _cellCanvasGroup = GetComponent<CanvasGroup>();
    }

    private void Awake()
    {
        _cellCanvasGroup = UIVisibilityHelper.EnsureCanvasGroup(gameObject);

        UIVisibilityHelper.ForceActive(_background);
        UIVisibilityHelper.ForceActive(_icon);
        UIVisibilityHelper.ForceActive(_occupancyRatioText);
        UIVisibilityHelper.ForceActive(_sectorJudgeTimeText);
    }

    public void SetMissing()
    {
        SetVisible(false);

        ClearIcon();
        ClearRatio();
        ClearJudgeTime();
    }

    public void SetLocked(Color backgroundColor, Sprite lockedIcon)
    {
        SetVisible(true);

        SetBackground(backgroundColor);
        SetIcon(lockedIcon);
        ClearRatio();
        ClearJudgeTime();
    }

    public void SetOpened(Color backgroundColor,Sprite iconSprite,string ratioText,bool showIcon,
    bool showRatio,string judgeTimeText,bool showJudgeTime)
    {
        SetVisible(true);

        SetBackground(backgroundColor);

        if (showIcon)
            SetIcon(iconSprite);
        else
            ClearIcon();

        if (showRatio)
            SetRatio(ratioText);
        else
            ClearRatio();
        if (showJudgeTime)
            SetJudgeTime(judgeTimeText);
        else
            ClearJudgeTime();
    }

    private void SetVisible(bool visible)
    {
        UIVisibilityHelper.SetVisible(_cellCanvasGroup, visible);
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

    private void SetRatio(string text)
    {
        if (_occupancyRatioText == null)
            return;

        _occupancyRatioText.text = text;
        UIVisibilityHelper.SetVisible(_occupancyRatioText, true);
    }

    private void ClearRatio()
    {
        if (_occupancyRatioText == null)
            return;

        _occupancyRatioText.text = string.Empty;
        UIVisibilityHelper.SetVisible(_occupancyRatioText, false);
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
    private void SetRoot(GameObject root, bool active)
    {
        UIVisibilityHelper.SetVisible(root, active);
    }
}
