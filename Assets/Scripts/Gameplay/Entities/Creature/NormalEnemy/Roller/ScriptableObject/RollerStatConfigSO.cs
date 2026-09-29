/*
 * Role hierarchy: Gameplay > Entities > Creature > NormalEnemy > Roller > ScriptableObject
 * Primary responsibility: Defines the RollerStatConfigSO data asset used to configure runtime rules in the Inspector.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using UnityEngine;

[CreateAssetMenu(
    fileName = "Roller_Stat_Config",
    menuName = "Game/Enemy/Definition_Config/NormalEnemy/Roller/Stat Config")]
public class RollerStatConfigSO : CreatureEnemyStatConfigSO
{
    
}
