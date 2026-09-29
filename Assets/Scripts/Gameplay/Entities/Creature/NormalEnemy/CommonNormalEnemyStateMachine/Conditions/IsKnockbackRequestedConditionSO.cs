/*
 * Role hierarchy: Gameplay > Entities > Creature > NormalEnemy > CommonNormalEnemyStateMachine > Conditions
 * Primary responsibility: Defines IsKnockbackRequestedConditionSO state behavior or transitions that drive the runtime flow.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using UnityEngine;

[CreateAssetMenu(fileName = "IsKnockbackRequestedCondition", menuName = "State Machines/Common Conditions/Is Knockback Requested")]
public class IsKnockbackRequestedConditionSO : HasPendingKnockbackConditionSO
{
}
