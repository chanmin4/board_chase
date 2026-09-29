/*
 * Role hierarchy: Gameplay > Entities > Creature > CommonEnemyStateMachine > AttackExecutor
 * Primary responsibility: Handles EnemyAttackExecutorContext combat resolution, damage, projectile, or status changes.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using UnityEngine;
using UnityEngine.AI;
using VSplatter.StateMachine;

public readonly struct EnemyAttackExecutorContext
{
    public EnemyAttackExecutorContext(
        StateMachine stateMachine,
        Enemy enemy,
        EnemyAttackExecutorController runtimeController)
    {
        StateMachine = stateMachine;
        Enemy = enemy;
        RuntimeController = runtimeController;

        OwnerGameObject = stateMachine != null ? stateMachine.gameObject : null;
        OwnerTransform = enemy != null ? enemy.transform : stateMachine != null ? stateMachine.transform : null;

        Agent = ResolveComponent<NavMeshAgent>(stateMachine, enemy);
        Rig = ResolveComponent<EnemyAttackRig>(stateMachine, enemy);
        Animator = ResolveAnimator(stateMachine, enemy);
        SelfDamageable = ResolveComponent<Damageable>(stateMachine, enemy);
    }

    public StateMachine StateMachine { get; }
    public Enemy Enemy { get; }
    public EnemyAttackExecutorController RuntimeController { get; }
    public GameObject OwnerGameObject { get; }
    public Transform OwnerTransform { get; }
    public NavMeshAgent Agent { get; }
    public EnemyAttackRig Rig { get; }
    public Animator Animator { get; }
    public Damageable SelfDamageable { get; }

    public Damageable Target =>
        Enemy != null ? Enemy.currentTarget : null;

    private static T ResolveComponent<T>(StateMachine stateMachine, Enemy enemy)
        where T : Component
    {
        if (stateMachine != null && stateMachine.TryGetComponent(out T component))
            return component;

        return enemy != null
            ? enemy.GetComponentInChildren<T>(true)
            : null;
    }

    // Purpose: Resolves the animator needed by this component.
    private static Animator ResolveAnimator(StateMachine stateMachine, Enemy enemy)
    {
        if (stateMachine == null)
            return null;

        if (stateMachine.TryGetComponent(out Animator animator))
            return animator;

        animator = stateMachine.GetComponentInChildren<Animator>(true);

        if (animator != null)
            return animator;

        return enemy != null
            ? enemy.GetComponentInChildren<Animator>(true)
            : stateMachine.GetComponentInParent<Animator>();
    }
}

public abstract class EnemyAttackExecutor
{
    protected EnemyAttackExecutor(EnemyAttackExecutorContext context)
    {
        Context = context;
    }

    protected EnemyAttackExecutorContext Context { get; }
    public bool IsFinished { get; protected set; }

    // Purpose: Enters the enter state or area.
    public virtual void Enter()
    {
    }

    // Purpose: Processes one update step for tick.
    public abstract void Tick(float deltaTime);

    // Purpose: Exits the active exit state or area.
    public virtual void Exit()
    {
    }

    // Purpose: Finishes the active finish workflow.
    protected void Finish()
    {
        IsFinished = true;
    }
}
