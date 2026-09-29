/*
 * Role hierarchy: Gameplay > World > Sectors > ScriptableObject
 * Primary responsibility: Defines the SectorBossBattlePresetSO data asset used to configure runtime rules in the Inspector.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using UnityEngine;

[CreateAssetMenu(
    fileName = "SectorBossBattlePreset",
    menuName = "Game/Battle/Sector Boss Battle Preset")]
public sealed class SectorBossBattlePresetSO : SectorBattlePresetSO
{
    public override StageRoomType RoomType => StageRoomType.Boss;

#if UNITY_EDITOR
    // Purpose: Normalizes Inspector-authored data after it changes in the Editor.
    private void OnValidate()
    {
        SetSerializedRoomType(StageRoomType.Boss);
    }
#endif
}
