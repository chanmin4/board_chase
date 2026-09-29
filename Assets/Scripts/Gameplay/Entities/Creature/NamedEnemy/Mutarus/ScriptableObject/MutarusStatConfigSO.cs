/*
 * Role hierarchy: Gameplay > Entities > Creature > NamedEnemy > Mutarus > ScriptableObject
 * Primary responsibility: Defines the MutarusStatConfigSO data asset used to configure runtime rules in the Inspector.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using UnityEngine;

[CreateAssetMenu(
    fileName = "Mutarus_Stat_Config",
    menuName = "Game/Enemy/Definition_Config/NamedEnemy/Mutarus/Stat Config")]
public class MutarusStatConfigSO : NamedEnemyStatConfigSO
{
}