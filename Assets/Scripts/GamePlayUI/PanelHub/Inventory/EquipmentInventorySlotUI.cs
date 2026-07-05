using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class EquipmentInventorySlotUI : MonoBehaviour,
    IPointerClickHandler,
    IBeginDragHandler,
    IEndDragHandler,
    IDropHandler
{
    [Header("UI")]
    [SerializeField] private Image _iconImage;
    [FormerlySerializedAs("_labelText")]
    [SerializeField] private TextMeshProUGUI _displayNameText;

    protected IInventorySlotOwner Owner { get; private set; }
    protected ItemSO Item { get; private set; }
    protected InventoryDragSourceKind DragSourceKind { get; private set; } = InventoryDragSourceKind.Equipment;
    protected int DragSlotIndex { get; private set; } = -1;
    public bool HasItem => Item != null;

    public virtual void Initialize(IInventorySlotOwner owner)
    {
        Initialize(owner, InventoryDragSourceKind.Equipment, -1);
    }

    public virtual void Initialize(
        IInventorySlotOwner owner,
        InventoryDragSourceKind dragSourceKind,
        int dragSlotIndex)
    {
        Owner = owner;
        DragSourceKind = dragSourceKind;
        DragSlotIndex = dragSlotIndex;
    }

    public virtual void Bind(ItemSO item)
    {
        Item = item;

        if (_iconImage != null)
        {
            _iconImage.sprite = item != null ? item.PreviewImage : null;
            _iconImage.enabled = item != null && _iconImage.sprite != null;
        }

        if (_displayNameText != null)
            _displayNameText.text = InventoryItemDisplayUtility.ResolveItemName(item);

        OnBound(item);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData == null || Owner == null || !HasItem)
            return;

        if (eventData.button == PointerEventData.InputButton.Right)
        {
            HandleRightClick(eventData.position);
            return;
        }

        if (eventData.button == PointerEventData.InputButton.Left)
            HandleLeftClick();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!HasItem)
            return;

        BeginDragPayload();
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        InventoryDragContext.Clear();
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (Owner == null || !InventoryDragContext.HasPayload)
            return;

        HandleDropPayload();
        InventoryDragContext.Clear();
    }

    protected virtual void OnBound(ItemSO item)
    {
    }

    protected virtual void HandleLeftClick()
    {
    }

    protected virtual void HandleRightClick(Vector2 screenPosition)
    {
    }

    protected virtual void BeginDragPayload()
    {
    }

    protected virtual void HandleDropPayload()
    {
    }

    protected bool TryBeginEnemyInventoryDrag()
    {
        if (DragSourceKind != InventoryDragSourceKind.EnemyInventory)
            return false;

        if (Owner is not EnemyInventoryUI enemyInventory)
            return true;

        InventoryDragContext.BeginEnemyInventoryDrag(enemyInventory, DragSlotIndex);
        return true;
    }
}
