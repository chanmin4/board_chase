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

    public bool ExcludeFromEnemySpawn(Vector2Int coord)
    {
        return TryGetEntry(coord, out Entry entry) && entry.excludeFromEnemySpawn;
    }

    public bool ExcludeFromNamedReservation(Vector2Int coord)
    {
        return TryGetEntry(coord, out Entry entry) && entry.excludeFromNamedReservation;
    }

    public bool ExcludeFromOccupancySummary(Vector2Int coord)
    {
        return TryGetEntry(coord, out Entry entry) && entry.excludeFromOccupancySummary;
    }

    public bool HideFromMiniMapAlways(Vector2Int coord)
    {
        return TryGetEntry(coord, out Entry entry) && entry.hideFromMiniMapAlways;
    }

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
