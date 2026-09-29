/*
 * Role hierarchy: Gameplay > Entities > Creature > NormalEnemy > CommonNormalEnemyStateMachine > Conditions
 * Primary responsibility: Defines TimerKnockbackConditionSO state behavior or transitions that drive the runtime flow.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using UnityEngine;

[CreateAssetMenu(fileName = "TimerKnockbackCondition", menuName = "State Machines/Enemy Conditions/Timer Knockback")]
public class TimerKnockbackConditionSO : TimeElapsedConditionSO
{
}
