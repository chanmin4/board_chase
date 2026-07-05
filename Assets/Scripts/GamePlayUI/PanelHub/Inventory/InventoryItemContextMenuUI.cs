using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.UI;

public enum InventoryContextMenuTargetKind
{
    None = 0,
    InventoryItem = 1,
    EquipmentSlot = 2,
    AmmoSlot = 3
}

public enum InventoryContextMenuActionKind
{
    EquipInventoryItem = 0,
    UnequipEquipmentSlot = 1,
    UnequipAmmoSlot = 2,
    DiscardInventoryItem = 3,
    DiscardEquipmentSlot = 4,
    DiscardAmmoSlot = 5,
    SplitInventoryItem = 6,
    SplitAmmoSlot = 7
}

[Serializable]
public sealed class InventoryContextMenuActionDefinition
{
    [SerializeField] private bool _enabled = true;
    [SerializeField] private InventoryContextMenuActionKind _action;
    [SerializeField] private LocalizedString _label;
    [SerializeField] private string _fallbackLabel;

    public bool Enabled => _enabled;
    public InventoryContextMenuActionKind Action => _action;

    public string ResolveLabel()
    {
        if (_label != null && !_label.IsEmpty)
        {
            string localized = _label.GetLocalizedString();
            if (!string.IsNullOrWhiteSpace(localized))
                return localized;
        }

        return _fallbackLabel ?? string.Empty;
    }
}

[DisallowMultipleComponent]
public class InventoryItemContextMenuUI : MonoBehaviour
{
    [Header("Visibility")]
    [SerializeField] private CanvasGroup _group;
    [SerializeField] private RectTransform _root;

    [Header("Button Instancing")]
    [SerializeField] private Button _buttonPrefab;
    [SerializeField] private Transform _buttonRoot;
    [SerializeField] private bool _hideButtonPrefabOnAwake = true;

    [Header("Split")]
    [SerializeField] private CanvasGroup _splitGroup;
    [SerializeField] private TextMeshProUGUI _splitTitleText;
    [SerializeField] private string _splitTitleFormat = "{0}";
    [SerializeField] private string _splitAmountFormat = "{0}";
    [SerializeField] private Slider _splitAmountSlider;
    [SerializeField] private TMP_InputField _splitAmountInput;
    [SerializeField] private Button _splitConfirmButton;
    [SerializeField] private Button _splitCancelButton;

    [Header("Inventory Item Actions")]
    [SerializeField] private InventoryContextMenuActionDefinition[] _inventoryItemActions;

    [Header("Equipment Slot Actions")]
    [SerializeField] private InventoryContextMenuActionDefinition[] _equipmentSlotActions;

    [Header("Ammo Slot Actions")]
    [SerializeField] private InventoryContextMenuActionDefinition[] _ammoSlotActions;

    private readonly List<Button> _spawnedButtons = new();

    private IInventoryItemContextMenuOwner _owner;
    private InventoryContextMenuTargetKind _targetKind;
    private ItemSO _currentItem;
    private int _inventoryIndex = -1;
    private int _inventoryAmount;
    private PlayerInventoryEquipmentSlotKind _equipmentSlotKind;
    private int _ammoSlotIndex = -1;
    private int _ammoAmount;

    private int _splitMaxAmount = 1;
    private int _splitCurrentAmount = 1;
    private bool _suppressSplitEvents;

    private void Reset()
    {
        _group = GetComponent<CanvasGroup>();
        _root = transform as RectTransform;
        _buttonRoot = transform;
    }

    private void Awake()
    {
        EnsureRefs();
        HideButtonPrefabIfNeeded();
        Hide();
    }

    private void OnEnable()
    {
        if (_splitAmountSlider != null)
            _splitAmountSlider.onValueChanged.AddListener(HandleSplitSliderChanged);

        if (_splitAmountInput != null)
            _splitAmountInput.onValueChanged.AddListener(HandleSplitInputChanged);

        if (_splitConfirmButton != null)
            _splitConfirmButton.onClick.AddListener(HandleSplitConfirmClicked);

        if (_splitCancelButton != null)
            _splitCancelButton.onClick.AddListener(HideSplitPanel);
    }

