using System;
using System.Collections.Generic;
using UnityEngine;

public enum PlayerInventoryInteractionMode
{
    Normal = 0,
    SellSelect = 1
}

[DisallowMultipleComponent]
public class PlayerInventoryUI : MonoBehaviour,
    IInventorySlotOwner,
    IInventoryItemContextMenuOwner
{
    [Header("Refs")]
    [SerializeField] private SystemMessageEventChannelSO _systemMessageChannel;

    [Header("Runtime Ready")]
    [SerializeField] private PlayerInventoryRuntimeReadyEventChannelSO _inventoryRuntimeReadyChannel;

    [Header("Equipment Slots")]
    [SerializeField] private EquipmentInventoryArmorSlotUI _armorSlot;
    [SerializeField] private EquipmentInventoryWeaponSlotUI _weaponSlot;
    [SerializeField] private EquipmentInventoryAmmoSlotUI[] _ammoSlots;

    [Header("Inventory Slot Instancing")]
    [SerializeField] private InventoryItemSlotUI _inventorySlotPrefab;
    [SerializeField] private Transform _inventorySlotRoot;
    [SerializeField] private bool _hideInventorySlotPrefabOnAwake = true;

    [Header("Context Menu")]
    [SerializeField] private InventoryItemContextMenuUI _contextMenu;

    [Header("World Drop")]
    [SerializeField, Min(0f)] private float _dropForwardOffset = 1f;
    [SerializeField] private float _dropUpOffset = 0.05f;

    private Transform _dropActor;
    private readonly List<InventoryItemSlotUI> _inventorySlots = new();

    private PlayerInventoryRuntime _inventoryRuntime;
    private PlayerInventoryInteractionMode _interactionMode = PlayerInventoryInteractionMode.Normal;
    private int _selectedInventoryIndex = -1;
    private PlayerInventoryItemSelection _selectedItemSelection = PlayerInventoryItemSelection.None;

    public PlayerInventoryRuntime InventoryRuntime => _inventoryRuntime;
    public PlayerInventoryInteractionMode InteractionMode => _interactionMode;
    public int SelectedInventoryIndex => _selectedInventoryIndex;
    public PlayerInventoryItemStack SelectedItemStack => ResolveSelectedStack();
    public PlayerInventoryItemSelection SelectedItemSelection => _selectedItemSelection;

    public event Action<int, PlayerInventoryItemStack> OnInventorySelectionChanged;
    public event Action<PlayerInventoryItemSelection> OnItemSelectionChanged;

    private void Reset()
    {
        ResolveRefs();
    }

    private void Awake()
    {
        ResolveRefs();
        InitializeStaticSlots();
        HideSlotTemplateIfNeeded();
    }

    private void OnEnable()
    {
        ResolveRefs();
        InitializeStaticSlots();
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
        }

        Refresh();
    }

    private void OnDisable()
    {
        if (_inventoryRuntime != null)
            _inventoryRuntime.OnChanged -= Refresh;

        if (_inventoryRuntimeReadyChannel != null)
            _inventoryRuntimeReadyChannel.OnEventRaised -= HandleInventoryRuntimeReady;
    }

    public void HandleInventorySlotLeftClicked(int slotIndex)
    {
        HideContextMenu();

        if (_inventoryRuntime == null)
            return;

        if (_interactionMode == PlayerInventoryInteractionMode.SellSelect)
        {
            SelectInventorySlot(slotIndex);
            return;
        }

        if (_inventoryRuntime.TryQuickEquipInventoryItem(slotIndex, out string message))
            RaiseMessage(message);
        else
            RaiseMessage(message);
    }

    public void HandleInventorySlotRightClicked(int slotIndex, Vector2 screenPosition)
    {
        if (_interactionMode == PlayerInventoryInteractionMode.SellSelect)
            return;

        if (_inventoryRuntime == null || _contextMenu == null)
            return;

        if (!TryGetInventoryStack(slotIndex, out PlayerInventoryItemStack stack))
            return;

        _contextMenu.ShowInventoryItem(
            this,
            slotIndex,
            stack.Item,
            stack.Amount,
            screenPosition);
    }

    public void HandleContextMenuEquipInventoryItem(int inventoryIndex)
    {
        if (_inventoryRuntime == null)
            return;

        if (_inventoryRuntime.TryQuickEquipInventoryItem(inventoryIndex, out string message))
            RaiseMessage(message);
        else
            RaiseMessage(message);
    }

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
            TrySpawnWorldDrop(removed);
            RaiseMessage(message);
        }
        else
        {
            RaiseMessage(message);
        }
    }

    public void HandleContextMenuSplitInventoryItem(int inventoryIndex, int amount)
    {
        HideContextMenu();

        if (_inventoryRuntime == null)
            return;

        if (_inventoryRuntime.TrySplitInventoryItem(
                inventoryIndex,
                amount,
                out _,
                out string message))
        {
            RaiseMessage(message);
        }
        else
        {
            RaiseMessage(message);
        }
    }

 

    public void HandleContextMenuUnequipEquipmentSlot(PlayerInventoryEquipmentSlotKind slotKind)
    {
        HideContextMenu();

        if (_inventoryRuntime == null)
            return;

        if (_inventoryRuntime.TryUnequipToInventorySlot(slotKind, -1, out string message))
            RaiseMessage(message);
        else
            RaiseMessage(message);
    }

    public void HandleContextMenuDiscardEquipmentSlot(PlayerInventoryEquipmentSlotKind slotKind)
    {
        HideContextMenu();

        if (_inventoryRuntime == null)
            return;

        if (_inventoryRuntime.TryRemoveEquipped(
                slotKind,
                out PlayerInventoryRemovedStack removed,
                out string message))
        {
            TrySpawnWorldDrop(removed);
            RaiseMessage(message);
        }
        else
        {
            RaiseMessage(message);
        }
    }

    public void HandleContextMenuUnequipAmmoSlot(int ammoSlotIndex)
    {
        HideContextMenu();

        if (_inventoryRuntime == null)
            return;

        if (_inventoryRuntime.TryUnequipAmmoSlotToInventorySlot(
                ammoSlotIndex,
                -1,
                out string message))
        {
            RaiseMessage(message);
        }
        else
        {
            RaiseMessage(message);
        }
    }

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
            TrySpawnWorldDrop(removed);
            RaiseMessage(message);
        }
        else
        {
            RaiseMessage(message);
        }
    }

    public void HandleContextMenuSplitAmmoSlot(int ammoSlotIndex, int amount)
    {
        HideContextMenu();
        RaiseMessage("Move ammo to inventory before splitting.");
    }

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
            _ => null
        };

        if (item == null)
            return;

        _contextMenu.ShowEquipmentSlot(this, slotKind, item, screenPosition);
    }

    public void HandleInventorySlotDroppedOnInventory(int fromIndex, int toIndex)
    {
        HideContextMenu();

        if (_inventoryRuntime == null)
            return;

        if (_inventoryRuntime.TryMoveInventoryItem(fromIndex, toIndex, out string message))
            RaiseMessage(message);
        else
            RaiseMessage(message);
    }

    public void HandleInventorySlotDroppedOnEquipment(
        int inventoryIndex,
        PlayerInventoryEquipmentSlotKind slotKind)
    {
        HideContextMenu();

        if (_inventoryRuntime == null)
            return;

        if (_inventoryRuntime.TryEquipInventoryItemToSlot(inventoryIndex, slotKind, out string message))
            RaiseMessage(message);
        else
            RaiseMessage(message);
    }

    public void HandleEquipmentSlotLeftClicked(PlayerInventoryEquipmentSlotKind slotKind)
    {
        HideContextMenu();

        if (_interactionMode != PlayerInventoryInteractionMode.SellSelect)
            return;

        SelectEquipmentSlot(slotKind);
    }

    public void HandleEquipmentSlotDroppedOnInventory(
        PlayerInventoryEquipmentSlotKind slotKind,
        int targetInventoryIndex)
    {
        HideContextMenu();

        if (_inventoryRuntime == null)
            return;

        if (_inventoryRuntime.TryUnequipToInventorySlot(slotKind, targetInventoryIndex, out string message))
            RaiseMessage(message);
        else
            RaiseMessage(message);
    }

    public void HandleInventorySlotDroppedOnAmmo(int inventoryIndex, int ammoSlotIndex)
    {
        HideContextMenu();

        if (_inventoryRuntime == null)
            return;

        if (_inventoryRuntime.TryEquipInventoryBulletToAmmoSlot(
                inventoryIndex,
                ammoSlotIndex,
                out string message))
        {
            RaiseMessage(message);
        }
        else
        {
            RaiseMessage(message);
        }
    }

    public void HandleAmmoSlotDroppedOnInventory(int ammoSlotIndex, int targetInventoryIndex)
    {
        HideContextMenu();

        if (_inventoryRuntime == null)
            return;

        if (_inventoryRuntime.TryUnequipAmmoSlotToInventorySlot(
                ammoSlotIndex,
                targetInventoryIndex,
                out string message))
        {
            RaiseMessage(message);
        }
        else
        {
            RaiseMessage(message);
        }
    }

    public void HandleAmmoSlotDroppedOnAmmo(int fromAmmoSlotIndex, int toAmmoSlotIndex)
    {
        HideContextMenu();

        if (_inventoryRuntime == null)
            return;

        if (_inventoryRuntime.TrySwapAmmoSlots(
                fromAmmoSlotIndex,
                toAmmoSlotIndex,
                out string message))
        {
            RaiseMessage(message);
        }
        else
        {
            RaiseMessage(message);
        }
    }

    public void HandleAmmoSlotLeftClicked(int ammoSlotIndex)
    {
        HideContextMenu();

        if (_interactionMode != PlayerInventoryInteractionMode.SellSelect)
            return;

        SelectAmmoSlot(ammoSlotIndex);
    }

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

        if (_weaponSlot != null)
            _weaponSlot.Bind(_inventoryRuntime.EquippedWeapon);

        RefreshAmmoEquipmentSlots();

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

    public bool TryGetSelectedInventoryStack(out int slotIndex, out PlayerInventoryItemStack stack)
    {
        slotIndex = _selectedInventoryIndex;
        stack = ResolveSelectedStack();
        return stack != null && !stack.IsEmpty;
    }

    public bool TryGetSelectedItemSelection(out PlayerInventoryItemSelection selection)
    {
        selection = _selectedItemSelection;
        return selection.HasItem;
    }

    public void ClearSelectedInventorySlot()
    {
        ClearSelectedItemSelection();
    }

    public void ClearSelectedItemSelection()
    {
        if (_selectedInventoryIndex < 0 && !_selectedItemSelection.HasItem)
            return;

        _selectedInventoryIndex = -1;
        _selectedItemSelection = PlayerInventoryItemSelection.None;
        OnInventorySelectionChanged?.Invoke(_selectedInventoryIndex, null);
        OnItemSelectionChanged?.Invoke(_selectedItemSelection);
    }

    public static string ResolveItemName(ItemSO item)
    {
        return InventoryItemDisplayUtility.ResolveItemName(item);
    }

    private void InitializeStaticSlots()
    {
        if (_contextMenu != null)
            _contextMenu.Initialize(this);

        if (_armorSlot != null)
            _armorSlot.Initialize(this);

        if (_weaponSlot != null)
            _weaponSlot.Initialize(this);

        if (_ammoSlots != null)
        {
            for (int i = 0; i < _ammoSlots.Length; i++)
            {
                if (_ammoSlots[i] != null)
                    _ammoSlots[i].Initialize(this, i);
            }
        }
    }

    private void RefreshAmmoEquipmentSlots()
    {
        if (_ammoSlots == null || _inventoryRuntime == null)
            return;

        for (int i = 0; i < _ammoSlots.Length; i++)
        {
            EquipmentInventoryAmmoSlotUI slot = _ammoSlots[i];

            if (slot == null)
                continue;

            if (_inventoryRuntime.TryGetAmmoSlotSnapshot(i, out WeaponAmmoSlotSnapshot snapshot))
                slot.Bind(snapshot);
            else
                slot.Bind(default(WeaponAmmoSlotSnapshot));
        }
    }

    private void EnsureInventorySlots()
    {
        if (_inventoryRuntime == null || _inventorySlotPrefab == null)
            return;

        if (_inventorySlotRoot == null && _inventorySlotPrefab.transform.parent != null)
            _inventorySlotRoot = _inventorySlotPrefab.transform.parent;

        if (_inventorySlotRoot == null)
            return;

        int capacity = _inventoryRuntime.Capacity;

        while (_inventorySlots.Count < capacity)
        {
            InventoryItemSlotUI slot = Instantiate(_inventorySlotPrefab, _inventorySlotRoot);
            slot.name = $"{_inventorySlotPrefab.name}_{_inventorySlots.Count:00}";
            _inventorySlots.Add(slot);
        }

        for (int i = 0; i < _inventorySlots.Count; i++)
        {
            InventoryItemSlotUI slot = _inventorySlots[i];

            if (slot == null)
                continue;

            ConfigureInventorySlot(slot, i);
            slot.gameObject.SetActive(i < capacity);
        }
    }

    private void ConfigureInventorySlots()
    {
        for (int i = 0; i < _inventorySlots.Count; i++)
        {
            InventoryItemSlotUI slot = _inventorySlots[i];

            if (slot != null)
                ConfigureInventorySlot(slot, i);
        }
    }

    private void ConfigureInventorySlot(InventoryItemSlotUI slot, int slotIndex)
    {
        if (slot == null)
            return;

        bool requireDoubleClick =
            _interactionMode != PlayerInventoryInteractionMode.SellSelect;

        slot.Initialize(
            this,
            slotIndex,
            true,
            requireDoubleClick,
            InventoryDragSourceKind.Inventory);
    }

    private void HideSlotTemplateIfNeeded()
    {
        if (!_hideInventorySlotPrefabOnAwake || _inventorySlotPrefab == null)
            return;

        if (_inventorySlotRoot == null && _inventorySlotPrefab.transform.parent != null)
            _inventorySlotRoot = _inventorySlotPrefab.transform.parent;

        if (_inventorySlotRoot != null && _inventorySlotPrefab.transform.IsChildOf(_inventorySlotRoot))
            _inventorySlotPrefab.gameObject.SetActive(false);
    }

    private void ResolveRefs()
    {
        if (_contextMenu == null)
            _contextMenu = GetComponentInChildren<InventoryItemContextMenuUI>(true);

        if (_inventorySlotRoot == null && _inventorySlotPrefab != null && _inventorySlotPrefab.transform.parent != null)
            _inventorySlotRoot = _inventorySlotPrefab.transform.parent;

        if (_dropActor == null && _inventoryRuntime != null)
            _dropActor = _inventoryRuntime.transform;
    }

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
            _ => null
        };

        if (item == null)
        {
            ClearSelectedItemSelection();
            return;
        }

        float durability = slotKind == PlayerInventoryEquipmentSlotKind.Armor
            ? _inventoryRuntime.EquippedArmorDurability
            : -1f;

        float maxDurability = slotKind == PlayerInventoryEquipmentSlotKind.Armor
            ? _inventoryRuntime.EquippedArmorMaxDurability
            : -1f;

        _selectedInventoryIndex = -1;
        _selectedItemSelection = new PlayerInventoryItemSelection(
            PlayerInventoryItemSelectionKind.Equipment,
            -1,
            slotKind,
            -1,
            item,
            1,
            durability,
            maxDurability);

        OnInventorySelectionChanged?.Invoke(_selectedInventoryIndex, null);
        OnItemSelectionChanged?.Invoke(_selectedItemSelection);
    }

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

    private static PlayerInventoryItemSelection CreateInventorySelection(
        int slotIndex,
        PlayerInventoryItemStack stack)
    {
        if (stack == null || stack.IsEmpty)
            return PlayerInventoryItemSelection.None;

        float maxDurability = stack.Item is ArmorItemSO armor
            ? armor.MaxDurability
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
            maxDurability);
    }

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
                SelectEquipmentSlot(_selectedItemSelection.equipmentSlotKind);
                return;

            case PlayerInventoryItemSelectionKind.Ammo:
                SelectAmmoSlot(_selectedItemSelection.ammoSlotIndex);
                return;
        }

        ClearSelectedItemSelection();
    }

    private void HandleInventoryRuntimeReady(PlayerInventoryRuntime runtime)
    {
        if (_inventoryRuntime != null)
            _inventoryRuntime.OnChanged -= Refresh;

        _inventoryRuntime = runtime;

        if (_inventoryRuntime != null)
            _inventoryRuntime.OnChanged += Refresh;

        Refresh();
    }

    private void ClearBoundSlots()
    {
        if (_armorSlot != null)
            _armorSlot.Bind(null);

        if (_weaponSlot != null)
            _weaponSlot.Bind(null);

        if (_ammoSlots != null)
        {
            for (int i = 0; i < _ammoSlots.Length; i++)
            {
                if (_ammoSlots[i] != null)
                    _ammoSlots[i].Bind(default(WeaponAmmoSlotSnapshot));
            }
        }

        for (int i = 0; i < _inventorySlots.Count; i++)
        {
            if (_inventorySlots[i] != null)
                _inventorySlots[i].Bind(null);
        }

        ClearSelectedInventorySlot();
    }

    private void RaiseMessage(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
            return;

        if (_systemMessageChannel != null)
            _systemMessageChannel.RaiseEvent(message);
        else
            Debug.Log($"[Inventory] {message}", this);
    }

    private void HideContextMenu()
    {
        if (_contextMenu != null)
            _contextMenu.Hide();
    }

    private bool TrySpawnWorldDrop(PlayerInventoryRemovedStack removed)
    {
        if (!removed.HasItem || removed.item.WorldItemPrefab == null)
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

        GameObject pickupObject = Instantiate(
            removed.item.WorldItemPrefab,
            position,
            Quaternion.identity);

        ItemWorldPickup pickup =
            pickupObject.GetComponent<ItemWorldPickup>() ??
            pickupObject.GetComponentInChildren<ItemWorldPickup>(true);

        if (pickup == null)
            pickup = pickupObject.AddComponent<ItemWorldPickup>();

        int amount = Mathf.Max(1, removed.amount);

        pickup.Initialize(
            removed.item,
            amount,
            amount,
            0f,
            removed.armorDurability);

        return true;
    }
}
