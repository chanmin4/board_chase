/*
 * Role hierarchy: Gameplay > World > Sectors > ScriptableObject
 * Primary responsibility: Defines the StageDefinitionSO data asset used to configure runtime rules in the Inspector.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(
    fileName = "StageDefinition",
    menuName = "Game/Stage/Stage Definition")]
public sealed class StageDefinitionSO : ScriptableObject
{
    private const int SectorPresetPickSalt = 809;
    private const int NormalEncounterPickSalt = 103;
    private const int BossBattlePickSalt = 211;

    [Serializable]
    public sealed class WeightedSectorPreset
    {
        [Tooltip("Authored sector preset eligible for this room type.")]
        public SectorPresetSO preset;

        [Tooltip("Relative selection weight. Zero disables this entry; totals do not need to equal 100.")]
        [FormerlySerializedAs("relativeWeight")]
        [Min(0)] public int weight = 1;
    }

    [Serializable]
    public sealed class WeightedEncounterPreset
    {
        [Tooltip("Reusable enemy encounter preset eligible for a Normal Battle room.")]
        public StageBattleEncounterPresetSO preset;

        [Tooltip("Relative selection weight. Zero disables this entry.")]
        [Min(0)] public int weight = 1;
    }

    [Serializable]
    public sealed class BossBattleEntry
    {
        [Tooltip("Encounter containing exactly one Boss and any Boss Guards required for room clear.")]
        public StageBattleEncounterPresetSO encounterPreset;

        [Tooltip("Exact authored Boss sector paired with this encounter. The pair is selected atomically.")]
        public SectorBossBattlePresetSO sectorPreset;

        [Tooltip("Relative selection weight for this complete Boss battle pair. Zero disables it.")]
        [Min(0)] public int weight = 1;
    }

    [Serializable]
    public sealed class WeightedBranchTopology
    {
        [Tooltip("Optional authored branch-and-merge map shape. Leave this list empty to keep the legacy full square grid.")]
        public StageTopologySO topology;

        [Tooltip("Relative deterministic selection weight. Zero keeps a WIP topology visible but disables it for this stage.")]
        [Min(0)] public int weight = 1;
    }

    [Serializable]
    public sealed class NormalBattleSettings
    {
        [Tooltip("Authored Normal Sector pool for this stage. Encounter and portal policy are configured separately during the next migration.")]
        public List<WeightedSectorPreset> sectorPresets = new();

        [Tooltip("Current default encounter pool for Normal Sector rooms. A later policy layer will allow no-combat, patrol, and ambush variants.")]
        public List<WeightedEncounterPreset> encounterPresets = new();

        [Tooltip("Normal Battle room timer duration. Boss rooms do not use this timer.")]
        [Min(0f)] public float timerSeconds = 30f;

        [Tooltip("If enabled, losing the timed requirement resets the Normal Battle timer.")]
        public bool resetTimerWhenRequirementLost;
    }

    [Serializable]
    public sealed class BiochemicalGasSettings
    {
        [Tooltip("Enables personal Infection pressure in eligible generated rooms. Stages 3-5 use this as their primary environmental mechanic.")]
        public bool enabled;

        [Tooltip("Applies the baseline gas pressure to Normal Sector rooms.")]
        public bool applyToNormalRooms = true;

        [Tooltip("Applies the baseline gas pressure to Boss Sector rooms. Keep this enabled only when the Boss encounter is authored around it.")]
        public bool applyToBossRooms = true;

        [Tooltip("Personal Infection added per second before gas-mask mitigation while the room's base gas is active.")]
        [Min(0f)] public float baselineInfectionPerSecond = 1f;

        [Tooltip("Additional Infection per second added each time the Normal Sector clear timer expires. This persists until the player leaves or clears that room.")]
        [Min(0f)] public float timerFailureLeakInfectionPerSecond = 1.5f;

        [Tooltip("Maximum number of timer-failure leak stacks allowed in one room.")]
        [Min(0)] public int maxTimerFailureLeakStacks = 3;

        [Tooltip("How often gas exposure is resolved. Lower values make the HUD react more smoothly but do not change total Infection per second.")]
        [Min(0.1f)] public float tickInterval = 0.5f;

        public bool AppliesTo(StageRoomType roomType)
        {
            if (!enabled)
                return false;

            return roomType switch
            {
                StageRoomType.Normal => applyToNormalRooms,
                StageRoomType.Boss => applyToBossRooms,
                _ => false
            };
        }
    }

    [Serializable]
    public sealed class TreasureSettings
    {
        [Header("WIP Legacy Room Generation (Not Used At Runtime)")]
        public bool enabled;
        [Min(0)] public int minCount;
        [Min(0)] public int maxCount = 1;
        [Range(0f, 1f)] public float extraChancePerNoHitStage;
        [Range(0f, 1f)] public float maxExtraChance = 1f;
        public bool excludeFirstRoom = true;

        [Header("WIP Legacy Room Presets (Not Used At Runtime)")]
        public List<WeightedSectorPreset> sectorPresets = new();

        [Header("WIP Legacy Reward (Use Loot Progression Rare Cache Instead)")]
        public TreasureRoomDropTableSO dropTable;
    }

    [Header("Identity")]
    [Tooltip("Stable stage index used by runtime snapshots and deterministic generation.")]
    [SerializeField] private int _stageIndex = 1;

    [Tooltip("HUD and run-result display name. The asset name is used when empty.")]
    [SerializeField] private string _displayName;

    [Header("Map")]
    [SerializeField] private StageMapPolicy _mapPolicy;
    [SerializeField] private RaidMapSettings _raidMap = new();
    [SerializeField] private RaidRuntimeSettings _raidRuntime = new();
    [Tooltip("Generated square room-grid size. Zero is valid only for Start Sector-only stages.")]
    [Min(0)] [SerializeField] private int _roomGridSize = 1;

    [Tooltip("If enabled, this stage contains only its Start Sector and no generated room grid.")]
    [SerializeField] private bool _useStartSectorOnly;

    [Header("Optional Branch Topology")]
    [Tooltip("When one or more valid entries have positive weight, this stage uses a deterministically selected authored branch-and-merge topology instead of Room Grid Size. Leave empty to preserve the full-grid map.")]
    [SerializeField] private List<WeightedBranchTopology> _branchTopologies = new();

    [Header("Start Room")]
    [Tooltip("Authored Start Sector pool for this stage.")]
    [SerializeField] private List<WeightedSectorPreset> _startSectorPresets = new();

    [Header("Normal Sector Rooms")]
    [SerializeField] private NormalBattleSettings _normalBattle = new();

    [Header("Sector Room Traits")]
    [Tooltip("Weighted room-trait pool used by generated Normal rooms, and optionally Boss rooms when that trait entry explicitly allows it.")]
    [SerializeField] private SectorRoomTraitTableSO _roomTraitTable;

    [Header("Stage Environmental Traits")]
    [Tooltip("WIP compatibility toggle. New authoring uses Biochemical Gas below. It remains serialized so existing Stage 3-5 assets retain their gas setting.")]
    [SerializeField] private bool _enableBiochemicalGas;

    [Tooltip("Personal Infection pressure for generated rooms. Timer failure adds leak stacks; it never uses the retired Infection Control resource.")]
    [SerializeField] private BiochemicalGasSettings _biochemicalGas = new();

    [Header("WIP Legacy Treasure Rooms (No Longer Generated)")]
    [SerializeField] private TreasureSettings _treasure = new();

    [Header("Boss Goal Room")]
    [Tooltip("Complete Boss encounter-and-sector pairs. Every generated stage uses a Boss goal room.")]
    [SerializeField] private List<BossBattleEntry> _bossBattles = new();

    [Header("Completion")]
    [Tooltip("Allows a Start Sector-only stage to advance as soon as its Start Sector completes.")]
    [SerializeField] private bool _advanceStageOnStartSectorComplete;

    [Tooltip("WIP retired Infection Control value. Preserved only to deserialize old StageDefinition assets; it has no gameplay effect.")]
    [HideInInspector] [Min(0f)] [SerializeField] private float _infectionControlRecoverOnComplete;

    public int StageIndex => _stageIndex;
    public string DisplayName => string.IsNullOrWhiteSpace(_displayName) ? name : _displayName;
    public int RoomGridSize => _roomGridSize;
    public StageMapPolicy MapPolicy => _mapPolicy;
    public RaidMapSettings RaidMap => _raidMap;
    public RaidRuntimeSettings RaidRuntime => _raidRuntime;
    public bool UseStartSectorOnly => _useStartSectorOnly;
    /// <summary>
    /// Read-only authoring view of the Normal room preset candidates. Runtime
    /// selection still uses TryPickSectorPreset and this accessor never
    /// creates, normalizes, or changes stage data.
    /// </summary>
    public IReadOnlyList<WeightedSectorPreset> NormalSectorPresets =>
        _normalBattle != null && _normalBattle.sectorPresets != null
            ? _normalBattle.sectorPresets
            : Array.Empty<WeightedSectorPreset>();

    /// <summary>
    /// Read-only authoring view of the Normal room encounter candidates. It
    /// lets editor tools explain the Spawn Point -> Encounter -> Enemy -> Loot
    /// path without assigning encounter data to an individual spawn marker.
    /// </summary>
    public IReadOnlyList<WeightedEncounterPreset> NormalEncounterPresets =>
        _normalBattle != null && _normalBattle.encounterPresets != null
            ? _normalBattle.encounterPresets
            : Array.Empty<WeightedEncounterPreset>();
    public IReadOnlyList<WeightedBranchTopology> BranchTopologies => _branchTopologies;
    public StageRoomType GoalRoomType => StageRoomType.Boss;
    public bool AdvanceStageOnStartSectorComplete => _advanceStageOnStartSectorComplete;
    public float InfectionControlRecoverOnComplete => 0f;
    public float NormalBattleTimerSeconds => _normalBattle != null ? _normalBattle.timerSeconds : 30f;
    public bool ResetNormalBattleTimerWhenRequirementLost =>
        _normalBattle != null && _normalBattle.resetTimerWhenRequirementLost;
    public SectorRoomTraitTableSO RoomTraitTable => _roomTraitTable;
    public bool EnableBiochemicalGas =>
        _biochemicalGas != null
            ? _biochemicalGas.enabled
            : _enableBiochemicalGas;
    public BiochemicalGasSettings BiochemicalGas => _biochemicalGas;
    public TreasureRoomDropTableSO TreasureDropTable => _treasure != null ? _treasure.dropTable : null;
    public bool HasLegacyTreasureRoomData =>
        _treasure != null &&
        (_treasure.enabled ||
         _treasure.dropTable != null ||
         (_treasure.sectorPresets != null && _treasure.sectorPresets.Count > 0));

    public bool HasEnabledBranchTopology
    {
        get
        {
            if (_branchTopologies == null)
                return false;

            for (int i = 0; i < _branchTopologies.Count; i++)
            {
                WeightedBranchTopology entry = _branchTopologies[i];
                if (entry != null && entry.weight > 0)
                    return true;
            }

            return false;
        }
    }

    /// <summary>
    /// WIP compatibility entry point. Treasure rooms are no longer generated;
    /// their old serialized data is retained only so it can be imported into a
    /// Loot Progression Rare Cache source without losing authored rewards.
    /// </summary>
    public StageTreasureRoomGenerationSettings CreateTreasureRoomGenerationSettings(
        int consecutiveNoHitStageCount)
    {
        return StageTreasureRoomGenerationSettings.Disabled;
    }

    // Purpose: Attempts to pick sector preset and reports whether the request succeeded.
    public bool TryPickSectorPreset(
        int stageSeed,
        Vector2Int sectorCoord,
        StageRoomType roomType,
        out SectorPresetSO preset)
    {
        preset = null;

        List<WeightedSectorPreset> candidates = roomType switch
        {
            StageRoomType.Start => _startSectorPresets,
            StageRoomType.Normal => _normalBattle != null ? _normalBattle.sectorPresets : null,
            StageRoomType.Treasure => _treasure != null ? _treasure.sectorPresets : null,
            // Shops exist only in the shared Start hub, never as generated rooms.
            StageRoomType.Shop => null,
            _ => null
        };

        return TryPickWeightedSector(
            candidates,
            roomType,
            BuildSectorSeed(stageSeed, sectorCoord, SectorPresetPickSalt + (int)roomType),
            out preset);
    }

    /// <summary>
    /// Selects one complete authored branch topology using the stage seed. No
    /// entry means the caller must retain the legacy full square grid.
    /// </summary>
    public bool TryPickBranchTopology(int stageSeed, out StageTopologySO topology)
    {
        topology = null;
        if (_branchTopologies == null)
            return false;

        int totalWeight = 0;
        for (int i = 0; i < _branchTopologies.Count; i++)
        {
            WeightedBranchTopology entry = _branchTopologies[i];
            if (IsBranchTopologyCandidateValid(entry))
                totalWeight += entry.weight;
        }

        if (totalWeight <= 0)
            return false;

        int roll = new System.Random(stageSeed ^ 0x49A3).Next(0, totalWeight);
        for (int i = 0; i < _branchTopologies.Count; i++)
        {
            WeightedBranchTopology entry = _branchTopologies[i];
            if (!IsBranchTopologyCandidateValid(entry))
                continue;

            if (roll < entry.weight)
            {
                topology = entry.topology;
                return true;
            }

            roll -= entry.weight;
        }

        return false;
    }

    // Purpose: Attempts to validate branch topology pool and reports whether the request succeeded.
    public bool TryValidateBranchTopologyPool(out string error)
    {
        if (!HasEnabledBranchTopology)
        {
            error = string.Empty;
            return true;
        }

        if (_branchTopologies == null)
        {
            error = "Branch topology list is missing.";
            return false;
        }

        for (int i = 0; i < _branchTopologies.Count; i++)
        {
            WeightedBranchTopology entry = _branchTopologies[i];
            if (entry == null || entry.weight <= 0)
                continue;

            if (entry.topology == null)
            {
                error = $"Branch topology entry {i} has positive weight but no StageTopologySO.";
                return false;
            }

            if (!entry.topology.TryValidate(out string topologyError))
            {
                error = $"Branch topology '{entry.topology.name}' is invalid: {topologyError}";
                return false;
            }
        }

        error = string.Empty;
        return true;
    }

    /// <summary>
    /// Sector Maker uses this explicit opt-in method when an author chooses to
    /// add its currently edited topology to a StageDefinitionSO.
    /// </summary>
    public void AddOrUpdateBranchTopology(StageTopologySO topology, int weight)
    {
        if (topology == null)
            return;

        _branchTopologies ??= new List<WeightedBranchTopology>();

        for (int i = 0; i < _branchTopologies.Count; i++)
        {
            WeightedBranchTopology entry = _branchTopologies[i];
            if (entry != null && entry.topology == topology)
            {
                entry.weight = Mathf.Max(0, weight);
                return;
            }
        }

        _branchTopologies.Add(new WeightedBranchTopology
        {
            topology = topology,
            weight = Mathf.Max(0, weight)
        });
    }

    // Purpose: Attempts to pick normal sector encounter and reports whether the request succeeded.
    public bool TryPickNormalSectorEncounter(
        int stageSeed,
        Vector2Int sectorCoord,
        out StageBattleEncounterPresetSO preset)
    {
        return TryPickWeightedEncounter(
            _normalBattle != null ? _normalBattle.encounterPresets : null,
            BuildSectorSeed(stageSeed, sectorCoord, NormalEncounterPickSalt),
            out preset);
    }

    /// <summary>
    /// Compatibility entry point retained while call sites migrate to the
    /// Normal Sector terminology. New code should use TryPickNormalSectorEncounter.
    /// </summary>
    public bool TryPickNormalBattleEncounter(
        int stageSeed,
        Vector2Int sectorCoord,
        out StageBattleEncounterPresetSO preset)
    {
        return TryPickNormalSectorEncounter(stageSeed, sectorCoord, out preset);
    }

    // Purpose: Checks whether this component has normal sector that uses enemy encounter.
    public bool HasNormalSectorThatUsesEnemyEncounter()
    {
        if (_normalBattle?.sectorPresets == null)
            return false;

        for (int i = 0; i < _normalBattle.sectorPresets.Count; i++)
        {
            WeightedSectorPreset entry = _normalBattle.sectorPresets[i];
            if (IsSectorCandidateValid(entry, StageRoomType.Normal) &&
                NormalSectorPolicySO.ResolveUsesEnemyEncounter(entry.preset.NormalSectorPolicy))
            {
                return true;
            }
        }

        return false;
    }

    // Purpose: Attempts to pick boss battle and reports whether the request succeeded.
    public bool TryPickBossBattle(
        int stageSeed,
        Vector2Int sectorCoord,
        out StageBattleEncounterPresetSO encounterPreset,
        out SectorBossBattlePresetSO sectorPreset)
    {
        encounterPreset = null;
        sectorPreset = null;

        if (_bossBattles == null)
            return false;

        int totalWeight = 0;

        for (int i = 0; i < _bossBattles.Count; i++)
        {
            if (IsBossBattleValid(_bossBattles[i]))
                totalWeight += _bossBattles[i].weight;
        }

        if (totalWeight <= 0)
            return false;

        int roll = new System.Random(
            BuildSectorSeed(stageSeed, sectorCoord, BossBattlePickSalt)).Next(0, totalWeight);

        for (int i = 0; i < _bossBattles.Count; i++)
        {
            BossBattleEntry candidate = _bossBattles[i];

            if (!IsBossBattleValid(candidate))
                continue;

            if (roll < candidate.weight)
            {
                encounterPreset = candidate.encounterPreset;
                sectorPreset = candidate.sectorPreset;
                return true;
            }

            roll -= candidate.weight;
        }

        return false;
    }

    // Purpose: Builds the sector seed from the current authored data.
    public static int BuildSectorSeed(int stageSeed, Vector2Int sectorCoord, int salt)
    {
        unchecked
        {
            int hash = 17;
            hash = hash * 31 + stageSeed;
            hash = hash * 31 + sectorCoord.x;
            hash = hash * 31 + sectorCoord.y;
            hash = hash * 31 + salt;
            return hash;
        }
    }

    // Purpose: Attempts to pick weighted sector and reports whether the request succeeded.
    private static bool TryPickWeightedSector(
        List<WeightedSectorPreset> candidates,
        StageRoomType expectedRoomType,
        int seed,
        out SectorPresetSO picked)
    {
        picked = null;
        int totalWeight = 0;

        if (candidates == null)
            return false;

        for (int i = 0; i < candidates.Count; i++)
        {
            if (IsSectorCandidateValid(candidates[i], expectedRoomType))
                totalWeight += candidates[i].weight;
        }

        if (totalWeight <= 0)
            return false;

        int roll = new System.Random(seed).Next(0, totalWeight);

        for (int i = 0; i < candidates.Count; i++)
        {
            WeightedSectorPreset candidate = candidates[i];

            if (!IsSectorCandidateValid(candidate, expectedRoomType))
                continue;

            if (roll < candidate.weight)
            {
                picked = candidate.preset;
                return true;
            }

            roll -= candidate.weight;
        }

        return false;
    }

    // Purpose: Attempts to pick weighted encounter and reports whether the request succeeded.
    private static bool TryPickWeightedEncounter(
        List<WeightedEncounterPreset> candidates,
        int seed,
        out StageBattleEncounterPresetSO picked)
    {
        picked = null;
        int totalWeight = 0;

        if (candidates == null)
            return false;

        for (int i = 0; i < candidates.Count; i++)
        {
            if (IsEncounterCandidateValid(candidates[i]))
                totalWeight += candidates[i].weight;
        }

        if (totalWeight <= 0)
            return false;

        int roll = new System.Random(seed).Next(0, totalWeight);

        for (int i = 0; i < candidates.Count; i++)
        {
            WeightedEncounterPreset candidate = candidates[i];

            if (!IsEncounterCandidateValid(candidate))
                continue;

            if (roll < candidate.weight)
            {
                picked = candidate.preset;
                return true;
            }

            roll -= candidate.weight;
        }

        return false;
    }

    // Purpose: Checks whether sector candidate valid is currently true.
    private static bool IsSectorCandidateValid(
        WeightedSectorPreset candidate,
        StageRoomType expectedRoomType)
    {
        return candidate != null &&
               candidate.weight > 0 &&
               candidate.preset != null &&
               candidate.preset.IsValid &&
               candidate.preset.RoomType == expectedRoomType;
    }

    // Purpose: Checks whether encounter candidate valid is currently true.
    private static bool IsEncounterCandidateValid(WeightedEncounterPreset candidate)
    {
        return candidate != null &&
               candidate.weight > 0 &&
               candidate.preset != null &&
               candidate.preset.IsValid;
    }

    // Purpose: Checks whether boss battle valid is currently true.
    private static bool IsBossBattleValid(BossBattleEntry candidate)
    {
        return candidate != null &&
               candidate.weight > 0 &&
               candidate.encounterPreset != null &&
               candidate.encounterPreset.IsValid &&
               candidate.sectorPreset != null &&
               candidate.sectorPreset.IsValid &&
               candidate.sectorPreset.RoomType == StageRoomType.Boss;
    }

    // Editor validation must point at the leaf asset that needs authoring.  The
    // previous single "Invalid Boss battle pair" message hid whether the
    // problem was the encounter, a wave, an enemy config, or the room preset.
    // Purpose: Performs the describe boss battle invalid reason workflow for this component.
    private static string DescribeBossBattleInvalidReason(BossBattleEntry candidate)
    {
        if (candidate == null)
            return "Boss Battles entry is null.";

        if (candidate.weight <= 0)
            return $"Weight must be greater than zero. value={candidate.weight}.";

        if (candidate.encounterPreset == null)
            return "Encounter Preset is None or missing.";

        string encounterError = DescribeEncounterInvalidReason(candidate.encounterPreset);

        if (!string.IsNullOrEmpty(encounterError))
            return encounterError;

        if (candidate.sectorPreset == null)
            return "Sector Preset is None or missing.";

        if (candidate.sectorPreset.SectorPrefab == null)
        {
            return $"Sector Preset '{candidate.sectorPreset.name}' has Sector Prefab " +
                   "(SectorRuntime) None or a stale missing component reference.";
        }

        if (!candidate.sectorPreset.IsValid)
            return $"Sector Preset '{candidate.sectorPreset.name}' is invalid. Assign its authored Boss sector prefab.";

        if (candidate.sectorPreset.RoomType != StageRoomType.Boss)
        {
            return $"Sector Preset '{candidate.sectorPreset.name}' has Room Type " +
                   $"'{candidate.sectorPreset.RoomType}', but Boss is required.";
        }

        return "The pair failed validation for an unknown reason.";
    }

    // Purpose: Performs the describe encounter invalid reason workflow for this component.
    private static string DescribeEncounterInvalidReason(StageBattleEncounterPresetSO encounter)
    {
        if (!encounter.IsValid)
        {
            IReadOnlyList<StageBattleEncounterPresetSO.EncounterWave> waves = encounter.Waves;

            if (waves == null || waves.Count == 0)
                return $"Encounter Preset '{encounter.name}' has no Waves.";

            for (int waveIndex = 0; waveIndex < waves.Count; waveIndex++)
            {
                StageBattleEncounterPresetSO.EncounterWave wave = waves[waveIndex];

                if (wave == null)
                    return $"Encounter Preset '{encounter.name}' has a null Wave at index={waveIndex}.";

                List<StageBattleEncounterPresetSO.EnemyWavePresetCandidate> candidates =
                    wave.enemyWavePresetCandidates;

                if (candidates == null || candidates.Count == 0)
                {
                    return $"Encounter Preset '{encounter.name}' Wave index={waveIndex} " +
                           "has no Wave Preset candidates.";
                }

                bool hasValidCandidate = false;
                string firstCandidateError = null;

                for (int candidateIndex = 0; candidateIndex < candidates.Count; candidateIndex++)
                {
                    StageBattleEncounterPresetSO.EnemyWavePresetCandidate candidate = candidates[candidateIndex];

                    if (candidate == null)
                    {
                        firstCandidateError ??= $"Encounter Preset '{encounter.name}' Wave index={waveIndex} " +
                                                $"has a null candidate at index={candidateIndex}.";
                        continue;
                    }

                    if (candidate.weight <= 0)
                    {
                        firstCandidateError ??= $"Encounter Preset '{encounter.name}' Wave index={waveIndex} " +
                                                $"candidate index={candidateIndex} has Weight={candidate.weight}.";
                        continue;
                    }

                    if (candidate.preset == null)
                    {
                        firstCandidateError ??= $"Encounter Preset '{encounter.name}' Wave index={waveIndex} " +
                                                $"candidate index={candidateIndex} has Wave Preset None or missing.";
                        continue;
                    }

                    if (!candidate.preset.IsValid)
                    {
                        firstCandidateError ??= DescribeEnemyWaveInvalidReason(
                            encounter,
                            waveIndex,
                            candidateIndex,
                            candidate.preset);
                        continue;
                    }

                    hasValidCandidate = true;
                }

                if (!hasValidCandidate)
                    return firstCandidateError ?? $"Encounter Preset '{encounter.name}' Wave index={waveIndex} has no valid candidate.";
            }
        }

        return null;
    }

    // Purpose: Performs the describe enemy wave invalid reason workflow for this component.
    private static string DescribeEnemyWaveInvalidReason(
        StageBattleEncounterPresetSO encounter,
        int encounterWaveIndex,
        int candidateIndex,
        StageEnemyWavePresetSO wavePreset)
    {
        IReadOnlyList<StageEnemyWavePresetSO.EnemySpawn> spawns = wavePreset.Enemies;

        if (spawns == null || spawns.Count == 0)
        {
            return $"Encounter Preset '{encounter.name}' Wave index={encounterWaveIndex} " +
                   $"candidate index={candidateIndex} references Wave Preset '{wavePreset.name}', " +
                   "but it has no enemy spawns.";
        }

        bool hasValidSpawn = false;
        string firstSpawnError = null;

        for (int spawnIndex = 0; spawnIndex < spawns.Count; spawnIndex++)
        {
            StageEnemyWavePresetSO.EnemySpawn spawn = spawns[spawnIndex];

            if (spawn == null)
            {
                firstSpawnError ??= $"Wave Preset '{wavePreset.name}' has a null enemy spawn at index={spawnIndex}.";
                continue;
            }

            if (spawn.count <= 0)
            {
                firstSpawnError ??= $"Wave Preset '{wavePreset.name}' enemy spawn index={spawnIndex} " +
                                    $"has Count={spawn.count}.";
                continue;
            }

            if (spawn.archetype == null)
            {
                firstSpawnError ??= $"Wave Preset '{wavePreset.name}' enemy spawn index={spawnIndex} " +
                                    "has Enemy Shooter Config None or missing.";
                continue;
            }

            if (!spawn.archetype.IsValid)
            {
                firstSpawnError ??= $"Enemy Shooter Config '{spawn.archetype.name}' in Wave Preset " +
                                    $"'{wavePreset.name}' has Enemy Shooter Prefab None or missing.";
                continue;
            }

            hasValidSpawn = true;
        }

        return hasValidSpawn
            ? null
            : firstSpawnError ?? $"Wave Preset '{wavePreset.name}' has no valid enemy spawn.";
    }

    // Purpose: Checks whether branch topology candidate valid is currently true.
    private static bool IsBranchTopologyCandidateValid(WeightedBranchTopology candidate)
    {
        return candidate != null &&
               candidate.weight > 0 &&
               candidate.topology != null &&
               candidate.topology.TryValidate(out _);
    }

#if UNITY_EDITOR
    // Purpose: Normalizes Inspector-authored data after it changes in the Editor.
    private void OnValidate()
    {
        ValidateSectorPool(_startSectorPresets, StageRoomType.Start, "Start");
        ValidateSectorPool(
            _normalBattle != null ? _normalBattle.sectorPresets : null,
            StageRoomType.Normal,
            "Normal Battle");

        if (!TryValidateBranchTopologyPool(out string topologyError))
            Debug.LogError($"[StageDefinitionSO] Invalid Branch Topology pool. {topologyError}", this);
        // WIP legacy: dedicated Treasure rooms no longer participate in map
        // generation, so their old preset pool must not block authoring.
        if (_normalBattle != null && _normalBattle.encounterPresets != null)
        {
            for (int i = 0; i < _normalBattle.encounterPresets.Count; i++)
            {
                WeightedEncounterPreset entry = _normalBattle.encounterPresets[i];

                if (entry != null && entry.weight > 0 && !IsEncounterCandidateValid(entry))
                {
                    string reason = entry.preset == null
                        ? "Encounter Preset is None or points to an asset with the wrong ScriptableObject type."
                        : DescribeEncounterInvalidReason(entry.preset) ??
                          $"Encounter Preset '{entry.preset.name}' failed validation for an unknown reason.";

                    Debug.LogError(
                        $"[StageDefinitionSO] Invalid Normal Battle encounter. " +
                        $"stage={StageIndex} ({name}), index={i}. {reason}",
                        this);
                }
            }
        }

        if (_bossBattles == null || _bossBattles.Count == 0)
        {
            Debug.LogError("[StageDefinitionSO] At least one Boss battle pair is required.", this);
            return;
        }

        for (int i = 0; i < _bossBattles.Count; i++)
        {
            BossBattleEntry entry = _bossBattles[i];

            if (entry != null && entry.weight == 0)
                continue;

            if (!IsBossBattleValid(entry))
            {
                Debug.LogError(
                    $"[StageDefinitionSO] Invalid Boss battle pair. index={i}. " +
                    DescribeBossBattleInvalidReason(entry),
                    this);
            }
        }
    }

    // Purpose: Validates sector pool and reports invalid authored data.
    private void ValidateSectorPool(
        List<WeightedSectorPreset> candidates,
        StageRoomType roomType,
        string label)
    {
        if (candidates == null)
            return;

        for (int i = 0; i < candidates.Count; i++)
        {
            WeightedSectorPreset entry = candidates[i];

            if (entry != null && entry.weight == 0)
                continue;

            if (!IsSectorCandidateValid(entry, roomType))
                Debug.LogError($"[StageDefinitionSO] Invalid {label} sector preset. index={i}", this);
        }
    }
#endif
}
