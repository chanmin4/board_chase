/*
 * Role hierarchy: Gameplay > Entities > Creature > NamedEnemy > Mutarus > ScriptableObject
 * Primary responsibility: Defines the MutarusPoisonPuddleAttackConfigSO data asset used to configure runtime rules in the Inspector.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using UnityEngine;

[CreateAssetMenu(
    fileName = "Mutarus_PoisonPuddleAttack_Config",
    menuName = "ScriptableObject/GameScene/Enemy/Definition_Config/NamedEnemy/Mutarus/Poison Puddle Attack Config")]
public class MutarusPoisonPuddleAttackConfigSO : EnemyArcBombAttackConfigSO
{
}