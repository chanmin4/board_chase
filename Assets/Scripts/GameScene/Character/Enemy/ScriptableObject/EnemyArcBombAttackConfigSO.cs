using UnityEngine;

[CreateAssetMenu(
    fileName = "EnemyArcBombAttackConfig",
    menuName = "ScriptableObject/GameScene/Enemy/Definition_Config/Enemy Arc Bomb Attack Config")]
public class EnemyArcBombAttackConfigSO : PoisonPuddleDamageConfigSO
{
    [Header("Projectile")]
    [SerializeField] private EnemyArcBombProjectile _projectilePrefab;
    [SerializeField] private MaskRenderManagerEventChannelSO _maskRenderManagerReadyChannel;

    [Header("Arc")]
    [SerializeField, Min(0.01f)] private float _travelTime = 1.2f;
    [SerializeField, Min(0f)] private float _arcHeight = 4f;
    [SerializeField] private float _spawnYOffset = 0.5f;
    [SerializeField] private float _targetYOffset;
    [SerializeField, Min(0f)] private float _fallbackDistance = 6f;

    [Header("Flight")]
    [SerializeField] private bool _disableProjectileCollidersDuringFlight = true;

    [Header("Cycle")]
    [SerializeField] private bool _fireOnEnter = true;
    [SerializeField, Min(1)] private int _shotsPerCycle = 1;
    [SerializeField, Min(0f)] private float _shotInterval = 0.15f;
    [SerializeField, Min(0)] private int _maxCycles = 1;
    [SerializeField, Min(0f)] private float _cycleInterval = 5f;

    [Header("Impact Damage")]
    [SerializeField] private ArcBombRadiusSource _damageRadiusSource = ArcBombRadiusSource.PaintRadiusWorld;
    [SerializeField, Min(0f)] private float _damageRadius = 1.5f;
    [SerializeField, Min(0f)] private float _impactHealthDamage = 10f;
    [SerializeField, Min(0f)] private float _impactInfectionDamage = 5f;
    [SerializeField] private LayerMask _damageTargetMask;
    [SerializeField] private QueryTriggerInteraction _triggerInteraction = QueryTriggerInteraction.Collide;

    [Header("Impact Telegraph")]
    [SerializeField] private bool _showImpactTelegraph = true;
    [SerializeField] private ArcBombRadiusSource _telegraphRadiusSource = ArcBombRadiusSource.PaintRadiusWorld;
    [SerializeField, Min(0f)] private float _telegraphRadiusCustom = 1.5f;
    [SerializeField] private AreaAttackTelegraphStyle _impactTelegraphStyle = default;

    [Header("Paint")]
    [SerializeField] private MaskRenderManager.PaintChannel _paintChannel = MaskRenderManager.PaintChannel.Virus;
    [SerializeField, Min(0f)] private float _paintRadiusWorld = 1.5f;
    [SerializeField] private int _paintPriority;

    public EnemyArcBombProjectile ProjectilePrefab => _projectilePrefab;
    public MaskRenderManagerEventChannelSO MaskRenderManagerReadyChannel => _maskRenderManagerReadyChannel;
    public float TravelTime => Mathf.Max(0.01f, _travelTime);
    public float ArcHeight => Mathf.Max(0f, _arcHeight);
    public float SpawnYOffset => _spawnYOffset;
    public float TargetYOffset => _targetYOffset;
    public float FallbackDistance => Mathf.Max(0f, _fallbackDistance);
    public bool DisableProjectileCollidersDuringFlight => _disableProjectileCollidersDuringFlight;
    public bool FireOnEnter => _fireOnEnter;
    public int ShotsPerCycle => Mathf.Max(1, _shotsPerCycle);
    public float ShotInterval => Mathf.Max(0f, _shotInterval);
    public int MaxCycles => Mathf.Max(0, _maxCycles);
    public float CycleInterval => Mathf.Max(0f, _cycleInterval);
    public float ImpactHealthDamage => DifficultyRuntime.ApplyEnemyDamage(_impactHealthDamage);
    public float ImpactInfectionDamage => DifficultyRuntime.ApplyPlayerInfectionGain(_impactInfectionDamage);
    public LayerMask DamageTargetMask => _damageTargetMask;
    public QueryTriggerInteraction TriggerInteraction => _triggerInteraction;
    public bool ShowImpactTelegraph => _showImpactTelegraph;
    public AreaAttackTelegraphStyle ImpactTelegraphStyle =>
        _impactTelegraphStyle.segments > 0 ? _impactTelegraphStyle : AreaAttackTelegraphStyle.Default;
    public MaskRenderManager.PaintChannel PaintChannel => _paintChannel;
    public float PaintRadiusWorld => DifficultyRuntime.ApplyEnemyVirusPaintRadius(_paintRadiusWorld);
    public int PaintPriority => _paintPriority;
    public PoisonPuddleDamageConfigSO PoisonPuddleDamageConfig => this;

    public float DamageRadius => ResolveRadius(_damageRadiusSource, _damageRadius, _damageRadius, PaintRadiusWorld);
    public float ImpactTelegraphRadius => ResolveRadius(_telegraphRadiusSource, _telegraphRadiusCustom, DamageRadius, PaintRadiusWorld);

    private static float ResolveRadius(ArcBombRadiusSource source, float customRadius, float damageRadius, float paintRadiusWorld)
    {
        return source switch
        {
            ArcBombRadiusSource.DamageRadius => Mathf.Max(0f, damageRadius),
            ArcBombRadiusSource.Custom => Mathf.Max(0f, customRadius),
            _ => Mathf.Max(0f, paintRadiusWorld)
        };
    }
}