    private void OnDisable()
    {
        if (_splitAmountSlider != null)
            _splitAmountSlider.onValueChanged.RemoveListener(HandleSplitSliderChanged);

        if (_splitAmountInput != null)
            _splitAmountInput.onValueChanged.RemoveListener(HandleSplitInputChanged);

        if (_splitConfirmButton != null)
            _splitConfirmButton.onClick.RemoveListener(HandleSplitConfirmClicked);

        if (_splitCancelButton != null)
            _splitCancelButton.onClick.RemoveListener(HideSplitPanel);

        ClearButtons();
    }

    public void Initialize(IInventoryItemContextMenuOwner owner)
    {
        _owner = owner;
    }

    public void ShowInventoryItem(IInventoryItemContextMenuOwner owner, int inventoryIndex, ItemSO item, int amount, Vector2 screenPosition)
    {
        Initialize(owner);
        EnsureRefs();

        _targetKind = InventoryContextMenuTargetKind.InventoryItem;
        _currentItem = item;
        _inventoryIndex = inventoryIndex;
        _inventoryAmount = Mathf.Max(0, amount);
        _equipmentSlotKind = default;
        _ammoSlotIndex = -1;
        _ammoAmount = 0;

        ShowInternal(item, screenPosition, _inventoryItemActions);
    }

    public void ShowEquipmentSlot(IInventoryItemContextMenuOwner owner, PlayerInventoryEquipmentSlotKind slotKind, ItemSO item, Vector2 screenPosition)
    {
        Initialize(owner);
        EnsureRefs();

        _targetKind = InventoryContextMenuTargetKind.EquipmentSlot;
        _currentItem = item;
        _inventoryIndex = -1;
        _inventoryAmount = 0;
        _equipmentSlotKind = slotKind;
        _ammoSlotIndex = -1;
        _ammoAmount = 0;

        ShowInternal(item, screenPosition, _equipmentSlotActions);
    }

    public void ShowAmmoSlot(IInventoryItemContextMenuOwner owner, int ammoSlotIndex, ItemSO item, int amount, Vector2 screenPosition)
    {
        Initialize(owner);
        EnsureRefs();

        _targetKind = InventoryContextMenuTargetKind.AmmoSlot;
        _currentItem = item;
        _inventoryIndex = -1;
        _inventoryAmount = 0;
        _equipmentSlotKind = default;
        _ammoSlotIndex = ammoSlotIndex;
        _ammoAmount = Mathf.Max(0, amount);

        ShowInternal(item, screenPosition, _ammoSlotActions);
    }

    public void Hide()
    {
        _targetKind = InventoryContextMenuTargetKind.None;
        _currentItem = null;
        _inventoryIndex = -1;
        _inventoryAmount = 0;
        _equipmentSlotKind = default;
        _ammoSlotIndex = -1;
        _ammoAmount = 0;

        HideSplitPanel();
        ClearButtons();
        SetVisible(false);
    }

    private void ShowInternal(ItemSO item, Vector2 screenPosition, InventoryContextMenuActionDefinition[] actions)
    {
        HideSplitPanel();
        RebuildButtons(actions, item);

        if (_spawnedButtons.Count <= 0)
        {
            SetVisible(false);
            return;
        }

        SetScreenPosition(screenPosition);
        SetVisible(true);
    }

    private void RebuildButtons(InventoryContextMenuActionDefinition[] actions, ItemSO item)
    {
        ClearButtons();

        if (_buttonPrefab == null || actions == null)
            return;

        for (int i = 0; i < actions.Length; i++)
        {
            InventoryContextMenuActionDefinition definition = actions[i];

            if (definition == null || !definition.Enabled || !CanRunAction(definition.Action, item))
                continue;

            Button button = Instantiate(_buttonPrefab, ResolveButtonRoot());
            button.gameObject.SetActive(true);
            SetButtonLabel(button, definition.ResolveLabel());

            InventoryContextMenuActionKind action = definition.Action;
            button.onClick.AddListener(() => ExecuteAction(action));

            _spawnedButtons.Add(button);
        }
    }

