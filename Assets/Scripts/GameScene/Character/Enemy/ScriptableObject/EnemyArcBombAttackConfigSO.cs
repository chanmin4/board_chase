using UnityEngine;

[CreateAssetMenu(
    fileName = "EnemyArcBombAttackConfig",
    menuName = "ScriptableObject/GameScene/Enemy/Definition_Config/Enemy Arc Bomb Attack Config")]
public class EnemyArcBombAttackConfigSO : ScriptableObject
{
    [Header("Projectile")]
    [SerializeField] private EnemyArcBombProjectile _projectilePrefab;
    [SerializeField] private MaskRenderManagerEventChannelSO _maskRenderManagerReadyChannel;

    [Header("Arc")]
    [SerializeField, Min(0.01f)] private float _travelTime = 1.2f;
    [SerializeField, Min(0f)] private float _arcHeight = 4f;
    [SerializeField] private float _spawnYOffset = 0.5f;
    [SerializeField] private float _targetYOffset = 0f;
    [SerializeField, Min(0f)] private float _fallbackDistance = 6f;

    [Header("Cycle")]
    [SerializeField] private bool _fireOnEnter = true;
    [SerializeField, Min(1)] private int _shotsPerCycle = 1;
    [SerializeField, Min(0f)] private float _shotInterval = 0.15f;
    [Tooltip("0 means infinite while the state is active.")]
    [SerializeField, Min(0)] private int _maxCycles = 1;
    [SerializeField, Min(0f)] private float _cycleInterval = 5f;

    [Header("Impact Damage")]
    [SerializeField, Min(0f)] private float _damageRadius = 1.5f;
    [SerializeField, Min(0f)] private float _impactHealthDamage = 10f;
    [SerializeField, Min(0f)] private float _impactInfectionDamage = 5f;
    [SerializeField] private LayerMask _damageTargetMask;
    [SerializeField] private QueryTriggerInteraction _triggerInteraction = QueryTriggerInteraction.Collide;

    [Header("Paint")]
    [SerializeField] private MaskRenderManager.PaintChannel _paintChannel = MaskRenderManager.PaintChannel.Virus;
    [SerializeField, Min(0f)] private float _paintRadiusWorld = 1.5f;
    [SerializeField] private int _paintPriority = 0;

    [Header("Poison Puddle Damage Config")]
    [SerializeField] private PoisonPuddleDamageConfigSO _poisonPuddleDamageConfig;

    public EnemyArcBombProjectile ProjectilePrefab => _projectilePrefab;
    public MaskRenderManagerEventChannelSO MaskRenderManagerReadyChannel => _maskRenderManagerReadyChannel;

    public float TravelTime => Mathf.Max(0.01f, _travelTime);
    public float ArcHeight => Mathf.Max(0f, _arcHeight);
    public float SpawnYOffset => _spawnYOffset;
    public float TargetYOffset => _targetYOffset;
    public float FallbackDistance => Mathf.Max(0f, _fallbackDistance);

    public bool FireOnEnter => _fireOnEnter;
    public int ShotsPerCycle => Mathf.Max(1, _shotsPerCycle);
    public float ShotInterval => Mathf.Max(0f, _shotInterval);
    public int MaxCycles => Mathf.Max(0, _maxCycles);
    public float CycleInterval => Mathf.Max(0f, _cycleInterval);

    public float DamageRadius => Mathf.Max(0f, _damageRadius);
    public float ImpactHealthDamage => DifficultyRuntime.ApplyEnemyDamage(_impactHealthDamage);
    public float ImpactInfectionDamage => DifficultyRuntime.ApplyPlayerInfectionGain(_impactInfectionDamage);
    public LayerMask DamageTargetMask => _damageTargetMask;
    public QueryTriggerInteraction TriggerInteraction => _triggerInteraction;

    public MaskRenderManager.PaintChannel PaintChannel => _paintChannel;
    public float PaintRadiusWorld => DifficultyRuntime.ApplyEnemyVirusPaintRadius(_paintRadiusWorld);
    public int PaintPriority => _paintPriority;
    public PoisonPuddleDamageConfigSO PoisonPuddleDamageConfig => _poisonPuddleDamageConfig;
}
