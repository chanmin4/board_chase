using UnityEngine;

[CreateAssetMenu(
    fileName = "ChaserSelfDestructConfig",
    menuName = "Game/Enemy/Definition_Config/NormalEnemy/Chaser/Chaser Self Destruct Config")]
public class ChaserSelfDestructConfigSO : ScriptableObject
{
    [Header("Virus Paint")]
    [SerializeField] private float _virusPaintRadius = 3.5f;
    [SerializeField] private int _paintPriority = 10;

    [Header("Player Hit")]
    [SerializeField] private LayerMask _damageMask = Physics.DefaultRaycastLayers;
    [SerializeField] private QueryTriggerInteraction _triggerInteraction = QueryTriggerInteraction.Collide;
    [SerializeField] private float _playerHitRadius = 3f;
    [SerializeField] private float _healthDamage = 0f;
    [SerializeField] private float _infectionDamage = 25f;
    [SerializeField] private int _maxOverlapHits = 32;

    public float VirusPaintRadius => DifficultyRuntime.ApplyEnemyVirusPaintRadius(_virusPaintRadius);
    public int PaintPriority => _paintPriority;
    public LayerMask DamageMask => _damageMask;
    public QueryTriggerInteraction TriggerInteraction => _triggerInteraction;
    public float PlayerHitRadius => Mathf.Max(0f, _playerHitRadius);
    public float HealthDamage => DifficultyRuntime.ApplyEnemyDamage(_healthDamage);
    public float InfectionDamage => DifficultyRuntime.ApplyPlayerInfectionGain(_infectionDamage);
    public int MaxOverlapHits => Mathf.Max(1, _maxOverlapHits);
}
