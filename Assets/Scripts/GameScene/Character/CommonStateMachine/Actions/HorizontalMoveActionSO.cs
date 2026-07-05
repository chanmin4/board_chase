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
    private VSplatter_Character _vsplatterScript;
    private PlayerStatsRuntime _statsRuntime;
    private PlayerAimAction _aimAction;

    private HorizontalMoveActionSO _originSO => (HorizontalMoveActionSO)base.OriginSO;

    public override void Awake(StateMachine stateMachine)
    {
        if (stateMachine == null)
            return;

        if (!stateMachine.TryGetComponent(out _vsplatterScript))
            _vsplatterScript = stateMachine.GetComponentInChildren<VSplatter_Character>(true);

        if (!stateMachine.TryGetComponent(out _statsRuntime))
            _statsRuntime = stateMachine.GetComponentInChildren<PlayerStatsRuntime>(true);

        if (!stateMachine.TryGetComponent(out _aimAction))
            _aimAction = stateMachine.GetComponentInChildren<PlayerAimAction>(true);
    }

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
