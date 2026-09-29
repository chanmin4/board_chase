/*
 * Role hierarchy: Presentation > UI > Gameplay > PanelHub > Inventory
 * Primary responsibility: Manages InventoryContextMenuTargetKind item storage, equipment, transfer state, and related rules.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
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
    SplitAmmoSlot = 7,
    UseInventoryItem = 8
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
    public LocalizedString Label => _label;
    public string FallbackLabel => _fallbackLabel ?? string.Empty;

    // Purpose: Resolves the label needed by this component.
    public string ResolveLabel()
    {
        return FallbackLabel;
    }
}

[DisallowMultipleComponent]
public class InventoryItemContextMenuUI : MonoBehaviour
{
    // The split popup is shared by player, loot, and shop context menus.
    // Only the menu that opened it may consume its confirm/cancel events.
    private static InventoryItemContextMenuUI _activeSplitPanelOwner;

    [Header("Visibility")]
    [SerializeField] private CanvasGroup _group;
    [SerializeField] private RectTransform _root;

    [Header("Button Instancing")]
    [SerializeField] private Button _buttonPrefab;
    [SerializeField] private Transform _buttonRoot;
    [SerializeField] private bool _hideButtonPrefabOnAwake = true;

    [Header("Split")]
    // Shared popup references are located by their authored hierarchy names.
    // Keep them serialized for play-mode inspection but out of the prefab
    // inspector; assigning scene popup instances here would be invalid.
    [SerializeField, HideInInspector] private CanvasGroup _splitGroup;
    [SerializeField, HideInInspector] private TextMeshProUGUI _splitTitleText;
    [SerializeField] private string _splitTitleFormat = "{0}";
    [SerializeField, HideInInspector] private Slider _splitAmountSlider;
    [SerializeField, HideInInspector] private TMP_InputField _splitAmountInput;
    [SerializeField, HideInInspector] private Button _splitConfirmButton;
    [SerializeField, HideInInspector] private Button _splitCancelButton;

    [Header("Discard Confirm")]
    [SerializeField, HideInInspector] private CanvasGroup _discardConfirmGroup;
    [SerializeField, HideInInspector] private TextMeshProUGUI _discardConfirmText;
    [SerializeField] private LocalizedString _discardConfirmLabel;
    [SerializeField] private string _discardConfirmFallbackLabel = "버리겠습니까?";
    [SerializeField, HideInInspector] private Button _discardConfirmYesButton;
    [SerializeField, HideInInspector] private Button _discardConfirmNoButton;

    [Header("Position")]
    [SerializeField] private Vector2 _screenOffset = new Vector2(12f, -8f);
    [SerializeField] private bool _keepInsideParent = true;
    [SerializeField] private Vector2 _screenPadding = new Vector2(8f, 8f);

    [Header("Inventory Item Actions")]
    [SerializeField] private InventoryContextMenuActionDefinition[] _inventoryItemActions;

    [Header("Equipment Slot Actions")]
    [SerializeField] private InventoryContextMenuActionDefinition[] _equipmentSlotActions;

    [Header("Ammo Slot Actions")]
    [SerializeField] private InventoryContextMenuActionDefinition[] _ammoSlotActions;

    [Header("Debug")]
    [SerializeField] private bool _debugLogs = true;

    private readonly List<Button> _spawnedButtons = new();
    private readonly List<LocalizedButtonBinding> _localizedButtonBindings = new();
    private readonly List<RaycastResult> _raycastResults = new();

    private Canvas _overlayCanvas;
    private GraphicRaycaster _overlayRaycaster;
    private Canvas _splitOverlayCanvas;
    private GraphicRaycaster _splitOverlayRaycaster;

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
    private int _splitSourceAmount = 1;
    private bool _suppressSplitEvents;
    private bool _hasPendingConfirmAction;
    private InventoryContextMenuActionKind _pendingConfirmAction;
    private LocalizedString.ChangeHandler _discardConfirmLabelChanged;

    private sealed class LocalizedButtonBinding
    {
        private readonly LocalizedString _localizedString;
        private readonly LocalizedString.ChangeHandler _handler;

        public LocalizedButtonBinding(
            LocalizedString localizedString,
            LocalizedString.ChangeHandler handler)
        {
            _localizedString = localizedString;
            _handler = handler;
        }

        // Purpose: Releases the owned release resource.
        public void Release()
        {
            if (_localizedString != null && _handler != null)
                _localizedString.StringChanged -= _handler;
        }
    }

    // Purpose: Initializes Inspector defaults for this component.
    private void Reset()
    {
        _group = GetComponent<CanvasGroup>();
        _root = transform as RectTransform;
        _buttonRoot = transform;
    }

    // Purpose: Caches the component references required before runtime initialization.
    private void Awake()
    {
        EnsureRefs();
        HideButtonPrefabIfNeeded();
        Hide();
    }

    // Purpose: Activates runtime subscriptions and refreshes the enabled state.
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

        if (_discardConfirmYesButton != null)
            _discardConfirmYesButton.onClick.AddListener(HandleDiscardConfirmYesClicked);

        if (_discardConfirmNoButton != null)
            _discardConfirmNoButton.onClick.AddListener(HandleDiscardConfirmNoClicked);
    }

    // Purpose: Releases active subscriptions and transient state while disabled.
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

        if (_discardConfirmYesButton != null)
            _discardConfirmYesButton.onClick.RemoveListener(HandleDiscardConfirmYesClicked);

        if (_discardConfirmNoButton != null)
            _discardConfirmNoButton.onClick.RemoveListener(HandleDiscardConfirmNoClicked);

        UnbindDiscardConfirmLabel();
        if (_activeSplitPanelOwner == this)
            HideSplitPanel();
        ClearButtons();
    }

    // Purpose: Processes this component's per-frame runtime work.
    private void Update()
    {
        if (!IsVisible())
            return;

        if (!TryGetDismissPointerDown(out Vector2 screenPosition))
            return;

        // A Button receives its click slightly later in the EventSystem update.
        // Keep the menu alive when the pointer is inside its actual rect even if
        // another canvas incorrectly wins a RaycastAll query for that frame.
        if (IsPointerOverMenu(screenPosition) || IsPointerInsideMenuRect(screenPosition))
            return;

        Hide();
    }

    // Purpose: Initializes initialize for use by this component.
    public void Initialize(IInventoryItemContextMenuOwner owner)
    {
        _owner = owner;
    }

    // Purpose: Shows the inventory item UI or visual state.
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

    // Purpose: Shows the equipment slot UI or visual state.
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

    // Purpose: Shows the ammo slot UI or visual state.
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

    // Purpose: Hides the hide UI or visual state.
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
        HideDiscardConfirmPanel();
        ClearButtons();
        SetVisible(false);
    }

    // Purpose: Shows the internal UI or visual state.
    private void ShowInternal(ItemSO item, Vector2 screenPosition, InventoryContextMenuActionDefinition[] actions)
    {
        HideSplitPanel();
        HideDiscardConfirmPanel();
        RebuildButtons(actions, item);

        if (_spawnedButtons.Count <= 0)
        {
            if (_debugLogs)
            {
                Debug.Log(
                    $"[InventoryItemContextMenuUI] No buttons created. target={_targetKind}, item={InventoryItemDisplayUtility.ResolveItemName(item)}, actionsNull={actions == null}, buttonPrefabNull={_buttonPrefab == null}",
                    this);
            }

            SetVisible(false);
            return;
        }

        if (_debugLogs)
        {
            Debug.Log(
                $"[InventoryItemContextMenuUI] Show. target={_targetKind}, item={InventoryItemDisplayUtility.ResolveItemName(item)}, buttons={_spawnedButtons.Count}",
                this);
        }

        EnsureOverlayInputLayer();
        BringOverlayToFront();
        SetScreenPosition(screenPosition);
        SetVisible(true);
    }

    // Purpose: Repairs or rebuilds buttons from the current source data.
    private void RebuildButtons(InventoryContextMenuActionDefinition[] actions, ItemSO item)
    {
        ClearButtons();

        if (_buttonPrefab == null || actions == null)
        {
            if (_debugLogs)
            {
                Debug.Log(
                    $"[InventoryItemContextMenuUI] Rebuild skipped. buttonPrefabNull={_buttonPrefab == null}, actionsNull={actions == null}, target={_targetKind}",
                    this);
            }

            return;
        }

        for (int i = 0; i < actions.Length; i++)
        {
            InventoryContextMenuActionDefinition definition = actions[i];

            if (definition == null || !definition.Enabled || !CanRunAction(definition.Action, item))
                continue;

            Button button = Instantiate(_buttonPrefab, ResolveButtonRoot());
            button.gameObject.SetActive(true);
            BindButtonLabel(button, definition);

            InventoryContextMenuActionKind action = definition.Action;
            button.onClick.AddListener(() => ExecuteAction(action));

            _spawnedButtons.Add(button);
        }
    }

    // Purpose: Checks whether run action is currently permitted.
    private bool CanRunAction(InventoryContextMenuActionKind action, ItemSO item)
    {
        return action switch
        {
            InventoryContextMenuActionKind.EquipInventoryItem =>
                _targetKind == InventoryContextMenuTargetKind.InventoryItem &&
                (item is WeaponSO || item is ArmorItemSO),

            InventoryContextMenuActionKind.UseInventoryItem =>
                _targetKind == InventoryContextMenuTargetKind.InventoryItem &&
                item is ConsumableItemSO &&
                _owner is IConsumableInventoryContextMenuOwner,

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
                _ammoAmount > 1,

            InventoryContextMenuActionKind.DiscardEquipmentSlot =>
                _targetKind == InventoryContextMenuTargetKind.EquipmentSlot,

            InventoryContextMenuActionKind.DiscardAmmoSlot =>
                _targetKind == InventoryContextMenuTargetKind.AmmoSlot,

            _ => false
        };
    }

    // Purpose: Performs the execute action workflow for this component.
    private void ExecuteAction(InventoryContextMenuActionKind action)
    {
        if (_debugLogs)
        {
            Debug.Log(
                $"[{nameof(InventoryItemContextMenuUI)}] Action clicked. " +
                $"action={action}, target={_targetKind}, item={InventoryItemDisplayUtility.ResolveItemName(_currentItem)}.",
                this);
        }

        if (_owner == null)
        {
            Hide();
            return;
        }

        switch (action)
        {
            case InventoryContextMenuActionKind.UseInventoryItem:
                if (_inventoryIndex >= 0 && _owner is IConsumableInventoryContextMenuOwner consumableOwner)
                    consumableOwner.HandleContextMenuUseInventoryItem(_inventoryIndex);
                Hide();
                break;

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
                RequestDiscardConfirmation(action);
                break;

            case InventoryContextMenuActionKind.SplitInventoryItem:
                if (HasSplitAmountPanel())
                    ShowSplitPanel(_inventoryAmount);
                else
                    ExecuteDefaultSplit(_inventoryAmount);
                break;

            case InventoryContextMenuActionKind.SplitAmmoSlot:
                if (HasSplitAmountPanel())
                    ShowSplitPanel(_ammoAmount);
                else
                    ExecuteDefaultSplit(_ammoAmount);
                break;

            case InventoryContextMenuActionKind.DiscardEquipmentSlot:
                RequestDiscardConfirmation(action);
                break;

            case InventoryContextMenuActionKind.DiscardAmmoSlot:
                RequestDiscardConfirmation(action);
                break;
        }
    }

    // Purpose: Checks whether this component has split amount panel.
    private bool HasSplitAmountPanel()
    {
        EnsureSplitPanelRefs();
        EnsureDiscardConfirmRefs();
        return _splitGroup != null &&
               _splitConfirmButton != null &&
               (_splitAmountSlider != null || _splitAmountInput != null);
    }

    // Purpose: Performs the execute default split workflow for this component.
    private void ExecuteDefaultSplit(int stackAmount)
    {
        if (_owner == null || stackAmount <= 1)
        {
            Hide();
            return;
        }

        int amount = Mathf.Clamp(
            Mathf.CeilToInt(stackAmount * 0.5f),
            1,
            stackAmount - 1);

        if (_targetKind == InventoryContextMenuTargetKind.InventoryItem && _inventoryIndex >= 0)
            _owner.HandleContextMenuSplitInventoryItem(_inventoryIndex, amount);
        else if (_targetKind == InventoryContextMenuTargetKind.AmmoSlot && _ammoSlotIndex >= 0)
            _owner.HandleContextMenuSplitAmmoSlot(_ammoSlotIndex, amount);

        Hide();
    }

    // Purpose: Requests discard confirmation from the owning service.
    private void RequestDiscardConfirmation(InventoryContextMenuActionKind action)
    {
        EnsureDiscardConfirmRefs();

        if (_discardConfirmGroup == null ||
            _discardConfirmYesButton == null ||
            _discardConfirmNoButton == null)
        {
            ExecuteConfirmedDiscard(action);
            return;
        }

        _hasPendingConfirmAction = true;
        _pendingConfirmAction = action;

        BindDiscardConfirmLabel();

        SetCanvasGroup(_discardConfirmGroup, true);
    }

    // Purpose: Performs the execute confirmed discard workflow for this component.
    private void ExecuteConfirmedDiscard(InventoryContextMenuActionKind action)
    {
        if (_owner == null)
        {
            Hide();
            return;
        }

        switch (action)
        {
            case InventoryContextMenuActionKind.DiscardInventoryItem:
                if (_inventoryIndex >= 0)
                    _owner.HandleContextMenuDiscardInventoryItem(_inventoryIndex);
                break;

            case InventoryContextMenuActionKind.DiscardEquipmentSlot:
                _owner.HandleContextMenuDiscardEquipmentSlot(_equipmentSlotKind);
                break;

            case InventoryContextMenuActionKind.DiscardAmmoSlot:
                if (_ammoSlotIndex >= 0)
                    _owner.HandleContextMenuDiscardAmmoSlot(_ammoSlotIndex);
                break;
        }

        Hide();
    }

    // Purpose: Handles the incoming discard confirm yes clicked request or event.
    private void HandleDiscardConfirmYesClicked()
    {
        if (!_hasPendingConfirmAction)
        {
            HideDiscardConfirmPanel();
            return;
        }

        ExecuteConfirmedDiscard(_pendingConfirmAction);
    }

    // Purpose: Handles the incoming discard confirm no clicked request or event.
    private void HandleDiscardConfirmNoClicked()
    {
        HideDiscardConfirmPanel();
    }

    // Purpose: Hides the discard confirm panel UI or visual state.
    private void HideDiscardConfirmPanel()
    {
        _hasPendingConfirmAction = false;
        _pendingConfirmAction = default;
        UnbindDiscardConfirmLabel();
        SetCanvasGroup(_discardConfirmGroup, false);
    }

    // Purpose: Resolves the discard confirm fallback label needed by this component.
    private string ResolveDiscardConfirmFallbackLabel()
    {
        return _discardConfirmFallbackLabel;
    }

    // Purpose: Binds discard confirm label to the active runtime or UI target.
    private void BindDiscardConfirmLabel()
    {
        UnbindDiscardConfirmLabel();

        if (_discardConfirmText == null)
            return;

        _discardConfirmText.text = ResolveDiscardConfirmFallbackLabel();

        if (_discardConfirmLabel == null || _discardConfirmLabel.IsEmpty)
            return;

        _discardConfirmLabelChanged = HandleDiscardConfirmLabelChanged;
        _discardConfirmLabel.StringChanged += _discardConfirmLabelChanged;
        _discardConfirmLabel.RefreshString();
    }

    // Purpose: Removes the discard confirm label binding.
    private void UnbindDiscardConfirmLabel()
    {
        if (_discardConfirmLabel != null && _discardConfirmLabelChanged != null)
            _discardConfirmLabel.StringChanged -= _discardConfirmLabelChanged;

        _discardConfirmLabelChanged = null;
    }

    // Purpose: Handles the incoming discard confirm label changed request or event.
    private void HandleDiscardConfirmLabelChanged(string value)
    {
        if (_discardConfirmText != null && !string.IsNullOrWhiteSpace(value))
            _discardConfirmText.text = value;
    }

    // Purpose: Shows the split panel UI or visual state.
    private void ShowSplitPanel(int stackAmount)
    {
        EnsureSplitPanelRefs();

        if (_splitGroup == null)
        {
            Debug.LogError(
                $"[{nameof(InventoryItemContextMenuUI)}] Cannot open split popup " +
                "because its required references were not resolved.",
                this);
            return;
        }

        if (_activeSplitPanelOwner != null &&
            _activeSplitPanelOwner != this)
        {
            _activeSplitPanelOwner.HideSplitPanel();
        }

        _activeSplitPanelOwner = this;

        // The split dialog starts at the full source stack (the right edge).
        // The player then drags left to choose the amount moved into a new slot.
        _splitSourceAmount = Mathf.Max(1, stackAmount);
        _splitMaxAmount = _splitSourceAmount;
        _splitCurrentAmount = _splitMaxAmount;

        if (_splitTitleText != null)
        {
            string itemName = InventoryItemDisplayUtility.ResolveItemName(_currentItem);
            _splitTitleText.text = string.Format(_splitTitleFormat, itemName);
        }

        _splitGroup.transform.SetAsLastSibling();
        EnsureSplitPanelInputLayer();
        SetCanvasGroup(_splitGroup, true);

        // Apply the runtime range after the dialog becomes visible so the
        // handle visibly starts at the selected full-stack value.
        ConfigureSplitSlider();
        RefreshSplitAmountViews();

        if (_debugLogs)
        {
            Debug.Log(
                $"[{nameof(InventoryItemContextMenuUI)}] Split popup opened. " +
                $"item={InventoryItemDisplayUtility.ResolveItemName(_currentItem)}, " +
                $"stack={_splitSourceAmount}, selected={_splitCurrentAmount}, max={_splitMaxAmount}.",
                this);
        }
    }

    // Purpose: Hides the split panel UI or visual state.
    private void HideSplitPanel()
    {
        if (_activeSplitPanelOwner != null &&
            _activeSplitPanelOwner != this)
        {
            return;
        }

        SetCanvasGroup(_splitGroup, false);

        if (_activeSplitPanelOwner == this)
            _activeSplitPanelOwner = null;
    }

    // Purpose: Configures this component''s split slider.
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

    // Purpose: Handles the incoming split slider changed request or event.
    private void HandleSplitSliderChanged(float value)
    {
        // The SplitSlider popup is shared by the player, loot, and shop menu
        // instances. Every enabled menu can hold a listener, but only the menu
        // that opened the popup may write its value back to the shared slider.
        if (_suppressSplitEvents || _activeSplitPanelOwner != this)
            return;

        SetSplitAmount(Mathf.RoundToInt(value));

        if (_debugLogs)
        {
            Debug.Log(
                $"[{nameof(InventoryItemContextMenuUI)}] Split amount changed. " +
                $"stack={_splitSourceAmount}, selected={_splitCurrentAmount}.",
                this);
        }
    }

    // Purpose: Handles the incoming split input changed request or event.
    private void HandleSplitInputChanged(string value)
    {
        if (_suppressSplitEvents || _activeSplitPanelOwner != this)
            return;

        if (int.TryParse(value, out int parsed))
            SetSplitAmount(parsed);
    }

    // Purpose: Updates the split amount state.
    private void SetSplitAmount(int amount)
    {
        _splitCurrentAmount = Mathf.Clamp(amount, 1, Mathf.Max(1, _splitMaxAmount));
        RefreshSplitAmountViews();
    }

    // Purpose: Refreshes the displayed or cached split amount views.
    private void RefreshSplitAmountViews()
    {
        _suppressSplitEvents = true;

        if (_splitAmountSlider != null)
            _splitAmountSlider.value = _splitCurrentAmount;

        if (_splitAmountInput != null)
            _splitAmountInput.text = _splitCurrentAmount.ToString();
        _suppressSplitEvents = false;
    }

    // Purpose: Handles the incoming split confirm clicked request or event.
    private void HandleSplitConfirmClicked()
    {
        if (_activeSplitPanelOwner != this)
            return;

        if (_debugLogs)
        {
            Debug.Log(
                $"[{nameof(InventoryItemContextMenuUI)}] Split popup confirmed. " +
                $"target={_targetKind}, amount={_splitCurrentAmount}.",
                this);
        }

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

    // Purpose: Binds button label to the active runtime or UI target.
    private void BindButtonLabel(Button button, InventoryContextMenuActionDefinition definition)
    {
        if (button == null || definition == null)
            return;

        TextMeshProUGUI labelText = button.GetComponentInChildren<TextMeshProUGUI>(true);

        if (labelText != null)
            labelText.text = definition.ResolveLabel();

        LocalizedString label = definition.Label;
        if (labelText == null || label == null || label.IsEmpty)
            return;

        LocalizedString.ChangeHandler handler = value =>
        {
            if (labelText != null && !string.IsNullOrWhiteSpace(value))
                labelText.text = value;
        };

        label.StringChanged += handler;
        label.RefreshString();
        _localizedButtonBindings.Add(new LocalizedButtonBinding(label, handler));
    }

    // Purpose: Resolves the button root needed by this component.
    private Transform ResolveButtonRoot()
    {
        return _buttonRoot != null ? _buttonRoot : transform;
    }

    // Purpose: Clears the current buttons state.
    private void ClearButtons()
    {
        ClearLocalizedButtonBindings();

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

    // Purpose: Clears the current localized button bindings state.
    private void ClearLocalizedButtonBindings()
    {
        for (int i = 0; i < _localizedButtonBindings.Count; i++)
            _localizedButtonBindings[i]?.Release();

        _localizedButtonBindings.Clear();
    }

    // Purpose: Updates the screen position state.
    private void SetScreenPosition(Vector2 screenPosition)
    {
        if (_root == null)
            return;

        RectTransform parent = _root.parent as RectTransform;
        Vector2 targetScreenPosition = screenPosition + _screenOffset;

        if (parent != null &&
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                parent,
                targetScreenPosition,
                null,
                out Vector2 localPoint))
        {
            _root.anchoredPosition = localPoint;

            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(_root);

            if (_keepInsideParent)
                ClampRootInsideParent(parent);

            return;
        }

        _root.position = targetScreenPosition;
    }

    // The shared menu overlay is a sibling of the inventory slots. Moving the
    // overlay root, rather than only its generated buttons, keeps it last among
    // those siblings. The nested canvas on the actual menu rect guarantees the
    // generated buttons also win over inventory graphics in deeper layouts.
    // Purpose: Performs the bring overlay to front workflow for this component.
    private void BringOverlayToFront()
    {
        if (_group != null)
            _group.transform.SetAsLastSibling();
    }

    // Purpose: Ensures overlay input layer is ready before the workflow continues.
    private void EnsureOverlayInputLayer()
    {
        RectTransform inputRoot = _root;

        if (inputRoot == null)
            return;

        if (_overlayCanvas == null)
            _overlayCanvas = inputRoot.GetComponent<Canvas>();

        if (_overlayCanvas == null)
            _overlayCanvas = inputRoot.gameObject.AddComponent<Canvas>();

        if (_overlayRaycaster == null)
            _overlayRaycaster = inputRoot.GetComponent<GraphicRaycaster>();

        if (_overlayRaycaster == null)
            _overlayRaycaster = inputRoot.gameObject.AddComponent<GraphicRaycaster>();

        Canvas parentCanvas = inputRoot.parent != null
            ? inputRoot.parent.GetComponentInParent<Canvas>()
            : null;

        _overlayCanvas.overrideSorting = true;
        _overlayCanvas.sortingOrder = parentCanvas != null
            ? parentCanvas.sortingOrder + 10
            : 10;

        if (_debugLogs)
        {
            Debug.Log(
                $"[{nameof(InventoryItemContextMenuUI)}] Overlay input layer ready. " +
                $"root={inputRoot.name}, sortingOrder={_overlayCanvas.sortingOrder}, " +
                $"raycaster={_overlayRaycaster != null}.",
                this);
        }
    }

    // Purpose: Checks whether pointer inside menu rect is currently true.
    private bool IsPointerInsideMenuRect(Vector2 screenPosition)
    {
        if (_root == null || !_root.gameObject.activeInHierarchy)
            return false;

        Canvas canvas = _root.GetComponentInParent<Canvas>();
        Camera eventCamera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? canvas.worldCamera
            : null;

        return RectTransformUtility.RectangleContainsScreenPoint(
            _root,
            screenPosition,
            eventCamera);
    }

    // Purpose: Ensures split panel input layer is ready before the workflow continues.
    private void EnsureSplitPanelInputLayer()
    {
        if (_splitGroup == null)
            return;

        if (_splitOverlayCanvas == null)
            _splitOverlayCanvas = _splitGroup.GetComponent<Canvas>();

        if (_splitOverlayCanvas == null)
            _splitOverlayCanvas = _splitGroup.gameObject.AddComponent<Canvas>();

        if (_splitOverlayRaycaster == null)
            _splitOverlayRaycaster = _splitGroup.GetComponent<GraphicRaycaster>();

        if (_splitOverlayRaycaster == null)
            _splitOverlayRaycaster = _splitGroup.gameObject.AddComponent<GraphicRaycaster>();

        Canvas parentCanvas = _splitGroup.transform.parent != null
            ? _splitGroup.transform.parent.GetComponentInParent<Canvas>()
            : null;

        _splitOverlayCanvas.overrideSorting = true;
        _splitOverlayCanvas.sortingOrder = parentCanvas != null
            ? parentCanvas.sortingOrder + 20
            : 20;

        if (_debugLogs)
        {
            Debug.Log(
                $"[{nameof(InventoryItemContextMenuUI)}] Split input layer ready. " +
                $"root={_splitGroup.name}, sortingOrder={_splitOverlayCanvas.sortingOrder}, " +
                $"raycaster={_splitOverlayRaycaster != null}.",
                this);
        }
    }

    // Purpose: Performs the clamp root inside parent workflow for this component.
    private void ClampRootInsideParent(RectTransform parent)
    {
        if (_root == null || parent == null)
            return;

        Vector2 pos = _root.anchoredPosition;
        Rect parentRect = parent.rect;
        Rect rootRect = _root.rect;

        float minX = parentRect.xMin + _screenPadding.x - rootRect.xMin;
        float maxX = parentRect.xMax - _screenPadding.x - rootRect.xMax;
        float minY = parentRect.yMin + _screenPadding.y - rootRect.yMin;
        float maxY = parentRect.yMax - _screenPadding.y - rootRect.yMax;

        pos.x = Mathf.Clamp(pos.x, minX, maxX);
        pos.y = Mathf.Clamp(pos.y, minY, maxY);

        _root.anchoredPosition = pos;
    }
    // Purpose: Updates the visible state.
    private void SetVisible(bool visible)
    {
        SetCanvasGroup(_group, visible);
    }

    // Purpose: Checks whether visible is currently true.
    private bool IsVisible()
    {
        return _group != null &&
               _group.alpha > 0f &&
               _group.interactable &&
               _group.blocksRaycasts;
    }

    // Purpose: Attempts to get dismiss pointer down and reports whether the request succeeded.
    private static bool TryGetDismissPointerDown(out Vector2 screenPosition)
    {
        screenPosition = default;

        Mouse mouse = Mouse.current;

        if (mouse == null)
            return false;

        bool pressed =
            mouse.leftButton.wasPressedThisFrame ||
            mouse.rightButton.wasPressedThisFrame;

        if (!pressed)
            return false;

        screenPosition = mouse.position.ReadValue();
        return true;
    }

    // Purpose: Checks whether pointer over menu is currently true.
    private bool IsPointerOverMenu(Vector2 screenPosition)
    {
        EventSystem eventSystem = EventSystem.current;

        if (eventSystem == null)
            return false;

        PointerEventData eventData = new(eventSystem)
        {
            position = screenPosition
        };

        _raycastResults.Clear();
        eventSystem.RaycastAll(eventData, _raycastResults);

        for (int i = 0; i < _raycastResults.Count; i++)
        {
            Transform hit = _raycastResults[i].gameObject != null
                ? _raycastResults[i].gameObject.transform
                : null;

            if (IsMenuTransform(hit))
                return true;
        }

        return false;
    }

    // Purpose: Checks whether menu transform is currently true.
    private bool IsMenuTransform(Transform target)
    {
        if (target == null)
            return false;

        for (int i = 0; i < _spawnedButtons.Count; i++)
        {
            Button button = _spawnedButtons[i];

            if (button != null && target.IsChildOf(button.transform))
                return true;
        }

        if (_splitGroup != null && target.IsChildOf(_splitGroup.transform))
            return true;

        if (_discardConfirmGroup != null && target.IsChildOf(_discardConfirmGroup.transform))
            return true;

        return false;
    }

    // Purpose: Updates the canvas group state.
    private static void SetCanvasGroup(CanvasGroup group, bool visible)
    {
        if (group == null)
            return;

        group.alpha = visible ? 1f : 0f;
        group.interactable = visible;
        group.blocksRaycasts = visible;
    }

    // Purpose: Ensures refs is ready before the workflow continues.
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

        EnsureSplitPanelRefs();
    }

    // Purpose: Ensures split panel refs is ready before the workflow continues.
    private void EnsureSplitPanelRefs()
    {
        if (_splitGroup != null &&
            _splitConfirmButton != null &&
            (_splitAmountSlider != null || _splitAmountInput != null))
        {
            return;
        }

        Transform hierarchyRoot = transform.root;
        Transform splitRoot = null;
        Transform[] candidates =
            hierarchyRoot.GetComponentsInChildren<Transform>(true);

        for (int i = 0; i < candidates.Length; i++)
        {
            Transform candidate = candidates[i];
            if (candidate == null ||
                candidate == transform ||
                !string.Equals(candidate.name, "SplitSlider", StringComparison.Ordinal))
            {
                continue;
            }

            splitRoot = candidate;
            break;
        }

        if (splitRoot == null)
        {
            Debug.LogError(
                $"[{nameof(InventoryItemContextMenuUI)}] SplitSlider was not found " +
                $"below canvas root '{hierarchyRoot.name}'. Assign the split popup " +
                "references or add the SplitSlider prefab.",
                this);
            return;
        }

        _splitGroup ??= splitRoot.GetComponent<CanvasGroup>();
        _splitTitleText ??=
            FindNamedChild(splitRoot, "ItemNameText")
                ?.GetComponent<TextMeshProUGUI>();
        _splitAmountSlider ??=
            FindNamedChild(splitRoot, "Slider")
                ?.GetComponent<Slider>();
        _splitAmountInput ??=
            FindNamedChild(splitRoot, "AmountInput")
                ?.GetComponent<TMP_InputField>();
        _splitConfirmButton ??=
            FindNamedChild(splitRoot, "SplitButton")
                ?.GetComponent<Button>();
        _splitCancelButton ??=
            FindNamedChild(splitRoot, "CancelButon")
                ?.GetComponent<Button>();

        if (_splitGroup == null ||
            _splitConfirmButton == null ||
            (_splitAmountSlider == null && _splitAmountInput == null))
        {
            Debug.LogError(
                $"[{nameof(InventoryItemContextMenuUI)}] SplitSlider exists but is " +
                "missing required CanvasGroup/Button/Slider references.",
                splitRoot);
            return;
        }

        splitRoot.SetAsLastSibling();

        if (_activeSplitPanelOwner == null)
        {
            SetCanvasGroup(_splitGroup, false);
        }
    }

    // Purpose: Resolves the shared discard confirmation popup by its authored
    // hierarchy names so each context menu does not need scene-only inspector
    // assignments.
    private void EnsureDiscardConfirmRefs()
    {
        if (_discardConfirmGroup != null &&
            _discardConfirmYesButton != null &&
            _discardConfirmNoButton != null)
        {
            return;
        }

        Transform hierarchyRoot = transform.root;
        Transform warningRoot = FindNamedChild(hierarchyRoot, "Popup_Warning");

        if (warningRoot == null)
            return;

        _discardConfirmGroup ??= warningRoot.GetComponent<CanvasGroup>();
        _discardConfirmText ??=
            FindNamedChild(warningRoot, "MessageText")
                ?.GetComponent<TextMeshProUGUI>();
        _discardConfirmYesButton ??=
            FindNamedChild(warningRoot, "Btn_Red_Ok")
                ?.GetComponent<Button>();
        _discardConfirmNoButton ??=
            FindNamedChild(warningRoot, "Btn_Red_Cancel")
                ?.GetComponent<Button>();

        if (_discardConfirmGroup == null ||
            _discardConfirmYesButton == null ||
            _discardConfirmNoButton == null)
        {
            return;
        }

        SetCanvasGroup(_discardConfirmGroup, false);
    }

    // Purpose: Finds the requested named child.
    private static Transform FindNamedChild(
        Transform root,
        string childName)
    {
        if (root == null || string.IsNullOrEmpty(childName))
            return null;

        Transform[] children =
            root.GetComponentsInChildren<Transform>(true);

        for (int i = 0; i < children.Length; i++)
        {
            if (string.Equals(
                    children[i].name,
                    childName,
                    StringComparison.Ordinal))
            {
                return children[i];
            }
        }

        return null;
    }

    // Purpose: Hides the button prefab if needed UI or visual state.
    private void HideButtonPrefabIfNeeded()
    {
        if (_hideButtonPrefabOnAwake && _buttonPrefab != null)
            _buttonPrefab.gameObject.SetActive(false);
    }
}
