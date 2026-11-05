using System;
using UnityEngine;

/// <summary>Owns one generated raid's power, clock and extraction state.</summary>
[DisallowMultipleComponent]
public sealed class RaidRuntime : MonoBehaviour
{
    public static RaidRuntime Active { get; private set; }
    public RaidRunSaveData State { get; private set; }
    public StageDefinitionSO Definition { get; private set; }
    public string LastExtractionFailure { get; private set; } = string.Empty;
    internal static bool CanPersistCheckpoint => Application.isPlaying;
    /// <summary>Presentation notification after a completed state change, not during inventory consumption.</summary>
    public event Action StateChanged;
    private SectorStateManager _sectors;
    private bool _transaction;
    private float _nextCheckpoint;
    private PlayerInventoryRuntime _player;
    private bool _ready;
    private readonly System.Collections.Generic.List<LootableObject> _containers = new();
    private readonly System.Collections.Generic.List<SectorRuntime> _boundSectors = new();

    public void Initialize(StageDefinitionSO definition, StageMapLayout layout, SectorStateManager sectors,
        RaidRunSaveData restored = null)
    {
        Active = this;
        Definition = definition;
        _sectors = sectors;
        _ready = false;
        _transaction = false;
        _containers.Clear();
        _boundSectors.Clear();
        State = restored ?? new RaidRunSaveData
        {
            active = true, layout = layout, playerSectorCoord = layout.startSectorCoord,
            timerEnabled = definition.RaidRuntime.durationSeconds > 0,
            remainingSeconds = Mathf.Max(0, definition.RaidRuntime.durationSeconds)
        };
        State.layout = layout;
        StageGasSpread.Initialize(State, definition.EncounterRules);
        // Retired raid/room deadlines must not restore an old overtime leak.
        State.timerEnabled = false;
        State.remainingSeconds = 0f;
        State.gasReleased = false;
        if (layout.rooms != null)
            foreach (var room in layout.rooms)
                if (room != null) room.gasLeakStacks = 0;
        if (!State.medicalStateInitialized)
        {
            State.medicalStateInitialized = true;
            State.suppressionRemainingSeconds = Mathf.Max(1f, definition.RaidRuntime.suppressionDurationSeconds);
            State.ampouleRemainingSeconds = State.ampouleRecoveryPerSecond = 0f;
        }
        _nextCheckpoint = 10f;
        NotifyStateChanged();
    }

    public void BindContainers(SectorRuntime sector)
    {
        _boundSectors.Add(sector);
        RuntimeContentCatalogSO catalog = Resources.Load<RuntimeContentCatalogSO>("RuntimeContentCatalog");
        foreach (LootableObject container in sector.GetComponentsInChildren<LootableObject>(true))
        {
            string key = "";
            for (Transform current = container.transform; current != null && current != sector.transform; current = current.parent)
                key = "/" + current.name + ":" + current.GetSiblingIndex() + key;
            key = sector.Coord.x + "," + sector.Coord.y + key;
            string legacyKey = key;
            var marker = container.GetComponentInParent<SectorContentRuntimeInstance>();
            if (marker != null && !string.IsNullOrEmpty(marker.SlotId))
            {
                // 여러 컨테이너를 가진 기존 프리팹도 구분하되, 방/자리 순서에는 의존하지 않습니다.
                string suffix = "";
                for (Transform child = container.transform; child != null && child != marker.transform; child = child.parent)
                    suffix = "/" + child.name + ":" + child.GetSiblingIndex() + suffix;
                key = sector.Coord.x + "," + sector.Coord.y + "/ContentSlot:" + marker.SlotId + suffix;
            }
            var saved = State.containers?.Find(entry => entry != null && entry.key == key) ??
                State.containers?.Find(entry => entry != null && entry.key == legacyKey);
            int seed = State.layout.stageSeed;
            string seedKey = saved != null && saved.key == legacyKey ? legacyKey : key;
            unchecked { foreach (char c in seedKey) seed = seed * 31 + c; }
            container.BindRaidState(key, seed, saved, catalog);
            _containers.Add(container);
        }
    }

    public void MarkReady()
    {
        _ready=true;
        GameProgressionRuntime.BeginRun();
        GameProgressionRuntime.EnterSector(_sectors?.CurrentSector);
    }
    public void Suspend() => _ready = false;
    public static bool IsHatchRevealedFor(StageMapLayout layout) => Active != null &&
        Active.State != null && Active.State.active && Active.State.layout == layout && Active.State.hatchRevealed;

    public bool IsInSector(SectorRuntime sector) => _ready && State != null && State.active && !State.extractionCommitted &&
        sector != null && _sectors != null && _sectors.CurrentSector == sector && _sectors.IsManagedSector(sector);

