using UnityEngine;

[CreateAssetMenu(
    fileName = "PoisonPuddleDamageConfig",
    menuName = "Game/Enemy/Poison Puddle Damage Config")]
public class PoisonPuddleDamageConfigSO : NamedAttackConfigSO
{
    [Header("Damage Per Tick")]
    [SerializeField, Min(0f)] private float _healthDamagePerTick = 1f;
    [SerializeField, Min(0f)] private float _infectionGainPerTick = 1f;

    public float HealthDamagePerTick => DifficultyRuntime.ApplyEnemyDamage(_healthDamagePerTick);
    public float InfectionGainPerTick => DifficultyRuntime.ApplyPlayerInfectionGain(_infectionGainPerTick);

    public bool HasDamage =>
        HealthDamagePerTick > 0f ||
        InfectionGainPerTick > 0f;
}