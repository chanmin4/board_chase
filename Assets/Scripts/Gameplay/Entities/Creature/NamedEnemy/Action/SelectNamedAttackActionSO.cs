/*
 * Role hierarchy: Gameplay > Entities > Creature > NamedEnemy > Action
 * Primary responsibility: Defines SelectNamedAttackActionSO state behavior or transitions that drive the runtime flow.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using UnityEngine;
using VSplatter.StateMachine;
using VSplatter.StateMachine.ScriptableObjects;

[CreateAssetMenu(
    fileName = "SelectNamedAttackAction",
    menuName = "State Machines/Named Enemy Actions/Select Attack")]
public class SelectNamedAttackActionSO : StateActionSO<SelectNamedAttackAction>
{
    [SerializeField] private NamedNormalAttackConfigSO _config;

    [Tooltip("If true, does not replace an already selected attack.")]
    [SerializeField] private bool _keepExistingSelection = true;

    public NamedNormalAttackConfigSO Config => _config;
    public bool KeepExistingSelection => _keepExistingSelection;
}

public class SelectNamedAttackAction : StateAction
{
    private SelectNamedAttackActionSO _origin;
    private NamedEnemyBlackboard _blackboard;
    private Enemy _enemy;
    private Transform _owner;

    // Purpose: Caches the component references required before runtime initialization.
    public override void Awake(StateMachine stateMachine)
    {
        _origin = (SelectNamedAttackActionSO)OriginSO;
        _blackboard = stateMachine.GetComponentInParent<NamedEnemyBlackboard>();
        _enemy = stateMachine.GetComponentInParent<Enemy>();
        _owner = _enemy != null ? _enemy.transform : stateMachine.transform.root;
    }

    // Purpose: Responds to the state enter callback.
    public override void OnStateEnter()
    {
        TrySelect();
    }

    // Purpose: Responds to the update callback.
    public override void OnUpdate()
    {
        TrySelect();
    }

    // Purpose: Attempts to select and reports whether the request succeeded.
    private void TrySelect()
    {
        if (_origin.Config == null || _blackboard == null || _enemy == null)
            return;

        if (_origin.KeepExistingSelection && _blackboard.HasSelectedAttack)
            return;

        if (_enemy.currentTarget == null)
            return;

        float distance = ResolveTargetDistance();

        if (!_origin.Config.TryPickAttack(distance, out EnemyAttackConfigSO attackConfig))
            return;

        _blackboard.SelectAttack(attackConfig);
    }

    // Purpose: Resolves the target distance needed by this component.
    private float ResolveTargetDistance()
    {
        Vector3 from = _owner.position;
        Vector3 to = _enemy.currentTarget.transform.position;

        from.y = 0f;
        to.y = 0f;

        return Vector3.Distance(from, to);
    }
}
