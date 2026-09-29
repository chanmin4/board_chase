/*
 * Role hierarchy: Gameplay > Entities > Shooter > Enemy > Common
 * Primary responsibility: Handles EnemyShooterVisionController camera, vision, range, or aiming calculations and presentation.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using UnityEngine;

[DisallowMultipleComponent]
public sealed class EnemyShooterVisionController : EntityVisionController
{
    [Header("Enemy Aim")]
    [SerializeField] private ShooterEquipmentController _equipmentController;
    [SerializeField] private Transform _aimForwardRoot;

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

        if (_equipmentController == null)
            _equipmentController = EntityComponentResolver.Resolve<ShooterEquipmentController>(this);

        if (_aimForwardRoot == null && _equipmentController != null)
            _aimForwardRoot = _equipmentController.transform;
    }

    // Purpose: Resolves the aim01 needed by this component.
    protected override float ResolveAim01()
    {
        // Enemy vision is authored by its own base cone and must not inherit the
        // player's full-aim cone shrink.
        return 0f;
    }

    // Purpose: Resolves the forward direction needed by this component.
    protected override Vector3 ResolveForwardDirection()
    {
        if (_equipmentController != null)
        {
            Vector3 direction = _equipmentController.FireDirection;
            direction.y = 0f;

            if (direction.sqrMagnitude > 0.0001f)
                return direction.normalized;
        }

        if (_aimForwardRoot != null)
        {
            Vector3 direction = _aimForwardRoot.forward;
            direction.y = 0f;

            if (direction.sqrMagnitude > 0.0001f)
                return direction.normalized;
        }

        return base.ResolveForwardDirection();
    }
}
