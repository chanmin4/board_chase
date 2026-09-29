# Authoring 스크립트 전수 정적 인덱스
검사일: 2026-09-22. 함수/타입/직렬화 연결은 정적 추출이며 실행 검증을 뜻하지 않습니다. 동적 생성, 리플렉션 및 Addressables 간접 참조는 별도 시스템 문서와 함께 확인하세요.

## Assets/Scripts/Authoring/Attribute/ReadOnly/ReadOnlyAttribute.cs
- 분류: 일반 소스; 9행
- 타입: ReadOnlyAttribute : PropertyAttribute
- 메서드: 
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 0개: 

## Assets/Scripts/Authoring/Editor/AudioCueLibraryWindow.cs
- 분류: 에디터; 855행
- 타입: AudioCueLibraryWindow : EditorWindow
- 메서드: Open, OnEnable, ResolveDefaultLibrary, OnGUI, DrawNewCueInspector, ApplyCueTemplate, DrawToolbar, DrawValidationSummary, DrawCategoryBrowser, DrawCategoryBrowserRow, DrawCueList, DrawCueCardGrid, DrawCueCard, DrawSelectedCue, DrawSelectedCueAuthoringFields, DrawSelectedCueIssues, RefreshCatalog, RebuildFilteredCues, CountCues, CountCuesWithoutPlayableClips, CountUnregisteredCues, GetActiveBrowserLabel, ValidateCatalog, GetIssuesFor, CreateLibrary, CreateCue, BeginCreateCue, DuplicateSelectedCue, SanitizeAssetName, GetCueDirectory, SyncLibrary, FindUsagePaths, IsReferenceSearchPath, EnsureDirectory, PreviewCue, StopPreview
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 1개: Assets/Scripts/Authoring/Editor/BoardChaseAuthoringHubWindow.cs

## Assets/Scripts/Authoring/Editor/AuthoringWorkspaceUi.cs
- 분류: 에디터; 60행
- 타입: AuthoringWorkspaceUi
- 메서드: DrawHeader
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 10개: Assets/Editor/ItemMakerWindow.cs; Assets/Editor/LootProgressionEditorWindow.cs; Assets/Editor/QuestDesignTreeWindow.cs; Assets/Editor/ShooterCustomizeStationWindow.cs; Assets/Editor/ShopProgressionTreeWindow.cs; Assets/Scripts/Authoring/Editor/AudioCueLibraryWindow.cs; Assets/Scripts/Authoring/Editor/BoardChaseAuthoringHubWindow.cs; Assets/Scripts/Authoring/Editor/ContentPreflightWindow.cs

## Assets/Scripts/Authoring/Editor/BoardChaseAuthoringHubWindow.cs
- 분류: 에디터; 90행
- 타입: BoardChaseAuthoringHubWindow : EditorWindow
- 메서드: Open, OnGUI, DrawToolCard
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 0개: 

## Assets/Scripts/Authoring/Editor/BossConfigPrefabLinkProvisioner.cs
- 분류: 에디터; 121행
- 타입: BossConfigPrefabLinkProvisioner, LinkTarget
- 메서드: RepairMissingPrefabLinks
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 0개: 

## Assets/Scripts/Authoring/Editor/BulletTestPrefabGenerator.cs
- 분류: 에디터; 305행
- 타입: BulletTestPrefabGenerator, BulletTestPrefabSourcePostprocessor : AssetPostprocessor
- 메서드: RegeneratePrefabs, GenerateMissingPrefabs, Generate, AreAllOutputsPresent, GetOrCreateMaterial, CreatePrefab, FindDescendant, OnPostprocessAllAssets
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 0개: 

