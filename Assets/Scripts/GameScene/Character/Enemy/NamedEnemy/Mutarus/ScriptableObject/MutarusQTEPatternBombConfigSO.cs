using UnityEngine;

[CreateAssetMenu(
    fileName = "MutarusQTEPatternBombConfig",
    menuName = "ScriptableObject/GameScene/Enemy/Definition_Config/Mutarus QTE Pattern Bomb Config")]
public class MutarusQTEPatternBombConfigSO : ScriptableObject
{
    [Header("Periodic Arc Bomb")]
    [SerializeField] private bool _usePeriodicArcBomb = true;
    [SerializeField] private EnemyArcBombProjectile _arcBombPrefab;
    [SerializeField] private MaskRenderManagerEventChannelSO _maskRenderManagerReadyChannel;

    [Tooltip("If true, the first burst starts as soon as the QTE runtime becomes ready.")]
    [SerializeField] private bool _fireBombOnEnter = false;

    [Tooltip("Delay before the first burst when Fire Bomb On Enter is false.")]
    [SerializeField, Min(0f)] private float _firstBombDelay = 2f;

    [Tooltip("Long delay after each burst finishes. Pattern keeps repeating bursts while active.")]
    [SerializeField, Min(0.01f)] private float _bombInterval = 5f;

    [Header("Periodic Arc Bomb Burst")]
    [Tooltip("Number of bombs fired in one burst.")]
    [SerializeField, Min(1)] private int _bombsPerBurst = 1;

    [Tooltip("Delay between bombs inside one burst.")]
    [SerializeField, Min(0f)] private float _bombShotInterval = 0.25f;

    [Header("Arc Bomb Trajectory")]
    [SerializeField, Min(0.01f)] private float _bombTravelTime = 1.2f;
    [SerializeField, Min(0f)] private float _bombArcHeight = 4f;
    [SerializeField] private float _bombSpawnYOffset = 0.5f;
    [SerializeField] private float _bombTargetYOffset = 0f;
    [SerializeField, Min(0f)] private float _fallbackTargetDistance = 6f;
    [SerializeField, Min(0f)] private float _targetRandomRadius = 0f;

    [Header("Arc Bomb Flight")]
    [Tooltip("If true, projectile colliders are disabled while the bomb is flying. Damage is applied only on impact.")]
    [SerializeField] private bool _disableProjectileCollidersDuringFlight = true;

    [Header("Arc Bomb Impact Damage")]
    [Tooltip("PaintRadiusWorld makes impact damage radius match the poison puddle radius.")]
    [SerializeField] private ArcBombRadiusSource _damageRadiusSource = ArcBombRadiusSource.PaintRadiusWorld;

    [SerializeField, Min(0f)] private float _damageRadius = 1.5f;
    [SerializeField, Min(0f)] private float _impactHealthDamage = 10f;
    [SerializeField, Min(0f)] private float _impactInfectionDamage = 5f;
    [SerializeField] private LayerMask _damageTargetMask;
    [SerializeField] private QueryTriggerInteraction _triggerInteraction = QueryTriggerInteraction.Collide;

    [Header("Arc Bomb Impact Telegraph")]
    [SerializeField] private bool _showImpactTelegraph = true;

    [Tooltip("PaintRadiusWorld makes the red warning circle match the poison puddle radius.")]
    [SerializeField] private ArcBombRadiusSource _telegraphRadiusSource = ArcBombRadiusSource.PaintRadiusWorld;

    [SerializeField, Min(0f)] private float _telegraphRadiusCustom = 1.5f;
    [SerializeField] private AreaAttackTelegraphStyle _impactTelegraphStyle = default;

    [Header("Arc Bomb Paint")]
    [SerializeField] private MaskRenderManager.PaintChannel _paintChannel = MaskRenderManager.PaintChannel.PoisonPuddle;
    [SerializeField, Min(0f)] private float _paintRadiusWorld = 1.5f;
    [SerializeField] private int _paintPriority = 0;
    [SerializeField] private PoisonPuddleDamageConfigSO _poisonPuddleDamageConfig;

    [Header("Pattern Failed Bomb")]
    [SerializeField] private bool _fireBombOnPatternFailed = true;

