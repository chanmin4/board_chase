/*
 * Role hierarchy: Gameplay > Entities > Creature > NormalEnemy > Spitter > ScriptableObjects
 * Primary responsibility: Defines the SpitterStatConfigSO data asset used to configure runtime rules in the Inspector.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using UnityEngine;

[CreateAssetMenu(
    fileName = "Spitter_Stat_Config",
    menuName = "Game/Enemy/Definition_Config/NormalEnemy/Spitter/Stat Config")]
public class SpitterStatConfigSO : CreatureEnemyStatConfigSO
{
}
