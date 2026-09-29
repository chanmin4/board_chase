/*
 * Role hierarchy: Gameplay > World > Sectors > Content
 * Primary responsibility: Marks one authored transform as a future deterministic sector-content spawn location.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using UnityEngine;

/// <summary>
/// Place this component on an empty child in a SectorRuntime prefab. Its ID
/// must match a row in SectorContentProfileSO. SectorContentSlotSpawner owns
/// the deterministic spawn and cleanup work; this component only keeps the
/// authored location and semantic label.
/// </summary>
[DisallowMultipleComponent]
public sealed class SectorContentSlotAnchor : MonoBehaviour
{
    [Tooltip("Stable profile key. Example: loot_a, loot_b, rare_cache_a, special_npc_a.")]
    [SerializeField] private string _slotId = "content_slot";

    [Tooltip("Designer-facing semantic label. The runtime runner verifies this against the profile before spawning.")]
    [SerializeField] private SectorContentSlotKind _contentKind = SectorContentSlotKind.LootContainer;

    [Tooltip("Optional child used as the instantiated prefab's parent. Leave empty to use this anchor transform.")]
    [SerializeField] private Transform _spawnParent;

    [Tooltip("Keeps the marker in the prefab while allowing this location to be disabled without deleting it.")]
    [SerializeField] private bool _enabledForRuntime = true;

    public string SlotId => _slotId;
    public SectorContentSlotKind ContentKind => _contentKind;
    public Transform SpawnParent => _spawnParent != null ? _spawnParent : transform;
    public bool EnabledForRuntime => _enabledForRuntime;

    /// <summary>
    /// Authoring-only tools configure an anchor immediately after creating it.
    /// Runtime code only reads these values and never mutates prefab layout.
    /// </summary>
    public void ConfigureForAuthoring(string slotId, SectorContentSlotKind contentKind)
    {
        _slotId = slotId != null ? slotId.Trim() : string.Empty;
        _contentKind = contentKind;
    }

#if UNITY_EDITOR
    // Purpose: Normalizes Inspector-authored data after it changes in the Editor.
    private void OnValidate()
    {
        _slotId = _slotId != null ? _slotId.Trim() : string.Empty;

        if (_spawnParent != null && !_spawnParent.IsChildOf(transform))
        {
            Debug.LogWarning(
                $"[SectorContentSlotAnchor] Spawn Parent must be this anchor or one of its children. anchor={name}, parent={_spawnParent.name}",
                this);
            _spawnParent = null;
        }
    }
#endif
}
