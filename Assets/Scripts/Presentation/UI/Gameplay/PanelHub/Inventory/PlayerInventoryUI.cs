/*
 * Role hierarchy: Presentation > UI > Gameplay > PanelHub > Inventory
 * Primary responsibility: Manages PlayerInventoryInteractionMode item storage, equipment, transfer state, and related rules.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum PlayerInventoryInteractionMode
{
    Normal = 0,
    SellSelect = 1,
    TradeSelect = 2
}

[DisallowMultipleComponent]
public class PlayerInventoryUI : MonoBehaviour,
    IInventorySlotOwner,
    IInventoryItemContextMenuOwner,
    IConsumableInventoryContextMenuOwner
{
    [Header("Refs")]
    [SerializeField] private SystemMessageEventChannelSO _systemMessageChannel;

    [Header("Runtime Ready")]
    [SerializeField] private PlayerInventoryRuntimeReadyEventChannelSO _inventoryRuntimeReadyChannel;

    [Header("Equipment Slots")]
    [SerializeField] private EquipmentInventoryArmorSlotUI _armorSlot;
    [Tooltip("Pre-authored Head / Gas Mask equipment slot beside Armor. Runtime binds it but never creates a replacement slot.")]
    [SerializeField] private EquipmentInventoryGasMaskSlotUI _headSlot;
    [Tooltip("Dedicated Weapon Slot 1-2 widgets. These remain weapon-only and are not part of the usable quick-slot bindings.")]
    [SerializeField] private EquipmentInventoryWeaponSlotUI[] _weaponSlots;
    [Tooltip("Equipment-panel view of usable Quick Slots 3-5. These mirror the same runtime bindings shown by SlotHUD.")]
    [SerializeField] private EquipmentInventoryAmmoSlotUI[] _ammoSlots;

    [Header("Passive Slots")]
    // Resolved from the active player inventory at runtime. It is intentionally
    // hidden because a prefab asset cannot safely point at a scene runtime.
    [SerializeField, HideInInspector] private PlayerPassiveInventoryRuntime _passiveRuntime;
    [Tooltip("Pre-authored children of PassiveSection. Their order is their passive-slot order; runtime never creates or repositions them.")]
    [SerializeField] private PassiveInventorySlotUI[] _passiveSlots;

    [Header("Inventory Slot Instancing")]
    [SerializeField] private InventoryItemSlotUI _inventorySlotPrefab;
    [SerializeField] private Transform _inventorySlotRoot;
    [SerializeField] private bool _hideInventorySlotPrefabOnAwake = true;
    [Tooltip("Separates Pocket slots from Inventory (four permanent slots plus equipped-backpack bonus slots).")]
    [SerializeField] private bool _useSeparatedPocketAndInventorySections = true;

    [Header("Context Menu")]
    [SerializeField] private InventoryItemContextMenuUI _contextMenu;

    [Header("Tooltip")]
    [Tooltip("Tooltip prefab displayed immediately to the right of Player equipment and inventory slots.")]
    [SerializeField] private PlayerTooltipUI _tooltipPrefab;
    [SerializeField] private TooltipPlacementSide _tooltipSide = TooltipPlacementSide.Right;
    [SerializeField, Min(0f)] private float _tooltipGap;

    [Header("World Drop")]
    [SerializeField, Min(0f)] private float _dropForwardOffset = 1f;
    [SerializeField] private float _dropUpOffset = 0.05f;

    [Header("Debug")]
    [SerializeField] private bool _debugInteractionLogs = true;

    private Transform _dropActor;
    private readonly List<InventoryItemSlotUI> _inventorySlots = new();

    private PlayerInventoryRuntime _inventoryRuntime;
    private PlayerConsumableUseRuntime _consumableUseRuntime;
    private PlayerQuickSlotRuntime _quickSlots;
    private PlayerInventoryInteractionMode _interactionMode = PlayerInventoryInteractionMode.Normal;
    private int _selectedInventoryIndex = -1;
    private int _contextEquipmentWeaponSlotIndex;
    private PlayerInventoryItemSelection _selectedItemSelection = PlayerInventoryItemSelection.None;
    private PlayerTooltipUI _tooltip;
    private RectTransform _pocketSlotGrid;
    private RectTransform _pocketSection;
    private RectTransform _inventorySection;
    private RectTransform _inventoryScrollView;
    private TMP_Text _pocketSectionHeader;
    private TMP_Text _inventorySectionHeader;
    private string _inventorySectionTitle;
    private float _displayedCarryWeight = -1f;
    private float _displayedCarryCapacity = -1f;
    private Vector2 _inventorySlotCellSize = new(InventorySplitLayout.CellSize, InventorySplitLayout.CellSize);
    private Vector2 _inventorySlotSpacing = new(InventorySplitLayout.CellGap, InventorySplitLayout.CellGap);
    private int _inventorySlotColumnCount = InventorySplitLayout.Columns;
    public PlayerInventoryRuntime InventoryRuntime => _inventoryRuntime;
    public InventoryItemSlotUI InventorySlotTemplate => _inventorySlotPrefab;
    public PlayerInventoryInteractionMode InteractionMode => _interactionMode;
    public int SelectedInventoryIndex => _selectedInventoryIndex;
    public PlayerInventoryItemStack SelectedItemStack => ResolveSelectedStack();
    public PlayerInventoryItemSelection SelectedItemSelection => _selectedItemSelection;

    public event Action<int, PlayerInventoryItemStack> OnInventorySelectionChanged;
    public event Action<PlayerInventoryItemSelection> OnItemSelectionChanged;
    /// <summary>
    /// Raised only while a transactional overlay is waiting for a player item.
    /// The source item remains in inventory until that overlay's Confirm step.
    /// </summary>
    public event Action<int, PlayerInventoryItemStack> OnInventoryTradeRequested;

    // Purpose: Initializes Inspector defaults for this component.
    private void Reset()
    {
        ResolveRefs();
    }

    // Purpose: Caches the component references required before runtime initialization.
    private void Awake()
    {
        ResolveRefs();
        ApplyCompactPanelLayout();
        InitializeStaticSlots();
        EnsurePassiveSlots();
        HideSlotTemplateIfNeeded();
        if (GetComponent<PlayerRecyclingPanelUI>() == null)
            gameObject.AddComponent<PlayerRecyclingPanelUI>().Initialize(this);
    }

    // Purpose: Reapplies the authored compact stack when this panel is edited inside Canvas-GamePlay.
    private void OnValidate()
    {
#if UNITY_EDITOR
        // OnValidate also runs during prefab import. Hierarchy/layout changes
        // must wait until the main editor loop and only affect scene instances.
        UnityEditor.EditorApplication.delayCall -= ApplyEditorPreview;
        if (!Application.isPlaying)
            UnityEditor.EditorApplication.delayCall += ApplyEditorPreview;
#endif
    }

#if UNITY_EDITOR
    private void ApplyEditorPreview()
    {
        if (this == null || UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode ||
            UnityEditor.EditorUtility.IsPersistent(this) || !gameObject.scene.IsValid() ||
            !gameObject.scene.isLoaded)
            return;

        ResolveRefs();
        EnsureSeparatedInventorySectionLayout();
        ApplyCompactPanelLayout();
    }
#endif

    // Purpose: Activates runtime subscriptions and refreshes the enabled state.
    private void OnEnable()
    {
        ResolveRefs();
        ApplyCompactPanelLayout();
        InitializeStaticSlots();
        EnsurePassiveSlots();
        HideSlotTemplateIfNeeded();

        if (_inventoryRuntimeReadyChannel != null)
        {
            _inventoryRuntimeReadyChannel.OnEventRaised += HandleInventoryRuntimeReady;

            if (_inventoryRuntimeReadyChannel.HasCurrent)
                HandleInventoryRuntimeReady(_inventoryRuntimeReadyChannel.Current);
        }
        else if (_inventoryRuntime != null)
        {
            _inventoryRuntime.OnChanged -= Refresh;
            _inventoryRuntime.OnChanged += Refresh;

            if (_quickSlots != null)
            {
                _quickSlots.OnChanged -= Refresh;
                _quickSlots.OnChanged += Refresh;
            }
        }

        SubscribePassiveRuntime();
        Refresh();
    }

    // Purpose: Releases active subscriptions and transient state while disabled.
    private void OnDisable()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.delayCall -= ApplyEditorPreview;
#endif
        HideContextMenu();
        HandleItemTooltipExited();

        if (_inventoryRuntime != null)
            _inventoryRuntime.OnChanged -= Refresh;

        if (_quickSlots != null)
            _quickSlots.OnChanged -= Refresh;

        UnsubscribePassiveRuntime();

        if (_inventoryRuntimeReadyChannel != null)
            _inventoryRuntimeReadyChannel.OnEventRaised -= HandleInventoryRuntimeReady;
    }

    // Purpose: Handles the incoming inventory slot left clicked request or event.
    public void HandleInventorySlotLeftClicked(int slotIndex)
    {
        HideContextMenu();

        if (_inventoryRuntime == null)
            return;

        if (_interactionMode == PlayerInventoryInteractionMode.TradeSelect)
        {
            if (TryGetInventoryStack(slotIndex, out PlayerInventoryItemStack tradeStack))
                OnInventoryTradeRequested?.Invoke(slotIndex, tradeStack);

            return;
        }

        if (_interactionMode == PlayerInventoryInteractionMode.SellSelect)
        {
            SelectInventorySlot(slotIndex);
            return;
        }

        if (TryGetInventoryStack(slotIndex, out PlayerInventoryItemStack keyToolStack) &&
            keyToolStack.Item is KeyToolItemSO keyTool)
        {
            if (KeyToolContainerPanel.TryOpen(this, _inventoryRuntime, keyTool))
                RaiseMessage("Key Tool opened. Double-click a key to move it back to inventory.");
            else
                RaiseMessage("Could not open Key Tool.");

            return;
        }

        if (TryGetInventoryStack(slotIndex, out PlayerInventoryItemStack stack) &&
            stack.Item is PassiveItemSO passive)
        {
            if (_passiveRuntime == null)
            {
                RaiseMessage("Player passive runtime is missing.");
                return;
            }

            UIAudio.ReportResult(_passiveRuntime.TryEquipFirstAvailable(passive, out string passiveMessage), UIAudioCueType.Equip);
            RaiseMessage(passiveMessage);
            return;
        }

        if (UIAudio.ReportResult(_inventoryRuntime.TryQuickEquipInventoryItem(slotIndex, out string message), UIAudioCueType.Equip))
            RaiseMessage(message);
        else
            RaiseMessage(message);
    }

    // Purpose: Handles the incoming item tooltip entered request or event.
    public void HandleItemTooltipEntered(ItemSO item, RectTransform anchor)
    {
        if (item == null || anchor == null)
            return;

        PlayerTooltipUI tooltip = GetOrCreateTooltip();

        if (tooltip != null)
            tooltip.ShowItem(item, anchor, _tooltipSide, _tooltipGap);
    }

    // Purpose: Handles the incoming item tooltip exited request or event.
    public void HandleItemTooltipExited()
    {
        DestroyTooltip();
    }

    // Purpose: Handles the incoming inventory slot right clicked request or event.
    public void HandleInventorySlotRightClicked(int slotIndex, Vector2 screenPosition)
    {
        if (_debugInteractionLogs)
        {
            Debug.Log(
                $"[PlayerInventoryUI] Right click received. slot={slotIndex}, mode={_interactionMode}, runtimeNull={_inventoryRuntime == null}, contextMenuNull={_contextMenu == null}",
                this);
        }

        if (_interactionMode == PlayerInventoryInteractionMode.SellSelect ||
            _interactionMode == PlayerInventoryInteractionMode.TradeSelect)
            return;

        if (_inventoryRuntime == null || _contextMenu == null)
            return;

        if (!TryGetInventoryStack(slotIndex, out PlayerInventoryItemStack stack))
        {
            if (_debugInteractionLogs)
                Debug.Log($"[PlayerInventoryUI] Right click stack missing. slot={slotIndex}", this);

            return;
        }

        _contextMenu.ShowInventoryItem(
            this,
            slotIndex,
            stack.Item,
            stack.Amount,
            screenPosition);
    }

    // Purpose: Handles the incoming context menu equip inventory item request or event.
    public void HandleContextMenuEquipInventoryItem(int inventoryIndex)
    {
        if (_inventoryRuntime == null)
            return;

        if (UIAudio.ReportResult(_inventoryRuntime.TryQuickEquipInventoryItem(inventoryIndex, out string message), UIAudioCueType.Equip))
            RaiseMessage(message);
        else
            RaiseMessage(message);
    }

    // Purpose: Handles the incoming context menu use inventory item request or event.
    public void HandleContextMenuUseInventoryItem(int inventoryIndex)
    {
        if (_inventoryRuntime == null)
            return;

        if (_consumableUseRuntime == null)
            _consumableUseRuntime = EntityComponentResolver.Resolve<PlayerConsumableUseRuntime>(_inventoryRuntime);

        if (_consumableUseRuntime == null)
        {
            RaiseMessage("Consumable use runtime is missing.");
            return;
        }

        if (!TryGetInventoryStack(inventoryIndex, out PlayerInventoryItemStack stack) ||
            stack.Item is not ConsumableItemSO consumable)
        {
            RaiseMessage("This item cannot be used.");
            return;
        }

        _consumableUseRuntime.TryBeginUse(inventoryIndex, consumable, out string message);
        RaiseMessage(message);
    }

    // Purpose: Handles the incoming context menu discard inventory item request or event.
    public void HandleContextMenuDiscardInventoryItem(int inventoryIndex)
    {
        if (_inventoryRuntime == null)
            return;

        if (_inventoryRuntime.TryRemoveInventoryItem(
                inventoryIndex,
                0,
                out PlayerInventoryRemovedStack removed,
                out string message))
        {
            CompleteWorldDropOrRestore(removed, message);
        }
        else
        {
            RaiseMessage(message);
        }
    }

    // Purpose: Handles the incoming context menu split inventory item request or event.
    public void HandleContextMenuSplitInventoryItem(int inventoryIndex, int amount)
    {
        HideContextMenu();

        if (_inventoryRuntime == null)
            return;

        if (UIAudio.ReportResult(_inventoryRuntime.TrySplitInventoryItem(
                inventoryIndex,
                amount,
                out _,
                out string message), UIAudioCueType.Split))
        {
            RaiseMessage(message);
        }
        else
        {
            RaiseMessage(message);
        }
    }

 

    // Purpose: Handles the incoming context menu unequip equipment slot request or event.
    public void HandleContextMenuUnequipEquipmentSlot(PlayerInventoryEquipmentSlotKind slotKind)
    {
        HideContextMenu();

        if (_inventoryRuntime == null)
            return;

        int equipmentSlotIndex = slotKind == PlayerInventoryEquipmentSlotKind.Weapon
            ? _contextEquipmentWeaponSlotIndex
            : 0;

        if (UIAudio.ReportResult(_inventoryRuntime.TryUnequipToInventorySlot(
                slotKind,
                equipmentSlotIndex,
                -1,
                out string message), UIAudioCueType.Unequip))
        {
            RaiseMessage(message);
        }
        else
        {
            RaiseMessage(message);
        }
    }

    // Purpose: Handles the incoming context menu discard equipment slot request or event.
    public void HandleContextMenuDiscardEquipmentSlot(PlayerInventoryEquipmentSlotKind slotKind)
    {
        HideContextMenu();

        if (_inventoryRuntime == null)
            return;

        int equipmentSlotIndex = slotKind == PlayerInventoryEquipmentSlotKind.Weapon
            ? _contextEquipmentWeaponSlotIndex
            : 0;

        if (_inventoryRuntime.TryRemoveEquipped(
                slotKind,
                equipmentSlotIndex,
                out PlayerInventoryRemovedStack removed,
                out string message))
        {
            CompleteWorldDropOrRestore(removed, message);
        }
        else
        {
            RaiseMessage(message);
        }
    }

    // Purpose: Handles the incoming context menu unequip ammo slot request or event.
    public void HandleContextMenuUnequipAmmoSlot(int ammoSlotIndex)
    {
        HideContextMenu();

        if (_inventoryRuntime == null)
            return;

        if (UIAudio.ReportResult(_inventoryRuntime.TryUnequipAmmoSlotToInventorySlot(
                ammoSlotIndex,
                -1,
                out string message), UIAudioCueType.Unequip))
        {
            RaiseMessage(message);
        }
        else
        {
            RaiseMessage(message);
        }
    }

    // Purpose: Handles the incoming context menu discard ammo slot request or event.
    public void HandleContextMenuDiscardAmmoSlot(int ammoSlotIndex)
    {
        HideContextMenu();

        if (_inventoryRuntime == null)
            return;

        if (_inventoryRuntime.TryRemoveAmmoSlot(
                ammoSlotIndex,
                out PlayerInventoryRemovedStack removed,
                out string message))
        {
            CompleteWorldDropOrRestore(removed, message);
        }
        else
        {
            RaiseMessage(message);
        }
    }

    // Purpose: Handles the incoming context menu split ammo slot request or event.
    public void HandleContextMenuSplitAmmoSlot(int ammoSlotIndex, int amount)
    {
        HideContextMenu();

        if (_inventoryRuntime == null)
            return;

        if (UIAudio.ReportResult(_inventoryRuntime.TrySplitAmmoSlotToInventory(
                ammoSlotIndex,
                amount,
                out _,
                out string message), UIAudioCueType.Split))
        {
            RaiseMessage(message);
        }
        else
        {
            RaiseMessage(message);
        }
    }

    // Purpose: Handles the incoming equipment slot right clicked request or event.
    public void HandleEquipmentSlotRightClicked(
        PlayerInventoryEquipmentSlotKind slotKind,
        Vector2 screenPosition)
    {
        if (_interactionMode == PlayerInventoryInteractionMode.SellSelect)
            return;

        if (_inventoryRuntime == null || _contextMenu == null)
            return;

        ItemSO item = slotKind switch
        {
            PlayerInventoryEquipmentSlotKind.Weapon => _inventoryRuntime.EquippedWeapon,
            PlayerInventoryEquipmentSlotKind.Armor => _inventoryRuntime.EquippedArmor,
            PlayerInventoryEquipmentSlotKind.Head => _inventoryRuntime.EquippedGasMask,
            _ => null
        };

        if (item == null)
            return;

        _contextEquipmentWeaponSlotIndex = 0;
        _contextMenu.ShowEquipmentSlot(this, slotKind, item, screenPosition);
    }

    // Purpose: Handles the incoming weapon slot right clicked request or event.
    public void HandleWeaponSlotRightClicked(int weaponSlotIndex, Vector2 screenPosition)
    {
        if (_interactionMode == PlayerInventoryInteractionMode.SellSelect)
            return;

        if (_inventoryRuntime == null || _contextMenu == null)
            return;

        ItemSO item = _inventoryRuntime.GetEquippedWeapon(weaponSlotIndex);

        if (item == null)
            return;

        _contextEquipmentWeaponSlotIndex = Mathf.Max(0, weaponSlotIndex);
        _contextMenu.ShowEquipmentSlot(
            this,
            PlayerInventoryEquipmentSlotKind.Weapon,
            item,
            screenPosition);
    }

    // Purpose: Handles the incoming inventory slot dropped on inventory request or event.
    public void HandleInventorySlotDroppedOnInventory(int fromIndex, int toIndex)
    {
        if (fromIndex == toIndex) return;
        HideContextMenu();

        if (_inventoryRuntime == null)
            return;

        if (UIAudio.ReportResult(_inventoryRuntime.TryMoveInventoryItem(fromIndex, toIndex, out string message), UIAudioCueType.InventoryMove))
            RaiseMessage(message);
        else
            RaiseMessage(message);
    }

    // Purpose: Handles the incoming inventory slot dropped on equipment request or event.
    public void HandleInventorySlotDroppedOnEquipment(
        int inventoryIndex,
        PlayerInventoryEquipmentSlotKind slotKind)
    {
        HideContextMenu();

        if (_inventoryRuntime == null)
            return;

        if (UIAudio.ReportResult(_inventoryRuntime.TryEquipInventoryItemToSlot(inventoryIndex, slotKind, out string message), UIAudioCueType.Equip))
            RaiseMessage(message);
        else
            RaiseMessage(message);
    }

    // Purpose: Handles the incoming inventory slot dropped on weapon slot request or event.
    public void HandleInventorySlotDroppedOnWeaponSlot(int inventoryIndex, int weaponSlotIndex)
    {
        HideContextMenu();

        if (_inventoryRuntime == null)
            return;

        if (UIAudio.ReportResult(_inventoryRuntime.TryEquipInventoryItemToSlot(
                inventoryIndex,
                PlayerInventoryEquipmentSlotKind.Weapon,
                weaponSlotIndex,
                out string message), UIAudioCueType.Equip))
        {
            RaiseMessage(message);
        }
        else
        {
            RaiseMessage(message);
        }
    }

    // Purpose: Handles the incoming weapon slot dropped on weapon slot request or event.
    public void HandleWeaponSlotDroppedOnWeaponSlot(
        int fromWeaponSlotIndex,
        int toWeaponSlotIndex)
    {
        HideContextMenu();

        if (_inventoryRuntime == null)
            return;

        if (UIAudio.ReportResult(_inventoryRuntime.TrySwapWeaponSlots(
                fromWeaponSlotIndex,
                toWeaponSlotIndex,
                out string message), UIAudioCueType.InventoryMove))
        {
            RaiseMessage(message);
        }
        else
        {
            RaiseMessage(message);
        }
    }

    // Purpose: Handles the incoming equipment slot left clicked request or event.
    public void HandleEquipmentSlotLeftClicked(PlayerInventoryEquipmentSlotKind slotKind)
    {
        HideContextMenu();

        if (_interactionMode != PlayerInventoryInteractionMode.SellSelect)
            return;

        SelectEquipmentSlot(slotKind);
    }

    // Purpose: Handles the incoming weapon slot left clicked request or event.
    public void HandleWeaponSlotLeftClicked(int weaponSlotIndex)
    {
        HideContextMenu();

        if (_interactionMode != PlayerInventoryInteractionMode.SellSelect)
            return;

        SelectWeaponSlot(weaponSlotIndex);
    }

    // Purpose: Handles the incoming equipment slot dropped on inventory request or event.
    public void HandleEquipmentSlotDroppedOnInventory(
        PlayerInventoryEquipmentSlotKind slotKind,
        int targetInventoryIndex)
    {
        HideContextMenu();

        if (_inventoryRuntime == null)
            return;

        int equipmentSlotIndex = slotKind == PlayerInventoryEquipmentSlotKind.Weapon
            ? InventoryDragContext.EquipmentSlotIndex
            : 0;

        if (UIAudio.ReportResult(_inventoryRuntime.TryUnequipToInventorySlot(
                slotKind,
                equipmentSlotIndex,
                targetInventoryIndex,
                out string message), UIAudioCueType.Unequip))
        {
            RaiseMessage(message);
        }
        else
        {
            RaiseMessage(message);
        }
    }

    // Purpose: Handles the incoming inventory slot dropped on ammo request or event.
    public void HandleInventorySlotDroppedOnAmmo(int inventoryIndex, int ammoSlotIndex)
    {
        HideContextMenu();

        if (_inventoryRuntime == null)
            return;

        if (UIAudio.ReportResult(_inventoryRuntime.TryEquipInventoryBulletToAmmoSlot(
                inventoryIndex,
                ammoSlotIndex,
                out string message), UIAudioCueType.Equip))
        {
            RaiseMessage(message);
        }
        else
        {
            RaiseMessage(message);
        }
    }

    // Purpose: Handles the incoming inventory slot dropped on passive request or event.
    public void HandleInventorySlotDroppedOnPassive(
        int inventoryIndex,
        int passiveSlotIndex)
    {
        HideContextMenu();

        if (_passiveRuntime == null)
        {
            RaiseMessage("Player passive runtime is missing.");
            return;
        }

        UIAudio.ReportResult(_passiveRuntime.TryEquipFromInventory(
            inventoryIndex,
            passiveSlotIndex,
            out string message), UIAudioCueType.Equip);
        RaiseMessage(message);
    }

    // Purpose: Handles the incoming passive slot left clicked request or event.
    public void HandlePassiveSlotLeftClicked(int passiveSlotIndex)
    {
        HideContextMenu();

        if (_interactionMode == PlayerInventoryInteractionMode.SellSelect)
            return;

        if (_passiveRuntime == null)
        {
            RaiseMessage("Player passive runtime is missing.");
            return;
        }

        UIAudio.ReportResult(_passiveRuntime.TryUnequip(passiveSlotIndex, out string message), UIAudioCueType.Unequip);
        RaiseMessage(message);
    }

    // Purpose: Handles the incoming passive slot right clicked request or event.
    public void HandlePassiveSlotRightClicked(int passiveSlotIndex)
    {
        if (_passiveRuntime == null || _inventoryRuntime == null || _contextMenu == null ||
            !_passiveRuntime.TryGetEquippedPassive(passiveSlotIndex, out var passive)) return;
        for (int i = 0; i < _inventoryRuntime.Items.Count; i++)
            if (_inventoryRuntime.Items[i]?.Item == passive)
            {
                _contextMenu.ShowInventoryItem(this, i, passive, 1,
                    UnityEngine.InputSystem.Mouse.current != null ? UnityEngine.InputSystem.Mouse.current.position.ReadValue() : Vector2.zero);
                break;
            }
    }

    public void HandlePassiveModuleAction(int inventoryIndex, bool reroll)
    {
        if (_inventoryRuntime == null) return;
        string message;
        bool success = reroll ? _inventoryRuntime.TryRerollPassive(inventoryIndex, out message)
            : _inventoryRuntime.TryDismantlePassive(inventoryIndex, out message);
        UIAudio.ReportResult(success, UIAudioCueType.InventoryMove);
        RaiseMessage(message);
    }

    // Purpose: Handles the incoming ammo slot dropped on inventory request or event.
    public void HandleAmmoSlotDroppedOnInventory(int ammoSlotIndex, int targetInventoryIndex)
    {
        HideContextMenu();

        if (_inventoryRuntime == null)
            return;

        if (UIAudio.ReportResult(_inventoryRuntime.TryUnequipAmmoSlotToInventorySlot(
                ammoSlotIndex,
                targetInventoryIndex,
                out string message), UIAudioCueType.Unequip))
        {
            RaiseMessage(message);
        }
        else
        {
            RaiseMessage(message);
        }
    }

    // Purpose: Handles the incoming ammo slot dropped on ammo request or event.
    public void HandleAmmoSlotDroppedOnAmmo(int fromAmmoSlotIndex, int toAmmoSlotIndex)
    {
        if (fromAmmoSlotIndex == toAmmoSlotIndex) return;
        HideContextMenu();

        if (_inventoryRuntime == null)
            return;

        if (UIAudio.ReportResult(_inventoryRuntime.TrySwapAmmoSlots(
                fromAmmoSlotIndex,
                toAmmoSlotIndex,
                out string message), UIAudioCueType.InventoryMove))
        {
            RaiseMessage(message);
        }
        else
        {
            RaiseMessage(message);
        }
    }

    // Purpose: Handles the incoming ammo slot left clicked request or event.
    public void HandleAmmoSlotLeftClicked(int ammoSlotIndex)
    {
        HideContextMenu();

        if (_interactionMode != PlayerInventoryInteractionMode.SellSelect)
            return;

        SelectAmmoSlot(ammoSlotIndex);
    }

    // Purpose: Handles the incoming ammo slot right clicked request or event.
    public void HandleAmmoSlotRightClicked(int ammoSlotIndex, Vector2 screenPosition)
    {
        if (_interactionMode == PlayerInventoryInteractionMode.SellSelect)
            return;

        if (_inventoryRuntime == null || _contextMenu == null)
            return;

        if (!_inventoryRuntime.TryGetAmmoSlotSnapshot(
                ammoSlotIndex,
                out WeaponAmmoSlotSnapshot snapshot))
        {
            return;
        }

        if (snapshot.isEmpty || snapshot.bullet == null)
            return;

        _contextMenu.ShowAmmoSlot(
            this,
            ammoSlotIndex,
            snapshot.bullet,
            Mathf.Max(0, snapshot.totalAmmo),
            screenPosition);
    }

    // Purpose: Handles the incoming quick slot left clicked request or event.
    public void HandleQuickSlotLeftClicked(int quickSlotIndex)
    {
        HideContextMenu();

        if (_quickSlots == null)
        {
            RaiseMessage("Player quick-slot runtime is missing.");
            return;
        }

        if (!_quickSlots.Activate(quickSlotIndex, out string message))
            RaiseMessage(message);
    }

    // Purpose: Handles the incoming quick slot right clicked request or event.
    public void HandleQuickSlotRightClicked(int quickSlotIndex)
    {
        HideContextMenu();

        if (_quickSlots == null)
            return;

        UIAudio.ReportResult(_quickSlots.TryUnequipQuickSlot(quickSlotIndex, out string message), UIAudioCueType.Unequip);
        RaiseMessage(message);
    }

    // Purpose: Handles the incoming quick slot double clicked request or event.
    public void HandleQuickSlotDoubleClicked(int quickSlotIndex)
    {
        HandleQuickSlotRightClicked(quickSlotIndex);
    }

    // Purpose: Handles the incoming quick slot dropped on inventory request or event.
    public void HandleQuickSlotDroppedOnInventory(
        int quickSlotIndex,
        int targetInventoryIndex)
    {
        HideContextMenu();

        if (_quickSlots == null)
            return;

        UIAudio.ReportResult(_quickSlots.TryUnequipQuickSlot(
            quickSlotIndex,
            targetInventoryIndex,
            out string message), UIAudioCueType.Unequip);
        RaiseMessage(message);
    }

    // Purpose: Handles the incoming quick slot drop request or event.
    public void HandleQuickSlotDrop(int targetQuickSlotIndex)
    {
        if (_quickSlots == null || !InventoryDragContext.HasPayload)
            return;

        bool success = false;
        string message;

        switch (InventoryDragContext.SourceKind)
        {
            case InventoryDragSourceKind.QuickSlot:
                success = UIAudio.ReportResult(_quickSlots.SwapBindings(
                    InventoryDragContext.QuickSlotIndex,
                    targetQuickSlotIndex,
                    out message), UIAudioCueType.InventoryMove);
                break;

            case InventoryDragSourceKind.Inventory:
                success = UIAudio.ReportResult(_quickSlots.TryAssignInventoryItem(
                    InventoryDragContext.InventoryIndex,
                    targetQuickSlotIndex,
                    out message), UIAudioCueType.Equip);
                break;

            case InventoryDragSourceKind.Equipment:
                message = "Only usable inventory items can be moved to quick slots.";
                break;

            case InventoryDragSourceKind.EquipmentAmmo:
                message = "Ammo is selected automatically. Press T to cycle compatible ammo.";
                break;

            case InventoryDragSourceKind.EnemyInventory:
                message = "Move the item to the player inventory before binding it.";
                break;

            default:
                message = "This item cannot be bound to a quick slot.";
                break;
        }

        if (!string.IsNullOrWhiteSpace(message))
            RaiseMessage(message);

        if (success)
            Refresh();
        else
            UIAudio.Request(UIAudioCueType.Denied);
    }

    // Purpose: Handles the incoming equipment slot double clicked request or event.
    public void HandleEquipmentSlotDoubleClicked(
        PlayerInventoryEquipmentSlotKind slotKind,
        int equipmentSlotIndex = 0)
    {
        HideContextMenu();

        if (_inventoryRuntime == null)
            return;

        UIAudio.ReportResult(_inventoryRuntime.TryUnequipToInventorySlot(
            slotKind,
            equipmentSlotIndex,
            -1,
            out string message), UIAudioCueType.Unequip);
        RaiseMessage(message);
    }

    // Purpose: Handles the incoming ammo slot double clicked request or event.
    public void HandleAmmoSlotDoubleClicked(int ammoSlotIndex)
    {
        HideContextMenu();

        if (_inventoryRuntime == null)
            return;

        UIAudio.ReportResult(_inventoryRuntime.TryUnequipAmmoSlotToInventorySlot(
            ammoSlotIndex,
            -1,
            out string message), UIAudioCueType.Unequip);
        RaiseMessage(message);
    }

    // Purpose: Refreshes the displayed or cached refresh.
    public void Refresh()
    {
        if (_inventoryRuntime == null)
        {
            ClearBoundSlots();
            return;
        }

        EnsureInventorySlots();

        if (_armorSlot != null)
        {
            _armorSlot.Bind(
                _inventoryRuntime.EquippedArmor,
                _inventoryRuntime.EquippedArmorDurability,
                _inventoryRuntime.EquippedArmorMaxDurability);
        }

        if (_headSlot != null)
        {
            _headSlot.Bind(
                _inventoryRuntime.EquippedGasMask,
                _inventoryRuntime.EquippedGasMaskFilterDurability,
                _inventoryRuntime.EquippedGasMaskMaxFilterDurability);
        }

        RefreshEquipmentSlots();
        RefreshPassiveSlots();

        IReadOnlyList<PlayerInventoryItemStack> items = _inventoryRuntime.Items;

        for (int i = 0; i < _inventorySlots.Count; i++)
        {
            InventoryItemSlotUI slot = _inventorySlots[i];

            if (slot == null)
                continue;

            bool active = i < _inventoryRuntime.Capacity;
            slot.gameObject.SetActive(active);

            if (!active)
                continue;

            PlayerInventoryItemStack stack =
                items != null && i < items.Count
                    ? items[i]
                    : null;

            slot.Bind(stack);
        }

        ValidateSelection();
    }

    // Purpose: Updates the interaction mode state.
    public void SetInteractionMode(PlayerInventoryInteractionMode mode)
    {
        if (_interactionMode == mode)
            return;

        _interactionMode = mode;
        HideContextMenu();
        ConfigureInventorySlots();

        if (_interactionMode != PlayerInventoryInteractionMode.SellSelect)
            ClearSelectedItemSelection();
    }

    // Purpose: Attempts to get selected inventory stack and reports whether the request succeeded.
    public bool TryGetSelectedInventoryStack(out int slotIndex, out PlayerInventoryItemStack stack)
    {
        slotIndex = _selectedInventoryIndex;
        stack = ResolveSelectedStack();
        return stack != null && !stack.IsEmpty;
    }

    // Purpose: Attempts to get selected item selection and reports whether the request succeeded.
    public bool TryGetSelectedItemSelection(out PlayerInventoryItemSelection selection)
    {
        selection = _selectedItemSelection;
        return selection.HasItem;
    }

    // Purpose: Clears the current selected inventory slot state.
    public void ClearSelectedInventorySlot()
    {
        ClearSelectedItemSelection();
    }

    // Purpose: Closes the active context menu UI or workflow.
    public void CloseContextMenu()
    {
        HideContextMenu();
    }

    // Purpose: Clears the current selected item selection state.
    public void ClearSelectedItemSelection()
    {
        if (_selectedInventoryIndex < 0 && !_selectedItemSelection.HasItem)
            return;

        _selectedInventoryIndex = -1;
        _selectedItemSelection = PlayerInventoryItemSelection.None;
        OnInventorySelectionChanged?.Invoke(_selectedInventoryIndex, null);
        OnItemSelectionChanged?.Invoke(_selectedItemSelection);
    }

    // Purpose: Resolves the item name needed by this component.
    public static string ResolveItemName(ItemSO item)
    {
        return InventoryItemDisplayUtility.ResolveItemName(item);
    }

    // Purpose: Initializes static slots for use by this component.
    private void InitializeStaticSlots()
    {
        if (_contextMenu != null)
            _contextMenu.Initialize(this);

        if (_armorSlot != null)
            _armorSlot.Initialize(this);

        EnsureHeadSlot();

        if (_headSlot != null)
            _headSlot.Initialize(this);

        InitializeEquipmentSlots();

        if (_ammoSlots != null)
        {
            for (int i = 0; i < _ammoSlots.Length; i++)
            {
                EquipmentInventoryAmmoSlotUI slot = _ammoSlots[i];
                if (slot == null)
                    continue;

                // Equipment and SlotHUD are two views of the same Quick 3-5
                // runtime bindings.
                slot.gameObject.SetActive(true);
                slot.InitializeQuickSlot(this, 2 + i);
            }
        }

        EnsurePassiveSlots();
    }

    // Purpose: Initializes equipment slots for use by this component.
    private void InitializeEquipmentSlots()
    {
        if (_weaponSlots == null)
            return;

        for (int i = 0; i < _weaponSlots.Length; i++)
        {
            if (_weaponSlots[i] != null)
                _weaponSlots[i].Initialize(this);
        }
    }

    // Purpose: Refreshes the displayed or cached equipment slots.
    private void RefreshEquipmentSlots()
    {
        if (_weaponSlots != null)
        {
            for (int i = 0; i < _weaponSlots.Length; i++)
            {
                EquipmentInventoryWeaponSlotUI slot = _weaponSlots[i];
                if (slot != null)
                {
                    slot.Bind(
                        _inventoryRuntime.GetEquippedWeapon(i),
                        _inventoryRuntime.GetEquippedWeaponCurrentDurability(i),
                        _inventoryRuntime.GetEquippedWeaponMaxDurability(i));
                }
            }
        }

        if (_ammoSlots != null)
        {
            for (int i = 0; i < _ammoSlots.Length; i++)
            {
                EquipmentInventoryAmmoSlotUI slot = _ammoSlots[i];
                if (slot == null)
                    continue;

                slot.gameObject.SetActive(true);

                PlayerQuickSlotSnapshot snapshot =
                    _quickSlots != null &&
                    _quickSlots.TryGetSnapshot(2 + i, out PlayerQuickSlotSnapshot resolved)
                        ? resolved
                        : new PlayerQuickSlotSnapshot(
                            2 + i,
                            PlayerQuickSlotBindingKind.None,
                            null,
                            0,
                            false,
                            0f,
                            0f,
                            default);

                slot.BindQuickSlot(snapshot);
            }
        }
    }

    // Purpose: Ensures passive slots is ready before the workflow continues.
    private void EnsurePassiveSlots()
    {
        if (_passiveSlots == null)
            return;

        for (int i = 0; i < _passiveSlots.Length; i++)
        {
            PassiveInventorySlotUI slot = _passiveSlots[i];

            if (slot == null)
                continue;

            slot.gameObject.SetActive(true);
            slot.InitializePassiveSlot(this, i);
        }
    }

    // Purpose: Refreshes the displayed or cached passive slots.
    private void RefreshPassiveSlots()
    {
        EnsurePassiveSlots();

        if (_passiveSlots == null)
            return;

        int unlockedCount = _passiveRuntime != null
            ? _passiveRuntime.UnlockedSlotCount
            : PlayerPassiveInventoryRuntime.MinimumUnlockedSlotCount;

        for (int i = 0; i < _passiveSlots.Length; i++)
        {
            PassiveInventorySlotUI slot = _passiveSlots[i];

            if (slot == null)
                continue;

            bool unlocked = _passiveRuntime != null ? _passiveRuntime.IsSlotUnlocked(i) : i < 2;
            PassiveItemSO item = null;

            if (unlocked && _passiveRuntime != null)
                _passiveRuntime.TryGetEquippedPassive(i, out item);

            slot.gameObject.SetActive(true);
            slot.BindPassive(item, unlocked);
        }
    }

    /// <summary>
    /// Keeps the approved split layout: authored passives/equipment left, six-column
    /// pocket/inventory right. The inventory viewport shows five rows, not five rows of new capacity.
    /// </summary>
    public void ApplyCompactPanelLayout()
    {
        var panelRoot=transform as RectTransform;
        var playerSection=FindDirectChildRect(transform,"PlayerInventorySection");
        var passiveSection=FindDirectChildRect(transform,"PassiveSection");
        if(panelRoot==null||playerSection==null)return;
        panelRoot.sizeDelta=InventorySplitLayout.PanelSize;
        playerSection.SetAsFirstSibling();StretchRect(playerSection,Vector2.zero,Vector2.zero);
        var area=FindDirectChildRect(playerSection,"InventoryArea");
        area?.SetAsFirstSibling();StretchRect(area,Vector2.zero,Vector2.zero);
        var header=FindDirectChildRect(playerSection,"HeaderSection");
        AnchorTopRect(header,18f,54f,InventorySplitLayout.Outer);
        InventorySplitLayout.Box(passiveSection,24f,84f,260f,InventorySplitLayout.PassiveHeight);
        if(passiveSection!=null)
        {
            passiveSection.SetAsLastSibling();
            AnchorTopRect(FindDirectChildRect(passiveSection,"PassiveTitle"),0f,28f,0f);
            var row=FindDirectChildRect(passiveSection,"PassiveSection");
            StretchRect(row,new Vector2(0f,8f),new Vector2(0f,-28f));
            if(row!=null&&row.TryGetComponent(out HorizontalLayoutGroup layout))layout.enabled=false;
            if(_passiveSlots!=null)for(int i=0;i<_passiveSlots.Length;i++)InventorySplitLayout.PassiveSlot(_passiveSlots[i],i);
        }
        var equipment=FindDirectChildRect(area,"EquipmentSection");
        InventorySplitLayout.Box(equipment,24f,InventorySplitLayout.EquipmentTop,260f,InventorySplitLayout.EquipmentHeight);
        AnchorTopRect(FindDirectChildRect(equipment,"Header"),0f,28f,0f);
        var body=FindDirectChildRect(equipment,"Body");
        StretchRect(body,Vector2.zero,new Vector2(0f,-28f));
        InventorySplitLayout.EquipmentBody(body);
        InventorySplitLayout.EquipmentSlot(_armorSlot,0);InventorySplitLayout.EquipmentSlot(_headSlot,1);
        if(_weaponSlots!=null)for(int i=0;i<_weaponSlots.Length;i++)InventorySplitLayout.EquipmentSlot(_weaponSlots[i],2+i);
        if(_ammoSlots!=null)for(int i=0;i<_ammoSlots.Length;i++)InventorySplitLayout.EquipmentSlot(_ammoSlots[i],4+i);
        RefreshInventorySectionLayout();
    }

    // Purpose: Locates a directly authored RectTransform without adding serialized UI refs.
    private static RectTransform FindDirectChildRect(Transform parent, string childName)
    {
        return parent != null ? parent.Find(childName) as RectTransform : null;
    }

    // Purpose: Makes one authored rect fill its parent while preserving explicit offsets.
    private static void StretchRect(RectTransform rect, Vector2 offsetMin, Vector2 offsetMax)
    {
        if (rect == null)
            return;

        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
    }

    // Purpose: Positions one authored rect as a full-width top strip.
    private static void AnchorTopRect(RectTransform rect, float topInset, float height, float sideInset)
    {
        if (rect == null)
            return;

        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(0f, -topInset);
        rect.sizeDelta = new Vector2(-sideInset * 2f, height);
    }

    // Purpose: Ensures inventory slots is ready before the workflow continues.
    private void EnsureInventorySlots()
    {
        if (_inventoryRuntime == null || _inventorySlotPrefab == null)
            return;

        if (_inventorySlotRoot == null && _inventorySlotPrefab.transform.parent != null)
            _inventorySlotRoot = _inventorySlotPrefab.transform.parent;

        if (_inventorySlotRoot == null)
            return;

        EnsureSeparatedInventorySectionLayout();

        int capacity = _inventoryRuntime.Capacity;

        while (_inventorySlots.Count < capacity)
        {
            InventoryItemSlotUI slot = Instantiate(_inventorySlotPrefab, _inventorySlotRoot);
            slot.name = GetInventorySlotName(_inventorySlots.Count);
            _inventorySlots.Add(slot);
        }

        for (int i = 0; i < _inventorySlots.Count; i++)
        {
            InventoryItemSlotUI slot = _inventorySlots[i];

            if (slot == null)
                continue;

            ConfigureInventorySlot(slot, i);
            slot.name = GetInventorySlotName(i);
            MoveSlotToStorageSection(slot, i);
            slot.gameObject.SetActive(i < capacity);
        }

        RefreshInventorySectionLayout();
    }

    // Purpose: Connects the authored Pocket and Inventory sections to one authoritative inventory runtime.
    private void EnsureSeparatedInventorySectionLayout()
    {
        if (!_useSeparatedPocketAndInventorySections || _inventorySlotRoot == null ||
            !_inventorySlotRoot.IsChildOf(transform))
            return;

        if (_inventorySlotRoot.TryGetComponent(out GridLayoutGroup authoredGrid))
        {
            _inventorySlotCellSize = new Vector2(InventorySplitLayout.CellSize, InventorySplitLayout.CellSize);
            _inventorySlotSpacing = new Vector2(InventorySplitLayout.CellGap, InventorySplitLayout.CellGap);
            _inventorySlotColumnCount = InventorySplitLayout.Columns;
            authoredGrid.cellSize = _inventorySlotCellSize;
            authoredGrid.spacing = _inventorySlotSpacing;
            authoredGrid.childAlignment = TextAnchor.UpperLeft;
            authoredGrid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            authoredGrid.constraintCount = _inventorySlotColumnCount;
            authoredGrid.enabled = true;
        }

        if (_inventorySlotRoot.TryGetComponent(out ContentSizeFitter authoredFitter))
            authoredFitter.enabled = true;

        Transform inventorySection = _inventorySlotRoot.parent != null &&
                                     _inventorySlotRoot.parent.parent != null &&
                                     _inventorySlotRoot.parent.parent.parent != null
            ? _inventorySlotRoot.parent.parent.parent
            : null;
        _inventorySection ??= inventorySection as RectTransform;

        if (_inventorySection == null)
            return;

        _pocketSection ??= _inventorySection.parent != null
            ? _inventorySection.parent.Find("PocketSection") as RectTransform
            : null;
        _inventoryScrollView ??= _inventorySection.Find("InventoryScrollView") as RectTransform;

        if (_pocketSection == null)
            return;

        DisableObsoleteRuntimeStorageObjects();

        Transform oldPocketBody = _pocketSection.Find("Body");

        if (oldPocketBody != null)
            oldPocketBody.gameObject.SetActive(false);

        _pocketSectionHeader ??= CreateOrFindStorageSectionHeader(
            _pocketSection,
            UIRuntimeLocalization.Get("UI_Inventory", "Inventory_Pocket", "Pocket"));
        _inventorySectionHeader ??= CreateOrFindStorageSectionHeader(
            _inventorySection,
            UIRuntimeLocalization.Get("UI_Inventory", "Inventory_Title", "Inventory"));
        _pocketSlotGrid ??= FindOrCreatePocketSlotGrid(_pocketSection);
        ConfigureStorageGrid(_pocketSlotGrid);
        RefreshSortButtons(_pocketSection);
        RefreshSortButtons(_inventorySection);
    }

    // Buttons are authored in PlayerInventoryPanel.prefab, inherited by
    // Canvas-GamePlay. Never spawn UI into whichever scene happens to be active.
    private void RefreshSortButtons(RectTransform section)
    {
        if (section == null || !section.IsChildOf(transform))
            return;

        Transform header = section.Find("Header");
        if (header == null)
            return;

        for (int i = 0; i < 3; i++)
        {
            bool rarity = i == 1;
            string buttonName = i == 2 ? "SortCategory" : rarity ? "SortRarity" : "SortCompact";
            Transform existing = header.Find(buttonName);
            Button button = existing != null ? existing.GetComponent<Button>() : null;
            if (button == null) continue;

            // The prefab's persistent OnClick points to SortPlayerInventory(bool).
            // Only presentation/state is refreshed here, including after domain reload.
            TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
            if (label != null)
                label.text = i == 2 ? "분류" : UIRuntimeLocalization.Get("UI_Inventory",
                    rarity ? "Inventory_SortRarity" : "Inventory_Compact", rarity ? "Rarity" : "Compact");
            button.interactable = _interactionMode == PlayerInventoryInteractionMode.Normal;
        }
    }

    public void SortPlayerInventory(bool byRarity)
        => SortPlayerInventoryMode(byRarity ? InventorySortMode.Rarity : InventorySortMode.Compact);

    public void SortPlayerInventoryByCategory() => SortPlayerInventoryMode(InventorySortMode.Category);

    private void SortPlayerInventoryMode(InventorySortMode mode)
    {
        if (_inventoryRuntime == null || _interactionMode != PlayerInventoryInteractionMode.Normal ||
            InventoryDragContext.HasPayload) return;
        _contextMenu?.Hide();
        ClearSelectedItemSelection();
        if (_inventoryRuntime.SortInventory(mode)) SaveSystem.Active?.TrySaveRunDataToDisk();
    }

    // Purpose: Hides dynamic storage objects from older panel versions so they cannot overlap the authored section layout.
    private void DisableObsoleteRuntimeStorageObjects()
    {
        string[] obsoleteObjectNames = { "PocketHeader", "InventoryHeader", "PocketSlots", "InventorySlots" };

        for (int i = 0; i < obsoleteObjectNames.Length; i++)
        {
            Transform obsolete = _inventorySlotRoot.Find(obsoleteObjectNames[i]);

            if (obsolete != null)
                obsolete.gameObject.SetActive(false);
        }
    }

    // Purpose: Adds the runtime title to an existing authored section bar without replacing its background or framing.
    private TMP_Text CreateOrFindStorageSectionHeader(RectTransform section, string title)
    {
        Transform headerRoot = section != null ? section.Find("Header") : null;

        // Prefer the authored TMP label already inside the section header. This
        // keeps edit-mode and runtime visuals identical and avoids creating a
        // second title on top of the pre-authored one.
        TMP_Text authoredTitle = headerRoot != null
            ? headerRoot.GetComponentInChildren<TMP_Text>(true)
            : null;
        if(headerRoot!=null)
            foreach(var label in headerRoot.GetComponentsInChildren<TMP_Text>(true))
                if(label.name is "PocketTitle" or "InventoryTitle") { authoredTitle=label;break; }

        if (authoredTitle != null)
        {
            authoredTitle.text = title;
            return authoredTitle;
        }

        Transform titleRoot = headerRoot != null ? headerRoot.Find("RuntimeSectionTitle") : null;

        if (titleRoot != null && titleRoot.TryGetComponent(out TMP_Text existingTitle))
        {
            existingTitle.text = title;
            return existingTitle;
        }

        if (!Application.isPlaying)
            return null;

        GameObject headerObject = new(
            "RuntimeSectionTitle",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(TextMeshProUGUI));
        headerObject.layer = gameObject.layer;
        headerObject.transform.SetParent(headerRoot != null ? headerRoot : section, false);

        RectTransform headerTransform = (RectTransform)headerObject.transform;
        headerTransform.anchorMin = Vector2.zero;
        headerTransform.anchorMax = Vector2.one;
        headerTransform.offsetMin = new Vector2(12f, 0f);
        headerTransform.offsetMax = new Vector2(-12f, 0f);

        TextMeshProUGUI header = headerObject.GetComponent<TextMeshProUGUI>();
        header.font = TMP_Settings.defaultFontAsset;
        header.fontSize = 16f;
        header.fontStyle = FontStyles.Bold;
        header.alignment = TextAlignmentOptions.Left | TextAlignmentOptions.Midline;
        header.color = new Color(0.55f, 0.93f, 1f, 0.95f);
        header.raycastTarget = false;
        header.text = title;
        return header;
    }

    // Purpose: Resolves the pre-authored Pocket grid or creates a safe fallback for panels created before that child existed.
    private RectTransform FindOrCreatePocketSlotGrid(RectTransform pocketSection)
    {
        // Authored in the prefab. Never create a second runtime pocket UI.
        return pocketSection.Find("PocketScrollView/Viewport/PocketGrid") as RectTransform
            ?? pocketSection.Find("PocketGrid") as RectTransform;
    }

    // Purpose: Applies the same compact six-column geometry to both storage grids.
    private void ConfigureStorageGrid(RectTransform gridTransform)
    {
        _inventorySlotCellSize=new Vector2(InventorySplitLayout.CellSize,InventorySplitLayout.CellSize);
        _inventorySlotSpacing=new Vector2(InventorySplitLayout.CellGap,InventorySplitLayout.CellGap);
        _inventorySlotColumnCount=InventorySplitLayout.Columns;
        InventorySplitLayout.Grid(gridTransform);
    }

    // Purpose: Moves one visual slot into its Pocket or equipped-backpack UI section without changing its absolute inventory index.
    private void MoveSlotToStorageSection(InventoryItemSlotUI slot, int slotIndex)
    {
        if (!_useSeparatedPocketAndInventorySections || slot == null)
            return;

        Transform target = slotIndex < _inventoryRuntime.PocketCapacity
            ? _pocketSlotGrid
            : _inventorySlotRoot;

        if (target != null && slot.transform.parent != target)
            slot.transform.SetParent(target, false);
    }

    // Purpose: Sizes and vertically stacks the authored Equipment, Pocket, and equipped-backpack Inventory sections.
    private void RefreshInventorySectionLayout(float equipmentTopInset=-1f,float explicitEquipmentHeight=-1f,float sideInset=-1f)
    {
        if(!_useSeparatedPocketAndInventorySections||_pocketSection==null||_inventorySection==null||
            _pocketSlotGrid==null||_pocketSectionHeader==null||_inventorySectionHeader==null)return;
        float width=Mathf.Max(0f,((RectTransform)transform).rect.width-InventorySplitLayout.StorageLeft-InventorySplitLayout.Outer);
        _pocketSection.gameObject.SetActive(true);_inventorySection.gameObject.SetActive(true);
        _pocketSectionHeader.text=UIRuntimeLocalization.Get("UI_Inventory","Inventory_Pocket","Pocket");
        _inventorySectionTitle=UIRuntimeLocalization.Get("UI_Inventory","Inventory_Title","Inventory");
        RefreshCarryWeightHeader(force: true);
        InventorySplitLayout.Box(_pocketSection,InventorySplitLayout.StorageLeft,InventorySplitLayout.ContentTop,width,InventorySplitLayout.PocketHeight);
        InventorySplitLayout.Box(_inventorySection,InventorySplitLayout.StorageLeft,InventorySplitLayout.InventoryTop,width,InventorySplitLayout.InventoryHeight);
        AnchorTopRect(_pocketSection.Find("Header") as RectTransform,0f,28f,0f);
        AnchorTopRect(_inventorySection.Find("Header") as RectTransform,0f,28f,0f);
        var pocketScroll=_pocketSection.Find("PocketScrollView") as RectTransform;
        InventorySplitLayout.Scroll(pocketScroll,_pocketSlotGrid);
        InventorySplitLayout.Scroll(_inventoryScrollView,_inventorySlotRoot as RectTransform);
    }

    private void LateUpdate()
    {
        if (_useSeparatedPocketAndInventorySections)
            RefreshCarryWeightHeader();
    }

    private void RefreshCarryWeightHeader(bool force = false)
    {
        if (_inventorySectionHeader == null || _inventorySectionTitle == null) return;

        if (_inventoryRuntime == null)
        {
            if (force || _displayedCarryCapacity >= 0f) _inventorySectionHeader.text = _inventorySectionTitle;
            _displayedCarryCapacity = -1f;
            return;
        }

        PlayerCarryLoad load = _inventoryRuntime.CarryLoad;
        if (!force && _displayedCarryWeight == load.WeightKg && _displayedCarryCapacity == load.CapacityKg) return;
        _displayedCarryWeight = load.WeightKg;
        _displayedCarryCapacity = load.CapacityKg;

        string color = load.Ratio >= 1f ? "#FF7979" : load.Ratio >= .9f ? "#FFC36B" :
            load.Ratio >= .7f ? "#FFE78C" : "#C4F5FF";
        _inventorySectionHeader.text = $"{_inventorySectionTitle}  <color={color}>{load.WeightKg:0.##}/{load.CapacityKg:0.##} kg</color>";
    }

    // Purpose: Names instantiated slots by their owning storage surface for easy hierarchy inspection.
    private string GetInventorySlotName(int slotIndex)
    {
        int pocketCapacity = _inventoryRuntime != null ? _inventoryRuntime.PocketCapacity : 0;

        return slotIndex < pocketCapacity
            ? $"PocketSlot_{slotIndex:00}"
            : $"InventorySlot_{slotIndex - pocketCapacity:00}";
    }

    // Purpose: Configures this component''s inventory slots.
    private void ConfigureInventorySlots()
    {
        for (int i = 0; i < _inventorySlots.Count; i++)
        {
            InventoryItemSlotUI slot = _inventorySlots[i];

            if (slot != null)
                ConfigureInventorySlot(slot, i);
        }
    }

    // Purpose: Configures this component''s inventory slot.
    private void ConfigureInventorySlot(InventoryItemSlotUI slot, int slotIndex)
    {
        if (slot == null)
            return;

        bool requireDoubleClick =
            _interactionMode != PlayerInventoryInteractionMode.SellSelect;

        // A Key Tool is a container, not an equip action. Open it on one
        // click so its behavior matches the usual container expectation.
        IReadOnlyList<PlayerInventoryItemStack> items = _inventoryRuntime != null
            ? _inventoryRuntime.Items
            : null;
        if (items != null && slotIndex >= 0 && slotIndex < items.Count &&
            items[slotIndex] != null && items[slotIndex].Item is KeyToolItemSO)
        {
            requireDoubleClick = false;
        }

        slot.Initialize(
            this,
            slotIndex,
            true,
            requireDoubleClick,
            InventoryDragSourceKind.Inventory);
    }

    // Purpose: Hides the slot template if needed UI or visual state.
    private void HideSlotTemplateIfNeeded()
    {
        if (!_hideInventorySlotPrefabOnAwake || _inventorySlotPrefab == null)
            return;

        if (_inventorySlotRoot == null && _inventorySlotPrefab.transform.parent != null)
            _inventorySlotRoot = _inventorySlotPrefab.transform.parent;

        if (_inventorySlotRoot != null && _inventorySlotPrefab.transform.IsChildOf(_inventorySlotRoot))
            _inventorySlotPrefab.gameObject.SetActive(false);
    }

    /// <summary>
    /// Resolves the pre-authored Head slot. Equipment UI is deliberately static:
    /// this method never creates, clones, or repositions UI at runtime.
    /// </summary>
    private void EnsureHeadSlot()
    {
        if (_headSlot != null)
            return;

        _headSlot = GetComponentInChildren<EquipmentInventoryGasMaskSlotUI>(true);
        if (_headSlot == null)
        {
            Debug.LogError(
                "[PlayerInventoryUI] PlayerInventoryPanel is missing its pre-authored HeadSlot " +
                "(EquipmentInventoryGasMaskSlotUI). Add the static slot to the panel prefab.",
                this);
        }
    }

    // Purpose: Resolves the refs needed by this component.
    private void ResolveRefs()
    {
        if (_contextMenu == null)
            _contextMenu = GetComponentInChildren<InventoryItemContextMenuUI>(true);

        if (_armorSlot == null)
            _armorSlot = GetComponentInChildren<EquipmentInventoryArmorSlotUI>(true);

        if (_headSlot == null)
            _headSlot = GetComponentInChildren<EquipmentInventoryGasMaskSlotUI>(true);

        if (_weaponSlots == null || _weaponSlots.Length == 0 || AreAllWeaponSlotsMissing())
        {
            _weaponSlots = GetComponentsInChildren<EquipmentInventoryWeaponSlotUI>(true);
            Array.Sort(_weaponSlots, (left, right) =>
            {
                if (left == null && right == null)
                    return 0;

                if (left == null)
                    return 1;

                if (right == null)
                    return -1;

                return left.WeaponSlotIndex.CompareTo(right.WeaponSlotIndex);
            });
        }

        if (_ammoSlots == null || _ammoSlots.Length == 0 || AreAllAmmoSlotsMissing())
        {
            _ammoSlots = GetComponentsInChildren<EquipmentInventoryAmmoSlotUI>(true);
            Array.Sort(_ammoSlots, (left, right) =>
            {
                if (left == null && right == null)
                    return 0;

                if (left == null)
                    return 1;

                if (right == null)
                    return -1;

                return left.AmmoSlotIndex.CompareTo(right.AmmoSlotIndex);
            });
        }

        if (_inventorySlotPrefab == null)
        {
            InventoryItemSlotUI[] slots = GetComponentsInChildren<InventoryItemSlotUI>(true);

            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i] == null)
                    continue;

                _inventorySlotPrefab = slots[i];
                break;
            }
        }

        if (_inventorySlotRoot == null && _inventorySlotPrefab != null && _inventorySlotPrefab.transform.parent != null)
            _inventorySlotRoot = _inventorySlotPrefab.transform.parent;

        if (_dropActor == null && _inventoryRuntime != null)
            _dropActor = _inventoryRuntime.transform;

        if (_consumableUseRuntime == null && _inventoryRuntime != null)
            _consumableUseRuntime = EntityComponentResolver.Resolve<PlayerConsumableUseRuntime>(_inventoryRuntime);

        if (_quickSlots == null && _inventoryRuntime != null)
            _quickSlots = EntityComponentResolver.Resolve<PlayerQuickSlotRuntime>(_inventoryRuntime);

        if (_passiveRuntime == null && _inventoryRuntime != null)
        {
            _passiveRuntime = EntityComponentResolver.Resolve<PlayerPassiveInventoryRuntime>(
                _inventoryRuntime);
        }
    }

    // Purpose: Performs the are all weapon slots missing workflow for this component.
    private bool AreAllWeaponSlotsMissing()
    {
        if (_weaponSlots == null || _weaponSlots.Length == 0)
            return true;

        for (int i = 0; i < _weaponSlots.Length; i++)
        {
            if (_weaponSlots[i] != null)
                return false;
        }

        return true;
    }

    // Purpose: Attempts to get inventory stack and reports whether the request succeeded.
    private bool TryGetInventoryStack(int slotIndex, out PlayerInventoryItemStack stack)
    {
        stack = null;

        if (_inventoryRuntime == null)
            return false;

        IReadOnlyList<PlayerInventoryItemStack> items = _inventoryRuntime.Items;

        if (items == null || slotIndex < 0 || slotIndex >= items.Count)
            return false;

        stack = items[slotIndex];
        return stack != null && !stack.IsEmpty;
    }

    // Purpose: Performs the are all ammo slots missing workflow for this component.
    private bool AreAllAmmoSlotsMissing()
    {
        if (_ammoSlots == null || _ammoSlots.Length == 0)
            return true;

        for (int i = 0; i < _ammoSlots.Length; i++)
        {
            if (_ammoSlots[i] != null)
                return false;
        }

        return true;
    }

    // Purpose: Selects inventory slot for the active workflow.
    private void SelectInventorySlot(int slotIndex)
    {
        if (!TryGetInventoryStack(slotIndex, out PlayerInventoryItemStack stack))
        {
            ClearSelectedItemSelection();
            return;
        }

        _selectedInventoryIndex = slotIndex;
        _selectedItemSelection = CreateInventorySelection(slotIndex, stack);
        OnInventorySelectionChanged?.Invoke(_selectedInventoryIndex, stack);
        OnItemSelectionChanged?.Invoke(_selectedItemSelection);
    }

    // Purpose: Selects equipment slot for the active workflow.
    private void SelectEquipmentSlot(PlayerInventoryEquipmentSlotKind slotKind)
    {
        if (_inventoryRuntime == null)
        {
            ClearSelectedItemSelection();
            return;
        }

        ItemSO item = slotKind switch
        {
            PlayerInventoryEquipmentSlotKind.Weapon => _inventoryRuntime.EquippedWeapon,
            PlayerInventoryEquipmentSlotKind.Armor => _inventoryRuntime.EquippedArmor,
            PlayerInventoryEquipmentSlotKind.Head => _inventoryRuntime.EquippedGasMask,
            _ => null
        };

        if (item == null)
        {
            ClearSelectedItemSelection();
            return;
        }

        float armorDurability = slotKind == PlayerInventoryEquipmentSlotKind.Armor
            ? _inventoryRuntime.EquippedArmorDurability
            : -1f;

        float armorMaxDurability = slotKind == PlayerInventoryEquipmentSlotKind.Armor
            ? _inventoryRuntime.EquippedArmorMaxDurability
            : -1f;

        float gasMaskFilterDurability = slotKind == PlayerInventoryEquipmentSlotKind.Head
            ? _inventoryRuntime.EquippedGasMaskFilterDurability
            : -1f;
        float gasMaskMaxFilterDurability = slotKind == PlayerInventoryEquipmentSlotKind.Head
            ? _inventoryRuntime.EquippedGasMaskMaxFilterDurability
            : -1f;

        _selectedInventoryIndex = -1;
        _selectedItemSelection = new PlayerInventoryItemSelection(
            PlayerInventoryItemSelectionKind.Equipment,
            -1,
            slotKind,
            -1,
            item,
            1,
            armorDurability,
            armorMaxDurability,
            slotKind == PlayerInventoryEquipmentSlotKind.Weapon ? 0 : -1,
            slotKind == PlayerInventoryEquipmentSlotKind.Weapon
                ? _inventoryRuntime.GetEquippedWeaponCurrentDurability(0)
                : -1f,
            slotKind == PlayerInventoryEquipmentSlotKind.Weapon
                ? _inventoryRuntime.GetEquippedWeaponMaxDurability(0)
                : -1f,
            gasMaskFilterDurability,
            gasMaskMaxFilterDurability);

        OnInventorySelectionChanged?.Invoke(_selectedInventoryIndex, null);
        OnItemSelectionChanged?.Invoke(_selectedItemSelection);
    }

    // Purpose: Selects weapon slot for the active workflow.
    private void SelectWeaponSlot(int weaponSlotIndex)
    {
        if (_inventoryRuntime == null)
        {
            ClearSelectedItemSelection();
            return;
        }

        ItemSO item = _inventoryRuntime.GetEquippedWeapon(weaponSlotIndex);

        if (item == null)
        {
            ClearSelectedItemSelection();
            return;
        }

        _selectedInventoryIndex = -1;
        _selectedItemSelection = new PlayerInventoryItemSelection(
            PlayerInventoryItemSelectionKind.Equipment,
            -1,
            PlayerInventoryEquipmentSlotKind.Weapon,
            -1,
            item,
            Mathf.Max(1, _inventoryRuntime.GetEquippedWeaponAmount(weaponSlotIndex)),
            -1f,
            -1f,
            weaponSlotIndex,
            _inventoryRuntime.GetEquippedWeaponCurrentDurability(weaponSlotIndex),
            _inventoryRuntime.GetEquippedWeaponMaxDurability(weaponSlotIndex));

        OnInventorySelectionChanged?.Invoke(_selectedInventoryIndex, null);
        OnItemSelectionChanged?.Invoke(_selectedItemSelection);
    }

    // Purpose: Selects ammo slot for the active workflow.
    private void SelectAmmoSlot(int ammoSlotIndex)
    {
        if (_inventoryRuntime == null ||
            !_inventoryRuntime.TryGetAmmoSlotSnapshot(
                ammoSlotIndex,
                out WeaponAmmoSlotSnapshot snapshot) ||
            snapshot.isEmpty ||
            snapshot.bullet == null)
        {
            ClearSelectedItemSelection();
            return;
        }

        _selectedInventoryIndex = -1;
        _selectedItemSelection = new PlayerInventoryItemSelection(
            PlayerInventoryItemSelectionKind.Ammo,
            -1,
            default,
            ammoSlotIndex,
            snapshot.bullet,
            Mathf.Max(0, snapshot.totalAmmo),
            -1f,
            -1f);

        OnInventorySelectionChanged?.Invoke(_selectedInventoryIndex, null);
        OnItemSelectionChanged?.Invoke(_selectedItemSelection);
    }

    // Purpose: Creates the inventory selection owned by this workflow.
    private static PlayerInventoryItemSelection CreateInventorySelection(
        int slotIndex,
        PlayerInventoryItemStack stack)
    {
        if (stack == null || stack.IsEmpty)
            return PlayerInventoryItemSelection.None;

        float maxDurability = stack.Item is ArmorItemSO armor
            ? stack.HasArmorCondition ? stack.ArmorMaxDurability : armor.MaxDurability
            : -1f;

        float durability = stack.Item is ArmorItemSO
            ? stack.HasArmorDurability ? stack.ArmorDurability : maxDurability
            : -1f;

        return new PlayerInventoryItemSelection(
            PlayerInventoryItemSelectionKind.Inventory,
            slotIndex,
            default,
            -1,
            stack.Item,
            stack.Amount,
            durability,
            maxDurability,
            -1,
            stack.WeaponCurrentDurability,
            stack.WeaponMaxDurability,
            stack.GasMaskFilterDurability,
            stack.GasMaskMaxFilterDurability);
    }

    // Purpose: Resolves the selected stack needed by this component.
    private PlayerInventoryItemStack ResolveSelectedStack()
    {
        if (_selectedInventoryIndex < 0 ||
            _inventoryRuntime == null ||
            _inventoryRuntime.Items == null ||
            _selectedInventoryIndex >= _inventoryRuntime.Items.Count)
        {
            return null;
        }

        PlayerInventoryItemStack stack = _inventoryRuntime.Items[_selectedInventoryIndex];
        return stack != null && !stack.IsEmpty ? stack : null;
    }

    // Purpose: Validates selection and reports invalid authored data.
    private void ValidateSelection()
    {
        if (!_selectedItemSelection.HasItem)
            return;

        switch (_selectedItemSelection.kind)
        {
            case PlayerInventoryItemSelectionKind.Inventory:
            {
                PlayerInventoryItemStack stack = ResolveSelectedStack();

                if (stack != null && !stack.IsEmpty)
                {
                    _selectedItemSelection = CreateInventorySelection(_selectedInventoryIndex, stack);
                    OnInventorySelectionChanged?.Invoke(_selectedInventoryIndex, stack);
                    OnItemSelectionChanged?.Invoke(_selectedItemSelection);
                    return;
                }

                break;
            }

            case PlayerInventoryItemSelectionKind.Equipment:
                if (_selectedItemSelection.equipmentSlotKind == PlayerInventoryEquipmentSlotKind.Weapon)
                    SelectWeaponSlot(_selectedItemSelection.equipmentSlotIndex);
                else
                    SelectEquipmentSlot(_selectedItemSelection.equipmentSlotKind);
                return;

            case PlayerInventoryItemSelectionKind.Ammo:
                SelectAmmoSlot(_selectedItemSelection.ammoSlotIndex);
                return;
        }

        ClearSelectedItemSelection();
    }

    // Purpose: Handles the incoming inventory runtime ready request or event.
    private void HandleInventoryRuntimeReady(PlayerInventoryRuntime runtime)
    {
        if (_inventoryRuntime != null)
            _inventoryRuntime.OnChanged -= Refresh;

        if (_quickSlots != null)
            _quickSlots.OnChanged -= Refresh;

        UnsubscribePassiveRuntime();

        _inventoryRuntime = runtime;
        _consumableUseRuntime = runtime != null
            ? EntityComponentResolver.Resolve<PlayerConsumableUseRuntime>(runtime)
            : null;
        _quickSlots = runtime != null
            ? EntityComponentResolver.Resolve<PlayerQuickSlotRuntime>(runtime)
            : null;
        _passiveRuntime = runtime != null
            ? EntityComponentResolver.Resolve<PlayerPassiveInventoryRuntime>(runtime)
            : null;

        if (_inventoryRuntime != null)
            _inventoryRuntime.OnChanged += Refresh;

        if (_quickSlots != null)
            _quickSlots.OnChanged += Refresh;

        SubscribePassiveRuntime();

        Refresh();
    }

    // Purpose: Clears the current bound slots state.
    private void ClearBoundSlots()
    {
        if (_armorSlot != null)
            _armorSlot.Bind(null);

        if (_headSlot != null)
            _headSlot.Bind(null, 0f, 0f);

        if (_weaponSlots != null && _weaponSlots.Length > 0)
        {
            for (int i = 0; i < _weaponSlots.Length; i++)
            {
                if (_weaponSlots[i] != null)
                    _weaponSlots[i].Bind(null, 0f, 0f);
            }
        }

        if (_ammoSlots != null)
        {
            for (int i = 0; i < _ammoSlots.Length; i++)
            {
                EquipmentInventoryAmmoSlotUI slot = _ammoSlots[i];
                if (slot == null)
                    continue;

                slot.gameObject.SetActive(true);
                slot.BindQuickSlot(new PlayerQuickSlotSnapshot(
                    2 + i,
                    PlayerQuickSlotBindingKind.None,
                    null,
                    0,
                    false,
                    0f,
                    0f,
                    default));
            }
        }

        if (_passiveSlots != null)
        {
            for (int i = 0; i < _passiveSlots.Length; i++)
            {
                if (_passiveSlots[i] != null)
                    _passiveSlots[i].BindPassive(null, false);
            }
        }

        for (int i = 0; i < _inventorySlots.Count; i++)
        {
            if (_inventorySlots[i] != null)
                _inventorySlots[i].Bind(null);
        }

        ClearSelectedInventorySlot();
    }

    // Purpose: Subscribes to passive runtime notifications.
    private void SubscribePassiveRuntime()
    {
        if (_passiveRuntime == null)
            return;

        _passiveRuntime.OnChanged -= RefreshPassiveSlots;
        _passiveRuntime.OnChanged += RefreshPassiveSlots;
    }

    // Purpose: Removes the passive runtime notification subscription.
    private void UnsubscribePassiveRuntime()
    {
        if (_passiveRuntime != null)
            _passiveRuntime.OnChanged -= RefreshPassiveSlots;
    }

    // Purpose: Publishes the message notification.
    private void RaiseMessage(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
            return;

        if (_systemMessageChannel != null)
            _systemMessageChannel.RaiseEvent(message);
        else
            Debug.Log($"[Inventory] {message}", this);
    }

    // Purpose: Hides the context menu UI or visual state.
    private void HideContextMenu()
    {
        if (_contextMenu != null)
            _contextMenu.Hide();
    }

    // Purpose: Returns the current or create tooltip.
    private PlayerTooltipUI GetOrCreateTooltip()
    {
        if (_tooltipPrefab == null)
            return null;

        DestroyTooltip();
        Transform parent = PlayerTooltipUI.GetOrCreatePanelTooltipRoot(this);
        if (parent == null)
            return null;

        _tooltip = Instantiate(_tooltipPrefab, parent);
        _tooltip.transform.SetAsLastSibling();
        return _tooltip;
    }

    // Purpose: Removes tooltip from the active runtime context.
    private void DestroyTooltip()
    {
        if (_tooltip == null)
            return;

        PlayerTooltipUI tooltip = _tooltip;
        _tooltip = null;
        Destroy(tooltip.gameObject);
    }

    // Purpose: Attempts to spawn world drop and reports whether the request succeeded.
    private bool TrySpawnWorldDrop(PlayerInventoryRemovedStack removed)
    {
        if (!removed.HasItem)
            return false;

        Transform dropSource = _dropActor != null
            ? _dropActor
            : _inventoryRuntime != null ? _inventoryRuntime.transform : transform;

        Vector3 dropForward = Vector3.ProjectOnPlane(dropSource.forward, Vector3.up);

        if (dropForward.sqrMagnitude < 0.0001f)
            dropForward = Vector3.forward;
        else
            dropForward.Normalize();

        Vector3 position =
            dropSource.position +
            dropForward * _dropForwardOffset +
            Vector3.up * _dropUpOffset;

        int amount = Mathf.Max(1, removed.amount);

        bool spawned = WorldItemPickupSpawner.TrySpawn(
            removed.item,
            amount,
            amount,
            0f,
            removed.armorDurability,
            removed.weaponCurrentDurability,
            removed.weaponMaxDurability,
            removed.gasMaskFilterDurability,
            removed.armorMaxDurability,
            removed.gasMaskMaxFilterDurability,
            position,
            Quaternion.identity,
            out WorldItemPickup pickup);

        if (!spawned || pickup == null)
        {
            Debug.LogError(
                $"[PlayerInventoryUI][WorldDrop] Spawn failed. " +
                $"item={removed.item.name}, amount={amount}, position={position}. " +
                $"The removed stack will be restored.",
                this);
            return false;
        }

        Debug.Log(
            $"[PlayerInventoryUI][WorldDrop] Spawned. " +
            $"item={removed.item.name}, amount={amount}, position={position}",
            pickup);
        return true;
    }

    // Purpose: Performs the complete world drop or restore workflow for this component.
    private void CompleteWorldDropOrRestore(
        PlayerInventoryRemovedStack removed,
        string removeMessage)
    {
        if (TrySpawnWorldDrop(removed))
        {
            RaiseMessage(removeMessage);
            return;
        }

        if (_inventoryRuntime != null &&
            _inventoryRuntime.TryRestoreRemovedStack(
                removed,
                out string restoreMessage))
        {
            RaiseMessage("Could not drop the item. It was returned to inventory.");
            Debug.LogWarning(
                $"[PlayerInventoryUI][WorldDrop] Restored removed stack. " +
                $"item={removed.item.name}, amount={removed.amount}, result={restoreMessage}",
                this);
            return;
        }

        RaiseMessage("Could not drop or restore the item. Check the Console.");
        Debug.LogError(
            $"[PlayerInventoryUI][WorldDrop] CRITICAL: failed to restore removed stack. " +
            $"item={(removed.item != null ? removed.item.name : "null")}, amount={removed.amount}",
            this);
    }
}
