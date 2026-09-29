/*
 * Role hierarchy: Gameplay > Entities > Creature > NormalEnemy > CommonNormalEnemyStateMachine > Conditions
 * Primary responsibility: Defines MoveTimeoutConditionSO state behavior or transitions that drive the runtime flow.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using UnityEngine;

[CreateAssetMenu(fileName = "MoveTimeoutCondition", menuName = "State Machines/Enemy Conditions/Move Timeout")]
public class MoveTimeoutConditionSO : TimeElapsedConditionSO
{
}
