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

    private HorizontalMoveActionSO _originSO => (HorizontalMoveActionSO)base.OriginSO;

    public override void Awake(StateMachine stateMachine)
    {
        _vsplatterScript = stateMachine.GetComponent<VSplatter_Character>();
        stateMachine.TryGetComponent(out _statsRuntime);
    }

    public override void OnUpdate()
    {
        float speed = _statsRuntime != null
            ? _statsRuntime.Movement.moveSpeed
            : _originSO.speed;

        _vsplatterScript.movementVector.x = _vsplatterScript.movementInput.x * speed;
        _vsplatterScript.movementVector.z = _vsplatterScript.movementInput.z * speed;
    }
}
