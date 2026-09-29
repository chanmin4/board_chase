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
    MeleeDamage = 37
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

