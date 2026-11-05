/*
 * Role hierarchy: Gameplay > World > Sectors > Treasure
 * Primary responsibility: Coordinates the InheritanceWarehouseRuntime subsystem lifecycle and collaborating objects.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using System.Collections.Generic;
using System.Text;
using UnityEngine;

[DisallowMultipleComponent]
public class InheritanceWarehouseRuntime : EnemyLootInventoryRuntime
{
    [Header("Inheritance Warehouse")]
    [Tooltip("Persistent save asset. Warehouse contents are stored here instead of the current run inventory.")]
    [SerializeField] private SaveSystem _saveSystem;

    [Tooltip("Optional explicit item catalog used to resolve saved ItemSO GUIDs. Missing GUIDs are reported instead of being replaced.")]
    [SerializeField] private ItemSO[] _itemCatalog;

    [SerializeField, Min(1)] private int _warehouseCapacity = 24;
    [SerializeField] private string _displayName = "Inheritance Warehouse";

    private bool _isConfigured;
    private bool _isLoading;
    private string _lastPersistedFingerprint = string.Empty;

    public override string LootDisplayName => _displayName;
    public override bool ShowsEquipment => false;
    public override bool CanInteract => true;

    public bool CanReceiveQuestItems(QuestRuntimeState state,out string message)
    {
        message="창고가 준비되지 않았습니다.";
        if(!_isConfigured||_saveSystem==null||state?.Definition==null)return false;
        var items=new ItemSO[InventoryCapacity];var amounts=new int[InventoryCapacity];
        for(int i=0;i<items.Length&&i<ItemStacks.Count;i++)
            if(ItemStacks[i]?.IsEmpty==false){items[i]=ItemStacks[i].Item;amounts[i]=ItemStacks[i].Amount;}
        var rewards=state.Definition.Rewards;
        for(int r=0;r<rewards.Length;r++)
        {
            var reward=rewards[r];
            if(state.IsRewardGranted(r)||reward?.RewardType!=QuestRewardType.Item||
                (_saveSystem.Profile.questWarehouseItemReceipts??=new()).Contains(state.Definition.QuestId+":"+r))continue;
            var item=reward.Item;if(item==null){message="물품 보상 원본이 없습니다.";return false;}
            if(!PassiveInstanceRuntime.CanCreate(item,out message))return false;
            int remaining=reward.ItemAmount;
            for(int i=0;i<items.Length&&remaining>0;i++)
            {
                if(items[i]!=item||item.MaxStack<=1)continue;
                int add=Mathf.Min(remaining,Mathf.Max(0,item.MaxStack-amounts[i]));amounts[i]+=add;remaining-=add;
            }
            for(int i=0;i<items.Length&&remaining>0;i++)
            {
                if(items[i]!=null)continue;
                int add=Mathf.Min(remaining,item.MaxStack);items[i]=item;amounts[i]=add;remaining-=add;
            }
            if(remaining>0){message="창고 공간이 부족합니다. 모든 물품 보상을 받을 공간을 확보한 뒤 완료하세요.";return false;}
        }
        message="창고 보상 공간 확인 완료.";return true;
    }
    public bool TryReceiveQuestItem(ItemSO item,int amount,string receipt,out string message)
    {
        message="창고가 준비되지 않았습니다.";
        if(!_isConfigured||_saveSystem==null||item==null)return false;
        var receipts=_saveSystem.Profile.questWarehouseItemReceipts??=new();
        if(receipts.Contains(receipt)){message="이미 지급된 창고 보상입니다.";return true;}
        if(!PassiveInstanceRuntime.CanCreate(item,out message))return false;
        if(PassiveInstanceRuntime.NeedsRoll(item))
        {
            var empty=new List<int>();
            for(int i=0;i<InventoryCapacity;i++)if(i>=ItemStacks.Count||ItemStacks[i]?.IsEmpty!=false)empty.Add(i);
            int count=Mathf.Max(1,amount);
            if(empty.Count<count){message="창고 공간이 부족합니다.";return false;}
            var prepared=new List<ItemSO>();
            for(int i=0;i<count;i++)
            {
                var instance=PassiveInstanceRuntime.Create(item);
                if(instance==null){message="보상 패시브 생성 실패. 창고는 변경하지 않았습니다.";return false;}
                prepared.Add(instance);
            }
            for(int i=0;i<count;i++)SetItemStack(empty[i],prepared[i],1);
        }
        else if(!TryAddItemStack(item,amount,out message))return false;
        receipts.Add(receipt);
        // Capture only. QuestRuntime persists the grant marker and warehouse in the same profile write.
        PersistContents(BuildFingerprint(),false);
        return true;
    }

    // Purpose: Caches the component references required before runtime initialization.
    protected override void Awake()
    {
        base.Awake();
        ConfigureInventoryCapacity(_warehouseCapacity);
    }

    // Purpose: Processes this component's per-frame runtime work.
    private void Update()
    {
        if (!_isConfigured || _isLoading)
            return;

        string fingerprint = BuildFingerprint();

        if (fingerprint == _lastPersistedFingerprint)
            return;

        PersistContents(fingerprint);
    }

    // Purpose: Releases active subscriptions and transient state while disabled.
    private void OnDisable()
    {
        if (_isConfigured && !_isLoading)
            PersistContents(BuildFingerprint());
    }

    // Purpose: Configures this component''s persistence.
    public void ConfigurePersistence(
        SaveSystem saveSystem,
        ItemSO[] itemCatalog,
        int capacity,
        EnemyLootOpenRequestEventChannelSO openRequestChannel,
        UIOverlayRequestEventChannelSO overlayRequestChannel)
    {
        _saveSystem = saveSystem;
        _itemCatalog = itemCatalog;
        _warehouseCapacity = Mathf.Max(1, capacity);

        if (_saveSystem != null)
        {
            if (_saveSystem.saveData == null)
                _saveSystem.saveData = new Save();

            _saveSystem.saveData.EnsureRuntimeDefaults();
            _warehouseCapacity = Mathf.Max(
                _warehouseCapacity,
                _saveSystem.saveData.InheritanceWarehouse.capacity);
        }

        ConfigureInteractionChannels(openRequestChannel, overlayRequestChannel);
        ConfigureInventoryCapacity(_warehouseCapacity);

        _isConfigured = true;
        LoadContents();
    }

    /// <summary>
    /// Applies an already-persisted quest capacity reward to the live Home warehouse.
    /// This only expands capacity; it never truncates stored items.
    /// </summary>
    public bool TryApplyPersistentCapacity(int requestedCapacity)
    {
        requestedCapacity = Mathf.Max(1, requestedCapacity);
        if (requestedCapacity <= InventoryCapacity)
            return true;

        _warehouseCapacity = requestedCapacity;
        ConfigureInventoryCapacity(_warehouseCapacity);

        if (_saveSystem != null)
        {
            if (_saveSystem.saveData == null)
                _saveSystem.saveData = new Save();

            _saveSystem.saveData.InheritanceWarehouse.capacity = _warehouseCapacity;
            _saveSystem.saveData.InheritanceWarehouse.EnsureDefaults();
        }

        return true;
    }

#if UNITY_EDITOR
    public IReadOnlyList<PlayerInventoryItemStack> DebugContents => ItemStacks;
    public int DebugCapacity => InventoryCapacity;
    public bool DebugPersistenceReady =>
        _isConfigured &&
        _saveSystem != null &&
        _saveSystem.saveData != null;

    public string DebugPersistenceStatus
    {
        get
        {
            if (!_isConfigured)
                return "Warehouse persistence has not been configured.";

            if (_saveSystem == null)
                return "SaveSystem is missing.";

            if (_saveSystem.saveData == null)
                return "Save data is missing.";

            int usedSlots = 0;
            int totalItems = 0;
            IReadOnlyList<PlayerInventoryItemStack> stacks = ItemStacks;

            for (int i = 0; i < stacks.Count; i++)
            {
                PlayerInventoryItemStack stack = stacks[i];

                if (stack == null || stack.IsEmpty)
                    continue;

                usedSlots++;
                totalItems += stack.Amount;
            }

            return
                $"Ready. slots={usedSlots}/{InventoryCapacity}, totalItems={totalItems}";
        }
    }

    // Purpose: Performs the debug try add item workflow for this component.
    public bool DebugTryAddItem(
        ItemSO item,
        int amount,
        out string message)
    {
        float armorDurability = item is ArmorItemSO armor
            ? armor.MaxDurability
            : -1f;
        float weaponMaxDurability = item is RangedWeaponSO rangedWeapon
            ? rangedWeapon.MaxDurability
            : -1f;

        return DebugTryAddItem(
            item,
            amount,
            armorDurability,
            weaponMaxDurability,
            weaponMaxDurability,
            out message);
    }

    // Purpose: Performs the debug try add item workflow for this component.
    public bool DebugTryAddItem(
        ItemSO item,
        int amount,
        float armorDurability,
        float weaponCurrentDurability,
        float weaponMaxDurability,
        out string message)
    {
        if (!TryValidateDebugPersistence(out message))
            return false;

        if (item == null)
        {
            message = "Select an ItemSO to add.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(item.Guid))
        {
            message =
                $"Item cannot be inherited because it has no serialized GUID. item={item.name}";
            return false;
        }

        if (!IsDebugCatalogItem(item))
        {
            message =
                $"Item is not in this warehouse's Item Catalog and may not survive a restart. " +
                $"Add it to the warehouse catalog first. item={item.name}";
            return false;
        }

        if (amount <= 0)
        {
            message = "Amount must be greater than zero.";
            return false;
        }

        int maxStack = item.MaxStack;
        int ownedAmount = 0;
        int availableAmount = 0;
        int capacity = InventoryCapacity;
        IReadOnlyList<PlayerInventoryItemStack> stacks = ItemStacks;

        for (int i = 0; i < capacity; i++)
        {
            PlayerInventoryItemStack stack =
                i < stacks.Count ? stacks[i] : null;

            if (stack == null || stack.IsEmpty)
            {
                availableAmount += maxStack;
                continue;
            }

            if (!IsSameDebugItem(stack.Item, item))
                continue;

            ownedAmount += stack.Amount;
            availableAmount += Mathf.Max(0, maxStack - stack.Amount);
        }

        int maxOwnedAmount = item.MaxOwnedAmount;

        if (maxOwnedAmount > 0 && ownedAmount + amount > maxOwnedAmount)
        {
            message =
                $"Ownership cap exceeded. item={item.name}, " +
                $"owned={ownedAmount}, requested={amount}, cap={maxOwnedAmount}";
            return false;
        }

        if (availableAmount < amount)
        {
            message =
                $"Warehouse has insufficient stack space. item={item.name}, " +
                $"requested={amount}, available={availableAmount}";
            return false;
        }

        int remaining = amount;

        // Preserve the durability metadata already attached to existing stacks.
        for (int i = 0; i < capacity && remaining > 0; i++)
        {
            PlayerInventoryItemStack stack =
                i < stacks.Count ? stacks[i] : null;

            if (stack == null ||
                stack.IsEmpty ||
                !IsSameDebugItem(stack.Item, item))
            {
                continue;
            }

            int added = Mathf.Min(
                remaining,
                Mathf.Max(0, maxStack - stack.Amount));

            if (added <= 0)
                continue;

            SetItemStack(
                i,
                item,
                stack.Amount + added,
                stack.ArmorDurability,
                stack.WeaponCurrentDurability,
                stack.WeaponMaxDurability,
                stack.GasMaskFilterDurability,
                stack.ArmorMaxDurability,
                stack.GasMaskMaxFilterDurability);
            remaining -= added;
        }

        for (int i = 0; i < capacity && remaining > 0; i++)
        {
            PlayerInventoryItemStack stack =
                i < stacks.Count ? stacks[i] : null;

            if (stack != null && !stack.IsEmpty)
                continue;

            int added = Mathf.Min(remaining, maxStack);
            SetItemStack(
                i,
                item,
                added,
                armorDurability,
                weaponCurrentDurability,
                weaponMaxDurability);
            remaining -= added;
        }

        DebugPersistImmediately();
        message =
            $"Added item to inheritance warehouse. item={item.name}, amount={amount}";
        return true;
    }

    // Purpose: Performs the debug clear contents workflow for this component.
    public bool DebugClearContents(out string message)
    {
        if (!TryValidateDebugPersistence(out message))
            return false;

        int removedStacks = 0;
        int removedItems = 0;
        IReadOnlyList<PlayerInventoryItemStack> stacks = ItemStacks;

        for (int i = 0; i < stacks.Count; i++)
        {
            PlayerInventoryItemStack stack = stacks[i];

            if (stack == null || stack.IsEmpty)
                continue;

            removedStacks++;
            removedItems += stack.Amount;
        }

        ClearAllLoot();
        DebugPersistImmediately();
        message =
            $"Cleared inheritance warehouse. stacks={removedStacks}, items={removedItems}";
        return true;
    }

    // Purpose: Performs the debug reload from save workflow for this component.
    public bool DebugReloadFromSave(out string message)
    {
        if (!TryValidateDebugPersistence(out message))
            return false;

        LoadContents();
        message = $"Reloaded inheritance warehouse. {DebugPersistenceStatus}";
        return true;
    }

    // Purpose: Attempts to validate debug persistence and reports whether the request succeeded.
    private bool TryValidateDebugPersistence(out string message)
    {
        if (DebugPersistenceReady)
        {
            message = string.Empty;
            return true;
        }

        message = DebugPersistenceStatus;
        return false;
    }

    // Purpose: Performs the debug persist immediately workflow for this component.
    private void DebugPersistImmediately()
    {
        // Force an explicit Debug Monitor mutation to reach disk even if an
        // external edit left the runtime fingerprint looking unchanged.
        _lastPersistedFingerprint = null;
        PersistContents(BuildFingerprint());
    }

    // Purpose: Checks whether same debug item is currently true.
    private static bool IsSameDebugItem(ItemSO left, ItemSO right)
    {
        if (left == right)
            return true;

        return left != null &&
               right != null &&
               !string.IsNullOrWhiteSpace(left.Guid) &&
               left.Guid == right.Guid;
    }

    // Purpose: Checks whether debug catalog item is currently true.
    private bool IsDebugCatalogItem(ItemSO item)
    {
        if (_itemCatalog == null || _itemCatalog.Length == 0)
            return false;

        for (int i = 0; i < _itemCatalog.Length; i++)
        {
            if (IsSameDebugItem(_itemCatalog[i], item))
                return true;
        }

        return false;
    }
#endif

    // Purpose: Attempts to take item stack and reports whether the request succeeded.
    public override bool TryTakeItemStack(
        int index,
        PlayerInventoryRuntime inventory,
        out string message)
    {
        bool taken = base.TryTakeItemStack(index, inventory, out message);

        if (taken)
            PersistContents(BuildFingerprint());

        return taken;
    }

    // Purpose: Loads contents into the active runtime state.
    private void LoadContents()
    {
        _isLoading = true;

        try
        {
            ClearAllLoot();

            if (_saveSystem == null)
            {
                Debug.LogError(
                    "[InheritanceWarehouse] SaveSystem is missing. Persistent warehouse cannot be loaded.",
                    this);
                _lastPersistedFingerprint = BuildFingerprint();
                return;
            }

            Save save = _saveSystem.saveData;

            if (save == null)
            {
                save = new Save();
                _saveSystem.saveData = save;
            }

            InheritanceWarehouseSaveData warehouse = save.InheritanceWarehouse;
            warehouse.capacity = _warehouseCapacity;
            warehouse.EnsureDefaults();

            Dictionary<string, ItemSO> itemLookup = BuildItemLookup();

            for (int i = 0; i < warehouse.items.Count; i++)
            {
                InheritanceWarehouseItemSaveData entry = warehouse.items[i];

                if (entry == null ||
                    entry.slotIndex < 0 ||
                    entry.slotIndex >= InventoryCapacity)
                {
                    continue;
                }

                if (!(itemLookup.TryGetValue(entry.itemGuid, out ItemSO item) ||
                      PassiveInstanceRuntime.TryResolve(entry.itemGuid, Resources.Load<RuntimeContentCatalogSO>("RuntimeContentCatalog"), out item)) ||
                    item == null)
                {
                    Debug.LogError(
                        $"[InheritanceWarehouse] Saved item GUID cannot be resolved. guid={entry.itemGuid}, slot={entry.slotIndex}. Add the item to the warehouse item catalog.",
                        this);
                    continue;
                }

                SetItemStack(
                    entry.slotIndex,
                    item,
                    entry.amount,
                    entry.armorDurability,
                    entry.weaponCurrentDurability,
                    entry.weaponMaxDurability,
                    entry.gasMaskFilterDurability,
                    entry.armorMaxDurability,
                    entry.gasMaskMaxFilterDurability);
            }

            _lastPersistedFingerprint = BuildFingerprint();
        }
        finally
        {
            _isLoading = false;
        }
    }

    // Purpose: Builds the item lookup from the current authored data.
    private Dictionary<string, ItemSO> BuildItemLookup()
    {
        Dictionary<string, ItemSO> lookup = new Dictionary<string, ItemSO>();

        AddItemsToLookup(_itemCatalog, lookup);
        AddItemsToLookup(Resources.FindObjectsOfTypeAll<ItemSO>(), lookup);

        return lookup;
    }

    // Purpose: Adds the requested items to lookup to this component.
    private static void AddItemsToLookup(
        ItemSO[] items,
        Dictionary<string, ItemSO> lookup)
    {
        if (items == null)
            return;

        for (int i = 0; i < items.Length; i++)
        {
            ItemSO item = items[i];

            if (item == null || string.IsNullOrWhiteSpace(item.Guid))
                continue;

            if (!lookup.ContainsKey(item.Guid))
                lookup.Add(item.Guid, item);
        }
    }

    // Purpose: Performs the persist contents workflow for this component.
    private void PersistContents(string fingerprint,bool writeToDisk=true)
    {
        if (_saveSystem == null ||
            _saveSystem.saveData == null ||
            fingerprint == _lastPersistedFingerprint)
        {
            return;
        }

        InheritanceWarehouseSaveData warehouse =
            _saveSystem.saveData.InheritanceWarehouse;

        warehouse.capacity = _warehouseCapacity;
        warehouse.items.Clear();

        IReadOnlyList<PlayerInventoryItemStack> stacks = ItemStacks;

        for (int i = 0; i < stacks.Count; i++)
        {
            PlayerInventoryItemStack stack = stacks[i];

            if (stack == null || stack.IsEmpty || stack.Item == null)
                continue;

            string guid = stack.Item.Guid;

            if (string.IsNullOrWhiteSpace(guid))
            {
                Debug.LogError(
                    $"[InheritanceWarehouse] Item has no serialized GUID and cannot be inherited. item={stack.Item.name}, slot={i}",
                    stack.Item);
                continue;
            }

            warehouse.items.Add(
                new InheritanceWarehouseItemSaveData
                {
                    slotIndex = i,
                    itemGuid = guid,
                    amount = stack.Amount,
                    armorDurability = stack.ArmorDurability,
                    armorMaxDurability = stack.ArmorMaxDurability,
                    weaponCurrentDurability = stack.WeaponCurrentDurability,
                    weaponMaxDurability = stack.WeaponMaxDurability,
                    gasMaskFilterDurability = stack.GasMaskFilterDurability,
                    gasMaskMaxFilterDurability = stack.GasMaskMaxFilterDurability
                });
        }

        warehouse.EnsureDefaults();
        if(writeToDisk)_saveSystem.SaveDataToDisk();
        _lastPersistedFingerprint = fingerprint;
    }

    // Purpose: Builds the fingerprint from the current authored data.
    private string BuildFingerprint()
    {
        StringBuilder builder = new StringBuilder(InventoryCapacity * 32);
        IReadOnlyList<PlayerInventoryItemStack> stacks = ItemStacks;

        for (int i = 0; i < stacks.Count; i++)
        {
            PlayerInventoryItemStack stack = stacks[i];

            if (stack == null || stack.IsEmpty || stack.Item == null)
                continue;

            builder.Append(i)
                .Append(':')
                .Append(stack.Item.Guid)
                .Append(':')
                .Append(stack.Amount)
                .Append(':')
                .Append(stack.ArmorDurability)
                .Append(':')
                .Append(stack.ArmorMaxDurability)
                .Append(':')
                .Append(stack.WeaponCurrentDurability)
                .Append(':')
                .Append(stack.WeaponMaxDurability)
                .Append(':')
                .Append(stack.GasMaskFilterDurability)
                .Append(':')
                .Append(stack.GasMaskMaxFilterDurability)
                .Append('|');
        }

        return builder.ToString();
    }
}