    private bool CanRunAction(InventoryContextMenuActionKind action, ItemSO item)
    {
        return action switch
        {
            InventoryContextMenuActionKind.EquipInventoryItem =>
                _targetKind == InventoryContextMenuTargetKind.InventoryItem &&
                (item is WeaponSO || item is ArmorItemSO || item is BulletSO),

            InventoryContextMenuActionKind.UnequipEquipmentSlot =>
                _targetKind == InventoryContextMenuTargetKind.EquipmentSlot,

            InventoryContextMenuActionKind.UnequipAmmoSlot =>
                _targetKind == InventoryContextMenuTargetKind.AmmoSlot,

            InventoryContextMenuActionKind.DiscardInventoryItem =>
                _targetKind == InventoryContextMenuTargetKind.InventoryItem,

            InventoryContextMenuActionKind.SplitInventoryItem =>
                _targetKind == InventoryContextMenuTargetKind.InventoryItem &&
                item != null &&
                item.MaxStack > 1 &&
                _inventoryAmount > 1,

            InventoryContextMenuActionKind.SplitAmmoSlot =>
                _targetKind == InventoryContextMenuTargetKind.AmmoSlot &&
                item != null &&
                item.MaxStack > 1 &&
                _ammoAmount > 1,

            InventoryContextMenuActionKind.DiscardEquipmentSlot =>
                _targetKind == InventoryContextMenuTargetKind.EquipmentSlot,

            InventoryContextMenuActionKind.DiscardAmmoSlot =>
                _targetKind == InventoryContextMenuTargetKind.AmmoSlot,

            _ => false
        };
    }

    private void ExecuteAction(InventoryContextMenuActionKind action)
    {
        if (_owner == null)
        {
            Hide();
            return;
        }

        switch (action)
        {
            case InventoryContextMenuActionKind.EquipInventoryItem:
                if (_inventoryIndex >= 0)
                    _owner.HandleContextMenuEquipInventoryItem(_inventoryIndex);
                Hide();
                break;

            case InventoryContextMenuActionKind.UnequipEquipmentSlot:
                _owner.HandleContextMenuUnequipEquipmentSlot(_equipmentSlotKind);
                Hide();
                break;

            case InventoryContextMenuActionKind.UnequipAmmoSlot:
                if (_ammoSlotIndex >= 0)
                    _owner.HandleContextMenuUnequipAmmoSlot(_ammoSlotIndex);
                Hide();
                break;

            case InventoryContextMenuActionKind.DiscardInventoryItem:
                if (_inventoryIndex >= 0)
                    _owner.HandleContextMenuDiscardInventoryItem(_inventoryIndex);
                Hide();
                break;

            case InventoryContextMenuActionKind.SplitInventoryItem:
                ShowSplitPanel(_inventoryAmount);
                break;

            case InventoryContextMenuActionKind.SplitAmmoSlot:
                ShowSplitPanel(_ammoAmount);
                break;

            case InventoryContextMenuActionKind.DiscardEquipmentSlot:
                _owner.HandleContextMenuDiscardEquipmentSlot(_equipmentSlotKind);
                Hide();
                break;

            case InventoryContextMenuActionKind.DiscardAmmoSlot:
                if (_ammoSlotIndex >= 0)
                    _owner.HandleContextMenuDiscardAmmoSlot(_ammoSlotIndex);
                Hide();
                break;
        }
    }

    private void ShowSplitPanel(int stackAmount)
    {
        _splitMaxAmount = Mathf.Max(1, stackAmount - 1);
        _splitCurrentAmount = Mathf.Clamp(Mathf.CeilToInt(stackAmount * 0.5f), 1, _splitMaxAmount);

        if (_splitTitleText != null)
        {
            string itemName = InventoryItemDisplayUtility.ResolveItemName(_currentItem);
            _splitTitleText.text = string.Format(_splitTitleFormat, itemName);
        }

        ConfigureSplitSlider();
        RefreshSplitAmountViews();
        SetCanvasGroup(_splitGroup, true);
    }

