/*
 * Role hierarchy: Presentation > UI > Gameplay > PanelHub > Inventory
 * Primary responsibility: Presents EnemyInventoryUI state and handles its user-facing UI interaction.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class EnemyInventoryUI : InventoryPanelUIBase,
    IInventorySlotOwner,
    IInventoryItemContextMenuOwner
{
    private const int WeaponSlotIndexBase = -2000;
    private const int ArmorSlotIndex = -1001;
    private const int AmmoSlotIndexBase = 0;
    private const int ItemSlotIndexBase = 1000;

    [Header("Player Receiver")]
    
    [SerializeField] private PlayerInventoryRuntimeReadyEventChannelSO _playerInventoryRuntimeReadyChannel;

    [Header("Loot Header")]
    [SerializeField] private TextMeshProUGUI _enemyNameText;
    [SerializeField] private string _enemyNameFormat = "LOOT INVENTORY · {0}";
    [SerializeField] private string _emptyLootName = "LOOT INVENTORY";

    [Header("Looted Equipment Slots")]
    [Tooltip("Equipment shown only while looting a character. Element 0 is primary and element 1 is secondary/throwable.")]
    [SerializeField] private EquipmentInventoryWeaponSlotUI[] _weaponSlots;

    [Tooltip("Enemy currently equipped armor slot.")]
    [SerializeField] private EquipmentInventoryArmorSlotUI _armorSlot;

    [Tooltip("Enemy equipped ammo slots. Currently one slot is enough, but multiple slots are supported.")]
    [SerializeField] private EquipmentInventoryAmmoSlotUI[] _ammoSlots;
    [Tooltip("Entire equipment area. Structure loot hides this and shows inventory slots only.")]
    [SerializeField] private GameObject _equipmentSectionRoot;

    [Header("Loot Inventory Slot Instancing")]
    [Tooltip("Prefab used for the selected loot source's carried inventory slots. Currency is handled as a normal item stack.")]
    [SerializeField] private InventoryItemSlotUI _inventorySlotPrefab;
    [SerializeField] private Transform _inventorySlotRoot;
    [Tooltip("Vertical scroll view that owns Inventory Slot Root. It is reset to the top whenever a new loot source opens.")]
    [SerializeField] private ScrollRect _inventoryScrollRect;
    [SerializeField] private bool _hideInventorySlotPrefabOnAwake = true;

    [Header("Context Menu")]
    [SerializeField] private InventoryItemContextMenuUI _contextMenu;

    [Header("Tooltip")]
    [Tooltip("Tooltip prefab displayed immediately to the left of Enemy equipment and inventory slots.")]
    [SerializeField] private PlayerTooltipUI _tooltipPrefab;
    [SerializeField] private TooltipPlacementSide _tooltipSide = TooltipPlacementSide.Left;
    [SerializeField, Min(0f)] private float _tooltipGap;

    [Header("World Drop")]
    [SerializeField, Min(0f)] private float _dropForwardOffset = 1f;
    [SerializeField] private float _dropUpOffset = 0.05f;

    private Transform _dropActor;

    [Header("Events")]
    [SerializeField] private EnemyLootOpenRequestEventChannelSO _openRequestChannel;
    [SerializeField] private UIOverlayRequestEventChannelSO _overlayRequestChannel;
    [SerializeField] private SystemMessageEventChannelSO _systemMessageChannel;
    private PlayerInventoryRuntime _playerInventoryRuntime;
    private EnemyLootInventoryRuntime _activeInventory;
    private readonly System.Collections.Generic.List<InventoryItemSlotUI> _itemSlots = new();
    private int _contextEquipmentWeaponSlotIndex;
    private PlayerTooltipUI _tooltip;

    // Purpose: Caches the component references required before runtime initialization.
    protected override void Awake()
    {
        base.Awake();
        ResolveRefs();
        InitializeSlots();
        HideSlotTemplateIfNeeded();
        HidePanel();
    }

    // Purpose: Reapplies the shared Loot-column layout in edit mode so the Canvas scene matches the Play-mode workspace.
    private void OnValidate()
    {
        if (!Application.isPlaying)
            ApplyContextPresentation();
    }

    // Purpose: Activates runtime subscriptions and refreshes the enabled state.
    private void OnEnable()
    {
        ResolveRefs();
        InitializeSlots();
        HideSlotTemplateIfNeeded();

        if (_openRequestChannel != null)
            _openRequestChannel.OnEventRaised += HandleOpenRequested;

        if (_playerInventoryRuntimeReadyChannel != null)
        {
            _playerInventoryRuntimeReadyChannel.OnEventRaised += HandlePlayerInventoryRuntimeReady;

            if (_playerInventoryRuntimeReadyChannel.HasCurrent)
                HandlePlayerInventoryRuntimeReady(_playerInventoryRuntimeReadyChannel.Current);
        }

        Refresh();
    }

    // Purpose: Releases active subscriptions and transient state while disabled.
    private void OnDisable()
    {
        CloseContextMenu();
        HandleItemTooltipExited();

        if (_openRequestChannel != null)
            _openRequestChannel.OnEventRaised -= HandleOpenRequested;

        if (_playerInventoryRuntimeReadyChannel != null)
            _playerInventoryRuntimeReadyChannel.OnEventRaised -= HandlePlayerInventoryRuntimeReady;
    }

    // Purpose: Responds to the panel hidden callback.
    protected override void OnPanelHidden()
    {
        CloseContextMenu();
        HandleItemTooltipExited();
        _activeInventory = null;
        Refresh();
    }

    // Purpose: Closes the active context menu UI or workflow.
    public void CloseContextMenu()
    {
        if (_contextMenu != null)
            _contextMenu.Hide();
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

    // Purpose: Handles the incoming inventory slot left clicked request or event.
    public void HandleInventorySlotLeftClicked(int slotIndex)
    {
        if (TryDecodeWeaponSlotIndex(slotIndex, out int weaponSlotIndex))
        {
            TryTakeWeapon(weaponSlotIndex);
            return;
        }

        switch (slotIndex)
        {
            case ArmorSlotIndex:
                TryTakeArmor();
                break;

            default:
                if (slotIndex >= AmmoSlotIndexBase)
                {
                    if (slotIndex >= ItemSlotIndexBase)
                        TryTakeItem(slotIndex - ItemSlotIndexBase);
                    else
                        TryTakeAmmo(slotIndex - AmmoSlotIndexBase);
                }
                break;
        }
    }

    // Purpose: Handles the incoming inventory slot right clicked request or event.
    public void HandleInventorySlotRightClicked(int slotIndex, Vector2 screenPosition)
    {
        if (TryDecodeWeaponSlotIndex(slotIndex, out int weaponSlotIndex))
        {
            HandleWeaponSlotRightClicked(weaponSlotIndex, screenPosition);
            return;
        }

        if (slotIndex == ArmorSlotIndex)
        {
            HandleEquipmentSlotRightClicked(
                PlayerInventoryEquipmentSlotKind.Armor,
                screenPosition);
            return;
        }

        if (slotIndex >= AmmoSlotIndexBase && slotIndex < ItemSlotIndexBase)
        {
            HandleAmmoSlotRightClicked(slotIndex - AmmoSlotIndexBase, screenPosition);
            return;
        }

        if (_contextMenu == null || slotIndex < ItemSlotIndexBase)
            return;

        PlayerInventoryItemStack stack = ResolveItemStack(slotIndex - ItemSlotIndexBase);

        if (stack == null)
            return;

        _contextMenu.ShowInventoryItem(
            this,
            slotIndex,
            stack.Item,
            stack.Amount,
            screenPosition);
    }

    // Purpose: Handles the incoming inventory slot dropped on inventory request or event.
    public void HandleInventorySlotDroppedOnInventory(int fromIndex, int toIndex)
    {
        TryReceiveDragPayloadToEnemyInventorySlot(toIndex);
    }

    // Purpose: Handles the incoming inventory slot dropped on equipment request or event.
    public void HandleInventorySlotDroppedOnEquipment(
        int inventoryIndex,
        PlayerInventoryEquipmentSlotKind slotKind)
    {
        TryReceiveDragPayloadToEnemyEquipmentSlot(slotKind);
    }

    // Purpose: Handles the incoming inventory slot dropped on ammo request or event.
    public void HandleInventorySlotDroppedOnAmmo(int inventoryIndex, int ammoSlotIndex)
    {
        TryReceiveDragPayloadToEnemyAmmoSlot(ammoSlotIndex);
    }

    // Purpose: Handles the incoming equipment slot left clicked request or event.
    public void HandleEquipmentSlotLeftClicked(PlayerInventoryEquipmentSlotKind slotKind)
    {
        switch (slotKind)
        {
            case PlayerInventoryEquipmentSlotKind.Weapon:
                TryTakeWeapon(0);
                break;

            case PlayerInventoryEquipmentSlotKind.Armor:
                TryTakeArmor();
                break;
        }
    }

    // Purpose: Handles the incoming equipment slot dropped on inventory request or event.
    public void HandleEquipmentSlotDroppedOnInventory(
        PlayerInventoryEquipmentSlotKind slotKind,
        int targetInventoryIndex)
    {
        TryReceiveDragPayloadToEnemyInventorySlot(targetInventoryIndex);
    }

    // Purpose: Handles the incoming equipment slot right clicked request or event.
    public void HandleEquipmentSlotRightClicked(
        PlayerInventoryEquipmentSlotKind slotKind,
        Vector2 screenPosition)
    {
        if (_contextMenu == null || _activeInventory == null)
            return;

        ItemSO item = slotKind switch
        {
            PlayerInventoryEquipmentSlotKind.Weapon =>
                _activeInventory.GetWeapon(_contextEquipmentWeaponSlotIndex),
            PlayerInventoryEquipmentSlotKind.Armor => _activeInventory.Armor,
            _ => null
        };

        if (item == null)
            return;

        _contextMenu.ShowEquipmentSlot(
            this,
            slotKind,
            item,
            screenPosition);
    }

    // Purpose: Handles the incoming weapon slot right clicked request or event.
    public void HandleWeaponSlotRightClicked(int weaponSlotIndex, Vector2 screenPosition)
    {
        if (_contextMenu == null || _activeInventory == null)
            return;

        ItemSO item = _activeInventory.GetWeapon(weaponSlotIndex);

        if (item == null)
            return;

        _contextEquipmentWeaponSlotIndex = Mathf.Max(0, weaponSlotIndex);
        _contextMenu.ShowEquipmentSlot(
            this,
            PlayerInventoryEquipmentSlotKind.Weapon,
            item,
            screenPosition);
    }

    // Purpose: Handles the incoming ammo slot dropped on inventory request or event.
    public void HandleAmmoSlotDroppedOnInventory(int ammoSlotIndex, int targetInventoryIndex)
    {
        TryReceiveDragPayloadToEnemyInventorySlot(targetInventoryIndex);
    }

    // Purpose: Handles the incoming ammo slot dropped on ammo request or event.
    public void HandleAmmoSlotDroppedOnAmmo(int fromAmmoSlotIndex, int toAmmoSlotIndex)
    {
        TryReceiveDragPayloadToEnemyAmmoSlot(toAmmoSlotIndex);
    }

    // Purpose: Handles the incoming ammo slot left clicked request or event.
    public void HandleAmmoSlotLeftClicked(int ammoSlotIndex)
    {
        TryTakeAmmo(ammoSlotIndex);
    }

    // Purpose: Handles the incoming ammo slot right clicked request or event.
    public void HandleAmmoSlotRightClicked(int ammoSlotIndex, Vector2 screenPosition)
    {
        if (_contextMenu == null)
        {
            TryTakeAmmo(ammoSlotIndex);
            return;
        }

        EnemyLootBulletStack stack = ResolveAmmoStack(ammoSlotIndex);

        if (stack == null)
            return;

        _contextMenu.ShowAmmoSlot(
            this,
            ammoSlotIndex,
            stack.Bullet,
            stack.Amount,
            screenPosition);
    }

    // Purpose: Handles the incoming context menu equip inventory item request or event.
    public void HandleContextMenuEquipInventoryItem(int inventoryIndex)
    {
        if (_activeInventory == null || _playerInventoryRuntime == null)
            return;

        if (inventoryIndex >= ItemSlotIndexBase)
        {
            int itemIndex = inventoryIndex - ItemSlotIndexBase;
            PlayerInventoryItemStack stack = ResolveItemStack(itemIndex);

            if (stack != null && stack.Item is BulletSO)
            {
                if (!_playerInventoryRuntime.TryPickup(
                        stack.Item,
                        stack.Amount,
                        out string ammoMessage))
                {
                    RaiseMessage(ammoMessage);
                    return;
                }

                _activeInventory.ClearItemStack(itemIndex);
                RaiseMessage(ammoMessage);
                RefreshAfterTake();
                return;
            }
        }

        TryTakeSlotToPlayerEquipmentSlot(
            inventoryIndex,
            ResolvePreferredEquipmentSlotKind(inventoryIndex));
    }

    // Purpose: Handles the incoming context menu split inventory item request or event.
    public void HandleContextMenuSplitInventoryItem(int inventoryIndex, int amount)
    {
        if (_activeInventory == null || inventoryIndex < ItemSlotIndexBase)
            return;

        int itemIndex = inventoryIndex - ItemSlotIndexBase;

        if (UIAudio.ReportResult(_activeInventory.TrySplitItemStack(
                itemIndex,
                amount,
                out _,
                out string message), UIAudioCueType.Split))
        {
            RaiseMessage(message);
            Refresh();
        }
        else
        {
            RaiseMessage(message);
        }
    }

    // Purpose: Handles the incoming context menu discard inventory item request or event.
    public void HandleContextMenuDiscardInventoryItem(int inventoryIndex)
    {
        if (_activeInventory == null || inventoryIndex < ItemSlotIndexBase)
            return;

        int itemIndex = inventoryIndex - ItemSlotIndexBase;
        PlayerInventoryItemStack stack = ResolveItemStack(itemIndex);

        if (stack != null)
        {
            TrySpawnWorldDrop(new PlayerInventoryRemovedStack(
                stack.Item,
                stack.Amount,
                stack.ArmorDurability,
                stack.WeaponCurrentDurability,
                stack.WeaponMaxDurability));
        }

        _activeInventory.ClearItemStack(itemIndex);
        RefreshAfterTake();
    }

    // Purpose: Handles the incoming context menu unequip equipment slot request or event.
    public void HandleContextMenuUnequipEquipmentSlot(PlayerInventoryEquipmentSlotKind slotKind)
    {
        if (slotKind == PlayerInventoryEquipmentSlotKind.Weapon)
            TryTakeWeapon(_contextEquipmentWeaponSlotIndex);
        else
            TryTakeArmor();
    }

    // Purpose: Handles the incoming context menu discard equipment slot request or event.
    public void HandleContextMenuDiscardEquipmentSlot(PlayerInventoryEquipmentSlotKind slotKind)
    {
        if (_activeInventory == null)
            return;

        switch (slotKind)
        {
            case PlayerInventoryEquipmentSlotKind.Weapon:
                EnemyLootWeaponStack discardedWeapon =
                    _activeInventory.GetWeaponStack(_contextEquipmentWeaponSlotIndex);
                TrySpawnWorldDrop(new PlayerInventoryRemovedStack(
                    discardedWeapon != null ? discardedWeapon.Weapon : null,
                    discardedWeapon != null ? discardedWeapon.Amount : 0,
                    -1f,
                    discardedWeapon != null ? discardedWeapon.CurrentDurability : -1f,
                    discardedWeapon != null ? discardedWeapon.MaxDurability : -1f));
                _activeInventory.ClearWeapon(_contextEquipmentWeaponSlotIndex);
                break;

            case PlayerInventoryEquipmentSlotKind.Armor:
                TrySpawnWorldDrop(new PlayerInventoryRemovedStack(
                    _activeInventory.Armor,
                    1,
                    _activeInventory.ArmorDurability));
                _activeInventory.ClearArmor();
                break;
        }

        RefreshAfterTake();
    }

    // Purpose: Handles the incoming context menu unequip ammo slot request or event.
    public void HandleContextMenuUnequipAmmoSlot(int ammoSlotIndex)
    {
        TryTakeAmmo(ammoSlotIndex);
    }

    // Purpose: Handles the incoming context menu split ammo slot request or event.
    public void HandleContextMenuSplitAmmoSlot(int ammoSlotIndex, int amount)
    {
        if (_activeInventory == null)
            return;

        if (UIAudio.ReportResult(_activeInventory.TrySplitBulletStack(
                ammoSlotIndex,
                amount,
                out _,
                out string message), UIAudioCueType.Split))
        {
            RaiseMessage(message);
            Refresh();
        }
        else
        {
            RaiseMessage(message);
        }
    }

    // Purpose: Handles the incoming context menu discard ammo slot request or event.
    public void HandleContextMenuDiscardAmmoSlot(int ammoSlotIndex)
    {
        if (_activeInventory == null)
            return;

        EnemyLootBulletStack stack = ResolveAmmoStack(ammoSlotIndex);

        if (stack != null)
        {
            TrySpawnWorldDrop(new PlayerInventoryRemovedStack(
                stack.Bullet,
                stack.Amount,
                -1f));
        }

        _activeInventory.ClearBulletStack(ammoSlotIndex);
        RefreshAfterTake();
    }

    // Purpose: Attempts to take weapon and reports whether the request succeeded.
    public void TryTakeWeapon(int weaponSlotIndex)
    {
        if (_activeInventory == null || _playerInventoryRuntime == null)
            return;

        WeaponSO weapon = _activeInventory.GetWeapon(weaponSlotIndex);
        int amount = _activeInventory.GetWeaponAmount(weaponSlotIndex);
        EnemyLootWeaponStack weaponStack = _activeInventory.GetWeaponStack(weaponSlotIndex);

        if (weapon == null)
            return;

        if (!UIAudio.ReportResult(_playerInventoryRuntime.TryStoreExternalItemToInventorySlot(
                weapon,
                amount,
                -1f,
                weaponStack != null ? weaponStack.CurrentDurability : -1f,
                weaponStack != null ? weaponStack.MaxDurability : -1f,
                -1,
                out string message), UIAudioCueType.InventoryMove))
        {
            RaiseMessage(message);
            return;
        }

        _activeInventory.ClearWeapon(weaponSlotIndex);
        RaiseMessage(message);
        RefreshAfterTake();
    }

    // Purpose: Attempts to take armor and reports whether the request succeeded.
    public void TryTakeArmor()
    {
        if (_activeInventory == null || _playerInventoryRuntime == null)
            return;

        ArmorItemSO armor = _activeInventory.Armor;

        if (armor == null)
            return;

        if (!UIAudio.ReportResult(_playerInventoryRuntime.TryStoreExternalItemToInventorySlot(
                armor,
                1,
                _activeInventory.ArmorDurability,
                -1f,
                -1f,
                -1f,
                _activeInventory.Armor != null
                    ? _activeInventory.Armor.MaxDurability
                    : -1f,
                -1f,
                -1,
                out string message), UIAudioCueType.InventoryMove))
        {
            RaiseMessage(message);
            return;
        }

        _activeInventory.ClearArmor();
        RaiseMessage(message);
        RefreshAfterTake();
    }

    // Purpose: Attempts to take ammo and reports whether the request succeeded.
    public void TryTakeAmmo(int index)
    {
        if (_activeInventory == null || _playerInventoryRuntime == null)
            return;

        EnemyLootBulletStack stack = ResolveAmmoStack(index);

        if (stack == null)
            return;

        if (!UIAudio.ReportResult(_playerInventoryRuntime.TryStoreExternalItemToInventorySlot(
                stack.Bullet,
                stack.Amount,
                -1f,
                -1,
                out string message), UIAudioCueType.InventoryMove))
        {
            RaiseMessage(message);
            return;
        }

        _activeInventory.ClearBulletStack(index);
        RaiseMessage(message);
        RefreshAfterTake();
    }

    // Purpose: Attempts to take item and reports whether the request succeeded.
    public void TryTakeItem(int index)
    {
        if (_activeInventory == null || _playerInventoryRuntime == null)
            return;

        PlayerInventoryItemStack stack = ResolveItemStack(index);

        if (stack == null)
            return;

        if (!UIAudio.ReportResult(_activeInventory.TryTakeItemStack(
                index,
                _playerInventoryRuntime,
                out string message), UIAudioCueType.InventoryMove))
        {
            RaiseMessage(message);
            return;
        }

        RaiseMessage(message);
        RefreshAfterTake();
    }

    // Purpose: Attempts to take bullet and reports whether the request succeeded.
    public void TryTakeBullet(int index)
    {
        TryTakeAmmo(index);
    }

    // Purpose: Attempts to take slot to player inventory slot and reports whether the request succeeded.
    public bool TryTakeSlotToPlayerInventorySlot(int enemySlotIndex, int targetInventoryIndex)
    {
        if (_activeInventory == null || _playerInventoryRuntime == null)
            return false;

        if (TryDecodeWeaponSlotIndex(enemySlotIndex, out int weaponSlotIndex))
        {
            WeaponSO weapon = _activeInventory.GetWeapon(weaponSlotIndex);
            int amount = _activeInventory.GetWeaponAmount(weaponSlotIndex);
            EnemyLootWeaponStack weaponStack = _activeInventory.GetWeaponStack(weaponSlotIndex);

            if (weapon == null)
                return false;

            if (!UIAudio.ReportResult(_playerInventoryRuntime.TryStoreExternalItemToInventorySlot(
                    weapon,
                    amount,
                    -1f,
                    weaponStack != null ? weaponStack.CurrentDurability : -1f,
                    weaponStack != null ? weaponStack.MaxDurability : -1f,
                    targetInventoryIndex,
                    out string message), UIAudioCueType.InventoryMove))
            {
                RaiseMessage(message);
                return false;
            }

            _activeInventory.ClearWeapon(weaponSlotIndex);
            RaiseMessage(message);
            RefreshAfterTake();
            return true;
        }

        if (enemySlotIndex == ArmorSlotIndex)
        {
            ArmorItemSO armor = _activeInventory.Armor;

            if (armor == null)
                return false;

            float durability = _activeInventory.ArmorDurability;

            if (!UIAudio.ReportResult(_playerInventoryRuntime.TryStoreExternalItemToInventorySlot(
                    armor,
                    1,
                    durability,
                    -1f,
                    -1f,
                    -1f,
                    armor.MaxDurability,
                    -1f,
                    targetInventoryIndex,
                    out string message), UIAudioCueType.InventoryMove))
            {
                RaiseMessage(message);
                return false;
            }

            _activeInventory.ClearArmor();
            RaiseMessage(message);
            RefreshAfterTake();
            return true;
        }

        if (enemySlotIndex >= ItemSlotIndexBase)
        {
            int itemIndex = enemySlotIndex - ItemSlotIndexBase;
            PlayerInventoryItemStack itemStack = ResolveItemStack(itemIndex);

            if (itemStack == null)
                return false;

            if (!UIAudio.ReportResult(_playerInventoryRuntime.TryStoreExternalItemToInventorySlot(
                    itemStack.Item,
                    itemStack.Amount,
                    itemStack.ArmorDurability,
                    itemStack.WeaponCurrentDurability,
                    itemStack.WeaponMaxDurability,
                    itemStack.GasMaskFilterDurability,
                    itemStack.ArmorMaxDurability,
                    itemStack.GasMaskMaxFilterDurability,
                    targetInventoryIndex,
                    out string itemMessage), UIAudioCueType.InventoryMove))
            {
                RaiseMessage(itemMessage);
                return false;
            }

            _activeInventory.ClearItemStack(itemIndex);
            RaiseMessage(itemMessage);
            RefreshAfterTake();
            return true;
        }

        int ammoIndex = enemySlotIndex - AmmoSlotIndexBase;
        EnemyLootBulletStack stack = ResolveAmmoStack(ammoIndex);

        if (stack == null)
            return false;

        if (!UIAudio.ReportResult(_playerInventoryRuntime.TryStoreExternalItemToInventorySlot(
                stack.Bullet,
                stack.Amount,
                -1f,
                targetInventoryIndex,
                out string ammoMessage), UIAudioCueType.InventoryMove))
        {
            RaiseMessage(ammoMessage);
            return false;
        }

        _activeInventory.ClearBulletStack(ammoIndex);
        RaiseMessage(ammoMessage);
        RefreshAfterTake();
        return true;
    }

    // Purpose: Attempts to take slot to player equipment slot and reports whether the request succeeded.
    public bool TryTakeSlotToPlayerEquipmentSlot(
        int enemySlotIndex,
        PlayerInventoryEquipmentSlotKind targetSlotKind,
        int targetEquipmentSlotIndex = 0)
    {
        if (_activeInventory == null || _playerInventoryRuntime == null)
            return false;

        if (enemySlotIndex >= ItemSlotIndexBase)
        {
            int itemIndex = enemySlotIndex - ItemSlotIndexBase;
            PlayerInventoryItemStack stack = ResolveItemStack(itemIndex);

            if (stack == null)
                return false;

            if (!UIAudio.ReportResult(_playerInventoryRuntime.TryEquipExternalItemToSlot(
                    stack.Item,
                    stack.Amount,
                    stack.ArmorDurability,
                    stack.WeaponCurrentDurability,
                    stack.WeaponMaxDurability,
                    stack.GasMaskFilterDurability,
                    stack.ArmorMaxDurability,
                    stack.GasMaskMaxFilterDurability,
                    targetSlotKind,
                    targetEquipmentSlotIndex,
                    out string itemMessage), UIAudioCueType.Equip))
            {
                RaiseMessage(itemMessage);
                return false;
            }

            _activeInventory.ClearItemStack(itemIndex);
            RaiseMessage(itemMessage);
            RefreshAfterTake();
            return true;
        }

        ItemSO item = null;
        int itemAmount = 1;
        float armorDurability = -1f;
        float armorMaxDurability = -1f;
        float weaponCurrentDurability = -1f;
        float weaponMaxDurability = -1f;
        float gasMaskFilterDurability = -1f;
        float gasMaskMaxFilterDurability = -1f;
        int sourceWeaponSlotIndex = -1;

        if (TryDecodeWeaponSlotIndex(enemySlotIndex, out sourceWeaponSlotIndex))
        {
            item = _activeInventory.GetWeapon(sourceWeaponSlotIndex);
            itemAmount = _activeInventory.GetWeaponAmount(sourceWeaponSlotIndex);
            EnemyLootWeaponStack weaponStack = _activeInventory.GetWeaponStack(sourceWeaponSlotIndex);
            weaponCurrentDurability = weaponStack != null ? weaponStack.CurrentDurability : -1f;
            weaponMaxDurability = weaponStack != null ? weaponStack.MaxDurability : -1f;
        }
        else if (enemySlotIndex == ArmorSlotIndex)
        {
            item = _activeInventory.Armor;
            armorDurability = _activeInventory.ArmorDurability;
            armorMaxDurability = item is ArmorItemSO armor
                ? armor.MaxDurability
                : -1f;
        }

        if (item == null)
            return false;

        if (!UIAudio.ReportResult(_playerInventoryRuntime.TryEquipExternalItemToSlot(
                item,
                itemAmount,
                armorDurability,
                weaponCurrentDurability,
                weaponMaxDurability,
                gasMaskFilterDurability,
                armorMaxDurability,
                gasMaskMaxFilterDurability,
                targetSlotKind,
                targetEquipmentSlotIndex,
                out string message), UIAudioCueType.Equip))
        {
            RaiseMessage(message);
            return false;
        }

        if (sourceWeaponSlotIndex >= 0)
            _activeInventory.ClearWeapon(sourceWeaponSlotIndex);
        else if (enemySlotIndex == ArmorSlotIndex)
            _activeInventory.ClearArmor();

        RaiseMessage(message);
        RefreshAfterTake();
        return true;
    }

    // Purpose: Attempts to take ammo to player ammo slot and reports whether the request succeeded.
    public bool TryTakeAmmoToPlayerAmmoSlot(int enemySlotIndex, int targetAmmoSlotIndex)
    {
        if (_activeInventory == null || _playerInventoryRuntime == null)
            return false;

        if (enemySlotIndex >= ItemSlotIndexBase)
        {
            int itemIndex = enemySlotIndex - ItemSlotIndexBase;
            PlayerInventoryItemStack itemStack = ResolveItemStack(itemIndex);

            if (itemStack == null || itemStack.Item is not BulletSO bullet)
                return false;

            if (!UIAudio.ReportResult(_playerInventoryRuntime.TryEquipExternalBulletToAmmoSlot(
                    bullet,
                    itemStack.Amount,
                    targetAmmoSlotIndex,
                    out string itemMessage), UIAudioCueType.Equip))
            {
                RaiseMessage(itemMessage);
                return false;
            }

            _activeInventory.ClearItemStack(itemIndex);
            RaiseMessage(itemMessage);
            RefreshAfterTake();
            return true;
        }

        int ammoIndex = enemySlotIndex - AmmoSlotIndexBase;
        EnemyLootBulletStack stack = ResolveAmmoStack(ammoIndex);

        if (stack == null)
            return false;

        if (!UIAudio.ReportResult(_playerInventoryRuntime.TryEquipExternalBulletToAmmoSlot(
                stack.Bullet,
                stack.Amount,
                targetAmmoSlotIndex,
                out string message), UIAudioCueType.Equip))
        {
            RaiseMessage(message);
            return false;
        }

        _activeInventory.ClearBulletStack(ammoIndex);
        RaiseMessage(message);
        RefreshAfterTake();
        return true;
    }

    // Purpose: Attempts to receive drag payload to enemy inventory slot and reports whether the request succeeded.
    public bool TryReceiveDragPayloadToEnemyInventorySlot(int targetSlotIndex)
    {
        if (_activeInventory == null)
            return UIAudio.ReportResult(false, UIAudioCueType.InventoryMove);

        int itemIndex = ResolveEnemyItemIndex(targetSlotIndex);

        if (!TryPeekCurrentDragPayload(out PlayerInventoryRemovedStack payload, out string message))
        {
            RaiseMessage(message);
            return UIAudio.ReportResult(false, UIAudioCueType.InventoryMove);
        }

        if (IsCurrentEnemyInventorySource(this, targetSlotIndex))
            return UIAudio.ReportResult(false, UIAudioCueType.InventoryMove);

        if (!CanPlaceStackInEnemyInventorySlot(payload, itemIndex, out message))
        {
            RaiseMessage(message);
            return UIAudio.ReportResult(false, UIAudioCueType.InventoryMove);
        }

        if (!TryRemoveCurrentDragPayload(out payload, out message))
        {
            RaiseMessage(message);
            return UIAudio.ReportResult(false, UIAudioCueType.InventoryMove);
        }

        if (!TryPlaceStackInEnemyInventorySlot(payload, itemIndex, out message))
        {
            RaiseMessage(message);
            return UIAudio.ReportResult(false, UIAudioCueType.InventoryMove);
        }

        RaiseMessage(message);
        RefreshDragSources();
        return UIAudio.ReportResult(true, UIAudioCueType.InventoryMove);
    }

    // Purpose: Attempts to receive drag payload to enemy equipment slot and reports whether the request succeeded.
    public bool TryReceiveDragPayloadToEnemyEquipmentSlot(
        PlayerInventoryEquipmentSlotKind targetSlotKind,
        int targetEquipmentSlotIndex = 0)
    {
        if (_activeInventory == null)
            return UIAudio.ReportResult(false, UIAudioCueType.Equip);

        if (IsCurrentEnemyEquipmentSource(
                this,
                targetSlotKind,
                targetEquipmentSlotIndex))
            return UIAudio.ReportResult(false, UIAudioCueType.Equip);

        if (targetSlotKind == PlayerInventoryEquipmentSlotKind.Weapon &&
            InventoryDragContext.SourceKind == InventoryDragSourceKind.EnemyInventory &&
            InventoryDragContext.EnemyInventoryOwner == this &&
            TryDecodeWeaponSlotIndex(
                InventoryDragContext.EnemyInventorySlotIndex,
                out int sourceWeaponSlotIndex))
        {
            if (!UIAudio.ReportResult(_activeInventory.TrySwapWeaponSlots(
                    sourceWeaponSlotIndex,
                    targetEquipmentSlotIndex), UIAudioCueType.InventoryMove))
            {
                return UIAudio.ReportResult(false, UIAudioCueType.Equip);
            }

            RaiseMessage("Enemy weapon slots swapped.");
            RefreshDragSources();
            return UIAudio.ReportResult(true, UIAudioCueType.Equip);
        }

        if (!TryPeekCurrentDragPayload(out PlayerInventoryRemovedStack payload, out string message))
        {
            RaiseMessage(message);
            return UIAudio.ReportResult(false, UIAudioCueType.Equip);
        }

        if (!CanEquipPayloadToEnemyEquipmentSlot(payload, targetSlotKind, out message))
        {
            RaiseMessage(message);
            return UIAudio.ReportResult(false, UIAudioCueType.Equip);
        }

        PlayerInventoryRemovedStack previous = ResolveEnemyEquipmentSlotPayload(
            targetSlotKind,
            targetEquipmentSlotIndex);
        int reservedFreeItemIndex = ResolveCurrentEnemyInventorySourceFreeIndex(this);

        if (previous.HasItem &&
            !CanStoreStackInEnemyInventory(previous, reservedFreeItemIndex, out message))
        {
            RaiseMessage(message);
            return UIAudio.ReportResult(false, UIAudioCueType.Equip);
        }

        if (!TryRemoveCurrentDragPayload(out payload, out message))
        {
            RaiseMessage(message);
            return UIAudio.ReportResult(false, UIAudioCueType.Equip);
        }

        switch (targetSlotKind)
        {
            case PlayerInventoryEquipmentSlotKind.Weapon:
                _activeInventory.SetWeapon(
                    targetEquipmentSlotIndex,
                    payload.item as WeaponSO,
                    payload.amount,
                    payload.weaponCurrentDurability,
                    payload.weaponMaxDurability);
                break;

            case PlayerInventoryEquipmentSlotKind.Armor:
                _activeInventory.SetArmor(
                    payload.item as ArmorItemSO,
                    payload.armorDurability);
                break;
        }

        if (previous.HasItem &&
            !TryPlaceStackInFirstEnemyInventorySlot(previous, out message))
        {
            RaiseMessage(message);
            return UIAudio.ReportResult(false, UIAudioCueType.Equip);
        }

        RaiseMessage($"{InventoryItemDisplayUtility.ResolveItemName(payload.item)} equipped.");
        RefreshDragSources();
        return UIAudio.ReportResult(true, UIAudioCueType.Equip);
    }

    // Purpose: Attempts to receive drag payload to enemy ammo slot and reports whether the request succeeded.
    public bool TryReceiveDragPayloadToEnemyAmmoSlot(int targetAmmoSlotIndex)
    {
        if (_activeInventory == null)
            return UIAudio.ReportResult(false, UIAudioCueType.Equip);

        if (IsCurrentEnemyAmmoSource(this, targetAmmoSlotIndex))
            return UIAudio.ReportResult(false, UIAudioCueType.Equip);

        if (!TryPeekCurrentDragPayload(out PlayerInventoryRemovedStack payload, out string message))
        {
            RaiseMessage(message);
            return UIAudio.ReportResult(false, UIAudioCueType.Equip);
        }

        if (payload.item is not BulletSO bullet)
        {
            RaiseMessage("This item is not ammo.");
            return UIAudio.ReportResult(false, UIAudioCueType.Equip);
        }

        EnemyLootBulletStack previousStack = ResolveAmmoStack(targetAmmoSlotIndex);
        PlayerInventoryRemovedStack previous = previousStack != null
            ? new PlayerInventoryRemovedStack(previousStack.Bullet, previousStack.Amount, -1f)
            : default;

        int reservedFreeItemIndex = ResolveCurrentEnemyInventorySourceFreeIndex(this);

        if (previous.HasItem &&
            !CanStoreStackInEnemyInventory(previous, reservedFreeItemIndex, out message))
        {
            RaiseMessage(message);
            return UIAudio.ReportResult(false, UIAudioCueType.Equip);
        }

        if (!TryRemoveCurrentDragPayload(out payload, out message))
        {
            RaiseMessage(message);
            return UIAudio.ReportResult(false, UIAudioCueType.Equip);
        }

        _activeInventory.SetBulletStack(
            targetAmmoSlotIndex,
            bullet,
            Mathf.Max(1, payload.amount));

        if (previous.HasItem &&
            !TryPlaceStackInFirstEnemyInventorySlot(previous, out message))
        {
            RaiseMessage(message);
            return UIAudio.ReportResult(false, UIAudioCueType.Equip);
        }

        RaiseMessage($"{bullet.DisplayName} equipped.");
        RefreshDragSources();
        return UIAudio.ReportResult(true, UIAudioCueType.Equip);
    }

    // Purpose: Closes the active panel UI or workflow.
    public void ClosePanel()
    {
        _overlayRequestChannel?.Close(UIOverlayId.LootHub);
    }

    // Purpose: Handles the incoming open requested request or event.
    private void HandleOpenRequested(EnemyLootOpenRequest request)
    {
        _activeInventory = request.lootInventory;
        _dropActor = request.actor != null ? request.actor.transform : _dropActor;
        if (_equipmentSectionRoot != null)
            _equipmentSectionRoot.SetActive(_activeInventory == null || _activeInventory.ShowsEquipment);
        ApplyContextPresentation();
        ShowPanel();
        Refresh();
        ResetInventoryScrollToTop();
    }

    // Purpose: Handles the incoming player inventory runtime ready request or event.
    private void HandlePlayerInventoryRuntimeReady(PlayerInventoryRuntime runtime)
    {
        _playerInventoryRuntime = runtime;
    }

    // Purpose: Refreshes the displayed or cached after take.
    private void RefreshAfterTake()
    {
        Refresh();

        if (_activeInventory != null && !_activeInventory.CanInteract)
            ClosePanel();
    }

    // Purpose: Refreshes the displayed or cached drag sources.
    private void RefreshDragSources()
    {
        Refresh();

        EnemyInventoryUI sourceEnemy = InventoryDragContext.EnemyInventoryOwner;

        if (sourceEnemy != null && sourceEnemy != this)
            sourceEnemy.Refresh();
    }

    // Purpose: Refreshes the displayed or cached refresh.
    private void Refresh()
    {
        if (_activeInventory != null)
            _activeInventory.SyncArmorDurabilityFromEquipment();

        RefreshHeader();
        RefreshEquippedSlots();
        RefreshAmmoSlots();
        RefreshItemSlots();
    }

    /// <summary>
    /// Reflows the right side of the two-column Loot workspace. Character sources
    /// retain their equipment block; chest/object sources hide that block and
    /// let the inventory section immediately consume the released vertical space.
    /// </summary>
    public void ApplyContextPresentation()
    {
        RectTransform headerSection = FindChildRect("HeaderSection");
        RectTransform inventoryArea = FindChildRect("InventoryArea");
        RectTransform inventorySection = FindChildRect("InventorySection");
        RectTransform inventoryHeader = inventorySection != null
            ? inventorySection.Find("Header") as RectTransform
            : null;
        RectTransform inventoryScroll = FindChildRect("InventoryScrollView");
        RectTransform equipmentSection = _equipmentSectionRoot != null
            ? _equipmentSectionRoot.transform as RectTransform
            : FindChildRect("EquipmentSection");

        const float outerInset = 24f;
        const float headerTopInset = 18f;
        const float headerHeight = 54f;
        const float sectionGap = 10f;
        const float equipmentHeight = 146f;

        // InventoryArea is the full-height Loot backdrop. It must render
        // behind the HeaderSection; otherwise its Image appears in front of
        // the Loot title and makes the layout look like duplicate panels.
        inventoryArea?.SetAsFirstSibling();
        AnchorTop(headerSection, headerTopInset, headerHeight, outerInset);

        bool showsEquipment = _activeInventory == null || _activeInventory.ShowsEquipment;
        if (_equipmentSectionRoot != null && _equipmentSectionRoot.activeSelf != showsEquipment)
            _equipmentSectionRoot.SetActive(showsEquipment);

        if (equipmentSection != null)
        {
            equipmentSection.anchorMin = new Vector2(0f, 1f);
            equipmentSection.anchorMax = new Vector2(1f, 1f);
            equipmentSection.pivot = new Vector2(0.5f, 1f);
            equipmentSection.anchoredPosition = new Vector2(0f, -(headerTopInset + headerHeight + sectionGap));
            equipmentSection.sizeDelta = new Vector2(-outerInset * 2f, equipmentHeight);
            ConfigureEquipmentRow(equipmentSection);
        }

        // Keep the scroll content after the equipment surface in hierarchy as
        // well as vertically, so it cannot cover the equipment header while
        // the UI is being edited in Scene view.
        inventorySection?.SetAsLastSibling();

        // The inventory always owns the remaining vertical space. Object loot
        // has no equipment surface, so it starts directly below the title
        // instead of preserving an empty character-equipment gap.
        float inventoryTopInset = headerTopInset + headerHeight + sectionGap;
        if (showsEquipment)
            inventoryTopInset += equipmentHeight + sectionGap;

        // InventoryArea owns the full Loot-panel backdrop, matching the
        // Player panel's full-height background. InventorySection is only the
        // lower content surface and moves below Equipment when it is shown.
        Stretch(inventoryArea, Vector2.zero, Vector2.zero);
        Stretch(inventorySection,
            new Vector2(outerInset, outerInset),
            new Vector2(-outerInset, -inventoryTopInset));
        AnchorTop(inventoryHeader, 0f, 32f, 0f);
        Stretch(inventoryScroll, new Vector2(16f, 14f), new Vector2(-16f, -42f));

        Canvas.ForceUpdateCanvases();
        if (_inventorySlotRoot is RectTransform content)
            LayoutRebuilder.ForceRebuildLayoutImmediate(content);
    }

    // Purpose: Keeps character-loot equipment in one compact row so it cannot
    // overflow into the inventory surface beneath it.
    private static void ConfigureEquipmentRow(RectTransform equipmentSection)
    {
        if (equipmentSection == null)
            return;

        RectTransform body = equipmentSection.Find("Body") as RectTransform;
        RectTransform header = equipmentSection.Find("Header") as RectTransform;
        AnchorTop(header, 0f, 24f, 0f);
        Stretch(body, Vector2.zero, new Vector2(0f, -24f));

        if (body == null || !body.TryGetComponent(out GridLayoutGroup grid))
            return;

        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 6;
        grid.cellSize = new Vector2(104f, 112f);
        grid.spacing = new Vector2(8f, 0f);
        grid.childAlignment = TextAnchor.UpperLeft;
    }

    // Purpose: Finds one authored loot RectTransform by name without expanding inspector wiring.
    private RectTransform FindChildRect(string childName)
    {
        foreach (RectTransform rect in GetComponentsInChildren<RectTransform>(true))
        {
            if (rect != null && rect.name == childName)
                return rect;
        }

        return null;
    }

    // Purpose: Anchors a child to fill its parent with offsets.
    private static void Stretch(RectTransform rect, Vector2 offsetMin, Vector2 offsetMax)
    {
        if (rect == null)
            return;

        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
    }

    // Purpose: Anchors a child as a full-width top strip.
    private static void AnchorTop(RectTransform rect, float topInset, float height, float sideInset)
    {
        if (rect == null)
            return;

        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(0f, -topInset);
        rect.sizeDelta = new Vector2(-sideInset * 2f, height);
    }

    // Purpose: Refreshes the displayed or cached header.
    private void RefreshHeader()
    {
        if (_enemyNameText == null)
            return;

        string enemyName = _activeInventory != null
            ? _activeInventory.LootDisplayName
            : string.Empty;

        _enemyNameText.text = string.IsNullOrWhiteSpace(enemyName)
            ? _emptyLootName
            : string.Format(_enemyNameFormat, enemyName);
    }

    // Purpose: Refreshes the displayed or cached equipped slots.
    private void RefreshEquippedSlots()
    {
        if (_weaponSlots != null)
        {
            for (int i = 0; i < _weaponSlots.Length; i++)
            {
                EquipmentInventoryWeaponSlotUI slot = _weaponSlots[i];

                if (slot == null)
                    continue;

                int weaponSlotIndex = slot.WeaponSlotIndex;
                WeaponSO weapon = _activeInventory != null
                    ? _activeInventory.GetWeapon(weaponSlotIndex)
                    : null;

                EnemyLootWeaponStack stack = _activeInventory != null
                    ? _activeInventory.GetWeaponStack(weaponSlotIndex)
                    : null;
                slot.Bind(
                    weapon,
                    stack != null ? stack.CurrentDurability : -1f,
                    stack != null ? stack.MaxDurability : -1f);
            }
        }

        if (_armorSlot != null)
        {
            ArmorItemSO armor = _activeInventory != null ? _activeInventory.Armor : null;
            float durability = _activeInventory != null ? _activeInventory.ArmorDurability : -1f;
            float maxDurability = armor != null ? armor.MaxDurability : -1f;
            _armorSlot.Bind(armor, durability, maxDurability);
        }
    }

    // Purpose: Refreshes the displayed or cached ammo slots.
    private void RefreshAmmoSlots()
    {
        if (_ammoSlots == null)
            return;

        for (int i = 0; i < _ammoSlots.Length; i++)
        {
            EquipmentInventoryAmmoSlotUI slot = _ammoSlots[i];

            if (slot == null)
                continue;

            EnemyLootBulletStack stack = ResolveAmmoStack(i);
            slot.Bind(
                stack != null ? stack.Bullet : null,
                stack != null ? stack.Amount : 0);
        }
    }

    // Purpose: Refreshes the displayed or cached item slots.
    private void RefreshItemSlots()
    {
        int slotCount = _activeInventory != null && _activeInventory.ItemStacks != null
            ? _activeInventory.ItemStacks.Count
            : 0;

        EnsureItemSlots(slotCount);

        for (int i = 0; i < _itemSlots.Count; i++)
        {
            InventoryItemSlotUI slot = _itemSlots[i];

            if (slot == null)
                continue;

            bool active = i < slotCount;
            slot.gameObject.SetActive(active);

            if (active)
                slot.Bind(ResolveRawItemStack(i));
        }

        RebuildInventoryLayout();
    }

    // Purpose: Resolves the ammo stack needed by this component.
    private EnemyLootBulletStack ResolveAmmoStack(int index)
    {
        if (_activeInventory == null || _activeInventory.BulletStacks == null)
            return null;

        if (index < 0 || index >= _activeInventory.BulletStacks.Count)
            return null;

        EnemyLootBulletStack stack = _activeInventory.BulletStacks[index];
        return stack != null && stack.IsAvailable ? stack : null;
    }

    // Purpose: Resolves the item stack needed by this component.
    private PlayerInventoryItemStack ResolveItemStack(int index)
    {
        PlayerInventoryItemStack stack = ResolveRawItemStack(index);
        return stack != null && !stack.IsEmpty ? stack : null;
    }

    // Purpose: Resolves the raw item stack needed by this component.
    private PlayerInventoryItemStack ResolveRawItemStack(int index)
    {
        if (_activeInventory == null || _activeInventory.ItemStacks == null)
            return null;

        if (index < 0 || index >= _activeInventory.ItemStacks.Count)
            return null;

        return _activeInventory.ItemStacks[index];
    }

    // Purpose: Attempts to peek current drag payload and reports whether the request succeeded.
    private bool TryPeekCurrentDragPayload(
        out PlayerInventoryRemovedStack payload,
        out string message)
    {
        payload = default;
        message = string.Empty;

        switch (InventoryDragContext.SourceKind)
        {
            case InventoryDragSourceKind.Inventory:
                return TryPeekPlayerInventoryPayload(
                    InventoryDragContext.InventoryIndex,
                    out payload,
                    out message);

            case InventoryDragSourceKind.Equipment:
                return TryPeekPlayerEquipmentPayload(
                    InventoryDragContext.EquipmentSlotKind,
                    InventoryDragContext.EquipmentSlotIndex,
                    out payload,
                    out message);

            case InventoryDragSourceKind.EquipmentAmmo:
                return TryPeekPlayerAmmoPayload(
                    InventoryDragContext.EquipmentAmmoSlotIndex,
                    out payload,
                    out message);

            case InventoryDragSourceKind.EnemyInventory:
                if (InventoryDragContext.EnemyInventoryOwner == null)
                    return Fail("Enemy inventory source is missing.", out message);

                return InventoryDragContext.EnemyInventoryOwner.TryPeekEnemySlotPayload(
                    InventoryDragContext.EnemyInventorySlotIndex,
                    out payload,
                    out message);
        }

        return Fail("Drag payload is missing.", out message);
    }

    // Purpose: Attempts to remove current drag payload and reports whether the request succeeded.
    private bool TryRemoveCurrentDragPayload(
        out PlayerInventoryRemovedStack payload,
        out string message)
    {
        payload = default;
        message = string.Empty;

        switch (InventoryDragContext.SourceKind)
        {
            case InventoryDragSourceKind.Inventory:
                if (_playerInventoryRuntime == null)
                    return Fail("Player inventory is missing.", out message);

                return _playerInventoryRuntime.TryRemoveInventoryItem(
                    InventoryDragContext.InventoryIndex,
                    0,
                    out payload,
                    out message);

            case InventoryDragSourceKind.Equipment:
                if (_playerInventoryRuntime == null)
                    return Fail("Player inventory is missing.", out message);

                return _playerInventoryRuntime.TryRemoveEquipped(
                    InventoryDragContext.EquipmentSlotKind,
                    InventoryDragContext.EquipmentSlotIndex,
                    out payload,
                    out message);

            case InventoryDragSourceKind.EquipmentAmmo:
                if (_playerInventoryRuntime == null)
                    return Fail("Player inventory is missing.", out message);

                return _playerInventoryRuntime.TryRemoveAmmoSlot(
                    InventoryDragContext.EquipmentAmmoSlotIndex,
                    out payload,
                    out message);

            case InventoryDragSourceKind.EnemyInventory:
                if (InventoryDragContext.EnemyInventoryOwner == null)
                    return Fail("Enemy inventory source is missing.", out message);

                return InventoryDragContext.EnemyInventoryOwner.TryRemoveEnemySlotPayload(
                    InventoryDragContext.EnemyInventorySlotIndex,
                    out payload,
                    out message);
        }

        return Fail("Drag payload is missing.", out message);
    }

    // Purpose: Attempts to peek player inventory payload and reports whether the request succeeded.
    private bool TryPeekPlayerInventoryPayload(
        int inventoryIndex,
        out PlayerInventoryRemovedStack payload,
        out string message)
    {
        payload = default;

        if (_playerInventoryRuntime == null ||
            _playerInventoryRuntime.Items == null ||
            inventoryIndex < 0 ||
            inventoryIndex >= _playerInventoryRuntime.Items.Count)
        {
            return Fail("Invalid player inventory slot.", out message);
        }

        PlayerInventoryItemStack stack = _playerInventoryRuntime.Items[inventoryIndex];

        if (stack == null || stack.IsEmpty)
            return Fail("Player inventory slot is empty.", out message);

        payload = new PlayerInventoryRemovedStack(
            stack.Item,
            stack.Amount,
            stack.ArmorDurability,
            stack.WeaponCurrentDurability,
            stack.WeaponMaxDurability);
        message = string.Empty;
        return true;
    }

    // Purpose: Attempts to peek player equipment payload and reports whether the request succeeded.
    private bool TryPeekPlayerEquipmentPayload(
        PlayerInventoryEquipmentSlotKind slotKind,
        int equipmentSlotIndex,
        out PlayerInventoryRemovedStack payload,
        out string message)
    {
        payload = default;

        if (_playerInventoryRuntime == null)
            return Fail("Player inventory is missing.", out message);

        switch (slotKind)
        {
            case PlayerInventoryEquipmentSlotKind.Weapon:
                WeaponSO weapon = _playerInventoryRuntime.GetEquippedWeapon(equipmentSlotIndex);
                int weaponAmount = _playerInventoryRuntime.GetEquippedWeaponAmount(equipmentSlotIndex);

                if (weapon == null)
                    return Fail("No player weapon equipped.", out message);

                payload = new PlayerInventoryRemovedStack(
                    weapon,
                    weaponAmount,
                    -1f,
                    _playerInventoryRuntime.GetEquippedWeaponCurrentDurability(equipmentSlotIndex),
                    _playerInventoryRuntime.GetEquippedWeaponMaxDurability(equipmentSlotIndex));
                message = string.Empty;
                return true;

            case PlayerInventoryEquipmentSlotKind.Armor:
                if (_playerInventoryRuntime.EquippedArmor == null)
                    return Fail("No player armor equipped.", out message);

                payload = new PlayerInventoryRemovedStack(
                    _playerInventoryRuntime.EquippedArmor,
                    1,
                    _playerInventoryRuntime.EquippedArmorDurability);
                message = string.Empty;
                return true;
        }

        return Fail("Unsupported equipment slot.", out message);
    }

    // Purpose: Attempts to peek player ammo payload and reports whether the request succeeded.
    private bool TryPeekPlayerAmmoPayload(
        int ammoSlotIndex,
        out PlayerInventoryRemovedStack payload,
        out string message)
    {
        payload = default;

        if (_playerInventoryRuntime == null)
            return Fail("Player inventory is missing.", out message);

        if (!_playerInventoryRuntime.TryGetAmmoSlotSnapshot(
                ammoSlotIndex,
                out WeaponAmmoSlotSnapshot snapshot) ||
            snapshot.isEmpty ||
            snapshot.bullet == null ||
            snapshot.totalAmmo <= 0)
        {
            return Fail("No player ammo equipped.", out message);
        }

        payload = new PlayerInventoryRemovedStack(
            snapshot.bullet,
            Mathf.Max(1, snapshot.totalAmmo),
            -1f);
        message = string.Empty;
        return true;
    }

    // Purpose: Attempts to peek enemy slot payload and reports whether the request succeeded.
    private bool TryPeekEnemySlotPayload(
        int enemySlotIndex,
        out PlayerInventoryRemovedStack payload,
        out string message)
    {
        payload = default;

        if (_activeInventory == null)
            return Fail("Enemy inventory is missing.", out message);

        if (TryDecodeWeaponSlotIndex(enemySlotIndex, out int weaponSlotIndex))
        {
            WeaponSO weapon = _activeInventory.GetWeapon(weaponSlotIndex);
            int amount = _activeInventory.GetWeaponAmount(weaponSlotIndex);
            EnemyLootWeaponStack weaponStack = _activeInventory.GetWeaponStack(weaponSlotIndex);

            if (weapon == null)
                return Fail("Enemy weapon slot is empty.", out message);

            payload = new PlayerInventoryRemovedStack(
                weapon,
                amount,
                -1f,
                weaponStack != null ? weaponStack.CurrentDurability : -1f,
                weaponStack != null ? weaponStack.MaxDurability : -1f);
            message = string.Empty;
            return true;
        }

        if (enemySlotIndex == ArmorSlotIndex)
        {
            if (_activeInventory.Armor == null)
                return Fail("Enemy armor slot is empty.", out message);

            payload = new PlayerInventoryRemovedStack(
                _activeInventory.Armor,
                1,
                _activeInventory.ArmorDurability);
            message = string.Empty;
            return true;
        }

        if (enemySlotIndex >= ItemSlotIndexBase)
        {
            PlayerInventoryItemStack stack =
                ResolveItemStack(enemySlotIndex - ItemSlotIndexBase);

            if (stack == null)
                return Fail("Enemy inventory slot is empty.", out message);

            payload = new PlayerInventoryRemovedStack(
                stack.Item,
                stack.Amount,
                stack.ArmorDurability,
                stack.WeaponCurrentDurability,
                stack.WeaponMaxDurability);
            message = string.Empty;
            return true;
        }

        EnemyLootBulletStack ammo = ResolveAmmoStack(enemySlotIndex - AmmoSlotIndexBase);

        if (ammo == null)
            return Fail("Enemy ammo slot is empty.", out message);

        payload = new PlayerInventoryRemovedStack(
            ammo.Bullet,
            ammo.Amount,
            -1f);
        message = string.Empty;
        return true;
    }

    // Purpose: Attempts to remove enemy slot payload and reports whether the request succeeded.
    private bool TryRemoveEnemySlotPayload(
        int enemySlotIndex,
        out PlayerInventoryRemovedStack payload,
        out string message)
    {
        if (!TryPeekEnemySlotPayload(enemySlotIndex, out payload, out message))
            return false;

        if (TryDecodeWeaponSlotIndex(enemySlotIndex, out int weaponSlotIndex))
        {
            _activeInventory.ClearWeapon(weaponSlotIndex);
            return true;
        }

        if (enemySlotIndex == ArmorSlotIndex)
        {
            _activeInventory.ClearArmor();
            return true;
        }

        if (enemySlotIndex >= ItemSlotIndexBase)
        {
            _activeInventory.ClearItemStack(enemySlotIndex - ItemSlotIndexBase);
            return true;
        }

        _activeInventory.ClearBulletStack(enemySlotIndex - AmmoSlotIndexBase);
        return true;
    }

    // Purpose: Checks whether equip payload to enemy equipment slot is currently permitted.
    private bool CanEquipPayloadToEnemyEquipmentSlot(
        PlayerInventoryRemovedStack payload,
        PlayerInventoryEquipmentSlotKind targetSlotKind,
        out string message)
    {
        switch (targetSlotKind)
        {
            case PlayerInventoryEquipmentSlotKind.Weapon:
                if (payload.item is WeaponSO)
                {
                    message = string.Empty;
                    return true;
                }

                return Fail("This item is not a weapon.", out message);

            case PlayerInventoryEquipmentSlotKind.Armor:
                if (payload.item is ArmorItemSO)
                {
                    message = string.Empty;
                    return true;
                }

                return Fail("This item is not armor.", out message);
        }

        return Fail("Unsupported equipment slot.", out message);
    }

    // Purpose: Resolves the enemy equipment slot payload needed by this component.
    private PlayerInventoryRemovedStack ResolveEnemyEquipmentSlotPayload(
        PlayerInventoryEquipmentSlotKind slotKind,
        int equipmentSlotIndex)
    {
        if (_activeInventory == null)
            return default;

        return slotKind switch
        {
            PlayerInventoryEquipmentSlotKind.Weapon
                when _activeInventory.GetWeapon(equipmentSlotIndex) != null =>
                new PlayerInventoryRemovedStack(
                    _activeInventory.GetWeapon(equipmentSlotIndex),
                    _activeInventory.GetWeaponAmount(equipmentSlotIndex),
                    -1f,
                    _activeInventory.GetWeaponStack(equipmentSlotIndex).CurrentDurability,
                    _activeInventory.GetWeaponStack(equipmentSlotIndex).MaxDurability),

            PlayerInventoryEquipmentSlotKind.Armor when _activeInventory.Armor != null =>
                new PlayerInventoryRemovedStack(
                    _activeInventory.Armor,
                    1,
                    _activeInventory.ArmorDurability),

            _ => default
        };
    }

    // Purpose: Checks whether store stack in enemy inventory is currently permitted.
    private bool CanStoreStackInEnemyInventory(
        PlayerInventoryRemovedStack stack,
        int reservedFreeItemIndex,
        out string message)
    {
        message = string.Empty;

        if (!stack.HasItem)
            return true;

        int maxStack = Mathf.Max(1, stack.item.MaxStack);

        if (stack.amount > maxStack && stack.item.MaxStack <= 1)
            return Fail("Enemy inventory cannot hold this stack.", out message);

        if (reservedFreeItemIndex >= 0 &&
            reservedFreeItemIndex < (_activeInventory?.ItemStacks?.Count ?? 0) &&
            stack.amount <= maxStack)
        {
            return true;
        }

        for (int i = 0; i < (_activeInventory?.ItemStacks?.Count ?? 0); i++)
        {
            if (CanPlaceStackInEnemyInventorySlot(stack, i, out _))
                return true;
        }

        return Fail("Enemy inventory is full.", out message);
    }

    // Purpose: Attempts to place stack in first enemy inventory slot and reports whether the request succeeded.
    private bool TryPlaceStackInFirstEnemyInventorySlot(
        PlayerInventoryRemovedStack stack,
        out string message)
    {
        for (int i = 0; i < (_activeInventory?.ItemStacks?.Count ?? 0); i++)
        {
            if (!CanPlaceStackInEnemyInventorySlot(stack, i, out _))
                continue;

            return TryPlaceStackInEnemyInventorySlot(stack, i, out message);
        }

        return Fail("Enemy inventory is full.", out message);
    }

    // Purpose: Checks whether place stack in enemy inventory slot is currently permitted.
    private bool CanPlaceStackInEnemyInventorySlot(
        PlayerInventoryRemovedStack stack,
        int itemIndex,
        out string message)
    {
        message = string.Empty;

        if (!stack.HasItem)
            return Fail("Dragged item is missing.", out message);

        if (_activeInventory == null ||
            _activeInventory.ItemStacks == null ||
            itemIndex < 0 ||
            itemIndex >= _activeInventory.ItemStacks.Count)
        {
            return Fail("Invalid enemy inventory slot.", out message);
        }

        int maxStack = Mathf.Max(1, stack.item.MaxStack);

        if (stack.amount > maxStack)
            return Fail("Target slot cannot hold the full stack.", out message);

        PlayerInventoryItemStack target = ResolveRawItemStack(itemIndex);

        if (target == null || target.IsEmpty)
            return true;

        if (stack.item.MaxStack > 1 &&
            target.Item == stack.item &&
            target.Amount + stack.amount <= maxStack)
        {
            return true;
        }

        return Fail("Target enemy inventory slot is occupied.", out message);
    }

    // Purpose: Attempts to place stack in enemy inventory slot and reports whether the request succeeded.
    private bool TryPlaceStackInEnemyInventorySlot(
        PlayerInventoryRemovedStack stack,
        int itemIndex,
        out string message)
    {
        if (!CanPlaceStackInEnemyInventorySlot(stack, itemIndex, out message))
            return false;

        PlayerInventoryItemStack target = ResolveRawItemStack(itemIndex);

        if (target == null || target.IsEmpty)
        {
            _activeInventory.SetItemStack(
                itemIndex,
                stack.item,
                stack.amount,
                stack.armorDurability,
                stack.weaponCurrentDurability,
                stack.weaponMaxDurability,
                stack.gasMaskFilterDurability,
                stack.armorMaxDurability,
                stack.gasMaskMaxFilterDurability);
        }
        else
        {
                target.Set(
                    stack.item,
                    target.Amount + stack.amount,
                    target.ArmorDurability,
                    target.WeaponCurrentDurability,
                    target.WeaponMaxDurability,
                    target.GasMaskFilterDurability,
                    target.ArmorMaxDurability,
                    target.GasMaskMaxFilterDurability);
        }

        message = $"{InventoryItemDisplayUtility.ResolveItemName(stack.item)} moved.";
        return true;
    }

    // Purpose: Resolves the enemy item index needed by this component.
    private static int ResolveEnemyItemIndex(int slotIndex)
    {
        return slotIndex >= ItemSlotIndexBase
            ? slotIndex - ItemSlotIndexBase
            : slotIndex;
    }

    // Purpose: Performs the encode weapon slot index workflow for this component.
    private static int EncodeWeaponSlotIndex(int weaponSlotIndex)
    {
        return WeaponSlotIndexBase - Mathf.Max(0, weaponSlotIndex);
    }

    // Purpose: Attempts to decode weapon slot index and reports whether the request succeeded.
    private static bool TryDecodeWeaponSlotIndex(
        int slotIndex,
        out int weaponSlotIndex)
    {
        if (slotIndex > WeaponSlotIndexBase)
        {
            weaponSlotIndex = -1;
            return false;
        }

        weaponSlotIndex = WeaponSlotIndexBase - slotIndex;
        return true;
    }

    // Purpose: Checks whether current enemy inventory source is currently true.
    private static bool IsCurrentEnemyInventorySource(
        EnemyInventoryUI targetOwner,
        int targetSlotIndex)
    {
        return InventoryDragContext.SourceKind == InventoryDragSourceKind.EnemyInventory &&
               InventoryDragContext.EnemyInventoryOwner == targetOwner &&
               InventoryDragContext.EnemyInventorySlotIndex == targetSlotIndex;
    }

    // Purpose: Checks whether current enemy equipment source is currently true.
    private static bool IsCurrentEnemyEquipmentSource(
        EnemyInventoryUI targetOwner,
        PlayerInventoryEquipmentSlotKind slotKind,
        int equipmentSlotIndex)
    {
        int sourceIndex = slotKind == PlayerInventoryEquipmentSlotKind.Weapon
            ? EncodeWeaponSlotIndex(equipmentSlotIndex)
            : ArmorSlotIndex;

        return InventoryDragContext.SourceKind == InventoryDragSourceKind.EnemyInventory &&
               InventoryDragContext.EnemyInventoryOwner == targetOwner &&
               InventoryDragContext.EnemyInventorySlotIndex == sourceIndex;
    }

    // Purpose: Checks whether current enemy ammo source is currently true.
    private static bool IsCurrentEnemyAmmoSource(
        EnemyInventoryUI targetOwner,
        int ammoSlotIndex)
    {
        return InventoryDragContext.SourceKind == InventoryDragSourceKind.EnemyInventory &&
               InventoryDragContext.EnemyInventoryOwner == targetOwner &&
               InventoryDragContext.EnemyInventorySlotIndex == ammoSlotIndex;
    }

    // Purpose: Resolves the current enemy inventory source free index needed by this component.
    private static int ResolveCurrentEnemyInventorySourceFreeIndex(EnemyInventoryUI targetOwner)
    {
        if (InventoryDragContext.SourceKind != InventoryDragSourceKind.EnemyInventory ||
            InventoryDragContext.EnemyInventoryOwner != targetOwner ||
            InventoryDragContext.EnemyInventorySlotIndex < ItemSlotIndexBase)
        {
            return -1;
        }

        return InventoryDragContext.EnemyInventorySlotIndex - ItemSlotIndexBase;
    }

    // Purpose: Resolves the preferred equipment slot kind needed by this component.
    private PlayerInventoryEquipmentSlotKind ResolvePreferredEquipmentSlotKind(int inventoryIndex)
    {
        if (inventoryIndex < ItemSlotIndexBase)
            return PlayerInventoryEquipmentSlotKind.Weapon;

        PlayerInventoryItemStack stack = ResolveItemStack(inventoryIndex - ItemSlotIndexBase);

        return stack != null && stack.Item is ArmorItemSO
            ? PlayerInventoryEquipmentSlotKind.Armor
            : PlayerInventoryEquipmentSlotKind.Weapon;
    }

    // Purpose: Initializes slots for use by this component.
    private void InitializeSlots()
    {
        if (_contextMenu != null)
            _contextMenu.Initialize(this);

        if (_weaponSlots != null)
        {
            for (int i = 0; i < _weaponSlots.Length; i++)
            {
                EquipmentInventoryWeaponSlotUI slot = _weaponSlots[i];

                if (slot == null)
                    continue;

                slot.Initialize(
                    this,
                    InventoryDragSourceKind.EnemyInventory,
                    EncodeWeaponSlotIndex(slot.WeaponSlotIndex));
            }
        }

        if (_armorSlot != null)
        {
            _armorSlot.Initialize(
                this,
                InventoryDragSourceKind.EnemyInventory,
                ArmorSlotIndex);
        }

        if (_ammoSlots == null)
        {
            InitializeItemSlots();
            return;
        }

        for (int i = 0; i < _ammoSlots.Length; i++)
        {
            if (_ammoSlots[i] != null)
            {
                _ammoSlots[i].Initialize(
                    this,
                    AmmoSlotIndexBase + i,
                    InventoryDragSourceKind.EnemyInventory,
                    AmmoSlotIndexBase + i);
            }
        }

        InitializeItemSlots();
    }

    // Purpose: Initializes item slots for use by this component.
    private void InitializeItemSlots()
    {
        for (int i = 0; i < _itemSlots.Count; i++)
        {
            if (_itemSlots[i] == null)
                continue;

            _itemSlots[i].Initialize(
                this,
                ItemSlotIndexBase + i,
                true,
                true,
                InventoryDragSourceKind.EnemyInventory);
        }
    }

    // Purpose: Ensures item slots is ready before the workflow continues.
    private void EnsureItemSlots(int count)
    {
        if (_inventorySlotPrefab == null || _inventorySlotRoot == null)
            return;

        count = Mathf.Max(0, count);

        while (_itemSlots.Count < count)
        {
            InventoryItemSlotUI slot = Instantiate(
                _inventorySlotPrefab,
                _inventorySlotRoot);

            int slotIndex = _itemSlots.Count;
            slot.gameObject.SetActive(true);
            slot.Initialize(
                this,
                ItemSlotIndexBase + slotIndex,
                true,
                true,
                InventoryDragSourceKind.EnemyInventory);

            _itemSlots.Add(slot);
        }
    }

    // Purpose: Repairs or rebuilds inventory layout from the current source data.
    private void RebuildInventoryLayout()
    {
        if (_inventorySlotRoot is not RectTransform content)
            return;

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(content);
    }

    // Purpose: Performs the reset inventory scroll to top workflow for this component.
    private void ResetInventoryScrollToTop()
    {
        RebuildInventoryLayout();

        if (_inventoryScrollRect != null)
            _inventoryScrollRect.verticalNormalizedPosition = 1f;
    }

    // Purpose: Hides the slot template if needed UI or visual state.
    private void HideSlotTemplateIfNeeded()
    {
        if (_hideInventorySlotPrefabOnAwake && _inventorySlotPrefab != null)
            _inventorySlotPrefab.gameObject.SetActive(false);
    }

    // Purpose: Resolves the refs needed by this component.
    private void ResolveRefs()
    {
        if (_playerInventoryRuntime == null)
            _playerInventoryRuntime = FindAnyObjectByType<PlayerInventoryRuntime>();

        if (_contextMenu == null)
            _contextMenu = GetComponentInChildren<InventoryItemContextMenuUI>(true);
        if (_dropActor == null && _playerInventoryRuntime != null)
            _dropActor = _playerInventoryRuntime.transform;
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

    // Purpose: Publishes the message notification.
    private void RaiseMessage(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
            return;

        if (_systemMessageChannel != null)
            _systemMessageChannel.RaiseEvent(message);
        else
            Debug.Log($"[EnemyInventory] {message}", this);
    }

    // Purpose: Marks the active fail workflow as failed.
    private static bool Fail(string failureMessage, out string message)
    {
        message = failureMessage;
        return false;
    }

    // Purpose: Attempts to spawn world drop and reports whether the request succeeded.
    private bool TrySpawnWorldDrop(PlayerInventoryRemovedStack removed)
    {
        if (!removed.HasItem)
            return false;

        Transform dropSource = _dropActor != null
            ? _dropActor
            : _playerInventoryRuntime != null ? _playerInventoryRuntime.transform : transform;

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

        return WorldItemPickupSpawner.TrySpawn(
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
            out _);
    }
}
