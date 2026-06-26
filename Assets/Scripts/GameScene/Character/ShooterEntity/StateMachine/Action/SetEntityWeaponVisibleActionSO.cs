using UnityEngine;
using VSplatter.StateMachine;
using VSplatter.StateMachine.ScriptableObjects;
using Moment = VSplatter.StateMachine.StateAction.SpecificMoment;

[CreateAssetMenu(
    fileName = "SetEntityWeaponVisibleAction",
    menuName = "State Machines/Actions/Shooter/Set Entity Weapon Visible")]
public class SetEntityWeaponVisibleActionSO : StateActionSO<SetEntityWeaponVisibleAction>
{
    [Tooltip("해당 state moment에 현재 EntityWeaponHolder의 무기 view를 보이게 할지 여부입니다.")]
    [SerializeField] private bool _visible = true;

    [Tooltip("무기 표시 상태를 적용할 state action 실행 시점입니다.")]
    [SerializeField] private Moment _whenToRun = Moment.OnStateEnter;

    public bool Visible => _visible;
    public Moment WhenToRun => _whenToRun;
}

public class SetEntityWeaponVisibleAction : StateAction
{
    private EntityWeaponHolder _weaponHolder;
    private SetEntityWeaponVisibleActionSO _originSO => (SetEntityWeaponVisibleActionSO)OriginSO;

    public override void Awake(StateMachine stateMachine)
    {
        if (stateMachine == null)
            return;

        if (!stateMachine.TryGetComponent(out _weaponHolder))
        {
            _weaponHolder =
                stateMachine.GetComponentInChildren<EntityWeaponHolder>(true) ??
                stateMachine.GetComponentInParent<EntityWeaponHolder>();
        }
    }

    public override void OnStateEnter()
    {
        if (_originSO.WhenToRun == Moment.OnStateEnter)
            Apply();
    }

    public override void OnStateExit()
    {
        if (_originSO.WhenToRun == Moment.OnStateExit)
            Apply();
    }

    public override void OnUpdate()
    {
        if (_originSO.WhenToRun == Moment.OnUpdate)
            Apply();
    }

    private void Apply()
    {
        _weaponHolder?.SetVisible(_originSO.Visible);
    }
}