## Assets/Scripts/Authoring/Editor/ContentPreflightWindow.cs
- 분류: 에디터; 1465행
- 타입: ContentPreflightSeverity, ContentPreflightIssue, ContentPreflightValidator, ContentPreflightWindow : EditorWindow
- 메서드: CollectIssues, ValidateStageRunFlows, ValidateStages, ValidateSectorContentSlots, ValidateSectorContentProfile, ValidateSectorContentCandidates, HasPositiveContentCandidate, ValidateLootProgression, ValidateDirectLootTable, ValidateCommonLootPool, ValidateShops, ValidateExplicitShopUnlockRoutes, ValidateShopUnlockNodes, DetectShopUnlockCycles, VisitShopNode, ValidateQuests, ValidateQuestDetails, DetectQuestCycles, VisitQuest, ValidateAudioCues, ValidateBulletPrefabs, ValidateEnemyPrefabs, GetSerializedString, AddError, AddWarning, Open, OnGUI, DrawToolbar, DrawSummary, DrawIssues, RunPreflight
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 0개: 

## Assets/Scripts/Authoring/Editor/ContentTemplateInspectors.cs
- 분류: 에디터; 602행
- 타입: ContentTemplateInspectorUi, ContentTemplateItemInspector : Editor, ContentTemplateQuestInspector : Editor, ContentTemplateStatusEffectInspector : Editor, ContentTemplateAudioCueInspector : Editor, ContentTemplateSectorPresetInspector : Editor, ContentTemplateEncounterInspector : Editor, ContentTemplateQuestLineInspector : Editor, ContentTemplateSectorContentProfileInspector : Editor
- 메서드: IsTemplateAsset, DrawHeader, DrawScript, DrawSection, DrawProperty, DrawRemainingProperties, DuplicateTemplate, OnInspectorGUI, DrawBaseItemProperties, DrawGasMaskProperties, DrawBackpackProperties, DrawArmorProperties, DrawBulletProperties, DrawWeaponProperties
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 0개: 

## Assets/Scripts/Authoring/Editor/CustomBaseEditor.cs
- 분류: 에디터; 33행
- 타입: with, CustomBaseEditor : Editor
- 메서드: 
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 219개: Assets/Editor/ItemMakerWindow.cs; Assets/Editor/LootProgressionEditorWindow.cs; Assets/Editor/QuestDesignTreeWindow.cs; Assets/Editor/ShooterCustomizeStationWindow.cs; Assets/Editor/ShopProgressionTreeWindow.cs; Assets/Scripts/Authoring/Attribute/ReadOnly/ReadOnlyAttribute.cs; Assets/Scripts/Authoring/Editor/AudioCueLibraryWindow.cs; Assets/Scripts/Authoring/Editor/ContentPreflightWindow.cs

## Assets/Scripts/Authoring/Editor/DebugMonitorWindow.cs
- 분류: 에디터; 1694행
- 타입: DebugMonitorWindow : EditorWindow, MonitorScope, CharacterFilter, CharacterRow
- 메서드: OpenWindow, OnEnable, OnDisable, HandleEditorUpdate, OnGUI, DrawToolbar, DrawSummary, DrawStageMapDebug, DrawStageSectorStateTable, ExecuteCoordDebugMove, ExecuteNextStageDebugMove, ResolveDebugPlayerRoot, MoveDebugPlayerToSectorSafePoint, ResolveRoomTypeLabel, FormatStageSectorFlags, AppendStageSectorFlag, SetStageDebugStatus, DrawTable, DrawTableHeader, DrawTableRow, DrawSelectedDetails, DrawGeneralDetails, DrawVisionAndCombatDetails, DrawNavigationDetails, DrawStateMachineDetails, DrawValue, HandleSceneGUI, DrawCharacterOverlay, BuildSceneLabel, RefreshRows, CompareStageSectorStates, TryAddCharacter, BuildStatusSummary, ResolveSector, ResolveStateMachines, MatchesFilter, ContainsIgnoreCase, CompareRows, ResolveDisplayName, ResolveEnemyRole, ResolveAttackMovement, PlanarDistance, FindSelectedRow, SelectRow, BuildStateSummary, ResolvePrimaryStateMachine, FormatStateElapsed, FormatDistance, FormatSector, ResolveTargetName, ResolveVisionResult, ResolveVisionStyle, FormatVision, ResolveAttackStatus, ResolveAttackStyle, FormatHealth, ResolveHealthStyle, FormatAgent, FormatAge, ResolveRowBackground, DrawCell, EnsureStyles
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 5개: Assets/Scripts/Authoring/Editor/BoardChaseAuthoringHubWindow.cs; Assets/Scripts/Authoring/Editor/DebugMonitorWindow.Encounter.cs; Assets/Scripts/Authoring/Editor/DebugMonitorWindow.Operations.cs; Assets/Scripts/Authoring/Editor/DebugMonitorWindow.Quests.cs; Assets/Scripts/Authoring/Editor/DebugMonitorWindow.StatusEffects.cs

