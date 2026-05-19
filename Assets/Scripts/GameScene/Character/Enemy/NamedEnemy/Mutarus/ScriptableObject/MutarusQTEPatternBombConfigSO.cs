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
    [SerializeField] private bool _fireBombOnEnter = false;
    [SerializeField, Min(0f)] private float _firstBombDelay = 2f;
    [SerializeField, Min(0.01f)] private float _bombInterval = 5f;

    [Header("Arc Bomb Trajectory")]
    [SerializeField, Min(0.01f)] private float _bombTravelTime = 1.2f;
    [SerializeField, Min(0f)] private float _bombArcHeight = 4f;
    [SerializeField] private float _bombSpawnYOffset = 0.5f;
    [SerializeField] private float _bombTargetYOffset = 0f;
    [SerializeField, Min(0f)] private float _fallbackTargetDistance = 6f;
    [SerializeField, Min(0f)] private float _targetRandomRadius = 0f;

    [Header("Arc Bomb Impact Damage")]
    [SerializeField, Min(0f)] private float _damageRadius = 1.5f;
    [SerializeField, Min(0f)] private float _impactHealthDamage = 10f;
    [SerializeField, Min(0f)] private float _impactInfectionDamage = 5f;
    [SerializeField] private LayerMask _damageTargetMask;
    [SerializeField] private QueryTriggerInteraction _triggerInteraction = QueryTriggerInteraction.Collide;

    [Header("Arc Bomb Paint")]
    [SerializeField] private MaskRenderManager.PaintChannel _paintChannel = MaskRenderManager.PaintChannel.PoisonPuddle;
    [SerializeField, Min(0f)] private float _paintRadiusWorld = 1.5f;
    [SerializeField] private int _paintPriority = 0;
    [SerializeField] private PoisonPuddleDamageConfigSO _poisonPuddleDamageConfig;

    [Header("Periodic Bomb Animator")]
    [SerializeField] private bool _triggerAnimatorOnBomb = true;
    [SerializeField] private string _bombAnimatorTrigger = "PatternBomb";

    public bool UsePeriodicArcBomb => _usePeriodicArcBomb;
    public EnemyArcBombProjectile ArcBombPrefab => _arcBombPrefab;
    public MaskRenderManagerEventChannelSO MaskRenderManagerReadyChannel => _maskRenderManagerReadyChannel;
    public bool FireBombOnEnter => _fireBombOnEnter;
    public float FirstBombDelay => Mathf.Max(0f, _firstBombDelay);
    public float BombInterval => Mathf.Max(0.01f, _bombInterval);
    public float BombTravelTime => Mathf.Max(0.01f, _bombTravelTime);
    public float BombArcHeight => Mathf.Max(0f, _bombArcHeight);
    public float BombSpawnYOffset => _bombSpawnYOffset;
    public float BombTargetYOffset => _bombTargetYOffset;
    public float FallbackTargetDistance => Mathf.Max(0f, _fallbackTargetDistance);
    public float TargetRandomRadius => Mathf.Max(0f, _targetRandomRadius);
    public float DamageRadius => Mathf.Max(0f, _damageRadius);
    public float ImpactHealthDamage => DifficultyRuntime.ApplyEnemyDamage(_impactHealthDamage);
    public float ImpactInfectionDamage => DifficultyRuntime.ApplyPlayerInfectionGain(_impactInfectionDamage);
    public LayerMask DamageTargetMask => _damageTargetMask;
    public QueryTriggerInteraction TriggerInteraction => _triggerInteraction;
    public MaskRenderManager.PaintChannel PaintChannel => _paintChannel;
    public float PaintRadiusWorld => DifficultyRuntime.ApplyEnemyVirusPaintRadius(_paintRadiusWorld);
    public int PaintPriority => _paintPriority;
    public PoisonPuddleDamageConfigSO PoisonPuddleDamageConfig => _poisonPuddleDamageConfig;
    public bool TriggerAnimatorOnBomb => _triggerAnimatorOnBomb;
    public string BombAnimatorTrigger => _bombAnimatorTrigger;
}
