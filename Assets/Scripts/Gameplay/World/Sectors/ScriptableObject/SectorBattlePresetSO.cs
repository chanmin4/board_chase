/*
 * Role hierarchy: Gameplay > World > Sectors > ScriptableObject
 * Primary responsibility: Defines the SectorBattlePresetSO data asset used to configure runtime rules in the Inspector.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

/// <summary>
/// Common authored-sector preset base for combat rooms.
/// Use a concrete Normal or Boss preset so the room role cannot be ambiguous.
/// </summary>
public abstract class SectorBattlePresetSO : SectorPresetSO
{
}
