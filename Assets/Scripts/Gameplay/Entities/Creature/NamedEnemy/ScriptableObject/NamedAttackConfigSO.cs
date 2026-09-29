/*
 * Role hierarchy: Gameplay > Entities > Creature > NamedEnemy > ScriptableObject
 * Primary responsibility: Defines the NamedAttackConfigSO data asset used to configure runtime rules in the Inspector.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

/// <summary>
/// Base asset for named enemy attacks.
/// The asset reference itself is used as the selected attack identity.
/// </summary>
public abstract class NamedAttackConfigSO : EnemyAttackConfigSO
{
}