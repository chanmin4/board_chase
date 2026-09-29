/*
 * Role hierarchy: Gameplay > Entities > Shooter > Common > StateMachine > Action
 * Primary responsibility: Implements the focused ShooterLocomotionActionBase responsibility and collaborates with its role group.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using UnityEngine;
using VSplatter.StateMachine;

public abstract class ShooterLocomotionActionBase: StateAction
{
    protected ShooterStatsRuntime StatsRuntime { get; private set; }
    protected ShooterEffectController Effects { get; private set; }
    protected ShooterDashController DashController { get; private set; }

    // Purpose: Caches the component references required before runtime initialization.
    public override void Awake(StateMachine stateMachine)
    {
        if (stateMachine == null)
            return;

        StatsRuntime = ResolveComponent<ShooterStatsRuntime>(stateMachine);
        Effects = ResolveComponent<ShooterEffectController>(stateMachine);
        DashController = ResolveComponent<ShooterDashController>(stateMachine);

        OnAwakeLocomotion(stateMachine);
    }

    // Purpose: Responds to the awake locomotion callback.
    protected virtual void OnAwakeLocomotion(StateMachine stateMachine)
    {
    }

    // Purpose: Checks whether update blocked is currently true.
    protected bool IsUpdateBlocked()
    {
        return GamePause.IsPaused || Time.timeScale <= 0f;
    }

    // Purpose: Resolves the move speed needed by this component.
    protected float ResolveMoveSpeed()
    {
        if (StatsRuntime == null)
            return 0f;

        return Mathf.Max(0f, StatsRuntime.MoveSpeed);
    }

    // Purpose: Attempts to build dash vector and reports whether the request succeeded.
    protected bool TryBuildDashVector(float downwardVelocity, out Vector3 dashVector)
    {
        dashVector = Vector3.zero;

        if (DashController == null || !DashController.IsDashing)
            return false;

        dashVector = DashController.BuildDashMovementVector(downwardVelocity);
        return true;
    }

    // Purpose: Updates the foot trail by planar motion state.
    protected void SetFootTrailByPlanarMotion(Vector3 planarMotion)
    {
        if (Effects == null)
            return;

        planarMotion.y = 0f;
        Effects.SetFootTrailActive(planarMotion.sqrMagnitude > 0.0001f);
    }

    // Purpose: Stops the active foot trail behavior.
    protected void StopFootTrail()
    {
        Effects?.SetFootTrailActive(false);
    }

    protected static T ResolveComponent<T>(StateMachine stateMachine)
        where T : Component
    {
        if (stateMachine == null)
            return null;

        if (stateMachine.TryGetComponent(out T component))
            return component;

        component = stateMachine.GetComponentInChildren<T>(true);
        if (component != null)
            return component;

        return stateMachine.GetComponentInParent<T>();
    }
}
