using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Run-only raid snapshot. Stores the resolved graph, never rerolls a loaded raid.</summary>
[Serializable]
public sealed class RaidRunSaveData
{
    public bool active;
    public Vector2Int returnPortalCoord;
    public SectorPortalDirection returnPortalDirection;
    public float returnPortalCooldownRemaining;
    public StageMapLayout layout;
    public Vector2Int playerSectorCoord;
    public bool generatorPowered;
    public bool hatchRevealed;
    public bool bossDefeated;
    public bool extractionCommitted;
    public bool timerEnabled;
    public float remainingSeconds;
    public bool gasReleased;
    public InfectionSpreadSaveData infectionSpread = new();
    public bool medicalStateInitialized;
    public float suppressionRemainingSeconds;
    public float ampouleRemainingSeconds;
    public float ampouleRecoveryPerSecond;
    public float ampouleExposureMultiplier = 1f;
    public List<RaidLootContainerSaveData> containers = new();
    public List<RaidSectorContentSaveData> sectorContent = new();
    public List<RaidCoverSaveData> covers = new();

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
        if(!ValidMedicalNumber(returnPortalCooldownRemaining)||returnPortalCooldownRemaining>0f&&
            (!Enum.IsDefined(typeof(SectorPortalDirection),returnPortalDirection)||!layout.TryGetRoom(returnPortalCoord,out _)))
        {error="Saved return portal cooldown is invalid.";return false;}
        if (infectionSpread != null && infectionSpread.initialized && !ValidMedicalNumber(infectionSpread.remainingSeconds))
        {
            error = "Saved infection spread countdown is invalid.";
            return false;
        }
        if (!layout.TryGetRoom(playerSectorCoord, out _) || float.IsNaN(remainingSeconds) ||
            float.IsInfinity(remainingSeconds) || remainingSeconds < 0f || generatorPowered != hatchRevealed)
        {
            error = "Saved raid position, clock or power state is invalid.";
            return false;
        }
        if (medicalStateInitialized && (!ValidMedicalNumber(suppressionRemainingSeconds) ||
            !ValidMedicalNumber(ampouleRemainingSeconds) || !ValidMedicalNumber(ampouleRecoveryPerSecond) ||
            !ValidMedicalNumber(ampouleExposureMultiplier) || ampouleExposureMultiplier > 1f))
        {
            error = "Saved infection treatment state is invalid.";
            return false;
        }
        return true;
    }
    private static bool ValidMedicalNumber(float value) =>
        !float.IsNaN(value) && !float.IsInfinity(value) && value >= 0f;
}

[Serializable]
public sealed class RaidCoverSaveData
{
    public Vector2Int coord;
    public string placementId;
    public float remainingDurability;
}

/// <summary>추첨 실패 자리도 포함하는 섹터 단위 확정 결과. 후보/확률 변경으로 재추첨하지 않습니다.</summary>
[Serializable]
public sealed class RaidSectorContentSaveData
{
    public Vector2Int coord;
    public bool authoredLootPlacement;
    public List<RaidSectorContentSpawnSaveData> spawns = new();
}

[Serializable]
public sealed class RaidSectorContentSpawnSaveData
{
    public string slotId;
    public SectorContentSlotKind kind;
    public string prefabKey;
    public string itemGuid;
    public int amount = 1;
    public bool pickedUp;
    public Vector3 localPosition;
    public Quaternion localRotation = Quaternion.identity;
    public Vector3 localScale = Vector3.one;
    public float armorDurability = -1f;
    public float armorMaxDurability = -1f;
    public float weaponDurability = -1f;
    public float weaponMaxDurability = -1f;
    public float filterDurability = -1f;
    public float filterMaxDurability = -1f;
}

[Serializable]
public sealed class RaidLootContainerSaveData
{
    public string key;
    public bool hasRollSeed;
    public int rollSeed;
    public bool rolled;
    public bool unlocked;
    public List<PlayerRunInventorySlotSaveData> items = new();
}