## Assets/Scripts/Authoring/Editor/DebugMonitorWindow.Encounter.cs
- 분류: 에디터; 48행
- 타입: DebugMonitorWindow
- 메서드: DrawSelectedEncounterDetails
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 5개: Assets/Scripts/Authoring/Editor/BoardChaseAuthoringHubWindow.cs; Assets/Scripts/Authoring/Editor/DebugMonitorWindow.cs; Assets/Scripts/Authoring/Editor/DebugMonitorWindow.Operations.cs; Assets/Scripts/Authoring/Editor/DebugMonitorWindow.Quests.cs; Assets/Scripts/Authoring/Editor/DebugMonitorWindow.StatusEffects.cs

## Assets/Scripts/Authoring/Editor/DebugMonitorWindow.Operations.cs
- 분류: 에디터; 1266행
- 타입: DebugMonitorWindow, MonitorWorkspaceTab
- 메서드: DrawMonitorWorkspace, DrawStageAndStartHubOperations, DrawCurrentRoomTraitDebugControls, DrawStageDefinitionList, ReturnDebugPlayerToStartHub, DrawItemCatalog, ScanDebugItems, DrawEntityItemOperations, DrawPlayerInventoryRefactorBoundary, DrawSelectedCharacterRuntimeItems, DrawSelectedCombatTestControls, DrawCombatLiveStatus, DrawRuntimeTargetPopup, AddSelectedItemToEntityInventory, DropSelectedItemAtEntityFeet, TryAddEnemyLootItem, IsSameItem, EquipSelectedItemToEntity, ResolveDefaultDurability, DrawShopOperations, ResolvePreferredDebugShop, DrawWarehouseOperations, CountNonEmptyStacks, SetOperationsStatus, DrawOperationsStatus
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 5개: Assets/Scripts/Authoring/Editor/BoardChaseAuthoringHubWindow.cs; Assets/Scripts/Authoring/Editor/DebugMonitorWindow.cs; Assets/Scripts/Authoring/Editor/DebugMonitorWindow.Encounter.cs; Assets/Scripts/Authoring/Editor/DebugMonitorWindow.Quests.cs; Assets/Scripts/Authoring/Editor/DebugMonitorWindow.StatusEffects.cs

## Assets/Scripts/Authoring/Editor/DebugMonitorWindow.Quests.cs
- 분류: 에디터; 138행
- 타입: DebugMonitorWindow
- 메서드: DrawQuestOperations, DrawQuestDebugRow, FindQuestState, CountAccepted, CountCompleted
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 5개: Assets/Scripts/Authoring/Editor/BoardChaseAuthoringHubWindow.cs; Assets/Scripts/Authoring/Editor/DebugMonitorWindow.cs; Assets/Scripts/Authoring/Editor/DebugMonitorWindow.Encounter.cs; Assets/Scripts/Authoring/Editor/DebugMonitorWindow.Operations.cs; Assets/Scripts/Authoring/Editor/DebugMonitorWindow.StatusEffects.cs

