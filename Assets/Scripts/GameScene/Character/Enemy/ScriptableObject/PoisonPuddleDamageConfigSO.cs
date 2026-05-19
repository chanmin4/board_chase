using UnityEngine;

[CreateAssetMenu(
    fileName = "PoisonPuddleDamageConfig",
    menuName = "Game/Enemy/Poison Puddle Damage Config")]
public class PoisonPuddleDamageConfigSO : ScriptableObject
{
    [Header("Tick")]
    [SerializeField, Min(0.01f)] private float _tickInterval = 0.5f;

    [Header("Damage Per Second")]
    [SerializeField, Min(0f)] private float _healthDamagePerSecond = 5f;
    [SerializeField, Min(0f)] private float _infectionGainPerSecond = 12f;

    public float TickInterval => Mathf.Max(0.01f, _tickInterval);
    public float HealthDamagePerSecond => DifficultyRuntime.ApplyEnemyDamage(_healthDamagePerSecond);
    public float InfectionGainPerSecond => DifficultyRuntime.ApplyPlayerInfectionGain(_infectionGainPerSecond);

    public bool HasDamage =>
        HealthDamagePerSecond > 0f ||
        InfectionGainPerSecond > 0f;
}
