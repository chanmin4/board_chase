/*
 * Role hierarchy: Gameplay > Entities > Shooter > Player
 * Primary responsibility: Coordinates the PlayerShooterDashController subsystem lifecycle and collaborating objects.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using UnityEngine;

[DisallowMultipleComponent]
public class PlayerShooterDashController : ShooterDashController
{
    [Header("Player Refs")]
    [SerializeField] private PlayerCharacter _character;

    private bool _suppressUntilInputRelease;

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

    // Purpose: Releases active subscriptions and transient state while disabled.
    protected override void OnDisable()
    {
        base.OnDisable();
        _suppressUntilInputRelease = false;
    }

    // Purpose: Processes this component's per-frame runtime work.
    protected override void Update()
    {
        base.Update();
        TickPlayerDashInput();
    }

    // Purpose: Attempts to start dash and reports whether the request succeeded.
    public bool TryStartDash()
    {
        if (_character == null)
            return false;

        return TryStartDash(_character.movementInput);
    }

    // Purpose: Resolves the refs needed by this component.
    protected override void ResolveRefs()
    {
        base.ResolveRefs();

        if (_character == null)
            _character = EntityComponentResolver.Resolve<PlayerCharacter>(this);
    }

    // Purpose: Processes one update step for player dash input.
    private void TickPlayerDashInput()
    {
        if (_character == null)
            return;

        if (GameplayActionInputBlocker.IsMovementBlocked)
        {
            _character.DashInput = false;
            _suppressUntilInputRelease = false;
            return;
        }

        if (_suppressUntilInputRelease)
        {
            if (!_character.DashInput)
                _suppressUntilInputRelease = false;

            return;
        }

        if (IsDashing || !_character.DashInput)
            return;

        if (TryStartDash())
        {
            _character.DashInput = false;
            _suppressUntilInputRelease = true;
        }
    }
}
