/*
 * Role hierarchy: Presentation > UI > Gameplay > PanelHub > Inventory
 * Primary responsibility: Presents EquipmentInventorySlotUI state and handles its user-facing UI interaction.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Localization;
using UnityEngine.Serialization;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class EquipmentInventorySlotUI : MonoBehaviour,
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
    [FormerlySerializedAs("_labelText")]
    [SerializeField] private TextMeshProUGUI _displayNameText;

    [Header("Slot Type")]
    [Tooltip("Fixed label for this equipment slot, such as Weapon, Armor, or Ammo.")]
    [SerializeField] private TextMeshProUGUI _slotTypeNameText;
    [Tooltip("Localized fixed label for this equipment slot. This is independent from the equipped item name.")]
    [SerializeField] private LocalizedString _slotTypeName;
    [SerializeField] private string _slotTypeFallbackName;

    [Header("Debug")]
    [SerializeField] private bool _debugInteractionLogs = true;

    protected IInventorySlotOwner Owner { get; private set; }
    protected ItemSO Item { get; private set; }
    protected Image IconImage => _iconImage;
    protected InventoryDragSourceKind DragSourceKind { get; private set; } = InventoryDragSourceKind.Equipment;
    protected int DragSlotIndex { get; private set; } = -1;
    protected bool IsEnemyInventoryTarget => DragSourceKind == InventoryDragSourceKind.EnemyInventory;
    public bool HasItem => Item != null;
    private LocalizedString _boundItemName;
    private string _boundItemNameFallback;
    private string _runtimeSlotTypeOverride;

    /// <summary>
    /// Runtime-only visual bindings used when an older panel prefab does not
    /// yet contain a newly added dedicated equipment widget.
    /// </summary>
    public readonly struct RuntimeVisualBindings
    {
        public readonly Image iconImage;
        public readonly TextMeshProUGUI displayNameText;
        public readonly TextMeshProUGUI slotTypeNameText;
        public readonly LocalizedString slotTypeName;
        public readonly string slotTypeFallbackName;

        public RuntimeVisualBindings(
            Image iconImage,
            TextMeshProUGUI displayNameText,
            TextMeshProUGUI slotTypeNameText,
            LocalizedString slotTypeName,
            string slotTypeFallbackName)
        {
            this.iconImage = iconImage;
            this.displayNameText = displayNameText;
            this.slotTypeNameText = slotTypeNameText;
            this.slotTypeName = slotTypeName;
            this.slotTypeFallbackName = slotTypeFallbackName;
        }
    }

    // Purpose: Caches the component references required before runtime initialization.
    protected virtual void Awake()
    {
        EnsureRaycastTargets();
    }

    // Purpose: Captures the authored visual references before a runtime fallback replaces its behavior component.
    public RuntimeVisualBindings CaptureRuntimeVisualBindings()
    {
        return new RuntimeVisualBindings(
            _iconImage,
            _displayNameText,
            _slotTypeNameText,
            _slotTypeName,
            _slotTypeFallbackName);
    }

    // Purpose: Applies visual references to a runtime fallback without changing the authored panel hierarchy.
    public void ApplyRuntimeVisualBindings(RuntimeVisualBindings bindings)
    {
        _iconImage = bindings.iconImage;
        _displayNameText = bindings.displayNameText;
        _slotTypeNameText = bindings.slotTypeNameText;
        _slotTypeName = bindings.slotTypeName;
        _slotTypeFallbackName = bindings.slotTypeFallbackName;
        EnsureRaycastTargets();
        RefreshSlotTypeName();
    }

    // Purpose: Activates runtime subscriptions and refreshes the enabled state.
    private void OnEnable()
    {
        if (_slotTypeName != null && !_slotTypeName.IsEmpty)
            _slotTypeName.StringChanged += HandleSlotTypeNameChanged;

        RefreshSlotTypeName();
        BindDisplayName(Item);
    }

    // Purpose: Releases active subscriptions and transient state while disabled.
    private void OnDisable()
    {
        if (_slotTypeName != null)
            _slotTypeName.StringChanged -= HandleSlotTypeNameChanged;

        UnbindDisplayName();
    }

    // Purpose: Initializes initialize for use by this component.
    public virtual void Initialize(IInventorySlotOwner owner)
    {
        Initialize(owner, InventoryDragSourceKind.Equipment, -1);
    }

    // Purpose: Initializes initialize for use by this component.
    public virtual void Initialize(
        IInventorySlotOwner owner,
        InventoryDragSourceKind dragSourceKind,
        int dragSlotIndex)
    {
        Owner = owner;
        DragSourceKind = dragSourceKind;
        DragSlotIndex = dragSlotIndex;
    }

    // Purpose: Binds bind to the active runtime or UI target.
    public virtual void Bind(ItemSO item)
    {
        Item = item;

        if (_iconImage != null)
        {
            _iconImage.sprite = item != null ? item.PreviewImage : null;
            _iconImage.enabled = item != null && _iconImage.sprite != null;
            ModuleRuneView.Bind(_iconImage, item);
        }

        BindDisplayName(item);

        OnBound(item);
    }

    // Purpose: Responds to the pointer click callback.
    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData == null || Owner == null || !HasItem)
        {
            if (_debugInteractionLogs)
            {
                Debug.Log(
                    $"[EquipmentInventorySlotUI] Click ignored. type={GetType().Name}, eventNull={eventData == null}, ownerNull={Owner == null}, hasItem={HasItem}, dragSource={DragSourceKind}, dragSlot={DragSlotIndex}",
                    this);
            }

            return;
        }

        if (_debugInteractionLogs)
        {
            Debug.Log(
                $"[EquipmentInventorySlotUI] Click received. type={GetType().Name}, button={eventData.button}, clickCount={eventData.clickCount}, dragSource={DragSourceKind}, dragSlot={DragSlotIndex}, item={InventoryItemDisplayUtility.ResolveItemName(Item)}",
                this);
        }

        PublishItemDetailSelection();
        if (eventData.button == PointerEventData.InputButton.Left || eventData.button == PointerEventData.InputButton.Right)
            UIAudio.Request(UIAudioCueType.ButtonClick);

        if (eventData.button == PointerEventData.InputButton.Right)
        {
            HandleRightClick(eventData.position);
            return;
        }

        if (eventData.button == PointerEventData.InputButton.Left)
        {
            // Enemy loot equipment mirrors carried loot: a single click only selects/inspects,
            // while an intentional double-click transfers the item to the player.
            if (Owner is EnemyInventoryUI && eventData.clickCount < 2)
                return;

            if (eventData.clickCount >= 2)
            {
                HandleDoubleLeftClick();
                return;
            }

            HandleLeftClick();
        }
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

    // Purpose: Responds to the begin drag callback.
    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!HasItem)
            return;

        BeginDragPayload();

        if (InventoryDragContext.HasPayload)
            ShowDragGhost(eventData);
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
        if (Owner == null || !InventoryDragContext.HasPayload)
            return;

        HandleDropPayload();
        InventoryDragContext.Clear();
    }

    // Purpose: Responds to the bound callback.
    protected virtual void OnBound(ItemSO item)
    {
    }

    // Purpose: Publishes the item detail selection notification.
    protected virtual void PublishItemDetailSelection()
    {
        if (Item != null)
            ItemDetailSelectionBus.Publish(new ItemDetailSelection(Item, 1));
    }

    // Purpose: Handles the incoming left click request or event.
    protected virtual void HandleLeftClick()
    {
    }

    // Purpose: Handles the incoming double left click request or event.
    protected virtual void HandleDoubleLeftClick()
    {
        HandleLeftClick();
    }

    // Purpose: Handles the incoming right click request or event.
    protected virtual void HandleRightClick(Vector2 screenPosition)
    {
    }

    // Purpose: Begins the drag payload workflow.
    protected virtual void BeginDragPayload()
    {
    }

    // Purpose: Handles the incoming drop payload request or event.
    protected virtual void HandleDropPayload()
    {
    }

    /// <summary>
    /// Lets the player inventory reuse the existing serialized equipment widgets as
    /// generic quick slots without replacing prefab components or losing references.
    /// </summary>
    protected void SetRuntimeSlotTypeOverride(string value)
    {
        _runtimeSlotTypeOverride = value ?? string.Empty;
        RefreshSlotTypeName();
    }

    /// <summary>
    /// Slot-specific layouts can suppress the serialized equipment label when
    /// another label already explains the slot state.
    /// </summary>
    protected void SetSlotTypeTextVisible(bool visible)
    {
        if (_slotTypeNameText != null)
            _slotTypeNameText.gameObject.SetActive(visible);
    }

    /// <summary>
    /// Runtime-only layouts can use the icon plus their custom state label
    /// without also drawing the generic equipped-item display name.
    /// </summary>
    protected void SetDisplayNameTextVisible(bool visible)
    {
        if (_displayNameText != null)
            _displayNameText.gameObject.SetActive(visible);
    }

    /// <summary>
    /// Allows a specialized slot prefab to supply the icon image it inherits
    /// from its visual base without changing its regular bind behavior.
    /// </summary>
    protected void SetIconImage(Image iconImage)
    {
        _iconImage = iconImage;

        if (_iconImage != null)
            _iconImage.raycastTarget = true;
    }

    // Purpose: Attempts to begin enemy inventory drag and reports whether the request succeeded.
    protected bool TryBeginEnemyInventoryDrag()
    {
        if (DragSourceKind != InventoryDragSourceKind.EnemyInventory)
            return false;

        if (Owner is not EnemyInventoryUI enemyInventory)
            return true;

        InventoryDragContext.BeginEnemyInventoryDrag(enemyInventory, DragSlotIndex);
        return true;
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

    // Purpose: Refreshes the displayed or cached slot type name.
    private void RefreshSlotTypeName()
    {
        if (_slotTypeNameText == null)
            return;

        if (!string.IsNullOrWhiteSpace(_runtimeSlotTypeOverride))
        {
            _slotTypeNameText.text = _runtimeSlotTypeOverride;
            return;
        }

        if (_slotTypeName != null && !_slotTypeName.IsEmpty)
        {
            _slotTypeName.RefreshString();
            return;
        }

        _slotTypeNameText.text = _slotTypeFallbackName ?? string.Empty;
    }

    // Purpose: Handles the incoming slot type name changed request or event.
    private void HandleSlotTypeNameChanged(string value)
    {
        if (_slotTypeNameText == null)
            return;

        _slotTypeNameText.text = string.IsNullOrWhiteSpace(value)
            ? _slotTypeFallbackName ?? string.Empty
            : value;
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
