/*
 * Role hierarchy: Gameplay > Entities > Shooter > Enemy > StateMachine
 * Primary responsibility: Implements the focused EnemyShooterStateMachineUtility responsibility and collaborates with its role group.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using UnityEngine;
using VSplatter.StateMachine;

public static class EnemyShooterStateMachineUtility
{
    // Purpose: Resolves the combat controller needed by this component.
    public static EnemyShooterCombatController ResolveCombatController(StateMachine stateMachine)
    {
        if (stateMachine == null)
        {
            Debug.LogError("[EnemyShooterStateMachineUtility] StateMachine is null.");
            return null;
        }

        EnemyShooterCombatController combat =
            stateMachine.gameObject.GetComponent<EnemyShooterCombatController>() ??
            stateMachine.GetComponentInParent<EnemyShooterCombatController>() ??
            stateMachine.GetComponentInChildren<EnemyShooterCombatController>(true);

        if (combat != null)
            return combat;

        EnemyShooter shooter =
            stateMachine.gameObject.GetComponent<EnemyShooter>() ??
            stateMachine.GetComponentInParent<EnemyShooter>() ??
            stateMachine.GetComponentInChildren<EnemyShooter>(true);

        if (shooter != null)
        {
            combat =
                shooter.gameObject.GetComponent<EnemyShooterCombatController>() ??
                shooter.GetComponentInChildren<EnemyShooterCombatController>(true) ??
                shooter.GetComponentInParent<EnemyShooterCombatController>();

            if (combat != null)
                return combat;
        }

        Debug.LogError(
            $"[EnemyShooterStateMachineUtility] EnemyShooterCombatController not found. " +
            $"stateMachine={stateMachine.name}, root={stateMachine.transform.root.name}",
            stateMachine);

        return null;
    }
}
