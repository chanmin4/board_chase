using UnityEngine;
using VSplatter.StateMachine;
using VSplatter.StateMachine.ScriptableObjects;

public enum NamedBlackboardBoolField
{
    SpawnInitialized,
    IntroFinished,
    CanEnterPattern,
    ShouldStopChase,
    AttackFinished
}

[CreateAssetMenu(
    fileName = "SetNamedBlackboardBoolAction",
    menuName = "State Machines/Named Enemy Actions/Set Blackboard Bool")]
public class SetNamedBlackboardBoolActionSO : StateActionSO<SetNamedBlackboardBoolAction>
{
    [SerializeField] private NamedBlackboardBoolField _field;
    [SerializeField] private bool _value = true;
    [SerializeField] private StateAction.SpecificMoment _moment = StateAction.SpecificMoment.OnStateEnter;

    [Header("Delay")]
    [SerializeField] private bool _useDelay;
    [SerializeField, Min(0f)] private float _delaySeconds;

    [Header("Optional")]
    [SerializeField] private bool _clearSelectedAttack;

    public NamedBlackboardBoolField Field => _field;
    public bool Value => _value;
    public StateAction.SpecificMoment Moment => _moment;
    public bool UseDelay => _useDelay;
    public float DelaySeconds => _delaySeconds;
    public bool ClearSelectedAttack => _clearSelectedAttack;
}

public class SetNamedBlackboardBoolAction : StateAction
{
    private SetNamedBlackboardBoolActionSO _config;
    private NamedEnemyBlackboard _blackboard;
    private float _timer;
    private bool _applied;

    public override void Awake(StateMachine stateMachine)
    {
        _config = (SetNamedBlackboardBoolActionSO)OriginSO;
        _blackboard = stateMachine.GetComponentInParent<NamedEnemyBlackboard>();
    }

    public override void OnStateEnter()
    {
        _timer = 0f;
        _applied = false;

        if (_config.Moment == SpecificMoment.OnStateEnter && !_config.UseDelay)
            Apply();
    }

    public override void OnUpdate()
    {
        if (_config.Moment != SpecificMoment.OnUpdate &&
            _config.Moment != SpecificMoment.OnStateEnter)
            return;

        if (!_config.UseDelay || _applied)
            return;

        _timer += Time.deltaTime;

        if (_timer >= _config.DelaySeconds)
            Apply();
    }

    public override void OnStateExit()
    {
        if (_config.Moment == SpecificMoment.OnStateExit)
            Apply();
    }

    private void Apply()
    {
        if (_blackboard == null)
            return;

        switch (_config.Field)
        {
            case NamedBlackboardBoolField.SpawnInitialized:
                _blackboard.spawnInitialized = _config.Value;
                break;
            case NamedBlackboardBoolField.IntroFinished:
                _blackboard.introFinished = _config.Value;
                break;
            case NamedBlackboardBoolField.CanEnterPattern:
                _blackboard.canEnterPattern = _config.Value;
                break;
            case NamedBlackboardBoolField.ShouldStopChase:
                _blackboard.shouldStopChase = _config.Value;
                break;
            case NamedBlackboardBoolField.AttackFinished:
                _blackboard.attackFinished = _config.Value;
                break;
        }

        if (_config.ClearSelectedAttack)
            _blackboard.ClearSelectedAttack();

        _applied = true;
    }
}
