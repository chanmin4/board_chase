using UnityEngine;
using VSplatter.StateMachine;
using VSplatter.StateMachine.ScriptableObjects;

[CreateAssetMenu(
    fileName = "FinishNamedNormalAttackAction",
    menuName = "State Machines/Named Enemy Actions/Finish Normal Attack")]
public class FinishNamedNormalAttackActionSO : StateActionSO<FinishNamedNormalAttackAction>
{
    [SerializeField] private NamedNormalAttackConfigSO _config;
    [SerializeField] private StateAction.SpecificMoment _moment = StateAction.SpecificMoment.OnStateEnter;

    [Header("Delay")]
    [SerializeField] private bool _useDelay = true;
    [SerializeField, Min(0f)] private float _delaySeconds = 2f;

    public NamedNormalAttackConfigSO Config => _config;
    public StateAction.SpecificMoment Moment => _moment;
    public bool UseDelay => _useDelay;
    public float DelaySeconds => _delaySeconds;
}

public class FinishNamedNormalAttackAction : StateAction
{
    private FinishNamedNormalAttackActionSO _origin;
    private NamedEnemyBlackboard _blackboard;
    private float _timer;
    private bool _applied;

    public override void Awake(StateMachine stateMachine)
    {
        _origin = (FinishNamedNormalAttackActionSO)OriginSO;
        _blackboard = stateMachine.GetComponentInParent<NamedEnemyBlackboard>();
    }

    public override void OnStateEnter()
    {
        _timer = 0f;
        _applied = false;

        if (_origin.Moment == SpecificMoment.OnStateEnter && !_origin.UseDelay)
            Apply();
    }

    public override void OnUpdate()
    {
        if (_applied)
            return;

        if (_origin.Moment != SpecificMoment.OnUpdate &&
            _origin.Moment != SpecificMoment.OnStateEnter)
            return;

        if (!_origin.UseDelay)
            return;

        _timer += Time.deltaTime;

        if (_timer >= _origin.DelaySeconds)
            Apply();
    }

    public override void OnStateExit()
    {
        if (_origin.Moment == SpecificMoment.OnStateExit)
            Apply();
    }

    private void Apply()
    {
        if (_blackboard == null)
            return;

        float cooldown = _origin.Config != null
            ? _origin.Config.NormalAttackCooldown
            : 0f;

        _blackboard.FinishNormalAttack(cooldown);
        _applied = true;
    }
}
