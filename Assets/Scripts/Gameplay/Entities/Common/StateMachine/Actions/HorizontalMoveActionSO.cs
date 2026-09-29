/*
 * Role hierarchy: Gameplay > Entities > Common > StateMachine > Actions
 * Primary responsibility: Defines HorizontalMoveActionSO state behavior or transitions that drive the runtime flow.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using UnityEngine;
using VSplatter.StateMachine;
using VSplatter.StateMachine.ScriptableObjects;

[CreateAssetMenu(fileName = "HorizontalMove", menuName = "State Machines/Actions/Horizontal Move")]
public class HorizontalMoveActionSO : StateActionSO<HorizontalMoveAction>
{
    [Tooltip("Horizontal XZ plane speed multiplier")]
    public float speed = 8f;
}

public class HorizontalMoveAction : StateAction
{
    private PlayerCharacter _vsplatterScript;
    private PlayerStatsRuntime _statsRuntime;
    private PlayerAimController _aimAction;

    private HorizontalMoveActionSO _originSO => (HorizontalMoveActionSO)base.OriginSO;

    // Purpose: Caches the component references required before runtime initialization.
    public override void Awake(StateMachine stateMachine)
    {
        if (stateMachine == null)
            return;

        if (!stateMachine.TryGetComponent(out _vsplatterScript))
            _vsplatterScript = stateMachine.GetComponentInChildren<PlayerCharacter>(true);

        if (!stateMachine.TryGetComponent(out _statsRuntime))
            _statsRuntime = stateMachine.GetComponentInChildren<PlayerStatsRuntime>(true);

        if (!stateMachine.TryGetComponent(out _aimAction))
            _aimAction = stateMachine.GetComponentInChildren<PlayerAimController>(true);
    }

    // Purpose: Responds to the update callback.
    public override void OnUpdate()
    {
        if (GamePause.IsPaused || Time.timeScale <= 0f)
            return;

        if (_vsplatterScript == null)
            return;

        float speed = _statsRuntime != null
            ? _statsRuntime.Movement.moveSpeed
            : _originSO.speed;

        if (_aimAction != null)
            speed *= _aimAction.MoveSpeedMultiplier;

        _vsplatterScript.movementVector.x = _vsplatterScript.movementInput.x * speed;
        _vsplatterScript.movementVector.z = _vsplatterScript.movementInput.z * speed;
    }
}
