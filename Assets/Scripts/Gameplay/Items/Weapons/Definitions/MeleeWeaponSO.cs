/*
 * Role hierarchy: Gameplay > Items > Weapons > Definitions
 * Primary responsibility: Defines the MeleeWeaponSO data asset used to configure runtime rules in the Inspector.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using UnityEngine;

[CreateAssetMenu(
    fileName = "MeleeWeapon",
    menuName = "Game/Character Item/Weapon/WIP/Melee Weapon")]
public class MeleeWeaponSO : WeaponSO, IMeleeAttackProfile
{
    [Header("WIP - Legacy / NPC Melee (not player equipment)")]
    [Tooltip("Base HP damage dealt by one melee swing before armor reduction.")]
    [SerializeField, Min(0f)] private float _damage = 10f;

    [Tooltip("Penetration class used when armor resolves the final HP damage multiplier.")]
    [SerializeField, Min(0)] private float _penetrationClass = 0;

    [Tooltip("Durability loss multiplier applied to HP damage absorbed by armor.")]
    [SerializeField, Min(0f)] private float _armorHealthDurabilityLossMultiplier = 1f;

    [Header("Melee Shape")]
    [Tooltip("Maximum world distance from the swing origin to a target.")]
    [SerializeField, Min(0.01f)] private float _range = 2f;

    [Tooltip("Swing cone angle in degrees. Targets outside this cone are ignored.")]
    [SerializeField, Range(1f, 180f)] private float _arcAngle = 80f;

    [Tooltip("Extra overlap radius used to catch targets near the edge of the swing.")]
    [SerializeField, Min(0.01f)] private float _hitRadius = 0.5f;

    [Header("Timing")]
    [Tooltip("Minimum seconds between melee swings.")]
    [SerializeField, Min(0.01f)] private float _cooldownSeconds = 0.6f;

    [Tooltip("Seconds from attack acceptance and the Animator MeleeAttack trigger until the locked melee hit volume becomes active. Match this to the visual weapon wind-up.")]
    [SerializeField, Min(0f)] private float _hitDelaySeconds = 0.08f;

    [Tooltip("Seconds during which the locked overlap box is sampled for targets. Each Damageable can still be hit only once per swing.")]
    [SerializeField, Min(0.01f)] private float _activeHitSeconds = 0.12f;

    [Header("Damage Targets")]
    [Tooltip("Layers damaged when this melee weapon is used by the player.")]
    [SerializeField] private LayerMask _playerDamageTargetMask;

    [Tooltip("Layers damaged when this melee weapon is used by an enemy shooter.")]
    [SerializeField] private LayerMask _enemyDamageTargetMask;

    [Header("Physics")]
    [Tooltip("Trigger handling for melee overlap checks. Shooter hurtboxes use trigger colliders, so Collide is the normal setting.")]
    [SerializeField] private QueryTriggerInteraction _triggerInteraction = QueryTriggerInteraction.Collide;

    [Header("Legacy Audio Fallback")]
    [Tooltip("Used only when Audio Profile has no Melee Swing Cue.")]
    [SerializeField] private AudioCueSO _swingAudioCue;

    [Header("Swing Visual")]
    [Tooltip("Optional particle prefab spawned from MeleeWeaponView Swing Effect Origin when the swing starts.")]
    [SerializeField] private ParticleSystem _swingParticlePrefab;

    [Tooltip("Optional world-space particle trail spawned alongside the primary swing visual.")]
    [SerializeField] private ParticleSystem _swingTrailParticlePrefab;

    public float Damage => Mathf.Max(0f, _damage);
    public float PenetrationClass => Mathf.Max(0, _penetrationClass);
    public float ArmorHealthDurabilityLossMultiplier => Mathf.Max(0f, _armorHealthDurabilityLossMultiplier);
    public float Range => Mathf.Max(0.01f, _range);
    public float ArcAngle => Mathf.Clamp(_arcAngle, 1f, 180f);
    public float HitRadius => Mathf.Max(0.01f, _hitRadius);
    public float CooldownSeconds => Mathf.Max(0.01f, _cooldownSeconds);
    public float HitDelaySeconds => Mathf.Max(0f, _hitDelaySeconds);
    public float ActiveHitSeconds => Mathf.Max(0.01f, _activeHitSeconds);
    public QueryTriggerInteraction TriggerInteraction => _triggerInteraction;
    public AudioCueSO SwingAudioCue =>
        AudioProfile != null && AudioProfile.MeleeSwingCue != null
            ? AudioProfile.MeleeSwingCue
            : _swingAudioCue;
    public ParticleSystem SwingParticlePrefab => _swingParticlePrefab;
    public ParticleSystem SwingTrailParticlePrefab => _swingTrailParticlePrefab;
    public string ProfileName => name;

    // Purpose: Resolves the damage target mask needed by this component.
    public LayerMask ResolveDamageTargetMask(ShooterFaction faction)
    {
        return faction != ShooterFaction.Player
            ? _enemyDamageTargetMask
            : _playerDamageTargetMask;
    }
}
