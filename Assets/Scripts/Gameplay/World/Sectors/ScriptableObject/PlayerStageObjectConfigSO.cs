/*
 * Role hierarchy: Gameplay > World > Sectors > ScriptableObject
 * Primary responsibility: Defines the PlayerStageObjectConfigSO data asset used to configure runtime rules in the Inspector.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using UnityEngine;

[CreateAssetMenu(
    fileName = "PlayerStageObjectConfig",
    menuName = "Game/Sector/Player Stage Object Config")]
public class PlayerStageObjectConfigSO : StageObjectConfigSO
{
}
