/*
 * Role hierarchy: Gameplay > Entities > Creature > CommonEnemyStateMachine > AttackExecutor
 * Primary responsibility: Handles EnemyAttackExecutorFactory combat resolution, damage, projectile, or status changes.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using UnityEngine;

public static class EnemyAttackExecutorFactory
{
    // Purpose: Attempts to create and reports whether the request succeeded.
    public static bool TryCreate(
        EnemyAttackConfigSO config,
        EnemyAttackExecutorContext context,
        out EnemyAttackExecutor runtime)
    {
        runtime = null;

        switch (config)
        {
            case EnemyFireProjectileConfigSO projectileConfig:
                runtime = new EnemyProjectileAttackExecutor(projectileConfig, context);
                return true;

            case EnemyArcBombAttackConfigSO arcBombConfig:
                runtime = new EnemyArcBombAttackExecutor(arcBombConfig, context);
                return true;

            case MutarusChargeAttackConfigSO chargeConfig:
                runtime = new EnemyChargeAttackExecutor(chargeConfig, context);
                return true;

            case ChaserSelfDestructConfigSO selfDestructConfig:
                runtime = new EnemySelfDestructAttackExecutor(selfDestructConfig, context);
                return true;
        }

        if (config != null)
        {
            Debug.LogWarning(
                $"[EnemyAttackRuntimeFactory] No runtime for attack config '{config.name}' ({config.AttackBehaviorType}).",
                config);
        }

        return false;
    }
}
