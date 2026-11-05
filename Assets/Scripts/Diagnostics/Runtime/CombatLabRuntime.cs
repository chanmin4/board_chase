#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using VSplatter.StateMachine.Debugging;

[Serializable]
public sealed class CombatLabSpawn
{
    public EnemyShooterConfigSO Config;
    // Sector-local floor position: moving the additive room never changes the plan.
    public Vector3 Position;
    public float Yaw;
}

/// <summary>Editor-only additive sector using the existing HOME player and RUN services.</summary>
public sealed class CombatLabRuntime : MonoBehaviour
{
    public const string IsolationKey = "BoardChase.CombatLab.Isolated";
    public static CombatLabRuntime Active { get; private set; }
    public static bool IsIsolatedSession => Application.isPlaying &&
        (Active != null || UnityEditor.SessionState.GetBool(IsolationKey, false));
    public SectorPresetSO Preset;
    public StageDefinitionSO Stage;
    public CameraManager CameraRig;
    public SectorRuntime Sector;
    public Vector3 PlayerPosition;
    public float PlayerYaw;
    public int Seed = 12345;
    public bool StrongRoom;
    public List<CombatLabSpawn> Spawns = new();
    [NonSerialized] public PlayerCharacter Player;
    [NonSerialized] public readonly List<Enemy> Enemies = new();
    [NonSerialized] public string Status;
    [NonSerialized] public bool Ready;
    public bool IsEnding { get; private set; }
    private SectorRuntime _home;
    private SectorStateManager _manager;
    private Transform _actors, _projectiles, _previousProjectileRoot, _previousParent;
    private Scene _previousScene, _previousActiveScene;
    private Quaternion _homeRotation;
    private PlayerRunStateSaveData _snapshot;
    private RuntimeContentCatalogSO _catalog;
    private string _runBefore;
    private bool _oldTrace, _oldPhysicsTrace, _deathPending, _ownsPlayer;
    private UnityEngine.Random.State _randomBefore;
    private readonly Dictionary<Enemy, CombatLabSpawn> _enemyPlans = new();

    private T PlayerComponent<T>() where T : Component => EntityComponentResolver.Resolve<T>(Player);
    private T RequirePlayerComponent<T>() where T : Component
    {
        var component = PlayerComponent<T>();
        if (component == null) throw new InvalidOperationException("플레이어에 필요한 컴포넌트가 없습니다: " + typeof(T).Name);
        return component;
    }

