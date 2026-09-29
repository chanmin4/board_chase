/*
 * Role hierarchy: Gameplay > Entities > Shooter > Common > Core
 * Primary responsibility: Handles ShooterMeleeAttack combat resolution, damage, projectile, or status changes.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class ShooterMeleeAttack : MonoBehaviour
{
    private const int MaxHits = 32;

    [Header("Refs")]
    [SerializeField] private ShooterEquipmentController _equipmentController;
    [SerializeField] private EntityShooterCharacterAudio _audio;

    [Tooltip("Shooter-owned melee box. The attack pose is locked when input is accepted and cannot follow later aim rotation.")]
    [SerializeField] private ShooterMeleeHitZone _hitZone;

    [Header("Unarmed Fallback")]
    [Tooltip("Non-item swing shape/timing. Players reuse it for fists and guns with no fireable ammunition; enemies retain their original unarmed fallback.")]
    [SerializeField] private UnarmedAttackProfileSO _unarmedProfile;

    [Header("Runtime Melee Shape")]
    [Tooltip(
        "MeleeWeaponSO currently copied into the live tuning values below. " +
        "This reference changes automatically when the equipped melee weapon changes.")]
    [SerializeField] private MeleeWeaponSO _runtimeSourceWeapon;

    [Tooltip(
        "Live attack reach in world units. It is copied once when a different MeleeWeaponSO is equipped, " +
        "then can be adjusted during Play Mode for the next swing.")]
    [SerializeField, Min(0.01f)] private float _runtimeRange = 2f;

    [Tooltip(
        "Live half-width of the overlap box. The complete left-to-right width is twice this value.")]
    [SerializeField, Min(0.01f)] private float _runtimeHitRadius = 0.5f;

    [Tooltip("Live horizontal cone angle used to reject targets outside the swing arc.")]
    [SerializeField, Range(1f, 180f)] private float _runtimeArcAngle = 80f;

    [Header("Hit Volume")]
    [Tooltip("Vertical offset from the locked attack origin to the melee overlap volume.")]
    [SerializeField, Min(0f)] private float _verticalOffset = 0.4f;

    [Tooltip("World height of the melee overlap box.")]
    [SerializeField, Min(0.01f)] private float _verticalSize = 1f;

    [Header("Swing Particles")]
    [Tooltip(
        "Hierarchy parent and authoring offset for melee particles. Create this as a child of the Melee/Combat object. " +
        "Its local X is attack-right, local Y is up, and local Z is attack-forward. Rotate this transform to correct " +
        "the particle prefab axis. The final world pose is locked when the attack input is accepted.")]
    [SerializeField] private Transform _particleContainerRoot;

    [Tooltip(
        "World rotation correction applied after the locked attack direction and the particle container rotation. " +
        "Use the Scene gizmo axes while selected; Y = 180 flips a slash prefab that faces backward. " +
        "This value can be adjusted during Play Mode without changing the locked attack direction.")]
    [SerializeField] private Vector3 _particleRotationOffsetEuler = new(0f, 180f, 0f);

    [Header("Particle Auto Scale")]
    [Tooltip(
        "Attack range represented by a slash particle at its authored prefab scale. " +
        "For example, 2 means a prefab at scale 1 is treated as a 2-world-unit slash.")]
    [SerializeField, Min(0.01f)] private float _particleReferenceRange = 2f;

    [Tooltip(
        "Hit radius represented by a slash particle at its authored prefab scale. " +
        "The larger of the range ratio and radius ratio becomes the uniform particle scale.")]
    [SerializeField, Min(0.01f)] private float _particleReferenceHitRadius = 0.5f;

    [Tooltip(
        "Final uniform multiplier after matching the particle to the live attack range and radius. " +
        "Use one value here for visual fine tuning instead of resizing every melee particle prefab.")]
    [SerializeField, Min(0.01f)] private float _particleScaleMultiplier = 1f;

    [Header("Debug")]
    [Tooltip("Logs rejected attacks, accepted swing shape/timing, overlap candidates, and every damaged target.")]
    [SerializeField] private bool _debugLogs;

    [Header("Debug Gizmo")]
    [Tooltip("Scene-view hit-volume color while the damage window is inactive.")]
    [SerializeField] private Color _idleGizmoColor =
        new(1f, 0.65f, 0f, 0.8f);

    [Tooltip("Scene-view hit-volume color while the damage window is active.")]
    [SerializeField] private Color _activeGizmoColor =
        new(1f, 0.1f, 0.1f, 1f);

    private readonly Collider[] _hits = new Collider[MaxHits];
    private readonly HashSet<Damageable> _damagedThisSwing = new();
    private Coroutine _attackRoutine;
    private PlayerStatsRuntime _playerStats;
    private PlayerBulletLoadoutRuntime _playerAmmo;
    private float _nextAttackTime;
    private int _attackSequence;
    private int _candidateSamples;
    private int _rejectedNoDamageable;
    private int _rejectedUnavailable;
    private int _rejectedAlreadyDamaged;
    private int _rejectedOwnEntity;
    private int _rejectedDistance;
    private int _rejectedAngle;
    private bool _hasDebugParticlePose;
    private Vector3 _lastAttackOrigin;
    private Vector3 _lastParticlePosition;
    private Quaternion _lastParticleRotation = Quaternion.identity;
    private float _lastParticleScale = 1f;

    /// <summary>
    /// Increases once whenever an attack is accepted. ShooterAnimatorParameterAction
    /// observes this value and raises the shared MeleeAttack Animator trigger.
    /// </summary>
    public int AttackSequence => _attackSequence;
    public bool IsAttacking => _attackRoutine != null;
    public ShooterMeleeHitZone HitZone => _hitZone;
    public Transform ParticleContainerRoot => _particleContainerRoot;
    public MeleeWeaponSO RuntimeSourceWeapon => _runtimeSourceWeapon;
    public float RuntimeRange => Mathf.Max(0.01f, _runtimeRange);
    public float RuntimeHitRadius => Mathf.Max(0.01f, _runtimeHitRadius);
    public float RuntimeArcAngle => Mathf.Clamp(_runtimeArcAngle, 1f, 180f);
    public bool CanAttackCurrentWeaponOrUnarmed
    {
        get
        {
            WeaponSO current = _equipmentController != null ? _equipmentController.CurrentWeapon : null;
            return current is MeleeWeaponSO || current == null && _unarmedProfile != null;
        }
    }
    public float CurrentAttackRange
    {
        get
        {
            MeleeWeaponSO weapon = _equipmentController != null
                ? _equipmentController.CurrentWeapon as MeleeWeaponSO
                : null;
            return weapon != null
                ? weapon.Range
                : _unarmedProfile != null ? _unarmedProfile.Range : 0f;
        }
    }

    private readonly struct MeleeShapeSnapshot
    {
        public readonly float Range;
        public readonly float HitRadius;
        public readonly float ArcAngle;
        public readonly float VerticalOffset;
        public readonly float VerticalSize;

        public MeleeShapeSnapshot(
            float range,
            float hitRadius,
            float arcAngle,
            float verticalOffset,
            float verticalSize)
        {
            Range = Mathf.Max(0.01f, range);
            HitRadius = Mathf.Max(0.01f, hitRadius);
            ArcAngle = Mathf.Clamp(arcAngle, 1f, 180f);
            VerticalOffset = Mathf.Max(0f, verticalOffset);
            VerticalSize = Mathf.Max(0.01f, verticalSize);
        }
    }

    // Purpose: Initializes Inspector defaults for this component.
    protected virtual void Reset()
    {
        ResolveRefs();
    }

    // Purpose: Caches the component references required before runtime initialization.
    protected virtual void Awake()
    {
        ResolveRefs();
        RefreshRuntimeShapeFromEquippedWeapon();
        ApplyRuntimeShapeToHitZone();
    }

    // Purpose: Processes this component's per-frame runtime work.
    protected virtual void Update()
    {
        RefreshRuntimeShapeIfWeaponChanged();
        ApplyRuntimeShapeToHitZone();
    }

    // Purpose: Normalizes Inspector-authored data after it changes in the Editor.
    protected virtual void OnValidate()
    {
        _runtimeRange = Mathf.Max(0.01f, _runtimeRange);
        _runtimeHitRadius = Mathf.Max(0.01f, _runtimeHitRadius);
        _runtimeArcAngle = Mathf.Clamp(_runtimeArcAngle, 1f, 180f);
        _verticalOffset = Mathf.Max(0f, _verticalOffset);
        _verticalSize = Mathf.Max(0.01f, _verticalSize);
        _particleReferenceRange = Mathf.Max(0.01f, _particleReferenceRange);
        _particleReferenceHitRadius = Mathf.Max(0.01f, _particleReferenceHitRadius);
        _particleScaleMultiplier = Mathf.Max(0.01f, _particleScaleMultiplier);

        if (_hitZone == null)
            _hitZone = GetComponent<ShooterMeleeHitZone>();

        ApplyRuntimeShapeToHitZone();
    }

    // Purpose: Releases active subscriptions and transient state while disabled.
    protected virtual void OnDisable()
    {
        if (_attackRoutine != null)
        {
            StopCoroutine(_attackRoutine);
            _attackRoutine = null;
        }

        _hitZone?.SetHitWindowActive(false);
        _damagedThisSwing.Clear();
    }

    // Purpose: Resolves the refs needed by this component.
    protected void ResolveRefs()
    {
        if (_equipmentController == null)
            _equipmentController = EntityComponentResolver.Resolve<ShooterEquipmentController>(this);

        if (_audio == null)
            _audio = EntityComponentResolver.Resolve<EntityShooterCharacterAudio>(this);

        if (_hitZone == null)
            _hitZone = GetComponent<ShooterMeleeHitZone>();
    }

    [ContextMenu("Reload Runtime Shape From Equipped Weapon")]
    // Purpose: Refreshes the displayed or cached runtime shape from equipped weapon.
    private void RefreshRuntimeShapeFromEquippedWeapon()
    {
        ResolveRefs();

        MeleeWeaponSO equippedWeapon = _equipmentController != null
            ? _equipmentController.CurrentWeapon as MeleeWeaponSO
            : null;

        SetRuntimeShapeSource(equippedWeapon);
    }

    // Purpose: Refreshes the displayed or cached runtime shape if weapon changed.
    private void RefreshRuntimeShapeIfWeaponChanged()
    {
        MeleeWeaponSO equippedWeapon = _equipmentController != null
            ? _equipmentController.CurrentWeapon as MeleeWeaponSO
            : null;

        if (equippedWeapon == _runtimeSourceWeapon)
            return;

        SetRuntimeShapeSource(equippedWeapon);
    }

    // Purpose: Ensures runtime shape source is ready before the workflow continues.
    private void EnsureRuntimeShapeSource(IMeleeAttackProfile profile)
    {
        MeleeWeaponSO weapon = profile as MeleeWeaponSO;

        bool usesUnarmedProfile =
            (profile as UnityEngine.Object) == _unarmedProfile;

        if (weapon != _runtimeSourceWeapon || usesUnarmedProfile || profile is PlayerEmergencyMeleeProfile)
            SetRuntimeShapeSource(profile);
    }

    // Purpose: Updates the runtime shape source state.
    private void SetRuntimeShapeSource(MeleeWeaponSO weapon)
    {
        SetRuntimeShapeSource((IMeleeAttackProfile)weapon);
    }

    // Purpose: Updates the runtime shape source state.
    private void SetRuntimeShapeSource(IMeleeAttackProfile profile)
    {
        _runtimeSourceWeapon = profile as MeleeWeaponSO;

        if (profile == null)
            return;

        _runtimeRange = profile.Range;
        _runtimeHitRadius = profile.HitRadius;
        _runtimeArcAngle = profile.ArcAngle;
        ApplyRuntimeShapeToHitZone();
    }

    // Purpose: Performs the capture runtime shape workflow for this component.
    private MeleeShapeSnapshot CaptureRuntimeShape()
    {
        return new MeleeShapeSnapshot(
            RuntimeRange,
            RuntimeHitRadius,
            RuntimeArcAngle,
            _verticalOffset,
            _verticalSize);
    }

    // Purpose: Applies the requested runtime shape to hit zone change.
    private void ApplyRuntimeShapeToHitZone()
    {
        if (_hitZone == null)
            return;

        _hitZone.ConfigureShape(
            RuntimeRange,
            RuntimeHitRadius,
            RuntimeArcAngle,
            _verticalOffset,
            _verticalSize);
    }

    // Purpose: Attempts to attack current weapon and reports whether the request succeeded.
    public bool TryAttackCurrentWeapon(
        ShooterFaction faction,
        Vector3 origin,
        Vector3 direction)
    {
        ResolveRefs();

        if (faction == ShooterFaction.Player)
        {
            if (_playerStats == null) _playerStats = EntityComponentResolver.Resolve<PlayerStatsRuntime>(this);
            if (_playerAmmo == null) _playerAmmo = EntityComponentResolver.Resolve<PlayerBulletLoadoutRuntime>(this);
            if (_equipmentController == null || _unarmedProfile == null ||
                !PlayerEmergencyMeleeProfile.IsAvailable(_equipmentController.CurrentWeapon,
                    _playerAmmo != null && _playerAmmo.HasNoFireableAmmunition))
                return Reject("player emergency melee requires an empty slot or a gun with no fireable ammunition");
            return TryAttackProfile(new PlayerEmergencyMeleeProfile(_unarmedProfile,
                _playerStats != null ? _playerStats.MeleeDamage : 5f), faction, origin, direction);
        }

        MeleeWeaponSO weapon = _equipmentController != null
            ? _equipmentController.CurrentWeapon as MeleeWeaponSO
            : null;

        IMeleeAttackProfile profile = weapon;

        if (profile == null &&
            _equipmentController != null &&
            _equipmentController.CurrentWeapon == null)
        {
            profile = _unarmedProfile;
        }

        return TryAttackProfile(profile, faction, origin, direction);
    }

    // Purpose: Attempts to attack and reports whether the request succeeded.
    public bool TryAttack(
        MeleeWeaponSO weapon,
        ShooterFaction faction,
        Vector3 origin,
        Vector3 direction)
    {
        // Dedicated player melee weapons are retained as WIP; enemy behavior is unchanged.
        return faction != ShooterFaction.Player && TryAttackProfile(weapon, faction, origin, direction);
    }

    // Purpose: Attempts to attack profile and reports whether the request succeeded.
    private bool TryAttackProfile(
        IMeleeAttackProfile profile,
        ShooterFaction faction,
        Vector3 origin,
        Vector3 direction)
    {
        ResolveRefs();

        if (profile == null)
            return Reject("neither a melee weapon nor an unarmed profile is available");

        EnsureRuntimeShapeSource(profile);
        MeleeShapeSnapshot shape = CaptureRuntimeShape();

        if (_hitZone == null)
            return Reject("ShooterMeleeHitZone is missing");

        if (_attackRoutine != null)
            return Reject("previous melee hit window is still active");

        if (Time.time < _nextAttackTime)
        {
            return Reject(
                $"cooldown active, remaining={Mathf.Max(0f, _nextAttackTime - Time.time):0.###}s");
        }

        direction.y = 0f;

        if (direction.sqrMagnitude <= 0.0001f)
            direction = transform.forward;

        direction.y = 0f;
        direction.Normalize();

        float attackDuration = profile.HitDelaySeconds + profile.ActiveHitSeconds;
        _nextAttackTime = Time.time + Mathf.Max(profile.CooldownSeconds, attackDuration);
        _damagedThisSwing.Clear();
        _candidateSamples = 0;
        _rejectedNoDamageable = 0;
        _rejectedUnavailable = 0;
        _rejectedAlreadyDamaged = 0;
        _rejectedOwnEntity = 0;
        _rejectedDistance = 0;
        _rejectedAngle = 0;

        unchecked
        {
            _attackSequence++;
        }

        _hitZone.BeginSwing(
            origin,
            direction,
            shape.Range,
            shape.HitRadius,
            shape.ArcAngle,
            shape.VerticalOffset,
            shape.VerticalSize);

        Transform particleFollowTarget = ResolveParticleFollowTarget();

        ResolveParticlePose(
            origin,
            direction,
            particleFollowTarget,
            out Vector3 particlePosition,
            out Quaternion particleRotation);

        Vector3 particleFollowOffset = particleFollowTarget != null
            ? particlePosition - particleFollowTarget.position
            : Vector3.zero;

        float particleScale = ResolveParticleScale(shape);

        _hasDebugParticlePose = true;
        _lastAttackOrigin = origin;
        _lastParticlePosition = particlePosition;
        _lastParticleRotation = particleRotation;
        _lastParticleScale = particleScale;

        // Visual animation is driven only through the shared FSM animator action.
        // Audio starts immediately; particles and damage wait for the hit window.
        _audio?.PlayMeleeSwing(profile.SwingAudioCue);

        _attackRoutine = StartCoroutine(
            RunAttackWindow(
                profile,
                faction,
                shape,
                origin,
                direction,
                particlePosition,
                particleRotation,
                particleFollowTarget,
                particleFollowOffset,
                particleScale));

        if (_debugLogs)
        {
            Debug.Log(
                $"[ShooterMeleeAttack] Swing accepted. " +
                $"owner={ResolveOwnerEntityName()}, profile={profile.ProfileName}, faction={faction}, " +
                $"origin={origin}, direction={direction}, range={shape.Range:0.##}, " +
                $"radius={shape.HitRadius:0.##}, arc={shape.ArcAngle:0.##}, " +
                $"effectiveHitRange={_hitZone.EffectiveRange:0.##}, " +
                $"effectiveHitRadius={_hitZone.EffectiveRadius:0.##}, " +
                $"effectiveHitArc={_hitZone.EffectiveArcAngle:0.##}, " +
                $"hitCenter={_hitZone.LockedCenter}, hitSize={_hitZone.LockedSize}, " +
                $"delay={profile.HitDelaySeconds:0.###}, active={profile.ActiveHitSeconds:0.###}, " +
                $"particlePosition={particlePosition}, particleEuler={particleRotation.eulerAngles}, " +
                $"particleScale={particleScale:0.###}, " +
                $"particleContainer={ResolveParticleContainerName()}, " +
                //$"particleFollowTarget={(particleFollowTarget != null ? particleFollowTarget.name : \"None\")}, " +
                $"particleFollowOffset={particleFollowOffset}, " +
                $"containerLocalPosition={ResolveParticleLocalPosition()}, " +
                $"containerLocalEuler={ResolveParticleLocalEuler()}, " +
                $"sequence={_attackSequence}",
                this);
        }

        return true;
    }

    // Purpose: Performs the run attack window workflow for this component.
    private IEnumerator RunAttackWindow(
        IMeleeAttackProfile profile,
        ShooterFaction faction,
        MeleeShapeSnapshot shape,
        Vector3 origin,
        Vector3 direction,
        Vector3 particlePosition,
        Quaternion particleRotation,
        Transform particleFollowTarget,
        Vector3 particleFollowOffset,
        float particleScale)
    {
        if (profile.HitDelaySeconds > 0f)
            yield return new WaitForSeconds(profile.HitDelaySeconds);

        _hitZone.SetHitWindowActive(true);

        // The particle pose was captured with the hit direction when input was
        // accepted. Later aim or weapon rotation cannot redirect this swing.
        _equipmentController?.PlayMeleeSwing(
            profile.SwingParticlePrefab,
            profile.SwingTrailParticlePrefab,
            particlePosition,
            particleRotation,
            _particleContainerRoot,
            particleFollowTarget,
            particleFollowOffset,
            particleScale,
            _debugLogs);

        float finishTime = Time.time + profile.ActiveHitSeconds;

        do
        {
            SampleHitZone(profile, faction, shape, origin, direction);
            yield return null;
        }
        while (Time.time < finishTime);

        _hitZone.SetHitWindowActive(false);
        _attackRoutine = null;

        if (_debugLogs)
        {
            Debug.Log(
                $"[ShooterMeleeAttack] Swing finished. owner={ResolveOwnerEntityName()}, " +
                $"profile={profile.ProfileName}, candidateSamples={_candidateSamples}, " +
                $"damagedTargets={_damagedThisSwing.Count}, " +
                $"rejected(noDamageable={_rejectedNoDamageable}, " +
                $"unavailable={_rejectedUnavailable}, alreadyDamaged={_rejectedAlreadyDamaged}, " +
                $"ownEntity={_rejectedOwnEntity}, distance={_rejectedDistance}, " +
                $"angle={_rejectedAngle})",
                this);
        }
    }

    // Purpose: Performs the sample hit zone workflow for this component.
    private void SampleHitZone(
        IMeleeAttackProfile profile,
        ShooterFaction faction,
        MeleeShapeSnapshot shape,
        Vector3 origin,
        Vector3 direction)
    {
        int hitCount = _hitZone.Overlap(
            _hits,
            profile.ResolveDamageTargetMask(faction),
            profile.TriggerInteraction);

        _candidateSamples += hitCount;

        Vector3 raisedOrigin = origin + Vector3.up * shape.VerticalOffset;

        for (int i = 0; i < hitCount; i++)
        {
            Collider hit = _hits[i];

            if (hit == null)
                continue;

            Damageable damageable = ResolveDamageable(hit);

            if (damageable == null)
            {
                _rejectedNoDamageable++;
                continue;
            }

            if (!damageable.CanReceiveDamage)
            {
                _rejectedUnavailable++;
                continue;
            }

            if (_damagedThisSwing.Contains(damageable))
            {
                _rejectedAlreadyDamaged++;
                continue;
            }

            if (IsOwnDamageable(damageable))
            {
                _rejectedOwnEntity++;
                continue;
            }

            Vector3 targetPoint = hit.bounds.center;
            Vector3 toTarget = targetPoint - raisedOrigin;
            toTarget.y = 0f;

            float distance = toTarget.magnitude;

            if (distance > _hitZone.EffectiveRange + _hitZone.EffectiveRadius)
            {
                _rejectedDistance++;
                continue;
            }

            if (toTarget.sqrMagnitude > 0.0001f)
            {
                float angle = Vector3.Angle(direction, toTarget.normalized);

                if (angle > _hitZone.EffectiveArcAngle * 0.5f)
                {
                    _rejectedAngle++;
                    continue;
                }
            }

            _damagedThisSwing.Add(damageable);

            if (profile is PlayerEmergencyMeleeProfile)
                damageable.ReceiveFixedMeleeAttack(profile.Damage, ResolveOwnerEntityGameObject());
            else
                damageable.ReceiveAnAttack(
                    profile.Damage,
                    ResolveOwnerEntityGameObject(),
                    profile.PenetrationClass,
                    profile.ArmorHealthDurabilityLossMultiplier);

            if (_debugLogs)
            {
                Debug.Log(
                    $"[ShooterMeleeAttack] Target damaged. target={damageable.name}, " +
                    $"damage={profile.Damage:0.##}, distance={distance:0.##}, " +
                    $"collider={ResolveColliderPath(hit)}, " +
                    $"colliderLayer={ResolveLayerName(hit.gameObject.layer)}, " +
                    $"targetLayer={ResolveLayerName(damageable.gameObject.layer)}",
                    damageable);
            }
        }
    }

    // Purpose: Resolves the damageable needed by this component.
    private static Damageable ResolveDamageable(Collider collider)
    {
        if (collider == null)
            return null;

        Damageable damageable =
            collider.GetComponent<Damageable>() ??
            collider.GetComponentInParent<Damageable>();

        if (damageable != null)
            return damageable;

        Transform entityRoot =
            EntityComponentResolver.ResolveEntityRoot(collider.transform);

        if (entityRoot == null)
            return null;

        return entityRoot.GetComponent<Damageable>() ??
               entityRoot.GetComponentInChildren<Damageable>(true);
    }

    // Purpose: Resolves the collider path needed by this component.
    private static string ResolveColliderPath(Collider collider)
    {
        if (collider == null)
            return "None";

        Transform current = collider.transform;
        string path = current.name;

        while (current.parent != null)
        {
            current = current.parent;
            path = current.name + "/" + path;
        }

        return path;
    }

    // Purpose: Resolves the layer name needed by this component.
    private static string ResolveLayerName(int layer)
    {
        string layerName = LayerMask.LayerToName(layer);
        return string.IsNullOrEmpty(layerName)
            ? layer.ToString()
            : $"{layerName}({layer})";
    }

    // Purpose: Performs the reject workflow for this component.
    private bool Reject(string reason)
    {
        if (_debugLogs)
        {
            string weaponName = _equipmentController != null &&
                                _equipmentController.CurrentWeapon != null
                ? _equipmentController.CurrentWeapon.name
                : "None";

            Debug.LogWarning(
                $"[ShooterMeleeAttack] Attack rejected. owner={ResolveOwnerEntityName()}, " +
                $"weapon={weaponName}, reason={reason}",
                this);
        }

        return false;
    }

    // Purpose: Checks whether own damageable is currently true.
    private bool IsOwnDamageable(Damageable damageable)
    {
        if (damageable == null)
            return false;

        Transform ownerEntityRoot = ResolveOwnerEntityRoot();
        Transform targetEntityRoot =
            EntityComponentResolver.ResolveEntityRoot(damageable);

        return ownerEntityRoot != null &&
               targetEntityRoot != null &&
               ownerEntityRoot == targetEntityRoot;
    }

    // Purpose: Resolves the owner entity root needed by this component.
    private Transform ResolveOwnerEntityRoot()
    {
        return EntityComponentResolver.ResolveEntityRoot(this) ?? transform;
    }

    // Purpose: Resolves the owner entity game object needed by this component.
    private GameObject ResolveOwnerEntityGameObject()
    {
        Transform ownerEntityRoot = ResolveOwnerEntityRoot();
        return ownerEntityRoot != null ? ownerEntityRoot.gameObject : gameObject;
    }

    // Purpose: Resolves the owner entity name needed by this component.
    private string ResolveOwnerEntityName()
    {
        Transform ownerEntityRoot = ResolveOwnerEntityRoot();
        return ownerEntityRoot != null ? ownerEntityRoot.name : name;
    }

    // Purpose: Resolves the particle scale needed by this component.
    private float ResolveParticleScale(MeleeShapeSnapshot shape)
    {
        float rangeRatio = shape.Range / Mathf.Max(0.01f, _particleReferenceRange);
        float radiusRatio = shape.HitRadius / Mathf.Max(0.01f, _particleReferenceHitRadius);

        return Mathf.Max(0.01f, Mathf.Max(rangeRatio, radiusRatio)) *
               Mathf.Max(0.01f, _particleScaleMultiplier);
    }

    // Purpose: Resolves the particle pose needed by this component.
    private void ResolveParticlePose(
        Vector3 attackOrigin,
        Vector3 attackDirection,
        Transform positionFollowTarget,
        out Vector3 worldPosition,
        out Quaternion worldRotation)
    {
        attackDirection.y = 0f;

        if (attackDirection.sqrMagnitude <= 0.0001f)
            attackDirection = transform.forward;

        attackDirection.y = 0f;
        attackDirection.Normalize();

        Quaternion attackRotation = Quaternion.LookRotation(
            attackDirection,
            Vector3.up);

        Vector3 localPosition = ResolveParticleLocalPosition();
        Quaternion localRotation = _particleContainerRoot != null
            ? _particleContainerRoot.localRotation
            : Quaternion.identity;

        Vector3 positionOrigin = positionFollowTarget != null
            ? positionFollowTarget.position
            : attackOrigin;

        worldPosition = positionOrigin + attackRotation * localPosition;
        worldRotation =
            attackRotation *
            localRotation *
            Quaternion.Euler(_particleRotationOffsetEuler);
    }

    // Purpose: Resolves the particle follow target needed by this component.
    private Transform ResolveParticleFollowTarget()
    {
        if (_equipmentController != null &&
            _equipmentController.CurrentMeleeSwingEffectOrigin != null)
        {
            return _equipmentController.CurrentMeleeSwingEffectOrigin;
        }

        return _particleContainerRoot;
    }

    // Purpose: Resolves the particle local position needed by this component.
    private Vector3 ResolveParticleLocalPosition()
    {
        return _particleContainerRoot != null
            ? _particleContainerRoot.localPosition
            : Vector3.zero;
    }

    // Purpose: Resolves the particle local euler needed by this component.
    private Vector3 ResolveParticleLocalEuler()
    {
        return _particleContainerRoot != null
            ? _particleContainerRoot.localEulerAngles
            : Vector3.zero;
    }

    // Purpose: Resolves the particle container name needed by this component.
    private string ResolveParticleContainerName()
    {
        return _particleContainerRoot != null
            ? _particleContainerRoot.name
            : "None";
    }

    // Purpose: Responds to the draw gizmos selected callback.
    private void OnDrawGizmosSelected()
    {
        if (_hitZone == null)
            _hitZone = GetComponent<ShooterMeleeHitZone>();

        ApplyRuntimeShapeToHitZone();
        _hitZone?.DrawGizmos(_idleGizmoColor, _activeGizmoColor);

        Vector3 origin;
        Vector3 position;
        Quaternion rotation;
        float particleScale;

        if (Application.isPlaying && _hasDebugParticlePose)
        {
            origin = _lastAttackOrigin;
            position = _lastParticlePosition;
            rotation = _lastParticleRotation;
            particleScale = _lastParticleScale;
        }
        else
        {
            origin = transform.position;
            ResolveParticlePose(
                origin,
                transform.forward,
                ResolveParticleFollowTarget(),
                out position,
                out rotation);

            particleScale = ResolveParticleScale(CaptureRuntimeShape());
        }

        float axisLength = 0.8f * Mathf.Clamp(particleScale, 0.25f, 3f);

        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(origin, position);
        Gizmos.DrawWireSphere(position, 0.08f);

        Gizmos.color = Color.blue;
        Gizmos.DrawRay(position, rotation * Vector3.forward * axisLength);

        Gizmos.color = Color.red;
        Gizmos.DrawRay(position, rotation * Vector3.right * axisLength * 0.65f);

        Gizmos.color = Color.green;
        Gizmos.DrawRay(position, rotation * Vector3.up * axisLength * 0.65f);
    }
}
