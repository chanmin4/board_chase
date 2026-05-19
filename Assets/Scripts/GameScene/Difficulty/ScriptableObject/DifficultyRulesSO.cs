using UnityEngine;

[CreateAssetMenu(
    fileName = "DifficultyRules",
    menuName = "Game/Difficulty Rules")]
public class DifficultyRulesSO : ScriptableObject
{
    [Header("Identity")]
    [SerializeField] private GameDifficulty difficulty = GameDifficulty.Normal;
    [SerializeField] private string displayName = "Normal";

    [Header("Player")]
    [Tooltip("Multiplier for infection gained from virus zones, poison puddles, enemy hits, and similar sources.")]
    [Min(0.01f)]
    [SerializeField] private float playerInfectionGainMultiplier = 1f;

    [Tooltip("Multiplier for infection recovery from vaccine zones, named kill rewards, boss kill rewards, and similar sources.")]
    [Min(0.01f)]
    [SerializeField] private float playerInfectionRecoverMultiplier = 1f;

    [Tooltip("Multiplier applied to player outgoing attack damage when a weapon/combat system opts into difficulty scaling.")]
    [Min(0.01f)]
    [SerializeField] private float playerDamageMultiplier = 1f;

    [Tooltip("Optional player movement multiplier. Keep at 1 for fair arcade difficulty unless you intentionally want assist/penalty rules.")]
    [Min(0.01f)]
    [SerializeField] private float playerMoveSpeedMultiplier = 1f;

    [Tooltip("Optional player dash cooldown multiplier. Lower means faster dash recovery.")]
    [Min(0.01f)]
    [SerializeField] private float playerDashCooldownMultiplier = 1f;

    [Header("Enemy")]
    [Tooltip("Multiplier applied to enemy max health.")]
    [Min(0.01f)]
    [SerializeField] private float enemyHealthMultiplier = 1f;

    [Tooltip("Multiplier applied to enemy outgoing damage when enemy attacks opt into difficulty scaling.")]
    [Min(0.01f)]
    [SerializeField] private float enemyDamageMultiplier = 1f;

    [Tooltip("Multiplier for default enemy movement such as wander or infection target movement.")]
    [Min(0.01f)]
    [SerializeField] private float enemyNormalMoveSpeedMultiplier = 1f;

    [Tooltip("Multiplier for enemy chase movement.")]
    [Min(0.01f)]
    [SerializeField] private float enemyChaseMoveSpeedMultiplier = 1f;

    [Tooltip("Multiplier for enemy projectile speed when projectile configs opt into difficulty scaling.")]
    [Min(0.01f)]
    [SerializeField] private float enemyProjectileSpeedMultiplier = 1f;

    [Tooltip("Spawn rate multiplier for normal enemies. 2 means twice as many spawn attempts over time.")]
    [Min(0.01f)]
    [SerializeField] private float enemySpawnRateMultiplier = 1f;

    [Tooltip("Minimum interval after difficulty scaling is applied to normal enemy spawn interval.")]
    [Min(0.01f)]
    [SerializeField] private float enemySpawnIntervalMinSeconds = 0.25f;

    [Header("Territory")]
    [Tooltip("Multiplier for enemy infection casting speed or cast progress when the infection action opts into difficulty scaling.")]
    [Min(0.01f)]
    [SerializeField] private float enemyInfectionCastSpeedMultiplier = 1f;

    [Tooltip("Multiplier for enemy virus paint radius/area when paint systems opt into difficulty scaling.")]
    [Min(0.01f)]
    [SerializeField] private float enemyVirusPaintRadiusMultiplier = 1f;

    [Tooltip("Multiplier for player vaccine paint radius/area when paint systems opt into difficulty scaling.")]
    [Min(0.01f)]
    [SerializeField] private float playerVaccinePaintRadiusMultiplier = 1f;

    [Tooltip("Multiplier for global infection-control drain or outside pressure.")]
    [Min(0.01f)]
    [SerializeField] private float infectionControlDrainMultiplier = 1f;

