using UnityEngine;

[DisallowMultipleComponent]
[DefaultExecutionOrder(-1000)]
public class DifficultyRuntime : MonoBehaviour
{
    public static DifficultyRuntime Instance { get; private set; }

    [SerializeField] private DifficultyRulesSO _currentRules;

    public DifficultyRulesSO Current => _currentRules;
    public static DifficultyRulesSO CurrentRules => Instance != null ? Instance._currentRules : null;

    private void Awake()
    {
        if (Instance != null && Instance != this)
            Debug.LogWarning("[DifficultyRuntime] Multiple DifficultyRuntime instances found. Latest instance will be used.", this);

        Instance = this;
    }

    public void SetDifficulty(DifficultyRulesSO rules)
    {
        _currentRules = rules;
    }

    public static float PlayerInfectionGainMultiplier =>
        CurrentRules != null ? CurrentRules.PlayerInfectionGainMultiplier : 1f;

    public static float PlayerInfectionRecoverMultiplier =>
        CurrentRules != null ? CurrentRules.PlayerInfectionRecoverMultiplier : 1f;

    public static float PlayerDamageMultiplier =>
        CurrentRules != null ? CurrentRules.PlayerDamageMultiplier : 1f;

    public static float EnemyHealthMultiplier =>
        CurrentRules != null ? CurrentRules.EnemyHealthMultiplier : 1f;

    public static float EnemyDamageMultiplier =>
        CurrentRules != null ? CurrentRules.EnemyDamageMultiplier : 1f;

    public static float NamedOutsidePressureMultiplier =>
        CurrentRules != null ? CurrentRules.NamedOutsidePressureMultiplier : 1f;

    public static float ApplyPlayerDamage(float baseDamage)
    {
        return CurrentRules != null ? CurrentRules.ApplyPlayerDamage(baseDamage) : Mathf.Max(0f, baseDamage);
    }

    public static float ApplyEnemyHealth(float baseHealth)
    {
        return CurrentRules != null ? CurrentRules.ApplyEnemyHealth(baseHealth) : Mathf.Max(1f, baseHealth);
    }

    public static float ApplyEnemyDamage(float baseDamage)
    {
        return CurrentRules != null ? CurrentRules.ApplyEnemyDamage(baseDamage) : Mathf.Max(0f, baseDamage);
    }

    public static float ApplyPlayerInfectionGain(float baseAmount)
    {
        return CurrentRules != null ? CurrentRules.ApplyPlayerInfectionGain(baseAmount) : Mathf.Max(0f, baseAmount);
    }

    public static float ApplyEnemyNormalMoveSpeed(float baseSpeed)
    {
        return CurrentRules != null ? CurrentRules.ApplyEnemyNormalMoveSpeed(baseSpeed) : Mathf.Max(0f, baseSpeed);
    }

    public static float ApplyEnemyChaseMoveSpeed(float baseSpeed)
    {
        return CurrentRules != null ? CurrentRules.ApplyEnemyChaseMoveSpeed(baseSpeed) : Mathf.Max(0f, baseSpeed);
    }

    public static float ApplyEnemyProjectileSpeed(float baseSpeed)
    {
        return CurrentRules != null ? CurrentRules.ApplyEnemyProjectileSpeed(baseSpeed) : Mathf.Max(0f, baseSpeed);
    }

    public static float ApplyEnemyInfectionCastDuration(float baseSeconds)
    {
        return CurrentRules != null ? CurrentRules.ApplyEnemyInfectionCastDuration(baseSeconds) : Mathf.Max(0.01f, baseSeconds);
    }

    public static float ApplyEnemyVirusPaintRadius(float baseRadius)
    {
        return CurrentRules != null ? CurrentRules.ApplyEnemyVirusPaintRadius(baseRadius) : Mathf.Max(0f, baseRadius);
    }

    public static float ApplyPlayerVaccinePaintRadius(float baseRadius)
    {
        return CurrentRules != null ? CurrentRules.ApplyPlayerVaccinePaintRadius(baseRadius) : Mathf.Max(0f, baseRadius);
    }

    public static float ApplyEnemySpawnInterval(float baseSeconds)
    {
        return CurrentRules != null ? CurrentRules.ApplyEnemySpawnInterval(baseSeconds) : Mathf.Max(0.01f, baseSeconds);
    }

    public static float ApplyNamedFirstReservationDelay(float baseSeconds)
    {
        return CurrentRules != null ? CurrentRules.ApplyNamedFirstReservationDelay(baseSeconds) : Mathf.Max(0f, baseSeconds);
    }

    public static float ApplyNamedReservationDuration(float baseSeconds)
    {
        return CurrentRules != null ? CurrentRules.ApplyNamedReservationDuration(baseSeconds) : Mathf.Max(0f, baseSeconds);
    }

    public static float ApplyNamedRespawnCooldown(float baseSeconds)
    {
        return CurrentRules != null ? CurrentRules.ApplyNamedRespawnCooldown(baseSeconds) : Mathf.Max(0f, baseSeconds);
    }

    public static float ApplyNamedRetryDelay(float baseSeconds)
    {
        return CurrentRules != null ? CurrentRules.ApplyNamedRetryDelay(baseSeconds) : Mathf.Max(0f, baseSeconds);
    }

    public static float ApplyNamedPatternFirstDelay(float baseSeconds)
    {
        return CurrentRules != null ? CurrentRules.ApplyNamedPatternFirstDelay(baseSeconds) : Mathf.Max(0f, baseSeconds);
    }

    public static float ApplyNamedPatternRepeatDelay(float baseSeconds)
    {
        return CurrentRules != null ? CurrentRules.ApplyNamedPatternRepeatDelay(baseSeconds) : Mathf.Max(0f, baseSeconds);
    }

    public static float ApplyNamedPatternActiveDuration(float baseSeconds)
    {
        return CurrentRules != null ? CurrentRules.ApplyNamedPatternActiveDuration(baseSeconds) : Mathf.Max(0.01f, baseSeconds);
    }
}
