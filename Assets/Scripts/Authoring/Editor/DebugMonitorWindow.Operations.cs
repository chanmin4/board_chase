/*
 * Role hierarchy: Authoring > Editor > Debug Monitor Operations
 * Primary responsibility: Provides stage, entity item, shop, and inheritance warehouse debug operations.
 * Execution boundary: Editor only (Assembly-CSharp-Editor)
 */

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public sealed partial class DebugMonitorWindow
{
    private enum MonitorWorkspaceTab
    {
        Characters,
        StageAndStartHub,
        Quests,
        CombatLab,
        Colliders
    }

    private MonitorWorkspaceTab _monitorWorkspaceTab;
    private Vector2 _operationsScroll;
    private Vector2 _debugItemScroll;
    private readonly List<ItemSO> _debugItems = new();
    private bool _debugItemsLoaded;
    private string _debugItemSearch = string.Empty;
    private ItemSO _selectedDebugItem;
    private int _runtimeTargetRowId = -1;
    private int _runtimeItemAmount = 1;
    private int _runtimeEquipmentSlot;
    private int _stageSelectionIndex = 1;
    private int _stageUnlockIndex = 1;
    private bool _stageInputsInitialized;
    private int _stageManagerInstanceId;
    private int _roomTraitDebugSectorId;
    private SectorRoomTraitType _roomTraitDebugSelection = SectorRoomTraitType.Normal;
    private SectorShop _debugShop;
    private InheritanceWarehouseRuntime _debugWarehouse;
    private int _debugShopPrice = 10;
    private int _debugShopStock = 1;
    private int _debugShopBundle = 1;
    private string _operationsStatus = string.Empty;
    private MessageType _operationsStatusType = MessageType.None;
    private string _raidExtractionStatus = string.Empty;
    private MessageType _raidExtractionStatusType = MessageType.None;

    // Purpose: Draws the monitor workspace authoring UI.
    private void DrawMonitorWorkspace()
    {
        _monitorWorkspaceTab = (MonitorWorkspaceTab)GUILayout.Toolbar(
            (int)_monitorWorkspaceTab,
            new[] { "캐릭터", "Stage & Start Hub", "Quests", "전투 실험실", "콜라이더" });

        EditorGUILayout.Space(4f);

        if (_monitorWorkspaceTab == MonitorWorkspaceTab.CombatLab) { DrawCombatLab(); return; }
        if (_monitorWorkspaceTab == MonitorWorkspaceTab.Colliders) { DrawColliderControls(); return; }
        if (!EditorApplication.isPlaying)
        {
            EditorGUILayout.HelpBox("런타임 정보는 Play Mode에서 표시됩니다. 전투 실험실과 콜라이더는 여기서 바로 설정할 수 있습니다.", MessageType.Info);
            return;
        }

        switch (_monitorWorkspaceTab)
        {
            case MonitorWorkspaceTab.Characters:
                DrawTable();
                DrawSelectedDetails();
                break;

            case MonitorWorkspaceTab.StageAndStartHub:
                _operationsScroll = EditorGUILayout.BeginScrollView(_operationsScroll);
                DrawStageAndStartHubOperations();
                EditorGUILayout.EndScrollView();
                break;

            case MonitorWorkspaceTab.Quests:
                _operationsScroll = EditorGUILayout.BeginScrollView(_operationsScroll);
                DrawQuestOperations();
                EditorGUILayout.EndScrollView();
                break;
        }
    }

    // Purpose: Draws the stage and start hub operations authoring UI.
    private void DrawStageAndStartHubOperations()
    {
        EditorGUILayout.LabelField("Stage Progression", EditorStyles.boldLabel);

        if (_stageProgressionManager == null)
            _stageProgressionManager = UnityEngine.Object.FindAnyObjectByType<StageProgressionManager>();

        StageProgressionManager previousStageManager = _stageProgressionManager;
        _stageProgressionManager = (StageProgressionManager)EditorGUILayout.ObjectField(
            "Stage Manager",
            _stageProgressionManager,
            typeof(StageProgressionManager),
            true);

        int stageManagerInstanceId = _stageProgressionManager != null
            ? _stageProgressionManager.GetInstanceID()
            : 0;

        if (previousStageManager != _stageProgressionManager ||
            _stageManagerInstanceId != stageManagerInstanceId)
        {
            _stageInputsInitialized = false;
            _stageManagerInstanceId = stageManagerInstanceId;
        }

        if (_stageProgressionManager != null)
        {
            IReadOnlyList<StageDefinitionSO> stages = _stageProgressionManager.DebugStages;
            int firstStageIndex = int.MaxValue;
            int lastStageIndex = int.MinValue;
            for (int i = 0; i < stages.Count; i++)
            {
                if (stages[i] != null)
                {
                    firstStageIndex = Mathf.Min(firstStageIndex, stages[i].StageIndex);
                    lastStageIndex = Mathf.Max(lastStageIndex, stages[i].StageIndex);
                }
            }

            if (firstStageIndex == int.MaxValue)
            {
                EditorGUILayout.HelpBox("No stage definitions are configured.", MessageType.Warning);
                return;
            }

            if (!_stageInputsInitialized)
            {
                _stageSelectionIndex = Mathf.Clamp(
                    _stageProgressionManager.CurrentStageIndex,
                    firstStageIndex,
                    lastStageIndex);
                _stageUnlockIndex = Mathf.Clamp(
                    _stageProgressionManager.HighestUnlockedStageIndex,
                    firstStageIndex,
                    lastStageIndex);
                _stageInputsInitialized = true;
            }

            EditorGUILayout.LabelField("Current", _stageProgressionManager.DebugCurrentStageLabel);
            int highestUnlockedStageNumber = _stageProgressionManager.HighestUnlockedStageIndex;
            EditorGUILayout.LabelField(
                "Highest Unlocked",
                $"Stage {highestUnlockedStageNumber} (Stages {firstStageIndex}-{highestUnlockedStageNumber} available)");

            DrawStageDefinitionList(stages);

            _stageUnlockIndex = EditorGUILayout.IntSlider(
                "Unlock Through Stage",
                Mathf.Clamp(_stageUnlockIndex, firstStageIndex, lastStageIndex),
                firstStageIndex,
                lastStageIndex);
            EditorGUILayout.HelpBox(
                "This uses the visible Stage 1-5 numbering. Selecting Stage 3 makes Stages 1, 2, and 3 " +
                "available. It writes the contiguous unlock boundary to the Easy Save 3 profile immediately; choosing a " +
                "lower stage re-locks every later stage.",
                MessageType.Warning);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Reset Unlocks To Stage 1"))
                {
                    bool succeeded = _stageProgressionManager.DebugTrySetHighestUnlockedStage(firstStageIndex);
                    if (succeeded)
                        _stageUnlockIndex = firstStageIndex;

                    SetOperationsStatus(
                        succeeded
                            ? "Reset stage unlocks: Stage 1 is available and Stages 2-5 are locked."
                            : "Could not reset stage unlocks to Stage 1.",
                        succeeded ? MessageType.Info : MessageType.Error);
                }

                if (GUILayout.Button("Apply Unlock State") &&
                    EditorUtility.DisplayDialog(
                        "Persist Stage Unlock Boundary?",
                        $"Make Stages 1-{_stageUnlockIndex} available? " +
                        "This persists immediately and re-locks every later stage.",
                        "Set Boundary",
                        "Cancel"))
                {
                    bool succeeded = _stageProgressionManager.DebugTrySetHighestUnlockedStage(_stageUnlockIndex);
                    SetOperationsStatus(
                        succeeded
                            ? $"Stages 1-{_stageUnlockIndex} are now available."
                            : $"Could not make Stages 1-{_stageUnlockIndex} available.",
                        succeeded ? MessageType.Info : MessageType.Error);
                }

                using (new EditorGUI.DisabledScope(lastStageIndex <= firstStageIndex))
                {
                    if (GUILayout.Button("Unlock All") &&
                        EditorUtility.DisplayDialog(
                            "Unlock Every Stage?",
                            "This writes the maximum stage unlock boundary to the Easy Save 3 profile immediately.",
                            "Unlock All",
                            "Cancel"))
                    {
                        bool succeeded = _stageProgressionManager.DebugTrySetHighestUnlockedStage(lastStageIndex);
                        if (succeeded)
                            _stageUnlockIndex = lastStageIndex;

                        SetOperationsStatus(
                            succeeded ? "Every configured stage is unlocked." : "Could not unlock every stage.",
                            succeeded ? MessageType.Info : MessageType.Error);
                    }
                }
            }

            _stageSelectionIndex = EditorGUILayout.IntSlider(
                "Stage To Enter",
                Mathf.Clamp(_stageSelectionIndex, firstStageIndex, lastStageIndex),
                firstStageIndex,
                lastStageIndex);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Enter Unlocked Stage (Loading)"))
                {
                    bool succeeded = _stageProgressionManager.TryEnterUnlockedStage(_stageSelectionIndex);
                    SetOperationsStatus(
                        succeeded
                            ? $"Loading unlocked stage {_stageSelectionIndex}."
                            : $"Could not load stage {_stageSelectionIndex}. It may still be locked.",
                        succeeded ? MessageType.Info : MessageType.Error);
                    RefreshRows();
                }

                if (GUILayout.Button("Force Build Stage"))
                {
                    bool confirmed = EditorUtility.DisplayDialog(
                        "Force Build Stage?",
                        "This bypasses the unlock check and the normal loading screen. " +
                        "Use only for debug testing.",
                        "Force Build",
                        "Cancel");

                    if (confirmed)
                    {
                        bool succeeded = _stageProgressionManager.DebugTryBeginStage(_stageSelectionIndex);
                        SetOperationsStatus(
                            succeeded
                                ? $"Force-built stage {_stageSelectionIndex}."
                                : $"Could not force-build stage {_stageSelectionIndex}.",
                            succeeded ? MessageType.Info : MessageType.Error);
                        RefreshRows();
                    }
                }

                using (new EditorGUI.DisabledScope(!_stageProgressionManager.DebugHasNextStage))
                {
                    if (GUILayout.Button("Force Next Stage"))
                    {
                        bool succeeded = _stageProgressionManager.DebugTryBeginNextStage();
                        SetOperationsStatus(
                            succeeded ? "Entered the next stage." : "Could not enter the next stage.",
                            succeeded ? MessageType.Info : MessageType.Error);
                        RefreshRows();
                    }
                }
            }
        }
        else
        {
            EditorGUILayout.HelpBox(
                "StageProgressionManager was not found. Enter PlayMode after loading the gameplay scene.",
                MessageType.Warning);
        }

        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("Start Hub", EditorStyles.boldLabel);

        if (_sectorStateManager == null)
            _sectorStateManager = UnityEngine.Object.FindAnyObjectByType<SectorStateManager>();

        _sectorStateManager = (SectorStateManager)EditorGUILayout.ObjectField(
            "Sector Manager",
            _sectorStateManager,
            typeof(SectorStateManager),
            true);

        SectorRuntime startSector = _sectorStateManager != null
            ? _sectorStateManager.StartSector
            : null;
        EditorGUILayout.ObjectField("Generated Start", startSector, typeof(SectorRuntime), true);

        using (new EditorGUI.DisabledScope(_sectorStateManager == null || startSector == null))
        {
            if (GUILayout.Button("Return Player To Start Hub"))
                ReturnDebugPlayerToStartHub(startSector);
        }

        RaidRuntime raid = RaidRuntime.Active;
        DrawProgressionFoundationControls(raid);
        using (new EditorGUI.DisabledScope(!Application.isPlaying || CombatLabRuntime.IsIsolatedSession ||
                   raid == null))
        {
            if (GUILayout.Button("레이드 종료 · 탈출 처리 (HOME 복귀)"))
                ExtractDebugRaidToHome(raid);
        }
        if (!string.IsNullOrWhiteSpace(_raidExtractionStatus))
            EditorGUILayout.HelpBox(_raidExtractionStatus, _raidExtractionStatusType);
        if (!Application.isPlaying) EditorGUILayout.HelpBox("강제 탈출은 Play Mode에서만 가능합니다.", MessageType.Info);
        else if (CombatLabRuntime.IsIsolatedSession) EditorGUILayout.HelpBox("전투 실험실에서는 강제 탈출할 수 없습니다.", MessageType.Info);
        else if (raid == null) EditorGUILayout.HelpBox("현재 활성 레이드가 없습니다. 레이드 진입 후 사용하세요.", MessageType.Warning);
        EditorGUILayout.LabelField("방 클리어·탈출 위치·전력 조건을 생략합니다. 실제 탈출처럼 소지품을 유지하고 HOME 복귀·레이드 종료·저장 처리합니다. 스테이지 클리어/해금은 지급하지 않습니다.",
            EditorStyles.wordWrappedMiniLabel);

        EditorGUILayout.Space(8f);
        DrawStageMapDebug();

        EditorGUILayout.Space(8f);
        DrawCurrentRoomTraitDebugControls();

        EditorGUILayout.Space(12f);
        EditorGUILayout.LabelField("Global Item Utilities", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Character item grants now live on the Characters tab. These controls are kept here for Shop and Warehouse testing.",
            MessageType.None);
        DrawItemCatalog();
        EditorGUILayout.Space(8f);
        DrawShopOperations();
        EditorGUILayout.Space(8f);
        DrawWarehouseOperations();
        DrawOperationsStatus();
    }

    // Purpose: Draws the current room trait debug controls authoring UI.
    private void DrawCurrentRoomTraitDebugControls()
    {
        EditorGUILayout.LabelField("현재 섹터 이상징후 — 실행 중에만 변경", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "현재 섹터에만 시험 적용합니다. SO 기본값·스테이지 시드·저장된 레이드 배치는 변경하지 않습니다. " +
            "적 강화 / 적 피해 증가는 현재 섹터에 생성된 적에도 즉시 반영됩니다.",
            MessageType.None);

        if (_sectorStateManager == null)
            _sectorStateManager = UnityEngine.Object.FindAnyObjectByType<SectorStateManager>();

        SectorRuntime sector = _sectorStateManager != null
            ? _sectorStateManager.CurrentSector
            : null;
        SectorRoomTraitRuntime traits = sector != null
            ? sector.GetComponent<SectorRoomTraitRuntime>()
            : null;

        if (traits == null)
        {
            EditorGUILayout.HelpBox(
                "Enter a generated Normal or Boss sector to debug its room trait. Home and non-generated sectors have no room trait runtime.",
                MessageType.Info);
            return;
        }

        int sectorId = sector.GetInstanceID();
        if (_roomTraitDebugSectorId != sectorId)
        {
            _roomTraitDebugSectorId = sectorId;
            _roomTraitDebugSelection = traits.SelectedRoomTrait;
        }

        EditorGUILayout.ObjectField("현재 섹터", sector, typeof(SectorRuntime), true);
        EditorGUILayout.LabelField("생성 시 이상징후: " + DebugRoomTraitName(traits.RolledRoomTrait), EditorStyles.wordWrappedMiniLabel);
        EditorGUILayout.LabelField("현재 적용 중: " + DebugRoomTraitName(traits.SelectedRoomTrait), EditorStyles.wordWrappedMiniLabel);
        EditorGUILayout.LabelField("디버그 변경", traits.HasDebugRoomTraitOverride ? "적용 중 — 실행 중에만" : "없음");
        EditorGUILayout.LabelField(DebugRoomTraitDescription(traits), EditorStyles.wordWrappedMiniLabel);
        EditorGUILayout.LabelField("섹터 가스 감염", $"{traits.CurrentBiochemicalGasInfectionPerSecond:0.##} /초");
        EditorGUILayout.LabelField("실패 가스 누적", $"{traits.TimerFailureLeakStacks}스택");
        EditorGUILayout.LabelField("레이드 시간초과 감염", $"{RaidRuntime.CurrentOvertimeInfection:0.##} /초");
        EditorGUILayout.LabelField("플레이어 합계 가스 감염", $"{SectorRoomTraitRuntime.CurrentPlayerBiochemicalGasInfectionPerSecond:0.##} /초");
        EditorGUILayout.LabelField("플레이어 가스 적용 주기", $"{SectorRoomTraitRuntime.CurrentPlayerBiochemicalGasTickInterval:0.##}초");

        var traitValues = (SectorRoomTraitType[])Enum.GetValues(typeof(SectorRoomTraitType));
        int traitIndex = Array.IndexOf(traitValues, _roomTraitDebugSelection);
        string[] traitLabels = Array.ConvertAll(traitValues, DebugRoomTraitName);
        _roomTraitDebugSelection = traitValues[EditorGUILayout.Popup("시험 적용할 이상징후", Mathf.Max(0, traitIndex), traitLabels)];

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("현재 섹터에 시험 적용"))
            {
                traits.SetDebugRoomTraitOverride(_roomTraitDebugSelection);
                EditorApplication.QueuePlayerLoopUpdate();
                SetOperationsStatus(
                    $"Applied runtime room trait {_roomTraitDebugSelection} to {sector.name}.",
                    MessageType.Info);
            }

            using (new EditorGUI.DisabledScope(!traits.HasDebugRoomTraitOverride))
            {
                if (GUILayout.Button("생성 당시 이상징후로 복원"))
                {
                    traits.ClearDebugRoomTraitOverride();
                    _roomTraitDebugSelection = traits.SelectedRoomTrait;
                    EditorApplication.QueuePlayerLoopUpdate();
                    SetOperationsStatus(
                        $"Restored the generated room trait {traits.RolledRoomTrait} on {sector.name}.",
                        MessageType.Info);
                }
            }
        }
    }

    internal static string DebugRoomTraitName(SectorRoomTraitType trait) => trait switch
    {
        SectorRoomTraitType.Normal => "없음 (Normal)",
        SectorRoomTraitType.Darkness => "어둠 — 시야 감소 (Darkness)",
        SectorRoomTraitType.Armed => "적 종합 강화 — 기존 규칙 (Armed)",
        SectorRoomTraitType.AmmoScavenger => "탄약 소모 증가 (AmmoScavenger)",
        SectorRoomTraitType.Fear => "공포 — 부상 확률 증가 (Fear)",
        SectorRoomTraitType.EnemyDamage => "적 공격 피해 증가 (EnemyDamage)",
        SectorRoomTraitType.GasBoost => "가스 농도 증가 (GasBoost)",
        _ => trait.ToString()
    };

    internal static string DebugRoomTraitDescription(SectorRoomTraitRuntime traits)
    {
        if (traits == null) return "섹터 없음";
        switch (traits.SelectedRoomTrait)
        {
            case SectorRoomTraitType.Darkness:
                return $"시야 거리 ×{SectorRoomTraitRuntime.CurrentPlayerVisionRangeMultiplier:0.##} · " +
                       $"안개 알파 ×{SectorRoomTraitRuntime.CurrentPlayerFogAlphaMultiplier:0.##} (현재 플레이어 기준)";
            case SectorRoomTraitType.AmmoScavenger:
                return $"탄약 소모 ×{SectorRoomTraitRuntime.CurrentPlayerAmmoConsumptionMultiplier} (현재 플레이어 기준)";
            case SectorRoomTraitType.Fear:
                return $"부상 발생 확률 ×{SectorRoomTraitRuntime.CurrentPlayerInjuryChanceMultiplier:0.##} (현재 플레이어 기준)";
            case SectorRoomTraitType.EnemyDamage:
            case SectorRoomTraitType.Armed:
                return DebugEnemyTraitDescription(traits);
            case SectorRoomTraitType.GasBoost:
                return $"현재 섹터 가스 감염 {traits.CurrentBiochemicalGasInfectionPerSecond:0.##}/초. " +
                       "가스가 활성화되지 않은 섹터에서는 효과가 없습니다.";
            default: return "추가 섹터 이상징후 없음. 스테이지 기본 가스·시간초과 가스는 별도로 적용됩니다.";
        }
    }

    private static string DebugEnemyTraitDescription(SectorRoomTraitRuntime traits)
    {
        foreach (var modifier in traits.GetComponentsInChildren<SectorEnemyTraitModifier>(true))
            return $"생성된 적 적용값: 공격 피해 ×{modifier.AttackDamageMultiplier:0.##} · " +
                   $"최대 체력 ×{modifier.MaxHealthMultiplier:0.##} · 이동 속도 ×{modifier.MoveSpeedMultiplier:0.##} · " +
                   $"방탄 등급 +{modifier.ArmorClassBonus}";
        return "생성된 적의 강화 컴포넌트가 아직 없습니다. 적 생성 후 실제 적용값을 표시합니다.";
    }

    // Purpose: Draws the stage definition list authoring UI.
    private static void DrawStageDefinitionList(IReadOnlyList<StageDefinitionSO> stages)
    {
        if (stages == null || stages.Count == 0)
        {
            EditorGUILayout.HelpBox("No stage definitions are configured.", MessageType.Warning);
            return;
        }

        using (new EditorGUI.IndentLevelScope())
        {
            for (int i = 0; i < stages.Count; i++)
            {
                StageDefinitionSO stage = stages[i];
                if (stage == null)
                    continue;

                EditorGUILayout.LabelField(
                    $"Stage {stage.StageIndex}",
                    string.IsNullOrWhiteSpace(stage.DisplayName) ? stage.name : stage.DisplayName);
            }
        }
    }

    // Purpose: Ends the debug raid through the production extraction transaction.
    private void ExtractDebugRaidToHome(RaidRuntime raid)
    {
        if (raid == null)
        {
            SetRaidExtractionStatus("진행 중인 RaidRuntime을 찾지 못했습니다.", MessageType.Error);
            return;
        }
        Transform playerRoot = ResolveDebugPlayerRoot();
        PlayerInventoryRuntime inventory = playerRoot != null
            ? EntityComponentResolver.Resolve<PlayerInventoryRuntime>(playerRoot) : null;
        if (inventory == null)
        {
            SetRaidExtractionStatus("탈출할 플레이어 인벤토리를 찾지 못했습니다.", MessageType.Error);
            return;
        }
        SetRaidExtractionStatus("강제 탈출 요청 · 실제 탈출 경로로 HOME에 복귀합니다.", MessageType.Info);
        try
        {
            bool accepted = raid.DebugTryExtractCurrentSector(inventory, success =>
            {
                if (this == null) return;
                string failure = raid != null && !string.IsNullOrWhiteSpace(raid.LastExtractionFailure)
                    ? raid.LastExtractionFailure : "HOME 복귀가 완료되지 않았습니다. SceneLoader 로그를 확인하세요.";
                SetRaidExtractionStatus(success ? "레이드 탈출 완료 · HOME 복귀/실제 탈출 저장 처리." : "강제 탈출 실패: " + failure,
                    success ? MessageType.Info : MessageType.Error);
                RefreshRows(); Repaint();
            });
            if (!accepted) SetRaidExtractionStatus("강제 탈출 거절: " +
                (string.IsNullOrWhiteSpace(raid.LastExtractionFailure) ? "탈출 요청을 시작하지 못했습니다." : raid.LastExtractionFailure), MessageType.Warning);
        }
        catch (Exception error)
        {
            SetRaidExtractionStatus("탈출 처리 예외: " + error.Message, MessageType.Error);
            Debug.LogException(error);
        }
    }

    private void SetRaidExtractionStatus(string message, MessageType type)
    {
        _raidExtractionStatus = message;
        _raidExtractionStatusType = type;
        string log = "[DebugMonitor][RaidExtraction] " + message;
        if (type == MessageType.Error) Debug.LogError(log);
        else if (type == MessageType.Warning) Debug.LogWarning(log);
        else Debug.Log(log);
        Repaint();
    }

    // Purpose: Performs the return debug player to start hub workflow for this component.
    private void ReturnDebugPlayerToStartHub(SectorRuntime startSector)
    {
        Transform playerRoot = ResolveDebugPlayerRoot();
        if (playerRoot == null)
        {
            SetOperationsStatus("PlayerCharacter was not found.", MessageType.Error);
            return;
        }

        if (!_sectorStateManager.DebugTryEnterSector(startSector))
        {
            SetOperationsStatus("SectorStateManager rejected Start Hub entry.", MessageType.Error);
            return;
        }

        MoveDebugPlayerToSectorSafePoint(playerRoot, startSector);
        SetOperationsStatus("Player moved to the generated Start Hub.", MessageType.Info);
        RefreshRows();
        Repaint();
    }

    // Purpose: Draws the item catalog authoring UI.
    private void DrawItemCatalog()
    {
        EditorGUILayout.LabelField("Item Catalog", EditorStyles.boldLabel);

        using (new EditorGUILayout.HorizontalScope())
        {
            _debugItemSearch = EditorGUILayout.TextField("Search", _debugItemSearch);
            if (GUILayout.Button("Rescan Assets", GUILayout.Width(110f)))
                ScanDebugItems();
        }

        if (!_debugItemsLoaded)
            ScanDebugItems();

        _selectedDebugItem = (ItemSO)EditorGUILayout.ObjectField(
            "Selected Item",
            _selectedDebugItem,
            typeof(ItemSO),
            false);

        _debugItemScroll = EditorGUILayout.BeginScrollView(
            _debugItemScroll,
            GUI.skin.box,
            GUILayout.MinHeight(110f),
            GUILayout.MaxHeight(210f));

        string search = _debugItemSearch?.Trim();
        for (int i = 0; i < _debugItems.Count; i++)
        {
            ItemSO item = _debugItems[i];
            if (item == null)
            {
                continue;
            }

            string itemName = InventoryItemDisplayUtility.ResolveItemName(item);
            if (!string.IsNullOrEmpty(search) &&
                itemName.IndexOf(search, StringComparison.OrdinalIgnoreCase) < 0 &&
                item.name.IndexOf(search, StringComparison.OrdinalIgnoreCase) < 0)
            {
                continue;
            }

            GUIContent content = new(itemName, item.PreviewImage != null ? item.PreviewImage.texture : null);
            if (GUILayout.Button(content, GUILayout.Height(22f)))
                _selectedDebugItem = item;
        }

        EditorGUILayout.EndScrollView();
    }

    // Purpose: Performs the scan debug items workflow for this component.
    private void ScanDebugItems()
    {
        _debugItems.Clear();
        string[] guids = AssetDatabase.FindAssets("t:ItemSO", new[] { "Assets" });

        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            ItemSO item = AssetDatabase.LoadAssetAtPath<ItemSO>(path);
            if (item != null)
                _debugItems.Add(item);
        }

        _debugItems.Sort((left, right) =>
            string.Compare(left != null ? InventoryItemDisplayUtility.ResolveItemName(left) : string.Empty,
                right != null ? InventoryItemDisplayUtility.ResolveItemName(right) : string.Empty,
                StringComparison.OrdinalIgnoreCase));
        _debugItemsLoaded = true;
    }

    // Purpose: Draws the entity item operations authoring UI.
    private void DrawEntityItemOperations(CharacterRow targetOverride = null)
    {
        EditorGUILayout.LabelField("Runtime Items", EditorStyles.boldLabel);

        CharacterRow target = targetOverride ?? DrawRuntimeTargetPopup();
        if (targetOverride != null)
            EditorGUILayout.LabelField("Target", $"{target.displayName} [{target.typeName}]");

        _runtimeItemAmount = Mathf.Max(1, EditorGUILayout.IntField("Amount", _runtimeItemAmount));

        using (new EditorGUI.DisabledScope(target == null || _selectedDebugItem == null))
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Add To Inventory"))
                    AddSelectedItemToEntityInventory(target);

                if (GUILayout.Button("Drop At Feet"))
                    DropSelectedItemAtEntityFeet(target);
            }

            _runtimeEquipmentSlot = Mathf.Max(0, EditorGUILayout.IntField("Weapon Slot Index", _runtimeEquipmentSlot));
            if (GUILayout.Button("Equip Weapon / Armor"))
                EquipSelectedItemToEntity(target);
        }

        if (target != null)
        {
            EditorGUILayout.LabelField(
                "Runtime Refs",
                $"PlayerInventory={target.playerInventory != null}, " +
                $"LootInventory={target.lootInventory != null}, " +
                $"Equipment={target.equipmentRuntime != null}");

            DrawPlayerInventoryRefactorBoundary(target.playerInventory);
        }
    }

    // Purpose: Draws the player inventory refactor boundary authoring UI.
    private static void DrawPlayerInventoryRefactorBoundary(PlayerInventoryRuntime inventory)
    {
        if (inventory == null)
            return;

        PlayerInventoryRuntimeDiagnosticsSnapshot snapshot = inventory.GetDiagnosticsSnapshot();
        EditorGUILayout.Space(3f);
        EditorGUILayout.LabelField("Inventory Runtime Boundary", EditorStyles.miniBoldLabel);
        EditorGUILayout.LabelField(
            "Backpack",
            $"Slots {snapshot.UsedSlots}/{snapshot.Capacity}, free={snapshot.FreeSlots}, entries={snapshot.StackEntryCount}");
        EditorGUILayout.LabelField(
            "Ownership",
            $"Equipment={snapshot.HasEquipmentRuntime}, QuickSlot={snapshot.HasQuickSlotRuntime}, " +
            $"StartingLoadoutApplied={snapshot.StartingLoadoutApplied}");
        EditorGUILayout.LabelField(
            "Ammo Slot Compatibility",
            snapshot.HasAmmoLoadoutCompatibility
                ? "Connected — compatibility only; do not add new AmmoSlot authoring."
                : "Not connected — inventory / weapon virtual-magazine path only.");
    }

    // Purpose: Draws the selected character runtime items authoring UI.
    private void DrawSelectedCharacterRuntimeItems(CharacterRow row)
    {
        if (row == null)
            return;

        EditorGUILayout.Space(8f);
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("Selected Character Items", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "The selected character is always the target. Add places the item into that character's runtime inventory; Drop creates a world pickup at that character's feet.",
            MessageType.None);
        DrawItemCatalog();
        EditorGUILayout.Space(6f);
        DrawEntityItemOperations(row);
        DrawOperationsStatus();
        EditorGUILayout.EndVertical();
    }

    // Purpose: Draws the selected combat test controls authoring UI.
    private void DrawSelectedCombatTestControls(CharacterRow row)
    {
        if (row == null || row.combat == null)
            return;

        EnemyShooterCombatController combat = row.combat;

        EditorGUILayout.Space(8f);
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("Enemy Combat Test Controls", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Runtime overrides affect only this spawned enemy. They reset when Play Mode ends and never edit the shared EnemyShooterConfigSO. " +
            "With Use Cover enabled, the Debug Monitor can temporarily derive cover from a nearby blocking collider when the sector has no authored EnemyShooterCoverPoint.",
            MessageType.Info);

        bool overridesEnabled = EditorGUILayout.Toggle(
            new GUIContent(
                "Enable Runtime Overrides",
                "Allows the controls below to override only this live enemy. Disable to return to its EnemyShooterConfigSO values."),
            combat.DebugCombatOverridesEnabled);
        if (overridesEnabled != combat.DebugCombatOverridesEnabled)
            combat.SetDebugCombatOverridesEnabled(overridesEnabled);

        if (!combat.DebugCombatOverridesEnabled)
        {
            EditorGUILayout.LabelField(
                "Config Source",
                combat.Config != null ? combat.Config.name : "Missing EnemyShooterConfigSO");
            DrawCombatLiveStatus(combat);
            EditorGUILayout.EndVertical();
            return;
        }

        EditorGUI.BeginChangeCheck();
        bool tacticalIntent = EditorGUILayout.Toggle(
            new GUIContent(
                "Tactical Intent",
                "Master switch for cover selection, duel-token allocation, and teammate flanking. Cover and team tactics require this."),
            combat.EffectiveUseTacticalIntent);
        bool cover = EditorGUILayout.Toggle(
            new GUIContent(
                "Use Cover",
                "Looks for usable cover, moves behind it, waits, peeks to fire, then returns. Debug overrides can use a nearby blocking collider as temporary cover."),
            combat.EffectiveUseCoverTactics);
        bool fireWhileMovingToCover = EditorGUILayout.Toggle(
            new GUIContent(
                "Fire While Moving To Cover",
                "While advancing toward selected cover, continue normal bursts only when the enemy still has a clear muzzle path to the target. The advance speed multiplier below makes this movement slower."),
            combat.EffectiveFireWhileMovingToCover);
        bool duelToken = EditorGUILayout.Toggle(
            new GUIContent(
                "Duel Token",
                "Allows one squad member to claim the direct stand-off role against a target. Other members can use flanking instead of stacking on the same angle."),
            combat.EffectiveUseDuelToken);
        bool flanking = EditorGUILayout.Toggle(
            new GUIContent(
                "Flank Teammates",
                "When another squad member holds the duel token, try to take a wider left or right firing angle."),
            combat.EffectiveUseFlankingTactics);
        bool dash = EditorGUILayout.Toggle(
            new GUIContent(
                "Use Dash",
                "Enables defensive dash behavior. The trigger toggles below decide when a dash may start."),
            combat.EffectiveUseDash);
        bool dashOnDamaged = EditorGUILayout.Toggle(
            new GUIContent(
                "Dash On Incoming Projectile",
                "When a hostile basic bullet is predicted to hit this enemy shortly, roll Dash Chance On Incoming Projectile before the impact. The enemy does not dash after damage has already landed."),
            combat.EffectiveDashOnIncomingProjectile);
        bool dashWhenHealthLow = EditorGUILayout.Toggle(
            new GUIContent(
                "Dash At Low Health",
                "Allows the enemy to attempt a defensive dash once current health is at or below Dash Low Health Threshold."),
            combat.EffectiveDashWhenHealthLow);
        bool throwable = EditorGUILayout.Toggle(
            new GUIContent(
                "Use Carried Throwable",
                "Allows the enemy to use a carried ThrowableUsableItemSO when its target and throw path satisfy the configured conditions."),
            combat.EffectiveUseThrowableAttack);
        bool usePreferredRangeBeforeStationaryAttack = EditorGUILayout.Toggle(
            new GUIContent(
                "Use Preferred Range Before Stationary Attack",
                "For Stop While Attacking only: do not start direct fire at weapon maximum range. Chase into Attack Preferred Distance first. This live override never changes the shared config asset."),
            combat.EffectiveUsePreferredRangeBeforeStationaryAttack);

        if (EditorGUI.EndChangeCheck())
        {
            combat.SetDebugCombatToggles(
                tacticalIntent,
                cover,
                fireWhileMovingToCover,
                duelToken,
                flanking,
                dash,
                dashOnDamaged,
                dashWhenHealthLow,
                throwable,
                usePreferredRangeBeforeStationaryAttack);
        }

        EditorGUILayout.Space(4f);
        EditorGUILayout.LabelField("Runtime Tuning", EditorStyles.boldLabel);
        EditorGUI.BeginChangeCheck();
        float coverRadius = EditorGUILayout.FloatField(
            new GUIContent(
                "Cover Search Radius",
                "Maximum planar distance for this enemy to search cover from its current position."),
            combat.EffectiveCoverSearchRadius);
        float coverHide = EditorGUILayout.FloatField(
            new GUIContent(
                "Cover Hide Seconds",
                "How long the enemy waits protected behind cover before moving toward a peek point."),
            combat.EffectiveCoverHideSeconds);
        float coverPeek = EditorGUILayout.FloatField(
            new GUIContent(
                "Cover Peek Seconds",
                "Maximum time the enemy holds a clear peek position before returning behind cover."),
            combat.EffectiveCoverPeekSeconds);
        float coverAdvanceSpeed = EditorGUILayout.Slider(
            new GUIContent(
                "Cover Advance Speed Multiplier",
                "Movement speed multiplier only while Fire While Moving To Cover is enabled and the enemy is advancing toward cover."),
            combat.EffectiveCoverApproachSpeedMultiplier,
            0.1f,
            1f);
        float flankDistance = EditorGUILayout.FloatField(
            new GUIContent(
                "Flank Distance",
                "Target-relative distance used when a non-duel squad member searches for a wider firing lane."),
            combat.EffectiveTacticalFlankDistance);
        float dashChance = EditorGUILayout.Slider(
            new GUIContent(
                "Dash Chance On Incoming Projectile",
                "Probability rolled once for each hostile basic bullet predicted to hit this enemy while Dash On Incoming Projectile is enabled."),
            combat.EffectiveDashChanceOnIncomingProjectile,
            0f,
            1f);
        float dashHealth = EditorGUILayout.Slider(
            new GUIContent(
                "Dash Low Health Threshold",
                "Normalized health threshold from 0 to 1. For example, 0.35 means the enemy may dash at 35% health or lower."),
            combat.EffectiveDashHealthThreshold,
            0f,
            1f);
        float dashCooldown = EditorGUILayout.FloatField(
            new GUIContent(
                "Dash Cooldown Seconds",
                "Minimum wait after a dash before another defensive dash is allowed."),
            combat.EffectiveDashCooldownSeconds);
        float dashDuration = EditorGUILayout.FloatField(
            new GUIContent(
                "Dash Duration Seconds",
                "Duration of each defensive dash movement."),
            combat.EffectiveDashDurationSeconds);
        float dashSpeed = EditorGUILayout.FloatField(
            new GUIContent(
                "Dash Speed",
                "Movement speed applied while the defensive dash is active."),
            combat.EffectiveDashSpeed);
        float stationaryAttackKeepPadding = EditorGUILayout.FloatField(
            new GUIContent(
                "Stationary Attack Keep Padding",
                "Extra distance allowed after direct stationary fire has started. Used only when Use Preferred Range Before Stationary Attack is enabled."),
            combat.EffectiveStationaryAttackKeepRangePadding);
        if (EditorGUI.EndChangeCheck())
        {
            combat.SetDebugCombatTuning(
                coverRadius,
                coverHide,
                coverPeek,
                coverAdvanceSpeed,
                flankDistance,
                dashChance,
                dashHealth,
                dashCooldown,
                dashDuration,
                dashSpeed,
                stationaryAttackKeepPadding);
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button(new GUIContent(
                    "Force Defensive Dash",
                    "Immediately attempts one defensive dash using the live target, NavMesh, and current dash settings.")))
            {
                bool started = combat.DebugTryStartDefensiveDash();
                SetOperationsStatus(
                    started
                        ? $"Started defensive dash for {row.displayName}."
                        : "Dash did not start. Check NavMesh, cooldown, and Use Dash.",
                    started ? MessageType.Info : MessageType.Warning);
            }

            if (GUILayout.Button(new GUIContent(
                    "Throw Carried Throwable",
                    "Immediately attempts to throw the enemy's first carried throwable at its current target.")))
            {
                bool thrown = combat.DebugTryThrowCarriedThrowable();
                SetOperationsStatus(
                    thrown
                        ? $"Throwable launched by {row.displayName}."
                        : "Throwable did not launch. The enemy needs a target, a carried ThrowableUsableItemSO, and a valid throw path.",
                    thrown ? MessageType.Info : MessageType.Warning);
            }
        }

        DrawCombatLiveStatus(combat);
        DrawOperationsStatus();
        EditorGUILayout.EndVertical();
    }

    // Purpose: Draws the combat live status authoring UI.
    private static void DrawCombatLiveStatus(EnemyShooterCombatController combat)
    {
        EnemyShooterCombatDiagnosticsSnapshot snapshot = combat.GetDiagnosticsSnapshot();

        EditorGUILayout.Space(4f);
        EditorGUILayout.LabelField("Live Tactical State", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("Controller Boundary", "EnemyShooterCombatController.Diagnostics Snapshot");
        EditorGUILayout.LabelField(
            "Core",
            $"Goal={snapshot.Goal} / Phase={snapshot.AttackPhase} / " +
            $"Engaged={snapshot.IsEngaged} / Alerted={snapshot.IsAlerted}");
        EditorGUILayout.LabelField(
            "Attack",
            $"CanAttack={snapshot.CanAttackTarget} / {snapshot.AttackStatus} / " +
            $"{snapshot.ActiveAttackRange:0.##}m (Start {snapshot.AttackStartRange:0.##} / Keep {snapshot.AttackKeepRange:0.##})");
        EditorGUILayout.LabelField(
            "Target",
            snapshot.HasTarget
                ? $"{snapshot.TargetPlanarDistance:0.##}m / Sight={snapshot.CanSeeTarget} / Lock={snapshot.TargetLockRemaining:0.00}s"
                : "None");
        EditorGUILayout.LabelField(
            "Action Gate",
            snapshot.IsActionBlockedByStatusEffect ? "Blocked by status effect" : "Open");
        EditorGUILayout.LabelField("Intent", snapshot.TacticalIntent.ToString());
        EditorGUILayout.LabelField(
            "Cover",
            $"{snapshot.CoverTacticPhase} / Reserved={snapshot.HasReservedCover} / " +
            $"Registered={EnemyShooterCoverPoint.RegisteredPoints.Count} / RuntimeAuto={snapshot.HasRuntimeAutoCoverPoint}");
        EditorGUILayout.LabelField(
            "Duel Token",
            snapshot.HoldsDuelToken ? "Held by selected enemy" : "Not held");
        EditorGUILayout.LabelField("Tactical Destination", snapshot.TacticalDestination.ToString("F2"));
        EditorGUILayout.LabelField(
            "Dash",
            snapshot.IsDashing
                ? "Dashing"
                : $"Ready setting / cooldown {combat.EffectiveDashCooldownSeconds:0.00}s");
        EditorGUILayout.LabelField(
            "Throwable",
            $"Carried={snapshot.HasCarriedThrowable}, cooldown={snapshot.ThrowableCooldownRemaining:0.00}s");
    }

    // Purpose: Draws the runtime target popup authoring UI.
    private CharacterRow DrawRuntimeTargetPopup()
    {
        List<CharacterRow> targets = new();
        List<string> labels = new();

        for (int i = 0; i < _rows.Count; i++)
        {
            CharacterRow row = _rows[i];
            if (row == null ||
                (row.playerInventory == null &&
                 row.lootInventory == null &&
                 row.equipmentRuntime == null))
            {
                continue;
            }

            targets.Add(row);
            labels.Add($"{row.displayName} [{row.typeName}]");
        }

        if (targets.Count == 0)
        {
            EditorGUILayout.HelpBox("No runtime entity inventory was found.", MessageType.Warning);
            _runtimeTargetRowId = -1;
            return null;
        }

        int selectedIndex = 0;
        for (int i = 0; i < targets.Count; i++)
        {
            if (targets[i].id == _runtimeTargetRowId)
            {
                selectedIndex = i;
                break;
            }
        }

        selectedIndex = EditorGUILayout.Popup("Target Entity", selectedIndex, labels.ToArray());
        CharacterRow selected = targets[Mathf.Clamp(selectedIndex, 0, targets.Count - 1)];
        _runtimeTargetRowId = selected.id;
        return selected;
    }

    // Purpose: Adds the requested selected item to entity inventory to this component.
    private void AddSelectedItemToEntityInventory(CharacterRow target)
    {
        if (target.playerInventory != null)
        {
            bool succeeded = target.playerInventory.TryPickup(
                _selectedDebugItem,
                _runtimeItemAmount,
                out string message);
            SetOperationsStatus(message, succeeded ? MessageType.Info : MessageType.Error);
            return;
        }

        if (target.lootInventory != null)
        {
            bool succeeded = TryAddEnemyLootItem(
                target.lootInventory,
                _selectedDebugItem,
                _runtimeItemAmount,
                out string message);
            SetOperationsStatus(message, succeeded ? MessageType.Info : MessageType.Error);
            return;
        }

        SetOperationsStatus("The selected entity has no supported inventory runtime.", MessageType.Error);
    }

    // Purpose: Performs the drop selected item at entity feet workflow for this component.
    private void DropSelectedItemAtEntityFeet(CharacterRow target)
    {
        if (target == null || target.root == null || _selectedDebugItem == null)
        {
            SetOperationsStatus("Select a live target entity and an item before dropping.", MessageType.Error);
            return;
        }

        Vector3 position = target.root.position + Vector3.up * 0.1f;
        bool succeeded = WorldItemPickupSpawner.TrySpawn(
            _selectedDebugItem,
            Mathf.Max(1, _runtimeItemAmount),
            1,
            0f,
            -1f,
            -1f,
            -1f,
            position,
            Quaternion.identity,
            out WorldItemPickup pickup);

        string itemName = InventoryItemDisplayUtility.ResolveItemName(_selectedDebugItem);
        SetOperationsStatus(
            succeeded
                ? $"Dropped {itemName} x{Mathf.Max(1, _runtimeItemAmount)} at {target.displayName}'s feet."
                : $"Failed to drop {itemName}. Check the active WorldItemPickupSpawner configuration.",
            succeeded ? MessageType.Info : MessageType.Error);
    }

    // Purpose: Attempts to add enemy loot item and reports whether the request succeeded.
    private static bool TryAddEnemyLootItem(
        EnemyLootInventoryRuntime inventory,
        ItemSO item,
        int amount,
        out string message)
    {
        if (inventory == null || item == null || amount <= 0)
        {
            message = "Inventory, item, or amount is invalid.";
            return false;
        }

        inventory.ConfigureInventoryCapacity(Mathf.Max(1, inventory.InventoryCapacity));
        IReadOnlyList<PlayerInventoryItemStack> stacks = inventory.ItemStacks;
        int maxStack = Mathf.Max(1, item.MaxStack);
        int capacity = inventory.InventoryCapacity;
        int ownedAmount = 0;
        int availableAmount = 0;

        for (int i = 0; i < capacity; i++)
        {
            PlayerInventoryItemStack stack = i < stacks.Count ? stacks[i] : null;

            if (stack == null || stack.IsEmpty)
            {
                availableAmount += maxStack;
                continue;
            }

            if (!IsSameItem(stack.Item, item))
                continue;

            ownedAmount += stack.Amount;
            availableAmount += Mathf.Max(0, maxStack - stack.Amount);
        }

        if (item.MaxOwnedAmount > 0 && ownedAmount + amount > item.MaxOwnedAmount)
        {
            message =
                $"Ownership cap exceeded. item={InventoryItemDisplayUtility.ResolveItemName(item)}, " +
                $"owned={ownedAmount}, requested={amount}, cap={item.MaxOwnedAmount}.";
            return false;
        }

        if (availableAmount < amount)
        {
            message =
                $"Enemy inventory has insufficient capacity. requested={amount}, available={availableAmount}.";
            return false;
        }

        int remaining = amount;

        for (int i = 0; i < capacity && remaining > 0; i++)
        {
            PlayerInventoryItemStack stack = i < stacks.Count ? stacks[i] : null;
            if (stack == null || stack.IsEmpty || !IsSameItem(stack.Item, item) || stack.Amount >= maxStack)
                continue;

            int added = Mathf.Min(remaining, maxStack - stack.Amount);
            inventory.SetItemStack(
                i,
                item,
                stack.Amount + added,
                stack.ArmorDurability,
                stack.WeaponCurrentDurability,
                stack.WeaponMaxDurability);
            remaining -= added;
        }

        for (int i = 0; i < capacity && remaining > 0; i++)
        {
            PlayerInventoryItemStack stack = i < stacks.Count ? stacks[i] : null;
            if (stack != null && !stack.IsEmpty)
                continue;

            int added = Mathf.Min(remaining, maxStack);
            ResolveDefaultDurability(item, out float armor, out float currentWeapon, out float maxWeapon);
            inventory.SetItemStack(i, item, added, armor, currentWeapon, maxWeapon);
            remaining -= added;
        }

        message = $"Added {amount}x {InventoryItemDisplayUtility.ResolveItemName(item)} to enemy inventory.";
        return remaining == 0;
    }

    // Purpose: Checks whether same item is currently true.
    private static bool IsSameItem(ItemSO left, ItemSO right)
    {
        if (left == right)
            return true;

        return left != null &&
               right != null &&
               !string.IsNullOrWhiteSpace(left.Guid) &&
               left.Guid == right.Guid;
    }

    // Purpose: Equips selected item to entity for the current owner.
    private void EquipSelectedItemToEntity(CharacterRow target)
    {
        if (_selectedDebugItem is not WeaponSO && _selectedDebugItem is not ArmorItemSO)
        {
            SetOperationsStatus("Only WeaponSO or ArmorItemSO can be equipped.", MessageType.Warning);
            return;
        }

        ResolveDefaultDurability(
            _selectedDebugItem,
            out float armorDurability,
            out float weaponCurrentDurability,
            out float weaponMaxDurability);

        if (target.playerInventory != null)
        {
            PlayerInventoryEquipmentSlotKind kind = _selectedDebugItem is WeaponSO
                ? PlayerInventoryEquipmentSlotKind.Weapon
                : PlayerInventoryEquipmentSlotKind.Armor;
            bool succeeded = target.playerInventory.TryEquipExternalItemToSlot(
                _selectedDebugItem,
                _runtimeItemAmount,
                armorDurability,
                weaponCurrentDurability,
                weaponMaxDurability,
                kind,
                _runtimeEquipmentSlot,
                out string message);
            SetOperationsStatus(message, succeeded ? MessageType.Info : MessageType.Error);
            return;
        }

        if (target.equipmentRuntime == null)
        {
            SetOperationsStatus("EntityEquipmentRuntime was not found on the selected entity.", MessageType.Error);
            return;
        }

        if (_selectedDebugItem is WeaponSO weapon)
        {
            target.equipmentRuntime.EquipWeapon(
                _runtimeEquipmentSlot,
                weapon,
                _runtimeItemAmount,
                weaponCurrentDurability,
                weaponMaxDurability);
        }
        else if (_selectedDebugItem is ArmorItemSO armor)
        {
            target.equipmentRuntime.EquipArmor(armor, armorDurability);
        }

        target.lootInventory?.SyncEquipmentFromRuntime();
        SetOperationsStatus(
            $"Equipped {InventoryItemDisplayUtility.ResolveItemName(_selectedDebugItem)} on {target.displayName}.",
            MessageType.Info);
    }

    // Purpose: Resolves the default durability needed by this component.
    private static void ResolveDefaultDurability(
        ItemSO item,
        out float armorDurability,
        out float weaponCurrentDurability,
        out float weaponMaxDurability)
    {
        armorDurability = item is ArmorItemSO armor ? armor.MaxDurability : -1f;
        weaponMaxDurability = item is RangedWeaponSO ranged ? ranged.MaxDurability : -1f;
        weaponCurrentDurability = weaponMaxDurability;
    }

    // Purpose: Draws the shop operations authoring UI.
    private void DrawShopOperations()
    {
        EditorGUILayout.LabelField("Shop Runtime", EditorStyles.boldLabel);

        if (_debugShop == null)
            _debugShop = ResolvePreferredDebugShop();

        _debugShop = (SectorShop)EditorGUILayout.ObjectField(
            "Shop",
            _debugShop,
            typeof(SectorShop),
            true);
        _debugShopPrice = Mathf.Max(0, EditorGUILayout.IntField("Price", _debugShopPrice));
        _debugShopStock = Mathf.Max(1, EditorGUILayout.IntField("Stock", _debugShopStock));
        _debugShopBundle = Mathf.Max(1, EditorGUILayout.IntField("Bundle Amount", _debugShopBundle));

        if (_debugShop != null)
        {
            EditorGUILayout.LabelField(
                "Offers",
                $"Initialized={_debugShop.DebugOffersInitialized}, " +
                $"Bullets={_debugShop.DebugBulletOffers.Count}, Items={_debugShop.DebugItemOffers.Count}");

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Ensure Offers"))
                {
                    bool succeeded = _debugShop.DebugTryEnsureOffers(out string message);
                    SetOperationsStatus(message, succeeded ? MessageType.Info : MessageType.Error);
                }

                using (new EditorGUI.DisabledScope(_selectedDebugItem == null))
                {
                    if (GUILayout.Button("Add Selected Offer"))
                    {
                        bool succeeded = _debugShop.DebugTryAddOffer(
                            _selectedDebugItem,
                            _debugShopPrice,
                            _debugShopStock,
                            _debugShopBundle,
                            out string message);
                        SetOperationsStatus(message, succeeded ? MessageType.Info : MessageType.Error);
                    }
                }

                if (GUILayout.Button("Clear Offers"))
                {
                    bool succeeded = _debugShop.DebugClearOffers(out string message);
                    SetOperationsStatus(message, succeeded ? MessageType.Info : MessageType.Error);
                }
            }
        }
    }

    // Purpose: Resolves the preferred debug shop needed by this component.
    private SectorShop ResolvePreferredDebugShop()
    {
        if (_sectorStateManager == null)
            _sectorStateManager = UnityEngine.Object.FindAnyObjectByType<SectorStateManager>();

        SectorRuntime currentSector = _sectorStateManager != null
            ? _sectorStateManager.CurrentSector
            : null;

        SectorShop currentSectorShop = currentSector != null
            ? currentSector.GetComponentInChildren<SectorShop>(true)
            : null;

        return currentSectorShop != null
            ? currentSectorShop
            : UnityEngine.Object.FindAnyObjectByType<SectorShop>();
    }

    // Purpose: Draws the warehouse operations authoring UI.
    private void DrawWarehouseOperations()
    {
        EditorGUILayout.LabelField("Inheritance Warehouse (Persistent)", EditorStyles.boldLabel);

        if (_debugWarehouse == null)
            _debugWarehouse = UnityEngine.Object.FindAnyObjectByType<InheritanceWarehouseRuntime>();

        _debugWarehouse = (InheritanceWarehouseRuntime)EditorGUILayout.ObjectField(
            "Warehouse",
            _debugWarehouse,
            typeof(InheritanceWarehouseRuntime),
            true);

        if (_debugWarehouse == null)
            return;

        EditorGUILayout.HelpBox(
            "Debug mutations here are written to the inheritance save immediately.",
            MessageType.Warning);
        EditorGUILayout.LabelField("Persistence", _debugWarehouse.DebugPersistenceStatus);
        EditorGUILayout.LabelField(
            "Contents",
            $"{CountNonEmptyStacks(_debugWarehouse.DebugContents)} / {_debugWarehouse.DebugCapacity} slots");

        using (new EditorGUILayout.HorizontalScope())
        {
            using (new EditorGUI.DisabledScope(
                       !_debugWarehouse.DebugPersistenceReady || _selectedDebugItem == null))
            {
                if (GUILayout.Button("Add Selected Item"))
                {
                    bool succeeded = _debugWarehouse.DebugTryAddItem(
                        _selectedDebugItem,
                        _runtimeItemAmount,
                        out string message);
                    SetOperationsStatus(message, succeeded ? MessageType.Info : MessageType.Error);
                }
            }

            if (GUILayout.Button("Reload Save"))
            {
                bool succeeded = _debugWarehouse.DebugReloadFromSave(out string message);
                SetOperationsStatus(message, succeeded ? MessageType.Info : MessageType.Error);
            }

            if (GUILayout.Button("Clear Saved Warehouse") &&
                EditorUtility.DisplayDialog(
                    "Clear Inheritance Warehouse?",
                    "This permanently removes every saved inheritance item. This cannot be undone.",
                    "Clear",
                    "Cancel"))
            {
                bool succeeded = _debugWarehouse.DebugClearContents(out string message);
                SetOperationsStatus(message, succeeded ? MessageType.Info : MessageType.Error);
            }
        }
    }

    // Purpose: Counts the available non empty stacks.
    private static int CountNonEmptyStacks(IReadOnlyList<PlayerInventoryItemStack> stacks)
    {
        if (stacks == null)
            return 0;

        int count = 0;
        for (int i = 0; i < stacks.Count; i++)
        {
            if (stacks[i] != null && !stacks[i].IsEmpty)
                count++;
        }

        return count;
    }

    // Purpose: Updates the operations status state.
    private void SetOperationsStatus(string message, MessageType type)
    {
        _operationsStatus = message ?? string.Empty;
        _operationsStatusType = type;
        Repaint();
    }

    // Purpose: Draws the operations status authoring UI.
    private void DrawOperationsStatus()
    {
        if (!string.IsNullOrWhiteSpace(_operationsStatus))
            EditorGUILayout.HelpBox(_operationsStatus, _operationsStatusType);
    }
}
