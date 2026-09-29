/*
 * Role hierarchy: Gameplay > World > Sectors > StageMap
 * Primary responsibility: Manages StageSectorInstantiator stage, sector, navigation, spawn, or spatial progression state.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

// Assets/Scripts/GameScene/Sector/StageMap/StageSectorInstantiator.cs
using System.Collections.Generic;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Serialization;

[DisallowMultipleComponent]
public class StageSectorInstantiator : MonoBehaviour
{
    [Header("Seed")]
    [Tooltip("If enabled, a new run seed is generated when this component awakes.")]
    [SerializeField] private bool _randomizeRunSeedOnAwake = true;

    [Tooltip("Run seed used to generate deterministic stage maps when randomize is disabled.")]
    [SerializeField] private int _runSeed = 1;

    [Header("Stage Coord")]
    [Tooltip("Base StartSector anchor, normally (-1, 0). OddSquareRaid rotates this hub and the map to a seeded random corner. Legacy layouts use this coordinate directly.")]
    [SerializeField] private Vector2Int _startSectorCoord = new Vector2Int(-1, 0);

    [Header("Placement")]
    [Tooltip("Parent transform for generated sectors. Uses this transform when empty.")]
    [SerializeField] private Transform _generatedRoot;

    [Tooltip("World position for logical room (0,0). Uses this transform when empty.")]
    [SerializeField] private Transform _gridOrigin;

    [Tooltip("World spacing between generated logical rooms. X is world X spacing, Y is world Z spacing.")]
    [SerializeField] private Vector2 _roomSpacingXZ = new Vector2(40f, 40f);

    [Header("Runtime NavMesh")]
    [Tooltip("Rebuilds one continuous NavMesh for each generated sector after all nested floor tiles are instantiated. Runtime builds use Physics Colliders so imported visual meshes do not require Read/Write access.")]
    [SerializeField] private bool _rebuildGeneratedSectorNavMesh = true;

    [Tooltip("Only enabled, non-trigger Colliders on these layers are used by the generated sector NavMesh. Keep this limited to SectorFloor and SectorStructure so decorative MeshColliders do not require Read/Write access in a player build.")]
    [SerializeField] private LayerMask _runtimeNavMeshLayerMask =
        (1 << 7) | (1 << 9);

    [Tooltip("Logs an error when a generated sector has no enabled, non-trigger Collider that can be used to build its runtime NavMesh.")]
    [SerializeField] private bool _validateRuntimeNavMeshSources = true;

    [Header("Refs")]
    [Tooltip("Receives generated sectors and owns opened/cleared/current sector state.")]
    [SerializeField] private SectorStateManager _sectorStateManager;

    [Tooltip("Rebuilds portal links after generated sectors are registered.")]
    [SerializeField] private SectorPortalManager _sectorPortalManager;

    [Tooltip("Optional player anchor. If the player is parented under a generated sector, it is detached before sectors are rebuilt.")]
    [SerializeField] private TransformAnchor _playerTransformAnchor;

    [Header("Hideout Services")]
    [Tooltip("Single Home-shop catalog, economy rules, and progression unlock tree.")]
    [SerializeField] private ShopConfigSO _homeShopConfig;

    [Tooltip("Persistent save data that owns inheritance warehouse contents.")]
    [SerializeField] private SaveSystem _saveSystem;

    [Tooltip("Optional explicit GUID resolver catalog for inherited items. Add any item that is not otherwise loaded at startup.")]
    [SerializeField] private ItemSO[] _inheritanceWarehouseItemCatalog;

    [SerializeField] private EnemyLootOpenRequestEventChannelSO _inheritanceWarehouseOpenRequestChannel;
    [SerializeField] private UIOverlayRequestEventChannelSO _inheritanceWarehouseOverlayRequestChannel;

    [SerializeField, Min(1)] private int _inheritanceWarehouseCapacity = 24;

    private readonly List<SectorRuntime> _generatedSectors = new();
    private SectorRuntime _hideoutSector;

    public StageMapLayout CurrentLayout { get; private set; }
    public int RunSeed => _runSeed;
    public SectorRuntime HideoutSector => _hideoutSector;
    public bool HasAuthoredHomeSector => TryResolveAuthoredHomeSector(out _);

    // Purpose: Caches the component references required before runtime initialization.
    private void Awake()
    {
        if (_randomizeRunSeedOnAwake)
            _runSeed = Random.Range(int.MinValue, int.MaxValue);

        if (_sectorStateManager == null)
            _sectorStateManager = FindAnyObjectByType<SectorStateManager>();

        if (_sectorPortalManager == null)
            _sectorPortalManager = FindAnyObjectByType<SectorPortalManager>();
    }

    // Purpose: Builds the stage from the current authored data.
    public bool BuildStage(StageDefinitionSO stage)
    {
        return BuildStage(stage, _sectorStateManager);
    }

    // Purpose: Builds the stage from the current authored data.
    public bool BuildStage(
        StageDefinitionSO stage,
        SectorStateManager sectorStateManager)
    {
        return BuildStage(
            stage,
            sectorStateManager,
            consecutiveNoHitStageCount: 0);
    }

    // Purpose: Builds the stage from the current authored data.
    public bool BuildStage(
        StageDefinitionSO stage,
        SectorStateManager sectorStateManager,
        int consecutiveNoHitStageCount,
        RaidRunSaveData restoredRaid = null)
    {
        if (stage == null)
        {
            Debug.LogError("[StageSectorInstantiator] StageDefinitionSO is missing.", this);
            return false;
        }

        if (sectorStateManager == null)
        {
            Debug.LogError("[StageSectorInstantiator] SectorStateManager is missing.", this);
            return false;
        }

        bool startOnly = stage.UseStartSectorOnly || stage.RoomGridSize <= 0;
        int gridSize = startOnly ? 0 : Mathf.Max(1, stage.RoomGridSize);

        _sectorStateManager = sectorStateManager;

        int stageSeed = StageMapGenerator.BuildStageSeed(_runSeed, stage.StageIndex);
        if (!startOnly && stage.MapPolicy == StageMapPolicy.OddSquareRaid)
        {
            if (restoredRaid != null)
            {
                if (!restoredRaid.TryValidate(out string restoreError) || restoredRaid.layout.stageIndex != stage.StageIndex)
                { Debug.LogError($"[Raid] Cannot restore raid: {restoreError}", this); return false; }
                restoredRaid = JsonUtility.FromJson<RaidRunSaveData>(JsonUtility.ToJson(restoredRaid));
                CurrentLayout = restoredRaid.layout;
                _runSeed = CurrentLayout.runSeed;
            }
            else
            {
                if (_randomizeRunSeedOnAwake) _runSeed = Random.Range(int.MinValue, int.MaxValue);
                if (!RaidMapGenerator.TryGenerate(_runSeed, stage.StageIndex, gridSize, _startSectorCoord,
                        stage.RaidMap, out StageMapLayout raidLayout, out string raidError))
                { Debug.LogError($"[Raid] Cannot generate raid: {raidError}", this); return false; }
                CurrentLayout = raidLayout;
            }
        }
        else if (!startOnly && stage.HasEnabledBranchTopology)
        {
            string topologyError = "No valid positive-weight topology could be selected.";
            StageMapLayout topologyLayout = null;
            bool topologyGenerated =
                stage.TryPickBranchTopology(stageSeed, out StageTopologySO topology) &&
                StageMapGenerator.TryGenerateBranchTopology(
                    _runSeed,
                    stage.StageIndex,
                    _startSectorCoord,
                    stage.GoalRoomType,
                    topology,
                    out topologyLayout,
                    out topologyError);

            if (!topologyGenerated)
            {
                Debug.LogError(
                    $"[StageSectorInstantiator] Could not generate Branch Topology. " +
                    $"stage={stage.StageIndex}, definition={stage.name}, reason={topologyError}",
                    stage);
                ClearGeneratedSectors();
                CurrentLayout = null;
                return false;
            }

            CurrentLayout = topologyLayout;
        }
        else
        {
            CurrentLayout = StageMapGenerator.GenerateFullGrid(
                _runSeed,
                stage.StageIndex,
                gridSize,
                _startSectorCoord,
                stage.GoalRoomType,
                // Dedicated Treasure rooms are WIP legacy content. Normal Sector
                // loot slots own Rare Cache placement, so full-grid generation
                // must keep every non-boss room classified as Normal here.
                StageTreasureRoomGenerationSettings.Disabled,
                StageShopRoomGenerationSettings.Disabled);
        }

        ClearGeneratedSectors();
        RaidRuntime raidRuntime = GetComponent<RaidRuntime>();
        if (CurrentLayout.usesRaidLayout)
        {
            if (raidRuntime == null) raidRuntime = gameObject.AddComponent<RaidRuntime>();
            raidRuntime.Initialize(stage, CurrentLayout, sectorStateManager, restoredRaid);
        }
        else raidRuntime?.EndRaid();

        Vector3 roomStep = ResolveRoomStep();

        for (int i = 0; i < CurrentLayout.rooms.Count; i++)
        {
            StageRoomNode node = CurrentLayout.rooms[i];

            if (node == null || node.roomType == StageRoomType.Empty)
                continue;

            StageBattleEncounterPresetSO encounterPreset = null;
            SectorPresetSO sectorPreset;

            if (node.roomType == StageRoomType.Boss)
            {
                if (!stage.TryPickBossBattle(
                        CurrentLayout.stageSeed,
                        node.coord,
                        out encounterPreset,
                        out SectorBossBattlePresetSO bossSectorPreset))
                {
                    Debug.LogError(
                        $"[StageSectorInstantiator] No valid Boss battle entry is registered. " +
                        $"stage={stage.StageIndex}, coord={node.coord}, definition={stage.name}",
                        stage);
                    ClearGeneratedSectors();
                    CurrentLayout = null;
                    return false;
                }

                sectorPreset = bossSectorPreset;
            }
            else if (!stage.TryPickSectorPreset(
                         CurrentLayout.stageSeed,
                         node.coord,
                         node.roomType,
                         out sectorPreset))
            {
                Debug.LogError(
                    $"[StageSectorInstantiator] No valid SectorPresetSO is registered. stage={stage.StageIndex}, roomType={node.roomType}, coord={node.coord}, definition={stage.name}",
                    stage);
                ClearGeneratedSectors();
                CurrentLayout = null;
                return false;
            }

            RaidRoomPresetBinding raidBinding = CurrentLayout.usesRaidLayout
                ? stage.RaidRuntime.FindRoom(node.raidRole) : null;
            if (raidBinding != null && node.roomType == StageRoomType.Normal &&
                raidBinding.TryPickSectorPreset(stageSeed, node.coord, out SectorPresetSO rolePreset))
                sectorPreset = rolePreset;

            bool usesEnemyEncounter = !node.isCleared && (
                node.roomType == StageRoomType.Boss ||
                (node.roomType == StageRoomType.Normal &&
                 NormalSectorPolicySO.ResolveUsesEnemyEncounter(sectorPreset.NormalSectorPolicy)));

            if (node.roomType == StageRoomType.Normal &&
                usesEnemyEncounter &&
                raidBinding?.encounter == null && !stage.TryPickNormalSectorEncounter(
                    CurrentLayout.stageSeed,
                    node.coord,
                    out encounterPreset))
            {
                Debug.LogError(
                    $"[StageSectorInstantiator] No valid Normal Battle encounter is registered. " +
                    $"stage={stage.StageIndex}, coord={node.coord}, definition={stage.name}",
                    stage);
                ClearGeneratedSectors();
                CurrentLayout = null;
                return false;
            }

            if (usesEnemyEncounter && raidBinding?.encounter != null && node.roomType == StageRoomType.Normal)
                encounterPreset = raidBinding.encounter;

            SectorRuntime prefab = sectorPreset.SectorPrefab;

            bool isStartSector =
                node.roomType == StageRoomType.Start ||
                node.coord == CurrentLayout.startSectorCoord;

            SectorRuntime sector = Instantiate(
                prefab,
                ResolveWorldPosition(node.coord, roomStep),
                prefab.transform.rotation,
                _generatedRoot != null ? _generatedRoot : transform);

            sector.name = $"Stage_{stage.StageIndex}_{node.roomType}_{node.raidRole}_{node.coord.x}_{node.coord.y}";
            sector.SetRuntimeInfo(node.coord, node.isOpened, isStartSector);
            if (node.roomType == StageRoomType.Normal)
                sector.ConfigureNormalSectorPolicy(sectorPreset.NormalSectorPolicy);

            SectorRoomTraitRuntime roomTraitRuntime =
                sector.GetComponent<SectorRoomTraitRuntime>();

            if (roomTraitRuntime == null)
                roomTraitRuntime = sector.gameObject.AddComponent<SectorRoomTraitRuntime>();

            roomTraitRuntime.Configure(
                stage.RoomTraitTable,
                node.roomType,
                CurrentLayout.stageSeed,
                node.coord,
                stage.EnableBiochemicalGas,
                stage.BiochemicalGas);
            if (CurrentLayout.usesRaidLayout && node.raidRole == RaidRoomRole.Armory)
                roomTraitRuntime.ConfigureAuthoredRoomTrait(SectorRoomTraitType.Armed);
            sector.SetCleared(isStartSector || node.isCleared);
            if (CurrentLayout.usesRaidLayout)
                RaidStationFactory.Attach(raidRuntime, sector, node, stage.RaidRuntime);

            _generatedSectors.Add(sector);

            if (usesEnemyEncounter)
            {
                SectorEnemySpawner enemySpawner =
                    sector.GetComponentInChildren<SectorEnemySpawner>(true);

                if (enemySpawner == null ||
                    !enemySpawner.ConfigureEncounter(encounterPreset, stage.StageIndex))
                {
                    Debug.LogError(
                        $"[StageSectorInstantiator] Generated battle sector cannot receive its encounter. " +
                        $"stage={stage.StageIndex}, roomType={node.roomType}, coord={node.coord}, sector={sector.name}",
                        sector);
                    ClearGeneratedSectors();
                    CurrentLayout = null;
                    return false;
                }
            }

            if (node.roomType == StageRoomType.Normal &&
                (raidBinding?.content != null || sectorPreset.ContentProfile != null))
            {
                SectorContentSlotSpawner.Populate(
                    sector,
                    raidBinding?.content != null ? raidBinding.content : sectorPreset.ContentProfile,
                    CurrentLayout.stageSeed);
            }

            // WIP legacy: no StageRoomType.Treasure node is generated. Existing
            if (raidBinding?.lootObject != null)
                RaidRoomContentSpawner.Populate(sector, raidBinding.lootObject);
            if (CurrentLayout.usesRaidLayout) raidRuntime.BindContainers(sector);

            // SectorTreasure components remain serialized for manual migration
            // into a LootableObject / Rare Cache source.
        }

        RebuildGeneratedSectorNavMeshes();

        _sectorStateManager.RegisterGeneratedStageMap(CurrentLayout, _generatedSectors);

        // Sector prefabs enable as they are instantiated, before the full map
        // exists in SectorStateManager. Resolve every authored encounter only
        // after registration so the StageDefinition-selected wave cannot be
        // replaced by an early prefab lifecycle refresh.
        for (int i = 0; i < _generatedSectors.Count; i++)
        {
            SectorEnemySpawner enemySpawner =
                _generatedSectors[i] != null
                    ? _generatedSectors[i].GetComponentInChildren<SectorEnemySpawner>(true)
                    : null;

            if (_generatedSectors[i] != null && _generatedSectors[i].IsCleared)
                enemySpawner?.CancelEncounterForSectorSkip();
            else enemySpawner?.ResolveConfiguredEncounterAfterStageMapRegistered();
        }

        if (_sectorPortalManager != null)
            _sectorPortalManager.RebuildPortalLinks(resetStartSectorConsumed: true);

        if (CurrentLayout.usesRaidLayout) raidRuntime.MarkReady();
        return true;
    }

    /// <summary>
    /// Resolves the HomeSector placed directly in HomeScene. The Hideout is not
    /// generated and is never allowed to fall back to a StageStartSector.
    /// </summary>
    public bool EnsureHideout()
    {
        if (!TryResolveAuthoredHomeSector(out SectorRuntime authoredHomeSector))
        {
            Debug.LogError(
                "[StageSectorInstantiator] Authored HomeSector is unavailable. " +
                "Load HomeScene additively and place exactly one HomeSector prefab in that scene. " +
                "Generated StageStartSector presets are not used as a fallback.",
                this);
            return false;
        }

        _hideoutSector = authoredHomeSector;
        _hideoutSector.SetRuntimeInfo(Vector2Int.zero, opened: true, isStartSector: true);
        _hideoutSector.SetCleared(true);

        HideoutRuntime hideout = _hideoutSector.GetComponent<HideoutRuntime>();
        if (hideout == null)
            hideout = _hideoutSector.gameObject.AddComponent<HideoutRuntime>();

        hideout.Initialize(_hideoutSector);

        HomeSector homeSector = _hideoutSector.GetComponent<HomeSector>();
        if (homeSector == null)
        {
            Debug.LogError(
                "[StageSectorInstantiator] HomeSector prefab is missing HomeSector. " +
                "Add HomeSector to the dedicated prefab. " +
                "Stage entry is handled by an authored HideoutStageSelectStation.",
                _hideoutSector);
        }
        else
        {
            homeSector.Initialize(_hideoutSector);
            homeSector.ResetHomeResidents();
        }

        ConfigureAuthoredHomeSectorServices(_hideoutSector);

        // Keep normal generated-sector portals synchronized after the Hideout exists.
        // Home stage entry is handled by an authored HideoutStageSelectStation.
        if (_sectorPortalManager != null)
            _sectorPortalManager.RebuildPortalLinks(resetStartSectorConsumed: false);
        return true;
    }

    // Purpose: Attempts to resolve authored home sector and reports whether the request succeeded.
    private bool TryResolveAuthoredHomeSector(out SectorRuntime homeSector)
    {
        if (_hideoutSector != null &&
            _hideoutSector.gameObject.scene.IsValid() &&
            _hideoutSector.gameObject.scene.isLoaded)
        {
            homeSector = _hideoutSector;
            return true;
        }

        HomeSector[] homeSectors = FindObjectsByType<HomeSector>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        for (int i = 0; i < homeSectors.Length; i++)
        {
            HomeSector authoredHome = homeSectors[i];
            if (authoredHome == null ||
                !authoredHome.gameObject.scene.IsValid() ||
                !authoredHome.gameObject.scene.isLoaded)
            {
                continue;
            }

            SectorRuntime candidate = authoredHome.SectorRuntime;
            if (candidate == null)
                candidate = authoredHome.GetComponent<SectorRuntime>();

            if (candidate == null)
            {
                Debug.LogError(
                    "[StageSectorInstantiator] Authored HomeSector is missing SectorRuntime.",
                    authoredHome);
                continue;
            }

            _hideoutSector = candidate;
            homeSector = candidate;
            return true;
        }

        homeSector = null;
        return false;
    }

    // Purpose: Configures this component''s authored home sector services.
    private void ConfigureAuthoredHomeSectorServices(
        SectorRuntime homeSector)
    {
        if (homeSector == null)
            return;

        ConfigureAuthoredHomeShop(homeSector);
        ConfigureAuthoredInheritanceWarehouse(homeSector);
    }

    // Purpose: Configures this component''s authored home shop.
    private void ConfigureAuthoredHomeShop(
        SectorRuntime homeSector)
    {
        SectorShop shop = homeSector.GetComponentInChildren<SectorShop>(true);

        if (shop == null)
        {
            Debug.LogError(
                "[StageSectorInstantiator] Authored HomeShop is missing. " +
                "Place a SectorShop under HomeSector/HomeServices, then assign its Home ShopConfigSO.",
                this);
            return;
        }

        if (_homeShopConfig == null)
        {
            Debug.LogError(
                "[StageSectorInstantiator] Home ShopConfigSO is missing. " +
                "Stage-specific shop tables are intentionally not used as a fallback.",
                this);
        }

        if (!shop.ConfigureForHome(
                homeSector,
                _homeShopConfig,
                _saveSystem))
        {
            Debug.LogError(
                "[StageSectorInstantiator] Authored HomeShop configuration failed.",
                shop);
        }
    }

    // Purpose: Configures this component''s authored inheritance warehouse.
    private void ConfigureAuthoredInheritanceWarehouse(SectorRuntime homeSector)
    {
        InheritanceWarehouseRuntime warehouse =
            homeSector.GetComponentInChildren<InheritanceWarehouseRuntime>(true);

        if (warehouse == null)
        {
            Debug.LogError(
                "[StageSectorInstantiator] Authored InheritanceWarehouse is missing. " +
                "Place an InheritanceWarehouseRuntime under HomeSector/HomeServices and assign its channels.",
                this);
            return;
        }

        if (_saveSystem == null)
        {
            Debug.LogError(
                "[StageSectorInstantiator] SaveSystem is missing. Authored inheritance warehouse cannot be configured.",
                this);
            return;
        }

        warehouse.ConfigurePersistence(
            _saveSystem,
            _inheritanceWarehouseItemCatalog,
            _inheritanceWarehouseCapacity,
            _inheritanceWarehouseOpenRequestChannel,
            _inheritanceWarehouseOverlayRequestChannel);
    }

    // Purpose: Repairs or rebuilds generated sector nav meshes from the current source data.
    private void RebuildGeneratedSectorNavMeshes()
    {
        if (!_rebuildGeneratedSectorNavMesh || !Application.isPlaying)
            return;

        for (int i = 0; i < _generatedSectors.Count; i++)
        {
            SectorRuntime sector = _generatedSectors[i];

            if (sector == null)
                continue;

            NavMeshSurface[] nestedSurfaces =
                sector.GetComponentsInChildren<NavMeshSurface>(true);

            if (nestedSurfaces == null || nestedSurfaces.Length == 0)
                continue;

            NavMeshSurface template = ResolveNavMeshTemplate(nestedSurfaces);
            int agentTypeId = template.agentTypeID;
            LayerMask layerMask = _runtimeNavMeshLayerMask;
            int defaultArea = template.defaultArea;
            bool ignoreAgents = template.ignoreNavMeshAgent;
            bool ignoreObstacles = template.ignoreNavMeshObstacle;
            bool overrideTileSize = template.overrideTileSize;
            int tileSize = template.tileSize;
            bool overrideVoxelSize = template.overrideVoxelSize;
            float voxelSize = template.voxelSize;
            float minRegionArea = template.minRegionArea;
            bool buildHeightMesh = template.buildHeightMesh;

            for (int j = 0; j < nestedSurfaces.Length; j++)
            {
                NavMeshSurface nestedSurface = nestedSurfaces[j];

                if (nestedSurface == null)
                    continue;

                nestedSurface.RemoveData();
                nestedSurface.enabled = false;
            }

            if (_validateRuntimeNavMeshSources &&
                !HasRuntimeNavMeshColliderSource(sector.transform, layerMask))
            {
                Debug.LogError(
                    $"[StageSectorInstantiator] Runtime NavMesh build skipped because sector has no enabled, non-trigger Collider in the configured layer mask. sector={sector.name}, coord={sector.Coord}, layerMask={layerMask.value}",
                    sector);
                continue;
            }

            NavMeshSurface unifiedSurface = sector.GetComponent<NavMeshSurface>();

            if (unifiedSurface == null)
                unifiedSurface = sector.gameObject.AddComponent<NavMeshSurface>();

            unifiedSurface.agentTypeID = agentTypeId;
            unifiedSurface.collectObjects = CollectObjects.Children;
            unifiedSurface.layerMask = layerMask;
            unifiedSurface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
            unifiedSurface.defaultArea = defaultArea;
            unifiedSurface.ignoreNavMeshAgent = ignoreAgents;
            unifiedSurface.ignoreNavMeshObstacle = ignoreObstacles;
            unifiedSurface.overrideTileSize = overrideTileSize;
            unifiedSurface.tileSize = tileSize;
            unifiedSurface.overrideVoxelSize = overrideVoxelSize;
            unifiedSurface.voxelSize = voxelSize;
            unifiedSurface.minRegionArea = minRegionArea;
            unifiedSurface.buildHeightMesh = buildHeightMesh;
            unifiedSurface.enabled = true;
            unifiedSurface.BuildNavMesh();
        }
    }

    // Purpose: Resolves the nav mesh template needed by this component.
    private static NavMeshSurface ResolveNavMeshTemplate(
        NavMeshSurface[] nestedSurfaces)
    {
        for (int i = 0; i < nestedSurfaces.Length; i++)
        {
            NavMeshSurface surface = nestedSurfaces[i];

            if (surface != null && surface.isActiveAndEnabled)
                return surface;
        }

        return nestedSurfaces[0];
    }

    // Purpose: Checks whether this component has runtime nav mesh collider source.
    private static bool HasRuntimeNavMeshColliderSource(
        Transform sectorRoot,
        LayerMask layerMask)
    {
        Collider[] colliders = sectorRoot.GetComponentsInChildren<Collider>(true);

        for (int i = 0; i < colliders.Length; i++)
        {
            Collider collider = colliders[i];

            if (collider == null ||
                !collider.enabled ||
                collider.isTrigger ||
                !collider.gameObject.activeInHierarchy)
            {
                continue;
            }

            int colliderLayerMask = 1 << collider.gameObject.layer;

            if ((layerMask.value & colliderLayerMask) != 0)
                return true;
        }

        return false;
    }

    // Purpose: Clears the current generated sectors state.
    public void ClearGeneratedSectors()
    {
        GetComponent<RaidRuntime>()?.Suspend();
        DetachPlayerFromGeneratedSectors();

        for (int i = _generatedSectors.Count - 1; i >= 0; i--)
        {
            SectorRuntime sector = _generatedSectors[i];

            if (sector == null)
                continue;

            if (Application.isPlaying)
                Destroy(sector.gameObject);
            else
                DestroyImmediate(sector.gameObject);
        }

        _generatedSectors.Clear();
    }

    // Purpose: Performs the detach player from generated sectors workflow for this component.
    private void DetachPlayerFromGeneratedSectors()
    {
        if (_playerTransformAnchor == null ||
            !_playerTransformAnchor.isSet ||
            _playerTransformAnchor.Value == null)
        {
            return;
        }

        Transform player = _playerTransformAnchor.Value;

        for (int i = 0; i < _generatedSectors.Count; i++)
        {
            SectorRuntime sector = _generatedSectors[i];

            if (sector == null || !player.IsChildOf(sector.transform))
                continue;

            player.SetParent(null, worldPositionStays: true);
            return;
        }
    }

    // Purpose: Resolves the room step needed by this component.
    private Vector3 ResolveRoomStep()
    {
        float x = Mathf.Max(0.01f, _roomSpacingXZ.x);
        float z = Mathf.Max(0.01f, _roomSpacingXZ.y);

        return new Vector3(x, 0f, z);
    }

    // Purpose: Resolves the world position needed by this component.
    private Vector3 ResolveWorldPosition(Vector2Int coord, Vector3 roomStep)
    {
        Vector3 origin = _gridOrigin != null
            ? _gridOrigin.position
            : transform.position;

        return origin + new Vector3(
            coord.x * roomStep.x,
            0f,
            coord.y * roomStep.z);
    }
}
