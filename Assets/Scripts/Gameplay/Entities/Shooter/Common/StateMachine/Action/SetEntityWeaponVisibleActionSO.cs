/*
 * Role hierarchy: Gameplay > Entities > Shooter > Common > StateMachine > Action
 * Primary responsibility: Defines SetEntityWeaponVisibleActionSO state behavior or transitions that drive the runtime flow.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using UnityEngine;
using VSplatter.StateMachine;
using VSplatter.StateMachine.ScriptableObjects;
using Moment = VSplatter.StateMachine.StateAction.SpecificMoment;

[CreateAssetMenu(
    fileName = "SetEntityWeaponVisibleAction",
    menuName = "State Machines/Actions/Shooter/Set Entity Weapon Visible")]
public class SetEntityWeaponVisibleActionSO : StateActionSO<SetEntityWeaponVisibleAction>
{
    [Tooltip("Controls current ShooterEquipmentController weapon view visibility at the selected state moment.")]
    [SerializeField] private bool _visible = true;

    [Tooltip("무기 표시 상태를 적용할 state action 실행 시점입니다.")]
    [SerializeField] private Moment _whenToRun = Moment.OnStateEnter;

    public bool Visible => _visible;
    public Moment WhenToRun => _whenToRun;
}

public class SetEntityWeaponVisibleAction : StateAction
{
    private ShooterEquipmentController _equipmentController;
    private SetEntityWeaponVisibleActionSO _originSO => (SetEntityWeaponVisibleActionSO)OriginSO;

    // Purpose: Caches the component references required before runtime initialization.
    public override void Awake(StateMachine stateMachine)
    {
        if (stateMachine == null)
            return;

        if (!stateMachine.TryGetComponent(out _equipmentController))
            _equipmentController = EntityComponentResolver.Resolve<ShooterEquipmentController>(stateMachine);
    }

    // Purpose: Responds to the state enter callback.
    public override void OnStateEnter()
    {
        if (_originSO.WhenToRun == Moment.OnStateEnter)
            Apply();
    }

    // Purpose: Responds to the state exit callback.
    public override void OnStateExit()
    {
        if (_originSO.WhenToRun == Moment.OnStateExit)
            Apply();
    }

    // Purpose: Responds to the update callback.
    public override void OnUpdate()
    {
        if (_originSO.WhenToRun == Moment.OnUpdate)
            Apply();
    }

    // Purpose: Applies the requested apply change.
    private void Apply()
    {
        _equipmentController?.SetVisible(_originSO.Visible);
    }
}