    [Tooltip("Optional. If null, Arc Bomb Prefab is used.")]
    [SerializeField] private EnemyArcBombProjectile _failureArcBombPrefabOverride;

    [Tooltip("If true, failure bomb is aimed at Mutarus CurrentSector center.")]
    [SerializeField] private bool _failureBombTargetSectorCenter = true;

    [SerializeField, Min(0.01f)] private float _failureBombTravelTime = 1.5f;
    [SerializeField, Min(0f)] private float _failureBombArcHeight = 6f;
    [SerializeField] private float _failureBombSpawnYOffset = 0.5f;
    [SerializeField] private float _failureBombTargetYOffset = 0f;

    [Header("Pattern Failed Bomb Impact Damage")]
    [Tooltip("PaintRadiusWorld makes impact damage radius match the failed bomb poison puddle radius.")]
    [SerializeField] private ArcBombRadiusSource _failureDamageRadiusSource = ArcBombRadiusSource.PaintRadiusWorld;

    [SerializeField, Min(0f)] private float _failureDamageRadius = 5f;
    [SerializeField, Min(0f)] private float _failureImpactHealthDamage = 25f;
    [SerializeField, Min(0f)] private float _failureImpactInfectionDamage = 20f;

    [Header("Pattern Failed Bomb Impact Telegraph")]
    [Tooltip("PaintRadiusWorld makes the failed bomb warning circle match the failed poison puddle radius.")]
    [SerializeField] private ArcBombRadiusSource _failureTelegraphRadiusSource = ArcBombRadiusSource.PaintRadiusWorld;

    [SerializeField, Min(0f)] private float _failureTelegraphRadiusCustom = 5f;

    [Header("Pattern Failed Bomb Paint")]
    [SerializeField] private MaskRenderManager.PaintChannel _failurePaintChannel = MaskRenderManager.PaintChannel.PoisonPuddle;
    [SerializeField, Min(0f)] private float _failurePaintRadiusWorld = 5f;
    [SerializeField] private int _failurePaintPriority = 10;

    [Tooltip("Optional. If null, normal Poison Puddle Damage Config is used.")]
    [SerializeField] private PoisonPuddleDamageConfigSO _failurePoisonPuddleDamageConfig;

    [Header("Periodic Bomb Animator")]
    [SerializeField] private bool _triggerAnimatorOnBomb = true;
    [SerializeField] private string _bombAnimatorTrigger = "PatternBomb";

    [Header("Failure Bomb Animator")]
    [SerializeField] private bool _triggerAnimatorOnFailureBomb = true;
    [SerializeField] private string _failureBombAnimatorTrigger = "PatternFailedBomb";

    public bool UsePeriodicArcBomb => _usePeriodicArcBomb;
    public EnemyArcBombProjectile ArcBombPrefab => _arcBombPrefab;
    public MaskRenderManagerEventChannelSO MaskRenderManagerReadyChannel => _maskRenderManagerReadyChannel;

    public bool FireBombOnEnter => _fireBombOnEnter;
    public float FirstBombDelay => Mathf.Max(0f, _firstBombDelay);
    public float BombInterval => Mathf.Max(0.01f, _bombInterval);
    public int BombsPerBurst => Mathf.Max(1, _bombsPerBurst);
    public float BombShotInterval => Mathf.Max(0f, _bombShotInterval);

    public float BombTravelTime => Mathf.Max(0.01f, _bombTravelTime);
    public float BombArcHeight => Mathf.Max(0f, _bombArcHeight);
    public float BombSpawnYOffset => _bombSpawnYOffset;
    public float BombTargetYOffset => _bombTargetYOffset;
    public float FallbackTargetDistance => Mathf.Max(0f, _fallbackTargetDistance);
    public float TargetRandomRadius => Mathf.Max(0f, _targetRandomRadius);

    public bool DisableProjectileCollidersDuringFlight => _disableProjectileCollidersDuringFlight;

    public float DamageRadius => ResolveRadius(
        _damageRadiusSource,
        Mathf.Max(0f, _damageRadius),
        Mathf.Max(0f, _damageRadius),
        PaintRadiusWorld);

