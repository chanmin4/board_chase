using System;
using UnityEngine;

/// <summary>Owns one generated raid's power, clock and extraction state.</summary>
[DisallowMultipleComponent]
public sealed class RaidRuntime : MonoBehaviour
{
    public static RaidRuntime Active { get; private set; }
    public RaidRunSaveData State { get; private set; }
    public StageDefinitionSO Definition { get; private set; }
    private SectorStateManager _sectors;
    private bool _transaction;
    private float _nextCheckpoint;
    private PlayerInventoryRuntime _player;
    private bool _ready;
    private readonly System.Collections.Generic.List<LootableObject> _containers = new();

    public void Initialize(StageDefinitionSO definition, StageMapLayout layout, SectorStateManager sectors,
        RaidRunSaveData restored = null)
    {
        Active = this;
        Definition = definition;
        _sectors = sectors;
        _ready = false;
        _transaction = false;
        _containers.Clear();
        State = restored ?? new RaidRunSaveData
        {
            active = true, layout = layout, playerSectorCoord = layout.startSectorCoord,
            timerEnabled = definition.RaidRuntime.durationSeconds > 0,
            remainingSeconds = Mathf.Max(0, definition.RaidRuntime.durationSeconds)
        };
        State.layout = layout;
        _nextCheckpoint = 10f;
    }

    public void BindContainers(SectorRuntime sector)
    {
        RuntimeContentCatalogSO catalog = Resources.Load<RuntimeContentCatalogSO>("RuntimeContentCatalog");
        foreach (LootableObject container in sector.GetComponentsInChildren<LootableObject>(true))
        {
            string key = "";
            for (Transform current = container.transform; current != null && current != sector.transform; current = current.parent)
                key = "/" + current.name + ":" + current.GetSiblingIndex() + key;
            key = sector.Coord.x + "," + sector.Coord.y + key;
            int seed = State.layout.stageSeed;
            unchecked { foreach (char c in key) seed = seed * 31 + c; }
            var saved = State.containers?.Find(entry => entry != null && entry.key == key);
            container.BindRaidState(key, seed, saved, catalog);
            _containers.Add(container);
        }
    }

    public void MarkReady() => _ready = true;
    public void Suspend() => _ready = false;
    public static bool IsHatchRevealedFor(StageMapLayout layout) => Active != null &&
        Active.State != null && Active.State.active && Active.State.layout == layout && Active.State.hatchRevealed;

    public bool IsInSector(SectorRuntime sector) => _ready && State != null && State.active && !State.extractionCommitted &&
        sector != null && _sectors != null && _sectors.CurrentSector == sector && _sectors.IsManagedSector(sector);

    public static float CurrentOvertimeInfection => Active != null && Active.State != null &&
        Active.State.active && Active.State.gasReleased && Active.IsInSector(Active._sectors.CurrentSector)
        ? Mathf.Max(0, Active.Definition.RaidRuntime.overtimeInfectionPerSecond) : 0f;

    private void Update()
    {
        if (!_ready || State == null || !State.active || _sectors == null || !IsInSector(_sectors.CurrentSector)) return;
        if (_player == null) _player = FindFirstObjectByType<PlayerInventoryRuntime>();
        Damageable player = _player != null ? _player.GetComponent<Damageable>() : null;
        if (player != null && player.IsDead) { EndRaid(); return; }
        State.playerSectorCoord = _sectors.CurrentSector.Coord;
        if (State.layout.bossSpawned && State.layout.TryGetRoom(State.layout.bossRoomCoord, out var boss))
            State.bossDefeated = boss.isCleared;
        TickClock(State, Time.deltaTime); // Pauses with game time; offline time does not elapse.
        _nextCheckpoint -= Time.deltaTime;
        if (_nextCheckpoint <= 0f)
        {
            _nextCheckpoint = 10f;
            SaveSystem.Active?.TrySaveRunDataToDisk();
        }
    }

    public static void TickClock(RaidRunSaveData state, float deltaSeconds)
    {
        if (state == null || !state.active || state.extractionCommitted || !state.timerEnabled ||
            deltaSeconds <= 0 || float.IsNaN(deltaSeconds) || float.IsInfinity(deltaSeconds)) return;
        state.remainingSeconds = Mathf.Max(0, state.remainingSeconds - deltaSeconds);
        if (state.remainingSeconds <= 0) state.gasReleased = true;
    }

