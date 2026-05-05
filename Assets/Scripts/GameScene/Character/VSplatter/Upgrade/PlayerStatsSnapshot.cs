using System;
using UnityEngine;

[Serializable]
public struct PlayerWeaponStats
{
    public float attackDamage;
    public float namedBossDamageMultiplier;
    public float shotsPerSecond;
    public float reloadSpeedMultiplier;
    public float reloadDurationSeconds;
    public int magazineSize;
}

[Serializable]
public struct PlayerPaintStats
{
    public float paintRadius;
    public float occupationWinThreshold;
}

[Serializable]
public struct PlayerMovementStats
{
    public float moveSpeed;
    public float dashDistanceMultiplier;
}

[Serializable]
public struct PlayerSurvivalStats
{
    public float maxHealth;
}

[Serializable]
public struct PlayerShockwaveStats
{
    public float cooldownSeconds;
}

[Serializable]
public struct PlayerFeatureFlags
{
    public bool leaveVaccineOnEnemyKill;
    public bool paintBulletLeavesTrail;
    public bool shockwavePaintsVaccine;
}

[Serializable]
public struct PlayerStatsSnapshot
{
    public PlayerWeaponStats weapon;
    public PlayerPaintStats paint;
    public PlayerMovementStats movement;
    public PlayerSurvivalStats survival;
    public PlayerShockwaveStats shockwave;
    public PlayerFeatureFlags features;
}