    public bool IsPlayerInRaid => _sectors != null && IsInSector(_sectors.CurrentSector);
    public bool HasActivatedPortals(SectorRuntime sector) => sector != null && State?.layout != null &&
        State.layout.TryGetRoom(sector.Coord, out var room) && room.portalsActivated;

    public bool TryActivatePortals(SectorRuntime sector)
    {
        if (!IsInSector(sector) || HasActivatedPortals(sector) ||
            !State.layout.TryGetRoom(sector.Coord, out var room)) return false;
        room.portalsActivated = true;
        // No encounter, wave, AI or infection-clock reset when powering a room.
        NotifyStateChanged();
        var save = CanPersistCheckpoint ? SaveSystem.Active : null;
        Capture(save?.RunData);
        if (save != null && !save.TrySaveRunDataToDisk())
            Debug.LogError("[Portal activation] 포탈은 개방됐지만 저장에 실패했습니다. 현재 세션의 개방 상태는 유지됩니다.", this);
        return true;
    }

    public static float CurrentOvertimeInfection => 0f;

    private void Update()
    {
#if UNITY_EDITOR
        if (CombatLabRuntime.IsIsolatedSession) return;
#endif
        if (!_ready || State == null || !State.active || _sectors == null || !IsInSector(_sectors.CurrentSector)) return;
        if (_player == null) _player = FindFirstObjectByType<PlayerInventoryRuntime>();
        Damageable player = _player != null ? _player.GetComponent<Damageable>() : null;
        if (player != null && player.IsDead) { EndRaid(); return; }
        State.playerSectorCoord = _sectors.CurrentSector.Coord;
        if (State.layout.bossSpawned && State.layout.TryGetRoom(State.layout.bossRoomCoord, out var boss))
            State.bossDefeated = boss.isCleared;
        TickClock(State, Time.deltaTime); // Pauses with game time; offline time does not elapse.
        if (StageGasSpread.Tick(State, Definition.EncounterRules, Time.deltaTime) > 0)
        {
            foreach (var sector in _boundSectors)
                if (sector != null && State.layout.TryGetRoom(sector.Coord,out var node))
                    sector.GetComponent<SectorRoomTraitRuntime>()?.ConfigureVirusRoom(node.virusRoom, Definition.EncounterRules.infectedRoomGasPressure);
            Capture(SaveSystem.Active?.RunData);
            SaveSystem.Active?.TrySaveRunDataToDisk();
            NotifyStateChanged();
        }
        _nextCheckpoint -= Time.deltaTime;
        if (_nextCheckpoint <= 0f)
        {
            _nextCheckpoint = 10f;
            SaveSystem.Active?.TrySaveRunDataToDisk();
        }
    }

    public static void TickClock(RaidRunSaveData state, float deltaSeconds)
    {
        if (state == null) return;
        state.timerEnabled = false;
        state.remainingSeconds = 0f;
        state.gasReleased = false;
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
            // SaveSystem is a ScriptableObject and can be Active even outside Play Mode.
            // Authoring/Preview Scene checks must not capture or save the user's real run.
            SaveSystem saveSystem = CanPersistCheckpoint ? SaveSystem.Active : null;
            Capture(saveSystem?.RunData);
            if (saveSystem != null && !saveSystem.TrySaveRunDataToDisk())
                Debug.LogError("[Raid] Power applied in memory, but saving failed. The last verified run remains recoverable.", this);
            if (Application.isPlaying) FindFirstObjectByType<SectorOccupancyManager>()?.RefreshRaidMarkers();
            Play(settings.generatorCue, inventory.transform.position);
            NotifyStateChanged();
            return true;
        }
        finally { _transaction = false; }
    }

    public bool TryExtract(SectorRuntime sector, RaidStationKind kind, PlayerInventoryRuntime inventory, Action<bool> onCompleted = null)
    {
        if (kind == RaidStationKind.Hatch && Definition != null && Definition.RaidRuntime.hatchAdvancesStage)
            return TryAdvanceThroughHatch(sector, inventory, onCompleted);
        LastExtractionFailure = string.Empty;
        if (sector == null) return RejectExtraction("탈출 섹터가 없습니다.");
        if (!sector.IsCleared) return RejectExtraction("탈출방의 적을 모두 처치하고 방을 클리어해야 탈출할 수 있습니다.");
        bool allowed = AllowsExtraction(State, sector.Coord, kind);
        if (!allowed)
        {
            if (Definition != null && inventory != null) Play(Definition.RaidRuntime.rejectedCue, inventory.transform.position);
            return RejectExtraction("현재 위치가 활성 탈출구가 아니거나 필요한 발전기 전력 조건을 충족하지 못했습니다.");
        }
        return TryCommitExtraction(sector, inventory, onCompleted);
    }