## Assets/Scripts/Authoring/Editor/DebugMonitorWindow.StatusEffects.cs
- 분류: 에디터; 156행
- 타입: DebugMonitorWindow
- 메서드: DrawStatusEffectDebugControls, DrawStatusEffectDefinitionPicker, ScanDebugStatusEffectDefinitions
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 5개: Assets/Scripts/Authoring/Editor/BoardChaseAuthoringHubWindow.cs; Assets/Scripts/Authoring/Editor/DebugMonitorWindow.cs; Assets/Scripts/Authoring/Editor/DebugMonitorWindow.Encounter.cs; Assets/Scripts/Authoring/Editor/DebugMonitorWindow.Operations.cs; Assets/Scripts/Authoring/Editor/DebugMonitorWindow.Quests.cs

## Assets/Scripts/Authoring/Editor/Pathway/PathwayEditor.cs
- 분류: 에디터; 170행
- 타입: PathwayEditor : Editor, LIST_MODIFICATION
- 메서드: OnSceneGUI, OnInspectorGUI, OnEnable, OnDisable, DrawHeader, DrawElement, AddItem, RemoveItem, DragItem, ListModified, DoUndo
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 0개: 

## Assets/Scripts/Authoring/Editor/Pathway/PathwayGizmos.cs
- 분류: 에디터; 105행
- 타입: PathwayGizmos
- 메서드: DrawGizmosSelected, DrawLabel, DrawHandlesPath, DrawNavMeshPath, DrawHitPoints
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 1개: Assets/Scripts/Authoring/Editor/Pathway/PathwayEditor.cs

## Assets/Scripts/Authoring/Editor/Pathway/PathwayHandles.cs
- 분류: 에디터; 39행
- 타입: PathwayHandles
- 메서드: DisplayHandles
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 1개: Assets/Scripts/Authoring/Editor/Pathway/PathwayEditor.cs

## Assets/Scripts/Authoring/Editor/Pathway/PathwayNavMesh.cs
- 분류: 에디터; 129행
- 타입: PathwayNavMesh
- 메서드: HasNavMeshAt, GetPathCorners, CopyCorners, UpdateCornersAt, UpdatePath
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 1개: Assets/Scripts/Authoring/Editor/Pathway/PathWayNavMeshUI.cs

## Assets/Scripts/Authoring/Editor/Pathway/PathWayNavMeshUI.cs
- 분류: 에디터; 119행
- 타입: PathWayNavMeshUI
- 메서드: OnInspectorGUI, UpdatePath, UpdatePathAt, RealTime, RestorePath, GeneratePath
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 1개: Assets/Scripts/Authoring/Editor/Pathway/PathwayEditor.cs

## Assets/Scripts/Authoring/Editor/ReplaceObject_ContextualHierarchy/GameObjectPreview.cs
- 분류: 에디터; 125행
- 타입: GameObjectPreview
- 메서드: OnInitialize, CreatePreviewForTarget, RenderInteractivePreview, DrawPreviewTexture, HasRenderableParts
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 2개: Assets/Scripts/Authoring/Editor/ReplaceObject_ContextualHierarchy/PrefabSelectionTreeView.cs; Assets/Scripts/Authoring/Editor/ReplaceObject_ContextualHierarchy/ReplacePrefabSearchPopup.cs

## Assets/Scripts/Authoring/Editor/ReplaceObject_ContextualHierarchy/PrefabSelectionTreeView.cs
- 분류: 에디터; 289행
- 타입: PrefabSelectionTreeView : TreeView
- 메서드: FoldoutOverride, Cleanup, IsRenderable, CachePreview, CanMultiSelect, IsPrefabAsset, DoubleClickedItem, KeyEvent, SelectionChanged, BuildRoot, GetCustomRowHeight, OnGUI, RowGUI, AddFoldersItems, AddPrefabItem
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 1개: Assets/Scripts/Authoring/Editor/ReplaceObject_ContextualHierarchy/ReplacePrefabSearchPopup.cs

## Assets/Scripts/Authoring/Editor/ReplaceObject_ContextualHierarchy/ReplaceContextMenu.cs
- 분류: 에디터; 81행
- 타입: ReplaceContextMenu
- 메서드: OnInitialize, TrackFocusedHierarchy, OnFocusedHierarchyGUI, ReplaceSelectionValidate, ReplaceSelection
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 0개: 

