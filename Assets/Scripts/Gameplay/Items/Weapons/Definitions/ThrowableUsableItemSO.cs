/*
 * Role hierarchy: Gameplay > Items > Weapons > Definitions
 * Primary responsibility: Defines the ThrowableUsableItemSO data asset used to configure runtime rules in the Inspector.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using UnityEngine;

[CreateAssetMenu(
    fileName = "ThrowableUsableItem",
    menuName = "Game/Character Item/Usable/Throwable")]
public class ThrowableUsableItemSO : UsableItemSO
{
    [Header("Throw")]
    [Tooltip("Flat throw speed toward the aim point.")]
    [SerializeField, Min(0.1f)] private float _throwSpeed = 16f;
    [Tooltip("Upward velocity added to the throw for an arc.")]
    [SerializeField, Min(0f)] private float _upwardVelocity = 6f;
    [Tooltip("Random spin torque applied to the projectile rigidbody.")]
    [SerializeField, Min(0f)] private float _spinTorque = 4f;

    [Header("Close-Range Detonation")]
    [Tooltip("When enabled, an aim point this close to the throw origin detonates at the aim point immediately instead of forcing a full arc and fuse.")]
    [SerializeField] private bool _detonateImmediatelyAtCloseRange = true;

    [Tooltip("Planar distance from the throw origin that counts as a close-range throw. Use this for throwing at the shooter's feet.")]
    [SerializeField, Min(0f)] private float _immediateDetonationDistance = 2.5f;

    [Header("Fuse")]
    [SerializeField, Min(0.01f)] private float _fuseSeconds = 2f;
    [SerializeField] private bool _explodeOnImpact;

    [Header("Projectile Sound Stimuli")]
    [SerializeField, Min(0f)] private float _projectileFlybySoundRadius = 3f;
    [SerializeField, Min(0.1f)] private float _projectileFlybyEmitDistance = 3f;
    [SerializeField, Min(0f)] private float _projectileFlybySoundIntensity = 0.5f;
    [SerializeField, Min(0f)] private float _projectileImpactSoundRadius = 10f;
    [SerializeField, Min(0f)] private float _projectileImpactSoundIntensity = 0.8f;
    [SerializeField, Min(0f)] private float _projectileImpactSoundCooldown = 0.2f;
    [SerializeField, Min(0f)] private float _explosionSoundRadius = 35f;
    [SerializeField, Min(0f)] private float _explosionSoundIntensity = 1f;
    [SerializeField, Min(0f)] private float _soundInvestigateDelaySeconds = 0.05f;

    [Header("Explosion")]
    [SerializeField, Min(0.01f)] private float _explosionRadius = 4f;
    [SerializeField, Min(0f)] private float _healthDamage;
    [SerializeField, Min(0f)] private float _infectionDamage;
    [SerializeField, Min(0)] private float _penetrationClass;
    [SerializeField, Min(0f)] private float _armorHealthDurabilityLossMultiplier = 1f;
    [SerializeField, Min(0f)] private float _armorInfectionDurabilityLossMultiplier = 0.5f;

    [Header("Impact Status Effect")]
    [Tooltip("Optional effect applied once to every valid target caught in the explosion. Flashbangs use this for their stun.")]
    [SerializeField] private StatusEffectDefinitionSO _impactStatusEffect;
    [Tooltip("Optional duration override for the impact effect. Zero uses the Status Effect Definition duration.")]
    [SerializeField, Min(0f)] private float _impactStatusDurationSeconds;

    [Header("Damage Targets")]
    [SerializeField] private LayerMask _playerDamageTargetMask;
    [SerializeField] private LayerMask _enemyDamageTargetMask;
    [SerializeField] private QueryTriggerInteraction _triggerInteraction = QueryTriggerInteraction.Ignore;

    [Header("Mask Render")]
    [SerializeField] private MaskRenderSO _playerMaskRender;
    [SerializeField] private MaskRenderSO _enemyMaskRender;
    [SerializeField] private PaintChannel _playerFallbackChannel = PaintChannel.Vaccine;
    [SerializeField] private PaintChannel _enemyFallbackChannel = PaintChannel.Virus;
    [SerializeField] private int _paintPriority;

    [Header("Readable Impact Zone")]
    [Tooltip("Independent world-space circle shown even when SectorPaint or MaskRenderManager is unavailable.")]
    [SerializeField] private bool _showImpactZoneVisual = true;

    [Tooltip("Color of the temporary impact circle. Grenades should use a readable purple with non-zero alpha.")]
    [SerializeField] private Color _impactZoneVisualColor =
        new Color(0.68f, 0.12f, 1f, 0.48f);

    [Tooltip("Seconds the grenade impact circle remains visible. This does not change explosion gameplay.")]
    [SerializeField, Min(0.05f)] private float _impactZoneVisualDurationSeconds = 2f;

    [Header("Legacy Audio Fallback")]
    [Tooltip("Optional authored throwable audio handoff. When a cue is assigned here, it overrides the matching legacy cue field below.")]
    [SerializeField] private WeaponAudioProfileSO _audioProfile;

    [Tooltip("Used only when Audio Profile has no Throw Cue.")]
    [SerializeField] private AudioCueSO _throwAudioCue;

    [Tooltip("Used only when Audio Profile has no Explosion Cue.")]
    [SerializeField] private AudioCueSO _explosionAudioCue;

    public float ThrowSpeed => Mathf.Max(0.1f, _throwSpeed);
    public float UpwardVelocity => Mathf.Max(0f, _upwardVelocity);
    public float SpinTorque => Mathf.Max(0f, _spinTorque);
    public bool DetonateImmediatelyAtCloseRange => _detonateImmediatelyAtCloseRange;
    public float ImmediateDetonationDistance => Mathf.Max(0f, _immediateDetonationDistance);
    public float FuseSeconds => Mathf.Max(0.01f, _fuseSeconds);
    public bool ExplodeOnImpact => _explodeOnImpact;
    public float ProjectileFlybySoundRadius => Mathf.Max(0f, _projectileFlybySoundRadius);
    public float ProjectileFlybyEmitDistance => Mathf.Max(0.1f, _projectileFlybyEmitDistance);
    public float ProjectileFlybySoundIntensity => Mathf.Max(0f, _projectileFlybySoundIntensity);
    public float ProjectileImpactSoundRadius => Mathf.Max(0f, _projectileImpactSoundRadius);
    public float ProjectileImpactSoundIntensity => Mathf.Max(0f, _projectileImpactSoundIntensity);
    public float ProjectileImpactSoundCooldown => Mathf.Max(0f, _projectileImpactSoundCooldown);
    public float ExplosionSoundRadius => Mathf.Max(0f, _explosionSoundRadius);
    public float ExplosionSoundIntensity => Mathf.Max(0f, _explosionSoundIntensity);
    public float SoundInvestigateDelaySeconds => Mathf.Max(0f, _soundInvestigateDelaySeconds);
    public float ExplosionRadius => Mathf.Max(0.01f, _explosionRadius);
    public float HealthDamage => Mathf.Max(0f, _healthDamage);
    [SerializeField, Min(0f)] private float _infectionExposureOnImpact;
    public float InfectionExposureOnImpact => Mathf.Max(0f, _infectionExposureOnImpact);
    public float InfectionDamage => Mathf.Max(0f, _infectionDamage);
    public float PenetrationClass => Mathf.Max(0, _penetrationClass);
    public float ArmorHealthDurabilityLossMultiplier => Mathf.Max(0f, _armorHealthDurabilityLossMultiplier);
    public float ArmorInfectionDurabilityLossMultiplier => Mathf.Max(0f, _armorInfectionDurabilityLossMultiplier);
    public StatusEffectDefinitionSO ImpactStatusEffect => _impactStatusEffect;
    public float ImpactStatusDurationSeconds => Mathf.Max(0f, _impactStatusDurationSeconds);
    public QueryTriggerInteraction TriggerInteraction => _triggerInteraction;
    public int PaintPriority => _paintPriority;
    public WeaponAudioProfileSO AudioProfile => _audioProfile;
    public AudioCueSO ThrowAudioCue =>
        AudioProfile != null && AudioProfile.ThrowCue != null
            ? AudioProfile.ThrowCue
            : _throwAudioCue;

    public AudioCueSO ExplosionAudioCue =>
        AudioProfile != null && AudioProfile.ExplosionCue != null
            ? AudioProfile.ExplosionCue
            : _explosionAudioCue;
    public virtual bool IsHostileThreat => true;
    public bool ShowImpactZoneVisual => _showImpactZoneVisual;
    public virtual Color ImpactZoneVisualColor => _impactZoneVisualColor;
    public virtual float ImpactZoneVisualDurationSeconds =>
        Mathf.Max(0.05f, _impactZoneVisualDurationSeconds);

    // Purpose: Resolves the damage target mask needed by this component.
    public LayerMask ResolveDamageTargetMask(ShooterFaction faction) =>
        faction != ShooterFaction.Player ? _enemyDamageTargetMask : _playerDamageTargetMask;

    // Purpose: Resolves the mask render needed by this component.
    public MaskRenderSO ResolveMaskRender(ShooterFaction faction) =>
        faction != ShooterFaction.Player ? _enemyMaskRender : _playerMaskRender;

    // Purpose: Resolves the fallback paint channel needed by this component.
    public PaintChannel ResolveFallbackPaintChannel(ShooterFaction faction) =>
        faction != ShooterFaction.Player ? _enemyFallbackChannel : _playerFallbackChannel;

    // Purpose: Performs the should detonate immediately workflow for this component.
    public bool ShouldDetonateImmediately(Vector3 origin, Vector3 targetPoint)
    {
        if (!_detonateImmediatelyAtCloseRange)
            return false;

        Vector3 offset = targetPoint - origin;
        offset.y = 0f;
        float distance = ImmediateDetonationDistance;
        return offset.sqrMagnitude <= distance * distance;
    }
}
