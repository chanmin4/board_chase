using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class AmmoSlotSellPopupUI : MonoBehaviour
{
    [Header("Visibility")]
    [SerializeField] private CanvasGroup _canvasGroup;

    [Header("Texts")]
    [SerializeField] private TextMeshProUGUI _titleText;
    [SerializeField] private TextMeshProUGUI _amountText;
    [SerializeField] private TextMeshProUGUI _gainText;

    [Header("Controls")]
    [SerializeField] private Slider _amountSlider;
    [SerializeField] private Button _confirmButton;
    [SerializeField] private Button _cancelButton;

    [Header("Events")]
    [SerializeField] private WeaponAmmoSellPopupRequestEventChannelSO _popupRequestChannel;
    [SerializeField] private WeaponAmmoSellConfirmRequestEventChannelSO _sellConfirmRequestChannel;

    private WeaponAmmoSlotSnapshot _slot;
    private int _amount;

    private void Awake()
    {
        EnsureCanvasGroup();
        SetVisible(false);
    }

    private void OnEnable()
    {
        if (_popupRequestChannel != null)
            _popupRequestChannel.OnEventRaised += Show;

        if (_amountSlider != null)
            _amountSlider.onValueChanged.AddListener(HandleSliderChanged);

        if (_confirmButton != null)
            _confirmButton.onClick.AddListener(Confirm);

        if (_cancelButton != null)
            _cancelButton.onClick.AddListener(Cancel);
    }

    private void OnDisable()
    {
        if (_popupRequestChannel != null)
            _popupRequestChannel.OnEventRaised -= Show;

        if (_amountSlider != null)
            _amountSlider.onValueChanged.RemoveListener(HandleSliderChanged);

        if (_confirmButton != null)
            _confirmButton.onClick.RemoveListener(Confirm);

        if (_cancelButton != null)
            _cancelButton.onClick.RemoveListener(Cancel);
    }

    private void Show(WeaponAmmoSellPopupRequest request)
    {
        _slot = request.slot;

        if (!_slot.canSell)
            return;

        int maxAmount = Mathf.Max(1, _slot.totalAmmo);
        _amount = maxAmount;

        if (_titleText != null)
            _titleText.text = $"Sell {_slot.displayName}";

        if (_amountSlider != null)
        {
            _amountSlider.minValue = 1;
            _amountSlider.maxValue = maxAmount;
            _amountSlider.wholeNumbers = true;
            _amountSlider.SetValueWithoutNotify(_amount);
        }

        RefreshTexts();
        SetVisible(true);
    }

    private void HandleSliderChanged(float value)
    {
        _amount = Mathf.RoundToInt(value);
        RefreshTexts();
    }

    private void RefreshTexts()
    {
        if (_amountText != null)
            _amountText.text = _amount.ToString();

        if (_gainText != null)
            _gainText.text = (_amount * Mathf.Max(0, _slot.sellPricePerAmmo)).ToString();
    }

    private void Confirm()
    {
        if (_sellConfirmRequestChannel != null)
            _sellConfirmRequestChannel.RaiseEvent(
                new WeaponAmmoSellConfirmRequest(_slot.slotIndex, _amount));

        SetVisible(false);
    }

    private void Cancel()
    {
        SetVisible(false);
    }

    private void SetVisible(bool visible)
    {
        EnsureCanvasGroup();

        _canvasGroup.alpha = visible ? 1f : 0f;
        _canvasGroup.interactable = visible;
        _canvasGroup.blocksRaycasts = visible;
    }

    private void EnsureCanvasGroup()
    {
        if (_canvasGroup != null)
            return;

        _canvasGroup = GetComponent<CanvasGroup>();

        if (_canvasGroup == null)
            _canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }
}