## Assets/Scripts/Authoring/Editor/ReplaceObject_ContextualHierarchy/ReplacePrefabSearchPopup.cs
- 분류: 에디터; 207행
- 타입: ReplacePrefabSearchPopup : EditorWindow, ViewState : ScriptableObject, Styles
- 메서드: Show, Init, OnSelectEntry, OnEnable, OnDisable, Close, SaveState, OnGUI, SetInitialSize, SetSize
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 4개: Assets/Scripts/Authoring/Editor/ReplaceObject_ContextualHierarchy/ReplaceContextMenu.cs; Assets/Scripts/WIP/Legacy/Authoring/Editor/SceneSelector/SceneSelector.cs; Assets/Scripts/WIP/Legacy/Authoring/Editor/SceneSelector/SceneSelector.Data.cs; Assets/Scripts/WIP/Legacy/Authoring/Editor/SceneSelector/SceneSelector.PreferencesWindow.cs

## Assets/Scripts/Authoring/Editor/ReplaceObject_Window/ReplaceTool.cs
- 분류: 에디터; 184행
- 타입: ReplaceTool : EditorWindow, for, ReplaceData : ScriptableObject
- 메서드: InitDataIfNeeded, ShowWindow, OnGUI, OnInspectorUpdate, OnSelectionChange, ReplaceSelectedObjects
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 510개: Assets/Editor/InventorySortButtonMigration.cs; Assets/Editor/ItemCategoryMoveUtility.cs; Assets/Editor/ItemMakerSelection.cs; Assets/Editor/ItemMakerWindow.cs; Assets/Editor/LootProgressionEditorWindow.cs; Assets/Editor/QuestDesignTreeWindow.cs; Assets/Editor/SaveInspectorWindow.cs; Assets/Editor/ShooterCustomizeStationWindow.cs

## Assets/Scripts/Authoring/Editor/SectorMakerWindow.cs
- 분류: 에디터; 2874행
- 타입: SectorMakerWindow : EditorWindow, PlacementMode, RoomReadinessIssue
- 메서드: Open, OnEnable, OnDisable, OnGUI, DrawRoomSelection, DrawRoomReadinessCheck, RunRoomReadinessCheck, AddRoomReadinessIssue, CheckFloorPlacementReadiness, CheckStaticObjectParentReadiness, CheckEnemySpawnReadiness, CheckContentSlotReadiness, CheckActiveRoomMatchesPreset, DrawSimplePlacementControls, DrawPlacementToolButton, SetActiveSectorPreset, OpenActiveSectorPrefab, UseSelectedSectorRuntime, SetActiveSectorRuntime, ClearRoomReadinessCheck, OnSelectionChange, DrawSelectedDataLinkContext, DrawContentAnchorLinkContext, DrawLootableObjectLinkContext, DrawLootTableSummary, DrawLootTableFacts, DrawEnemySpawnPointLinkContext, TryResolveSelectedRoomProfile, DoesActivePresetMatchSelectedRoom, DrawStagesUsingActivePreset, StageUsesActiveNormalPreset, DrawEncounterLootPath, DrawEnemyWaveLootPath, DrawLootSourceSummary, DrawCommonPoolFacts, CountValidPoolItems, TryFindEnemyLootSource, TryFindLootObjectSource, FindChildByName, SaveCurrentRoom, DrawSectorAssemblyFlow, DrawFlowCard, DrawFlowConnector, BuildFlowValue, GetModeTitle, SetMode, DrawActiveLayerControls, DrawActiveLayerSettings, GetActiveLayerInstruction, DrawPlacementGuidance, DrawPlacementModeToggle, DrawFixedStructureSettings, DrawEnemySpawnPointSettings, DrawContentSlotSettings, CreateEnemySpawnPointParent, CreateContentSlotParent, CreateContentProfileAsset, ApplyContentProfileToPreset, DrawTopologySettings, CreateTopologyAsset, DrawTopologyPreview, DrawRotationSettings, HandleSceneGUI, TryGetFloorHit, DrawNoFloorHit, DrawExistingEnemySpawnPoints, GetSpawnPointLabelStyle, DrawExistingContentSlotAnchors, GetContentSlotLabelStyle, GetContentSlotColor, GetContentSlotShortName, HandleKeyboard, DrawPreview, PlaceAt, CreateFixedStructure, CreateEnemySpawnPoint, CreateContentSlotAnchor, HasContentSlotAnchorWithId, TryValidateConfiguration, IsInCurrentStage, TryAutoResolveSectorRuntime, TryAutoResolveContentSectorRuntime, FinalizeCreatedObject, TryCalculateRendererBounds, TryGetSelectedPrefab, InitializeFloorMask, MarkSceneDirty, LayerMaskField
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 0개: 

