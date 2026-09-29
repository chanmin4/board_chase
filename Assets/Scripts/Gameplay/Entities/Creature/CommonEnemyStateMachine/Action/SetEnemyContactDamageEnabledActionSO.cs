/*
 * Role hierarchy: Gameplay > Entities > Creature > CommonEnemyStateMachine > Action
 * Primary responsibility: Defines SetEnemyContactDamageEnabledActionSO state behavior or transitions that drive the runtime flow.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

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

    // Purpose: Creates the action owned by this workflow.
    protected override StateAction CreateAction() => new SetEnemyContactDamageEnabledAction();
}

public class SetEnemyContactDamageEnabledAction : StateAction
{
    private EnemyContactDamage _contactDamage;
    private new SetEnemyContactDamageEnabledActionSO OriginSO => (SetEnemyContactDamageEnabledActionSO)base.OriginSO;

    // Purpose: Caches the component references required before runtime initialization.
    public override void Awake(StateMachine stateMachine)
    {
        _contactDamage = ResolveInOwnerHierarchy<EnemyContactDamage>(stateMachine);
    }

    // Purpose: Responds to the update callback.
    public override void OnUpdate()
    {
        if (OriginSO.Moment == SpecificMoment.OnUpdate)
            Apply();
    }

    // Purpose: Responds to the state enter callback.
    public override void OnStateEnter()
    {
        if (OriginSO.Moment == SpecificMoment.OnStateEnter)
            Apply();
    }

    // Purpose: Responds to the state exit callback.
    public override void OnStateExit()
    {
        if (OriginSO.Moment == SpecificMoment.OnStateExit)
            Apply();
    }

    // Purpose: Applies the requested apply change.
    private void Apply()
    {
        if (_contactDamage != null)
            _contactDamage.enabled = OriginSO.EnabledValue;
    }

    private static T ResolveInOwnerHierarchy<T>(StateMachine stateMachine) where T : Component
    {
        if (stateMachine.TryGetComponent(out T component))
            return component;

        component = stateMachine.GetComponentInChildren<T>(true);
        if (component != null)
            return component;

        Transform current = stateMachine.transform.parent;

        while (current != null)
        {
            component = current.GetComponentInChildren<T>(true);
            if (component != null)
                return component;

            current = current.parent;
        }

        return null;
    }
}
