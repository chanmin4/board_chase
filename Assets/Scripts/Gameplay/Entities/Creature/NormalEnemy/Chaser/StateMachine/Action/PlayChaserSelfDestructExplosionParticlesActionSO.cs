/*
 * Role hierarchy: Gameplay > Entities > Creature > NormalEnemy > Chaser > StateMachine > Action
 * Primary responsibility: Defines PlayChaserSelfDestructExplosionParticlesActionSO state behavior or transitions that drive the runtime flow.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using UnityEngine;
using VSplatter.StateMachine;
using VSplatter.StateMachine.ScriptableObjects;

[CreateAssetMenu(
    fileName = "PlayChaserSelfDestructExplosionParticlesAction",
    menuName = "State Machines/Enemy Actions/Particles/Chaser Play Explosion")]
public class PlayChaserSelfDestructExplosionParticlesActionSO
    : StateActionSO<PlayChaserSelfDestructExplosionParticlesAction>
{
}

public class PlayChaserSelfDestructExplosionParticlesAction : StateAction
{
    private ChaserVirusEffectController _effects;

    // Purpose: Caches the component references required before runtime initialization.
    public override void Awake(StateMachine stateMachine)
    {
        _effects = ResolveEffects(stateMachine);

        if (_effects == null)
        {
            Debug.LogWarning(
                $"[PlayChaserSelfDestructExplosionParticlesAction] ChaserVirusEffectController not found. Action skipped. owner={stateMachine.name}",
                stateMachine);
        }
    }

    // Purpose: Responds to the state enter callback.
    public override void OnStateEnter()
    {
        if (_effects != null)
            _effects.PlaySelfDestructExplosionParticles();
    }

    // Purpose: Responds to the update callback.
    public override void OnUpdate()
    {
    }

    // Purpose: Resolves the effects needed by this component.
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