    public void Begin()
    {
        if (Ready) return;
        if (!Application.isPlaying || Active != null || Preset == null || Stage == null || Sector == null)
            throw new InvalidOperationException("기존 게임을 HOME에서 Play한 뒤 유효한 장소/스테이지를 선택하세요.");
        var progression = FindFirstObjectByType<StageProgressionManager>();
        _manager = FindFirstObjectByType<SectorStateManager>();
        Player = FindFirstObjectByType<PlayerCharacter>();
        _home = progression != null ? progression.InitialHideoutSector : null;
        CameraRig = FindFirstObjectByType<CameraManager>();
        if (Player == null || CameraRig == null || _home == null || _manager == null || _manager.CurrentSector != _home)
            throw new InvalidOperationException("HOME의 실제 플레이어·카메라·매니저가 준비되어 있어야 합니다. 레이드 중에는 시작하지 않습니다.");
        if (PlayerComponent<Damageable>() == null || PlayerComponent<Damageable>().IsDead)
            throw new InvalidOperationException("살아 있는 플레이어가 필요합니다.");
        _snapshot = CapturePlayer();
        _catalog = Resources.Load<RuntimeContentCatalogSO>("RuntimeContentCatalog");
        ValidateSnapshot(); // Fail before moving or changing the live player.
        _runBefore = SaveSystem.Active != null ? JsonUtility.ToJson(SaveSystem.Active.RunData) : null;
        _previousParent = Player.transform.parent;
        _previousScene = Player.gameObject.scene;
        _previousActiveScene = SceneManager.GetActiveScene();
        _homeRotation = _home.PlayerStartPoint.rotation;
        var equipment = PlayerComponent<ShooterEquipmentController>();
        _previousProjectileRoot = equipment != null ? equipment.ProjectilesRoot : null;
        _oldTrace = HfsmExecutionTrace.Enabled; _oldPhysicsTrace = CombatPhysicsTrace.Enabled;
        _randomBefore = UnityEngine.Random.state;
        Active = this;
        UnityEditor.SessionState.SetBool(IsolationKey, true);
        try
        {
            SectorAppearance.ApplyStageDefault(Sector, Stage.SectorTheme);
            BuildNavigation(Sector);
            Sector.SetRuntimeInfo(new Vector2Int(-1000, -1000), true, false);
            Sector.ConfigureNormalSectorPolicy(Preset.NormalSectorPolicy);
            var traits = Sector.GetComponent<SectorRoomTraitRuntime>();
            if (traits == null) traits = Sector.gameObject.AddComponent<SectorRoomTraitRuntime>();
            traits.Configure(Stage.RaidAnomalyTable, Preset.RoomType, Seed, Sector.Coord,
                Stage.EnableBiochemicalGas, Stage.BiochemicalGas, Stage.StageIndex, true, Stage.AnomalyGasBaseline, false);
            _actors = new GameObject("Lab actors").transform; _actors.SetParent(Sector.transform, false);
            _projectiles = new GameObject("Lab projectiles").transform; _projectiles.SetParent(transform, false);
            HfsmExecutionTrace.Clear(); HfsmExecutionTrace.Enabled = true;
            CombatPhysicsTrace.Clear();
            UnityEngine.Random.InitState(Seed);
            _ownsPlayer = true;
            MovePlayer(Sector, SnapToNavigation(Sector.transform.TransformPoint(PlayerPosition), PlayerComponent<CharacterController>()),
                Sector.transform.rotation * Quaternion.Euler(0, PlayerYaw, 0));
            equipment?.SetProjectilesRoot(_projectiles);
            _manager.SetExternalCurrentSector(Sector, false);
            CameraRig.SetFollowTarget(Player.transform, true);
            SceneManager.SetActiveScene(gameObject.scene);
            Ready = true;
            foreach (var spawn in Spawns) SpawnEnemy(spawn);
            Status = "기존 플레이 환경에서 실험 중 · 적 추가/제거 가능";
        }
        catch
        {
            ReturnPlayer(); ReleaseIsolation();
            throw;
        }
    }

    public Enemy SpawnEnemy(CombatLabSpawn spawn)
    {
        if (!Ready || IsEnding || spawn?.Config == null || spawn.Config.EnemyPrefab == null)
            throw new InvalidOperationException("실험 중인 방과 유효한 적 Config가 필요합니다.");
        var agent = EntityComponentResolver.Resolve<NavMeshAgent>(spawn.Config.EnemyPrefab);
        var filter = new NavMeshQueryFilter { agentTypeID = agent != null ? agent.agentTypeID : 0,
            areaMask = agent != null ? agent.areaMask : NavMesh.AllAreas };
        if (!NavMesh.SamplePosition(Sector.transform.TransformPoint(spawn.Position), out var hit, 2f, filter))
            throw new InvalidOperationException(spawn.Config.name + ": 해당 위치에 적 NavMesh가 없습니다.");
        var enemy = SectorEnemySpawner.SpawnConfiguredEnemy(spawn.Config, Sector, _projectiles, _actors,
            hit.position, Sector.transform.rotation * Quaternion.Euler(0, spawn.Yaw, 0), Stage.StageIndex);
        if (enemy == null) throw new InvalidOperationException("적 생성 실패: 해당 위치를 플레이어·적·시체가 차지하고 있습니다. 빈 좌표를 지정하세요.");
        enemy.name = "Lab " + spawn.Config.name + " " + (Enemies.Count + 1);
        Enemies.Add(enemy);
        _enemyPlans[enemy] = spawn;
        return enemy;
    }

    public Enemy SummonEnemy(EnemyShooterConfigSO config, Vector3 localPosition, float yaw)
    {
        var spawn = new CombatLabSpawn { Config = config, Position = localPosition, Yaw = yaw };
        var enemy = SpawnEnemy(spawn);
        Spawns.Add(spawn); // Runtime-only composition for restart; no authoring spawn-point objects.
        return enemy;
    }

