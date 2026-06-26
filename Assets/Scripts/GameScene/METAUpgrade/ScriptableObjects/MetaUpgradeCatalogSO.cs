using System;
using UnityEngine;

[CreateAssetMenu(
    fileName = "MetaUpgradeCatalog",
    menuName = "Game/Meta/Upgrade Catalog")]
public class MetaUpgradeCatalogSO : ScriptableObject
{
    [Header("Meta Upgrades")]
    [SerializeField] private MetaUpgradeDefinition[] _upgrades =
    {
        new MetaUpgradeDefinition
        {
            id = MetaUpgradeId.MaxHealth,
            displayName = "Max Health",
            description = "Permanently increases player max health.",
            statId = PlayerStatId.MaxHealth,
            modifierType = StatModifierType.FlatAdd,
            valueFormat = "+{0:0}",
            effectPerLevel = new float[] { 5f, 5f, 5f, 10f, 10f, 15f },
            costPerLevel = new int[] { 50, 50, 50, 100, 100, 200 }
        },
        new MetaUpgradeDefinition
        {
            id = MetaUpgradeId.AttackDamage,
            displayName = "Attack Damage",
            description = "Permanently increases player attack damage.",
            statId = PlayerStatId.AttackDamage,
            modifierType = StatModifierType.FlatAdd,
            valueFormat = "+{0:0}",
            effectPerLevel = new float[] { 1f, 1f, 1f, 2f, 2f, 3f },
            costPerLevel = new int[] { 50, 50, 50, 100, 100, 200 }
        },
        new MetaUpgradeDefinition
        {
            id = MetaUpgradeId.MagazineSize,
            displayName = "Magazine Size",
            description = "Permanently increases magazine size.",
            statId = PlayerStatId.MagazineSize,
            modifierType = StatModifierType.FlatAdd,
            valueFormat = "+{0:0}",
            effectPerLevel = new float[] { 1f, 1f, 1f },
            costPerLevel = new int[] { 300, 300, 300 }
        },
        new MetaUpgradeDefinition
        {
            id = MetaUpgradeId.MoveSpeed,
            displayName = "Move Speed",
            description = "Permanently increases player move speed.",
            statId = PlayerStatId.MoveSpeed,
            modifierType = StatModifierType.FlatAdd,
            valueFormat = "+{0:0.##}",
            effectPerLevel = new float[] { 1f, 1f, 1f, 2f },
            costPerLevel = new int[] { 150, 150, 150, 300 }
        },
        new MetaUpgradeDefinition
        {
            id = MetaUpgradeId.ReloadDuration,
            displayName = "Reload Time",
            description = "Permanently reduces reload duration.",
            statId = PlayerStatId.ReloadDurationSeconds,
            modifierType = StatModifierType.FlatAdd,
            valueFormat = "{0:0.##}s",
            effectPerLevel = new float[] { -0.1f, -0.1f, -0.1f, -0.2f },
            costPerLevel = new int[] { 100, 100, 100, 200 }
        },
        new MetaUpgradeDefinition
        {
            id = MetaUpgradeId.AttackSpeed,
            displayName = "Shot Speed",
            description = "Permanently increases shots per second.",
            statId = PlayerStatId.ShotsPerSecond,
            modifierType = StatModifierType.FlatAdd,
            valueFormat = "+{0:0.##}/s",
            effectPerLevel = new float[] { 0.1f, 0.10f, 0.10f, 0.20f },
            costPerLevel = new int[] { 100, 100, 100, 200 }
        }
    };

    public int UpgradeCount => _upgrades != null ? _upgrades.Length : 0;

    public MetaUpgradeDefinition GetUpgradeAt(int index)
    {
        if (_upgrades == null || index < 0 || index >= _upgrades.Length)
            return null;

        return _upgrades[index];
    }

    public bool TryGetUpgrade(MetaUpgradeId id, out MetaUpgradeDefinition upgrade)
    {
        upgrade = null;

        if (_upgrades == null)
            return false;

        for (int i = 0; i < _upgrades.Length; i++)
        {
            MetaUpgradeDefinition candidate = _upgrades[i];

            if (candidate != null && candidate.id == id)
            {
                upgrade = candidate;
                return true;
            }
        }

        return false;
    }

