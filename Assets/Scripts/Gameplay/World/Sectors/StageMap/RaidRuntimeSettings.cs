using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class RaidRuntimeSettings
{
    [Tooltip("Temporary authoring value, not a final balance pass. Zero disables the raid clock.")]
    [Min(0)] public float durationSeconds = 1200f;
    [Min(0)] public float overtimeInfectionPerSecond = 6f;
    [Tooltip("Physical ItemSO required by the generator. No name-based item matching.")]
    public ItemSO generatorBattery;
    [Min(1)] public int batteryAmount = 1;
    [Tooltip("Optional authored terminal prefab. Empty uses a labeled prototype terminal.")]
    public RaidStation terminalPrefab;
    public TMPro.TMP_FontAsset prototypeFont;
    public AudioCueEventChannelSO sfxChannel;
    public AudioConfigurationSO audioConfiguration;
    public AudioCueSO generatorCue;
    public AudioCueSO extractionCue;
    public AudioCueSO rejectedCue;
    [Tooltip("WIP: a separate third extraction, not the central hatch. Disabled until authored.")]
    public bool specialExtractionWip;
    [Min(3)] public int specialExtractionMinimumStage = 3;
    public List<RaidRoomPresetBinding> rooms = new();

    public RaidRoomPresetBinding FindRoom(RaidRoomRole role) => rooms?.Find(row => row != null && row.role == role);
}

[Serializable]
public sealed class RaidRoomPresetBinding
{
    public RaidRoomRole role;
    [Tooltip("Fallback Normal preset, used when the variant pool has no valid entries. Empty keeps the stage's Normal pool.")]
    public SectorPresetSO sectorPreset;
    [Tooltip("Room layout variants for this role. Positive weights are relative frequencies. Uses stage seed + coordinate, including on resume. Boss layouts still belong in the stage's Boss Battles list.")]
    public List<StageDefinitionSO.WeightedSectorPreset> sectorVariants = new();
    [Tooltip("Optional role-specific encounter (e.g. armored guards in the Armory).")]
    public StageBattleEncounterPresetSO encounter;
    public SectorContentProfileSO content;
    [Tooltip("Typed container used for this room. Its World Prefab must contain LootableObject.")]
    public LootObjectSO lootObject;

    public bool TryPickSectorPreset(int stageSeed, Vector2Int coordinate, out SectorPresetSO preset)
    {
        // Do not consume UnityEngine.Random: loading the same raid must select the same layout.
        double total = 0;
        if (sectorVariants != null)
            foreach (var entry in sectorVariants)
                if (IsCandidate(entry)) total += entry.weight;

        if (total > 0)
        {
            var random = new System.Random(StageDefinitionSO.BuildSectorSeed(stageSeed, coordinate, 1601 + (int)role));
            double roll = random.NextDouble() * total;
            foreach (var entry in sectorVariants)
            {
                if (!IsCandidate(entry)) continue;
                if (roll < entry.weight)
                {
                    preset = entry.preset;
                    return true;
                }
                roll -= entry.weight;
            }
        }

        preset = IsNormalPreset(sectorPreset) ? sectorPreset : null;
        return preset != null;
    }

    public bool ContainsPreset(SectorPresetSO preset)
    {
        if (!IsNormalPreset(preset)) return false;
        if (sectorPreset == preset) return true;
        return sectorVariants != null && sectorVariants.Exists(entry => IsCandidate(entry) && entry.preset == preset);
    }

    private static bool IsCandidate(StageDefinitionSO.WeightedSectorPreset entry) =>
        entry != null && entry.weight > 0 && IsNormalPreset(entry.preset);

    private static bool IsNormalPreset(SectorPresetSO preset) =>
        preset != null && preset.IsValid && preset.RoomType == StageRoomType.Normal;
}