    public void RemoveEnemy(Enemy enemy)
    {
        if (enemy == null) return;
        if (_enemyPlans.TryGetValue(enemy, out var plan)) Spawns.Remove(plan);
        _enemyPlans.Remove(enemy);
        Enemies.Remove(enemy);
        enemy.gameObject.SetActive(false);
        Destroy(enemy.gameObject); // Removal is not a kill/reward event.
    }

    public void ClearEnemies()
    {
        Enemies.Clear();
        _enemyPlans.Clear();
        if (_actors != null)
            foreach (Transform actor in _actors) { actor.gameObject.SetActive(false); Destroy(actor.gameObject); }
        if (_projectiles != null)
            foreach (Transform shot in _projectiles) { shot.gameObject.SetActive(false); Destroy(shot.gameObject); }
    }

    public void RestartCombat()
    {
        if (!Ready || IsEnding) return;
        foreach (var spawn in Spawns)
            if (spawn?.Config == null || spawn.Config.EnemyPrefab == null)
                throw new InvalidOperationException("재시작 전에 스폰 계획의 적 Config를 지정하세요.");
        ClearEnemies(); RestorePlayer();
        MovePlayer(Sector, SnapToNavigation(Sector.transform.TransformPoint(PlayerPosition), PlayerComponent<CharacterController>()),
            Sector.transform.rotation * Quaternion.Euler(0, PlayerYaw, 0));
        CameraRig.SetFollowTarget(Player.transform, true);
        HfsmExecutionTrace.Clear(); CombatPhysicsTrace.Clear();
        UnityEngine.Random.InitState(Seed);
        foreach (var spawn in Spawns) SpawnEnemy(spawn);
        Status = "전투 재시작 · 진입 시 장비/체력 복구";
    }

    // Do not run the production corpse/GameOver/save chain for the borrowed player.
    public static bool HandlePlayerDeath(Damageable damageable)
    {
        if (Active == null || !Active.Ready || Active.Player == null ||
            EntityComponentResolver.Resolve<PlayerCharacter>(damageable) != Active.Player) return false;
        Active._deathPending = true;
        Active.Status = "플레이어 사망 판정 · 전투를 자동 재시작합니다 (실제 소지품 손실 없음)";
        return true;
    }
    private void Update()
    {
        if (!_deathPending || IsEnding) return;
        _deathPending = false;
        try { RestartCombat(); }
        catch (Exception error) { Status = error.Message; Debug.LogException(error, this); }
    }

