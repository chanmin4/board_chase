using UnityEngine;

[CreateAssetMenu(
    fileName = "DifficultyRules",
    menuName = "Game/Difficulty Rules")]
public class DifficultyRulesSO : ScriptableObject
{
    [Header("Player")]
    [Min(0f)]
    [SerializeField] private float playerInfectionGainMultiplier = 1f;

    [Min(0f)]
    [SerializeField] private float playerInfectionRecoverMultiplier = 1f;

    [Min(0f)]
    [SerializeField] private float playerDamageMultiplier = 1f;

    [Header("Enemy")]
    [Min(0f)]
    [SerializeField] private float enemyHealthMultiplier = 1f;

    [Min(0f)]
    [SerializeField] private float enemyDamageMultiplier = 1f;

    public float PlayerInfectionGainMultiplier => playerInfectionGainMultiplier;
    public float PlayerInfectionRecoverMultiplier => playerInfectionRecoverMultiplier;
    public float PlayerDamageMultiplier => playerDamageMultiplier;
    public float EnemyHealthMultiplier => enemyHealthMultiplier;
    public float EnemyDamageMultiplier => enemyDamageMultiplier;
}
