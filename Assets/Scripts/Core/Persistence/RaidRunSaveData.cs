using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Run-only raid snapshot. Stores the resolved graph, never rerolls a loaded raid.</summary>
[Serializable]
public sealed class RaidRunSaveData
{
    public bool active;
    public StageMapLayout layout;
    public Vector2Int playerSectorCoord;
    public bool generatorPowered;
    public bool hatchRevealed;
    public bool bossDefeated;
    public bool extractionCommitted;
    public bool timerEnabled;
    public float remainingSeconds;
    public bool gasReleased;
    public List<RaidLootContainerSaveData> containers = new();

    public bool TryValidate(out string error)
    {
        error = string.Empty;
        if (!active) return true;
        if (layout == null || !layout.usesRaidLayout ||
            !RaidMapGenerator.IsSupportedGenerationVersion(layout.generationVersion))
        {
            error = "Saved raid has an unsupported layout version. It must not be regenerated silently.";
            return false;
        }
        if (!RaidMapValidation.TryValidate(layout, out error)) return false;
        if (!layout.TryGetRoom(playerSectorCoord, out _) || float.IsNaN(remainingSeconds) ||
            float.IsInfinity(remainingSeconds) || remainingSeconds < 0f || generatorPowered != hatchRevealed)
        {
            error = "Saved raid position, clock or power state is invalid.";
            return false;
        }
        return true;
    }
}

[Serializable]
public sealed class RaidLootContainerSaveData
{
    public string key;
    public bool rolled;
    public bool unlocked;
    public List<PlayerRunInventorySlotSaveData> items = new();
}