    public float ImpactHealthDamage => DifficultyRuntime.ApplyEnemyDamage(_impactHealthDamage);
    public float ImpactInfectionDamage => DifficultyRuntime.ApplyPlayerInfectionGain(_impactInfectionDamage);
    public LayerMask DamageTargetMask => _damageTargetMask;
    public QueryTriggerInteraction TriggerInteraction => _triggerInteraction;

    public bool ShowImpactTelegraph => _showImpactTelegraph;

    public float ImpactTelegraphRadius => ResolveRadius(
        _telegraphRadiusSource,
        Mathf.Max(0f, _telegraphRadiusCustom),
        DamageRadius,
        PaintRadiusWorld);

    public AreaAttackTelegraphStyle ImpactTelegraphStyle =>
        _impactTelegraphStyle.segments > 0 ? _impactTelegraphStyle : AreaAttackTelegraphStyle.Default;

    public MaskRenderManager.PaintChannel PaintChannel => _paintChannel;
    public float PaintRadiusWorld => DifficultyRuntime.ApplyEnemyVirusPaintRadius(_paintRadiusWorld);
    public int PaintPriority => _paintPriority;
    public PoisonPuddleDamageConfigSO PoisonPuddleDamageConfig => _poisonPuddleDamageConfig;

    public bool FireBombOnPatternFailed => _fireBombOnPatternFailed;
    public EnemyArcBombProjectile FailureArcBombPrefab =>
        _failureArcBombPrefabOverride != null ? _failureArcBombPrefabOverride : _arcBombPrefab;

    public bool FailureBombTargetSectorCenter => _failureBombTargetSectorCenter;
    public float FailureBombTravelTime => Mathf.Max(0.01f, _failureBombTravelTime);
    public float FailureBombArcHeight => Mathf.Max(0f, _failureBombArcHeight);
    public float FailureBombSpawnYOffset => _failureBombSpawnYOffset;
    public float FailureBombTargetYOffset => _failureBombTargetYOffset;

    public float FailurePaintRadiusWorld => DifficultyRuntime.ApplyEnemyVirusPaintRadius(_failurePaintRadiusWorld);

    public float FailureDamageRadius => ResolveRadius(
        _failureDamageRadiusSource,
        Mathf.Max(0f, _failureDamageRadius),
        Mathf.Max(0f, _failureDamageRadius),
        FailurePaintRadiusWorld);

    public float FailureImpactHealthDamage => DifficultyRuntime.ApplyEnemyDamage(_failureImpactHealthDamage);
    public float FailureImpactInfectionDamage => DifficultyRuntime.ApplyPlayerInfectionGain(_failureImpactInfectionDamage);

    public float FailureImpactTelegraphRadius => ResolveRadius(
        _failureTelegraphRadiusSource,
        Mathf.Max(0f, _failureTelegraphRadiusCustom),
        FailureDamageRadius,
        FailurePaintRadiusWorld);

    public MaskRenderManager.PaintChannel FailurePaintChannel => _failurePaintChannel;
    public int FailurePaintPriority => _failurePaintPriority;
    public PoisonPuddleDamageConfigSO FailurePoisonPuddleDamageConfig =>
        _failurePoisonPuddleDamageConfig != null
            ? _failurePoisonPuddleDamageConfig
            : _poisonPuddleDamageConfig;

    public bool TriggerAnimatorOnBomb => _triggerAnimatorOnBomb;
    public string BombAnimatorTrigger => _bombAnimatorTrigger;

    public bool TriggerAnimatorOnFailureBomb => _triggerAnimatorOnFailureBomb;
    public string FailureBombAnimatorTrigger => _failureBombAnimatorTrigger;

    private static float ResolveRadius(
        ArcBombRadiusSource source,
        float customRadius,
        float damageRadius,
        float paintRadiusWorld)
    {
        switch (source)
        {
            case ArcBombRadiusSource.DamageRadius:
                return Mathf.Max(0f, damageRadius);

            case ArcBombRadiusSource.Custom:
                return Mathf.Max(0f, customRadius);

            case ArcBombRadiusSource.PaintRadiusWorld:
            default:
                return Mathf.Max(0f, paintRadiusWorld);
        }
    }
}
