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
    private Vector3 _presentationForward;
    private int _presentationFrame = -1;
    private float _presentationAim01;

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
        if (!GameplayActionInputBlocker.IsAimBlocked)
            _presentationAim01 = _aimAction != null ? _aimAction.Aim01 : 0f;
        return _presentationAim01;
    }

    // Purpose: Resolves the forward direction needed by this component.
    public void RefreshPresentationAimPoint()
    {
        if (GameplayActionInputBlocker.IsAimBlocked)
        {
            if (_presentationForward.sqrMagnitude <= .0001f)
                _presentationForward = base.ResolveForwardDirection();
            _presentationFrame = Time.frameCount;
            return;
        }
        _presentationFrame = -1;
        if (_aimAction == null || !_aimAction.TryGetRawAimPoint(out _) ||
            !_aimAction.TryGetFlatAimDirection(VisualOrigin.position, out var direction)) return;
        _presentationForward = direction;
        _presentationFrame = Time.frameCount;
    }

    protected override Vector3 ResolveForwardDirection()
    {
        if (GameplayActionInputBlocker.IsAimBlocked)
            return _presentationForward.sqrMagnitude > .0001f ? _presentationForward : base.ResolveForwardDirection();
        if (_presentationFrame == Time.frameCount) return _presentationForward;
        if (_aimAction != null &&
            _aimAction.TryGetFlatAimDirection(VisualOrigin.position, out Vector3 direction))
        {
            return direction;
        }

        return base.ResolveForwardDirection();
    }
}
