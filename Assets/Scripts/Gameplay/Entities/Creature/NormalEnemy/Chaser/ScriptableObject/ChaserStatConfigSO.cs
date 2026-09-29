/*
 * Role hierarchy: Gameplay > Entities > Creature > NormalEnemy > Chaser > ScriptableObject
 * Primary responsibility: Defines the ChaserStatConfigSO data asset used to configure runtime rules in the Inspector.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using UnityEngine;

[CreateAssetMenu(
    fileName = "Chaser_Stat_Config",
    menuName = "Game/Enemy/Definition_Config/NormalEnemy/Chaser/Stat Config")]
public class ChaserStatConfigSO : CreatureEnemyStatConfigSO

{
}