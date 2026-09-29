/*
 * Role hierarchy: Gameplay > World > Sectors > ScriptableObject
 * Primary responsibility: Defines the SectorPresetSO data asset used to configure runtime rules in the Inspector.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using UnityEngine;

[CreateAssetMenu(
    fileName = "SectorPreset",
    menuName = "Game/Stage/Sector Preset")]
public class SectorPresetSO : ScriptableObject
{
    [Header("Display")]
    [Tooltip("Inspector label for this authored sector preset. The asset name is used when this is empty.")]
    [SerializeField] private string _displayName;

    [Header("Sector")]
    [Tooltip("Stage room type that may select this preset. Concrete battle preset types keep this value fixed automatically.")]
    [SerializeField] private StageRoomType _roomType = StageRoomType.Normal;

    [Tooltip("Authored SectorRuntime prefab instantiated when this preset is selected. Runtime cover, structures, and spawn points must already be stored in this prefab.")]
    [SerializeField] private SectorRuntime _sectorPrefab;

    [Header("Normal Sector Policy")]
    [Tooltip("Optional policy for Normal rooms using this preset. Leave empty to preserve the historical Required Encounter + Clear Encounter behaviour.")]
    [SerializeField] private NormalSectorPolicySO _normalSectorPolicy;

    [Header("Normal Sector Content Slots")]
    [Tooltip("Optional content plan for this Normal Sector preset. Its slot IDs must match SectorContentSlotAnchor components on the assigned sector prefab. The runtime runner populates only Normal Sector rooms from this plan.")]
    [SerializeField] private SectorContentProfileSO _contentProfile;

    public string DisplayName => string.IsNullOrWhiteSpace(_displayName)
        ? name
        : _displayName;

    public virtual StageRoomType RoomType => _roomType;
    public SectorRuntime SectorPrefab => _sectorPrefab;
    public NormalSectorPolicySO NormalSectorPolicy => _normalSectorPolicy;
    public SectorContentProfileSO ContentProfile => _contentProfile;
    public bool IsValid => RoomType != StageRoomType.Empty && _sectorPrefab != null;

    /// <summary>
    /// Used by Sector Maker when a designer explicitly applies a content
    /// profile. Runtime generation only reads this reference.
    /// </summary>
    public void SetContentProfile(SectorContentProfileSO profile)
    {
        _contentProfile = profile;
    }

    // Purpose: Updates the serialized room type state.
    protected void SetSerializedRoomType(StageRoomType roomType)
    {
        _roomType = roomType;
    }
}
