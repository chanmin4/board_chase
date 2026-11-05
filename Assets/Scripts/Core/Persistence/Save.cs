/*
 * Role hierarchy: Core > Persistence
 * Primary responsibility: Stores and restores Save runtime data and persistent state.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization;

[Serializable]
public class Save
{
    public PlayerExperienceSaveData experience = new();
    public int recyclingSlotBonus;
    public List<string> recyclingSlotReceipts=new();
    public List<string> questWarehouseItemReceipts=new();
    public List<PassiveInstanceSaveData> passiveInstances = new();
    public List<QuestPassiveSlotRewardSaveData> questPocketSlotRewards = new();
    public int PocketSlotBonus
    {
        get
        {
            int count = 0;
            var seen = new HashSet<string>();
            foreach (var row in questPocketSlotRewards ?? new())
                if (row != null && row.slotAmount > 0 && seen.Add(row.questId + ":" + row.rewardIndex)) count += Mathf.Min(4, row.slotAmount);
            return Mathf.Clamp(count, 0, 4);
        }
    }
    public List<QuestPassiveSlotRewardSaveData> _questPassiveSlotRewards = new();
    public List<QuestPassiveSlotRewardSaveData> QuestPassiveSlotRewards => _questPassiveSlotRewards ??= new();
    public int UnlockedPassiveSlotCount
    {
        get
        {
            int count = PlayerPassiveInventoryRuntime.MinimumUnlockedSlotCount;
            var keys = new HashSet<string>(StringComparer.Ordinal);
            foreach (var row in QuestPassiveSlotRewards)
                if (row != null && !string.IsNullOrWhiteSpace(row.questId) && row.rewardIndex >= 0 &&
                    keys.Add(row.questId + ":" + row.rewardIndex))
                    count += Mathf.Clamp(row.slotAmount, 0, PlayerPassiveInventoryRuntime.MaximumSlotCount);
            return Mathf.Clamp(count, PlayerPassiveInventoryRuntime.MinimumUnlockedSlotCount, PlayerPassiveInventoryRuntime.MaximumSlotCount);
        }
    }
    public const int CurrentSchemaVersion = 1;

    [Tooltip("Schema version for permanent player progression. Upgrade this through SaveMigration when the profile contract changes.")]
    public int schemaVersion = CurrentSchemaVersion;

    [Header("WIP Legacy Numeric Currency Migration")]
    [Tooltip("Read only for old save compatibility. Active money is stored as physical CurrencyItemSO inventory stacks.")]
    public int _roguelikeCurrency;

    [Header("Persistent Progress")]
    [HideInInspector,Tooltip("WIP legacy save payload only. Preserved for old profiles; never applied to active character stats.")]
    public MetaUpgradeSaveData _metaUpgrades = new MetaUpgradeSaveData();
    public InheritanceWarehouseSaveData _inheritanceWarehouse =
        new InheritanceWarehouseSaveData();

    [Tooltip("Contiguous StageRunFlowSO unlock boundary. -1 means the profile has not imported the legacy PlayerPrefs value yet.")]
    public int _highestUnlockedStageIndex = -1;
    public List<ShopUnlockProgressSaveData> _shopUnlockProgress =
        new List<ShopUnlockProgressSaveData>();
    public List<QuestProgressSaveData> _questProgress =
        new List<QuestProgressSaveData>();
    [Tooltip("One persistent stat modifier per completed quest reward. The quest id plus reward index is the stable duplicate guard.")]
    public List<QuestStatRewardSaveData> _questStatRewards =
        new List<QuestStatRewardSaveData>();
    [Tooltip("One permanent Inheritance Warehouse capacity reward per completed quest reward. The quest id plus reward index is the stable duplicate guard.")]
    public List<QuestWarehouseSlotRewardSaveData> _questWarehouseSlotRewards =
        new List<QuestWarehouseSlotRewardSaveData>();

    public MetaUpgradeSaveData MetaUpgrades
    {
        get
        {
            if (_metaUpgrades == null)
                _metaUpgrades = new MetaUpgradeSaveData();

            return _metaUpgrades;
        }
    }

    public InheritanceWarehouseSaveData InheritanceWarehouse
    {
        get
        {
            if (_inheritanceWarehouse == null)
                _inheritanceWarehouse = new InheritanceWarehouseSaveData();

            return _inheritanceWarehouse;
        }
    }

    public int HighestUnlockedStageIndex
    {
        get => _highestUnlockedStageIndex;
        set => _highestUnlockedStageIndex = Mathf.Max(-1, value);
    }

    public List<ShopUnlockProgressSaveData> ShopUnlockProgress
    {
        get
        {
            if (_shopUnlockProgress == null)
                _shopUnlockProgress = new List<ShopUnlockProgressSaveData>();

            return _shopUnlockProgress;
        }
    }

    public List<QuestProgressSaveData> QuestProgress
    {
        get
        {
            if (_questProgress == null)
                _questProgress = new List<QuestProgressSaveData>();

            return _questProgress;
        }
    }

    public List<QuestStatRewardSaveData> QuestStatRewards
    {
        get
        {
            if (_questStatRewards == null)
                _questStatRewards = new List<QuestStatRewardSaveData>();

            return _questStatRewards;
        }
    }

    public List<QuestWarehouseSlotRewardSaveData> QuestWarehouseSlotRewards
    {
        get
        {
            if (_questWarehouseSlotRewards == null)
                _questWarehouseSlotRewards = new List<QuestWarehouseSlotRewardSaveData>();

            return _questWarehouseSlotRewards;
        }
    }

    // Purpose: Performs the to json workflow for this component.
    public string ToJson()
    {
        EnsureRuntimeDefaults();
        return JsonUtility.ToJson(this);
    }

    // Purpose: Loads from json into the active runtime state.
    public void LoadFromJson(string json)
    {
        JsonUtility.FromJsonOverwrite(json, this);
        EnsureRuntimeDefaults();
    }

    // Purpose: Ensures runtime defaults is ready before the workflow continues.
    public void EnsureRuntimeDefaults()
    {
        experience??=new();experience.total=Math.Max(0,experience.total);
        experience.completedQuestIds??=new();experience.settledRunIds??=new();questWarehouseItemReceipts??=new();
        QuestPassiveSlotRewards.RemoveAll(row => row == null || string.IsNullOrWhiteSpace(row.questId) || row.rewardIndex < 0 || row.slotAmount <= 0);
        _roguelikeCurrency = 0;
        MetaUpgrades.ClampNonNegative();
        InheritanceWarehouse.EnsureDefaults();
        HighestUnlockedStageIndex = _highestUnlockedStageIndex;
        for (int i = ShopUnlockProgress.Count - 1; i >= 0; i--)
        {
            ShopUnlockProgressSaveData entry = ShopUnlockProgress[i];
            if (entry == null || string.IsNullOrWhiteSpace(entry.configId))
            {
                ShopUnlockProgress.RemoveAt(i);
                continue;
            }

            entry.EnsureDefaults();
        }

        for (int i = QuestProgress.Count - 1; i >= 0; i--)
        {
            QuestProgressSaveData entry = QuestProgress[i];
            if (entry == null || string.IsNullOrWhiteSpace(entry.questId))
            {
                QuestProgress.RemoveAt(i);
                continue;
            }

            entry.EnsureDefaults();
        }

        HashSet<string> uniqueQuestRewardKeys = new HashSet<string>(StringComparer.Ordinal);
        for (int i = QuestStatRewards.Count - 1; i >= 0; i--)
        {
            QuestStatRewardSaveData entry = QuestStatRewards[i];
            string key = entry != null
                ? $"{entry.questId}:{entry.rewardIndex}"
                : string.Empty;

            if (entry == null ||
                string.IsNullOrWhiteSpace(entry.questId) ||
                entry.rewardIndex < 0 ||
                !Enum.IsDefined(typeof(PlayerStatId), entry.stat) ||
                !Enum.IsDefined(typeof(StatModifierType), entry.modifierType) ||
                float.IsNaN(entry.value) ||
                float.IsInfinity(entry.value) ||
                !uniqueQuestRewardKeys.Add(key))
            {
                QuestStatRewards.RemoveAt(i);
                continue;
            }

            entry.EnsureDefaults();
        }

        HashSet<string> uniqueWarehouseRewardKeys = new HashSet<string>(StringComparer.Ordinal);
        for (int i = QuestWarehouseSlotRewards.Count - 1; i >= 0; i--)
        {
            QuestWarehouseSlotRewardSaveData entry = QuestWarehouseSlotRewards[i];
            string key = entry != null
                ? $"{entry.questId}:{entry.rewardIndex}"
                : string.Empty;

            if (entry == null ||
                string.IsNullOrWhiteSpace(entry.questId) ||
                entry.rewardIndex < 0 ||
                entry.slotAmount <= 0 ||
                !uniqueWarehouseRewardKeys.Add(key))
            {
                QuestWarehouseSlotRewards.RemoveAt(i);
                continue;
            }

            entry.EnsureDefaults();
        }
    }
}

[Serializable]
public sealed class ShopUnlockProgressSaveData
{
    public string configId;
    public List<string> unlockedNodeIds = new List<string>();

    // Purpose: Ensures defaults is ready before the workflow continues.
    public void EnsureDefaults()
    {
        if (unlockedNodeIds == null)
            unlockedNodeIds = new List<string>();

        HashSet<string> uniqueIds = new HashSet<string>(StringComparer.Ordinal);
        for (int i = unlockedNodeIds.Count - 1; i >= 0; i--)
        {
            string nodeId = unlockedNodeIds[i];
            if (string.IsNullOrWhiteSpace(nodeId) || !uniqueIds.Add(nodeId))
                unlockedNodeIds.RemoveAt(i);
        }
    }
}

[Serializable]
public sealed class QuestProgressSaveData
{
    public string questId;
    public bool accepted;
    public bool completed;
    public int[] objectiveProgress = Array.Empty<int>();
    public bool[] rewardGranted = Array.Empty<bool>();

    // Purpose: Ensures defaults is ready before the workflow continues.
    public void EnsureDefaults()
    {
        if (objectiveProgress == null)
            objectiveProgress = Array.Empty<int>();

        if (rewardGranted == null)
            rewardGranted = Array.Empty<bool>();

        for (int i = 0; i < objectiveProgress.Length; i++)
            objectiveProgress[i] = Mathf.Max(0, objectiveProgress[i]);
    }
}

/// <summary>
/// Save-only representation of possessions and player condition that survive a
/// normal application exit.  This is intentionally separate from meta
/// progression: RunLifecycleCoordinator clears it before writing a death save.
/// </summary>
[Serializable]
public sealed class PlayerRunStateSaveData
{
    public bool hasSnapshot;
    [Tooltip("WIP legacy migration field. Physical MoneyItem stacks live in inventorySlots.")]
    public int runCurrency;
    public float currentHealth = -1f;
    public float currentInfection;
    public int activeWeaponSlotIndex;
    [Tooltip("Non-persistent Home-shop offer state for the active run. It resets on death with the rest of this snapshot.")]
    public int shopRunId;
    public List<PlayerRunShopStateSaveData> shopStates = new List<PlayerRunShopStateSaveData>();
    public List<PlayerRunInventorySlotSaveData> inventorySlots = new List<PlayerRunInventorySlotSaveData>();
    public List<PlayerRunInventorySlotSaveData> usableQuickSlots = new List<PlayerRunInventorySlotSaveData>();
    public List<PlayerRunWeaponSaveData> equippedWeapons = new List<PlayerRunWeaponSaveData>();
    public string equippedArmorGuid;
    public float equippedArmorDurability = -1f;
    public float equippedArmorMaxDurability = -1f;
    public string equippedBackpackGuid;
    public string equippedGasMaskGuid;
    public float equippedGasMaskFilterDurability = -1f;
    public float equippedGasMaskMaxFilterDurability = -1f;
    public List<PlayerRunKeyToolSlotSaveData> keyToolSlots = new List<PlayerRunKeyToolSlotSaveData>();
    public List<PlayerRunMagazineSaveData> weaponMagazines = new List<PlayerRunMagazineSaveData>();
    public int unlockedPassiveSlotCount = PlayerPassiveInventoryRuntime.MinimumUnlockedSlotCount;
    public List<string> equippedPassiveGuids = new List<string>();
    public int passiveSlotSchemaVersion;
    public List<PlayerRunStatusEffectSaveData> statusEffects = new List<PlayerRunStatusEffectSaveData>();

    public void Clear()
    {
        hasSnapshot = false;
        runCurrency = 0;
        currentHealth = -1f;
        currentInfection = 0f;
        activeWeaponSlotIndex = 0;
        shopRunId = 0;
        shopStates?.Clear();
        usableQuickSlots?.Clear();
        inventorySlots?.Clear();
        equippedWeapons?.Clear();
        equippedArmorGuid = string.Empty;
        equippedArmorDurability = -1f;
        equippedArmorMaxDurability = -1f;
        equippedBackpackGuid = string.Empty;
        equippedGasMaskGuid = string.Empty;
        equippedGasMaskFilterDurability = -1f;
        equippedGasMaskMaxFilterDurability = -1f;
        keyToolSlots?.Clear();
        weaponMagazines?.Clear();
        unlockedPassiveSlotCount = PlayerPassiveInventoryRuntime.MinimumUnlockedSlotCount;
        equippedPassiveGuids?.Clear();
        statusEffects?.Clear();
    }

    public void EnsureDefaults()
    {
        runCurrency = 0;
        currentHealth = currentHealth < 0f || float.IsNaN(currentHealth) || float.IsInfinity(currentHealth)
            ? -1f
            : Mathf.Max(0f, currentHealth);
        currentInfection = float.IsNaN(currentInfection) || float.IsInfinity(currentInfection)
            ? 0f
            : Mathf.Max(0f, currentInfection);
        activeWeaponSlotIndex = Mathf.Max(0, activeWeaponSlotIndex);
        shopRunId = Mathf.Max(0, shopRunId);
        equippedArmorDurability = NormalizeDurability(equippedArmorDurability);
        equippedArmorMaxDurability = NormalizeDurability(equippedArmorMaxDurability);
        equippedGasMaskFilterDurability = NormalizeDurability(equippedGasMaskFilterDurability);
        equippedGasMaskMaxFilterDurability = NormalizeDurability(equippedGasMaskMaxFilterDurability);
        unlockedPassiveSlotCount = Mathf.Clamp(
            unlockedPassiveSlotCount,
            PlayerPassiveInventoryRuntime.MinimumUnlockedSlotCount,
            PlayerPassiveInventoryRuntime.MaximumSlotCount);

        inventorySlots ??= new List<PlayerRunInventorySlotSaveData>();
        shopStates ??= new List<PlayerRunShopStateSaveData>();
        equippedWeapons ??= new List<PlayerRunWeaponSaveData>();
        keyToolSlots ??= new List<PlayerRunKeyToolSlotSaveData>();
        weaponMagazines ??= new List<PlayerRunMagazineSaveData>();
        equippedPassiveGuids ??= new List<string>();
        statusEffects ??= new List<PlayerRunStatusEffectSaveData>();
        usableQuickSlots ??= new List<PlayerRunInventorySlotSaveData>();
        foreach (var quickSlot in usableQuickSlots) quickSlot?.EnsureDefaults();

        RemoveInvalidInventorySlots();
        RemoveInvalidShopStates();
        RemoveInvalidWeaponSlots();
        RemoveInvalidKeySlots();
        RemoveInvalidMagazineSlots();
        RemoveInvalidPassiveSlots();
        RemoveInvalidStatusEffects();
    }

    private static float NormalizeDurability(float value)
    {
        return float.IsNaN(value) || float.IsInfinity(value)
            ? -1f
            : value < 0f ? -1f : value;
    }

    private void RemoveInvalidInventorySlots()
    {
        HashSet<int> occupied = new HashSet<int>();
        for (int index = inventorySlots.Count - 1; index >= 0; index--)
        {
            PlayerRunInventorySlotSaveData entry = inventorySlots[index];
            if (entry == null || entry.slotIndex < 0 || string.IsNullOrWhiteSpace(entry.itemGuid) ||
                entry.amount <= 0 || !occupied.Add(entry.slotIndex))
            {
                inventorySlots.RemoveAt(index);
                continue;
            }

            entry.EnsureDefaults();
        }
    }

    private void RemoveInvalidShopStates()
    {
        HashSet<string> ids = new HashSet<string>(StringComparer.Ordinal);
        for (int index = shopStates.Count - 1; index >= 0; index--)
        {
            PlayerRunShopStateSaveData state = shopStates[index];
            if (state == null || string.IsNullOrWhiteSpace(state.shopStateId) || !ids.Add(state.shopStateId))
            {
                shopStates.RemoveAt(index);
                continue;
            }

            state.EnsureDefaults();
        }
    }

    private void RemoveInvalidWeaponSlots()
    {
        HashSet<int> occupied = new HashSet<int>();
        for (int index = equippedWeapons.Count - 1; index >= 0; index--)
        {
            PlayerRunWeaponSaveData entry = equippedWeapons[index];
            if (entry == null || entry.slotIndex < 0 || string.IsNullOrWhiteSpace(entry.weaponGuid) ||
                entry.amount <= 0 || !occupied.Add(entry.slotIndex))
            {
                equippedWeapons.RemoveAt(index);
                continue;
            }

            entry.EnsureDefaults();
        }
    }

    private void RemoveInvalidKeySlots()
    {
        HashSet<int> occupied = new HashSet<int>();
        for (int index = keyToolSlots.Count - 1; index >= 0; index--)
        {
            PlayerRunKeyToolSlotSaveData entry = keyToolSlots[index];
            if (entry == null || entry.slotIndex < 0 || string.IsNullOrWhiteSpace(entry.keyGuid) ||
                entry.remainingUses <= 0 || !occupied.Add(entry.slotIndex))
            {
                keyToolSlots.RemoveAt(index);
                continue;
            }

            entry.EnsureDefaults();
        }
    }

    private void RemoveInvalidMagazineSlots()
    {
        HashSet<int> occupied = new HashSet<int>();
        for (int index = weaponMagazines.Count - 1; index >= 0; index--)
        {
            PlayerRunMagazineSaveData entry = weaponMagazines[index];
            if (entry == null || entry.weaponSlotIndex < 0 || string.IsNullOrWhiteSpace(entry.bulletGuid) ||
                entry.currentAmmo < 0 || !occupied.Add(entry.weaponSlotIndex))
            {
                weaponMagazines.RemoveAt(index);
                continue;
            }

            entry.EnsureDefaults();
        }
    }

    private void RemoveInvalidPassiveSlots()
    {
        while (equippedPassiveGuids.Count > PlayerPassiveInventoryRuntime.MaximumSlotCount)
            equippedPassiveGuids.RemoveAt(equippedPassiveGuids.Count - 1);

        for (int index = 0; index < equippedPassiveGuids.Count; index++)
            equippedPassiveGuids[index] = equippedPassiveGuids[index] ?? string.Empty;
    }

    private void RemoveInvalidStatusEffects()
    {
        HashSet<string> ids = new HashSet<string>(StringComparer.Ordinal);
        for (int index = statusEffects.Count - 1; index >= 0; index--)
        {
            PlayerRunStatusEffectSaveData entry = statusEffects[index];
            if (entry == null || string.IsNullOrWhiteSpace(entry.statusEffectId) ||
                entry.remainingSeconds <= 0f || !ids.Add(entry.statusEffectId))
            {
                statusEffects.RemoveAt(index);
                continue;
            }

            entry.EnsureDefaults();
        }
    }
}

/// <summary>
/// Save-only snapshot of one configured Home-shop station. Offer contents are
/// deterministic from the run seed, but remaining stock must survive an app
/// restart so the shop only resets on the next run loss.
/// </summary>
[Serializable]
public sealed class PlayerRunShopStateSaveData
{
    public int merchantMoney = -1;
    public string shopStateId;
    public bool offersRolled;
    public int restockRevision;
    public List<PlayerRunShopOfferSaveData> bulletOffers = new List<PlayerRunShopOfferSaveData>();
    public List<PlayerRunShopOfferSaveData> itemOffers = new List<PlayerRunShopOfferSaveData>();

    public void EnsureDefaults()
    {
        restockRevision = Mathf.Max(0, restockRevision);
        bulletOffers ??= new List<PlayerRunShopOfferSaveData>();
        itemOffers ??= new List<PlayerRunShopOfferSaveData>();
        RemoveInvalidOffers(bulletOffers);
        RemoveInvalidOffers(itemOffers);
    }

    private static void RemoveInvalidOffers(List<PlayerRunShopOfferSaveData> offers)
    {
        for (int index = offers.Count - 1; index >= 0; index--)
        {
            PlayerRunShopOfferSaveData offer = offers[index];
            if (offer == null || string.IsNullOrWhiteSpace(offer.itemGuid))
            {
                offers.RemoveAt(index);
                continue;
            }

            offer.EnsureDefaults();
        }
    }
}

[Serializable]
public sealed class PlayerRunShopOfferSaveData
{
    public string itemGuid;
    public int kind;
    public int rarity;
    public int price;
    public int initialStock;
    public int remainingStock;
    public int bundleAmount;
    public bool unlimitedStock;
    public bool canReroll;
    public List<PlayerRunShopPaymentRequirementSaveData> paymentRequirements = new List<PlayerRunShopPaymentRequirementSaveData>();

    public void EnsureDefaults()
    {
        price = Mathf.Max(0, price);
        initialStock = Mathf.Max(0, initialStock);
        remainingStock = Mathf.Clamp(remainingStock, 0, initialStock);
        bundleAmount = Mathf.Max(1, bundleAmount);
        paymentRequirements ??= new List<PlayerRunShopPaymentRequirementSaveData>();

        for (int index = 0; index < paymentRequirements.Count; index++)
            paymentRequirements[index]?.EnsureDefaults();
    }
}

/// <summary>
/// Save-only identity for one physical payment item on a Home-shop offer.
/// Empty rows intentionally remain invalid so restoring the offer cannot
/// silently turn malformed barter data into a legacy numerical transaction.
/// </summary>
[Serializable]
public sealed class PlayerRunShopPaymentRequirementSaveData
{
    public string itemGuid;
    public int amount;

    public void EnsureDefaults() => amount = Mathf.Max(1, amount);
}

[Serializable]
public sealed class PlayerRunInventorySlotSaveData
{
    public int slotIndex;
    public string itemGuid;
    public int amount;
    public float armorDurability = -1f;
    public float armorMaxDurability = -1f;
    public float weaponCurrentDurability = -1f;
    public float weaponMaxDurability = -1f;
    public float gasMaskFilterDurability = -1f;
    public float gasMaskMaxFilterDurability = -1f;

    public void EnsureDefaults()
    {
        amount = Mathf.Max(1, amount);
        armorDurability = Normalize(armorDurability);
        armorMaxDurability = Normalize(armorMaxDurability);
        weaponCurrentDurability = Normalize(weaponCurrentDurability);
        weaponMaxDurability = Normalize(weaponMaxDurability);
        gasMaskFilterDurability = Normalize(gasMaskFilterDurability);
        gasMaskMaxFilterDurability = Normalize(gasMaskMaxFilterDurability);
    }

    private static float Normalize(float value) =>
        float.IsNaN(value) || float.IsInfinity(value) ? -1f : value < 0f ? -1f : value;
}

[Serializable]
public sealed class PlayerRunWeaponSaveData
{
    public bool jammed;
    public int slotIndex;
    public string weaponGuid;
    public int amount;
    public float currentDurability = -1f;
    public float maxDurability = -1f;

    public void EnsureDefaults()
    {
        amount = Mathf.Max(1, amount);
        currentDurability = Normalize(currentDurability);
        maxDurability = Normalize(maxDurability);
    }

    private static float Normalize(float value) =>
        float.IsNaN(value) || float.IsInfinity(value) ? -1f : value < 0f ? -1f : value;
}

[Serializable]
public sealed class PlayerRunKeyToolSlotSaveData
{
    public int slotIndex;
    public string keyGuid;
    public int remainingUses;

    public void EnsureDefaults() => remainingUses = Mathf.Max(0, remainingUses);
}

[Serializable]
public sealed class PlayerRunMagazineSaveData
{
    public int weaponSlotIndex;
    public string bulletGuid;
    public int currentAmmo;

    public void EnsureDefaults() => currentAmmo = Mathf.Max(0, currentAmmo);
}

[Serializable]
public sealed class PlayerRunStatusEffectSaveData
{
    public string statusEffectId;
    public float remainingSeconds;
    public int stacks = 1;
    public float tickRemaining = 1f;

    public void EnsureDefaults()
    {
        stacks = Mathf.Max(1, stacks);
        tickRemaining = float.IsNaN(tickRemaining) || float.IsInfinity(tickRemaining) ? 1f : Mathf.Max(.001f, tickRemaining);
        remainingSeconds = float.IsNaN(remainingSeconds) || float.IsInfinity(remainingSeconds)
            ? 0f
            : Mathf.Max(0f, remainingSeconds);
    }
}

[Serializable]
public sealed class QuestStatRewardSaveData
{
    public string questId;
    public int rewardIndex;
    public PlayerStatId stat;
    public StatModifierType modifierType;
    public float value;

    // Purpose: Ensures defaults is ready before the workflow continues.
    public void EnsureDefaults()
    {
        rewardIndex = Mathf.Max(0, rewardIndex);
    }
}

[Serializable]
public sealed class QuestWarehouseSlotRewardSaveData
{
    public string questId;
    public int rewardIndex;
    public int slotAmount;

    // Purpose: Normalizes the persistent one-time warehouse reward record.
    public void EnsureDefaults()
    {
        rewardIndex = Mathf.Max(0, rewardIndex);
        slotAmount = Mathf.Max(1, slotAmount);
    }
}

[Serializable]
public class InheritanceWarehouseSaveData
{
    [Min(1)] public int capacity = 24;
    public List<InheritanceWarehouseItemSaveData> items =
        new List<InheritanceWarehouseItemSaveData>();

    // Purpose: Ensures defaults is ready before the workflow continues.
    public void EnsureDefaults()
    {
        capacity = Mathf.Max(1, capacity);

        if (items == null)
            items = new List<InheritanceWarehouseItemSaveData>();

        HashSet<int> occupiedSlots = new HashSet<int>();

        for (int i = items.Count - 1; i >= 0; i--)
        {
            InheritanceWarehouseItemSaveData entry = items[i];

            if (entry == null ||
                entry.slotIndex < 0 ||
                entry.slotIndex >= capacity ||
                string.IsNullOrWhiteSpace(entry.itemGuid) ||
                entry.amount <= 0 ||
                !occupiedSlots.Add(entry.slotIndex))
            {
                items.RemoveAt(i);
                continue;
            }

            entry.amount = Mathf.Max(1, entry.amount);
        }
    }
}

[Serializable]
public class InheritanceWarehouseItemSaveData
{
    public int slotIndex;
    public string itemGuid;
    public int amount;
    public float armorDurability = -1f;
    public float armorMaxDurability = -1f;
    public float weaponCurrentDurability = -1f;
    public float weaponMaxDurability = -1f;
    public float gasMaskFilterDurability = -1f;
    public float gasMaskMaxFilterDurability = -1f;
}

[Serializable]
public class MetaUpgradeSaveData
{
    public int maxHealthLevel;
    public int attackDamageLevel;
    public int magazineSizeLevel;
    public int moveSpeedLevel;
    public int reloadDurationLevel;
    public int attackSpeedLevel;
    public int paintSpeedLevel;

    // Purpose: Returns the current level.
    public int GetLevel(MetaUpgradeId id)
    {
        return id switch
        {
            MetaUpgradeId.MaxHealth => maxHealthLevel,
            MetaUpgradeId.AttackDamage => attackDamageLevel,
            MetaUpgradeId.MagazineSize => magazineSizeLevel,
            MetaUpgradeId.MoveSpeed => moveSpeedLevel,
            MetaUpgradeId.ReloadDuration => reloadDurationLevel,
            MetaUpgradeId.AttackSpeed => attackSpeedLevel,
            MetaUpgradeId.PaintSpeed => paintSpeedLevel,
            _ => 0
        };
    }

    // Purpose: Updates the level state.
    public void SetLevel(MetaUpgradeId id, int level)
    {
        level = Mathf.Max(0, level);

        switch (id)
        {
            case MetaUpgradeId.MaxHealth:
                maxHealthLevel = level;
                break;

            case MetaUpgradeId.AttackDamage:
                attackDamageLevel = level;
                break;

            case MetaUpgradeId.MagazineSize:
                magazineSizeLevel = level;
                break;

            case MetaUpgradeId.MoveSpeed:
                moveSpeedLevel = level;
                break;

            case MetaUpgradeId.ReloadDuration:
                reloadDurationLevel = level;
                break;

            case MetaUpgradeId.AttackSpeed:
                attackSpeedLevel = level;
                break;

            case MetaUpgradeId.PaintSpeed:
                paintSpeedLevel = level;
                break;
        }
    }

    // Purpose: Performs the increase level workflow for this component.
    public void IncreaseLevel(MetaUpgradeId id)
    {
        SetLevel(id, GetLevel(id) + 1);
    }

    // Purpose: Performs the clamp non negative workflow for this component.
    public void ClampNonNegative()
    {
        maxHealthLevel = Mathf.Max(0, maxHealthLevel);
        attackDamageLevel = Mathf.Max(0, attackDamageLevel);
        magazineSizeLevel = Mathf.Max(0, magazineSizeLevel);
        moveSpeedLevel = Mathf.Max(0, moveSpeedLevel);
        reloadDurationLevel = Mathf.Max(0, reloadDurationLevel);
        attackSpeedLevel = Mathf.Max(0, attackSpeedLevel);
        paintSpeedLevel = Mathf.Max(0, paintSpeedLevel);
    }
}

/// <summary>
/// Device-local preferences. These intentionally survive a New Game and are
/// not part of a character's permanent progression profile.
/// </summary>
[Serializable]
public sealed class DeviceSettingsSaveData
{
    public const int CurrentSchemaVersion = 1;

    public int schemaVersion = CurrentSchemaVersion;
    public float masterVolume = 1f;
    public float musicVolume = 1f;
    public float sfxVolume = 1f;
    public int resolutionIndex;
    public bool isFullscreen = true;
    public float aimSensitivity = 1f;
    public bool useVSync = true;
    public int targetFrameRate = 60;
    public int antiAliasingIndex;
    public float shadowDistance = 50f;
    [Tooltip("Locale identifier such as ko or en. Never persist a Locale ScriptableObject reference.")]
    public string localeCode = string.Empty;
    [TextArea] public string inputBindingOverridesJson = string.Empty;

    public void EnsureDefaults()
    {
        masterVolume = Mathf.Clamp01(masterVolume);
        musicVolume = Mathf.Clamp01(musicVolume);
        sfxVolume = Mathf.Clamp01(sfxVolume);
        resolutionIndex = Mathf.Max(0, resolutionIndex);
        aimSensitivity = Mathf.Max(0.01f, aimSensitivity);
        targetFrameRate = Mathf.Max(1, targetFrameRate);
        antiAliasingIndex = Mathf.Max(0, antiAliasingIndex);
        shadowDistance = Mathf.Max(0f, shadowDistance);
        localeCode ??= string.Empty;
        inputBindingOverridesJson ??= string.Empty;
    }
}

/// <summary>
/// Save-only state which may be discarded on a run loss without touching
/// permanent profile progression or device-local preferences.
/// </summary>
[Serializable]
public sealed class RunSaveData
{
    public StartingPassivePreparationSaveData startingPassives = new();
    public RunExperienceSaveData experience = new();
    public InfectionExposureSaveData infectionExposure = new();
    public List<RecyclingSlotSaveData> recyclingSlots = new();
    public List<PassiveInstanceSaveData> passiveInstances = new();
    public const int CurrentSchemaVersion = 2;

    public int schemaVersion = CurrentSchemaVersion;
    public PlayerRunStateSaveData playerRunState = new PlayerRunStateSaveData();
    public RaidRunSaveData raid = new RaidRunSaveData();

    public PlayerRunStateSaveData PlayerRunState
    {
        get
        {
            playerRunState ??= new PlayerRunStateSaveData();
            return playerRunState;
        }
    }

    public void EnsureDefaults()
    {
        PlayerRunState.EnsureDefaults();
        raid ??= new RaidRunSaveData();
        experience??=new();experience.pending=Mathf.Max(0,experience.pending);experience.discoveries??=new();
        infectionExposure??=new();recyclingSlots??=new();
    }
}

/// <summary>
/// Wire-compatible shape of the former board_chase_progress.es3 JSON payload.
/// It exists only to migrate old installations; new writes use the three
/// dedicated settings/profile/run files.
/// </summary>
[Serializable]
public sealed class LegacySaveData
{
    public float _masterVolume = 1f;
    public float _musicVolume = 1f;
    public float _sfxVolume = 1f;
    public int _resolutionsIndex;
    public bool _isFullscreen = true;
    public float _aimSensitivity = 1f;
    public bool _useVSync = true;
    public int _targetFrameRate = 60;
    public int _antiAliasingIndex;
    public float _shadowDistance = 50f;
    public Locale _currentLocale;

    public int _roguelikeCurrency;
    public MetaUpgradeSaveData _metaUpgrades = new MetaUpgradeSaveData();
    public InheritanceWarehouseSaveData _inheritanceWarehouse = new InheritanceWarehouseSaveData();
    public int _highestUnlockedStageIndex = -1;
    public List<ShopUnlockProgressSaveData> _shopUnlockProgress = new List<ShopUnlockProgressSaveData>();
    public List<QuestProgressSaveData> _questProgress = new List<QuestProgressSaveData>();
    public List<QuestStatRewardSaveData> _questStatRewards = new List<QuestStatRewardSaveData>();
    public List<QuestWarehouseSlotRewardSaveData> _questWarehouseSlotRewards =
        new List<QuestWarehouseSlotRewardSaveData>();
    public PlayerRunStateSaveData _playerRunState = new PlayerRunStateSaveData();

    public Save ToProfileData()
    {
        return new Save
        {
            schemaVersion = 0,
            _roguelikeCurrency = _roguelikeCurrency,
            _metaUpgrades = _metaUpgrades,
            _inheritanceWarehouse = _inheritanceWarehouse,
            _highestUnlockedStageIndex = _highestUnlockedStageIndex,
            _shopUnlockProgress = _shopUnlockProgress,
            _questProgress = _questProgress,
            _questStatRewards = _questStatRewards,
            _questWarehouseSlotRewards = _questWarehouseSlotRewards
        };
    }

    public RunSaveData ToRunData()
    {
        return new RunSaveData { schemaVersion = 0, playerRunState = _playerRunState };
    }

    public DeviceSettingsSaveData ToDeviceSettingsData()
    {
        return new DeviceSettingsSaveData
        {
            schemaVersion = 0,
            masterVolume = _masterVolume,
            musicVolume = _musicVolume,
            sfxVolume = _sfxVolume,
            resolutionIndex = _resolutionsIndex,
            isFullscreen = _isFullscreen,
            aimSensitivity = _aimSensitivity,
            useVSync = _useVSync,
            targetFrameRate = _targetFrameRate,
            antiAliasingIndex = _antiAliasingIndex,
            shadowDistance = _shadowDistance,
            localeCode = _currentLocale != null ? _currentLocale.Identifier.Code : string.Empty
        };
    }
}

public static class SaveMigration
{
    public static bool TryMigrateProfile(Save profile, out string error)
    {
        if (profile == null)
        {
            error = "Profile payload was null.";
            return false;
        }

        if (!TryGetSupportedVersion(profile.schemaVersion, Save.CurrentSchemaVersion, "profile", out int version, out error))
            return false;

        while (version < Save.CurrentSchemaVersion)
        {
            if (version != 0)
            {
                error = $"No migration is registered for profile schema {version}.";
                return false;
            }

            // Version 0 used the same permanent fields inside the monolithic payload.
            version = 1;
        }

        profile.schemaVersion = Save.CurrentSchemaVersion;
        profile.EnsureRuntimeDefaults();
        error = string.Empty;
        return true;
    }

    public static bool TryMigrateRun(RunSaveData run, out string error)
    {
        if (run == null)
        {
            error = "Run payload was null.";
            return false;
        }

        if (!TryGetSupportedVersion(run.schemaVersion, RunSaveData.CurrentSchemaVersion, "run", out int version, out error))
            return false;

        while (version < RunSaveData.CurrentSchemaVersion)
        {
            if (version == 0)
            {
                version = 1;
                continue;
            }
            if (version == 1)
            {
                // Older run saves have inventory but no resumable map. Do not invent a map.
                run.raid ??= new RaidRunSaveData();
                version = 2;
                continue;
            }
            else
            {
                error = $"No migration is registered for run schema {version}.";
                return false;
            }
        }

        run.schemaVersion = RunSaveData.CurrentSchemaVersion;
        run.EnsureDefaults();
        if (!run.raid.TryValidate(out error)) return false;
        error = string.Empty;
        return true;
    }

    public static bool TryMigrateSettings(DeviceSettingsSaveData settings, out string error)
    {
        if (settings == null)
        {
            error = "Settings payload was null.";
            return false;
        }

        if (!TryGetSupportedVersion(
                settings.schemaVersion,
                DeviceSettingsSaveData.CurrentSchemaVersion,
                "settings",
                out int version,
                out error))
        {
            return false;
        }

        while (version < DeviceSettingsSaveData.CurrentSchemaVersion)
        {
            if (version != 0)
            {
                error = $"No migration is registered for settings schema {version}.";
                return false;
            }

            version = 1;
        }

        settings.schemaVersion = DeviceSettingsSaveData.CurrentSchemaVersion;
        settings.EnsureDefaults();
        error = string.Empty;
        return true;
    }

    private static bool TryGetSupportedVersion(
        int sourceVersion,
        int currentVersion,
        string partitionName,
        out int normalizedVersion,
        out string error)
    {
        normalizedVersion = Math.Max(0, sourceVersion);
        if (normalizedVersion <= currentVersion)
        {
            error = string.Empty;
            return true;
        }

        error =
            $"The {partitionName} save uses schema {normalizedVersion}, but this build supports only up to {currentVersion}.";
        return false;
    }
}
