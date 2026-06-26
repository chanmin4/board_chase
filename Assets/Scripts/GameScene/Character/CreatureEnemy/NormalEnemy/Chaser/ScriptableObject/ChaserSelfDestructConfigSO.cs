using UnityEngine;

[CreateAssetMenu(
    fileName = "ChaserSelfDestructConfig",
    menuName = "Game/Enemy/Definition_Config/NormalEnemy/Chaser/Chaser Self Destruct Config")]
public class ChaserSelfDestructConfigSO : EnemyAttackConfigSO
{
    [Header("Virus Paint")]
    [Tooltip("Event channel that provides the active MaskRenderManager. If empty, the runtime searches the scene.")]
    [SerializeField] private MaskRenderManagerEventChannelSO _maskRenderManagerReadyChannel;
    [SerializeField] private float _virusPaintRadius = 3.5f;
    [SerializeField] private int _paintPriority = 10;

    [Header("Player Hit")]
    [SerializeField] private LayerMask _damageMask = Physics.DefaultRaycastLayers;
    [SerializeField] private QueryTriggerInteraction _triggerInteraction = QueryTriggerInteraction.Collide;
    [SerializeField] private float _playerHitRadius = 3f;
    [SerializeField] private float _healthDamage = 0f;
    [SerializeField] private float _infectionDamage = 25f;
    [SerializeField] private int _maxOverlapHits = 32;

    [Header("Self")]
    [Tooltip("Stops the NavMeshAgent and clears its path before exploding.")]
    [SerializeField] private bool _stopAgentOnEnter = true;

    [Tooltip("Kills this enemy after the explosion.")]
    [SerializeField] private bool _killSelfAfterExplosion = true;

    public override EnemyAttackBehaviorType AttackBehaviorType => EnemyAttackBehaviorType.SelfDestruct;

    public MaskRenderManagerEventChannelSO MaskRenderManagerReadyChannel => _maskRenderManagerReadyChannel;
    public float VirusPaintRadius => DifficultyRuntime.ApplyEnemyVirusPaintRadius(_virusPaintRadius);
    public int PaintPriority => _paintPriority;
    public LayerMask DamageMask => _damageMask;
    public QueryTriggerInteraction TriggerInteraction => _triggerInteraction;
    public float PlayerHitRadius => Mathf.Max(0f, _playerHitRadius);
    public float HealthDamage => DifficultyRuntime.ApplyEnemyDamage(_healthDamage);
    public float InfectionDamage => DifficultyRuntime.ApplyPlayerInfectionGain(_infectionDamage);
    public int MaxOverlapHits => Mathf.Max(1, _maxOverlapHits);
    public bool StopAgentOnEnter => _stopAgentOnEnter;
    public bool KillSelfAfterExplosion => _killSelfAfterExplosion;
}
