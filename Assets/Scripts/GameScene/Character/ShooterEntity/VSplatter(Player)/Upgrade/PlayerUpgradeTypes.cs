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
    AttackDamage = 0,
   // NamedBossDamageMultiplier,

    MaxRange = 1,
    ShotsPerSecond = 2,
    MagazineSize = 4,

    PaintRadius = 5,

    MoveSpeed = 6,
    VisionRange = 7,
    MaxHealth = 8,
    DashDistance = 9,
    DashCooldownSeconds = 10,
   // VaccineZoneInfectionRecoveryMultiplier,

    ReloadDurationSeconds = 12,
    DodgeChance = 13,

    AimSpeed = 14,
    AimRangeMultiplier = 15,
    AimMoveSpeedMultiplier = 16,
    HipFireSpreadAngleDeg = 17,
    AimSpreadAngleDeg = 18,
    RecoilAngleDeg = 19,
    RecoilRecoverySpeedDegPerSecond = 20,
    GunshotSoundRadius = 21,
    SoundInvestigateDelaySeconds = 22,
    FootstepSoundRadius = 23,
    FootstepSoundInterval = 24,
    PaintMarkDamage = 25,
    InfectionDamage = 26,
    RecoilForwardDistancePerShot = 27,
    RecoilSideDistancePerShot = 28,
    MaxRecoilDistance = 29,
    RecoilDistanceRecoveryPerSecond = 30,
    HipFireSpreadRadius = 31,
    AimSpreadRadius = 32,
    ArmorClass = 33,
    PenetrationClass = 34,
    ArmorHealthDurabilityLossMultiplier = 35,
    ArmorInfectionDurabilityLossMultiplier = 36
}

public enum PlayerFeatureFlagId
{
    LeaveVaccineOnEnemyKill,
    PaintBulletLeavesTrail,
}

public enum StatModifierType
{
    FlatAdd,
    PercentAdd,
    PercentMultiply,
    Override

}

[Serializable]
public struct PlayerStatModifier
{
    public PlayerStatId stat;
    public StatModifierType type;

    [Tooltip("PercentAdd uses direct percent values. -20 means -20%, 15 means +15%. FlatAdd uses raw values.")]
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