    private void HideSplitPanel()
    {
        SetCanvasGroup(_splitGroup, false);
    }

    private void ConfigureSplitSlider()
    {
        if (_splitAmountSlider == null)
            return;

        _suppressSplitEvents = true;
        _splitAmountSlider.wholeNumbers = true;
        _splitAmountSlider.minValue = 1;
        _splitAmountSlider.maxValue = Mathf.Max(1, _splitMaxAmount);
        _splitAmountSlider.value = _splitCurrentAmount;
        _suppressSplitEvents = false;
    }

    private void HandleSplitSliderChanged(float value)
    {
        if (_suppressSplitEvents)
            return;

        SetSplitAmount(Mathf.RoundToInt(value));
    }

    private void HandleSplitInputChanged(string value)
    {
        if (_suppressSplitEvents)
            return;

        if (int.TryParse(value, out int parsed))
            SetSplitAmount(parsed);
    }

    private void SetSplitAmount(int amount)
    {
        _splitCurrentAmount = Mathf.Clamp(amount, 1, Mathf.Max(1, _splitMaxAmount));
        RefreshSplitAmountViews();
    }

    private void RefreshSplitAmountViews()
    {
        _suppressSplitEvents = true;

        if (_splitAmountSlider != null)
            _splitAmountSlider.value = _splitCurrentAmount;

        if (_splitAmountInput != null)
            _splitAmountInput.text = _splitCurrentAmount.ToString();
        _suppressSplitEvents = false;
    }

    private void HandleSplitConfirmClicked()
    {
        if (_owner == null)
        {
            Hide();
            return;
        }

        if (_targetKind == InventoryContextMenuTargetKind.InventoryItem && _inventoryIndex >= 0)
            _owner.HandleContextMenuSplitInventoryItem(_inventoryIndex, _splitCurrentAmount);
        else if (_targetKind == InventoryContextMenuTargetKind.AmmoSlot && _ammoSlotIndex >= 0)
            _owner.HandleContextMenuSplitAmmoSlot(_ammoSlotIndex, _splitCurrentAmount);

        Hide();
    }

    private void SetButtonLabel(Button button, string label)
    {
        if (button == null)
            return;

        TextMeshProUGUI labelText = button.GetComponentInChildren<TextMeshProUGUI>(true);

        if (labelText != null)
            labelText.text = label ?? string.Empty;
    }

    private Transform ResolveButtonRoot()
    {
        return _buttonRoot != null ? _buttonRoot : transform;
    }

    private void ClearButtons()
    {
        for (int i = 0; i < _spawnedButtons.Count; i++)
        {
            Button button = _spawnedButtons[i];

            if (button == null)
                continue;

            if (Application.isPlaying)
                Destroy(button.gameObject);
            else
                DestroyImmediate(button.gameObject);
        }

        _spawnedButtons.Clear();
    }

    private void SetScreenPosition(Vector2 screenPosition)
    {
        if (_root == null)
            return;

        RectTransform parent = _root.parent as RectTransform;

        if (parent != null &&
            RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, screenPosition, null, out Vector2 localPoint))
        {
            _root.anchoredPosition = localPoint;
            return;
        }

        _root.position = screenPosition;
    }

    private void SetVisible(bool visible)
    {
        SetCanvasGroup(_group, visible);
    }

    private static void SetCanvasGroup(CanvasGroup group, bool visible)
    {
        if (group == null)
            return;

        group.alpha = visible ? 1f : 0f;
        group.interactable = visible;
        group.blocksRaycasts = visible;
    }

    private void EnsureRefs()
    {
        if (_group == null)
            _group = GetComponent<CanvasGroup>();

        if (_group == null)
            _group = gameObject.AddComponent<CanvasGroup>();

        if (_root == null)
            _root = transform as RectTransform;

        if (_buttonRoot == null)
            _buttonRoot = transform;
    }

    private void HideButtonPrefabIfNeeded()
    {
        if (_hideButtonPrefabOnAwake && _buttonPrefab != null)
            _buttonPrefab.gameObject.SetActive(false);
    }
}