    public MetaProgressSnapshot BuildSnapshot(MetaUpgradeSaveData saveData, int currency)
    {
        currency = Mathf.Max(0, currency);

        MetaProgressSnapshot snapshot = new MetaProgressSnapshot
        {
            currency = currency,
            upgrades = new MetaUpgradeSnapshot[UpgradeCount]
        };

        for (int i = 0; i < UpgradeCount; i++)
        {
            MetaUpgradeDefinition upgrade = _upgrades[i];

            if (upgrade == null)
                continue;

            int currentLevel = saveData != null ? saveData.GetLevel(upgrade.id) : 0;
            currentLevel = Mathf.Clamp(currentLevel, 0, upgrade.MaxLevel);

            int nextCost = upgrade.GetCostForNextLevel(currentLevel);
            bool isMaxLevel = currentLevel >= upgrade.MaxLevel;

            snapshot.upgrades[i] = new MetaUpgradeSnapshot
            {
                id = upgrade.id,
                displayName = upgrade.DisplayName,
                description = upgrade.description,
                icon = upgrade.icon,
                currentLevel = currentLevel,
                maxLevel = upgrade.MaxLevel,
                nextCost = nextCost,
                currency = currency,
                totalBonus = upgrade.GetTotalEffect(currentLevel),
                nextLevelBonus = upgrade.GetEffectForNextLevel(currentLevel),
                valueFormat = upgrade.ValueFormat,
                isMaxLevel = isMaxLevel,
                canAfford = !isMaxLevel && currency >= nextCost,
                canUpgrade = !isMaxLevel && currency >= nextCost
            };
        }

        return snapshot;
    }

    public void ApplyMetaModifiers(
        MetaUpgradeSaveData saveData,
        Action<PlayerStatModifier> applyModifier)
    {
        if (saveData == null || applyModifier == null || _upgrades == null)
            return;

        for (int i = 0; i < _upgrades.Length; i++)
        {
            MetaUpgradeDefinition upgrade = _upgrades[i];

            if (upgrade == null)
                continue;

            int level = Mathf.Clamp(saveData.GetLevel(upgrade.id), 0, upgrade.MaxLevel);

            if (upgrade.TryCreateModifier(level, out PlayerStatModifier modifier))
                applyModifier.Invoke(modifier);
        }
    }
}

[Serializable]
public class MetaUpgradeDefinition
{
    public MetaUpgradeId id;
    public string displayName;
    [TextArea] public string description;
    public Sprite icon;

    [Header("Stat Effect")]
    public PlayerStatId statId;
    public StatModifierType modifierType = StatModifierType.FlatAdd;
    public string valueFormat = "+{0:0.##}";
    public float[] effectPerLevel;

    [Header("Cost")]
    public int[] costPerLevel;

    public string DisplayName =>
        string.IsNullOrWhiteSpace(displayName) ? id.ToString() : displayName;

    public string ValueFormat =>
        string.IsNullOrWhiteSpace(valueFormat) ? "{0:0.##}" : valueFormat;

    public int MaxLevel => effectPerLevel != null ? effectPerLevel.Length : 0;

    public int GetCostForNextLevel(int currentLevel)
    {
        if (currentLevel < 0 || currentLevel >= MaxLevel)
            return 0;

        if (costPerLevel == null || currentLevel >= costPerLevel.Length)
            return 0;

        return Mathf.Max(0, costPerLevel[currentLevel]);
    }

    public float GetEffectForNextLevel(int currentLevel)
    {
        if (effectPerLevel == null || currentLevel < 0 || currentLevel >= effectPerLevel.Length)
            return 0f;

        return effectPerLevel[currentLevel];
    }

    public float GetTotalEffect(int currentLevel)
    {
        if (effectPerLevel == null || currentLevel <= 0)
            return 0f;

        int count = Mathf.Min(currentLevel, effectPerLevel.Length);
        float total = 0f;

        for (int i = 0; i < count; i++)
            total += effectPerLevel[i];

        return total;
    }

    public bool TryCreateModifier(int currentLevel, out PlayerStatModifier modifier)
    {
        modifier = default;

        float total = GetTotalEffect(currentLevel);

        if (Mathf.Approximately(total, 0f))
            return false;

        modifier = new PlayerStatModifier
        {
            stat = statId,
            type = modifierType,
            value = total
        };

        return true;
    }
}
