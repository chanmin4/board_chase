using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
public class EnemyInventoryUI : InventoryPanelUIBase,
    IInventorySlotOwner,
    IInventoryItemContextMenuOwner
{
    private const int WeaponSlotIndex = -1000;
    private const int ArmorSlotIndex = -1001;
    private const int AmmoSlotIndexBase = 0;

    [Header("Player Receiver")]
    
    [SerializeField] private PlayerInventoryRuntimeReadyEventChannelSO _playerInventoryRuntimeReadyChannel;

    [Header("Enemy")]
    [SerializeField] private TextMeshProUGUI _enemyNameText;
    [SerializeField] private string _enemyNameFormat = "{0}";

    [Header("Enemy Equipped Slots")]
    [Tooltip("Enemy currently equipped weapon slot.")]
    [SerializeField] private EquipmentInventoryWeaponSlotUI _weaponSlot;

    [Tooltip("Enemy currently equipped armor slot.")]
    [SerializeField] private EquipmentInventoryArmorSlotUI _armorSlot;

    [Tooltip("Enemy equipped ammo slots. Currently one slot is enough, but multiple slots are supported.")]
    [SerializeField] private EquipmentInventoryAmmoSlotUI[] _ammoSlots;

    [Header("Context Menu")]
    [SerializeField] private InventoryItemContextMenuUI _contextMenu;

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

    protected override void Awake()
    {
        base.Awake();
        ResolveRefs();
        InitializeSlots();
        HidePanel();
    }

    private void OnEnable()
    {
        ResolveRefs();
        InitializeSlots();

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

    private void OnDisable()
    {
        if (_openRequestChannel != null)
            _openRequestChannel.OnEventRaised -= HandleOpenRequested;

        if (_playerInventoryRuntimeReadyChannel != null)
            _playerInventoryRuntimeReadyChannel.OnEventRaised -= HandlePlayerInventoryRuntimeReady;
    }

    protected override void OnPanelHidden()
    {
        _activeInventory = null;
        Refresh();
    }

    public void HandleInventorySlotLeftClicked(int slotIndex)
    {
        switch (slotIndex)
        {
            case WeaponSlotIndex:
                TryTakeWeapon();
                break;

            case ArmorSlotIndex:
                TryTakeArmor();
                break;

            default:
                if (slotIndex >= AmmoSlotIndexBase)
                    TryTakeAmmo(slotIndex - AmmoSlotIndexBase);
                break;
        }
    }

    public void HandleInventorySlotRightClicked(int slotIndex, Vector2 screenPosition)
    {
        HandleInventorySlotLeftClicked(slotIndex);
    }

    public void HandleInventorySlotDroppedOnInventory(int fromIndex, int toIndex)
    {
    }

    public void HandleInventorySlotDroppedOnEquipment(
        int inventoryIndex,
        PlayerInventoryEquipmentSlotKind slotKind)
    {
    }

    public void HandleInventorySlotDroppedOnAmmo(int inventoryIndex, int ammoSlotIndex)
    {
    }

    public void HandleEquipmentSlotLeftClicked(PlayerInventoryEquipmentSlotKind slotKind)
    {
        switch (slotKind)
        {
            case PlayerInventoryEquipmentSlotKind.Weapon:
                TryTakeWeapon();
                break;

            case PlayerInventoryEquipmentSlotKind.Armor:
                TryTakeArmor();
                break;
        }
    }

    public void HandleEquipmentSlotDroppedOnInventory(
        PlayerInventoryEquipmentSlotKind slotKind,
        int targetInventoryIndex)
    {
    }

    public void HandleEquipmentSlotRightClicked(
        PlayerInventoryEquipmentSlotKind slotKind,
        Vector2 screenPosition)
    {
        if (_contextMenu == null || _activeInventory == null)
            return;

        ItemSO item = slotKind switch
        {
            PlayerInventoryEquipmentSlotKind.Weapon => _activeInventory.Weapon,
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

    public void HandleAmmoSlotDroppedOnInventory(int ammoSlotIndex, int targetInventoryIndex)
    {
    }

    public void HandleAmmoSlotDroppedOnAmmo(int fromAmmoSlotIndex, int toAmmoSlotIndex)
    {
    }

    public void HandleAmmoSlotLeftClicked(int ammoSlotIndex)
    {
        TryTakeAmmo(ammoSlotIndex);
    }

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

    public void HandleContextMenuEquipInventoryItem(int inventoryIndex)
    {
    }

    public void HandleContextMenuSplitInventoryItem(int inventoryIndex, int amount)
    {
    }

    public void HandleContextMenuDiscardInventoryItem(int inventoryIndex)
    {
    }

    public void HandleContextMenuUnequipEquipmentSlot(PlayerInventoryEquipmentSlotKind slotKind)
    {
        HandleEquipmentSlotLeftClicked(slotKind);
    }

    public void HandleContextMenuDiscardEquipmentSlot(PlayerInventoryEquipmentSlotKind slotKind)
    {
        if (_activeInventory == null)
            return;

        switch (slotKind)
        {
            case PlayerInventoryEquipmentSlotKind.Weapon:
                TrySpawnWorldDrop(new PlayerInventoryRemovedStack(
                    _activeInventory.Weapon,
                    1,
                    -1f));
                _activeInventory.ClearWeapon();
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

    public void HandleContextMenuUnequipAmmoSlot(int ammoSlotIndex)
    {
        TryTakeAmmo(ammoSlotIndex);
    }

    public void HandleContextMenuSplitAmmoSlot(int ammoSlotIndex, int amount)
    {
        if (_activeInventory == null)
            return;

        if (_activeInventory.TrySplitBulletStack(
                ammoSlotIndex,
                amount,
                out _,
                out string message))
        {
            RaiseMessage(message);
            Refresh();
        }
        else
        {
            RaiseMessage(message);
        }
    }

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

    public void TryTakeWeapon()
    {
        if (_activeInventory == null || _playerInventoryRuntime == null)
            return;

        WeaponSO weapon = _activeInventory.Weapon;

        if (weapon == null)
            return;

        if (!_playerInventoryRuntime.TryStoreExternalItemToInventorySlot(
                weapon,
                1,
                -1f,
                -1,
                out string message))
        {
            RaiseMessage(message);
            return;
        }

        _activeInventory.ClearWeapon();
        RaiseMessage(message);
        RefreshAfterTake();
    }

    public void TryTakeArmor()
    {
        if (_activeInventory == null || _playerInventoryRuntime == null)
            return;

        ArmorItemSO armor = _activeInventory.Armor;

        if (armor == null)
            return;

        if (!_playerInventoryRuntime.TryStoreExternalItemToInventorySlot(
                armor,
                1,
                _activeInventory.ArmorDurability,
                -1,
                out string message))
        {
            RaiseMessage(message);
            return;
        }

        _activeInventory.ClearArmor();
        RaiseMessage(message);
        RefreshAfterTake();
    }

    public void TryTakeAmmo(int index)
    {
        if (_activeInventory == null || _playerInventoryRuntime == null)
            return;

        EnemyLootBulletStack stack = ResolveAmmoStack(index);

        if (stack == null)
            return;

        if (!_playerInventoryRuntime.TryStoreExternalItemToInventorySlot(
                stack.Bullet,
                stack.Amount,
                -1f,
                -1,
                out string message))
        {
            RaiseMessage(message);
            return;
        }

        _activeInventory.ClearBulletStack(index);
        RaiseMessage(message);
        RefreshAfterTake();
    }

    public void TryTakeBullet(int index)
    {
        TryTakeAmmo(index);
    }

    public bool TryTakeSlotToPlayerInventorySlot(int enemySlotIndex, int targetInventoryIndex)
    {
        if (_activeInventory == null || _playerInventoryRuntime == null)
            return false;

        if (enemySlotIndex == WeaponSlotIndex)
        {
            WeaponSO weapon = _activeInventory.Weapon;

            if (weapon == null)
                return false;

            if (!_playerInventoryRuntime.TryStoreExternalItemToInventorySlot(
                    weapon,
                    1,
                    -1f,
                    targetInventoryIndex,
                    out string message))
            {
                RaiseMessage(message);
                return false;
            }

            _activeInventory.ClearWeapon();
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

            if (!_playerInventoryRuntime.TryStoreExternalItemToInventorySlot(
                    armor,
                    1,
                    durability,
                    targetInventoryIndex,
                    out string message))
            {
                RaiseMessage(message);
                return false;
            }

            _activeInventory.ClearArmor();
            RaiseMessage(message);
            RefreshAfterTake();
            return true;
        }

        int ammoIndex = enemySlotIndex - AmmoSlotIndexBase;
        EnemyLootBulletStack stack = ResolveAmmoStack(ammoIndex);

        if (stack == null)
            return false;

        if (!_playerInventoryRuntime.TryStoreExternalItemToInventorySlot(
                stack.Bullet,
                stack.Amount,
                -1f,
                targetInventoryIndex,
                out string ammoMessage))
        {
            RaiseMessage(ammoMessage);
            return false;
        }

        _activeInventory.ClearBulletStack(ammoIndex);
        RaiseMessage(ammoMessage);
        RefreshAfterTake();
        return true;
    }

    public bool TryTakeSlotToPlayerEquipmentSlot(
        int enemySlotIndex,
        PlayerInventoryEquipmentSlotKind targetSlotKind)
    {
        if (_activeInventory == null || _playerInventoryRuntime == null)
            return false;

        ItemSO item = null;
        float armorDurability = -1f;

        if (enemySlotIndex == WeaponSlotIndex)
        {
            item = _activeInventory.Weapon;
        }
        else if (enemySlotIndex == ArmorSlotIndex)
        {
            item = _activeInventory.Armor;
            armorDurability = _activeInventory.ArmorDurability;
        }

        if (item == null)
            return false;

        if (!_playerInventoryRuntime.TryEquipExternalItemToSlot(
                item,
                1,
                armorDurability,
                targetSlotKind,
                out string message))
        {
            RaiseMessage(message);
            return false;
        }

        if (enemySlotIndex == WeaponSlotIndex)
            _activeInventory.ClearWeapon();
        else if (enemySlotIndex == ArmorSlotIndex)
            _activeInventory.ClearArmor();

        RaiseMessage(message);
        RefreshAfterTake();
        return true;
    }

    public bool TryTakeAmmoToPlayerAmmoSlot(int enemySlotIndex, int targetAmmoSlotIndex)
    {
        if (_activeInventory == null || _playerInventoryRuntime == null)
            return false;

        int ammoIndex = enemySlotIndex - AmmoSlotIndexBase;
        EnemyLootBulletStack stack = ResolveAmmoStack(ammoIndex);

        if (stack == null)
            return false;

        if (!_playerInventoryRuntime.TryEquipExternalBulletToAmmoSlot(
                stack.Bullet,
                stack.Amount,
                targetAmmoSlotIndex,
                out string message))
        {
            RaiseMessage(message);
            return false;
        }

        _activeInventory.ClearBulletStack(ammoIndex);
        RaiseMessage(message);
        RefreshAfterTake();
        return true;
    }

    public void ClosePanel()
    {
        _overlayRequestChannel?.Close(UIOverlayId.PlayerPanelHub);
    }

    private void HandleOpenRequested(EnemyLootOpenRequest request)
    {
        _activeInventory = request.lootInventory;
        _dropActor = request.actor != null ? request.actor.transform : _dropActor;
        ShowPanel();
        Refresh();
    }

    private void HandlePlayerInventoryRuntimeReady(PlayerInventoryRuntime runtime)
    {
        _playerInventoryRuntime = runtime;
    }

    private void RefreshAfterTake()
    {
        Refresh();

        if (_activeInventory != null && !_activeInventory.HasLoot)
            ClosePanel();
    }

    private void Refresh()
    {
        if (_activeInventory != null)
            _activeInventory.SyncArmorDurabilityFromEquipment();

        RefreshHeader();
        RefreshEquippedSlots();
        RefreshAmmoSlots();
    }

    private void RefreshHeader()
    {
        if (_enemyNameText == null)
            return;

        string enemyName = _activeInventory != null
            ? _activeInventory.name
            : string.Empty;

        _enemyNameText.text = string.IsNullOrWhiteSpace(enemyName)
            ? string.Empty
            : string.Format(_enemyNameFormat, enemyName);
    }

    private void RefreshEquippedSlots()
    {
        if (_weaponSlot != null)
        {
            WeaponSO weapon = _activeInventory != null ? _activeInventory.Weapon : null;
            _weaponSlot.Bind(weapon);
        }

        if (_armorSlot != null)
        {
            ArmorItemSO armor = _activeInventory != null ? _activeInventory.Armor : null;
            float durability = _activeInventory != null ? _activeInventory.ArmorDurability : -1f;
            float maxDurability = armor != null ? armor.MaxDurability : -1f;
            _armorSlot.Bind(armor, durability, maxDurability);
        }
    }

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

    private EnemyLootBulletStack ResolveAmmoStack(int index)
    {
        if (_activeInventory == null || _activeInventory.BulletStacks == null)
            return null;

        if (index < 0 || index >= _activeInventory.BulletStacks.Count)
            return null;

        EnemyLootBulletStack stack = _activeInventory.BulletStacks[index];
        return stack != null && stack.IsAvailable ? stack : null;
    }

    private void InitializeSlots()
    {
        if (_contextMenu != null)
            _contextMenu.Initialize(this);

        if (_weaponSlot != null)
        {
            _weaponSlot.Initialize(
                this,
                InventoryDragSourceKind.EnemyInventory,
                WeaponSlotIndex);
        }

        if (_armorSlot != null)
        {
            _armorSlot.Initialize(
                this,
                InventoryDragSourceKind.EnemyInventory,
                ArmorSlotIndex);
        }

        if (_ammoSlots == null)
            return;

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
    }

    private void ResolveRefs()
    {
        if (_playerInventoryRuntime == null)
            _playerInventoryRuntime = FindAnyObjectByType<PlayerInventoryRuntime>();

        if (_contextMenu == null)
            _contextMenu = GetComponentInChildren<InventoryItemContextMenuUI>(true);
        if (_dropActor == null && _playerInventoryRuntime != null)
            _dropActor = _playerInventoryRuntime.transform;
    }

    private void RaiseMessage(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
            return;

        if (_systemMessageChannel != null)
            _systemMessageChannel.RaiseEvent(message);
        else
            Debug.Log($"[EnemyInventory] {message}", this);
    }

    private bool TrySpawnWorldDrop(PlayerInventoryRemovedStack removed)
    {
        if (!removed.HasItem || removed.item.WorldItemPrefab == null)
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
