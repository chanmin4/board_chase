/*
 * Role hierarchy: Gameplay > Entities > Shooter > Enemy > Common
 * Primary responsibility: Handles EnemyLootBulletStack combat resolution, damage, projectile, or status changes.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class EnemyLootBulletStack
{
    [SerializeField] private BulletSO _bullet;
    [SerializeField, Min(0)] private int _amount;

    public BulletSO Bullet => _bullet;
    public int Amount => Mathf.Max(0, _amount);
    public bool IsAvailable => _bullet != null && Amount > 0;

    // Purpose: Updates the set state.
    public void Set(BulletSO bullet, int amount)
    {
        _bullet = bullet;
        _amount = Mathf.Max(0, amount);
    }

    // Purpose: Clears the current clear state.
    public void Clear()
    {
        _bullet = null;
        _amount = 0;
    }
}

[Serializable]
public sealed class EnemyLootWeaponStack
{
    [SerializeField] private WeaponSO _weapon;
    [SerializeField, Min(0)] private int _amount;
    [SerializeField] private float _currentDurability = -1f;
    [SerializeField] private float _maxDurability = -1f;

    public WeaponSO Weapon => _weapon;
    public int Amount => Mathf.Max(0, _amount);
    public float CurrentDurability => _currentDurability;
    public float MaxDurability => _maxDurability;
    public bool HasDurability =>
        _weapon is RangedWeaponSO && _currentDurability >= 0f && _maxDurability > 0f;
    public bool IsAvailable => _weapon != null && Amount > 0;

    // Purpose: Updates the set state.
    public void Set(WeaponSO weapon, int amount)
    {
        Set(weapon, amount, -1f, -1f);
    }

    // Purpose: Updates the set state.
    public void Set(
        WeaponSO weapon,
        int amount,
        float currentDurability,
        float maxDurability)
    {
        _weapon = weapon;
        _amount = weapon != null ? Mathf.Max(1, amount) : 0;

        if (weapon is RangedWeaponSO rangedWeapon)
        {
            _maxDurability = maxDurability > 0f ? maxDurability : rangedWeapon.MaxDurability;
            _currentDurability = Mathf.Clamp(
                currentDurability >= 0f ? currentDurability : _maxDurability,
                0f,
                _maxDurability);
        }
        else
        {
            _currentDurability = -1f;
            _maxDurability = -1f;
        }
    }

    // Purpose: Clears the current clear state.
    public void Clear()
    {
        _weapon = null;
        _amount = 0;
        _currentDurability = -1f;
        _maxDurability = -1f;
    }
}

[DisallowMultipleComponent]
public class EnemyLootInventoryRuntime : InventoryRuntimeBase
{
    private const int WeaponSlotCount = 2;

    [Header("Interaction")]
    [SerializeField] private bool _canLootOnlyWhenDead = true;
    [SerializeField, Min(0f)] private float _interactSeconds = 1.5f;
    [SerializeField] private bool _suppressRandomDropReward = true;

    [Tooltip("Logs why corpse loot interaction is accepted or rejected, including loot contents, Damageable state, and event-channel references.")]
    [SerializeField] private bool _debugInteractionLogs;
    [Tooltip("Logs each item-stack double-click transfer to the player inventory. Turn off after diagnosing loot stacking.")]
    [SerializeField] private bool _debugItemTransferLogs = true;

    [Header("Events")]
    [SerializeField] private EnemyLootOpenRequestEventChannelSO _openRequestChannel;
    [SerializeField] private UIOverlayRequestEventChannelSO _overlayRequestChannel;

    [Header("Refs")]
    [SerializeField] private Damageable _damageable;
    [SerializeField] private EntityEquipmentRuntime _equipmentRuntime;
    [SerializeField] private ShooterStatsRuntime _statsRuntime;

    [Header("Fallback")]
    [Tooltip("Used only when ShooterStatsRuntime or shooter stat config is not ready yet.")]
    [SerializeField, Min(0)] private int _fallbackInventoryCapacity = 8;

    [Header("Loot Runtime")]
    [Tooltip("Enemy weapon equipment slots. Slot 0 is primary and slot 1 is secondary/throwable.")]
    [SerializeField][ReadOnly] private EnemyLootWeaponStack[] _weaponSlots =
    {
        new(),
        new()
    };
    [SerializeField][ReadOnly] private ArmorItemSO _armor;
    [SerializeField, Min(0f)][ReadOnly] private float _armorDurability;
    [SerializeField][ReadOnly] private EnemyLootBulletStack[] _bulletStacks =
    {
        new(),
        new(),
        new()
    };
    [SerializeField][ReadOnly] private List<PlayerInventoryItemStack> _items = new();

    private bool _openInProgress;
    private int? _configuredInventoryCapacity;
    private bool _corpseArmorRolled;
    private float _corpseArmorCap = float.PositiveInfinity;

    public void PrepareCorpseArmorDrop()
    {
        if (_corpseArmorRolled) return;
        _corpseArmorRolled = true;
        ResolveRefs();
        var config = _statsRuntime != null ? _statsRuntime.StatConfig as EnemyShooterConfigSO : null;
        if (config == null || !config.LimitCorpseArmorDurability) return;
        Vector2 range = config.ArmorDropDurabilityRange;
        float min = Mathf.Max(0f, Mathf.Min(range.x, range.y));
        float max = Mathf.Max(min, Mathf.Max(range.x, range.y));
        _corpseArmorCap = UnityEngine.Random.Range(min, max);
        SyncArmorDurabilityFromEquipment();
        _armorDurability = Mathf.Min(_armorDurability, _corpseArmorCap);
    }
    private float _interactionStartedTime;
    private float _interactionDuration;

    public virtual string LootDisplayName => name;
    public virtual bool ShowsEquipment => true;
    public IReadOnlyList<EnemyLootWeaponStack> WeaponSlots => _weaponSlots;
    public ArmorItemSO Armor => _armor;
    public float ArmorDurability => ResolveArmorDurability();
    public IReadOnlyList<EnemyLootBulletStack> BulletStacks => _bulletStacks;
    public IReadOnlyList<PlayerInventoryItemStack> ItemStacks => _items;
    public int PocketCapacity => _statsRuntime?.InventorySettings?.PocketCapacity ?? 0;
    public int BaseInventoryCapacity => _statsRuntime?.InventorySettings?.BaseInventoryCapacity ?? Mathf.Max(0, _fallbackInventoryCapacity);
    // Same slot policy as the player. A future enemy bag equipment owner can override this hook;
    // it must also own transfer/persistence before it enables equipped bags on corpses.
    protected virtual BackpackItemSO CapacityBackpack => null;
    public float BackpackMoveSpeedMultiplier => CapacityBackpack != null ? CapacityBackpack.MoveSpeedMultiplier : 1f;
    public int InventoryCapacity => _configuredInventoryCapacity ??
        ((_statsRuntime?.InventorySettings?.ResolveCapacity(CapacityBackpack) ?? Mathf.Max(0, _fallbackInventoryCapacity))+
        Mathf.FloorToInt(_statsRuntime?.AdditionalStat(PlayerStatId.InventorySlots)??0));
    public bool HasOpenRequestChannel => _openRequestChannel != null;
    public bool HasOverlayRequestChannel => _overlayRequestChannel != null;
    public float InteractSeconds => Mathf.Max(0f, _interactSeconds);
    public bool SuppressRandomDropReward => _suppressRandomDropReward;
    public bool HasLoot => HasWeaponLoot() || _armor != null || HasBulletLoot() || HasItemLoot();

    /// <summary>
    /// New authored loot sources suppress the legacy DropReward state action for
    /// that spawned enemy only, preventing a duplicate legacy floor drop.
    /// </summary>
    public void SetSuppressRandomDropReward(bool suppress)
    {
        _suppressRandomDropReward = suppress;
    }

    // Corpses remain inspectable when empty. Consumable loot containers override this rule.
    public virtual bool CanInteract =>
        !_canLootOnlyWhenDead ||
        _damageable == null ||
        _damageable.IsDead;
    public bool IsInteractionCasting => _openInProgress;
    public float InteractionCastProgress01
    {
        get
        {
            if (!_openInProgress || _interactionDuration <= 0f)
                return 0f;

            return Mathf.Clamp01((Time.time - _interactionStartedTime) / _interactionDuration);
        }
    }

    // Purpose: Initializes Inspector defaults for this component.
    protected virtual void Reset()
    {
        ResolveRefs();
    }

    // Purpose: Caches the component references required before runtime initialization.
    protected virtual void Awake()
    {
        ResolveRefs();
        NormalizeWeaponSlots();
        NormalizeBulletStacks();
        NormalizeItemStacks();

        if (_debugInteractionLogs)
            Debug.Log($"[EnemyLootInventoryRuntime] Awake. {BuildInteractionDebugInfo()}", this);
    }

    // Purpose: Initializes initialize for use by this component.
    public void Initialize(EnemyShooterLoadout loadout)
    {
        ResolveRefs();
        NormalizeWeaponSlots();
        NormalizeBulletStacks();
        NormalizeItemStacks();

        for (int i = 0; i < _weaponSlots.Length; i++)
            _weaponSlots[i].Clear();

        if (loadout.WeaponStacks != null)
        {
            for (int i = 0; i < loadout.WeaponStacks.Length; i++)
            {
                ShooterLoadoutWeaponStack stack = loadout.WeaponStacks[i];

                if (!stack.HasWeapon || stack.weapon is MeleeWeaponSO || stack.slotIndex >= _weaponSlots.Length)
                    continue;

                _weaponSlots[stack.slotIndex].Set(stack.weapon, stack.amount);
            }
        }

        _armor = loadout.Armor;
        _armorDurability = _armor != null ? _armor.MaxDurability : 0f;

        for (int i = 0; i < _bulletStacks.Length; i++)
            _bulletStacks[i].Clear();

        for (int i = 0; i < _items.Count; i++)
            _items[i].Clear();

        if (loadout.AmmoStacks != null)
        {
            for (int i = 0; i < loadout.AmmoStacks.Length; i++)
            {
                ShooterLoadoutAmmoStack stack = loadout.AmmoStacks[i];

                if (!stack.HasBullet || stack.slotIndex >= _bulletStacks.Length)
                    continue;

                _bulletStacks[stack.slotIndex].Set(stack.bullet, stack.amount);
            }
        }

        int nextItemSlot = 0;

        if (loadout.Currency != null && loadout.CurrencyAmount > 0)
        {
            SetItemStack(nextItemSlot, loadout.Currency, loadout.CurrencyAmount);
            nextItemSlot++;
        }

        if (loadout.InventoryStacks == null)
            return;

        for (int i = 0; i < loadout.InventoryStacks.Length; i++)
        {
            ShooterLoadoutItemStack stack = loadout.InventoryStacks[i];

            if (!stack.HasItem)
                continue;

            if (nextItemSlot >= _items.Count)
            {
                Debug.LogError(
                    $"[EnemyLootInventoryRuntime] Rolled inventory stacks exceed loot capacity. " +
                    $"capacity={_items.Count}, omittedFromIndex={i}",
                    this);
                break;
            }

            SetItemStack(
                nextItemSlot,
                stack.item,
                stack.amount,
                stack.armorDurability);
            nextItemSlot++;
        }
    }

    // Purpose: Returns the current weapon.
    public WeaponSO GetWeapon(int slotIndex)
    {
        EnemyLootWeaponStack stack = GetWeaponStack(slotIndex);
        return stack != null ? stack.Weapon : null;
    }

    // Purpose: Returns the current weapon amount.
    public int GetWeaponAmount(int slotIndex)
    {
        EnemyLootWeaponStack stack = GetWeaponStack(slotIndex);
        return stack != null ? stack.Amount : 0;
    }

    // Purpose: Returns the current weapon stack.
    public EnemyLootWeaponStack GetWeaponStack(int slotIndex)
    {
        NormalizeWeaponSlots();

        if (slotIndex < 0 || slotIndex >= _weaponSlots.Length)
            return null;

        EnemyLootWeaponStack stack = _weaponSlots[slotIndex];
        return stack != null && stack.IsAvailable ? stack : null;
    }

    // Purpose: Updates the weapon state.
    public void SetWeapon(int slotIndex, WeaponSO weapon, int amount = 1)
    {
        SetWeapon(slotIndex, weapon, amount, -1f, -1f);
    }

    // Purpose: Updates the weapon state.
    public void SetWeapon(
        int slotIndex,
        WeaponSO weapon,
        int amount,
        float currentDurability,
        float maxDurability)
    {
        NormalizeWeaponSlots();

        if (slotIndex < 0 || slotIndex >= _weaponSlots.Length)
            return;

        _weaponSlots[slotIndex].Set(
            weapon,
            amount,
            currentDurability,
            maxDurability);
    }

    // Purpose: Updates the armor state.
    public void SetArmor(ArmorItemSO armor, float durability)
    {
        _armor = armor;
        _armorDurability = armor != null
            ? Mathf.Clamp(durability, 0f, armor.MaxDurability)
            : 0f;
    }

    // Purpose: Updates the bullet stack state.
    public void SetBulletStack(int index, BulletSO bullet, int amount)
    {
        NormalizeBulletStacks();

        if (index < 0 || index >= _bulletStacks.Length)
            return;

        _bulletStacks[index].Set(bullet, amount);
    }

    // Purpose: Updates the item stack state.
    public void SetItemStack(int index, ItemSO item, int amount)
    {
        SetItemStack(index, item, amount, -1f);
    }

    // Purpose: Updates the item stack state.
    public void SetItemStack(int index, ItemSO item, int amount, float armorDurability)
    {
        SetItemStack(index, item, amount, armorDurability, -1f, -1f);
    }

    // Purpose: Updates the item stack state.
    public void SetItemStack(
        int index,
        ItemSO item,
        int amount,
        float armorDurability,
        float weaponCurrentDurability,
        float weaponMaxDurability)
    {
        SetItemStack(
            index,
            item,
            amount,
            armorDurability,
            weaponCurrentDurability,
            weaponMaxDurability,
            -1f,
            -1f,
            -1f);
    }

    // Purpose: Updates an item stack while retaining all repairable-item condition values.
    public void SetItemStack(
        int index,
        ItemSO item,
        int amount,
        float armorDurability,
        float weaponCurrentDurability,
        float weaponMaxDurability,
        float gasMaskFilterDurability,
        float armorMaxDurability,
        float gasMaskMaxFilterDurability)
    {
        NormalizeItemStacks();

        if (index < 0 || index >= _items.Count)
            return;

        _items[index].Set(
            item,
            amount,
            armorDurability,
            weaponCurrentDurability,
            weaponMaxDurability,
            gasMaskFilterDurability,
            armorMaxDurability,
            gasMaskMaxFilterDurability);
    }

    /// <summary>
    /// Adds an authored loot result without replacing the enemy's spawned
    /// equipment or existing carried items. The operation is all-or-nothing so
    /// a failed corpse insertion can safely fall back to a world drop.
    /// </summary>
    public bool TryAddItemStack(ItemSO item, int amount, out string message, float armorDurabilityOverride = -1f)
    {
        NormalizeItemStacks();

        if (item == null)
            return Fail("Cannot add a null loot item.", out message);

        int requested = Mathf.Max(1, amount);
        int maxStack = Mathf.Max(1, item.MaxStack);
        int capacity = InventoryCapacity;
        int available = 0;

        for (int i = 0; i < capacity; i++)
        {
            PlayerInventoryItemStack stack = i < _items.Count ? _items[i] : null;

            if (stack == null || stack.IsEmpty)
            {
                available += maxStack;
                continue;
            }

            if (IsSameItem(stack.Item, item))
                available += Mathf.Max(0, maxStack - stack.Amount);
        }

        if (available < requested)
        {
            return Fail(
                $"Loot inventory has insufficient item space. item={item.name}, requested={requested}, available={available}.",
                out message);
        }

        int remaining = requested;

        for (int i = 0; i < capacity && remaining > 0; i++)
        {
            PlayerInventoryItemStack stack = i < _items.Count ? _items[i] : null;
            if (stack == null || stack.IsEmpty || !IsSameItem(stack.Item, item))
                continue;

            int added = Mathf.Min(remaining, Mathf.Max(0, maxStack - stack.Amount));
            if (added <= 0)
                continue;

            SetItemStack(
                i,
                item,
                stack.Amount + added,
                stack.ArmorDurability,
                stack.WeaponCurrentDurability,
                stack.WeaponMaxDurability);
            remaining -= added;
        }

        float armorDurability = item is ArmorItemSO armor
            ? Mathf.Clamp(armorDurabilityOverride >= 0f ? armorDurabilityOverride : armor.MaxDurability, 0f, armor.MaxDurability) : -1f;
        float weaponDurability = item is RangedWeaponSO weapon ? weapon.MaxDurability : -1f;

        for (int i = 0; i < capacity && remaining > 0; i++)
        {
            PlayerInventoryItemStack stack = i < _items.Count ? _items[i] : null;
            if (stack != null && !stack.IsEmpty)
                continue;

            int added = Mathf.Min(remaining, maxStack);
            SetItemStack(
                i,
                item,
                added,
                armorDurability,
                weaponDurability,
                weaponDurability);
            remaining -= added;
        }

        message = string.Empty;
        return true;
    }

    public bool TryGetFirstCarriedItem<T>(out T item) where T : ItemSO
    {
        NormalizeItemStacks();

        for (int i = 0; i < _items.Count; i++)
        {
            PlayerInventoryItemStack stack = _items[i];

            if (stack == null || stack.IsEmpty || stack.Item is not T found)
                continue;

            item = found;
            return true;
        }

        item = null;
        return false;
    }

    // Purpose: Attempts to consume carried item and reports whether the request succeeded.
    public bool TryConsumeCarriedItem(ItemSO item, int amount, out string message)
    {
        NormalizeItemStacks();

        if (item == null || amount <= 0)
        {
            message = "Invalid carried-item consumption request.";
            return false;
        }

        int available = 0;

        for (int i = 0; i < _items.Count; i++)
        {
            PlayerInventoryItemStack stack = _items[i];

            if (stack != null && !stack.IsEmpty && stack.Item == item)
                available += stack.Amount;
        }

        if (available < amount)
        {
            message = $"Not enough {InventoryItemDisplayUtility.ResolveItemName(item)}.";
            return false;
        }

        int remaining = amount;

        for (int i = 0; i < _items.Count && remaining > 0; i++)
        {
            PlayerInventoryItemStack stack = _items[i];

            if (stack == null || stack.IsEmpty || stack.Item != item)
                continue;

            int removeAmount = Mathf.Min(remaining, stack.Amount);

            if (stack.Remove(removeAmount))
                stack.Clear();

            remaining -= removeAmount;
        }

        message = $"{InventoryItemDisplayUtility.ResolveItemName(item)} consumed.";
        return remaining == 0;
    }

    // Purpose: Clears the current weapon state.
    public void ClearWeapon(int slotIndex)
    {
        NormalizeWeaponSlots();

        if (slotIndex < 0 || slotIndex >= _weaponSlots.Length)
            return;

        _weaponSlots[slotIndex].Clear();
    }

    // Purpose: Attempts to swap weapon slots and reports whether the request succeeded.
    public bool TrySwapWeaponSlots(int fromSlotIndex, int toSlotIndex)
    {
        NormalizeWeaponSlots();

        if (fromSlotIndex < 0 || fromSlotIndex >= _weaponSlots.Length ||
            toSlotIndex < 0 || toSlotIndex >= _weaponSlots.Length)
        {
            return false;
        }

        if (fromSlotIndex == toSlotIndex)
            return true;

        EnemyLootWeaponStack from = _weaponSlots[fromSlotIndex];
        EnemyLootWeaponStack to = _weaponSlots[toSlotIndex];
        WeaponSO fromWeapon = from.Weapon;
        int fromAmount = from.Amount;
        WeaponSO toWeapon = to.Weapon;
        int toAmount = to.Amount;

        from.Set(toWeapon, toAmount);
        to.Set(fromWeapon, fromAmount);
        return true;
    }

    // Purpose: Clears the current armor state.
    public void ClearArmor()
    {
        _armor = null;
        _armorDurability = 0f;
    }

    // Purpose: Clears the current bullet stack state.
    public void ClearBulletStack(int index)
    {
        NormalizeBulletStacks();

        if (index < 0 || index >= _bulletStacks.Length)
            return;

        _bulletStacks[index].Clear();
    }

    // Purpose: Clears the current item stack state.
    public void ClearItemStack(int index)
    {
        NormalizeItemStacks();

        if (index < 0 || index >= _items.Count)
            return;

        _items[index].Clear();
    }

    /// <summary>
    /// Clears every equipment, ammunition, and carried-item slot before a new loot
    /// source is initialized. Treasure containers use this to replace their roll.
    /// </summary>
    public void ClearAllLoot()
    {
        NormalizeWeaponSlots();

        for (int i = 0; i < _weaponSlots.Length; i++)
            _weaponSlots[i].Clear();

        _armor = null;
        _armorDurability = 0f;

        NormalizeBulletStacks();
        NormalizeItemStacks();

        for (int i = 0; i < _bulletStacks.Length; i++)
            _bulletStacks[i].Clear();

        for (int i = 0; i < _items.Count; i++)
            _items[i].Clear();
    }

    /// <summary>
    /// Allows a non-enemy loot source, such as a treasure container, to reuse the
    /// same Loot panel and overlay request flow.
    /// </summary>
    public void ConfigureInteractionChannels(
        EnemyLootOpenRequestEventChannelSO openRequestChannel,
        UIOverlayRequestEventChannelSO overlayRequestChannel)
    {
        _openRequestChannel = openRequestChannel;
        _overlayRequestChannel = overlayRequestChannel;
    }

    // Purpose: Configures this component''s inventory capacity.
    public void ConfigureInventoryCapacity(int capacity)
    {
        _fallbackInventoryCapacity = Mathf.Max(0, capacity);
        _configuredInventoryCapacity = _fallbackInventoryCapacity; // Containers are independent from character stats.

        if (_items == null)
            _items = new List<PlayerInventoryItemStack>();

        while (_items.Count > _fallbackInventoryCapacity)
            _items.RemoveAt(_items.Count - 1);

        NormalizeItemStacks();
    }

    // Purpose: Attempts to split bullet stack and reports whether the request succeeded.
    public bool TrySplitBulletStack(
        int index,
        int amount,
        out int newIndex,
        out string message)
    {
        NormalizeBulletStacks();
        newIndex = -1;

        if (index < 0 || index >= _bulletStacks.Length)
            return Fail("Invalid bullet loot slot.", out message);

        EnemyLootBulletStack source = _bulletStacks[index];

        if (source == null || !source.IsAvailable)
            return Fail("No bullet loot.", out message);

        if (source.Amount <= 1)
            return Fail("This stack cannot be split.", out message);

        for (int i = 0; i < _bulletStacks.Length; i++)
        {
            if (i == index)
                continue;

            if (_bulletStacks[i] == null)
                _bulletStacks[i] = new EnemyLootBulletStack();

            if (!_bulletStacks[i].IsAvailable)
            {
                newIndex = i;
                break;
            }
        }

        if (newIndex < 0)
            return Fail("No empty loot bullet slot.", out message);

        int splitAmount = Mathf.Clamp(amount, 1, source.Amount - 1);
        int remainingAmount = Mathf.Max(1, source.Amount - splitAmount);
        BulletSO bullet = source.Bullet;

        source.Set(bullet, remainingAmount);
        _bulletStacks[newIndex].Set(bullet, splitAmount);
        message = $"{bullet.DisplayName} split.";
        return true;
    }

    // Purpose: Attempts to split item stack and reports whether the request succeeded.
    public bool TrySplitItemStack(
        int index,
        int amount,
        out int newIndex,
        out string message)
    {
        NormalizeItemStacks();
        newIndex = -1;

        if (index < 0 || index >= _items.Count)
            return Fail("Invalid item loot slot.", out message);

        PlayerInventoryItemStack source = _items[index];

        if (source == null || source.IsEmpty)
            return Fail("No item loot.", out message);

        if (source.Amount <= 1 || source.Item == null || source.Item.MaxStack <= 1)
            return Fail("This stack cannot be split.", out message);

        for (int i = 0; i < _items.Count; i++)
        {
            if (i == index)
                continue;

            if (_items[i] == null)
                _items[i] = new PlayerInventoryItemStack();

            if (_items[i].IsEmpty)
            {
                newIndex = i;
                break;
            }
        }

        if (newIndex < 0)
            return Fail("No empty loot item slot.", out message);

        int splitAmount = Mathf.Clamp(amount, 1, source.Amount - 1);
        int remainingAmount = Mathf.Max(1, source.Amount - splitAmount);
        ItemSO item = source.Item;
        float armorDurability = source.ArmorDurability;

        source.Set(item, remainingAmount, armorDurability);
        _items[newIndex].Set(item, splitAmount, armorDurability);
        message = $"{ResolveItemName(item)} split.";
        return true;
    }

    // Purpose: Attempts to interact and reports whether the request succeeded.
    public bool TryInteract(Component actor)
    {
        if (!CanInteract)
        {
            if (_debugInteractionLogs)
                Debug.LogWarning($"[EnemyLootInventoryRuntime] TryInteract rejected. {BuildInteractionDebugInfo()}", this);

            return false;
        }

        if (_openRequestChannel == null)
        {
            if (_debugInteractionLogs)
                Debug.LogError($"[EnemyLootInventoryRuntime] TryInteract rejected because Open Request Channel is missing. {BuildInteractionDebugInfo()}", this);

            return false;
        }

        SyncArmorDurabilityFromEquipment();

        if (_debugInteractionLogs)
        {
            Debug.Log(
                $"[EnemyLootInventoryRuntime] TryInteract accepted. " +
                $"actor={(actor != null ? actor.name : "null")}, {BuildInteractionDebugInfo()}",
                this);
        }

        OpenLootPanel(actor);
        if(GameProgressionRuntime.InRaid && actor!=null && EntityComponentResolver.Resolve<PlayerStatsRuntime>(actor)!=null)
        {
            var rules=ExperienceRulesSO.Active;
            bool first=GameProgressionRuntime.MarkDiscovery("container:"+GameProgressionRuntime.Identity(this));
            for(int i=0;first&&rules!=null&&i<ItemStacks.Count;i++)
                if(ItemStacks[i]?.IsEmpty==false)GameProgressionRuntime.Discover("loot:"+GameProgressionRuntime.Identity(this)+":"+i,rules.experience.Discovery(ItemStacks[i].Item.Rarity));
            EntityComponentResolver.Resolve<PlayerShooterInfection>(actor)?.AddExposure(InfectionAggravationStatusSO.Active?.Settings.lootOpenExposure??0);
        }
        return true;
    }

    // Purpose: Opens the after delay UI or workflow.
    private IEnumerator OpenAfterDelay(Component actor, float seconds)
    {
        _openInProgress = true;
        _interactionStartedTime = Time.time;
        _interactionDuration = Mathf.Max(0f, seconds);

        while (Time.time < _interactionStartedTime + _interactionDuration)
        {
            if (!CanInteract)
            {
                _openInProgress = false;
                _interactionStartedTime = 0f;
                _interactionDuration = 0f;
                yield break;
            }

            yield return null;
        }

        _openInProgress = false;
        _interactionStartedTime = 0f;
        _interactionDuration = 0f;

        if (!CanInteract)
            yield break;

        OpenLootPanel(actor);
    }

    // Purpose: Opens the loot panel UI or workflow.
    private void OpenLootPanel(Component actor)
    {
        if (_debugInteractionLogs)
        {
            Debug.Log(
                $"[EnemyLootInventoryRuntime] Opening loot panel. " +
                $"actor={(actor != null ? actor.name : "null")}, " +
                $"overlayChannelNull={_overlayRequestChannel == null}, " +
                $"openChannelNull={_openRequestChannel == null}",
                this);
        }

        _openRequestChannel?.RaiseEvent(
            new EnemyLootOpenRequest(this, actor, 0f));
        _overlayRequestChannel?.Open(UIOverlayId.LootHub);
    }

    // Purpose: Builds the interaction debug info from the current authored data.
    public string BuildInteractionDebugInfo()
    {
        ResolveRefs();

        int bulletStackCount = 0;
        int itemStackCount = 0;

        NormalizeBulletStacks();
        NormalizeItemStacks();

        for (int i = 0; i < _bulletStacks.Length; i++)
        {
            if (_bulletStacks[i] != null && _bulletStacks[i].IsAvailable)
                bulletStackCount++;
        }

        for (int i = 0; i < _items.Count; i++)
        {
            if (_items[i] != null && !_items[i].IsEmpty)
                itemStackCount++;
        }

        return
            $"owner={name}, active={gameObject.activeInHierarchy}, " +
            $"canInteract={CanInteract}, hasLoot={HasLoot}, " +
            $"weapons={BuildWeaponDebugText()}, " +
            $"armor={(_armor != null ? _armor.name : "null")}, " +
            $"bulletStacks={bulletStackCount}, itemStacks={itemStackCount}, " +
            $"deadOnly={_canLootOnlyWhenDead}, " +
            $"damageableNull={_damageable == null}, " +
            $"isDead={(_damageable != null && _damageable.IsDead)}, " +
            $"openChannelNull={_openRequestChannel == null}, " +
            $"overlayChannelNull={_overlayRequestChannel == null}";
    }

    // Purpose: Attempts to take weapon and reports whether the request succeeded.
    public bool TryTakeWeapon(
        int weaponSlotIndex,
        PlayerInventoryRuntime inventory,
        out string message)
    {
        EnemyLootWeaponStack stack = GetWeaponStack(weaponSlotIndex);

        if (stack == null)
            return Fail("No weapon loot.", out message);

        if (inventory == null)
            return Fail("Player inventory is missing.", out message);

        WeaponSO weapon = stack.Weapon;
        int amount = stack.Amount;

        if (!inventory.TryPickupWeaponLoot(
                weapon,
                amount,
                stack.CurrentDurability,
                stack.MaxDurability,
                out message))
            return false;

        stack.Clear();
        return true;
    }

    // Purpose: Attempts to take armor and reports whether the request succeeded.
    public bool TryTakeArmor(PlayerInventoryRuntime inventory, out string message)
    {
        if (_armor == null)
            return Fail("No armor loot.", out message);

        if (inventory == null)
            return Fail("Player inventory is missing.", out message);

        SyncArmorDurabilityFromEquipment();

        ArmorItemSO armor = _armor;
        float durability = ArmorDurability;

        if (!inventory.TryPickupArmorLoot(armor, durability, out message))
            return false;

        // Clear the authoritative equipment as well, so later UI sync cannot resurrect a looted vest.
        if (_equipmentRuntime != null && _equipmentRuntime.CurrentArmor == armor) _equipmentRuntime.ClearArmor();
        _armor = null;
        _armorDurability = 0f;
        return true;
    }

    // Purpose: Attempts to take bullet stack and reports whether the request succeeded.
    public bool TryTakeBulletStack(
        int index,
        PlayerInventoryRuntime inventory,
        out string message)
    {
        NormalizeBulletStacks();

        if (index < 0 || index >= _bulletStacks.Length)
            return Fail("Invalid bullet loot slot.", out message);

        EnemyLootBulletStack stack = _bulletStacks[index];

        if (stack == null || !stack.IsAvailable)
            return Fail("No bullet loot.", out message);

        if (inventory == null)
            return Fail("Player inventory is missing.", out message);

        BulletSO bullet = stack.Bullet;
        int amount = stack.Amount;

        if (!inventory.TryPickup(bullet, 1, amount, 0f, out message))
            return false;

        stack.Clear();
        return true;
    }

    // Purpose: Attempts to take item stack and reports whether the request succeeded.
    public virtual bool TryTakeItemStack(
        int index,
        PlayerInventoryRuntime inventory,
        out string message)
    {
        NormalizeItemStacks();

        if (index < 0 || index >= _items.Count)
            return Fail("Invalid item loot slot.", out message);

        PlayerInventoryItemStack stack = _items[index];

        if (stack == null || stack.IsEmpty)
            return Fail("No item loot.", out message);

        if (inventory == null)
            return Fail("Player inventory is missing.", out message);

        ItemSO item = stack.Item;
        int amount = stack.Amount;
        float armorDurability = stack.ArmorDurability;
        float armorMaxDurability = stack.ArmorMaxDurability;
        float weaponCurrentDurability = stack.WeaponCurrentDurability;
        float weaponMaxDurability = stack.WeaponMaxDurability;
        float gasMaskFilterDurability = stack.GasMaskFilterDurability;
        float gasMaskMaxFilterDurability = stack.GasMaskMaxFilterDurability;

        if (_debugItemTransferLogs)
        {
            Debug.Log(
                $"[EnemyLootTransfer] begin source={name}, lootSlot={index}, item={item.name}, guid={item.Guid}, " +
                $"instance={item.GetInstanceID()}, amount={amount}, maxStack={item.MaxStack}",
                this);
        }

        bool accepted;

        if (item is PassiveItemSO passive)
        {
            accepted = inventory.TryPickupPassiveItem(
                passive,
                amount,
                out message);
        }
        else
        {
            accepted = inventory.TryStoreExternalItemToInventorySlot(
                item,
                amount,
                armorDurability,
                weaponCurrentDurability,
                weaponMaxDurability,
                gasMaskFilterDurability,
                armorMaxDurability,
                gasMaskMaxFilterDurability,
                -1,
                out message);
        }

        if (!accepted)
        {
            if (_debugItemTransferLogs)
                Debug.LogWarning($"[EnemyLootTransfer] rejected source={name}, lootSlot={index}, item={item.name}, reason={message}", this);

            return false;
        }

        stack.Clear();

        if (_debugItemTransferLogs)
            Debug.Log($"[EnemyLootTransfer] completed source={name}, lootSlot={index}, item={item.name}, amount={amount}, result={message}", this);

        return true;
    }

    // Purpose: Synchronizes armor durability from equipment with the current source state.
    public void SyncArmorDurabilityFromEquipment()
    {
        if (_equipmentRuntime == null)
            return;

        // Equipment is authoritative: a destroyed vest must not reappear in corpse loot.
        _armor = _equipmentRuntime.CurrentArmor;
        _armorDurability = _armor != null ? _equipmentRuntime.CurrentArmorDurability : 0f;
        // Do not heal damaged armor on death; repeated UI syncs never reroll the quality cap.
        if (_corpseArmorRolled) _armorDurability = Mathf.Min(_armorDurability, _corpseArmorCap);
    }

    // Purpose: Synchronizes equipment from runtime with the current source state.
    public void SyncEquipmentFromRuntime()
    {
        if (_equipmentRuntime == null)
            return;

        NormalizeWeaponSlots();

        for (int i = 0; i < _weaponSlots.Length; i++)
        {
            if (i < _equipmentRuntime.WeaponSlotCount && _equipmentRuntime.GetWeapon(i) is not MeleeWeaponSO)
            {
                _weaponSlots[i].Set(
                    _equipmentRuntime.GetWeapon(i),
                    _equipmentRuntime.GetWeaponAmount(i),
                    _equipmentRuntime.GetWeaponCurrentDurability(i),
                    _equipmentRuntime.GetWeaponMaxDurability(i));
            }
            else
            {
                _weaponSlots[i].Clear();
            }
        }

        SyncArmorDurabilityFromEquipment();
    }

    // Purpose: Resolves the refs needed by this component.
    private void ResolveRefs()
    {
        if (_damageable == null)
            _damageable = EntityComponentResolver.Resolve<Damageable>(this);

        if (_equipmentRuntime == null)
            _equipmentRuntime = EntityComponentResolver.Resolve<EntityEquipmentRuntime>(this);

        if (_statsRuntime == null)
            _statsRuntime = EntityComponentResolver.Resolve<ShooterStatsRuntime>(this);
    }

    // Purpose: Normalizes bullet stacks to a safe authored value.
    private void NormalizeBulletStacks()
    {
        if (_bulletStacks == null || _bulletStacks.Length == 0)
        {
            _bulletStacks = new[]
            {
                new EnemyLootBulletStack(),
                new EnemyLootBulletStack(),
                new EnemyLootBulletStack()
            };
            return;
        }

        for (int i = 0; i < _bulletStacks.Length; i++)
        {
            if (_bulletStacks[i] == null)
                _bulletStacks[i] = new EnemyLootBulletStack();
        }
    }

    // Purpose: Normalizes weapon slots to a safe authored value.
    private void NormalizeWeaponSlots()
    {
        if (_weaponSlots == null || _weaponSlots.Length != WeaponSlotCount)
        {
            EnemyLootWeaponStack[] previous = _weaponSlots;
            _weaponSlots = new EnemyLootWeaponStack[WeaponSlotCount];

            for (int i = 0; i < _weaponSlots.Length; i++)
            {
                _weaponSlots[i] = previous != null && i < previous.Length
                    ? previous[i]
                    : new EnemyLootWeaponStack();
            }
        }

        for (int i = 0; i < _weaponSlots.Length; i++)
        {
            if (_weaponSlots[i] == null)
                _weaponSlots[i] = new EnemyLootWeaponStack();
        }
    }

    // Purpose: Normalizes item stacks to a safe authored value.
    private void NormalizeItemStacks()
    {
        NormalizeInventoryStorage(_items, InventoryCapacity);
    }

    // Purpose: Checks whether this component has bullet loot.
    private bool HasBulletLoot()
    {
        NormalizeBulletStacks();

        for (int i = 0; i < _bulletStacks.Length; i++)
        {
            if (_bulletStacks[i] != null && _bulletStacks[i].IsAvailable)
                return true;
        }

        return false;
    }

    // Purpose: Checks whether same item is currently true.
    private static bool IsSameItem(ItemSO left, ItemSO right)
    {
        if (left == right)
            return true;

        return left != null &&
               right != null &&
               !string.IsNullOrWhiteSpace(left.Guid) &&
               left.Guid == right.Guid;
    }

    // Purpose: Checks whether this component has weapon loot.
    private bool HasWeaponLoot()
    {
        NormalizeWeaponSlots();

        for (int i = 0; i < _weaponSlots.Length; i++)
        {
            if (_weaponSlots[i].IsAvailable)
                return true;
        }

        return false;
    }

    // Purpose: Builds the weapon debug text from the current authored data.
    private string BuildWeaponDebugText()
    {
        NormalizeWeaponSlots();
        string result = string.Empty;

        for (int i = 0; i < _weaponSlots.Length; i++)
        {
            EnemyLootWeaponStack stack = _weaponSlots[i];

            if (i > 0)
                result += ", ";

            result += stack != null && stack.IsAvailable
                ? $"slot{i}:{stack.Weapon.name}x{stack.Amount}"
                : $"slot{i}:empty";
        }

        return result;
    }

    // Purpose: Checks whether this component has item loot.
    private bool HasItemLoot()
    {
        NormalizeItemStacks();

        for (int i = 0; i < _items.Count; i++)
        {
            if (_items[i] != null && !_items[i].IsEmpty)
                return true;
        }

        return false;
    }

    // Purpose: Resolves the armor durability needed by this component.
    private float ResolveArmorDurability()
    {
        if (_armor == null)
            return 0f;

        return Mathf.Clamp(_armorDurability, 0f, _armor.MaxDurability);
    }

    // Purpose: Marks the active fail workflow as failed.
    private static bool Fail(string error, out string message)
    {
        message = error;
        return false;
    }

    // Purpose: Resolves the item name needed by this component.
    private static string ResolveItemName(ItemSO item)
    {
        if (item == null)
            return "Item";

        if (item is BulletSO bullet)
            return bullet.DisplayName;

        if (item is WeaponSO weapon)
            return weapon.DisplayName;

        if (item is ArmorItemSO armor)
            return armor.DisplayName;

        return item.name;
    }
}
