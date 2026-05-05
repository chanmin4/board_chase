using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UpgradeNodeButtonUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Refs")]
    [Tooltip("Button on this upgrade node. Used to purchase the upgrade when the node is available.")]
    [SerializeField] private Button _button;

    [Tooltip("Icon image that represents this upgrade node.")]
    [SerializeField] private Image _iconImage;

    [Tooltip("CanvasGroup on this node root. Used to control node opacity by purchase/lock state.")]
    [SerializeField] private CanvasGroup _nodeCanvasGroup;

    [Header("Optional State Visuals")]
    [Tooltip("Optional object shown when this upgrade has already been purchased, such as a check mark.")]
    [SerializeField] private GameObject _purchasedOverlay;

    [Tooltip("Optional object shown when this upgrade is locked, such as a lock icon or dark overlay.")]
    [SerializeField] private GameObject _lockedOverlay;

    [Tooltip("Optional frame shown when this upgrade is currently available to purchase.")]
    [SerializeField] private GameObject _availableFrame;

    [Header("State Colors")]
    [Tooltip("Icon color used when this upgrade is available to purchase.")]
    [SerializeField] private Color _availableColor = Color.white;

    [Tooltip("Icon color used when this upgrade is locked.")]
    [SerializeField] private Color _lockedColor = new Color(0.45f, 0.45f, 0.45f, 1f);

    [Tooltip("Icon color used when this upgrade has already been purchased.")]
    [SerializeField] private Color _purchasedColor = Color.white;

    [Header("State Opacity")]
    [Tooltip("Node opacity used when this upgrade has already been purchased.")]
    [SerializeField, Range(0f, 1f)] private float _purchasedAlpha = 1f;

    [Tooltip("Node opacity used when this upgrade is available to purchase.")]
    [SerializeField, Range(0f, 1f)] private float _availableAlpha = 1f;

    [Tooltip("Node opacity used when this upgrade is locked.")]
    [SerializeField, Range(0f, 1f)] private float _lockedAlpha = 0.35f;

    private PlayerUpgradeNodeViewData _data;
    private Action<PlayerUpgradeTrack, int> _clicked;
    private Func<PlayerUpgradeTooltipUI> _tooltipGetter;
    private PlayerUpgradeTooltipUI _tooltip;

    private void Reset()
    {
        if (_button == null)
            _button = GetComponent<Button>();

        if (_iconImage == null)
            _iconImage = GetComponent<Image>();

        if (_nodeCanvasGroup == null)
            _nodeCanvasGroup = GetComponent<CanvasGroup>();
    }

    private void Awake()
    {
        if (_button == null)
            _button = GetComponent<Button>();

        if (_iconImage == null)
            _iconImage = GetComponent<Image>();

        if (_nodeCanvasGroup == null)
            _nodeCanvasGroup = GetComponent<CanvasGroup>();

        if (_nodeCanvasGroup == null)
            _nodeCanvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    private void OnEnable()
    {
        if (_button != null)
            _button.onClick.AddListener(OnClicked);
    }

    private void OnDisable()
    {
        if (_button != null)
            _button.onClick.RemoveListener(OnClicked);

        _tooltip?.Hide();
    }

    public void Bind(
        PlayerUpgradeNodeViewData data,
        Action<PlayerUpgradeTrack, int> clicked,
        Func<PlayerUpgradeTooltipUI> tooltipGetter)
    {
        _data = data;
        _clicked = clicked;
        _tooltipGetter = tooltipGetter;

        RefreshIcon(data);
        RefreshOptionalVisuals(data.state);
        ApplyVisualState(data.state);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        PlayerUpgradeTooltipUI tooltip = _tooltipGetter?.Invoke();
        if (tooltip == null)
            return;

        RectTransform rect = transform as RectTransform;
        Vector3[] corners = new Vector3[4];
        rect.GetWorldCorners(corners);

        Vector2 topRight = corners[2];
        tooltip.Show(_data, topRight);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        PlayerUpgradeTooltipUI tooltip = _tooltipGetter?.Invoke();
        if (tooltip == null)
            return;

        tooltip.Hide();
    }
    private void OnClicked()
    {
        if (!_data.canPurchase)
            return;

        _clicked?.Invoke(_data.track, _data.level);
    }

    private void RefreshIcon(PlayerUpgradeNodeViewData data)
    {
        if (_iconImage == null)
            return;

        _iconImage.sprite = data.icon;
        _iconImage.enabled = data.icon != null;
        _iconImage.color = ResolveColor(data.state);
    }

    private void RefreshOptionalVisuals(PlayerUpgradeNodeState state)
    {
        if (_purchasedOverlay != null)
            _purchasedOverlay.SetActive(state == PlayerUpgradeNodeState.Purchased);

        if (_lockedOverlay != null)
        {
            bool locked =
                state == PlayerUpgradeNodeState.LockedByPreviousLevel ||
                state == PlayerUpgradeNodeState.LockedByPoints ||
                state == PlayerUpgradeNodeState.LockedInBeta;

            _lockedOverlay.SetActive(locked);
        }

        if (_availableFrame != null)
            _availableFrame.SetActive(state == PlayerUpgradeNodeState.Available);
    }

    private void ApplyVisualState(PlayerUpgradeNodeState state)
    {
        if (_nodeCanvasGroup != null)
        {
            _nodeCanvasGroup.alpha = ResolveAlpha(state);
            _nodeCanvasGroup.interactable = state == PlayerUpgradeNodeState.Available;
            _nodeCanvasGroup.blocksRaycasts = true;
        }

        if (_button != null)
            _button.interactable = state == PlayerUpgradeNodeState.Available;
    }

    private Color ResolveColor(PlayerUpgradeNodeState state)
    {
        return state switch
        {
            PlayerUpgradeNodeState.Purchased => _purchasedColor,
            PlayerUpgradeNodeState.Available => _availableColor,
            _ => _lockedColor
        };
    }

    private float ResolveAlpha(PlayerUpgradeNodeState state)
    {
        return state switch
        {
            PlayerUpgradeNodeState.Purchased => _purchasedAlpha,
            PlayerUpgradeNodeState.Available => _availableAlpha,
            _ => _lockedAlpha
        };
    }
}
