using System;
using UnityEngine;

public enum PlayerUpgradeTrack
{
    Removal,
    Occupation,
    Control
}

public enum PlayerStatId
{
    AttackDamage,
    NamedBossDamageMultiplier,
    ShotsPerSecond,
    ReloadSpeedMultiplier,
    MagazineSize,

    PaintRadius,
    OccupationWinThreshold,

    MoveSpeed,
    MaxHealth,
    DashDistanceMultiplier,

    ShockwaveCooldownSeconds,
    VaccineZoneInfectionRecoveryMultiplier
}

public enum PlayerFeatureFlagId
{
    LeaveVaccineOnEnemyKill,
    PaintBulletLeavesTrail,
    ShockwavePaintsVaccine
}

public enum StatModifierType
{
    FlatAdd,
    PercentAdd,
    Override
}

[Serializable]
public struct PlayerStatModifier
{
    public PlayerStatId stat;
    public StatModifierType type;

    [Tooltip("PercentAdd는 15%면 0.15로 입력. FlatAdd는 +25, -3 같은 실제값.")]
    public float value;
}

[Serializable]
public struct PlayerFeatureFlagModifier
{
    public PlayerFeatureFlagId flag;
    public bool enabled;
}

[Serializable]
public class PlayerUpgradeDefinition
{
    [Min(1)] public int level = 1;
    [Min(0)] public int cost = 1;
    public Sprite icon;
    public string displayName;
    [TextArea] public string description;
    public bool lockedInBeta;

    public PlayerStatModifier[] statModifiers;
    public PlayerFeatureFlagModifier[] featureFlags;
}

[Serializable]
public class PlayerUpgradeTrackDefinition
{
    public PlayerUpgradeTrack track;
    public PlayerUpgradeDefinition[] upgrades;
}

[Serializable]
public class BossUpgradeDefinition
{
    public string id;
    public string displayName;
    [TextArea] public string description;

    public PlayerStatModifier[] normalModifiers;
    public PlayerStatModifier[] infectedModifiers;
}

[Serializable]
public struct BossUpgradePickRecord
{
    public string bossUpgradeId;
    public bool infected;
}
