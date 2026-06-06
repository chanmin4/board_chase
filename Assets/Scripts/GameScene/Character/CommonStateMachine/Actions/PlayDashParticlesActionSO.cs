using UnityEngine;
using VSplatter.StateMachine;
using VSplatter.StateMachine.ScriptableObjects;

[CreateAssetMenu(fileName = "PlayDashParticlesAction", menuName = "State Machines/Player Actions/Play Dash Particles")]
public class PlayDashParticlesActionSO : StateActionSO<PlayDashParticlesAction>
{
    [SerializeField] private bool _stopOnExit = true;
    public bool StopOnExit => _stopOnExit;
}

public class PlayDashParticlesAction : StateAction
{
    private PlayDashParticlesActionSO _config;
    private PlayerEffectController _effects;

    public override void Awake(StateMachine stateMachine)
    {
        _config = (PlayDashParticlesActionSO)OriginSO;
        _effects = ResolveEffects(stateMachine);
    }

    public override void OnStateEnter()
    {
        if (_effects != null)
            _effects.PlayDashParticles();
    }

    public override void OnStateExit()
    {
        if (_config.StopOnExit && _effects != null)
            _effects.StopDashParticles();
    }

    public override void OnUpdate() { }

    private static PlayerEffectController ResolveEffects(StateMachine stateMachine)
    {
        if (stateMachine == null)
            return null;

        if (stateMachine.TryGetComponent(out PlayerEffectController effects))
            return effects;

        effects = stateMachine.GetComponentInChildren<PlayerEffectController>(true);

        if (effects != null)
            return effects;

        return stateMachine.GetComponentInParent<PlayerEffectController>();
    }
}