    public bool TryPowerGenerator(PlayerInventoryRuntime inventory)
    {
        if (_transaction || inventory == null || _sectors == null || !IsInSector(_sectors.CurrentSector) ||
            _sectors.CurrentSector.Coord != State.layout.generatorRoomCoord || State.generatorPowered ||
            inventory.GetComponent<Damageable>()?.IsDead == true) return false;
        RaidRuntimeSettings settings = Definition.RaidRuntime;
        if (settings.generatorBattery == null || settings.batteryAmount <= 0 ||
            inventory.GetInventoryItemAmount(settings.generatorBattery) < settings.batteryAmount)
        { Play(settings.rejectedCue, inventory.transform.position); return false; }

        _transaction = true;
        try
        {
            // Set first so inventory change callbacks capture the same logical transaction.
            State.generatorPowered = State.hatchRevealed = true;
            if (!inventory.TryConsumeInventoryItem(settings.generatorBattery, settings.batteryAmount, out _))
            { State.generatorPowered = State.hatchRevealed = false; return false; }
            Capture(SaveSystem.Active?.RunData);
            if (SaveSystem.Active != null && !SaveSystem.Active.TrySaveRunDataToDisk())
                Debug.LogError("[Raid] Power applied in memory, but saving failed. The last verified run remains recoverable.", this);
            FindFirstObjectByType<SectorOccupancyManager>()?.RefreshRaidMarkers();
            Play(settings.generatorCue, inventory.transform.position);
            return true;
        }
        finally { _transaction = false; }
    }

    public bool TryExtract(SectorRuntime sector, RaidStationKind kind, PlayerInventoryRuntime inventory)
    {
        if (_transaction || inventory == null || inventory.GetComponent<Damageable>()?.IsDead == true || !IsInSector(sector)) return false;
        bool allowed = AllowsExtraction(State, sector.Coord, kind);
        if (!allowed) { Play(Definition.RaidRuntime.rejectedCue, inventory.transform.position); return false; }
        StageProgressionManager progression = FindFirstObjectByType<StageProgressionManager>();
        if (progression == null) return false;
        _transaction = true;
        State.extractionCommitted = true;
        try
        {
            if (!progression.TryExtractToHomeFromNormalExit(sector))
            { State.extractionCommitted = false; return false; }
            Play(Definition.RaidRuntime.extractionCue, inventory.transform.position);
            return true;
        }
        finally { _transaction = false; }
    }

    public static bool AllowsExtraction(RaidRunSaveData state, Vector2Int coord, RaidStationKind kind)
    {
        if (state == null || !state.active || state.extractionCommitted || state.layout == null) return false;
        return kind switch
        {
            RaidStationKind.StartReturn => coord == state.layout.startSectorCoord,
            RaidStationKind.NormalExtraction => coord == state.layout.extractionSectorCoord,
            RaidStationKind.Hatch => coord == state.layout.hatchRoomCoord && state.generatorPowered,
            _ => false // Special extraction is an intentionally non-operational authoring scaffold.
        };
    }

    public void Capture(RunSaveData run)
    {
        if (run == null || State == null || !_ready) return;
        if (State.active && _sectors?.CurrentSector != null && _sectors.IsManagedSector(_sectors.CurrentSector))
            State.playerSectorCoord = _sectors.CurrentSector.Coord;
        if (State.layout.bossSpawned && State.layout.TryGetRoom(State.layout.bossRoomCoord, out var boss))
            State.bossDefeated = boss.isCleared;
        State.containers ??= new System.Collections.Generic.List<RaidLootContainerSaveData>();
        State.containers.Clear();
        foreach (var container in _containers)
        {
            var saved = container != null ? container.CaptureRaidState() : null;
            if (saved != null) State.containers.Add(saved);
        }
        run.raid = JsonUtility.FromJson<RaidRunSaveData>(JsonUtility.ToJson(State));
    }

    public void EndRaid()
    {
        if (State == null) return;
        State.active = false;
        Capture(SaveSystem.Active?.RunData);
    }
    private void OnDestroy() { if (Active == this) Active = null; }
    private void Play(AudioCueSO cue, Vector3 position)
    {
        RaidRuntimeSettings settings = Definition.RaidRuntime;
        if (cue != null && cue.HasPlayableClip && !cue.looping)
            settings.sfxChannel?.RaisePlayEvent(cue, settings.audioConfiguration, position);
    }
}
