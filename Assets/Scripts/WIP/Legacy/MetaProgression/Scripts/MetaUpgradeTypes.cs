// WIP legacy compatibility only. Active growth uses quest rewards and experience, not this system.
/*
 * Role hierarchy: Gameplay > Progression > Meta
 * Primary responsibility: Implements the focused MetaUpgradeId responsibility and collaborates with its role group.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using System;
using UnityEngine;

public enum MetaUpgradeId
{
    MaxHealth,
    AttackDamage,
    MagazineSize,
    MoveSpeed,
    ReloadDuration,
    AttackSpeed,
    PaintSpeed
}

[Serializable]
public struct MetaUpgradeSnapshot
{
    public MetaUpgradeId id;
    public string displayName;
    public string description;
    public Sprite icon;

    public int currentLevel;
    public int maxLevel;
    public int nextCost;
    public int currency;

    public float totalBonus;
    public float nextLevelBonus;
    public string valueFormat;

    public bool isMaxLevel;
    public bool canAfford;
    public bool canUpgrade;
}

[Serializable]
public struct MetaProgressSnapshot
{
    public int currency;
    public MetaUpgradeSnapshot[] upgrades;
}