    [Header("Named Sector Cycle")]
    [SerializeField, Min(0.01f)] private float namedFirstReservationDelayMultiplier = 1f;
    [SerializeField] private float namedFirstReservationDelayAddSeconds = 0f;
    [SerializeField, Min(0f)] private float namedFirstReservationDelayMinSeconds = 0f;

    [SerializeField, Min(0.01f)] private float namedReservationDurationMultiplier = 1f;
    [SerializeField] private float namedReservationDurationAddSeconds = 0f;
    [SerializeField, Min(1f)] private float namedReservationDurationMinSeconds = 5f;

    [SerializeField, Min(0.01f)] private float namedRespawnCooldownMultiplier = 1f;
    [SerializeField] private float namedRespawnCooldownAddSeconds = 0f;
    [SerializeField, Min(1f)] private float namedRespawnCooldownMinSeconds = 10f;

    [SerializeField, Min(0.01f)] private float namedRetryDelayMultiplier = 1f;
    [SerializeField] private float namedRetryDelayAddSeconds = 0f;
    [SerializeField, Min(0f)] private float namedRetryDelayMinSeconds = 1f;

    [Header("Named Pattern")]
    [SerializeField, Min(0.01f)] private float namedPatternFirstDelayMultiplier = 1f;
    [SerializeField] private float namedPatternFirstDelayAddSeconds = 0f;
    [SerializeField, Min(0f)] private float namedPatternFirstDelayMinSeconds = 0f;

    [SerializeField, Min(0.01f)] private float namedPatternRepeatDelayMultiplier = 1f;
    [SerializeField] private float namedPatternRepeatDelayAddSeconds = 0f;
    [SerializeField, Min(0f)] private float namedPatternRepeatDelayMinSeconds = 0f;

    [SerializeField, Min(0.01f)] private float namedPatternActiveDurationMultiplier = 1f;
    [SerializeField] private float namedPatternActiveDurationAddSeconds = 0f;
    [SerializeField, Min(1f)] private float namedPatternActiveDurationMinSeconds = 3f;

    [Tooltip("Multiplier for pressure applied outside the named battle while a named enemy is active.")]
    [Min(0.01f)]
    [SerializeField] private float namedOutsidePressureMultiplier = 1f;

    public GameDifficulty Difficulty => difficulty;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? difficulty.ToString() : displayName;

    public float PlayerInfectionGainMultiplier => playerInfectionGainMultiplier;
    public float PlayerInfectionRecoverMultiplier => playerInfectionRecoverMultiplier;
    public float PlayerDamageMultiplier => playerDamageMultiplier;
    public float PlayerMoveSpeedMultiplier => playerMoveSpeedMultiplier;
    public float PlayerDashCooldownMultiplier => playerDashCooldownMultiplier;

    public float EnemyHealthMultiplier => enemyHealthMultiplier;
    public float EnemyDamageMultiplier => enemyDamageMultiplier;
    public float EnemyNormalMoveSpeedMultiplier => enemyNormalMoveSpeedMultiplier;
    public float EnemyChaseMoveSpeedMultiplier => enemyChaseMoveSpeedMultiplier;
    public float EnemyProjectileSpeedMultiplier => enemyProjectileSpeedMultiplier;
    public float EnemySpawnRateMultiplier => enemySpawnRateMultiplier;

    public float EnemyInfectionCastSpeedMultiplier => enemyInfectionCastSpeedMultiplier;
    public float EnemyVirusPaintRadiusMultiplier => enemyVirusPaintRadiusMultiplier;
    public float PlayerVaccinePaintRadiusMultiplier => playerVaccinePaintRadiusMultiplier;
    public float InfectionControlDrainMultiplier => infectionControlDrainMultiplier;
    public float NamedOutsidePressureMultiplier => namedOutsidePressureMultiplier;

    public float ApplyPlayerDamage(float baseDamage)
    {
        return Mathf.Max(0f, baseDamage) * playerDamageMultiplier;
    }

    public float ApplyEnemyHealth(float baseHealth)
    {
        return Mathf.Max(1f, baseHealth * enemyHealthMultiplier);
    }

