/*
 * Role hierarchy: Gameplay > Items > Weapons > Runtime
 * Primary responsibility: Handles ThrowableProjectile combat resolution, damage, projectile, or status changes.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody))]
public class ThrowableProjectile : MonoBehaviour
{
    private const int MaxExplosionHits = 64;
    private static readonly List<ThrowableProjectile> ActiveProjectiles = new();

    [Header("Runtime")]
    [SerializeField, ReadOnly] private ThrowableUsableItemSO _weapon;
    [SerializeField, ReadOnly] private ShooterFaction _faction;

    private readonly Collider[] _hits = new Collider[MaxExplosionHits];
    private readonly HashSet<Damageable> _damaged = new();

    private Rigidbody _rigidbody;
    private Collider[] _physicsColliders;
    private MaskRenderManager _maskRenderManager;
    private EntityShooterCharacterAudio _audio;
    private SoundStimulusEventChannelSO _soundStimulusEvent;
    private GameObject _source;
    private float _healthDamageMultiplier = 1f;
    private float _explodeTime;
    private float _nextImpactSoundTime;
    private bool _exploded;
    private bool _isLaunched;
    private bool _hasCollided;
    private int _collisionCount;
    private Vector3 _launchPosition;
    private Vector3 _intendedImpactPoint;
    private Vector3 _lastSoundPosition;
    private readonly ProjectileSoundStimulusEmitter _soundEmitter = new();

    // Purpose: Caches the component references required before runtime initialization.
    private void Awake()
    {
        CachePhysicsRefs();
        PrepareForEquipped();
    }

    // Purpose: Processes this component's per-frame runtime work.
    private void Update()
    {
        if (!_exploded && _weapon != null && Time.time >= _explodeTime)
            Explode();
    }

    // Purpose: Processes fixed-timestep work used by physics-aligned behavior.
    private void FixedUpdate()
    {
        if (_exploded || _weapon == null)
            return;

        Vector3 currentPosition = transform.position;
        _soundEmitter.EmitTravelSegment(_lastSoundPosition, currentPosition);
        _lastSoundPosition = currentPosition;
    }

    // Purpose: Responds to the collision enter callback.
    private void OnCollisionEnter(Collision collision)
    {
        if (_exploded || _weapon == null)
            return;

        _hasCollided = true;
        _collisionCount++;

        Vector3 impactPoint = collision.contactCount > 0
            ? collision.GetContact(0).point
            : transform.position;
        Vector3 direction = _rigidbody != null
            ? _rigidbody.linearVelocity
            : Vector3.zero;
        Collider hitCollider = collision.collider;

        Debug.Log(
            $"[ThrowableDebug] Collision. item={(_weapon != null ? _weapon.name : "null")}, " +
            $"count={_collisionCount}, hit={(hitCollider != null ? hitCollider.name : "null")}, " +
            $"point={impactPoint}, velocity={direction}, launch={_launchPosition}, " +
            $"intended={_intendedImpactPoint}, explodeOnImpact={_weapon.ExplodeOnImpact}",
            this);

        if (_weapon.ExplodeOnImpact)
        {
            Explode(_weapon.InfectionExposureOnImpact > 0f ? impactPoint : (Vector3?)null);
            return;
        }

        if (Time.time < _nextImpactSoundTime)
            return;

        _soundEmitter.EmitImpact(impactPoint, direction);
        _nextImpactSoundTime =
            Time.time + _weapon.ProjectileImpactSoundCooldown;
    }

    /// <summary>
    /// Configures a throwable prefab instance for an equipped hand preview.
    /// Physics and collision stay disabled; throwing creates a separate copy.
    /// </summary>
    public void PrepareForEquipped()
    {
        CachePhysicsRefs();
        UnregisterActiveProjectile();

        _weapon = null;
        _source = null;
        _maskRenderManager = null;
        _audio = null;
        _soundStimulusEvent = null;
        _exploded = false;
        _hasCollided = false;
        _collisionCount = 0;
        _launchPosition = transform.position;
        _intendedImpactPoint = transform.position;
        _nextImpactSoundTime = 0f;
        _lastSoundPosition = transform.position;

        if (_rigidbody != null)
        {
            // Unity rejects velocity writes while the prefab is already
            // kinematic. A released projectile is dynamic, so only that path
            // needs its previous velocity cleared before returning to the hand.
            if (!_rigidbody.isKinematic)
            {
                _rigidbody.linearVelocity = Vector3.zero;
                _rigidbody.angularVelocity = Vector3.zero;
            }

            _rigidbody.isKinematic = true;
            _rigidbody.detectCollisions = false;
            // Interpolation makes a kinematic child visually lag behind a
            // rotating weapon socket. Equipped throwables must stay exact.
            _rigidbody.interpolation = RigidbodyInterpolation.None;
        }

        SetPhysicsCollidersEnabled(false);
        enabled = false;
    }

    /// <summary>
    /// Activates an independently instantiated projectile copy. This is the
    /// only launch entry point for throwable usable items.
    /// </summary>
    public void Launch(
        ThrowableUsableItemSO weapon,
        Vector3 velocity,
        Vector3 intendedImpactPoint,
        GameObject source,
        ShooterFaction faction,
        MaskRenderManager maskRenderManager,
        EntityShooterCharacterAudio audio,
        SoundStimulusEventChannelSO soundStimulusEvent,
        float minimumFuseSeconds,
        bool detonateImmediately)
    {
        CachePhysicsRefs();

        // Awake prepares the prefab as an equipped, kinematic view. Capture
        // the world pose supplied by Instantiate before enabling simulation,
        // then explicitly synchronize the Rigidbody to it. Without this, an
        // interpolated body can resume from the prefab's old origin pose and
        // create its impact zone in another sector.
        Vector3 launchPosition = transform.position;
        Quaternion launchRotation = transform.rotation;

        _weapon = weapon;
        _source = source;
        _faction = faction;
        // Snapshot at launch: the source may die before this grenade explodes.
        _healthDamageMultiplier = faction != ShooterFaction.Player && source != null
            ? SectorEnemyTraitModifier.NonFirearmAnomalyMultiplier(source.transform) : 1f;
        _maskRenderManager = maskRenderManager;
        _audio = audio;
        _soundStimulusEvent = soundStimulusEvent;
        _exploded = false;
        _hasCollided = false;
        _collisionCount = 0;
        _launchPosition = launchPosition;
        _intendedImpactPoint = intendedImpactPoint;
        _explodeTime = Time.time + Mathf.Max(
            weapon != null ? weapon.FuseSeconds : 0.01f,
            minimumFuseSeconds);
        _nextImpactSoundTime = 0f;
        _lastSoundPosition = transform.position;

        Debug.Log(
            $"[ThrowableDebug] Projectile configured. item={(weapon != null ? weapon.name : "null")}, " +
            $"source={(source != null ? source.name : "null")}, faction={faction}, " +
            $"position={transform.position}, intended={intendedImpactPoint}, " +
            $"explodeAt={_explodeTime:0.000}, immediate={detonateImmediately}, " +
            $"maskManager={maskRenderManager != null}",
            this);

        _soundEmitter.Configure(
            soundStimulusEvent,
            source,
            gameObject,
            weapon != null ? weapon.ProjectileFlybySoundRadius : 0f,
            weapon != null ? weapon.ProjectileFlybyEmitDistance : 1f,
            weapon != null ? weapon.ProjectileFlybySoundIntensity : 0f,
            weapon != null ? weapon.ProjectileImpactSoundRadius : 0f,
            weapon != null ? weapon.ProjectileImpactSoundIntensity : 0f,
            weapon != null ? weapon.SoundInvestigateDelaySeconds : 0f);

        gameObject.SetActive(true);

        if (detonateImmediately)
        {
            transform.SetPositionAndRotation(
                intendedImpactPoint,
                launchRotation);

            if (_rigidbody != null)
            {
                _rigidbody.position = intendedImpactPoint;
                _rigidbody.rotation = launchRotation;
            }

            enabled = true;
            Explode();
            return;
        }

        SetPhysicsCollidersEnabled(true);
        IgnoreSourceCollisions(source);

        if (_rigidbody != null)
        {
            _rigidbody.isKinematic = true;
            _rigidbody.detectCollisions = false;
            _rigidbody.interpolation = RigidbodyInterpolation.None;
            _rigidbody.position = launchPosition;
            _rigidbody.rotation = launchRotation;
            transform.SetPositionAndRotation(
                launchPosition,
                launchRotation);

            _rigidbody.isKinematic = false;
            _rigidbody.detectCollisions = true;
            _rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
            _rigidbody.linearVelocity = velocity;
            _rigidbody.angularVelocity = Vector3.zero;

            if (weapon != null && weapon.SpinTorque > 0f)
            {
                _rigidbody.AddTorque(
                    Random.onUnitSphere * weapon.SpinTorque,
                    ForceMode.VelocityChange);
            }
        }

        enabled = true;
        RegisterActiveProjectile();
    }

    // Purpose: Executes the explode explosion behavior.
    private void Explode(Vector3? exactImpactPoint = null)
    {
        if (_exploded)
            return;

        _exploded = true;
        UnregisterActiveProjectile();
        Vector3 physicsPoint = transform.position;
        Vector3 point = exactImpactPoint ?? new Vector3(
            physicsPoint.x,
            _intendedImpactPoint.y,
            physicsPoint.z);

        Debug.Log(
            $"[ThrowableDebug] Explode entered. item={(_weapon != null ? _weapon.name : "null")}, " +
            $"physicsPoint={physicsPoint}, zonePoint={point}, " +
            $"launch={_launchPosition}, collisionCount={_collisionCount}, " +
            $"healing={_weapon is HealingBottleUsableItemSO}, " +
            $"showZone={(_weapon != null && _weapon.ShowImpactZoneVisual)}, " +
            $"maskManager={_maskRenderManager != null}",
            this);

        if (_weapon == null)
        {
            Debug.LogError(
                "[ThrowableDebug] Explosion aborted: projectile item definition is missing.",
                this);
            Destroy(gameObject);
            return;
        }

        if (_weapon is HealingBottleUsableItemSO healingBottle)
        {
            SpawnHealingZone(point, healingBottle);
        }
        else
        {
            StampMask(point);
            SpawnReadableImpactZone(
                point,
                _weapon.ExplosionRadius,
                _weapon.ImpactZoneVisualDurationSeconds,
                _weapon.ImpactZoneVisualColor,
                "GrenadeImpactZone");
            ApplyExplosionDamage(point);
        }

        _audio?.PlayThrowableExplode(_weapon, point);
        PublishExplosionSound(point);

        Destroy(gameObject);
    }

    // Purpose: Releases active subscriptions and transient state while disabled.
    private void OnDisable()
    {
        UnregisterActiveProjectile();
    }

    /// <summary>
    /// Finds a launched hostile throwable whose expected explosion area currently
    /// contains the subject. Airborne throwables use their authored aim point;
    /// after the first bounce the live rigidbody position/velocity takes over.
    /// </summary>
    public static bool TryGetClosestHostileThreat(
        GameObject subject,
        ShooterFaction subjectFaction,
        Vector3 subjectPosition,
        float safetyPadding,
        out Vector3 dangerCenter,
        out float dangerRadius,
        out int projectileId)
    {
        dangerCenter = default;
        dangerRadius = 0f;
        projectileId = 0;

        float bestNormalizedDistance = float.PositiveInfinity;

        for (int i = ActiveProjectiles.Count - 1; i >= 0; i--)
        {
            ThrowableProjectile projectile = ActiveProjectiles[i];

            if (projectile == null)
            {
                ActiveProjectiles.RemoveAt(i);
                continue;
            }

            if (!projectile.IsHostileThreatTo(subject, subjectFaction))
                continue;

            Vector3 center = projectile.ResolveDangerCenter();
            float radius = projectile.ResolveDangerRadius(safetyPadding);
            float sqrDistance = PlanarSqrDistance(subjectPosition, center);

            if (sqrDistance > radius * radius)
                continue;

            float normalizedDistance = sqrDistance / Mathf.Max(0.0001f, radius * radius);

            if (normalizedDistance >= bestNormalizedDistance)
                continue;

            bestNormalizedDistance = normalizedDistance;
            dangerCenter = center;
            dangerRadius = radius;
            projectileId = projectile.gameObject.GetInstanceID();
        }

        return projectileId != 0;
    }

    // Purpose: Calculates hostile threat at from the current inputs.
    public static float EvaluateHostileThreatAt(
        GameObject subject,
        ShooterFaction subjectFaction,
        Vector3 worldPosition,
        float safetyPadding)
    {
        float totalThreat = 0f;

        for (int i = ActiveProjectiles.Count - 1; i >= 0; i--)
        {
            ThrowableProjectile projectile = ActiveProjectiles[i];

            if (projectile == null)
            {
                ActiveProjectiles.RemoveAt(i);
                continue;
            }

            if (!projectile.IsHostileThreatTo(subject, subjectFaction))
                continue;

            Vector3 center = projectile.ResolveDangerCenter();
            float radius = projectile.ResolveDangerRadius(safetyPadding);
            float distance = Mathf.Sqrt(PlanarSqrDistance(worldPosition, center));

            if (distance >= radius)
                continue;

            totalThreat += 1f + (1f - distance / radius);
        }

        return totalThreat;
    }

    // Purpose: Checks whether hostile threat to is currently true.
    private bool IsHostileThreatTo(GameObject subject, ShooterFaction subjectFaction)
    {
        if (!_isLaunched ||
            _exploded ||
            _weapon == null ||
            !_weapon.IsHostileThreat ||
            !isActiveAndEnabled)
            return false;

        // Player and enemies are parented below the same SectorRuntime hierarchy,
        // so Transform.root cannot identify whether two objects are the same entity.
        // The authored shooter faction is the ownership boundary for throwables.
        return _faction != subjectFaction;
    }

    // Purpose: Resolves the danger center needed by this component.
    private Vector3 ResolveDangerCenter()
    {
        if (!_hasCollided)
            return _intendedImpactPoint;

        Vector3 center = transform.position;

        if (_rigidbody == null)
            return center;

        float remainingFuse = Mathf.Max(0f, _explodeTime - Time.time);
        float predictionSeconds = Mathf.Min(0.5f, remainingFuse);
        Vector3 planarVelocity = _rigidbody.linearVelocity;
        planarVelocity.y = 0f;
        center += planarVelocity * predictionSeconds;
        return center;
    }

    // Purpose: Resolves the danger radius needed by this component.
    private float ResolveDangerRadius(float safetyPadding)
    {
        float explosionRadius = _weapon != null ? _weapon.ExplosionRadius : 0f;
        return Mathf.Max(0.01f, explosionRadius + Mathf.Max(0f, safetyPadding));
    }

    // Purpose: Registers active projectile with its owning runtime service.
    private void RegisterActiveProjectile()
    {
        _isLaunched = true;

        if (!ActiveProjectiles.Contains(this))
            ActiveProjectiles.Add(this);
    }

    // Purpose: Unregisters active projectile from its owning runtime service.
    private void UnregisterActiveProjectile()
    {
        _isLaunched = false;
        ActiveProjectiles.Remove(this);
    }

    // Purpose: Performs the planar sqr distance workflow for this component.
    private static float PlanarSqrDistance(Vector3 a, Vector3 b)
    {
        float x = a.x - b.x;
        float z = a.z - b.z;
        return x * x + z * z;
    }

    // Purpose: Publishes the explosion sound notification.
    private void PublishExplosionSound(Vector3 point)
    {
        if (_weapon == null ||
            _soundStimulusEvent == null ||
            _weapon.ExplosionSoundRadius <= 0f ||
            _weapon.ExplosionSoundIntensity <= 0f)
        {
            return;
        }

        Vector3 direction = _rigidbody != null
            ? _rigidbody.linearVelocity
            : Vector3.zero;

        _soundStimulusEvent.RaiseEvent(new SoundStimulus(
            _source,
            point,
            _weapon.ExplosionSoundRadius,
            _weapon.SoundInvestigateDelaySeconds,
            SoundStimulusType.Explosion,
            _weapon.ExplosionSoundIntensity,
            gameObject.GetInstanceID(),
            direction));
    }

    // Purpose: Performs the stamp mask workflow for this component.
    private void StampMask(Vector3 point)
    {
        if (_weapon == null)
            return;

        if (_maskRenderManager == null)
        {
            Debug.LogWarning(
                $"[ThrowableDebug] Mask stamp skipped: MaskRenderManager is missing. " +
                $"item={_weapon.name}, point={point}. The fallback zone remains active.",
                this);
            return;
        }

        bool requested = _maskRenderManager.RequestCircle(
            _weapon.ResolveMaskRender(_faction),
            _weapon.ResolveFallbackPaintChannel(_faction),
            point,
            _weapon.ExplosionRadius,
            _weapon.PaintPriority,
            _source != null ? _source : gameObject);

        if (requested)
        {
            Debug.Log(
                $"[ThrowableDebug] Mask stamp accepted. item={_weapon.name}, " +
                $"point={point}, radius={_weapon.ExplosionRadius:0.###}",
                this);
        }
        else
        {
            Debug.LogWarning(
                $"[ThrowableDebug] Mask stamp rejected. item={_weapon.name}, " +
                $"point={point}, radius={_weapon.ExplosionRadius:0.###}. " +
                "The independent fallback zone remains active.",
                this);
        }
    }

    // Purpose: Spawns healing zone for the current runtime context.
    private void SpawnHealingZone(
        Vector3 point,
        HealingBottleUsableItemSO healingBottle)
    {
        SpawnReadableImpactZone(
            point,
            healingBottle.HealingRadius,
            healingBottle.HealingDurationSeconds,
            healingBottle.ImpactZoneVisualColor,
            "HealingBottleZone");

        if (healingBottle == null || healingBottle.HealingZonePrefab == null)
        {
            Debug.LogError(
                $"[{nameof(ThrowableProjectile)}] Healing Bottle requires a Healing Zone Prefab. " +
                $"item={(healingBottle != null ? healingBottle.name : "null")}",
                healingBottle);
            return;
        }

        GameObject zoneObject = Instantiate(
            healingBottle.HealingZonePrefab,
            point,
            Quaternion.identity);
        zoneObject.transform.position = point;

        HealingZoneRuntime runtime = zoneObject.GetComponent<HealingZoneRuntime>();

        if (runtime == null)
        {
            Debug.LogError(
                $"[{nameof(ThrowableProjectile)}] Healing Zone Prefab root requires " +
                $"{nameof(HealingZoneRuntime)}. prefab={healingBottle.HealingZonePrefab.name}",
                zoneObject);
            Destroy(zoneObject);
            return;
        }

        runtime.Configure(
            healingBottle.HealingRadius,
            healingBottle.HealingDurationSeconds,
            healingBottle.HealingTickInterval,
            healingBottle.HealPerTick,
            healingBottle.HealingTargetMask);

        Debug.Log(
            $"[ThrowableDebug] Healing runtime zone configured. item={healingBottle.name}, " +
            $"zone={zoneObject.name}, point={point}, radius={healingBottle.HealingRadius:0.###}, " +
            $"duration={healingBottle.HealingDurationSeconds:0.###}, " +
            $"tick={healingBottle.HealingTickInterval:0.###}, heal={healingBottle.HealPerTick:0.###}",
            zoneObject);

        if (_maskRenderManager != null && healingBottle.HealingMaskRender != null)
        {
            GameObject temporaryVisual =
                _maskRenderManager.CreateTemporaryCircleVisual(
                healingBottle.HealingMaskRender,
                point,
                healingBottle.HealingRadius,
                healingBottle.HealingDurationSeconds,
                _source,
                zoneObject.transform);

            if (temporaryVisual != null)
            {
                Debug.Log(
                    $"[ThrowableDebug] Healing MaskRender visual created. " +
                    $"item={healingBottle.name}, visual={temporaryVisual.name}",
                    temporaryVisual);
            }
            else
            {
                Debug.LogWarning(
                    $"[ThrowableDebug] Healing MaskRender visual creation failed. " +
                    $"item={healingBottle.name}. The independent green fallback zone remains active.",
                    zoneObject);
            }
        }
        else if (healingBottle.HealingMaskRender != null)
        {
            Debug.LogError(
                $"[{nameof(ThrowableProjectile)}] MaskRenderManager is missing. " +
                $"The healing zone will still work without its temporary visual.",
                this);
        }
    }

    // Purpose: Spawns readable impact zone for the current runtime context.
    private void SpawnReadableImpactZone(
        Vector3 point,
        float radius,
        float durationSeconds,
        Color color,
        string visualName)
    {
        if (_weapon == null)
        {
            Debug.LogError(
                $"[ThrowableDebug] Readable zone skipped because the item definition is missing. " +
                $"name={visualName}, point={point}",
                this);
            return;
        }

        if (!_weapon.ShowImpactZoneVisual)
        {
            Debug.LogWarning(
                $"[ThrowableDebug] Readable zone disabled by item setting. " +
                $"item={_weapon.name}, name={visualName}, point={point}",
                this);
            return;
        }

        ThrowableZoneVisualRuntime runtime =
            ThrowableZoneVisualRuntime.Create(
            point,
            radius,
            durationSeconds,
            color,
            visualName);

        if (runtime == null)
        {
            Debug.LogError(
                $"[ThrowableDebug] Readable zone creation failed. item={_weapon.name}, " +
                $"name={visualName}, point={point}, radius={radius:0.###}, " +
                $"duration={durationSeconds:0.###}, color={color}",
                this);
            return;
        }

        Debug.Log(
            $"[ThrowableDebug] Readable zone created. item={_weapon.name}, " +
            $"name={visualName}, point={point}, radius={radius:0.###}, " +
            $"duration={durationSeconds:0.###}, color={color}",
            runtime);
    }

    // Purpose: Applies the requested explosion damage change.
    private void ApplyExplosionDamage(Vector3 point)
    {
        if (_weapon == null)
            return;

        float exposure = _weapon.InfectionExposureOnImpact;
        LayerMask targetMask = exposure > 0f
            ? _weapon.ResolveDamageTargetMask(ShooterFaction.Player).value |
              _weapon.ResolveDamageTargetMask(ShooterFaction.Scraper).value
            : _weapon.ResolveDamageTargetMask(_faction).value;

        if (targetMask == 0)
            return;

        int count = Physics.OverlapSphereNonAlloc(
            point,
            _weapon.ExplosionRadius,
            _hits,
            targetMask,
            _weapon.TriggerInteraction);

        _damaged.Clear();

        for (int i = 0; i < count; i++)
        {
            Collider hit = _hits[i];

            if (hit == null)
                continue;

            Damageable damageable =
                hit.GetComponent<Damageable>() ??
                hit.GetComponentInParent<Damageable>();

            if (damageable == null ||
                (exposure > 0f ? damageable.IsDead : !damageable.CanReceiveDamage) ||
                _damaged.Contains(damageable) ||
                (exposure <= 0f && IsSourceDamageable(damageable)))
            {
                continue;
            }

            _damaged.Add(damageable);
            if (exposure > 0f)
                damageable.AddInfectionExposure(exposure);

            ApplyImpactStatusEffect(damageable);

            if (_weapon.HealthDamage > 0f)
            {
                damageable.ReceiveAnAttack(
                    _weapon.HealthDamage * _healthDamageMultiplier,
                    _source,
                    _weapon.PenetrationClass,
                    _weapon.ArmorHealthDurabilityLossMultiplier);
            }

            if (_weapon.InfectionDamage > 0f)
                ApplyInfectionDamage(damageable);
        }
    }

    // Purpose: Applies the requested infection damage change.
    private void ApplyInfectionDamage(Damageable damageable)
    {
        float finalInfectionDamage =
            _weapon.InfectionDamage *
            damageable.ResolveInfectionDamageMultiplierForPenetration(
                _weapon.PenetrationClass);

        EntityStatusEffectRuntime statusEffects =
            EntityComponentResolver.Resolve<EntityStatusEffectRuntime>(damageable);

        if (statusEffects != null)
        {
            finalInfectionDamage *= statusEffects.GetMultiplier(
                StatusEffectModifierType.DamageTakenMultiplier);
        }

        damageable.ApplyArmorInfectionDurabilityDamage(
            _weapon.InfectionDamage,
            finalInfectionDamage,
            _weapon.ArmorInfectionDurabilityLossMultiplier);

        ShooterInfection infection =
            damageable.GetComponent<ShooterInfection>() ??
            damageable.GetComponentInParent<ShooterInfection>() ??
            damageable.GetComponentInChildren<ShooterInfection>();

        if (infection != null)
            infection.AddInfection(finalInfectionDamage);

        damageable.ReportDamageNumber(
            finalInfectionDamage,
            DamageNumberKind.Infection);
    }

    // Purpose: Applies the requested impact status effect change.
    private void ApplyImpactStatusEffect(Damageable damageable)
    {
        if (_weapon == null ||
            _weapon.ImpactStatusEffect == null ||
            damageable == null)
        {
            return;
        }

        EntityStatusEffectRuntime effects =
            EntityComponentResolver.Resolve<EntityStatusEffectRuntime>(damageable);

        if (effects == null)
            return;

        effects.Apply(
            _weapon.ImpactStatusEffect,
            _weapon.ImpactStatusDurationSeconds);
    }

    // Purpose: Checks whether source damageable is currently true.
    private bool IsSourceDamageable(Damageable damageable)
    {
        if (_source == null || damageable == null)
            return false;

        return damageable.transform.root == _source.transform.root;
    }

    // Purpose: Caches physics refs for later reuse.
    private void CachePhysicsRefs()
    {
        if (_rigidbody == null)
            _rigidbody = GetComponent<Rigidbody>();

        if (_physicsColliders == null || _physicsColliders.Length == 0)
        {
            // A throwable view may contain display-only mesh colliders below its model.
            // Prefer the explicit projectile collider on this root so those visual
            // colliders never become active when the equipped object is launched.
            Collider[] rootColliders = GetComponents<Collider>();
            _physicsColliders = rootColliders.Length > 0
                ? rootColliders
                : GetComponentsInChildren<Collider>(true);
        }
    }

    // Purpose: Updates the physics colliders enabled state.
    private void SetPhysicsCollidersEnabled(bool enabled)
    {
        if (_physicsColliders == null)
            return;

        for (int i = 0; i < _physicsColliders.Length; i++)
        {
            Collider collider = _physicsColliders[i];

            if (collider != null)
                collider.enabled = enabled;
        }
    }

    // Purpose: Performs the ignore source collisions workflow for this component.
    private void IgnoreSourceCollisions(GameObject source)
    {
        if (source == null || _physicsColliders == null)
            return;

        Collider[] sourceColliders = source.GetComponentsInChildren<Collider>(true);

        for (int i = 0; i < _physicsColliders.Length; i++)
        {
            Collider projectileCollider = _physicsColliders[i];
            if (projectileCollider == null)
                continue;

            for (int j = 0; j < sourceColliders.Length; j++)
            {
                Collider sourceCollider = sourceColliders[j];

                if (sourceCollider != null && sourceCollider != projectileCollider)
                    Physics.IgnoreCollision(projectileCollider, sourceCollider, true);
            }
        }
    }
}
