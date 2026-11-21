/*
 * Role hierarchy: Gameplay > Entities > Shooter > Common > Core
 * Primary responsibility: Implements the focused PlayerStatId responsibility and collaborates with its role group.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using System;
using UnityEngine;

public enum PlayerStatId
{
    AttackDamage = 0,
   // NamedBossDamageMultiplier,

    MaxRange = 1,
    ShotsPerSecond = 2,
    MagazineSize = 4,

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
    ArmorInfectionDurabilityLossMultiplier = 36,
    MeleeDamage = 37,
    ShotKickDistance = 38,
    MaximumShotKickDistance = 39,
    ShotKickRecoveryPerSecond = 40,
    AimingMoveSpeedMultiplier = 41,
    GasInfectionResistance = 42,
    InfectionDefense = 43,
    InventorySlots = 44,
    MaximumStamina = 45,
    VisionArcBonus = 46,
    AimPunchReduction = 47,
    SuppressionDurationBonus = 48,
    AmmoPreservationChance = 49,
    ExperienceGainBonus = 50, // Retired save enum.
    CarryCapacityKg = 51
}

public enum PlayerFeatureFlagId
{
    LeaveVaccineOnEnemyKill,
    PaintBulletLeavesTrail,
}

/// <summary>Keep serialized enum numbers stable; retired/weapon-owned stats never enter character calculations.</summary>
public static class ShooterStatPolicy
{
    public static bool IsCharacterStat(PlayerStatId stat) => stat switch
    {
        PlayerStatId.CarryCapacityKg or PlayerStatId.InfectionDefense or PlayerStatId.InventorySlots or PlayerStatId.MaximumStamina or
        PlayerStatId.VisionArcBonus or PlayerStatId.AimPunchReduction or PlayerStatId.SuppressionDurationBonus or PlayerStatId.AmmoPreservationChance => true,
        PlayerStatId.AttackDamage or PlayerStatId.MaxRange or PlayerStatId.ShotsPerSecond or
        PlayerStatId.MoveSpeed or PlayerStatId.VisionRange or PlayerStatId.MaxHealth or
        PlayerStatId.DashDistance or PlayerStatId.DashCooldownSeconds or PlayerStatId.ReloadDurationSeconds or
        PlayerStatId.DodgeChance or PlayerStatId.AimSpeed or PlayerStatId.HipFireSpreadAngleDeg or
        PlayerStatId.AimSpreadAngleDeg or PlayerStatId.GunshotSoundRadius or
        PlayerStatId.SoundInvestigateDelaySeconds or PlayerStatId.FootstepSoundRadius or
        PlayerStatId.FootstepSoundInterval or PlayerStatId.ArmorClass or PlayerStatId.PenetrationClass or
        PlayerStatId.ArmorHealthDurabilityLossMultiplier or PlayerStatId.ArmorInfectionDurabilityLossMultiplier or
        PlayerStatId.ShotKickDistance or PlayerStatId.MaximumShotKickDistance or
        PlayerStatId.ShotKickRecoveryPerSecond or PlayerStatId.AimingMoveSpeedMultiplier or PlayerStatId.GasInfectionResistance => true,
        _ => false
    };

    // Deliberately smaller than the engine's stat set. Utility/technical tuning stays out of passive authoring.
    public static bool IsPassiveStat(PlayerStatId stat) => stat switch
    {
        PlayerStatId.InfectionDefense or PlayerStatId.GasInfectionResistance => false, // Protection is equipment-owned.
        PlayerStatId.InventorySlots or PlayerStatId.MaximumStamina or
        PlayerStatId.VisionArcBonus or PlayerStatId.AimPunchReduction or PlayerStatId.SuppressionDurationBonus or PlayerStatId.AmmoPreservationChance or
        PlayerStatId.ArmorHealthDurabilityLossMultiplier => true,
        PlayerStatId.AttackDamage or PlayerStatId.MaxRange or PlayerStatId.DodgeChance or
        PlayerStatId.MoveSpeed or PlayerStatId.MaxHealth or
        PlayerStatId.DashDistance or PlayerStatId.DashCooldownSeconds or PlayerStatId.ReloadDurationSeconds or
        PlayerStatId.AimSpeed or PlayerStatId.HipFireSpreadAngleDeg or PlayerStatId.AimSpreadAngleDeg or
        PlayerStatId.VisionRange or PlayerStatId.PenetrationClass or PlayerStatId.ArmorClass or
        PlayerStatId.ShotsPerSecond or PlayerStatId.ShotKickDistance or PlayerStatId.MaximumShotKickDistance or
        PlayerStatId.ShotKickRecoveryPerSecond => true,
        _ => false
    };
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

