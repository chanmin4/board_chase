using System;
using UnityEngine;

[Serializable]
public struct PlayerWeaponStats
{
    public float attackDamage;
    public float namedBossDamageMultiplier;

    public float maxRange;

    public float shotsPerSecond;

    public float reloadDurationSeconds;
    public int magazineSize;
    public float paintMarkDamage;
    public float infectionDamage;
    public int penetrationClass;
}


[Serializable]
public struct PlayerPaintStats
{
    public float paintRadius;
    public int paintPriority;
    public float occupationWinThreshold;
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
    public int armorClass;
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
    public float aimRangeMultiplier;
    public float aimMoveSpeedMultiplier;
    public float hipFireSpreadAngleDeg;
    public float aimSpreadAngleDeg;
    public float recoilAngleDeg;
    public float recoilRecoverySpeedDegPerSecond;
    public float recoilForwardDistancePerShot;
    public float recoilSideDistancePerShot;
    public float maxRecoilDistance;
    public float recoilDistanceRecoveryPerSecond;
    public float hipFireSpreadRadius;
    public float aimSpreadRadius;
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
    public PlayerPaintStats paint;
    public PlayerMovementStats movement;
    public PlayerSurvivalStats survival;
    public PlayerVisionStats vision;
    public PlayerAimStats aim;
    public PlayerSoundStats sound;
    public PlayerFeatureFlags features;
}
