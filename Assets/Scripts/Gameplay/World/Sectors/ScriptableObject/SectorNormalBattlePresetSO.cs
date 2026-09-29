/*
 * Role hierarchy: Gameplay > World > Sectors > ScriptableObject
 * Primary responsibility: Keeps existing Normal-sector preset assets compatible while the historical class name remains serialized.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using UnityEngine;

[CreateAssetMenu(
    fileName = "SectorNormalPreset",
    menuName = "Game/Sector/Normal Sector Preset")]
public sealed class SectorNormalBattlePresetSO : SectorBattlePresetSO
{
    public override StageRoomType RoomType => StageRoomType.Normal;

#if UNITY_EDITOR
    // Purpose: Normalizes Inspector-authored data after it changes in the Editor.
    private void OnValidate()
    {
        SetSerializedRoomType(StageRoomType.Normal);
    }
#endif
}