## Assets/Scripts/Authoring/Editor/ShooterLoadoutSOEditor.cs
- 분류: 에디터; 389행
- 타입: ShooterLoadoutSOEditor : Editor
- 메서드: OnEnable, OnInspectorGUI, DrawAmmoPools, DrawWeaponSetSelector, DrawResolvedWeaponInfo, DrawCandidateValidation, DrawWeaponSetValidation, AddCompatibleBullets, ResolveWeapon, GetWeaponSetIds, GetFirstWeaponSetId, BuildPoolLabel, DrawInventoryStackUsage
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 0개: 

## Assets/Scripts/Authoring/Editor/StageDefinitionRaidEditor.cs
- 분류: 에디터; 40행
- 타입: StageDefinitionRaidEditor : Editor
- 메서드: OnInspectorGUI
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 0개: 

## Assets/Scripts/Authoring/Editor/StandardEnemyConfigPrefabLinkRepair.cs
- 분류: 에디터; 101행
- 타입: StandardEnemyConfigPrefabLinkRepair
- 메서드: RepairOnceForCurrentEditorSession
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 0개: 

## Assets/Scripts/Authoring/MonoBehaviours/EditorColdStartup.cs
- 분류: 일반 소스; 96행
- 타입: EditorColdStartup : MonoBehaviour
- 메서드: Awake, Start, CreateSaveFileIfNotPresent, LoadEventChannel, RequestColdStartupNotification, OnNotifyChannelLoaded
- 직렬화 참조 2개 / 지정 씬 참조 그래프 내 2개: Assets/Prefabs/EditorTool/EditorInitializerMainMenu.prefab; Assets/Prefabs/EditorTool/EditorInitializer_Splatter.prefab
- 타입 이름을 참조하는 소스 0개: 

## Assets/Scripts/Authoring/RendererBoundsColliderFitter.cs
- 분류: 일반 소스; 94행
- 타입: RendererBoundsColliderFitter : MonoBehaviour
- 메서드: FitCollider, TryGetWorldBounds
- 직렬화 참조 10개 / 지정 씬 참조 그래프 내 0개: Assets/Prefabs/GameScene/Entity_WIP/Virus/NamedVirus/Mutarus/Visual_MutarusPurple_Named_Weak.prefab; Assets/Prefabs/GameScene/Entity_WIP/Virus/NormalVirus/Chaser/TestChaser.prefab; Assets/Prefabs/GameScene/Entity_WIP/Virus/NormalVirus/Chaser/VisualRoot_Chaser.prefab; Assets/Prefabs/GameScene/Entity_WIP/Virus/NormalVirus/Chaser/VisualRoot_ChaserUpgrade.prefab; Assets/Prefabs/GameScene/Entity_WIP/Virus/NormalVirus/Roller/VisualRoot_Roller.prefab; Assets/Prefabs/GameScene/Entity_WIP/Virus/NormalVirus/Roller/VisualRoot_Roller_Upgrade.prefab; Assets/Prefabs/GameScene/Entity_WIP/Virus/NormalVirus/Spitter/VisualRoot_Spitter.prefab; Assets/Prefabs/GameScene/Entity_WIP/Virus/NormalVirus/Spitter/VisualRoot_Spitter_ThreeShot.prefab
- 타입 이름을 참조하는 소스 0개: 