#if UNITY_EDITOR
    /// <summary>Bypasses room clear, exit location and power only; uses the real extraction transaction.</summary>
    public bool DebugTryExtractCurrentSector(PlayerInventoryRuntime inventory, Action<bool> onCompleted = null)
    {
        LastExtractionFailure = string.Empty;
        if (!Application.isPlaying) return RejectExtraction("강제 탈출은 Play Mode에서만 가능합니다.");
        if (CombatLabRuntime.IsIsolatedSession) return RejectExtraction("전투 실험실에서는 레이드 강제 탈출을 사용할 수 없습니다.");
        if (_sectors == null) return RejectExtraction("레이드의 SectorStateManager가 없습니다.");
        return TryCommitExtraction(_sectors.CurrentSector, inventory, onCompleted);
    }
#endif

    public bool TryAdvanceThroughHatch(SectorRuntime sector, PlayerInventoryRuntime inventory, Action<bool> onCompleted = null)
        => TryAdvanceThroughHatchTo(sector, inventory, null, onCompleted);

    private bool TryAdvanceThroughHatchTo(SectorRuntime sector, PlayerInventoryRuntime inventory, int? destination, Action<bool> onCompleted)
    {
        LastExtractionFailure = string.Empty;
        if (!_ready || State == null || !State.active || State.extractionCommitted || _transaction)
            return RejectExtraction("레이드가 준비되지 않았거나 이동/탈출 처리가 진행 중입니다.");
        if (Definition == null || !Definition.RaidRuntime.hatchAdvancesStage)
            return RejectExtraction("현재 해치는 다음 스테이지 이동 모드가 아닙니다.");
        if (inventory == null || !inventory.gameObject.activeInHierarchy || inventory != _player || inventory.GetComponent<Damageable>()?.IsDead == true)
            return RejectExtraction("현재 레이드의 살아 있는 플레이어가 필요합니다.");
        if (sector == null || _sectors == null || _sectors.CurrentSector != sector || !IsInSector(sector) || !sector.IsCleared ||
            !AllowsExtraction(State, sector.Coord, RaidStationKind.Hatch))
            return RejectExtraction("현재 해치 방 클리어와 발전기 전력 조건이 필요합니다.");
        if (Definition.RaidRuntime.hatchRequiredQuest != null && !QuestRuntime.IsCompleted(Definition.RaidRuntime.hatchRequiredQuest))
            return RejectExtraction("해치 이용 퀘스트를 먼저 완료해야 합니다.");
        var progression = FindFirstObjectByType<StageProgressionManager>();
        if (progression == null) return RejectExtraction("StageProgressionManager가 없습니다.");
        if (!destination.HasValue)
        {
            var choices = progression.GetUnlockedHatchDestinations();
            if (choices.Length > 1)
                return HideoutStageSelectPanel.TryOpenHatchChoice(choices, selected =>
                    this != null && TryAdvanceThroughHatchTo(sector, inventory, selected, onCompleted));
        }
        _transaction = true;
        bool accepted = progression.TryAdvanceInRun(destination, out string reason, success =>
        {
            _transaction = false;
            onCompleted?.Invoke(success);
        });
        if (!accepted) { _transaction = false; return RejectExtraction(reason); }
        return true;
    }

    private bool TryCommitExtraction(SectorRuntime sector, PlayerInventoryRuntime inventory, Action<bool> onCompleted = null)
    {
        LastExtractionFailure = string.Empty;
        if (_transaction) return RejectExtraction("이미 다른 레이드 트랜잭션/탈출 요청이 진행 중입니다.");
        if (inventory == null || !inventory.gameObject.activeInHierarchy) return RejectExtraction("활성 플레이어 인벤토리가 없습니다.");
        if (inventory.GetComponent<Damageable>()?.IsDead == true) return RejectExtraction("사망한 플레이어는 탈출할 수 없습니다.");
        if (!_ready) return RejectExtraction("레이드 준비가 완료되지 않았습니다.");
        if (State == null || !State.active) return RejectExtraction("진행 중인 레이드 상태가 없습니다.");
        if (State.extractionCommitted) return RejectExtraction("탈출이 이미 예약/처리되었습니다.");
        if (!IsInSector(sector)) return RejectExtraction("현재 섹터가 이 레이드에 속하지 않거나 현재 섹터 참조가 일치하지 않습니다.");
        StageProgressionManager progression = FindFirstObjectByType<StageProgressionManager>();
        if (progression == null) return RejectExtraction("StageProgressionManager가 없습니다.");
        _transaction = true;
        State.extractionCommitted = true;
        NotifyStateChanged();
        try
        {
            Vector3 extractionPosition = inventory.transform.position;
            if (!progression.TryExtractToHomeFromNormalExit(sector,
                    success =>
                    {
                        if (!success) LastExtractionFailure = string.IsNullOrWhiteSpace(progression.LastExtractionFailure)
                            ? "HOME 복귀가 완료되지 않았습니다. 레이드 상태를 유지합니다." : progression.LastExtractionFailure;
                        CompleteExtraction(success, extractionPosition);
                        onCompleted?.Invoke(success);
                    }))
            {
                State.extractionCommitted = false; _transaction = false; NotifyStateChanged();
                return RejectExtraction(string.IsNullOrWhiteSpace(progression.LastExtractionFailure)
                    ? "HOME 복귀 요청이 거절되었습니다." : progression.LastExtractionFailure);
            }
            return true;
        }
        catch (Exception error)
        {
            LastExtractionFailure = "탈출 처리 예외: " + error.Message;
            if (State.active) State.extractionCommitted = false;
            _transaction = false; NotifyStateChanged(); throw;
        }
    }

    private bool RejectExtraction(string reason)
    {
        LastExtractionFailure = reason;
        Debug.LogWarning("[RaidRuntime][Extraction] " + reason, this);
        return false;
    }

    private void CompleteExtraction(bool success, Vector3 extractionPosition)
    {
        if (!_transaction) return;
        if (!success) State.extractionCommitted = false;
        _transaction = false;
        NotifyStateChanged();
        if (success) Play(Definition.RaidRuntime.extractionCue, extractionPosition);
    }

    public bool TryExtractThroughPortal(SectorPortal portal, PlayerInventoryRuntime inventory, Action<bool> onCompleted = null)
    {
        if (portal == null || !portal.IsAvailable || !portal.IsExtraction ||
            portal.OwnerSector == null || State?.layout == null ||
            !State.layout.IsExtractionPortal(portal.OwnerSector.Coord, portal.Direction)) return false;
        return TryExtract(portal.OwnerSector, RaidStationKind.NormalExtraction, inventory, onCompleted);
    }

    public static bool AllowsExtraction(RaidRunSaveData state, Vector2Int coord, RaidStationKind kind)
    {
        if (state == null || !state.active || state.extractionCommitted || state.layout == null) return false;
        return kind switch
        {
            RaidStationKind.StartReturn => state.layout.generationVersion < 3 && coord == state.layout.startSectorCoord,
            RaidStationKind.NormalExtraction => state.layout.IsExtractionSector(coord),
            RaidStationKind.Hatch => coord == state.layout.hatchRoomCoord && state.generatorPowered,
            _ => false // Special extraction is an intentionally non-operational authoring scaffold.
        };
    }

    public bool TryReturnThroughShortcut(PlayerInventoryRuntime inventory)
    {
        if (_transaction || inventory == null || State?.layout == null || _sectors == null ||
            inventory.GetComponent<Damageable>()?.IsDead == true ||
            !State.generatorPowered || State.layout.generationVersion != 3 || !IsInSector(_sectors.CurrentSector) ||
            _sectors.CurrentSector.Coord != State.layout.generatorRoomCoord ||
            !_sectors.TryGetSector(State.layout.hatchRoomCoord, out var hatch)) return false;
        var portals = FindFirstObjectByType<SectorPortalManager>();
        if (portals == null || !portals.TryMoveThroughRaidShortcut(_sectors.CurrentSector, hatch, inventory.transform)) return false;
        State.playerSectorCoord = hatch.Coord;
        Capture(SaveSystem.Active?.RunData);
        SaveSystem.Active?.TrySaveRunDataToDisk();
        return true;
    }

    public bool TryAuthorizeVaultEntry(SectorRuntime destination, PlayerInventoryRuntime inventory)
    {
        // Authorization never auto-spends keys. Locked rooms require explicit UI confirmation.
        return !IsVaultEntryLocked(destination);
    }

    public LootKeyTier RequiredVaultEntryKeyTier => Definition.RaidRuntime.vaultEntryKeyTier;

    public bool IsVaultEntryLocked(SectorRuntime destination)
    {
        return State?.layout?.generationVersion == 3 && destination != null &&
            State.layout.TryGetRoom(destination.Coord, out var room) && room.raidRole == RaidRoomRole.Vault &&
            !room.hasInternalCombatLock &&
            !room.vaultEntryUnlocked;
    }

    public bool TryUnlockVaultEntry(SectorRuntime destination, PlayerInventoryRuntime inventory,
        int keySlot, KeyItemSO expectedKey, out string message)
    {
        message = string.Empty;
        if (!IsVaultEntryLocked(destination)) { message = "이미 열린 금고방입니다."; return true; }
        if (_transaction || !_ready || State == null || !State.active || inventory == null ||
            inventory.GetComponent<Damageable>()?.IsDead == true)
        { message = "현재 레이드/플레이어 상태에서는 금고를 열 수 없습니다."; return false; }
        State.layout.TryGetRoom(destination.Coord, out var room);
        _transaction = true;
        room.vaultEntryUnlocked = true;
        try
        {
            if (!inventory.TryConsumeKeyToolSlotUse(keySlot, RequiredVaultEntryKeyTier, expectedKey, out message))
            { room.vaultEntryUnlocked = false; return false; }
            Capture(SaveSystem.Active?.RunData);
            if (SaveSystem.Active != null && !SaveSystem.Active.TrySaveRunDataToDisk())
                Debug.LogError("[Vault entry] 금고는 열렸지만 저장에 실패했습니다. 현재 세션에서는 열쇠를 다시 소모하지 않습니다.", this);
            message = "열쇠 1회 사용 · 금고방을 열었습니다.";
            return true;
        }
        finally { _transaction = false; }
    }

    public bool TryUnlockInternalCombatArea(SectorLockedCombatArea area, SectorLockPanel panel,
        PlayerInventoryRuntime inventory, int keySlot, KeyItemSO expectedKey, out string message)
    {
        message = "패널에서 다시 상호작용하세요. 잠금 해제 시 섹터 전투가 시작됩니다.";
        if (_transaction || !_ready || area == null || area.Room == null ||
            !IsInSector(area.Sector) || !area.CanUsePanel(panel, inventory) ||
            !State.layout.TryGetRoom(area.Sector.Coord, out var room) || !ReferenceEquals(room, area.Room)) return false;
        _transaction = true;
        // Inventory callbacks can checkpoint synchronously. Save the unlock flag
        // before consuming, exactly as the existing vault-entry transaction does.
        room.internalCombatUnlocked = true;
        try
        {
            if (!inventory.TryConsumeKeyToolSlotUse(keySlot, room.internalCombatKeyTier, expectedKey, out message))
            { room.internalCombatUnlocked = false; return false; }
            room.vaultEntryUnlocked = true;
            area.BeginOpening();
            NotifyStateChanged(); // Refresh portal availability immediately, not next interaction.
            Capture(SaveSystem.Active?.RunData);
            if (SaveSystem.Active != null && !SaveSystem.Active.TrySaveRunDataToDisk())
                Debug.LogError("[Internal combat lock] 잠금은 해제됐지만 저장에 실패했습니다. 현재 세션에서는 열쇠를 다시 소모하지 않습니다.", area);
            message = "열쇠 1회 사용 · 전체 차단벽 해제 · 섹터 전투 시작";
            return true;
        }
        finally { _transaction = false; }
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
        foreach (var sector in _boundSectors)
            if (sector != null && State.layout.TryGetRoom(sector.Coord, out var room))
            {
                var traits = sector.GetComponent<SectorRoomTraitRuntime>();
                if (traits != null) room.gasLeakStacks = traits.TimerFailureLeakStacks;
            }
        foreach (var container in _containers)
        {
            var saved = container != null ? container.CaptureRaidState() : null;
            if (saved != null) State.containers.Add(saved);
        }
        run.raid = JsonUtility.FromJson<RaidRunSaveData>(JsonUtility.ToJson(State));
        // A loading request is not a successful extraction. An interrupted load must resume the raid.
        if (State.active && _transaction && State.extractionCommitted) run.raid.extractionCommitted = false;
    }

    public void EndRaid()
    {
        if (State == null) return;
        State.active = false;
        Capture(SaveSystem.Active?.RunData);
        NotifyStateChanged();
    }
    private void OnDestroy()
    {
        StateChanged = null;
        if (Active == this) Active = null;
    }
    private void NotifyStateChanged()
    {
        if (StateChanged == null) return;
        foreach (Action listener in StateChanged.GetInvocationList())
            try { listener(); }
            catch (Exception error) { Debug.LogException(error, this); }
    }
    private void Play(AudioCueSO cue, Vector3 position)
    {
        RaidRuntimeSettings settings = Definition.RaidRuntime;
        if (cue != null && cue.HasPlayableClip && !cue.looping)
            settings.sfxChannel?.RaisePlayEvent(cue, settings.audioConfiguration, position);
    }
}
