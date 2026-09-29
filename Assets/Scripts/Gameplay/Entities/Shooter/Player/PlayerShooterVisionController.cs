/*
 * Role hierarchy: Gameplay > Entities > Shooter > Player
 * Primary responsibility: Handles PlayerShooterVisionController camera, vision, range, or aiming calculations and presentation.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using UnityEngine;

[DisallowMultipleComponent]
public sealed class PlayerShooterVisionController : EntityVisionController
{
    [Header("Player Aim")]
    [SerializeField] private PlayerAimController _aimAction;

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

    // Purpose: Resolves the refs needed by this component.
    protected override void ResolveRefs()
    {
        base.ResolveRefs();

        if (_aimAction == null)
            _aimAction = EntityComponentResolver.Resolve<PlayerAimController>(this);
    }

    // Purpose: Resolves the aim01 needed by this component.
    protected override float ResolveAim01()
    {
        return _aimAction != null ? _aimAction.Aim01 : 0f;
    }

    // Purpose: Resolves the forward direction needed by this component.
    protected override Vector3 ResolveForwardDirection()
    {
        if (_aimAction != null &&
            _aimAction.TryGetFlatAimDirection(VisualOrigin.position, out Vector3 direction))
        {
            return direction;
        }

        return base.ResolveForwardDirection();
    }
}
