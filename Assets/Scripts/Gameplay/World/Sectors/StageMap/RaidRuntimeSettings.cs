using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class RaidRuntimeSettings
{
    [Header("해치 · 원정 중 다음 스테이지")]
    [Tooltip("ON: HOME 정산 없이 다음 해금된 스테이지로 진행. OFF: 기존 HOME 탈출.")]
    public bool hatchAdvancesStage = true;
    [Tooltip("선택한 퀘스트 완료 후에만 다음 스테이지 해치를 사용합니다. 다음 스테이지 접근 해금도 별도로 필요합니다.")]
    public QuestDefinitionSO hatchRequiredQuest;
    [Header("Infection management — demo tuning")]
    [Min(1f)] public float suppressionDurationSeconds = 180f;
    [Min(1)] public int stage1MedicalDose = 1;
    [Min(1)] public int stage2MedicalDose = 3;
    [Min(1)] public int stage3MedicalDose = 5;
    [Min(0f)] public float infectionPerPressureSecond = 1f;
    public int MedicalDose(int stage) => Mathf.Max(1,
        stage <= 1 ? stage1MedicalDose : stage == 2 ? stage2MedicalDose : stage3MedicalDose);
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
    [Header("Emergency merchant — test tuning")]
    public ShopConfigSO merchantConfig;
    [Min(0)] public int merchantInitialMoney = 1000;
    public SectorShopOpenRequestEventChannelSO merchantOpenChannel;
    public UIOverlayRequestEventChannelSO merchantOverlayChannel;
    [Header("Vault entry — uses existing Key Tool durability")]
    public LootKeyTier vaultEntryKeyTier = LootKeyTier.Normal;
    [Tooltip("새 원정 금고방은 입장 포탈 대신 내부 차단벽/4방향 패널을 사용합니다. 기존 저장의 포탈 잠금은 유지합니다.")]
    public bool vaultUsesInternalCombatLock = true;
    [Header("General-room supply — test tuning, existing loot pools")]
    [Range(0f, 1f)] public float normalSupplyChance = 0.25f;
    [Range(0f, 1f)] public float weaponSupplyShare = 0.5f;
    public LootObjectSO normalWeaponSupply;
    public LootObjectSO normalMedicalSupply;

    // Extraction is route metadata, never a separate room preset (including obsolete authoring rows).
    public RaidRoomPresetBinding FindRoom(RaidRoomRole role) => role == RaidRoomRole.Extraction ? null :
        rooms?.Find(row => row != null && row.role == role);
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
