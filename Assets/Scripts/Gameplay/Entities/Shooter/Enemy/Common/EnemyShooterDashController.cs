/*
 * Role hierarchy: Gameplay > Entities > Shooter > Enemy > Common
 * Primary responsibility: Coordinates the EnemyShooterDashController subsystem lifecycle and collaborating objects.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using UnityEngine;

[DisallowMultipleComponent]
public sealed class EnemyShooterDashController : ShooterDashController
{
    [Header("Enemy Refs")]
    [SerializeField] private EnemyShooterShoot _shoot;
    [SerializeField] private EnemyShooterCombatController _combat;

    private EnemyShooterConfigSO EnemyConfig => _shoot != null ? _shoot.Config : null;

    // Normal enemies intentionally share the PlayerDashConfig assigned on the
    // base prefab. The combat controller may still provide temporary values
    // from Debug Monitor, but those should never replace the common player
    // dash in normal gameplay.
    private bool HasRuntimeDashOverride =>
        _combat != null && _combat.HasRuntimeCombatOverrides;

    // Purpose: Attempts to start defensive dash and reports whether the request succeeded.
    public bool TryStartDefensiveDash(Vector3 direction)
    {
        return TryStartDash(direction);
    }

    // Purpose: Initializes Inspector defaults for this component.
    protected override void Reset()
    {
        base.Reset();
        ResolveRefs();
    }

    // Purpose: Caches the component references required before runtime initialization.
    protected override void Awake()
    {
        base.Awake();
        ResolveRefs();
    }

    // Purpose: Checks whether start dash is currently permitted.
    protected override bool CanStartDash()
    {
        return _combat != null
            ? _combat.EffectiveUseDash
            : EnemyConfig != null && EnemyConfig.UseDash;
    }

    // Purpose: Performs the use forward when no movement input workflow for this component.
    protected override bool UseForwardWhenNoMovementInput()
    {
        return false;
    }

    // Purpose: Resolves the dash duration needed by this component.
    protected override float ResolveDashDuration()
    {
        return HasRuntimeDashOverride
            ? _combat.EffectiveDashDurationSeconds
            : base.ResolveDashDuration();
    }

    // Purpose: Resolves the dash cooldown needed by this component.
    protected override float ResolveDashCooldown()
    {
        return HasRuntimeDashOverride
            ? _combat.EffectiveDashCooldownSeconds
            : base.ResolveDashCooldown();
    }

    // Purpose: Resolves the dash speed needed by this component.
    protected override float ResolveDashSpeed()
    {
        return HasRuntimeDashOverride
            ? _combat.EffectiveDashSpeed
            : base.ResolveDashSpeed();
    }

    // Purpose: Resolves the refs needed by this component.
    protected override void ResolveRefs()
    {
        base.ResolveRefs();

        if (_shoot == null)
            _shoot = EntityComponentResolver.Resolve<EnemyShooterShoot>(this);

        if (_combat == null)
            _combat = EntityComponentResolver.Resolve<EnemyShooterCombatController>(this);
    }
}
