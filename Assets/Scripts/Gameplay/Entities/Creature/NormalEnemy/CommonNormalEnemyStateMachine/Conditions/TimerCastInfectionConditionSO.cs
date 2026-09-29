/*
 * Role hierarchy: Gameplay > Entities > Creature > NormalEnemy > CommonNormalEnemyStateMachine > Conditions
 * Primary responsibility: Defines TimerCastInfectionConditionSO state behavior or transitions that drive the runtime flow.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using UnityEngine;

[CreateAssetMenu(fileName = "TimerCastInfectionCondition", menuName = "State Machines/Enemy Conditions/Timer Cast Infection")]
public class TimerCastInfectionConditionSO : TimeElapsedConditionSO
{
}
