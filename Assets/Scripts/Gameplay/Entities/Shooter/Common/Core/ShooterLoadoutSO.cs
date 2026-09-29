/*
 * Role hierarchy: Gameplay > Entities > Shooter > Common > Core
 * Primary responsibility: Manages ShooterLoadoutAmmoStack item storage, equipment, transfer state, and related rules.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using System;
using UnityEngine;

public readonly struct ShooterLoadoutAmmoStack
{
    public readonly int sourceWeaponSlotIndex;
    public readonly int slotIndex;
    public readonly BulletSO bullet;
    public readonly int amount;

    public bool HasBullet => bullet != null && amount > 0;

    public ShooterLoadoutAmmoStack(
        int sourceWeaponSlotIndex,
        int slotIndex,
        BulletSO bullet,
        int amount)
    {
        this.sourceWeaponSlotIndex = Mathf.Max(0, sourceWeaponSlotIndex);
        this.slotIndex = Mathf.Max(0, slotIndex);
        this.bullet = bullet;
        this.amount = Mathf.Max(0, amount);
    }
}

public readonly struct ShooterLoadoutItemStack
{
    public readonly ItemSO item;
    public readonly int amount;
    public readonly float armorDurability;

    public bool HasItem => item != null && amount > 0;

    public ShooterLoadoutItemStack(ItemSO item, int amount, float armorDurability = -1f)
    {
        this.item = item;
        this.amount = Mathf.Max(0, amount);
        this.armorDurability = item is ArmorItemSO ? armorDurability : -1f;
    }
}

public readonly struct ShooterLoadoutWeaponStack
{
    public readonly int slotIndex;
    public readonly WeaponSO weapon;
    public readonly int amount;

    public bool HasWeapon => weapon != null && amount > 0;

    public ShooterLoadoutWeaponStack(int slotIndex, WeaponSO weapon, int amount)
    {
        this.slotIndex = Mathf.Max(0, slotIndex);
        this.weapon = weapon;
        this.amount = Mathf.Max(0, amount);
    }
}

public readonly struct ShooterLoadoutSnapshot
{
    public readonly WeaponSO weapon;
    public readonly ArmorItemSO armor;
    public readonly float armorDurability;
    public readonly ShooterLoadoutWeaponStack[] weaponStacks;
    public readonly ShooterLoadoutAmmoStack[] ammoStacks;
    public readonly ShooterLoadoutItemStack[] inventoryStacks;

    public ShooterLoadoutSnapshot(
        WeaponSO weapon,
        ArmorItemSO armor,
        float armorDurability,
        ShooterLoadoutAmmoStack[] ammoStacks,
        ShooterLoadoutItemStack[] inventoryStacks)
    {
        this.weapon = weapon;
        this.armor = armor;
        this.armorDurability = armorDurability;
        weaponStacks = weapon != null
            ? new[] { new ShooterLoadoutWeaponStack(0, weapon, 1) }
            : Array.Empty<ShooterLoadoutWeaponStack>();
        this.ammoStacks = ammoStacks ?? Array.Empty<ShooterLoadoutAmmoStack>();
        this.inventoryStacks = inventoryStacks ?? Array.Empty<ShooterLoadoutItemStack>();
    }

    public ShooterLoadoutSnapshot(
        ShooterLoadoutWeaponStack[] weaponStacks,
        ArmorItemSO armor,
        float armorDurability,
        ShooterLoadoutAmmoStack[] ammoStacks,
        ShooterLoadoutItemStack[] inventoryStacks)
    {
        this.weaponStacks = weaponStacks ?? Array.Empty<ShooterLoadoutWeaponStack>();
        weapon = ResolvePrimaryWeapon(this.weaponStacks);
        this.armor = armor;
        this.armorDurability = armorDurability;
        this.ammoStacks = ammoStacks ?? Array.Empty<ShooterLoadoutAmmoStack>();
        this.inventoryStacks = inventoryStacks ?? Array.Empty<ShooterLoadoutItemStack>();
    }

    // Purpose: Performs the to enemy shooter loadout workflow for this component.
    public EnemyShooterLoadout ToEnemyShooterLoadout()
    {
        return new EnemyShooterLoadout(
            weaponStacks,
            ammoStacks,
            armor,
            null,
            0,
            inventoryStacks);
    }

    // Purpose: Resolves the primary weapon needed by this component.
    private static WeaponSO ResolvePrimaryWeapon(ShooterLoadoutWeaponStack[] stacks)
    {
        if (stacks == null)
            return null;

        for (int i = 0; i < stacks.Length; i++)
        {
            if (stacks[i].HasWeapon)
                return stacks[i].weapon;
        }

        return null;
    }
}

public readonly struct EnemyShooterLoadout
{
    public readonly ShooterLoadoutWeaponStack[] WeaponStacks;
    public readonly ShooterLoadoutAmmoStack[] AmmoStacks;
    public readonly ArmorItemSO Armor;
    public readonly CurrencyItemSO Currency;
    public readonly int CurrencyAmount;
    public readonly ShooterLoadoutItemStack[] InventoryStacks;

    public EnemyShooterLoadout(
        ShooterLoadoutWeaponStack[] weaponStacks,
        ShooterLoadoutAmmoStack[] ammoStacks,
        ArmorItemSO armor,
        CurrencyItemSO currency = null,
        int currencyAmount = 0,
        ShooterLoadoutItemStack[] inventoryStacks = null)
    {
        WeaponStacks = weaponStacks ?? Array.Empty<ShooterLoadoutWeaponStack>();
        AmmoStacks = ammoStacks ?? Array.Empty<ShooterLoadoutAmmoStack>();
        Armor = armor;
        Currency = currency;
        CurrencyAmount = Mathf.Max(0, currencyAmount);
        InventoryStacks = inventoryStacks ?? Array.Empty<ShooterLoadoutItemStack>();
    }

    public BulletSO Bullet => GetBulletForWeaponSlot(0, BulletAmmoType.Basic);

    // Purpose: Returns the current weapon.
    public WeaponSO GetWeapon(int slotIndex)
    {
        if (WeaponStacks == null)
            return null;

        for (int i = 0; i < WeaponStacks.Length; i++)
        {
            ShooterLoadoutWeaponStack stack = WeaponStacks[i];

            if (stack.HasWeapon && stack.slotIndex == slotIndex)
                return stack.weapon;
        }

        return null;
    }

    // Purpose: Returns the current bullet for weapon slot.
    public BulletSO GetBulletForWeaponSlot(
        int weaponSlotIndex,
        BulletAmmoType ammoType = BulletAmmoType.Basic)
    {
        if (GetWeapon(weaponSlotIndex) is not RangedWeaponSO rangedWeapon || AmmoStacks == null)
            return null;

        for (int i = 0; i < AmmoStacks.Length; i++)
        {
            ShooterLoadoutAmmoStack stack = AmmoStacks[i];

            if (!stack.HasBullet || stack.sourceWeaponSlotIndex != weaponSlotIndex)
                continue;

            if (stack.bullet.AmmoType == ammoType &&
                rangedWeapon.IsCompatibleWith(stack.bullet))
                return stack.bullet;
        }

        return null;
    }
}

[Serializable]
public sealed class ShooterLoadoutAmmoCandidate
{
    [Tooltip("One ammunition candidate for this pool. It must match both the selected ranged weapon's Bullet Family and the pool's Ammo Type.")]
    public BulletSO bullet;

    [Tooltip("Relative weight used when this pool selects exactly one compatible bullet. The weights do not need to total 100.")]
    [Min(0f)] public float weight = 1f;

    [Min(0)] public int minAmount = 15;
    [Min(0)] public int maxAmount = 20;

    // Purpose: Checks whether compatible with is currently true.
    public bool IsCompatibleWith(RangedWeaponSO weapon, BulletAmmoType requiredAmmoType)
    {
        return bullet != null &&
               bullet.AmmoType == requiredAmmoType &&
               weapon != null &&
               weapon.IsCompatibleWith(bullet);
    }

    // Purpose: Rolls the roll result from its configured probabilities.
    public ShooterLoadoutAmmoStack Roll(int sourceWeaponSlotIndex, int destinationAmmoSlotIndex)
    {
        int min = Mathf.Max(0, minAmount);
        int max = Mathf.Max(min, maxAmount);

        return new ShooterLoadoutAmmoStack(
            sourceWeaponSlotIndex,
            destinationAmmoSlotIndex,
            bullet,
            UnityEngine.Random.Range(min, max + 1));
    }
}

[Serializable]
public sealed class ShooterLoadoutAmmoPool
{
    [Tooltip("Stable ID of the Weapon Set that owns this ammo pool. Use the Shooter Loadout Inspector selector instead of typing it by hand.")]
    public string weaponSetId;

    [Tooltip("Weapon equipment slot inside the selected Weapon Set whose compatibility controls this pool. Slot indices are zero-based.")]
    [Min(0)] public int sourceWeaponSlotIndex;

    [Tooltip(
        "Stable inventory ordering value for this starting ammunition stack. " +
        "Ammunition is stored in the carried inventory and is not equipped in an ammo slot.")]
    [Min(0)] public int destinationAmmoSlotIndex;

    [Tooltip("Basic and Special ammunition use separate active selections. Only candidates of this type participate in this pool.")]
    public BulletAmmoType ammoType = BulletAmmoType.Basic;

    [Tooltip("Exactly one enabled compatible candidate is selected by relative Weight whenever this pool's Weapon Set is selected.")]
    public ShooterLoadoutAmmoCandidate[] candidates;

    // Purpose: Checks whether the current data matches weapon set.
    public bool MatchesWeaponSet(ShooterLoadoutWeaponEntry weaponSet)
    {
        return weaponSet != null &&
               !string.IsNullOrWhiteSpace(weaponSetId) &&
               string.Equals(weaponSetId, weaponSet.SetId, StringComparison.Ordinal);
    }

    // Purpose: Attempts to roll and reports whether the request succeeded.
    public bool TryRoll(RangedWeaponSO weapon, out ShooterLoadoutAmmoStack stack)
    {
        stack = default;

        if (weapon == null || candidates == null || candidates.Length == 0)
            return false;

        float totalWeight = 0f;

        for (int i = 0; i < candidates.Length; i++)
        {
            ShooterLoadoutAmmoCandidate candidate = candidates[i];

            if (candidate == null || candidate.weight <= 0f || !candidate.IsCompatibleWith(weapon, ammoType))
                continue;

            totalWeight += candidate.weight;
        }

        if (totalWeight <= 0f)
            return false;

        float roll = UnityEngine.Random.Range(0f, totalWeight);

        for (int i = 0; i < candidates.Length; i++)
        {
            ShooterLoadoutAmmoCandidate candidate = candidates[i];

            if (candidate == null || candidate.weight <= 0f || !candidate.IsCompatibleWith(weapon, ammoType))
                continue;

            roll -= candidate.weight;

            if (roll > 0f)
                continue;

            stack = candidate.Roll(sourceWeaponSlotIndex, destinationAmmoSlotIndex);
            return stack.HasBullet;
        }

        return false;
    }
}

[Serializable]
public sealed class ShooterLoadoutWeaponEntry
{
    [Tooltip("Stable unique ID used by top-level Ammo Pools to reference this complete Weapon Set.")]
    public string setId = "weapon_set";

    [Tooltip("Weapon equipped in slot 0 when this complete two-slot set is selected. Null intentionally leaves slot 0 empty.")]
    public WeaponSO weaponSlot0;

    [Tooltip("Stack amount for slot 0. Normal guns and melee weapons should stay at 1.")]
    [Min(1)] public int weaponSlot0Amount = 1;

    [Tooltip("Weapon equipped in slot 1 when this complete two-slot set is selected. Null intentionally leaves slot 1 empty.")]
    public WeaponSO weaponSlot1;

    [Tooltip("Stack amount for slot 1. Use values above 1 for stackable throwable weapons when needed.")]
    [Min(1)] public int weaponSlot1Amount = 1;

    [Tooltip("Relative selection weight for this complete two-slot weapon set. Set to zero to disable the set.")]
    [Min(0f)] public float weight = 1f;

    public string SetId => string.IsNullOrWhiteSpace(setId) ? string.Empty : setId.Trim();

    // Purpose: Returns the current weapon.
    public WeaponSO GetWeapon(int slotIndex)
    {
        return slotIndex switch
        {
            0 => weaponSlot0,
            1 => weaponSlot1,
            _ => null
        };
    }

    // Purpose: Rolls the roll result from its configured probabilities.
    public ShooterLoadoutWeaponStack[] Roll()
    {
        // Keep both slots in the snapshot so null is an intentional empty slot,
        // rather than an omitted or malformed loadout value.
        return new[]
        {
            new ShooterLoadoutWeaponStack(
                0,
                weaponSlot0,
                weaponSlot0 != null ? Mathf.Max(1, weaponSlot0Amount) : 0),
            new ShooterLoadoutWeaponStack(
                1,
                weaponSlot1,
                weaponSlot1 != null ? Mathf.Max(1, weaponSlot1Amount) : 0)
        };
    }
}

[Serializable]
public sealed class ShooterLoadoutArmorEntry
{
    public ArmorItemSO armor;
    [Min(0f)] public float weight = 1f;

    [Tooltip("If negative, armor starts at max durability.")]
    public float durability = -1f;

    // Purpose: Resolves the durability needed by this component.
    public float ResolveDurability()
    {
        if (armor == null)
            return 0f;

        return durability >= 0f
            ? Mathf.Clamp(durability, 0f, armor.MaxDurability)
            : armor.MaxDurability;
    }
}

[Serializable]
public sealed class ShooterLoadoutItemEntry
{
    [Tooltip("Item placed in the character's general inventory when this entry succeeds. Leave null to disable the entry.")]
    public ItemSO item;

    [Tooltip("Independent chance for this item to be generated. 100 always generates it; 0 disables it.")]
    [Range(0f, 100f)] public float probability = 100f;

    [Min(0)] public int minAmount = 1;
    [Min(0)] public int maxAmount = 1;

    [Tooltip("Only used when item is ArmorItemSO and this stack preserves durability.")]
    public float armorDurability = -1f;

    // Purpose: Rolls the roll result from its configured probabilities.
    public ShooterLoadoutItemStack Roll()
    {
        if (item == null)
            return default;

        int min = Mathf.Max(0, minAmount);
        int max = Mathf.Max(min, maxAmount);

        return new ShooterLoadoutItemStack(
            item,
            UnityEngine.Random.Range(min, max + 1),
            armorDurability);
    }

    // Purpose: Performs the passes probability workflow for this component.
    public bool PassesProbability()
    {
        float chance = Mathf.Clamp(probability, 0f, 100f);

        if (chance <= 0f)
            return false;

        if (chance >= 100f)
            return true;

        return UnityEngine.Random.value * 100f < chance;
    }
}

[CreateAssetMenu(
    fileName = "ShooterLoadout",
    menuName = "Game/Shooter/Loadout")]
public class ShooterLoadoutSO : ScriptableObject
{
    [Header("Weapon Equipment")]
    [Tooltip("Weighted list of complete two-slot Weapon Sets. Exactly one enabled set is selected. Set ID connects a set to top-level Ammo Pools.")]
    [SerializeField] private ShooterLoadoutWeaponEntry[] _weaponEntries;

    [Header("Starting Ammo Inventory")]
    [Tooltip(
        "Top-level starting-ammunition pools. Each pool references one Weapon Set ID, " +
        "selects one compatible weighted candidate, and stores the rolled stack in inventory. " +
        "T cycles compatible inventory ammunition at runtime; no ammo equipment slot is created.")]
    [SerializeField] private ShooterLoadoutAmmoPool[] _ammoPools;

    [Header("Armor Equipment")]
    [SerializeField] private ShooterLoadoutArmorEntry[] _armorEntries;

    [Header("Inventory")]
    [Tooltip("Each entry is evaluated independently. Use Probability 100 for fixed player items and a lower value for optional enemy loot.")]
    [SerializeField] private ShooterLoadoutItemEntry[] _inventoryEntries;

    // Purpose: Rolls the roll result from its configured probabilities.
    public ShooterLoadoutSnapshot Roll()
    {
        ShooterLoadoutWeaponEntry weaponEntry = PickWeaponEntry();
        ShooterLoadoutWeaponStack[] weaponStacks = weaponEntry != null
            ? weaponEntry.Roll()
            : Array.Empty<ShooterLoadoutWeaponStack>();

        ShooterLoadoutArmorEntry armorEntry = PickWeighted(
            _armorEntries,
            entry => entry != null && entry.weight > 0f,
            entry => entry.weight);

        ShooterLoadoutAmmoStack[] ammoStacks = RollAmmoStacks(weaponEntry);
        ShooterLoadoutItemStack[] itemStacks = RollInventoryStacks();

        return new ShooterLoadoutSnapshot(
            weaponStacks,
            armorEntry != null ? armorEntry.armor : null,
            armorEntry != null ? armorEntry.ResolveDurability() : 0f,
            ammoStacks,
            itemStacks);
    }

    // Purpose: Performs the pick weapon entry workflow for this component.
    private ShooterLoadoutWeaponEntry PickWeaponEntry()
    {
        return PickWeighted(
            _weaponEntries,
            candidate => candidate != null &&
                         candidate.weight > 0f &&
                         !string.IsNullOrWhiteSpace(candidate.SetId),
            candidate => candidate.weight);
    }

    // Purpose: Rolls the ammo stacks result from its configured probabilities.
    private ShooterLoadoutAmmoStack[] RollAmmoStacks(ShooterLoadoutWeaponEntry weaponEntry)
    {
        if (weaponEntry == null || _ammoPools == null || _ammoPools.Length == 0)
            return Array.Empty<ShooterLoadoutAmmoStack>();

        var results = new System.Collections.Generic.List<ShooterLoadoutAmmoStack>();

        for (int i = 0; i < _ammoPools.Length; i++)
        {
            ShooterLoadoutAmmoPool pool = _ammoPools[i];

            if (pool == null || !pool.MatchesWeaponSet(weaponEntry))
                continue;

            WeaponSO sourceWeapon = weaponEntry.GetWeapon(pool.sourceWeaponSlotIndex);

            if (sourceWeapon is not RangedWeaponSO rangedWeapon)
            {
                Debug.LogError(
                    $"[ShooterLoadoutSO] Ammo Pool points to a non-ranged or empty weapon slot. loadout={name}, weaponSet={weaponEntry.SetId}, sourceWeaponSlot={pool.sourceWeaponSlotIndex}.",
                    this);
                continue;
            }

            if (!pool.TryRoll(rangedWeapon, out ShooterLoadoutAmmoStack stack))
            {
                Debug.LogError(
                    $"[ShooterLoadoutSO] No enabled compatible ammo candidate exists. loadout={name}, weaponSet={weaponEntry.SetId}, weapon={rangedWeapon.name}, requiredFamily={rangedWeapon.RequiredBulletFamily}, ammoType={pool.ammoType}, inventoryOrder={pool.destinationAmmoSlotIndex}.",
                    this);
                continue;
            }

            if (stack.HasBullet)
                results.Add(stack);
        }

        return results.ToArray();
    }

    // Purpose: Rolls the inventory stacks result from its configured probabilities.
    private ShooterLoadoutItemStack[] RollInventoryStacks()
    {
        if (_inventoryEntries == null || _inventoryEntries.Length == 0)
            return Array.Empty<ShooterLoadoutItemStack>();

        var results = new System.Collections.Generic.List<ShooterLoadoutItemStack>();

        for (int i = 0; i < _inventoryEntries.Length; i++)
        {
            ShooterLoadoutItemEntry entry = _inventoryEntries[i];

            if (entry == null || entry.item == null || !entry.PassesProbability())
                continue;

            ShooterLoadoutItemStack stack = entry.Roll();

            if (stack.HasItem)
                results.Add(stack);
        }

        return results.ToArray();
    }

    private static T PickWeighted<T>(
        T[] entries,
        Predicate<T> isValid,
        Func<T, float> getWeight)
        where T : class
    {
        if (entries == null || entries.Length == 0)
            return null;

        float totalWeight = 0f;

        for (int i = 0; i < entries.Length; i++)
        {
            T entry = entries[i];

            if (isValid != null && !isValid(entry))
                continue;

            totalWeight += Mathf.Max(0f, getWeight(entry));
        }

        if (totalWeight <= 0f)
            return null;

        float roll = UnityEngine.Random.Range(0f, totalWeight);

        for (int i = 0; i < entries.Length; i++)
        {
            T entry = entries[i];

            if (isValid != null && !isValid(entry))
                continue;

            roll -= Mathf.Max(0f, getWeight(entry));

            if (roll <= 0f)
                return entry;
        }

        return null;
    }
}
