using UnityEngine;
using VSplatter.StateMachine;
using VSplatter.StateMachine.ScriptableObjects;

[CreateAssetMenu(
    fileName = "PlayChaserSelfDestructChargeParticlesAction",
    menuName = "State Machines/Enemy Actions/Particles/Chaser Play Charge")]
public class PlayChaserSelfDestructChargeParticlesActionSO
    : StateActionSO<PlayChaserSelfDestructChargeParticlesAction>
{
}

public class PlayChaserSelfDestructChargeParticlesAction : StateAction
{
    private ChaserVirusEffectController _effects;

    public override void Awake(StateMachine stateMachine)
    {
        _effects = ResolveEffects(stateMachine);

        if (_effects == null)
        {
            Debug.LogWarning(
                $"[PlayChaserSelfDestructChargeParticlesAction] ChaserVirusEffectController not found. Action skipped. owner={stateMachine.name}",
                stateMachine);
        }
    }

    public override void OnStateEnter()
    {
        if (_effects != null)
            _effects.PlaySelfDestructChargeParticles();
    }

    public override void OnUpdate()
    {
    }

    private static ChaserVirusEffectController ResolveEffects(StateMachine stateMachine)
    {
        if (stateMachine == null)
            return null;

        if (stateMachine.TryGetComponent(out ChaserVirusEffectController effects))
            return effects;

        effects = stateMachine.GetComponentInChildren<ChaserVirusEffectController>(true);

        if (effects != null)
            return effects;

        return stateMachine.GetComponentInParent<ChaserVirusEffectController>();
    }
}
