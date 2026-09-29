/*
 * Role hierarchy: Gameplay > Entities > Creature > NormalEnemy > Worm > ScriptableObject
 * Primary responsibility: Defines the WormStatConfigSO data asset used to configure runtime rules in the Inspector.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using UnityEngine;

[CreateAssetMenu(
    fileName = "Worm_Stat_Config",
    menuName = "Game/Enemy/Definition_Config/NormalEnemy/Worm/Stat Config")]
public class WormStatConfigSO : CreatureEnemyStatConfigSO
{
}