    public void End()
    {
        if (IsEnding) return;
        // Return/restore must succeed before unloading the scene containing the player.
        ReturnPlayer();
        IsEnding = true; Ready = false;
        Sector.gameObject.SetActive(false);
        StartCoroutine(UnloadAfterReturn());
    }
    private IEnumerator UnloadAfterReturn()
    {
        var ownedScene = gameObject.scene;
        // Keep isolation active through deferred Destroy/OnDisable callbacks.
        yield return null;
        ReleaseIsolation();
        SceneManager.UnloadSceneAsync(ownedScene);
    }
    private void ReturnPlayer()
    {
        ClearEnemies();
        if (!_ownsPlayer) return;
        if (Player == null || _home == null || !_home.gameObject.scene.isLoaded)
            throw new InvalidOperationException("HOME 복귀 대상이 없어 실험 씬을 제거하지 않았습니다.");
        RestorePlayer();
        Vector3 point = _home.PlayerStartPoint.position;
        var controller = PlayerComponent<CharacterController>();
        if (controller != null) point.y += Mathf.Max(.2f, controller.height * .5f - controller.center.y + controller.skinWidth);
        MovePlayer(_home, point, _homeRotation);
        if (_previousParent != null && _previousParent.gameObject.scene == _home.gameObject.scene)
            Player.transform.SetParent(_previousParent, true);
        else if (_previousScene.IsValid() && _previousScene.isLoaded && _previousParent == null)
            Player.transform.SetParent(null, true);
        PlayerComponent<ShooterEquipmentController>()?.SetProjectilesRoot(_previousProjectileRoot);
        _manager.SetExternalCurrentSector(_home, false);
        CameraRig.SetFollowTarget(Player.transform, true);
        SceneManager.SetActiveScene(_previousActiveScene.IsValid() && _previousActiveScene.isLoaded ? _previousActiveScene : _home.gameObject.scene);
        if (_runBefore != null && SaveSystem.Active != null)
            JsonUtility.FromJsonOverwrite(_runBefore, SaveSystem.Active.RunData);
        _ownsPlayer = false;
    }
    private void ReleaseIsolation()
    {
        if (Active != this) return;
        HfsmExecutionTrace.Enabled = _oldTrace; CombatPhysicsTrace.Enabled = _oldPhysicsTrace;
        UnityEngine.Random.state = _randomBefore;
        Active = null;
        UnityEditor.SessionState.EraseBool(IsolationKey);
    }
    private void MovePlayer(SectorRuntime destination, Vector3 position, Quaternion rotation)
    {
        var controller = PlayerComponent<CharacterController>();
        bool wasEnabled = controller != null && controller.enabled;
        if (controller != null) controller.enabled = false;
        Player.transform.SetParent(null, true);
        SceneManager.MoveGameObjectToScene(Player.gameObject, destination.gameObject.scene);
        Player.transform.SetParent(destination.transform, true);
        Player.transform.SetPositionAndRotation(position, rotation);
        Player.movementInput = Player.movementVector = Vector3.zero;
        Player.shootInput = Player.DashInput = false;
        Physics.SyncTransforms();
        if (controller != null) controller.enabled = wasEnabled;
    }
    private PlayerRunStateSaveData CapturePlayer()
    {
        var result = new PlayerRunStateSaveData();
        RequirePlayerComponent<PlayerInventoryRuntime>().CaptureRunState(result);
        RequirePlayerComponent<PlayerBulletLoadoutRuntime>().CaptureRunState(result);
        RequirePlayerComponent<PlayerPassiveInventoryRuntime>().CaptureRunState(result);
        RequirePlayerComponent<EntityStatusEffectRuntime>().CaptureRunState(result);
        PlayerComponent<PlayerQuickSlotRuntime>()?.CaptureUsableRunSlots(result.usableQuickSlots);
        ShopRunRuntime.EnsureRuntime().CaptureRunState(result);
        result.currentHealth = RequirePlayerComponent<Damageable>().CurrentHealth;
        result.currentInfection = RequirePlayerComponent<PlayerShooterInfection>().CurrentInfection;
        result.hasSnapshot = true;
        return result;
    }
    private void ValidateSnapshot()
    {
        if (_catalog == null || _catalog.ItemCount == 0)
            throw new InvalidOperationException("플레이어 복구용 RuntimeContentCatalog가 필요합니다.");
        if (!RequirePlayerComponent<PlayerInventoryRuntime>().CanRestoreCombatLabSnapshot(_snapshot, _catalog, out var validation))
            throw new InvalidOperationException("진입 장비 복구 사전 검사 실패: " + validation);
        var quickSlots = PlayerComponent<PlayerQuickSlotRuntime>();
        if (_snapshot.usableQuickSlots.Count > 0 && (quickSlots == null || !quickSlots.CanRestoreUsableRunSlots(_snapshot.usableQuickSlots, _catalog)))
            throw new InvalidOperationException("퀵슬롯 복구 사전 검사 실패");
        // Validate every snapshot content identifier before the live inventory is touched.
        void Item(string guid)
        {
            if (!string.IsNullOrEmpty(guid) && !_catalog.TryResolveItem(guid, out _))
                throw new InvalidOperationException("복구 카탈로그에 없는 아이템: " + guid);
        }
        foreach (var entry in _snapshot.inventorySlots) Item(entry.itemGuid);
        foreach (var entry in _snapshot.equippedWeapons) Item(entry.weaponGuid);
        foreach (var entry in _snapshot.keyToolSlots) Item(entry.keyGuid);
        foreach (var entry in _snapshot.weaponMagazines)
        {
            Item(entry.bulletGuid);
            if (!_catalog.TryResolveItem(entry.bulletGuid, out var bullet) || bullet is not BulletSO)
                throw new InvalidOperationException("복구할 수 없는 탄약");
        }
        foreach (string guid in _snapshot.equippedPassiveGuids)
        {
            Item(guid);
            if (!string.IsNullOrEmpty(guid) && (!_catalog.TryResolveItem(guid, out var passive) || passive is not PassiveItemSO))
                throw new InvalidOperationException("복구할 수 없는 패시브");
        }
        foreach (var entry in _snapshot.usableQuickSlots) Item(entry.itemGuid);
        Item(_snapshot.equippedArmorGuid); Item(_snapshot.equippedBackpackGuid); Item(_snapshot.equippedGasMaskGuid);
        foreach (var effect in _snapshot.statusEffects)
            if (!_catalog.TryResolveStatusEffect(effect.statusEffectId, out _))
                throw new InvalidOperationException("복구 카탈로그에 없는 상태 효과: " + effect.statusEffectId);
    }
    private void RestorePlayer()
    {
        // Restore methods normalize their input; keep the entry snapshot immutable for repeated restarts.
        var source = JsonUtility.FromJson<PlayerRunStateSaveData>(JsonUtility.ToJson(_snapshot));
        bool Check(bool result, string message) { if (!result) throw new InvalidOperationException("플레이어 복구 실패: " + message); return true; }
        Check(RequirePlayerComponent<PlayerInventoryRuntime>().TryRestoreRunState(source, _catalog, out var inventory), inventory);
        Check(RequirePlayerComponent<PlayerBulletLoadoutRuntime>().TryRestoreRunState(source, _catalog, out var bullets), bullets);
        Check(RequirePlayerComponent<PlayerPassiveInventoryRuntime>().TryRestoreRunState(source, _catalog, out var passives), passives);
        Check(RequirePlayerComponent<EntityStatusEffectRuntime>().TryRestoreRunState(source, _catalog, out var effects), effects);
        PlayerComponent<PlayerQuickSlotRuntime>()?.RestoreUsableRunSlots(source.usableQuickSlots, _catalog);
        RequirePlayerComponent<Damageable>().SetCurrentHealth(source.currentHealth);
        RequirePlayerComponent<PlayerShooterInfection>().RestoreCurrentInfection(source.currentInfection);
        ShopRunRuntime.EnsureRuntime().RestoreRunState(source, _catalog);
    }
    public static Vector3 SnapToNavigation(Vector3 position, CharacterController controller = null)
    {
        if (!NavMesh.SamplePosition(position, out var hit, 2f, NavMesh.AllAreas))
            throw new InvalidOperationException("지정 위치가 바닥 NavMesh에서 너무 멉니다.");
        float offset = controller != null ? Mathf.Max(.05f, controller.height * .5f - controller.center.y + controller.skinWidth) : 0;
        return hit.position + Vector3.up * offset;
    }
    public static void BuildNavigation(SectorRuntime sector)
    {
        var templates = sector.GetComponentsInChildren<NavMeshSurface>(true);
        var template = templates.Length > 0 ? templates[0] : null;
        int type = template != null ? template.agentTypeID : 0;
        int area = template != null ? template.defaultArea : 0;
        bool tileOverride = template != null && template.overrideTileSize;
        int tile = template != null ? template.tileSize : 256;
        bool voxelOverride = template != null && template.overrideVoxelSize;
        float voxel = template != null ? template.voxelSize : .1f;
        foreach (var surface in templates) { surface.RemoveData(); surface.enabled = false; }
        var unified = sector.GetComponent<NavMeshSurface>();
        if (unified == null) unified = sector.gameObject.AddComponent<NavMeshSurface>();
        unified.agentTypeID = type; unified.defaultArea = area;
        unified.collectObjects = CollectObjects.Children;
        unified.layerMask = LayerMask.GetMask("SectorFloor", "SectorStructure");
        unified.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
        unified.ignoreNavMeshAgent = unified.ignoreNavMeshObstacle = true;
        unified.overrideTileSize = tileOverride; unified.tileSize = tile;
        unified.overrideVoxelSize = voxelOverride; unified.voxelSize = voxel;
        unified.enabled = true; unified.BuildNavMesh();
    }
}
#endif