    public float ApplyEnemyDamage(float baseDamage)
    {
        return Mathf.Max(0f, baseDamage) * enemyDamageMultiplier;
    }

    public float ApplyPlayerInfectionGain(float baseAmount)
    {
        return Mathf.Max(0f, baseAmount) * playerInfectionGainMultiplier;
    }

    public float ApplyEnemyNormalMoveSpeed(float baseSpeed)
    {
        return Mathf.Max(0f, baseSpeed * enemyNormalMoveSpeedMultiplier);
    }

    public float ApplyEnemyChaseMoveSpeed(float baseSpeed)
    {
        return Mathf.Max(0f, baseSpeed * enemyChaseMoveSpeedMultiplier);
    }

    public float ApplyEnemyProjectileSpeed(float baseSpeed)
    {
        return Mathf.Max(0f, baseSpeed * enemyProjectileSpeedMultiplier);
    }

    public float ApplyEnemyInfectionCastDuration(float baseSeconds)
    {
        float speed = Mathf.Max(0.01f, enemyInfectionCastSpeedMultiplier);
        return Mathf.Max(0.01f, baseSeconds / speed);
    }

    public float ApplyEnemyVirusPaintRadius(float baseRadius)
    {
        return Mathf.Max(0f, baseRadius * enemyVirusPaintRadiusMultiplier);
    }

    public float ApplyPlayerVaccinePaintRadius(float baseRadius)
    {
        return Mathf.Max(0f, baseRadius * playerVaccinePaintRadiusMultiplier);
    }

    public float ApplyEnemySpawnInterval(float baseSeconds)
    {
        float rate = Mathf.Max(0.01f, enemySpawnRateMultiplier);
        return Mathf.Max(enemySpawnIntervalMinSeconds, baseSeconds / rate);
    }

    public float ApplyNamedFirstReservationDelay(float baseSeconds)
    {
        return ApplyTime(
            baseSeconds,
            namedFirstReservationDelayMultiplier,
            namedFirstReservationDelayAddSeconds,
            namedFirstReservationDelayMinSeconds);
    }

    public float ApplyNamedReservationDuration(float baseSeconds)
    {
        return ApplyTime(
            baseSeconds,
            namedReservationDurationMultiplier,
            namedReservationDurationAddSeconds,
            namedReservationDurationMinSeconds);
    }

    public float ApplyNamedRespawnCooldown(float baseSeconds)
    {
        return ApplyTime(
            baseSeconds,
            namedRespawnCooldownMultiplier,
            namedRespawnCooldownAddSeconds,
            namedRespawnCooldownMinSeconds);
    }

    public float ApplyNamedRetryDelay(float baseSeconds)
    {
        return ApplyTime(
            baseSeconds,
            namedRetryDelayMultiplier,
            namedRetryDelayAddSeconds,
            namedRetryDelayMinSeconds);
    }

    public float ApplyNamedPatternFirstDelay(float baseSeconds)
    {
        return ApplyTime(
            baseSeconds,
            namedPatternFirstDelayMultiplier,
            namedPatternFirstDelayAddSeconds,
            namedPatternFirstDelayMinSeconds);
    }

    public float ApplyNamedPatternRepeatDelay(float baseSeconds)
    {
        return ApplyTime(
            baseSeconds,
            namedPatternRepeatDelayMultiplier,
            namedPatternRepeatDelayAddSeconds,
            namedPatternRepeatDelayMinSeconds);
    }

    public float ApplyNamedPatternActiveDuration(float baseSeconds)
    {
        return ApplyTime(
            baseSeconds,
            namedPatternActiveDurationMultiplier,
            namedPatternActiveDurationAddSeconds,
            namedPatternActiveDurationMinSeconds);
    }

    private float ApplyTime(
        float baseSeconds,
        float multiplier,
        float addSeconds,
        float minSeconds)
    {
        return Mathf.Max(minSeconds, baseSeconds * multiplier + addSeconds);
    }
}
