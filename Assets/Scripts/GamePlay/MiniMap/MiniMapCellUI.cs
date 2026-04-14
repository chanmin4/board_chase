using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MiniMapCellUI : MonoBehaviour
{
    [Header("Roots")]
    [Tooltip("Root object to show when the cell is opened  (SetActive)")]
    [SerializeField] private GameObject _openedRoot;
    [SerializeField] private GameObject _lockedRoot;
    [Tooltip("the cell where player can't go and can't see this cell")]
    [SerializeField] private GameObject _missingRoot;

    [Header("Visuals")]
    [SerializeField] private Image _background;
    [SerializeField] private Image _icon;
    [SerializeField] private TextMeshProUGUI _occupancyRatioText;

    [Header("Options")]
    [SerializeField] private bool _hideMissingCell = true;

    public void SetMissing()
    {
        gameObject.SetActive(!_hideMissingCell);

        SetRoot(_openedRoot, false);
        SetRoot(_lockedRoot, false);
        SetRoot(_missingRoot, true);

        if (_background != null)
            _background.gameObject.SetActive(false);

        if (_icon != null)
            _icon.gameObject.SetActive(false);

        if (_occupancyRatioText != null)
            _occupancyRatioText.gameObject.SetActive(false);
    }

    public void SetLocked(Color backgroundColor, Sprite lockedIcon)
    {
        gameObject.SetActive(true);

        SetRoot(_openedRoot, false);
        SetRoot(_lockedRoot, true);
        SetRoot(_missingRoot, false);

        if (_background != null)
        {
            _background.gameObject.SetActive(true);
            _background.color = backgroundColor;
        }

        if (_icon != null)
        {
            _icon.gameObject.SetActive(lockedIcon != null);
            _icon.sprite = lockedIcon;
        }

        if (_occupancyRatioText != null)
            _occupancyRatioText.gameObject.SetActive(false);
    }

    public void SetOpened(Color backgroundColor, Sprite iconSprite, string ratioText, bool showIcon, bool showRatio)
    {
        gameObject.SetActive(true);

        SetRoot(_openedRoot, true);
        SetRoot(_lockedRoot, false);
        SetRoot(_missingRoot, false);

        if (_background != null)
        {
            _background.gameObject.SetActive(true);
            _background.color = backgroundColor;
        }

        if (_icon != null)
        {
            _icon.gameObject.SetActive(showIcon && iconSprite != null);
            _icon.sprite = iconSprite;
        }

        if (_occupancyRatioText != null)
        {
            _occupancyRatioText.gameObject.SetActive(showRatio);
            _occupancyRatioText.text = ratioText;
        }
    }

    private void SetRoot(GameObject target, bool active)
    {
        if (target != null)
            target.SetActive(active);
    }
}
