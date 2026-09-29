/*
 * Role hierarchy: Gameplay > Entities > Shooter > Player
 * Primary responsibility: Implements the focused PlayerShooterShoot responsibility and collaborates with its role group.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using UnityEngine;
using UnityEngine.Serialization;
using TMPro;

[DisallowMultipleComponent]
public class PlayerShooterShoot : EntityShootCore
{
    [Header("Refs")]
    [SerializeField] private PlayerAimController _aimAction;
    [SerializeField] private EntityRange _range;
    [SerializeField] private PlayerStatsRuntime _statsRuntime;
    [SerializeField] private PlayerBulletLoadoutRuntime _bulletLoadout;
    [FormerlySerializedAs("_weaponHolder")]
    [SerializeField] private ShooterEquipmentController _equipmentController;
    [SerializeField] private ShooterDashController _dashController;
    [SerializeField] private EntityShooterCharacterAudio _audio;

    [Header("Events")]
    [SerializeField] private MaskRenderManagerEventChannelSO _maskRenderManagerReadyChannel;
    [SerializeField] private ShootHitConfirmedEventChannelSO _shootHitConfirmedEvent;
    [SerializeField] private SoundStimulusEventChannelSO _soundStimulusEvent;
    [Tooltip("Shows invalid weapon/ammunition combinations to the player without consuming ammunition.")]
    [SerializeField] private SystemMessageEventChannelSO _systemMessageChannel;

    [Header("Invalid Action Feedback")]
    [Tooltip("World-space font size of the red X shown when the selected weapon and ammunition cannot be used together.")]
    [SerializeField, Min(1f)] private float _invalidActionIconFontSize = 12f;
    [Tooltip("Moves the invalid-action X upward on screen from the entity UIAnchor. The offset follows the gameplay camera's screen-up direction.")]
    [SerializeField, Min(0f)] private float _invalidActionIconScreenUpOffset = 1.1f;

    [Header("Debug")]
    [SerializeField] private bool _debugLogs = false;
    [SerializeField] private bool _debugDraw = false;
    [SerializeField, Min(0f)] private float _debugDrawDuration = 0.15f;

    private const ShooterFaction ShooterSide = ShooterFaction.Player;
    private const PaintMarkFaction HitMarkSide = PaintMarkFaction.Vaccine;

    private MaskRenderManager _maskRenderManager;
    private Transform _entityRoot;
    private float _nextCompatibilityFeedbackTime;
    private TextMeshPro _invalidActionText;
    private Transform _invalidActionAnchor;
    private Camera _invalidActionCamera;
    private float _invalidActionHideAt;

    public SoundStimulusEventChannelSO SoundStimulusEventChannel => _soundStimulusEvent;

    private Transform VisualFireOrigin =>
        _equipmentController != null && _equipmentController.VisualFireOrigin != null
            ? _equipmentController.VisualFireOrigin
            : null;

    private Transform RangeOrigin =>
        _range != null && _range.RangeOrigin != null
            ? _range.RangeOrigin
            : transform;

    private Transform ProjectilesRoot =>
        _equipmentController != null ? _equipmentController.ProjectilesRoot : null;

    private GameObject ProjectileSource
    {
        get
        {
            if (_entityRoot == null)
                _entityRoot = EntityComponentResolver.ResolveEntityRoot(this);

            return _entityRoot != null ? _entityRoot.gameObject : gameObject;
        }
    }

    private float MaxRange =>
        _range != null
            ? Mathf.Max(0.1f, _range.MaxRange)
            : _statsRuntime != null ? Mathf.Max(0.1f, _statsRuntime.MaxRange) : 0.1f;

    // Purpose: Initializes Inspector defaults for this component.
    private void Reset()
    {
        ResolveRefs();
    }

    // Purpose: Caches the component references required before runtime initialization.
    private void Awake()
    {
        ResolveRefs();
        EnsureFootstepSoundEmitter();
    }

    // Purpose: Activates runtime subscriptions and refreshes the enabled state.
    private void OnEnable()
    {
        if (_maskRenderManagerReadyChannel != null)
        {
            _maskRenderManagerReadyChannel.OnEventRaised += HandleMaskRenderManagerReady;

            if (_maskRenderManagerReadyChannel.Current != null)
                HandleMaskRenderManagerReady(_maskRenderManagerReadyChannel.Current);
        }
    }

    // Purpose: Releases active subscriptions and transient state while disabled.
    private void OnDisable()
    {
        if (_maskRenderManagerReadyChannel != null)
            _maskRenderManagerReadyChannel.OnEventRaised -= HandleMaskRenderManagerReady;

        _maskRenderManager = null;

        if (_invalidActionText != null)
            _invalidActionText.gameObject.SetActive(false);
    }

    // Purpose: Processes frame work that must run after the regular Update pass.
    private void LateUpdate()
    {
        if (_invalidActionText == null || !_invalidActionText.gameObject.activeSelf)
            return;

        if (Time.unscaledTime >= _invalidActionHideAt)
        {
            _invalidActionText.gameObject.SetActive(false);
            return;
        }

        UpdateInvalidActionIconTransform();
    }

    // Purpose: Attempts to fire once and reports whether the request succeeded.
    public bool TryFireOnce(BulletAmmoType ammoType)
    {
        ResolveRefs();

        if (!CanAttemptFire(ammoType, out bool dryFire))
            return false;

        if (dryFire)
        {
            PlayEmptyShoot(ammoType);
            return false;
        }

        if (!_aimAction.TryGetAimPoint(out Vector3 aimWorldPoint))
            return false;

        return ammoType switch
        {
            BulletAmmoType.Basic => TryFirePrimary(aimWorldPoint),
            BulletAmmoType.Special => TryFireSpecial(aimWorldPoint),
            _ => false
        };
    }

    // Purpose: Checks whether attempt fire is currently permitted.
    private bool CanAttemptFire(BulletAmmoType ammoType, out bool dryFire)
    {
        dryFire = false;

        if (_aimAction == null || _bulletLoadout == null)
            return false;

        if (GameplayActionInputBlocker.IsAttackBlocked)
            return false;

        // A shot cannot overlap the fallback swing if reload/equipment changes mid-swing.
        ShooterMeleeAttack melee = EntityComponentResolver.Resolve<ShooterMeleeAttack>(this);
        if (melee != null && melee.IsAttacking) return false;

        if (_dashController != null && _dashController.IsDashing)
            return false;

        if (_equipmentController == null ||
            _equipmentController.CurrentWeapon is not RangedWeaponSO rangedWeapon)
        {
            return false;
        }

        if (!_bulletLoadout.TryGetActiveBullet(ammoType, out BulletSO selectedBullet) ||
            selectedBullet == null)
        {
            ShowMissingAmmoFeedback(ammoType);
            return false;
        }

        if (!rangedWeapon.IsCompatibleWith(selectedBullet))
        {
            ShowIncompatibleAmmoFeedback(rangedWeapon, selectedBullet);
            return false;
        }

        EntityEquipmentRuntime equipmentRuntime = _equipmentController.EquipmentRuntime;
        if (equipmentRuntime != null && !equipmentRuntime.CanFireCurrentWeapon)
        {
            ShowBrokenWeaponFeedback(rangedWeapon);
            return false;
        }

        if (!_aimAction.CanFireNowFor(ammoType))
            return false;

        if (!_bulletLoadout.HasLoadedAmmo(ammoType))
        {
            dryFire = true;
            return true;
        }

        return true;
    }

    // Purpose: Plays the requested empty shoot action or effect.
    private void PlayEmptyShoot(BulletAmmoType ammoType)
    {
        _audio?.PlayEmptyShoot();
        _aimAction?.ApplyDryFireCooldown(ammoType);

        if (_debugLogs)
            Debug.Log($"[PlayerShooterShoot] Empty shot. ammoType={ammoType}", this);
    }

    // Purpose: Attempts to fire primary and reports whether the request succeeded.
    private bool TryFirePrimary(Vector3 aimWorldPoint)
    {
        if (!_bulletLoadout.TryGetActivePrimaryBullet(out BulletSO bullet))
            return false;

        if (!ValidatePrimaryBullet(bullet))
            return false;

        EntityShootStats stats = BuildPrimaryStats(bullet);

        Transform visualFireOrigin = VisualFireOrigin;
        Transform rangeOrigin = RangeOrigin;

        if (visualFireOrigin == null || rangeOrigin == null)
        {
            Debug.LogError(
                "[PlayerShooterShoot] Fire origin or range origin is missing. Assign WeaponSO WeaponViewPrefab with WeaponView Fire Origin.",
                this);
            return false;
        }

        aimWorldPoint = ApplyAimError(visualFireOrigin.position, aimWorldPoint);

        Vector3 visualAimPoint = aimWorldPoint;
        visualAimPoint.y = visualFireOrigin.position.y;

        if (!TryResolveShotFromAimPoints(
                bullet,
                aimWorldPoint,
                visualAimPoint,
                stats.maxRange,
                visualFireOrigin,
                rangeOrigin,
                out Vector3 gameplayStart,
                out Vector3 gameplayDirection,
                out Vector3 visualDirection,
                out float maxDistance,
                out Vector3 visualSpawn))
        {
            return false;
        }

        if (!_bulletLoadout.TryConsumePrimaryAmmo(1, out bullet))
            return false;

        bool fired = TryFirePrimary(
            bullet,
            _equipmentController != null
                ? _equipmentController.CurrentWeapon as RangedWeaponSO
                : null,
            gameplayStart,
            gameplayDirection,
            visualDirection,
            maxDistance,
            visualSpawn,
            ProjectilesRoot,
            stats,
            ProjectileSource,
            _soundStimulusEvent,
            _shootHitConfirmedEvent,
            _debugLogs,
            _debugDraw,
            _debugDrawDuration);

        if (fired)
        {
            _equipmentController?.PlayMuzzleParticle();
            _aimAction?.ApplyShotRecoil();
            PublishGunshotSound(visualFireOrigin.position);
            _equipmentController?.EquipmentRuntime?.ApplyCurrentWeaponFireWear();
        }

        return fired;
    }

    // Purpose: Attempts to fire special and reports whether the request succeeded.
    private bool TryFireSpecial(Vector3 aimWorldPoint)
    {
        if (!_bulletLoadout.TryGetActiveSpecialBullet(out SpecialBulletSO bullet))
            return false;

        if (!ValidateSpecialBullet(bullet))
            return false;

        Transform visualFireOrigin = VisualFireOrigin;
        Transform rangeOrigin = RangeOrigin;

        if (visualFireOrigin == null || rangeOrigin == null)
        {
            Debug.LogError(
                "[PlayerShooterShoot] Fire origin or range origin is missing. Assign WeaponSO WeaponViewPrefab with WeaponView Fire Origin.",
                this);
            return false;
        }

        // Ground-targeted zone specials must land at the warning-circle center.
        // Other specials keep the normal firearm accuracy calculation.
        if (!bullet.RangeControlZone)
            aimWorldPoint = ApplyAimError(visualFireOrigin.position, aimWorldPoint);

        Vector3 visualAimPoint = aimWorldPoint;
        visualAimPoint.y = visualFireOrigin.position.y;

        if (!TryResolveSpecialShotFromAimPoints(
                bullet,
                aimWorldPoint,
                visualAimPoint,
                MaxRange,
                visualFireOrigin,
                rangeOrigin,
                out Vector3 gameplayStart,
                out Vector3 gameplayDirection,
                out Vector3 visualSpawn,
                out Vector3 visualTarget,
                out Vector3 impactTarget))
        {
            return false;
        }

        _maskRenderManager = ResolveMaskRenderManager(_maskRenderManager);

        if (bullet.RequiresMaskRenderManager && _maskRenderManager == null)
        {
            Debug.LogError(
                $"[PlayerShooterShoot] Special bullet requires MaskRenderManager: {bullet.name}",
                bullet);
            return false;
        }

        if (!_bulletLoadout.TryConsumeSpecialAmmo(1, out bullet))
            return false;

        bool fired = TryFireSpecial(
            bullet,
            gameplayStart,
            gameplayDirection,
            visualSpawn,
            visualTarget,
            impactTarget,
            ProjectilesRoot,
            _maskRenderManager,
            ProjectileSource,
            _soundStimulusEvent,
            _debugLogs,
            _debugDraw,
            _debugDrawDuration);

        if (fired)
        {
            _equipmentController?.PlayMuzzleParticle();
            _aimAction?.ApplyShotRecoil();
            PublishGunshotSound(visualFireOrigin.position);
            _equipmentController?.EquipmentRuntime?.ApplyCurrentWeaponFireWear();
        }

        return fired;
    }

    // Purpose: Shows the incompatible ammo feedback UI or visual state.
    private void ShowIncompatibleAmmoFeedback(
        RangedWeaponSO weapon,
        BulletSO bullet)
    {
        if (Time.unscaledTime < _nextCompatibilityFeedbackTime)
            return;

        _nextCompatibilityFeedbackTime = Time.unscaledTime + 0.5f;
        _systemMessageChannel?.RaiseEvent("Not compatible Weapon&Bullet");

        Transform root = _entityRoot != null
            ? _entityRoot
            : EntityComponentResolver.ResolveEntityRoot(this);

        if (root != null)
            ShowInvalidActionIcon(root);

        if (_debugLogs)
        {
            Debug.LogWarning(
                $"[PlayerShooterShoot] Incompatible ammo. weapon={weapon.name}, weaponType={weapon.WeaponType}, required={weapon.RequiredBulletFamily}, bullet={bullet.name}, family={bullet.Family}",
                this);
        }
    }

    // Purpose: Shows the missing ammo feedback UI or visual state.
    private void ShowMissingAmmoFeedback(BulletAmmoType ammoType)
    {
        if (Time.unscaledTime < _nextCompatibilityFeedbackTime)
            return;

        _nextCompatibilityFeedbackTime = Time.unscaledTime + 0.5f;
        _systemMessageChannel?.RaiseEvent("No bullet equipped");

        Transform root = _entityRoot != null
            ? _entityRoot
            : EntityComponentResolver.ResolveEntityRoot(this);

        if (root != null)
            ShowInvalidActionIcon(root);

        if (_debugLogs)
        {
            Debug.LogWarning(
                $"[PlayerShooterShoot] No equipped bullet for fire input. ammoType={ammoType}",
                this);
        }
    }

    // Purpose: Shows the broken weapon feedback UI or visual state.
    private void ShowBrokenWeaponFeedback(RangedWeaponSO weapon)
    {
        if (Time.unscaledTime < _nextCompatibilityFeedbackTime)
            return;

        _nextCompatibilityFeedbackTime = Time.unscaledTime + 0.5f;
        _systemMessageChannel?.RaiseEvent("Weapon durability is 0");

        Transform root = _entityRoot != null
            ? _entityRoot
            : EntityComponentResolver.ResolveEntityRoot(this);

        if (root != null)
            ShowInvalidActionIcon(root);

        if (_debugLogs)
            Debug.LogWarning($"[PlayerShooterShoot] Weapon is broken. weapon={weapon.name}", this);
    }

    // Purpose: Shows the invalid action icon UI or visual state.
    private void ShowInvalidActionIcon(Transform entityRoot)
    {
        EnsureInvalidActionIcon(entityRoot);

        if (_invalidActionText == null)
            return;

        _invalidActionText.text = "X";
        _invalidActionText.color = new Color(1f, 0.12f, 0.12f, 1f);
        _invalidActionText.gameObject.SetActive(true);
        _invalidActionHideAt = Time.unscaledTime + 0.75f;
        UpdateInvalidActionIconTransform();
    }

    // Purpose: Ensures invalid action icon is ready before the workflow continues.
    private void EnsureInvalidActionIcon(Transform entityRoot)
    {
        if (_invalidActionText != null)
            return;

        _invalidActionAnchor = FindDescendant(entityRoot, "UIAnchor");
        if (_invalidActionAnchor == null)
            _invalidActionAnchor = entityRoot;

        const string iconObjectName = "InvalidAmmoIcon";
        Transform existing = _invalidActionAnchor.Find(iconObjectName);
        GameObject iconObject;

        if (existing != null)
        {
            iconObject = existing.gameObject;
            _invalidActionText = iconObject.GetComponent<TextMeshPro>();
        }
        else
        {
            iconObject = new GameObject(iconObjectName);
            iconObject.transform.SetParent(_invalidActionAnchor, false);
            _invalidActionText = iconObject.AddComponent<TextMeshPro>();
        }

        if (_invalidActionText == null)
            return;

        _invalidActionText.alignment = TextAlignmentOptions.Center;
        _invalidActionText.fontSize = _invalidActionIconFontSize;
        _invalidActionText.fontStyle = FontStyles.Bold;
        _invalidActionText.textWrappingMode = TextWrappingModes.NoWrap;
        _invalidActionText.rectTransform.sizeDelta = new Vector2(6f, 6f);

        MeshRenderer renderer = _invalidActionText.GetComponent<MeshRenderer>();
        if (renderer != null)
            renderer.sortingOrder = 1000;

        iconObject.SetActive(false);
    }

    // Purpose: Updates the invalid action icon transform from the current runtime state.
    private void UpdateInvalidActionIconTransform()
    {
        if (_invalidActionText == null || _invalidActionAnchor == null)
            return;

        if (_invalidActionCamera == null)
            _invalidActionCamera = Camera.main;

        Transform iconTransform = _invalidActionText.transform;
        Vector3 screenUp = _invalidActionCamera != null
            ? _invalidActionCamera.transform.up
            : Vector3.up;
        iconTransform.position =
            _invalidActionAnchor.position + screenUp * _invalidActionIconScreenUpOffset;

        if (_invalidActionCamera != null)
            iconTransform.rotation = _invalidActionCamera.transform.rotation;
    }

    // Purpose: Finds the requested descendant.
    private static Transform FindDescendant(Transform root, string objectName)
    {
        if (root == null)
            return null;

        if (root.name == objectName)
            return root;

        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = FindDescendant(root.GetChild(i), objectName);
            if (found != null)
                return found;
        }

        return null;
    }

    // Purpose: Builds the primary stats from the current authored data.
    private EntityShootStats BuildPrimaryStats(BulletSO bullet)
    {
        return new EntityShootStats(
            MaxRange,
            _statsRuntime != null ? _statsRuntime.ResolveAttackDamage(bullet) : 0f,
            bullet != null ? bullet.ResolveDamageTargetMask(ShooterSide) : default,
            HitMarkSide,
            _statsRuntime != null ? _statsRuntime.ResolvePaintMarkDamage(bullet) : 0f,
            _statsRuntime != null ? _statsRuntime.ResolveInfectionDamage(bullet) : 0f,
            _statsRuntime != null ? _statsRuntime.ResolvePenetrationClass(bullet) : bullet != null ? bullet.PenetrationClass : 0,
            _statsRuntime != null ? _statsRuntime.ResolveArmorHealthDurabilityLossMultiplier(bullet) : 1f,
            _statsRuntime != null ? _statsRuntime.ResolveArmorInfectionDurabilityLossMultiplier(bullet) : 0.5f);
    }

    // Purpose: Handles the incoming mask render manager ready request or event.
    private void HandleMaskRenderManagerReady(MaskRenderManager manager)
    {
        if (manager != null)
            _maskRenderManager = manager;
    }

    // Purpose: Resolves the refs needed by this component.
    private void ResolveRefs()
    {
        if (_entityRoot == null)
            _entityRoot = EntityComponentResolver.ResolveEntityRoot(this);

        if (_aimAction == null)
            _aimAction = EntityComponentResolver.Resolve<PlayerAimController>(this);

        if (_range == null)
            _range = _aimAction != null
                ? _aimAction
                : EntityComponentResolver.Resolve<EntityRange>(this);

        if (_statsRuntime == null)
            _statsRuntime = EntityComponentResolver.Resolve<PlayerStatsRuntime>(this);

        if (_bulletLoadout == null)
            _bulletLoadout = EntityComponentResolver.Resolve<PlayerBulletLoadoutRuntime>(this);

        if (_equipmentController == null)
            _equipmentController = EntityComponentResolver.Resolve<ShooterEquipmentController>(this);

        if (_dashController == null)
            _dashController = EntityComponentResolver.Resolve<ShooterDashController>(this);

        if (_audio == null)
            _audio = EntityComponentResolver.Resolve<EntityShooterCharacterAudio>(this);
    }

    // Purpose: Ensures footstep sound emitter is ready before the workflow continues.
    private void EnsureFootstepSoundEmitter()
    {
        if (_soundStimulusEvent == null || _statsRuntime == null)
            return;

        EntityFootstepSoundEmitter emitter =
            EntityComponentResolver.Resolve<EntityFootstepSoundEmitter>(this);

        if (emitter == null)
            emitter = _statsRuntime.gameObject.AddComponent<EntityFootstepSoundEmitter>();

        emitter.Configure(_statsRuntime, _soundStimulusEvent, _statsRuntime.transform);
    }

    // Purpose: Publishes the gunshot sound notification.
    private void PublishGunshotSound(Vector3 position)
    {
        if (_soundStimulusEvent == null || _statsRuntime == null)
            return;

        // StatsRuntime already includes the equipped weapon's sound modifier.
        float radius = _statsRuntime.GunshotSoundRadius;

        _soundStimulusEvent.RaiseEvent(new SoundStimulus(
            gameObject,
            position,
            radius,
            _statsRuntime.SoundInvestigateDelaySeconds,
            SoundStimulusType.Gunshot));
    }

    // Purpose: Applies the requested aim error change.
    private Vector3 ApplyAimError(Vector3 origin, Vector3 aimWorldPoint)
    {
        return _aimAction != null
            ? _aimAction.ApplyAccuracyAndRecoil(origin, aimWorldPoint)
            : aimWorldPoint;
    }
}
