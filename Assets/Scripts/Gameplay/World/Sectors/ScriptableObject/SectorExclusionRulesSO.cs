/*
 * Role hierarchy: Gameplay > World > Sectors > ScriptableObject
 * Primary responsibility: Defines the SectorExclusionRulesSO data asset used to configure runtime rules in the Inspector.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using System;
using UnityEngine;

[CreateAssetMenu(
    fileName = "SectorExclusionRules",
    menuName = "Game/Sector/Sector Exclusion Rules")]
public class SectorExclusionRulesSO : ScriptableObject
{
    [Serializable]
    public class Entry
    {
        [Tooltip("Sector coord. Vector2Int.x = X, Vector2Int.y = Z.")]
        public Vector2Int coord;

        [Header("Exclusions")]
        public bool excludeFromEnemySpawn = true;
        public bool excludeFromNamedReservation = true;
        public bool excludeFromOccupancySummary = true;
        public bool hideFromMiniMapAlways = false;
    }

    [SerializeField] private Entry[] _entries;

    // Purpose: Performs the exclude from enemy spawn workflow for this component.
    public bool ExcludeFromEnemySpawn(Vector2Int coord)
    {
        return TryGetEntry(coord, out Entry entry) && entry.excludeFromEnemySpawn;
    }

    // Purpose: Performs the exclude from named reservation workflow for this component.
    public bool ExcludeFromNamedReservation(Vector2Int coord)
    {
        return TryGetEntry(coord, out Entry entry) && entry.excludeFromNamedReservation;
    }

    // Purpose: Performs the exclude from occupancy summary workflow for this component.
    public bool ExcludeFromOccupancySummary(Vector2Int coord)
    {
        return TryGetEntry(coord, out Entry entry) && entry.excludeFromOccupancySummary;
    }

    // Purpose: Hides the from mini map always UI or visual state.
    public bool HideFromMiniMapAlways(Vector2Int coord)
    {
        return TryGetEntry(coord, out Entry entry) && entry.hideFromMiniMapAlways;
    }

    // Purpose: Attempts to get entry and reports whether the request succeeded.
    private bool TryGetEntry(Vector2Int coord, out Entry result)
    {
        if (_entries != null)
        {
            for (int i = 0; i < _entries.Length; i++)
            {
                Entry entry = _entries[i];
                if (entry != null && entry.coord == coord)
                {
                    result = entry;
                    return true;
                }
            }
        }

        result = null;
        return false;
    }
}
