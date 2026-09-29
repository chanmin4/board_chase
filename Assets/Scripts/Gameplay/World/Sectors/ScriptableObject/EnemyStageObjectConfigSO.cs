/*
 * Role hierarchy: Gameplay > World > Sectors > ScriptableObject
 * Primary responsibility: Defines the EnemyStageObjectConfigSO data asset used to configure runtime rules in the Inspector.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using UnityEngine;

[CreateAssetMenu(
    fileName = "EnemyStageObjectConfig",
    menuName = "Game/Sector/Enemy Stage Object Config")]
public class EnemyStageObjectConfigSO : StageObjectConfigSO
{
}
