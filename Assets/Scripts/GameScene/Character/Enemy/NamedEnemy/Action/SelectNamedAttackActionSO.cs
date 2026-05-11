using System;
using UnityEngine;
using VSplatter.StateMachine;
using VSplatter.StateMachine.ScriptableObjects;

[Serializable]
public struct NamedAttackWeight
{
    public NamedEnemyAttackType attackType;
    [Min(1)] public int weight;
}

[CreateAssetMenu(
    fileName = "SelectNamedAttackAction",
    menuName = "State Machines/Named Enemy Actions/Select Attack")]
public class SelectNamedAttackActionSO : StateActionSO<SelectNamedAttackAction>
{
    [SerializeField] private NamedAttackWeight[] _attacks =
    {
        new NamedAttackWeight { attackType = NamedEnemyAttackType.Bite, weight = 1 },
        new NamedAttackWeight { attackType = NamedEnemyAttackType.Charge, weight = 1 },
        new NamedAttackWeight { attackType = NamedEnemyAttackType.Projectile, weight = 1 },
        new NamedAttackWeight { attackType = NamedEnemyAttackType.PoisonPuddle, weight = 1 }
    };

    [SerializeField] private bool _selectOnEnter = true;
    [SerializeField] private bool _setAttackUnfinished = true;

    public NamedAttackWeight[] Attacks => _attacks;
    public bool SelectOnEnter => _selectOnEnter;
    public bool SetAttackUnfinished => _setAttackUnfinished;
}

public class SelectNamedAttackAction : StateAction
{
    private SelectNamedAttackActionSO _config;
    private NamedEnemyBlackboard _blackboard;

    public override void Awake(StateMachine stateMachine)
    {
        _config = (SelectNamedAttackActionSO)OriginSO;
        _blackboard = stateMachine.GetComponentInParent<NamedEnemyBlackboard>();
    }

    public override void OnStateEnter()
    {
        if (_config.SelectOnEnter)
            TrySelect();
    }

    public override void OnUpdate()
    {
        if (!_config.SelectOnEnter)
            TrySelect();
    }

    private void TrySelect()
    {
        if (_blackboard == null || _blackboard.HasSelectedAttack)
            return;

        NamedEnemyAttackType selected = PickWeightedAttack();
        if (selected == NamedEnemyAttackType.None)
            return;

        _blackboard.selectedAttack = selected;

        if (_config.SetAttackUnfinished)
            _blackboard.attackFinished = false;
    }

    private NamedEnemyAttackType PickWeightedAttack()
    {
        NamedAttackWeight[] attacks = _config.Attacks;
        if (attacks == null || attacks.Length == 0)
            return NamedEnemyAttackType.None;

        int totalWeight = 0;

        for (int i = 0; i < attacks.Length; i++)
        {
            if (attacks[i].attackType == NamedEnemyAttackType.None)
                continue;

            totalWeight += Mathf.Max(1, attacks[i].weight);
        }

        if (totalWeight <= 0)
            return NamedEnemyAttackType.None;

        int roll = UnityEngine.Random.Range(0, totalWeight);

        for (int i = 0; i < attacks.Length; i++)
        {
            if (attacks[i].attackType == NamedEnemyAttackType.None)
                continue;

            roll -= Mathf.Max(1, attacks[i].weight);

            if (roll < 0)
                return attacks[i].attackType;
        }

        return NamedEnemyAttackType.None;
    }
}
