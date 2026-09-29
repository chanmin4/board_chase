/*
 * Role hierarchy: Presentation > UI > Gameplay > PanelHub > Inventory
 * Primary responsibility: Presents InventoryItemSlotUI state and handles its user-facing UI interaction.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

// Generic inventory item slot. Used by player inventory grids and enemy inventory grids.
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Localization;
using UnityEngine.Serialization;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class InventoryItemSlotUI : MonoBehaviour,
    IPointerClickHandler,
    IPointerEnterHandler,
    IPointerExitHandler,
    IBeginDragHandler,
    IDragHandler,
    IEndDragHandler,
    IDropHandler
{
    [Header("UI")]
    [SerializeField] private Image _iconImage;
    [SerializeField] private TextMeshProUGUI _displayNameText;
    [SerializeField] private TextMeshProUGUI _itemInfoText;
    [Header("Durability")]
    [SerializeField] private Color _normalDurabilityTextColor = Color.white;
    [SerializeField] private Color _depletedDurabilityTextColor = Color.red;

    [Header("Debug")]
    [SerializeField] private bool _debugInteractionLogs = true;

    private IInventorySlotOwner _owner;
    private int _slotIndex = -1;
    private PlayerInventoryItemStack _stack;
    private PlayerInventoryItemStack _directBindStack;
    private bool _dragEnabled = true;
    private bool _requireDoubleClickForLeftAction;
    private InventoryDragSourceKind _dragSourceKind = InventoryDragSourceKind.Inventory;
    private LocalizedString _boundItemName;
    private string _boundItemNameFallback;

    public int SlotIndex => _slotIndex;
    public bool HasItem => _stack != null && !_stack.IsEmpty;

    // Purpose: Caches the component references required before runtime initialization.
    private void Awake()
    {
        EnsureRaycastTargets();
    }

    // Purpose: Activates runtime subscriptions and refreshes the enabled state.
    private void OnEnable()
    {
        ItemSO item = _stack != null && !_stack.IsEmpty ? _stack.Item : null;
        BindDisplayName(item);
    }

    // Purpose: Releases active subscriptions and transient state while disabled.
    private void OnDisable()
    {
        UnbindDisplayName();
    }

    // Purpose: Initializes initialize for use by this component.
    public void Initialize(IInventorySlotOwner owner, int slotIndex)
    {
        _owner = owner;
        _slotIndex = slotIndex;
        _dragEnabled = true;
        _requireDoubleClickForLeftAction = false;
        _dragSourceKind = InventoryDragSourceKind.Inventory;
    }

    // Purpose: Initializes initialize for use by this component.
    public void Initialize(IInventorySlotOwner owner, int slotIndex, bool dragEnabled)
    {
        Initialize(
            owner,
            slotIndex,
            dragEnabled,
            false,
            InventoryDragSourceKind.Inventory);
    }

    // Purpose: Initializes initialize for use by this component.
    public void Initialize(
        IInventorySlotOwner owner,
        int slotIndex,
        bool dragEnabled,
        bool requireDoubleClickForLeftAction,
        InventoryDragSourceKind dragSourceKind)
    {
        _owner = owner;
        _slotIndex = slotIndex;
        _dragEnabled = dragEnabled;
        _requireDoubleClickForLeftAction = requireDoubleClickForLeftAction;
        _dragSourceKind = dragSourceKind;
    }

    // Purpose: Binds bind to the active runtime or UI target.
    public void Bind(PlayerInventoryItemStack stack)
    {
        _stack = stack;
        ItemSO item = stack != null && !stack.IsEmpty ? stack.Item : null;

        if (_iconImage != null)
        {
            _iconImage.sprite = item != null ? item.PreviewImage : null;
            _iconImage.enabled = item != null && _iconImage.sprite != null;
        }

        BindDisplayName(item);

        RefreshItemInfoText(stack, item);
    }

    // Purpose: Binds item to the active runtime or UI target.
    public void BindItem(
        ItemSO item,
        int amount = 1,
        float armorDurability = -1f,
        float weaponCurrentDurability = -1f,
        float weaponMaxDurability = -1f)
    {
        if (item == null)
        {
            Bind(null);
            return;
        }

        if (_directBindStack == null)
            _directBindStack = new PlayerInventoryItemStack();

        _directBindStack.Set(
            item,
            Mathf.Max(1, amount),
            armorDurability,
            weaponCurrentDurability,
            weaponMaxDurability);
        Bind(_directBindStack);
    }

    // Purpose: Binds one durable physical Key Tool entry while retaining the shared inventory slot visual.
    public void BindKeyToolItem(KeyItemSO key, int remainingUses)
    {
        if (key == null)
        {
            Bind(null);
            return;
        }

        if (_directBindStack == null)
            _directBindStack = new PlayerInventoryItemStack();

        _directBindStack.Set(key, 1);
        Bind(_directBindStack);
        SetDurabilityInfo(
            Mathf.Clamp(remainingUses, 0, key.MaxUses),
            key.MaxUses);
    }

    // Purpose: Responds to the pointer click callback.
    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData == null || _owner == null || !HasItem)
        {
            if (_debugInteractionLogs)
            {
                Debug.Log(
                    $"[InventoryItemSlotUI] Click ignored. slot={_slotIndex}, eventNull={eventData == null}, ownerNull={_owner == null}, hasItem={HasItem}",
                    this);
            }

            return;
        }

        if (_debugInteractionLogs)
        {
            Debug.Log(
                $"[InventoryItemSlotUI] Click received. slot={_slotIndex}, button={eventData.button}, clickCount={eventData.clickCount}, doubleClickRequired={_requireDoubleClickForLeftAction}",
                this);
        }

        ItemDetailSelectionBus.Publish(_stack);
        if (eventData.button == PointerEventData.InputButton.Left || eventData.button == PointerEventData.InputButton.Right)
            UIAudio.Request(UIAudioCueType.ButtonClick);

        if (eventData.button == PointerEventData.InputButton.Left)
        {
            if (_requireDoubleClickForLeftAction && eventData.clickCount < 2)
                return;

            _owner.HandleInventorySlotLeftClicked(_slotIndex);
            return;
        }

        if (eventData.button == PointerEventData.InputButton.Right)
            _owner.HandleInventorySlotRightClicked(_slotIndex, eventData.position);
    }

    // Purpose: Responds to the begin drag callback.
    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!_dragEnabled)
            return;

        if (!HasItem)
            return;

        if (_dragSourceKind == InventoryDragSourceKind.EnemyInventory &&
            _owner is EnemyInventoryUI enemyInventory)
        {
            InventoryDragContext.BeginEnemyInventoryDrag(enemyInventory, _slotIndex);
            ShowDragGhost(eventData);
            return;
        }

        InventoryDragContext.BeginInventoryDrag(_slotIndex);
        ShowDragGhost(eventData);
    }

    // Purpose: Responds to the pointer enter callback.
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (HasItem) UIAudio.Request(UIAudioCueType.Navigate);
        // Item details are shown by the click-selected detail panel.
    }

    // Purpose: Responds to the pointer exit callback.
    public void OnPointerExit(PointerEventData eventData)
    {
        // Hover tooltips are intentionally disabled.
    }

    // Purpose: Responds to the end drag callback.
    public void OnEndDrag(PointerEventData eventData)
    {
        InventoryDragContext.Clear();
    }

    // Purpose: Responds to the drag callback.
    public void OnDrag(PointerEventData eventData)
    {
        if (eventData != null)
            InventoryDragGhostUI.Move(eventData.position);
    }

    // Purpose: Responds to the drop callback.
    public void OnDrop(PointerEventData eventData)
    {
        if (!_dragEnabled)
            return;

        if (_owner == null || !InventoryDragContext.HasPayload)
            return;

        switch (InventoryDragContext.SourceKind)
        {
            case InventoryDragSourceKind.Inventory:
                _owner.HandleInventorySlotDroppedOnInventory(
                    InventoryDragContext.InventoryIndex,
                    _slotIndex);
                break;

            case InventoryDragSourceKind.Equipment:
                _owner.HandleEquipmentSlotDroppedOnInventory(
                    InventoryDragContext.EquipmentSlotKind,
                    _slotIndex);
                break;

            case InventoryDragSourceKind.EnemyInventory:
                if (_owner is EnemyInventoryUI enemyInventory)
                {
                    enemyInventory.TryReceiveDragPayloadToEnemyInventorySlot(_slotIndex);
                }
                else
                {
                    InventoryDragContext.EnemyInventoryOwner?.TryTakeSlotToPlayerInventorySlot(
                        InventoryDragContext.EnemyInventorySlotIndex,
                        _slotIndex);
                }
                break;

            case InventoryDragSourceKind.EquipmentAmmo:
                _owner.HandleAmmoSlotDroppedOnInventory(
                    InventoryDragContext.EquipmentAmmoSlotIndex,
                    _slotIndex);
                break;

            case InventoryDragSourceKind.QuickSlot:
                if (_owner is PlayerInventoryUI playerInventory)
                {
                    playerInventory.HandleQuickSlotDroppedOnInventory(
                        InventoryDragContext.QuickSlotIndex,
                        _slotIndex);
                }
                break;
        }

        InventoryDragContext.Clear();
    }

    // Purpose: Refreshes the displayed or cached item info text.
    private void RefreshItemInfoText(PlayerInventoryItemStack stack, ItemSO item)
    {
        if (_itemInfoText == null)
            return;

        _itemInfoText.text = string.Empty;
        _itemInfoText.gameObject.SetActive(false);

        if (item == null || stack == null || stack.IsEmpty)
            return;

        if (item is ArmorItemSO armor)
        {
            float max = Mathf.Max(0f, armor.MaxDurability);
            float current = stack.HasArmorDurability
                ? Mathf.Clamp(stack.ArmorDurability, 0f, max)
                : max;
            SetDurabilityInfo(current, max);
            return;
        }

        if (item is RangedWeaponSO rangedWeapon)
        {
            float max = stack.HasWeaponDurability
                ? stack.WeaponMaxDurability
                : rangedWeapon.MaxDurability;
            float current = stack.HasWeaponDurability
                ? stack.WeaponCurrentDurability
                : max;
            SetDurabilityInfo(current, max);
            return;
        }

        if (item.MaxStack <= 1 && !(item is CurrencyItemSO))
            return;

        int amount = Mathf.Max(0, stack.Amount);

        if (amount <= 1)
            return;

        _itemInfoText.text = amount.ToString();
        _itemInfoText.gameObject.SetActive(true);
    }

    // Purpose: Updates the durability info state.
    private void SetDurabilityInfo(float current, float max)
    {
        bool depleted = max > 0f && current <= 0f;
        _itemInfoText.text = $"{Mathf.Max(0f, current):0}/{Mathf.Max(0f, max):0}";
        _itemInfoText.color = depleted
            ? _depletedDurabilityTextColor
            : _normalDurabilityTextColor;
        _itemInfoText.gameObject.SetActive(true);
    }

    // Purpose: Ensures raycast targets is ready before the workflow continues.
    private void EnsureRaycastTargets()
    {
        Graphic rootGraphic = GetComponent<Graphic>();

        if (rootGraphic != null)
            rootGraphic.raycastTarget = true;

        if (_iconImage != null)
            _iconImage.raycastTarget = true;
    }

    // Purpose: Binds display name to the active runtime or UI target.
    private void BindDisplayName(ItemSO item)
    {
        UnbindDisplayName();

        if (_displayNameText == null)
            return;

        _boundItemNameFallback = InventoryItemDisplayUtility.ResolveItemName(item);
        _displayNameText.text = _boundItemNameFallback;

        if (!InventoryItemDisplayUtility.TryGetItemNameReference(item, out _boundItemName))
            return;

        _boundItemName.StringChanged += HandleDisplayNameChanged;
        _boundItemName.RefreshString();
    }

    // Purpose: Removes the display name binding.
    private void UnbindDisplayName()
    {
        if (_boundItemName != null)
            _boundItemName.StringChanged -= HandleDisplayNameChanged;

        _boundItemName = null;
    }

    // Purpose: Handles the incoming display name changed request or event.
    private void HandleDisplayNameChanged(string value)
    {
        if (_displayNameText == null)
            return;

        _displayNameText.text = string.IsNullOrWhiteSpace(value)
            ? _boundItemNameFallback ?? string.Empty
            : value;
    }

    // Purpose: Shows the drag ghost UI or visual state.
    private void ShowDragGhost(PointerEventData eventData)
    {
        if (eventData == null || _iconImage == null || _iconImage.sprite == null)
            return;

        InventoryDragGhostUI.Show(
            transform,
            _iconImage.sprite,
            eventData.position);
    }
}
