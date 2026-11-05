/*
 * Role hierarchy: Gameplay > Entities > Shooter > Player > Upgrade
 * Primary responsibility: Implements the focused PlayerWeaponStats responsibility and collaborates with its role group.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using System;
using UnityEngine;

[Serializable]
public struct PlayerWeaponStats
{
    public float attackDamage;
    public float meleeDamage;
    [HideInInspector] public float namedBossDamageMultiplier; // Legacy snapshot data only.

    public float maxRange;

    public float shotsPerSecond;

    public float reloadDurationSeconds;
    public int magazineSize;
    [HideInInspector] public float paintMarkDamage;
    [HideInInspector] public float infectionDamage;
    public float penetrationClass;
}


[Serializable]
public struct PlayerPaintStats
{
    [HideInInspector] public float occupationWinThreshold;
}

[Serializable]
public struct PlayerMovementStats
{
    public float moveSpeed;
    public float dashCooldownSeconds;
    public float dashDistanceMultiplier;
}

[Serializable]
public struct PlayerSurvivalStats
{
    public float maxHealth;
    public float dodgeChance;
    public float armorClass;
    public float armorHealthDurabilityLossMultiplier;
    public float armorInfectionDurabilityLossMultiplier;
}

[Serializable]
public struct PlayerVisionStats
{
    public float visionRange;
}

[Serializable]
public struct PlayerAimStats
{
    public float aimSpeed;
    public float aimingMoveSpeedMultiplier;
    public float shotKickDistance;
    public float maximumShotKickDistance;
    public float shotKickRecoveryPerSecond;
    [HideInInspector] public float aimRangeMultiplier;
    [HideInInspector] public float aimMoveSpeedMultiplier;
    public float hipFireSpreadAngleDeg;
    public float aimSpreadAngleDeg;
    [HideInInspector] public float recoilAngleDeg;
    [HideInInspector] public float recoilRecoverySpeedDegPerSecond;
    [HideInInspector] public float recoilForwardDistancePerShot;
    [HideInInspector] public float recoilSideDistancePerShot;
    [HideInInspector] public float maxRecoilDistance;
    [HideInInspector] public float recoilDistanceRecoveryPerSecond;
    [HideInInspector] public float hipFireSpreadRadius;
    [HideInInspector] public float aimSpreadRadius;
}

[Serializable]
public struct PlayerSoundStats
{
    public float gunshotSoundRadius;
    public float soundInvestigateDelaySeconds;
    public float footstepSoundRadius;
    public float footstepSoundInterval;
}


[Serializable]
public struct PlayerFeatureFlags
{
    public bool leaveVaccineOnEnemyKill;
    public bool paintBulletLeavesTrail;
}

[Serializable]
public struct PlayerStatsSnapshot
{
    public PlayerWeaponStats weapon;
    [HideInInspector] public PlayerPaintStats paint;
    public PlayerMovementStats movement;
    public PlayerSurvivalStats survival;
    public PlayerVisionStats vision;
    public PlayerAimStats aim;
    public PlayerSoundStats sound;
    public PlayerFeatureFlags features;
}
