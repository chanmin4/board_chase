using System;
using UnityEngine;

// Existing branch/full-grid assets keep their serialized meaning until opted in.
public enum StageMapPolicy { Legacy = 0, OddSquareRaid = 1 }

// Content is independent of traversal/completion type (Normal/Boss/Start).
public enum RaidRoomRole
{
    None = 0, Normal = 1, Armory = 2, Medical = 3, Vault = 4,
    Boss = 5, Generator = 6, Hatch = 7, Extraction = 8
}

[Serializable]
public sealed class RaidMapSettings
{
    [Range(0, 100)] public int bossSpawnPercent = 65;
    [Min(0)] public int minimumExtractionPathDistance = 0;
    [Tooltip("Relative weights for unreserved rooms. Special coordinates are never overwritten.")]
    [Min(0)] public int normalWeight = 70;
    [Min(0)] public int armoryWeight = 10;
    [Min(0)] public int medicalWeight = 15;
    [Min(0)] public int vaultWeight = 5;

    public bool TryValidate(out string error)
    {
        if (bossSpawnPercent < 0 || bossSpawnPercent > 100 || minimumExtractionPathDistance < 0 ||
            normalWeight < 0 || armoryWeight < 0 || medicalWeight < 0 || vaultWeight < 0 ||
            (long)normalWeight + armoryWeight + medicalWeight + vaultWeight <= 0 ||
            (long)normalWeight + armoryWeight + medicalWeight + vaultWeight > int.MaxValue)
        {
            error = "Raid weights, boss probability or extraction distance are invalid.";
            return false;
        }
        error = string.Empty;
        return true;
    }
}
