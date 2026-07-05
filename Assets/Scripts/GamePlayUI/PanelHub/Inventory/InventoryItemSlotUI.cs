// Generic inventory item slot. Used by player inventory grids and enemy inventory grids.
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class InventoryItemSlotUI : MonoBehaviour,
    IPointerClickHandler,
    IBeginDragHandler,
    IEndDragHandler,
    IDropHandler
{
    [Header("UI")]
    [SerializeField] private Image _iconImage;
    [SerializeField] private TextMeshProUGUI _displayNameText;
    [SerializeField] private TextMeshProUGUI _itemInfoText;
    [Header("Durability")]
    [SerializeField] private string _durabilityFormat = "{0:0}/{1:0}";
    [SerializeField] private Color _normalDurabilityTextColor = Color.white;
    [SerializeField] private Color _depletedDurabilityTextColor = Color.red;

    private IInventorySlotOwner _owner;
    private int _slotIndex = -1;
    private PlayerInventoryItemStack _stack;
    private PlayerInventoryItemStack _directBindStack;
    private bool _dragEnabled = true;
    private bool _requireDoubleClickForLeftAction;
    private InventoryDragSourceKind _dragSourceKind = InventoryDragSourceKind.Inventory;

    public int SlotIndex => _slotIndex;
    public bool HasItem => _stack != null && !_stack.IsEmpty;

    public void Initialize(IInventorySlotOwner owner, int slotIndex)
    {
        _owner = owner;
        _slotIndex = slotIndex;
        _dragEnabled = true;
        _requireDoubleClickForLeftAction = false;
        _dragSourceKind = InventoryDragSourceKind.Inventory;
    }

    public void Initialize(IInventorySlotOwner owner, int slotIndex, bool dragEnabled)
    {
        Initialize(
            owner,
            slotIndex,
            dragEnabled,
            false,
            InventoryDragSourceKind.Inventory);
    }

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

    public void Bind(PlayerInventoryItemStack stack)
    {
        _stack = stack;
        ItemSO item = stack != null && !stack.IsEmpty ? stack.Item : null;

        if (_iconImage != null)
        {
            _iconImage.sprite = item != null ? item.PreviewImage : null;
            _iconImage.enabled = item != null && _iconImage.sprite != null;
        }

        if (_displayNameText != null)
            _displayNameText.text = InventoryItemDisplayUtility.ResolveItemName(item);

        RefreshItemInfoText(stack, item);
    }

    public void BindItem(ItemSO item, int amount = 1, float armorDurability = -1f)
    {
        if (item == null)
        {
            Bind(null);
            return;
        }

        if (_directBindStack == null)
            _directBindStack = new PlayerInventoryItemStack();

        _directBindStack.Set(item, Mathf.Max(1, amount), armorDurability);
        Bind(_directBindStack);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData == null || _owner == null || !HasItem)
            return;

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
            return;
        }

        InventoryDragContext.BeginInventoryDrag(_slotIndex);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        InventoryDragContext.Clear();
    }

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
                InventoryDragContext.EnemyInventoryOwner?.TryTakeSlotToPlayerInventorySlot(
                    InventoryDragContext.EnemyInventorySlotIndex,
                    _slotIndex);
                break;

            case InventoryDragSourceKind.EquipmentAmmo:
                _owner.HandleAmmoSlotDroppedOnInventory(
                    InventoryDragContext.EquipmentAmmoSlotIndex,
                    _slotIndex);
                break;
        }

        InventoryDragContext.Clear();
    }

    private void RefreshItemInfoText(PlayerInventoryItemStack stack, ItemSO item)
    {
        if (_itemInfoText == null)
            return;

        _itemInfoText.text = string.Empty;
        _itemInfoText.gameObject.SetActive(false);

        if (item == null || stack == null || stack.IsEmpty)
            return;

        if (item.MaxStack <= 1)
            return;

        int amount = Mathf.Max(0, stack.Amount);

        if (amount <= 1)
            return;

        _itemInfoText.text = amount.ToString();
        _itemInfoText.gameObject.SetActive(true);
    }

}
