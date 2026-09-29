/*
 * Role hierarchy: Gameplay > Entities > Shooter > Common > Core
 * Primary responsibility: Defines the UnarmedAttackProfileSO data asset used to configure runtime rules in the Inspector.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using UnityEngine;

[CreateAssetMenu(
    fileName = "UnarmedAttackProfile",
    menuName = "Game/Character/Unarmed Attack Profile")]
public sealed class UnarmedAttackProfileSO : ScriptableObject, IMeleeAttackProfile
{
    [Header("Damage")]
    [SerializeField, Min(0f)] private float _damage = 5f;
    [SerializeField, Min(0)] private int _penetrationClass;
    [SerializeField, Min(0f)] private float _armorHealthDurabilityLossMultiplier = 1f;

    [Header("Shape")]
    [SerializeField, Min(0.01f)] private float _range = 5f;
    [SerializeField, Range(1f, 180f)] private float _arcAngle = 80f;
    [SerializeField, Min(0.01f)] private float _hitRadius = 3f;

    [Header("Timing")]
    [SerializeField, Min(0.01f)] private float _cooldownSeconds = 0.8f;
    [SerializeField, Min(0f)] private float _hitDelaySeconds = 0.1f;
    [SerializeField, Min(0.01f)] private float _activeHitSeconds = 0.1f;

    [Header("Targets")]
    [SerializeField] private LayerMask _playerDamageTargetMask;
    [SerializeField] private LayerMask _enemyDamageTargetMask;
    [SerializeField] private QueryTriggerInteraction _triggerInteraction = QueryTriggerInteraction.Collide;

    [Header("Feedback")]
    [SerializeField] private AudioCueSO _swingAudioCue;
    [SerializeField] private ParticleSystem _swingParticlePrefab;
    [SerializeField] private ParticleSystem _swingTrailParticlePrefab;

    public string ProfileName => name;
    public float Damage => Mathf.Max(0f, _damage);
    public int PenetrationClass => Mathf.Max(0, _penetrationClass);
    public float ArmorHealthDurabilityLossMultiplier => Mathf.Max(0f, _armorHealthDurabilityLossMultiplier);
    public float Range => Mathf.Max(0.01f, _range);
    public float ArcAngle => Mathf.Clamp(_arcAngle, 1f, 180f);
    public float HitRadius => Mathf.Max(0.01f, _hitRadius);
    public float CooldownSeconds => Mathf.Max(0.01f, _cooldownSeconds);
    public float HitDelaySeconds => Mathf.Max(0f, _hitDelaySeconds);
    public float ActiveHitSeconds => Mathf.Max(0.01f, _activeHitSeconds);
    public QueryTriggerInteraction TriggerInteraction => _triggerInteraction;
    public AudioCueSO SwingAudioCue => _swingAudioCue;
    public ParticleSystem SwingParticlePrefab => _swingParticlePrefab;
    public ParticleSystem SwingTrailParticlePrefab => _swingTrailParticlePrefab;

    // Purpose: Resolves the damage target mask needed by this component.
    public LayerMask ResolveDamageTargetMask(ShooterFaction faction)
    {
        return faction == ShooterFaction.Enemy
            ? _enemyDamageTargetMask
            : _playerDamageTargetMask;
    }
}
