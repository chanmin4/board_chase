/*
 * Role hierarchy: Gameplay > Entities > Shooter > Player
 * Primary responsibility: Coordinates the PlayerClickController subsystem lifecycle and collaborating objects.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
public class PlayerClickController : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private InputReader _inputReader;
    [SerializeField] private PlayerCharacter _character;
    [SerializeField] private PlayerAimController _aimAction;
    [SerializeField] private PlayerShooterShoot _shoot;
    [SerializeField] private ShooterDashController _dashController;
    [SerializeField] private PlayerBulletLoadoutRuntime _bulletLoadout;
    [SerializeField] private ShooterEquipmentController _equipmentController;
    [SerializeField] private ShooterMeleeAttack _meleeAttack;
    [SerializeField] private ShooterThrowableAttack _throwableAttack;
    [SerializeField] private PlayerQuickSlotRuntime _quickSlots;

    private bool _isThrowableAimActive;
    private AreaAttackTelegraph _throwableWarningTelegraph;
    private AreaAttackTelegraph _rangeControlZoneTelegraph;

    // Purpose: Initializes Inspector defaults for this component.
    private void Reset()
    {
        ResolveRefs();
    }

    // Purpose: Caches the component references required before runtime initialization.
    private void Awake()
    {
        ResolveRefs();
        SetThrowablePreviewVisible(false);
    }

    // Purpose: Activates runtime subscriptions and refreshes the enabled state.
    private void OnEnable()
    {
        ResolveRefs();

        if (_inputReader != null)
        {
            _inputReader.MeleeAttackEvent += HandleMeleeAttackInput;
            _inputReader.ShootEvent += HandleShootStarted;
            _inputReader.ShootCanceledEvent += HandleShootCanceled;
            _inputReader.RightClickEvent += HandleRightClick;
        }
    }

    // Purpose: Releases active subscriptions and transient state while disabled.
    private void OnDisable()
    {
        if (_inputReader != null)
        {
            _inputReader.MeleeAttackEvent -= HandleMeleeAttackInput;
            _inputReader.ShootEvent -= HandleShootStarted;
            _inputReader.ShootCanceledEvent -= HandleShootCanceled;
            _inputReader.RightClickEvent -= HandleRightClick;
        }

        CancelThrowableAim();
        SetRangeControlZonePreviewVisible(false);
    }

    // Purpose: Processes frame work that must run after the regular Update pass.
    private void LateUpdate()
    {
        if (GamePause.IsPaused || Time.timeScale <= 0f)
        {
            CancelThrowableAim();
            SetRangeControlZonePreviewVisible(false);
            return;
        }

        // RightClickEvent belongs to the UI action map and is not guaranteed to
        // run while gameplay input is active. Poll the physical press as the
        // authoritative throwable-cancel input.
        if (_isThrowableAimActive &&
            Mouse.current != null &&
            Mouse.current.rightButton.wasPressedThisFrame)
        {
            CancelThrowableAim();
            return;
        }

        // ShootCanceled can be lost while the input action map changes (for
        // example after closing inventory/quick-slot UI). The physical release
        // is a safe fallback; TryReleaseThrowable clears the state first, so
        // receiving both callbacks can never throw twice.
        if (_isThrowableAimActive &&
            Mouse.current != null &&
            Mouse.current.leftButton.wasReleasedThisFrame)
        {
            TryReleaseThrowable();
        }

        if (_character == null || _aimAction == null)
        {
            CancelThrowableAim();
            SetRangeControlZonePreviewVisible(false);
            return;
        }

        UsableItemSO activeUsable = _quickSlots != null
            ? _quickSlots.ActiveUsableItem
            : null;

        if (activeUsable != null)
        {
            SetRangeControlZonePreviewVisible(false);

            if (activeUsable is ThrowableUsableItemSO)
                TickThrowableAimPreview();
            else
                CancelThrowableAim();

            return;
        }

        WeaponSO currentWeapon = _equipmentController != null
            ? _equipmentController.CurrentWeapon
            : null;

        CancelThrowableAim();

        if (currentWeapon is MeleeWeaponSO)
        {
            SetRangeControlZonePreviewVisible(false);
            return;
        }

        if (currentWeapon == null)
        {
            SetRangeControlZonePreviewVisible(false);
            return;
        }

        if (_bulletLoadout == null || _shoot == null)
        {
            SetRangeControlZonePreviewVisible(false);
            return;
        }

        TickRangeControlZonePreview();

        if (!_character.shootInput ||
            !TryGetSelectedShootAmmoType(out BulletAmmoType ammoType) ||
            !CanShoot())
        {
            return;
        }

        TryFireSelectedRanged(ammoType);
    }

    // Purpose: Handles the incoming shoot started request or event.
    private void HandleShootStarted()
    {
        ResolveRefs();

        if (_quickSlots != null && _quickSlots.ActiveUsableItem != null)
        {
            if (_quickSlots.ActiveThrowable != null)
            {
                if (CanUseThrowable())
                    _isThrowableAimActive = true;
            }
            else
            {
                bool started = _quickSlots.TryUseActiveConsumable(out string message);

                if (!string.IsNullOrWhiteSpace(message))
                    _quickSlots.RaiseMessage(message);

                if (!started)
                    CancelThrowableAim();
            }

            return;
        }

        if (_bulletLoadout == null || _shoot == null || !CanShoot())
            return;

        if (!TryGetSelectedShootAmmoType(out BulletAmmoType ammoType))
            return;

        // Every ranged weapon fires immediately on press, then continues at its
        // ShotsPerSecond cadence while the button is held.
        TryFireSelectedRanged(ammoType);
    }

    // Purpose: Attempts to fire selected ranged and reports whether the request succeeded.
    private void TryFireSelectedRanged(BulletAmmoType ammoType)
    {
        if (_shoot == null)
            return;

        // PlayerShooterShoot owns the final fire gate so compatibility is
        // checked before reload, ammo consumption, or projectile creation.
        _shoot.TryFireOnce(ammoType);
    }

    // Purpose: Handles the incoming shoot canceled request or event.
    private void HandleShootCanceled()
    {
        TryReleaseThrowable();
    }

    // Purpose: Attempts to release throwable and reports whether the request succeeded.
    private void TryReleaseThrowable()
    {
        if (!_isThrowableAimActive)
            return;

        Vector3 aimPoint = default;

        ThrowableUsableItemSO throwable = _quickSlots != null
            ? _quickSlots.ActiveThrowable
            : null;

        bool canThrow =
            throwable != null &&
            CanUseThrowable() &&
            _throwableAttack != null &&
            _aimAction != null &&
            _aimAction.TryGetRawAimPoint(out aimPoint);

        CancelThrowableAim();

        if (canThrow)
            _throwableAttack.TryThrowUsable(ShooterFaction.Player, throwable, aimPoint);
    }

    // Purpose: Handles the incoming right click request or event.
    private void HandleRightClick()
    {
        if (_isThrowableAimActive)
            CancelThrowableAim();
    }

    // Purpose: Processes one update step for throwable aim preview.
    private void TickThrowableAimPreview()
    {
        if (!_isThrowableAimActive)
        {
            SetThrowablePreviewVisible(false);
            return;
        }

        ThrowableUsableItemSO throwable = _quickSlots != null
            ? _quickSlots.ActiveThrowable
            : null;

        if (throwable == null || !CanUseThrowable())
        {
            CancelThrowableAim();
            return;
        }

        if (_throwableAttack == null ||
            _aimAction == null ||
            !_aimAction.TryGetRawAimPoint(out Vector3 aimPoint) ||
            !_throwableAttack.TryGetPrediction(
                throwable,
                aimPoint,
                out Vector3 predictedImpactPoint,
                out float explosionRadius))
        {
            SetThrowablePreviewVisible(false);
            return;
        }

        DrawThrowablePreview(predictedImpactPoint, explosionRadius);
    }

    // Purpose: Checks whether cancel throwable aim is currently permitted.
    private void CancelThrowableAim()
    {
        _isThrowableAimActive = false;
        SetThrowablePreviewVisible(false);
    }

    // Purpose: Checks whether use throwable is currently permitted.
    private bool CanUseThrowable()
    {
        return !GamePause.IsPaused &&
               Time.timeScale > 0f &&
               !GameplayActionInputBlocker.IsAttackBlocked &&
               (_dashController == null || !_dashController.IsDashing);
    }

    // Purpose: Draws the throwable preview authoring UI.
    private void DrawThrowablePreview(Vector3 center, float radius)
    {
        if (_throwableWarningTelegraph == null)
        {
            _throwableWarningTelegraph =
                AreaAttackTelegraph.SpawnPersistentCircle(
                    center,
                    radius,
                    AreaAttackTelegraphStyle.Default);
            return;
        }

        _throwableWarningTelegraph.SetPersistentCircle(center, radius);
    }

    // Purpose: Updates the throwable preview visible state.
    private void SetThrowablePreviewVisible(bool visible)
    {
        if (visible || _throwableWarningTelegraph == null)
            return;

        if (_throwableWarningTelegraph != null)
        {
            _throwableWarningTelegraph.CompleteAndDestroy();
            _throwableWarningTelegraph = null;
        }
    }

    // Purpose: Processes one update step for range control zone preview.
    private void TickRangeControlZonePreview()
    {
        if (GameplayActionInputBlocker.IsAttackBlocked ||
            _aimAction == null ||
            _bulletLoadout == null ||
            !_bulletLoadout.HasLoadedAmmo(BulletAmmoType.Special) ||
            !_bulletLoadout.TryGetSelectedAmmoType(out BulletAmmoType ammoType) ||
            ammoType != BulletAmmoType.Special ||
            !_bulletLoadout.TryGetActiveSpecialBullet(out SpecialBulletSO bullet) ||
            !bullet.RangeControlZone ||
            bullet.RangeControlZoneRadius <= 0f ||
            !_aimAction.TryGetRawAimPoint(out Vector3 aimPoint))
        {
            SetRangeControlZonePreviewVisible(false);
            return;
        }

        Vector3 center = _aimAction.ClampToRange(aimPoint);

        if (_rangeControlZoneTelegraph == null)
        {
            _rangeControlZoneTelegraph =
                AreaAttackTelegraph.SpawnPersistentCircle(
                    center,
                    bullet.RangeControlZoneRadius,
                    AreaAttackTelegraphStyle.Default);
            return;
        }

        _rangeControlZoneTelegraph.SetPersistentCircle(
            center,
            bullet.RangeControlZoneRadius);
    }

    // Purpose: Updates the range control zone preview visible state.
    private void SetRangeControlZonePreviewVisible(bool visible)
    {
        if (visible || _rangeControlZoneTelegraph == null)
            return;

        _rangeControlZoneTelegraph.CompleteAndDestroy();
        _rangeControlZoneTelegraph = null;
    }

    // Purpose: Resolves the refs needed by this component.
    private void ResolveRefs()
    {
        if (_inputReader == null)
            _inputReader = FindAnyObjectByType<InputReader>();

        if (_character == null)
            _character = GetComponent<PlayerCharacter>();

        if (_aimAction == null)
            _aimAction = EntityComponentResolver.Resolve<PlayerAimController>(this);

        if (_shoot == null)
            _shoot = EntityComponentResolver.Resolve<PlayerShooterShoot>(this);

        if (_dashController == null)
            _dashController = EntityComponentResolver.Resolve<ShooterDashController>(this);

        if (_bulletLoadout == null)
        {
            _bulletLoadout =
                EntityComponentResolver.Resolve<PlayerBulletLoadoutRuntime>(this);
        }

        if (_equipmentController == null)
        {
            _equipmentController =
                EntityComponentResolver.Resolve<ShooterEquipmentController>(this);
        }

        if (_meleeAttack == null)
            _meleeAttack = EntityComponentResolver.Resolve<ShooterMeleeAttack>(this);

        if (_throwableAttack == null)
        {
            _throwableAttack =
                EntityComponentResolver.Resolve<ShooterThrowableAttack>(this);
        }

        if (_quickSlots == null)
            _quickSlots = EntityComponentResolver.Resolve<PlayerQuickSlotRuntime>(this);
    }

    // Purpose: Handles the incoming melee attack input request or event.
    private void HandleMeleeAttackInput()
    {
        ResolveRefs();

        TryStartMeleeAttack();
    }

    // Purpose: Attempts to start melee attack and reports whether the request succeeded.
    private bool TryStartMeleeAttack()
    {
        if (GamePause.IsPaused ||
            Time.timeScale <= 0f ||
            GameplayActionInputBlocker.IsAttackBlocked ||
            (_dashController != null && _dashController.IsDashing) ||
            (_aimAction != null && (_aimAction.IsReloading || _aimAction.IsFunctionChecking)) ||
            (_quickSlots != null && _quickSlots.ActiveUsableItem != null) ||
            _meleeAttack == null ||
            _meleeAttack.PlayerCombatKnife == null)
        {
            return false;
        }

        // Melee range starts from the shooter body, not a ranged muzzle or the
        // animated held model. The dedicated hit zone applies its height offset.
        Vector3 origin = _meleeAttack.transform.position;

        Vector3 direction = transform.forward;

        if (_aimAction != null &&
            _aimAction.TryGetRawAimPoint(out Vector3 aimPoint))
        {
            direction = aimPoint - origin;
        }

        direction.y = 0f;

        if (direction.sqrMagnitude <= 0.0001f)
            direction = Vector3.forward;

        return _meleeAttack.TryAttackCurrentWeapon(
            ShooterFaction.Player,
            origin,
            direction.normalized);
    }

    // Purpose: Attempts to get selected shoot ammo type and reports whether the request succeeded.
    private bool TryGetSelectedShootAmmoType(out BulletAmmoType ammoType)
    {
        ammoType = BulletAmmoType.Basic;

        if (!_bulletLoadout.TryGetSelectedAmmoType(out ammoType))
            return true;

        return ammoType == BulletAmmoType.Basic ||
               ammoType == BulletAmmoType.Special;
    }

    // Purpose: Checks whether shoot is currently permitted.
    private bool CanShoot()
    {
        return (_dashController == null || !_dashController.IsDashing) &&
               (_meleeAttack == null || !_meleeAttack.IsAttacking);
    }
}
