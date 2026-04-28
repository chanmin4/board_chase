using UnityEngine;
using VSplatter.StateMachine;
using VSplatter.StateMachine.ScriptableObjects;

[CreateAssetMenu(
    fileName = "SetEnemyContactDamageEnabledAction",
    menuName = "State Machines/Enemy Actions/Set Enemy Contact Damage Enabled")]
public class SetEnemyContactDamageEnabledActionSO : StateActionSO
{
    [SerializeField] private StateAction.SpecificMoment _moment = StateAction.SpecificMoment.OnStateEnter;
    [SerializeField] private bool _enabledValue = true;

    public StateAction.SpecificMoment Moment => _moment;
    public bool EnabledValue => _enabledValue;

    protected override StateAction CreateAction() => new SetEnemyContactDamageEnabledAction();
}

public class SetEnemyContactDamageEnabledAction : StateAction
{
    private EnemyContactDamageManager _contactDamage;
    private new SetEnemyContactDamageEnabledActionSO OriginSO => (SetEnemyContactDamageEnabledActionSO)base.OriginSO;

    public override void Awake(StateMachine stateMachine)
    {
        _contactDamage = stateMachine.GetComponent<EnemyContactDamageManager>();
    }

    public override void OnUpdate()
    {
        if (OriginSO.Moment == SpecificMoment.OnUpdate)
            Apply();
    }

    public override void OnStateEnter()
    {
        if (OriginSO.Moment == SpecificMoment.OnStateEnter)
            Apply();
    }

    public override void OnStateExit()
    {
        if (OriginSO.Moment == SpecificMoment.OnStateExit)
            Apply();
    }

    private void Apply()
    {
        if (_contactDamage != null)
            _contactDamage.enabled = OriginSO.EnabledValue;
    }
}
