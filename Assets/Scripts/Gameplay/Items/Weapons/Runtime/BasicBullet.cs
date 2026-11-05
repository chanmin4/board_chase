/*
 * Role hierarchy: Gameplay > Items > Weapons > Runtime
 * Primary responsibility: Handles BasicBullet combat resolution, damage, projectile, or status changes.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class BasicBullet : MonoBehaviour
{
    private static readonly List<BasicBullet> ActiveProjectiles = new();
    public static void ClearForStageTransition()
    {
        foreach (var bullet in ActiveProjectiles.ToArray())
            if (bullet != null) { bullet.gameObject.SetActive(false); Destroy(bullet.gameObject); }
    }

    [SerializeField] private bool _debugHit;

    private Vector3 _direction;
    private Vector3 _visualDirection;
    private Vector3 _gameplayPosition;
    private Vector3 _visualStartPosition;

    private GameObject _source;
    private Transform _sourceEntityRoot;
    private float _speed;
    private float _castRadius;
    private float _maxDistance;
    private float _maxLifetime;
    private float _damage;
    private Damageable _headshotCandidate;
    private float _headshotDamageMultiplier = 1f;
    public static event System.Action<GameObject, Damageable, bool> HeadshotConfirmed;
    private WeaponSO _shotWeapon;
    public void ConfigureHeadshot(Damageable target, float multiplier, WeaponSO shotWeapon = null)
    {
        _headshotCandidate = target;
        _shotWeapon = shotWeapon;
        _headshotDamageMultiplier = Mathf.Max(1f, multiplier);
    }
    private float _penetrationClass;
    private readonly HashSet<SectorDestructibleCover> _penetratedCovers=new();
    private float _vaccineMarkAmountOnHit;
    private float _virusMarkAmountOnHit;
    private float _infectionDamageOnHit;
    private float _infectionInjectionDamagePerSecond;
    private float _authoredInfectionExposure;
    private float _infectionInjectionDurationSeconds;
    private float _infectionInjectionTickInterval;
    private StatusEffectDefinitionSO _infectionInjectionStatusEffect;
    private float _armorHealthDurabilityLossMultiplier = 1f;
    private float _armorInfectionDurabilityLossMultiplier = 0.5f;

    private LayerMask _damageTargetMask;
    private LayerMask _impactMask;
    private QueryTriggerInteraction _triggerInteraction;
    private ShootHitConfirmedEventChannelSO _shootHitConfirmedEvent;
    private readonly ProjectileSoundStimulusEmitter _soundEmitter = new();
    private BulletSO _bulletConfig;

    private float _travelledDistance;
    private float _lifeTime;
    private bool _initialized;
    private bool _completed;
    private string _bulletName;

    // Purpose: Performs the init workflow for this component.
    public void Init(
        BulletSO bulletConfig,
        Vector3 gameplayStartPosition,
        Vector3 direction,
        Vector3 visualDirection,
        float maxDistance,
        float speed,
        float castRadius,
        float maxLifetime,
        float damage,
        float penetrationClass,
        float vaccineMarkAmountOnHit,
        float virusMarkAmountOnHit,
        float infectionDamageOnHit,
        float armorHealthDurabilityLossMultiplier,
        float armorInfectionDurabilityLossMultiplier,
        LayerMask damageTargetMask,
        LayerMask impactMask,
        QueryTriggerInteraction triggerInteraction,
        GameObject source,
        SoundStimulusEventChannelSO soundStimulusEvent,
        ShootHitConfirmedEventChannelSO shootHitConfirmedEvent)
    {
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.0001f)
            direction = transform.forward;

        direction.y = 0f;

        if (direction.sqrMagnitude < 0.0001f)
            direction = Vector3.forward;

        if (visualDirection.sqrMagnitude < 0.0001f)
            visualDirection = direction;

        _direction = direction.normalized;
        _visualDirection = visualDirection.normalized;
        _gameplayPosition = gameplayStartPosition;
        _visualStartPosition = transform.position;

        _maxDistance = Mathf.Max(0.01f, maxDistance);
        _speed = Mathf.Max(0.01f, speed);
        _castRadius = Mathf.Max(0.001f, castRadius);
        _maxLifetime = Mathf.Max(0.01f, maxLifetime);
        _damage = Mathf.Max(0f, damage);
        _headshotCandidate = null;
        _headshotDamageMultiplier = 1f;
        _penetrationClass = Mathf.Max(0, penetrationClass);
        _vaccineMarkAmountOnHit = Mathf.Max(0f, vaccineMarkAmountOnHit);
        _virusMarkAmountOnHit = Mathf.Max(0f, virusMarkAmountOnHit);
        _infectionDamageOnHit = Mathf.Max(0f, infectionDamageOnHit);
        _infectionInjectionDamagePerSecond = 0f;
        _authoredInfectionExposure = bulletConfig?.InfectionExposureOnHit ?? 0f;
        _infectionInjectionDurationSeconds = 0f;
        _infectionInjectionTickInterval = 0f;
        _infectionInjectionStatusEffect = null;
        _armorHealthDurabilityLossMultiplier = Mathf.Max(0f, armorHealthDurabilityLossMultiplier);
        _armorInfectionDurabilityLossMultiplier = Mathf.Max(0f, armorInfectionDurabilityLossMultiplier);

        _damageTargetMask = damageTargetMask;
        _impactMask = impactMask;
        _triggerInteraction = triggerInteraction;
        _sourceEntityRoot = source != null
            ? EntityComponentResolver.ResolveEntityRoot(source.transform)
            : null;
        _source = _sourceEntityRoot != null
            ? _sourceEntityRoot.gameObject
            : source;
        _shootHitConfirmedEvent = shootHitConfirmedEvent;
        _bulletConfig = bulletConfig;

        _soundEmitter.Configure(
            soundStimulusEvent,
            _source,
            gameObject,
            bulletConfig != null ? bulletConfig.ProjectileFlybySoundRadius : 0f,
            bulletConfig != null ? bulletConfig.ProjectileFlybyEmitDistance : 1f,
            bulletConfig != null ? bulletConfig.ProjectileFlybySoundIntensity : 0f,
            bulletConfig != null ? bulletConfig.ProjectileImpactSoundRadius : 0f,
            bulletConfig != null ? bulletConfig.ProjectileImpactSoundIntensity : 0f,
            bulletConfig != null ? bulletConfig.ProjectileSoundInvestigateDelaySeconds : 0f);

        _travelledDistance = 0f;
        _lifeTime = 0f;
        _initialized = true;
        _completed = false;
        _bulletName = bulletConfig != null ? bulletConfig.name : name;
        RegisterActiveProjectile();

        if (_debugHit)
        {
            Debug.Log(
                $"[BasicBullet] Initialized. bullet={_bulletName}, source={ResolveObjectName(_source)}, " +
                $"start={_gameplayPosition}, direction={_direction}, visualDirection={_visualDirection}, " +
                $"distance={_maxDistance:0.###}, radius={_castRadius:0.###}, " +
                $"damageMask={FormatLayerMask(_damageTargetMask)}, impactMask={FormatLayerMask(_impactMask)}",
                this);
        }
    }

    // Purpose: Configures this component''s infection injection.
    public void ConfigureInfectionInjection(
        float damagePerSecond,
        float durationSeconds,
        float tickInterval,
        StatusEffectDefinitionSO statusEffect)
    {
        _infectionInjectionDamagePerSecond = Mathf.Max(0f, damagePerSecond);
        _infectionInjectionDurationSeconds = Mathf.Max(0f, durationSeconds);
        _infectionInjectionTickInterval = Mathf.Max(0.05f, tickInterval);
        _infectionInjectionStatusEffect = statusEffect;
    }

    // Purpose: Processes this component's per-frame runtime work.
    private void Update()
    {
        if (!_initialized)
        {
            Destroy(gameObject);
            return;
        }

        _lifeTime += Time.deltaTime;

        if (_lifeTime >= _maxLifetime)
        {
            Complete(_gameplayPosition, false, "lifetime expired");
            return;
        }

        float remainingDistance = _maxDistance - _travelledDistance;

        if (remainingDistance <= 0f)
        {
            Complete(_gameplayPosition, false, "maximum distance reached");
            return;
        }

        float travelDistance = Mathf.Min(
            _speed * Time.deltaTime,
            remainingDistance);

        if (travelDistance <= 0f)
            return;

        bool didHit = TryGetClosestHit(
                travelDistance,
                out RaycastHit hit,
                out bool isDamageHit);
#if UNITY_EDITOR
        CombatPhysicsTrace.Record(this, _source, _gameplayPosition, _direction,
            _castRadius, travelDistance, didHit, hit, isDamageHit);
#endif
        if (didHit)
        {
            Vector3 segmentStart = _gameplayPosition;
            float travelledToHit = _travelledDistance + hit.distance;

            _gameplayPosition += _direction * hit.distance;
            transform.position =
                _visualStartPosition + _visualDirection * travelledToHit;

            _travelledDistance = travelledToHit;
            _soundEmitter.EmitTravelSegment(segmentStart, _gameplayPosition);

            // Resolve surface audio at the actual contact point, before damage can
            // disable a destructible target. AI hearing remains a separate event.
            BulletImpactAudio.TryPlay(_bulletConfig, hit.collider, hit.point);
            var cover = hit.collider != null ? hit.collider.GetComponentInParent<SectorDestructibleCover>() : null;
            if (cover != null)
            {
                float multiplier=cover.IsDestructible?cover.ReceiveProjectileDamage(_damage,_penetrationClass):1f;
                if(cover.AllowsBulletPass)
                {
                    _penetratedCovers.Add(cover);_damage*=multiplier;
                    return; // Continue from this contact on the next physics segment; never bypass other walls/targets.
                }
            }

            if (isDamageHit)
                TryApplyDamage(hit);
            else if (_debugHit)
            {
                Debug.LogWarning(
                    $"[BasicBullet] Physical impact was not a damage target. " +
                    $"bullet={_bulletName}, collider={ResolveColliderPath(hit.collider)}, " +
                    $"colliderLayer={ResolveLayerName(hit.collider.gameObject.layer)}, point={hit.point}",
                    hit.collider);
            }

            Complete(
                hit.point,
                true,
                $"hit {ResolveColliderPath(hit.collider)}, damageTarget={isDamageHit}");
            return;
        }

        Vector3 travelStart = _gameplayPosition;
        _gameplayPosition += _direction * travelDistance;
        _travelledDistance += travelDistance;
        _soundEmitter.EmitTravelSegment(travelStart, _gameplayPosition);

        transform.position =
            _visualStartPosition +
            _visualDirection * _travelledDistance;

        if (_travelledDistance >= _maxDistance - 0.001f)
            Complete(_gameplayPosition, false, "maximum distance reached without hit");
    }

    // Purpose: Performs the complete workflow for this component.
    private void Complete(
        Vector3 worldPoint,
        bool emitImpactSound,
        string reason)
    {
        if (_completed)
            return;

        _completed = true;
        UnregisterActiveProjectile();

        if (emitImpactSound)
            _soundEmitter.EmitImpact(worldPoint, _direction);

        if (_debugHit)
        {
            Debug.Log(
                $"[BasicBullet] Completed. bullet={_bulletName}, reason={reason}, " +
                $"travelled={_travelledDistance:0.###}/{_maxDistance:0.###}, point={worldPoint}",
                this);
        }

        Destroy(gameObject);
    }

    // Purpose: Releases active subscriptions and transient state while disabled.
    private void OnDisable()
    {
        UnregisterActiveProjectile();
    }

    /// <summary>
    /// Finds a hostile basic bullet whose next predicted physics hit is the
    /// supplied subject. This intentionally ignores shots that will first hit
    /// cover or another target, preventing defensive dashes on harmless fire.
    /// </summary>
    public static bool TryGetClosestHostileIncomingThreat(
        GameObject subject,
        ShooterFaction subjectFaction,
        float predictionSeconds,
        out Vector3 incomingDirection,
        out int projectileId)
    {
        incomingDirection = default;
        projectileId = 0;

        if (subject == null)
            return false;

        Transform subjectRoot = EntityComponentResolver.ResolveEntityRoot(subject.transform) ?? subject.transform;
        float bestSqrDistance = float.PositiveInfinity;

        for (int i = ActiveProjectiles.Count - 1; i >= 0; i--)
        {
            BasicBullet projectile = ActiveProjectiles[i];
            if (projectile == null)
            {
                ActiveProjectiles.RemoveAt(i);
                continue;
            }

            if (!projectile.IsHostileTo(subjectRoot, subjectFaction) ||
                !projectile.WillHitSubjectWithin(subjectRoot, predictionSeconds))
            {
                continue;
            }

            float sqrDistance = PlanarSqrDistance(
                projectile._gameplayPosition,
                subjectRoot.position);
            if (sqrDistance >= bestSqrDistance)
                continue;

            bestSqrDistance = sqrDistance;
            incomingDirection = projectile._direction;
            projectileId = projectile.gameObject.GetInstanceID();
        }

        return projectileId != 0;
    }

    // Purpose: Attempts to get closest hit and reports whether the request succeeded.
    private bool TryGetClosestHit(
        float travelDistance,
        out RaycastHit closestHit,
        out bool isDamageHit)
    {
        closestHit = default;
        isDamageHit = false;

        bool found = false;
        float closestDistance = float.PositiveInfinity;

        if (TryGetClosestImpactHit(travelDistance, out RaycastHit impactHit))
        {
            found = true;
            closestDistance = impactHit.distance;
            closestHit = impactHit;
            isDamageHit = TryResolveDamageableTarget(
                              impactHit.collider,
                              out Damageable impactDamageable) &&
                          IsDamageTarget(impactDamageable);
        }

        if (TryGetClosestDamageHit(travelDistance, out RaycastHit damageHit))
        {
            if (!found || damageHit.distance <= closestDistance)
            {
                found = true;
                closestHit = damageHit;
                isDamageHit = true;
            }
        }

        return found;
    }

    // Purpose: Checks whether hostile to is currently true.
    private bool IsHostileTo(Transform subjectRoot, ShooterFaction subjectFaction)
    {
        if (!_initialized ||
            _completed ||
            _sourceEntityRoot == null ||
            subjectRoot == null ||
            subjectRoot == _sourceEntityRoot)
        {
            return false;
        }

        return ShooterFactionUtility.AreHostile(ShooterFactionUtility.Resolve(_sourceEntityRoot), subjectFaction);
    }

    // Purpose: Performs the will hit subject within workflow for this component.
    private bool WillHitSubjectWithin(Transform subjectRoot, float predictionSeconds)
    {
        if (subjectRoot == null)
            return false;

        float remainingDistance = _maxDistance - _travelledDistance;
        float predictionDistance = Mathf.Min(
            Mathf.Max(0f, predictionSeconds) * _speed,
            remainingDistance);

        if (predictionDistance <= 0.0001f ||
            !TryGetClosestHit(predictionDistance, out RaycastHit hit, out _))
        {
            return false;
        }

        Transform hitRoot = EntityComponentResolver.ResolveEntityRoot(hit.collider.transform);
        return hitRoot == subjectRoot || hit.collider.transform.IsChildOf(subjectRoot);
    }

    // Purpose: Attempts to get closest impact hit and reports whether the request succeeded.
    private bool TryGetClosestImpactHit(
        float travelDistance,
        out RaycastHit closestHit)
    {
        closestHit = default;

        if (_impactMask.value == 0)
            return false;

        RaycastHit[] hits = Physics.SphereCastAll(
            _gameplayPosition,
            _castRadius,
            _direction,
            travelDistance,
            _impactMask,
            _triggerInteraction);

        return TrySelectClosestHit(hits, false, out closestHit);
    }

    // Purpose: Attempts to get closest damage hit and reports whether the request succeeded.
    private bool TryGetClosestDamageHit(
        float travelDistance,
        out RaycastHit closestHit)
    {
        closestHit = default;

        if (_damageTargetMask.value == 0)
            return false;

        RaycastHit[] hits = Physics.SphereCastAll(
            _gameplayPosition,
            _castRadius,
            _direction,
            travelDistance,
            _damageTargetMask,
            QueryTriggerInteraction.Collide);

        return TrySelectClosestHit(hits, true, out closestHit);
    }

    // Purpose: Checks whether damage target layer is currently true.
    private bool IsDamageTargetLayer(int layer)
    {
        return (_damageTargetMask.value & (1 << layer)) != 0;
    }

    // Purpose: Attempts to select closest hit and reports whether the request succeeded.
    private bool TrySelectClosestHit(
        RaycastHit[] hits,
        bool requireDamageableTarget,
        out RaycastHit closestHit)
    {
        closestHit = default;

        if (hits == null || hits.Length == 0)
            return false;

        bool found = false;
        float closestDistance = float.PositiveInfinity;

        for (int i = 0; i < hits.Length; i++)
        {
            RaycastHit hit = hits[i];
            Collider collider = hit.collider;

            if (collider == null)
                continue;

            var passedCover=collider.GetComponentInParent<SectorDestructibleCover>();
            if(passedCover!=null&&_penetratedCovers.Contains(passedCover))continue;

            if (IsSourceEntityCollider(collider))
            {
                continue;
            }

            if (requireDamageableTarget &&
                (!TryResolveDamageableTarget(collider, out Damageable damageable) ||
                 !IsDamageTarget(damageable)))
            {
                continue;
            }

            if (!found || hit.distance < closestDistance)
            {
                found = true;
                closestDistance = hit.distance;
                closestHit = hit;
            }
        }

        return found;
    }

    // Purpose: Checks whether source entity collider is currently true.
    private bool IsSourceEntityCollider(Collider collider)
    {
        if (collider == null || _sourceEntityRoot == null)
            return false;

        Transform colliderTransform = collider.transform;
        return colliderTransform == _sourceEntityRoot ||
               colliderTransform.IsChildOf(_sourceEntityRoot);
    }

    // Purpose: Attempts to apply damage and reports whether the request succeeded.
    private void TryApplyDamage(RaycastHit hit)
    {
        if (hit.collider == null)
            return;

        if (!TryResolveDamageableTarget(hit.collider, out Damageable damageable))
        {
            if (_debugHit)
            {
                Debug.LogWarning(
                    $"[BasicBullet] Damage hit rejected: Damageable resolution failed. " +
                    $"collider={ResolveColliderPath(hit.collider)}",
                    hit.collider);
            }

            return;
        }

        if (!damageable.CanReceiveDamage)
        {
            if (_debugHit)
            {
                Debug.LogWarning(
                    $"[BasicBullet] Damage hit rejected: target cannot receive damage. " +
                    $"target={damageable.name}, dead={damageable.IsDead}, " +
                    $"invulnerable={damageable.IsInvulnerable}",
                    damageable);
            }

            return;
        }

        var infectionMarker=_source!=null?EntityComponentResolver.Resolve<InfectedEnemyMarker>(_source.transform):null;
        if(infectionMarker!=null)EntityComponentResolver.Resolve<PlayerShooterInfection>(damageable)?.AddExposure(infectionMarker.BulletExposure);

        if (_debugHit)
        {
            Debug.Log(
                $"[BasicBullet] Applying damage. target={damageable.name}, " +
                $"targetLayer={ResolveLayerName(damageable.gameObject.layer)}, " +
                $"collider={ResolveColliderPath(hit.collider)}, damage={_damage}, " +
                $"infection={_infectionDamageOnHit}, penetration={_penetrationClass}",
                damageable);
        }

        bool appliedAny = false;

        if (_damage > 0f)
        {
            bool headshot = damageable == _headshotCandidate && _headshotCandidate != null;
            float previousHealth = damageable.CurrentHealth;
            damageable.ReceiveAnAttack(
                _damage * (headshot ? _headshotDamageMultiplier : 1f),
                _source,
                _penetrationClass,
                _armorHealthDurabilityLossMultiplier);
            appliedAny = true;
            if (headshot && damageable.CurrentHealth < previousHealth)
            {
                QuestRuntime.ReportHeadshot(_source, damageable, damageable.IsDead, _shotWeapon);
                HeadshotConfirmed?.Invoke(_source, damageable, damageable.IsDead);
            }
        }

        if (TryApplyPaintMark(damageable))
            appliedAny = true;

        if (TryApplyInfectionDamage(damageable))
            appliedAny = true;

        if (TryApplyInfectionInjection(damageable))
            appliedAny = true;

        if (_authoredInfectionExposure > 0f && !damageable.IsDead)
        {
            damageable.AddInfectionExposure(_authoredInfectionExposure);
            appliedAny = true;
        }

        if (appliedAny)
        {
            TryApplyEnemyBulletInjuries(damageable);
            _shootHitConfirmedEvent?.RaiseEvent();
        }
    }

    // Purpose: Attempts to apply paint mark and reports whether the request succeeded.
    private bool TryApplyPaintMark(Damageable damageable)
    {
        // WIP: original hit-to-mark integration is archived outside Assets.
        return false;
    }

    // Purpose: Attempts to apply infection damage and reports whether the request succeeded.
    private bool TryApplyInfectionDamage(Damageable damageable)
    {
        if (!ProjectileCombatFeatureFlags.EnableLegacyBasicBulletInfectionDamage ||
            damageable == null || _infectionDamageOnHit <= 0f)
            return false;

        float infectionAmount =
            _infectionDamageOnHit *
            damageable.ResolveInfectionDamageMultiplierForPenetration(_penetrationClass);

        EntityStatusEffectRuntime statusEffects =
            EntityComponentResolver.Resolve<EntityStatusEffectRuntime>(damageable);

        if (statusEffects != null)
        {
            infectionAmount *= statusEffects.GetMultiplier(
                StatusEffectModifierType.DamageTakenMultiplier);
        }

        if (infectionAmount <= 0f)
            return false;

        damageable.ApplyArmorInfectionDurabilityDamage(
            _infectionDamageOnHit,
            infectionAmount,
            _armorInfectionDurabilityLossMultiplier);

        ShooterInfection infection =
            EntityComponentResolver.Resolve<ShooterInfection>(damageable);

        if (infection == null)
            return false;

        infection.AddInfection(infectionAmount);
        damageable.ReportDamageNumber(infectionAmount, DamageNumberKind.Infection);
        return true;
    }

    // Purpose: Attempts to apply infection injection and reports whether the request succeeded.
    private bool TryApplyInfectionInjection(Damageable damageable)
    {
        if (damageable == null ||
            _infectionInjectionDamagePerSecond <= 0f ||
            _infectionInjectionDurationSeconds <= 0f)
        {
            return false;
        }

        InfectionDamageOverTimeRuntime dot =
            damageable.GetComponent<InfectionDamageOverTimeRuntime>();

        if (dot == null)
            dot = damageable.gameObject.AddComponent<InfectionDamageOverTimeRuntime>();

        dot.Apply(
            damageable,
            _source,
            _infectionInjectionDamagePerSecond,
            _infectionInjectionDurationSeconds,
            _infectionInjectionTickInterval,
            _infectionInjectionStatusEffect);

        return true;
    }

    // Purpose: Attempts to apply enemy bullet injuries and reports whether the request succeeded.
    private void TryApplyEnemyBulletInjuries(Damageable damageable)
    {
        if (damageable == null || _source == null)
            return;

        PlayerStatsRuntime playerStats =
            EntityComponentResolver.Resolve<PlayerStatsRuntime>(damageable);

        if (playerStats == null)
            return;

        EnemyShooterStatsRuntime enemyStats =
           EntityComponentResolver.Resolve<EnemyShooterStatsRuntime>(_sourceEntityRoot);

        if (enemyStats == null)
            return;

        playerStats.TryApplyEnemyBulletInjuries();
    }

    // Purpose: Attempts to get projectile hurtbox and reports whether the request succeeded.
    private bool TryGetProjectileHurtbox(
        Collider collider,
        out EnemyProjectileHurtbox hurtbox)
    {
        hurtbox = null;

        if (collider == null)
            return false;

        EnemyProjectileHurtbox direct =
            collider.GetComponent<EnemyProjectileHurtbox>();

        if (IsAcceptedHurtbox(direct, collider, out hurtbox))
            return true;

        EnemyProjectileHurtbox[] parentHurtboxes =
            collider.GetComponentsInParent<EnemyProjectileHurtbox>(true);

        for (int i = 0; i < parentHurtboxes.Length; i++)
        {
            if (IsAcceptedHurtbox(
                    parentHurtboxes[i],
                    collider,
                    out hurtbox))
            {
                return true;
            }
        }

        Damageable damageable = collider.GetComponentInParent<Damageable>();

        if (damageable == null)
            return false;

        EnemyProjectileHurtbox[] childHurtboxes =
            damageable.GetComponentsInChildren<EnemyProjectileHurtbox>(true);

        for (int i = 0; i < childHurtboxes.Length; i++)
        {
            if (IsAcceptedHurtbox(
                    childHurtboxes[i],
                    collider,
                    out hurtbox))
            {
                return true;
            }
        }

        return false;
    }

    // Purpose: Attempts to resolve damageable target and reports whether the request succeeded.
    private bool TryResolveDamageableTarget(
        Collider collider,
        out Damageable damageable)
    {
        damageable = null;

        if (collider == null)
            return false;

        if (TryGetProjectileHurtbox(collider, out EnemyProjectileHurtbox hurtbox) &&
            hurtbox.TryGetDamageable(out damageable))
        {
            return damageable != null;
        }

        damageable = collider.GetComponent<Damageable>();

        if (damageable != null)
            return !HasConfiguredProjectileHurtbox(damageable);

        damageable = collider.GetComponentInParent<Damageable>();

        if (damageable == null)
        {
            Transform entityRoot =
                EntityComponentResolver.ResolveEntityRoot(collider.transform);

            if (entityRoot != null)
            {
                damageable = entityRoot.GetComponent<Damageable>() ??
                             entityRoot.GetComponentInChildren<Damageable>(true);
            }
        }

        return damageable != null &&
               !HasConfiguredProjectileHurtbox(damageable);
    }

    // Purpose: Checks whether damage target is currently true.
    private bool IsDamageTarget(Damageable damageable)
    {
        if (damageable == null)
            return false;

        Transform entityRoot =
            EntityComponentResolver.ResolveEntityRoot(damageable.transform);

        int entityLayer = entityRoot != null
            ? entityRoot.gameObject.layer
            : damageable.gameObject.layer;

        return IsDamageTargetLayer(entityLayer) ||
               IsDamageTargetLayer(damageable.gameObject.layer);
    }

    // Purpose: Checks whether this component has configured projectile hurtbox.
    private static bool HasConfiguredProjectileHurtbox(Damageable damageable)
    {
        return damageable != null &&
               damageable.GetComponentInChildren<EnemyProjectileHurtbox>(true) != null;
    }

    // Purpose: Checks whether accepted hurtbox is currently true.
    private static bool IsAcceptedHurtbox(
        EnemyProjectileHurtbox candidate,
        Collider collider,
        out EnemyProjectileHurtbox hurtbox)
    {
        hurtbox = null;

        if (candidate == null || !candidate.AcceptsCollider(collider))
            return false;

        hurtbox = candidate;
        return true;
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

    // Purpose: Resolves the object name needed by this component.
    private static string ResolveObjectName(Object value)
    {
        return value != null ? value.name : "None";
    }

    // Purpose: Resolves the layer name needed by this component.
    private static string ResolveLayerName(int layer)
    {
        string layerName = LayerMask.LayerToName(layer);
        return string.IsNullOrEmpty(layerName)
            ? layer.ToString()
            : $"{layerName}({layer})";
    }

    // Purpose: Performs the format layer mask workflow for this component.
    private static string FormatLayerMask(LayerMask mask)
    {
        return $"{mask.value} (0x{mask.value:X8})";
    }

    // Purpose: Registers active projectile with its owning runtime service.
    private void RegisterActiveProjectile()
    {
        if (!ActiveProjectiles.Contains(this))
            ActiveProjectiles.Add(this);
    }

    // Purpose: Unregisters active projectile from its owning runtime service.
    private void UnregisterActiveProjectile()
    {
        ActiveProjectiles.Remove(this);
    }

    // Purpose: Performs the planar sqr distance workflow for this component.
    private static float PlanarSqrDistance(Vector3 a, Vector3 b)
    {
        float x = a.x - b.x;
        float z = a.z - b.z;
        return x * x + z * z;
    }
}
