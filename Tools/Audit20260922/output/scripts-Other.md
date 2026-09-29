# Other 스크립트 전수 정적 인덱스
검사일: 2026-09-22. 함수/타입/직렬화 연결은 정적 추출이며 실행 검증을 뜻하지 않습니다. 동적 생성, 리플렉션 및 Addressables 간접 참조는 별도 시스템 문서와 함께 확인하세요.

## Assets/Editor/InventorySortButtonMigration.cs
- 분류: 에디터; 101행
- 타입: InventorySortButtonMigration
- 메서드: Initialize, OnSceneOpened, CleanupLoadedScenes, IsLegacyGeneratedButton
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 1개: Assets/Tests/EditMode/Editor/InventorySortUIOwnershipTests.cs

## Assets/Editor/ItemCategoryMoveUtility.cs
- 분류: 에디터; 629행
- 타입: ItemCategoryMoveUtility, ReferenceBinding, MoveRecord, MoveRequest, BatchMoveRecord, fixes
- 메서드: AssetFile, ConfigureWeaponType, ValidateWeaponType, GetRetiredFields, FindIncompatibleReferences, ScanReferences, ReferencesItem, TextContainsGuidReference, InspectHierarchy, InspectReferences, RebindReferences, ResolveFieldType, Move, MoveBatch, RestoreLastMove, RestoreLastMoves, RestoreBatch, Restore, ValidateRestore, RestoreCore, RestoreFiles, WriteRecord, Hash, RequireAssetPath, RequireBackupPath, EnsureFolder
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 5개: Assets/Editor/ItemMakerWindow.cs; Assets/Scripts/Presentation/UI/Gameplay/Vision/PlayerVisionFogOverlayUI.cs; Assets/Tests/EditMode/Editor/ItemHandAndCategoryTests.cs; Assets/Tests/EditMode/Editor/ItemMakerBatchTests.cs; Assets/Tests/EditMode/Editor/ItemMakerWeaponCategoryTests.cs

## Assets/Editor/ItemMakerSelection.cs
- 분류: 에디터; 70행
- 타입: ItemMakerSelection
- 메서드: Contains, Set, Click, Retain, IndexOf
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 2개: Assets/Editor/ItemMakerWindow.cs; Assets/Tests/EditMode/Editor/ItemMakerBatchTests.cs

## Assets/Editor/ItemMakerWindow.cs
- 분류: 에디터; 1881행
- 타입: ItemMakerWindow : EditorWindow, ItemCatalogCategory, identities, WorkbenchTab, ItemTemplate
- 메서드: Open, OnEnable, ApplyCompactWindowLayoutOnce, OnDisable, OnProjectChange, OnSelectionChange, OnGUI, DrawToolbar, DrawCategoryPane, DrawCategoryCard, DrawCreateItemPalette, DrawItemCatalogPane, DrawItemCardGrid, DrawItemCard, HandleItemCardInput, HandleCategoryDrop, ChooseCategoryMove, QueueCategoryMove, ExecuteCategoryMove, RestoreLastCategoryMove, DrawWorkbenchPane, DrawSelectedItemHeader, DrawWorkbenchTabs, DrawWorkbenchTabButton, DrawItemDetails, DrawPreviewImageAuthoring, DrawLootLinks, DrawLootTableSuggestions, DrawLootTableUsage, AddSelectedItemToLootTable, RemoveSelectedItemFromLootTable, CreateItem, DuplicateSelectedItem, RenameSelectedItemAsset, DeleteSelectedItem, RefreshCatalogs, RefreshItems, RefreshLootTables, RefreshLootObjectTypes, TryUseSelectedItem, SelectItem, SetSelectionFromGuids, ApplyCatalogSelection, SyncProjectSelection, SyncPreviewSourceFromItem, GetFilteredItems, CountItems, GetCategory, IsUmbrellaCategory, GetParentCategory, MatchesCategory, GetCategoryLabel, GetCategoryFilterLabel, MatchesSearch, GetItemPreview, GetFriendlyTypeName, GetTemplateLabels, ContainsItem, RebuildPreviewInstance, EnsurePreviewInstance, EnsurePreviewStudio, CreatePreviewLight, RenderPreview, PreparePreviewModel, SetLayerRecursively, DrawPreviewBackdrop, HandlePreviewViewportInput, FramePreviewCamera, CalculatePreviewBounds, EnsurePreviewTexture, CapturePreviewTexture, GenerateAndApplySprite, GetIconFileName, TryEnsureAssetFolder, SanitizeFileName, DisposePreviewStudio
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 1개: Assets/Tests/EditMode/Editor/ItemMakerWeaponCategoryTests.cs

## Assets/Editor/LootProgressionEditorWindow.cs
- 분류: 에디터; 2296행
- 타입: LootProgressionEditorWindow : EditorWindow, AuthoringMode, WorldTypeDetailTab
- 메서드: Open, OpenAndFocusWorldTable, OpenAndSelectSource, OnGUI, DrawConfigOverview, DrawAuthoringModeToolbar, DrawWorldContainerMarketplace, DrawWorldTypeCatalog, DrawWorldTypeCard, DrawSelectedWorldTypeDetail, DrawWorldTypeDetailHeader, DrawWorldTypeVariantGallery, DrawWorldObjectCardGrid, DrawWorldObjectCard, AssignWorldObjectType, FindWorldLootTypes, FindWorldLootObjects, EnsureSelectedWorldLootType, SelectWorldLootType, CountVariants, MatchesWorldTypeSearch, MatchesWorldObjectSearch, GetAssetPreview, CreateWorldLootType, DrawSelectedWorldLootTable, DrawWorldQuickAdd, AddWorldQuickItem, DrawWorldLootTier, CountValidItemEntries, RecordLootTable, DrawGlobalValuableCatalog, ToObjectArray, CountSources, MatchesSourceFilter, DrawSourceRow, DrawSelectedSourceOverview, GetSourceTitle, IsSourceBindingComplete, CountPoolItems, CountStageOverrides, DrawSourcePreview, GetCommonPoolForDisplay, DrawSourceList, DrawLootFlow, DrawLootFlowCanvas, DrawFlowConnector, DrawLootFlowCard, GetRarityColor, DrawSelectedSource, DrawSourceValidationSummary, DrawSourceBinding, DrawLootObjectBindingCheck, DrawQuickAddItem, DrawStageTable, PasteClipboardInto, GetStagePoolLabel, DrawSourceFields, DrawSelectedPool, DrawTier, AddSource, AddRareCacheSource, AddSelectedSource, AddQuickItem, DuplicateSelectedSource, ValidateSelectedSource, ValidatePool, MakeUniqueSourceId, HasOtherSourceWithId, IndexOfSource, CreateConfig, FindFirstConfig, RecordConfig, ContainsItem, MakeObjectId
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 3개: Assets/Editor/ItemMakerWindow.cs; Assets/Scripts/Authoring/Editor/BoardChaseAuthoringHubWindow.cs; Assets/Scripts/Authoring/Editor/SectorMakerWindow.cs

## Assets/Editor/QuestDesignTreeWindow.cs
- 분류: 에디터; 1574행
- 타입: QuestDesignTreeWindow : EditorWindow, upgrade, GraphLayout
- 메서드: Open, OnGUI, DrawQuestLinePane, DrawQuestFlowPane, DrawQuestInspectorPane, DrawPaneHeader, DrawToolbar, DrawLineValidationSummary, DrawQuestLineIdentity, DrawGraph, DrawGraphConnections, DrawOrthogonalConnector, DrawArrowHead, DrawLineRect, DrawQuestNode, BuildQuestRewardPreview, DrawLineQuestList, DrawSelectedQuestEditor, DrawPrerequisiteEditor, DrawOneWayTransitionEditor, DrawRewardEditor, DrawRewardFields, DrawShopUnlockNodePicker, DrawObjectiveEditor, DrawObjectiveFields, DrawQuestValidation, BuildGraphLayout, GetQuestDepth, GetLineQuests, AddQuestToLine, RemoveQuestFromLine, AddPrerequisite, LinkQuestForward, LinkQuestLineInListedOrder, MoveQuestInLine, CreateQuestAssetAndLinkAsNext, AddReward, AddArmorClassReward, WouldCreateCycle, HasPrerequisitePath, AddObjective, CreateQuestLineAsset, CreateQuestAssetAndAddToLine, ApplyObjectiveTemplate, CreateQuestAssetAndLinkAsPrerequisite, CreateQuestAsset, SanitizeAssetName, BuildQuestId, ApplyQuestChanges, DuplicateSelectedQuestToLine, ValidateQuestLine, HasDuplicateQuestId, HasPrerequisiteCycle, CountOutsidePrerequisites, SelectAsset
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 5개: Assets/Scripts/Authoring/Editor/BoardChaseAuthoringHubWindow.cs; Assets/Scripts/Gameplay/Entities/Common/Core/Damageable.cs; Assets/Scripts/Gameplay/Entities/Shooter/Player/PlayerStatsRuntime.cs; Assets/Scripts/Gameplay/Progression/Meta/ScriptableObjects/MetaUpgradeCatalogSO.cs; Assets/Scripts/Presentation/UI/Common/UIMainMenu.cs

## Assets/Editor/SaveInspectorWindow.cs
- 분류: 에디터; 331행
- 타입: SaveInspectorWindow : EditorWindow, PartitionSnapshot, FileSnapshot
- 메서드: Open, OnEnable, OnGUI, RefreshSnapshots, DrawPartition, DrawFileSnapshot, BuildSettingsSummary, BuildProfileSummary, BuildRunSummary, CountMetaLevels, DisplayOrDash, FindSaveSystemAsset, GetBackupFilename
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 0개: 

## Assets/Editor/ShooterCustomizeStationWindow.cs
- 분류: 에디터; 1011행
- 타입: ShooterCustomizeStationWindow : EditorWindow
- 메서드: Open, OnEnable, OnDisable, OnGUI, DrawPaneHeader, DrawWorkflowMap, DrawWorkflowConnector, DrawWorkflowCard, DrawCustomizeValidationSummary, DrawSourceAndAssetSection, DrawPreviewSection, DrawWorkingSetSummary, DrawWorkingSetRow, DrawHeadEquipmentPreviewSection, DrawGasMaskVisualDefinition, DrawManualEditSection, DrawSocketSelectionButton, DrawSaveAndExportSection, DrawExportSection, CreateOrRebuildPreview, LoadCustomizeIntoPreview, SavePreviewToCustomize, CaptureSocketPoses, CaptureDirectSocketAttachments, ResolveDirectChildSourcePrefab, CreateCustomizeAsset, BuildCustomizeId, DuplicateCustomizeAsset, ValidateCurrentCustomize, ExportCustomizePrefab, SelectPreview, SelectTransform, ClearPreview, RebuildPreviewGasMaskVisual, SavePreviewGasMaskTransform, ResolvePreviewPosition
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 1개: Assets/Scripts/Authoring/Editor/BoardChaseAuthoringHubWindow.cs

## Assets/Editor/ShooterMeleeAnimationSetup.cs
- 분류: 에디터; 276행
- 타입: ShooterMeleeAnimationSetup
- 메서드: Rebuild, CreateOrUpdateClip, ConfigureController, SetTransformCurve, HasParameter, FindState, HasAnyStateTriggerTransition, HasTransition
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 0개: 

## Assets/Editor/ShopProgressionTreeWindow.cs
- 분류: 에디터; 947행
- 타입: ShopProgressionTreeWindow : EditorWindow, ShopCatalogView
- 메서드: Open, OnEnable, OnGUI, DrawPaneHeader, DrawConfigSection, DrawEconomySection, DrawCatalogSection, DrawShopCatalogPane, DrawProductCatalogBrowser, DrawProductCard, DrawShopContextInspector, HasSelectedProduct, DrawSelectedProductEditor, DrawProductTradeRequirements, DrawTreeGraphPane, DrawNodeInspectorPane, DrawTreeCanvas, BuildUnlockedProductPreview, TruncateForNode, DrawSelectedNodeEditor, DrawNodePrerequisites, DrawNodeProducts, AddProduct, AddUnlockNode, CreateConfigAsset, ResolveNodeDepth, WouldCreateCycle, HasPrerequisitePath, CurrentNodeId, ContainsString, ResolveProductLabel, DrawArrowHead, FindDefaultConfig
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 0개: 

## Assets/Editor/StructureLootInventoryRuntimeEditor.cs
- 분류: 에디터; 61행
- 타입: StructureLootInventoryRuntimeEditor : Editor
- 메서드: OnInspectorGUI
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 0개: 

## Assets/Scripts/Diagnostics/Runtime/UI/UIRaycastDebugProbe.cs
- 분류: 일반 소스; 119행
- 타입: UIRaycastDebugProbe : MonoBehaviour
- 메서드: Update, LogRaycast, AppendCanvasGroups, GetPath
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 1개: Assets/Scenes/Manager/GamePlay.unity
- 타입 이름을 참조하는 소스 0개: 

## Assets/Scripts/Scenes/BattleTest/BattleTestSceneBootstrap.cs
- 분류: 일반 소스; 160행
- 타입: BattleTestSceneBootstrap : MonoBehaviour
- 메서드: Start, ResolveActors, ValidateEnemyNavMeshAgents, AlertEnemies
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 0개: Assets/Scenes/BattleTestScene.unity
- 타입 이름을 참조하는 소스 0개: 

## Assets/Scripts/Scenes/BattleTest/BattleTestSpawnPoint.cs
- 분류: 일반 소스; 22행
- 타입: BattleTestSpawnPointKind, BattleTestSpawnPoint : MonoBehaviour
- 메서드: 
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 0개: 

## Assets/Scripts/WIP/Legacy/Authoring/Editor/SceneSelector/SceneSelector.ColorSelectorWindow.cs
- 분류: WIP/Legacy 경로; 94행
- 타입: SceneSelector : EditorWindow, ColorSelectorWindow : EditorWindow
- 메서드: Open, Init, OnEnable, OnGUI, DrawMarkers, InitColors
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 5개: Assets/Scripts/Core/SceneFlow/ScriptableObject/GameSceneSO.cs; Assets/Scripts/WIP/Legacy/Authoring/Editor/SceneSelector/SceneSelector.cs; Assets/Scripts/WIP/Legacy/Authoring/Editor/SceneSelector/SceneSelector.Data.cs; Assets/Scripts/WIP/Legacy/Authoring/Editor/SceneSelector/SceneSelector.Helper.cs; Assets/Scripts/WIP/Legacy/Authoring/Editor/SceneSelector/SceneSelector.PreferencesWindow.cs

## Assets/Scripts/WIP/Legacy/Authoring/Editor/SceneSelector/SceneSelector.cs
- 분류: WIP/Legacy 경로; 194행
- 타입: SceneSelector : EditorWindow, IHasCustomMenu
- 메서드: Open, OnEnable, OnDisable, OnGUI, DrawWindow, DrawItems, DrawItem, LoadStorage, SaveStorage, PopulateItems, RemoveEmptyItemsIfRequired, EnsureStyles, OpenPreferences
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 5개: Assets/Scripts/Core/SceneFlow/ScriptableObject/GameSceneSO.cs; Assets/Scripts/WIP/Legacy/Authoring/Editor/SceneSelector/SceneSelector.ColorSelectorWindow.cs; Assets/Scripts/WIP/Legacy/Authoring/Editor/SceneSelector/SceneSelector.Data.cs; Assets/Scripts/WIP/Legacy/Authoring/Editor/SceneSelector/SceneSelector.Helper.cs; Assets/Scripts/WIP/Legacy/Authoring/Editor/SceneSelector/SceneSelector.PreferencesWindow.cs

## Assets/Scripts/WIP/Legacy/Authoring/Editor/SceneSelector/SceneSelector.Data.cs
- 분류: WIP/Legacy 경로; 67행
- 타입: SceneSelector : EditorWindow, Item, Styles, Textures, Storage : ISerializationCallbackReceiver
- 메서드: 
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 94개: Assets/Editor/ItemCategoryMoveUtility.cs; Assets/Editor/ItemMakerWindow.cs; Assets/Editor/LootProgressionEditorWindow.cs; Assets/Editor/QuestDesignTreeWindow.cs; Assets/Editor/ShooterCustomizeStationWindow.cs; Assets/Editor/ShopProgressionTreeWindow.cs; Assets/Scripts/Authoring/Editor/ContentPreflightWindow.cs; Assets/Scripts/Authoring/Editor/ContentTemplateInspectors.cs

## Assets/Scripts/WIP/Legacy/Authoring/Editor/SceneSelector/SceneSelector.Helper.cs
- 분류: WIP/Legacy 경로; 124행
- 타입: KeyValuePairExtension, Helper, ReplaceColor : IDisposable
- 메서드: With, RepaintOnMouseMove, DrawColorMarker, OpenSceneSafe, GetDefaultColor
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 4개: Assets/Scripts/StateMachine/Editor/TransitionTableEditor.cs; Assets/Scripts/WIP/Legacy/Authoring/Editor/SceneSelector/SceneSelector.ColorSelectorWindow.cs; Assets/Scripts/WIP/Legacy/Authoring/Editor/SceneSelector/SceneSelector.cs; Assets/Scripts/WIP/Legacy/Authoring/Editor/SceneSelector/SceneSelector.PreferencesWindow.cs

## Assets/Scripts/WIP/Legacy/Authoring/Editor/SceneSelector/SceneSelector.PreferencesWindow.cs
- 분류: WIP/Legacy 경로; 178행
- 타입: SceneSelector : EditorWindow, PreferencesWindow : EditorWindow, Styles
- 메서드: Open, OnEnable, OnDisable, OnGUI, RepaintAll, Init, CreateReorderableList, DrawWindow, DrawItem, DrawItemBackground, OnReorder, RepaintOwner, EnsureStyles
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 6개: Assets/Scripts/Authoring/Editor/ReplaceObject_ContextualHierarchy/ReplacePrefabSearchPopup.cs; Assets/Scripts/Core/SceneFlow/ScriptableObject/GameSceneSO.cs; Assets/Scripts/WIP/Legacy/Authoring/Editor/SceneSelector/SceneSelector.ColorSelectorWindow.cs; Assets/Scripts/WIP/Legacy/Authoring/Editor/SceneSelector/SceneSelector.cs; Assets/Scripts/WIP/Legacy/Authoring/Editor/SceneSelector/SceneSelector.Data.cs; Assets/Scripts/WIP/Legacy/Authoring/Editor/SceneSelector/SceneSelector.Helper.cs

## Assets/Scripts/WIP/Legacy/Authoring/Editor/ScriptableObjectBrowser/ScriptableObjectBrowser.cs
- 분류: WIP/Legacy 경로; 208행
- 타입: ScriptableObjectBrowser : EditorWindow
- 메서드: OnEnable, OnFocus, LoadData, ShowWindow, OnGUI, DrawTypeButtons, DrawAssetButtons, GetTypes, GetAssets, GetNiceName
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 0개: 

## Assets/Scripts/WIP/Legacy/Gameplay/NamedBoss/BossRewardResolver.cs
- 분류: WIP/Legacy 경로; 55행
- 타입: BossRewardResolver : MonoBehaviour
- 메서드: Reset, Awake, OnEnable, OnDisable, HandleRewardSelected
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 0개: Assets/Prefabs/GameScene/Sector/Stage1/Sector_Named.prefab
- 타입 이름을 참조하는 소스 0개: 

## Assets/Scripts/WIP/Legacy/Gameplay/NotUsing/Board/OcuppancyMinuteJudge.cs
- 분류: WIP/Legacy 경로; 113행
- 타입: OccupancyMinuteJudge : MonoBehaviour
- 메서드: StartJudge
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 0개: 

## Assets/Scripts/WIP/Legacy/Gameplay/NotUsing/Board/RotatingSpinner.cs
- 분류: WIP/Legacy 경로; 112행
- 타입: RotatingSpinner : MonoBehaviour
- 메서드: SetReverse, ToggleDirection
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 0개: 

## Assets/Scripts/WIP/Legacy/Gameplay/NotUsing/Board/WindowWall.cs
- 분류: WIP/Legacy 경로; 90행
- 타입: WindowWall : MonoBehaviour
- 메서드: SetWindowSmooth
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 0개: Assets/Scenes/City.unity
- 타입 이름을 참조하는 소스 0개: 

## Assets/Scripts/WIP/Legacy/Gameplay/Player/AmmoSlotCompatibility/PlayerInventoryRuntime.LegacyAmmoSlotCompatibility.cs
- 분류: WIP/Legacy 경로; 384행
- 타입: PlayerInventoryRuntime
- 메서드: TryEquipInventoryBulletToAmmoSlot, TryGetAmmoSlotSnapshot, TryUnequipAmmoSlotToInventorySlot, TrySplitAmmoSlotToInventory, TrySwapAmmoSlots, TryDiscardAmmoSlot, TryRemoveAmmoSlot, TryEquipExternalBulletToAmmoSlot, TrySellAmmoSlot
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 43개: Assets/Scripts/Authoring/Editor/DebugMonitorWindow.cs; Assets/Scripts/Authoring/Editor/DebugMonitorWindow.Operations.cs; Assets/Scripts/Core/Events/ScriptableObject/PlayerInventoryRuntimeReadyEventChannelSO.cs; Assets/Scripts/Core/Persistence/PlayerRunStatePersistence.cs; Assets/Scripts/Core/Persistence/RunLifecycleCoordinator.cs; Assets/Scripts/Gameplay/Entities/Shooter/Common/Core/ShooterEquipmentController.cs; Assets/Scripts/Gameplay/Entities/Shooter/Enemy/Common/EnemyLootInventoryRuntime.cs; Assets/Scripts/Gameplay/Entities/Shooter/Player/Inventory/PlayerBackpackDropInput.cs

## Assets/Scripts/WIP/Legacy/Gameplay/Player/ExistingWIP/PlayerWorldRangeRing.cs
- 분류: WIP/Legacy 경로; 672행
- 타입: PlayerWorldRangeRing : MonoBehaviour
- 메서드: Reset, Awake, OnEnable, OnDisable, LateUpdate, ResolveRefs, ConfigureLineRenderer, RefreshRing, ResolveWorldRadius, OnSoundStimulus, ShouldShowSoundIndicator, UpdateSoundIndicatorLifetime, UpdateSoundIndicatorTransform, UpdateSoundIndicatorRotation, HideSoundIndicator, IsSoundIndicatorVisible, SetSoundIndicatorVisible, SetSoundIndicatorColor, IsSourceVisible, IsOwnSoundSource, FindChildByName, LogDebug
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 0개: 

## Assets/Scripts/WIP/Legacy/Gameplay/Player/VSplatterAimUI.cs
- 분류: WIP/Legacy 경로; 293행
- 타입: VSplatterAimUI : MonoBehaviour
- 메서드: Reset, Awake, OnEnable, OnDisable, Update, EnsureCursorCanvasRoot, EnsureCursorUI, CacheCursorUIComponents, UpdateCursorUIPosition, RefreshCursorUI, SetCursorVisible, CacheHitCrosshair, HandleAttackHitConfirmed, UpdateHitCrosshair, HideHitCrosshairImmediate, FindChildRecursive
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 0개: 

## Assets/Scripts/WIP/Legacy/Gameplay/Player/VSplatterWeaponHolder.cs
- 분류: WIP/Legacy 경로; 10행
- 타입: VSplatterWeaponHolder : EntityWeaponHolder
- 메서드: 
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 0개: 

## Assets/Scripts/WIP/Legacy/Gameplay/Sectors/SectorObjectConfigSO.cs
- 분류: WIP/Legacy 경로; 13행
- 타입: SectorObjectConfigSO : StageObjectConfigSO
- 메서드: 
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 0개: 

## Assets/Scripts/WIP/Legacy/Gameplay/Sectors/StageSectorUnlockSO.cs
- 분류: WIP/Legacy 경로; 64행
- 타입: StageSectorUnlockSO : ScriptableObject, StageUnlockStep
- 메서드: TryGetStep, GetMaxStageIndex
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 0개: 

## Assets/Scripts/WIP/Legacy/MainScene/Achievement/AchievementManager.cs
- 분류: WIP/Legacy 경로; 124행
- 타입: AchievementManager : MonoBehaviour
- 메서드: Open, Refresh
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 0개: 

## Assets/Scripts/WIP/Legacy/MainScene/Achievement/AchievementRewardEntry.cs
- 분류: WIP/Legacy 경로; 66행
- 타입: AchievementRewardEntry : MonoBehaviour
- 메서드: Bind
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 0개: Assets/Prefabs/MainScene/Reward/RewardEntryPrefab.prefab
- 타입 이름을 참조하는 소스 1개: Assets/Scripts/WIP/Legacy/MainScene/Achievement/AchievementManager.cs

## Assets/Scripts/WIP/Legacy/MainScene/Achievement/Achievements.cs
- 분류: WIP/Legacy 경로; 50행
- 타입: UnlockType, Achievement, Achievements
- 메서드: 
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 10개: Assets/Scripts/Gameplay/Progression/ProgressManager.cs; Assets/Scripts/Presentation/UI/Common/UIMenuManager.cs; Assets/Scripts/Presentation/UI/Common/UIRuntimeLocalization.cs; Assets/Scripts/Presentation/UI/HUD/Legacy/AchievementPopupManager.cs; Assets/Scripts/WIP/Legacy/MainScene/Achievement/AchievementManager.cs; Assets/Scripts/WIP/Legacy/MainScene/Achievement/AchievementRewardEntry.cs; Assets/Scripts/WIP/Legacy/MainScene/Achievement/CardRewardSO.cs; Assets/Scripts/WIP/Legacy/MainScene/Achievement/RewardDB.cs

## Assets/Scripts/WIP/Legacy/MainScene/Achievement/CardRewardSO.cs
- 분류: WIP/Legacy 경로; 29행
- 타입: CardRewardSO : RewardSO
- 메서드: OnEnable, Grant
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 0개: 

## Assets/Scripts/WIP/Legacy/MainScene/Achievement/RewardDB.cs
- 분류: WIP/Legacy 경로; 87행
- 타입: RewardDB
- 메서드: EnsureLoaded, Get, GetIcon, GetTitle, GrantVisualOrRuntime, SyncFrom
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 8개: Assets/Scripts/Gameplay/Progression/ProgressManager.cs; Assets/Scripts/Presentation/UI/HUD/Legacy/AchievementPopupManager.cs; Assets/Scripts/WIP/Legacy/MainScene/Achievement/AchievementManager.cs; Assets/Scripts/WIP/Legacy/MainScene/Achievement/AchievementRewardEntry.cs; Assets/Scripts/WIP/Legacy/MainScene/Achievement/Achievements.cs; Assets/Scripts/WIP/Legacy/MainScene/Achievement/RewardSO.cs; Assets/Scripts/WIP/Legacy/MainScene/Skininventory/SkinInventoryManager.cs; Assets/Scripts/WIP/Legacy/Presentation/SurvivalSuccessManager.cs

## Assets/Scripts/WIP/Legacy/MainScene/Achievement/RewardSO.cs
- 분류: WIP/Legacy 경로; 42행
- 타입: RewardType, RewardSO : ScriptableObject
- 메서드: Grant
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 11개: Assets/Editor/QuestDesignTreeWindow.cs; Assets/Scripts/Authoring/Editor/ContentPreflightWindow.cs; Assets/Scripts/Gameplay/Progression/ProgressManager.cs; Assets/Scripts/Gameplay/Progression/Quests/QuestDefinitionSO.cs; Assets/Scripts/Gameplay/Progression/Quests/QuestRuntime.cs; Assets/Scripts/WIP/Legacy/MainScene/Achievement/AchievementManager.cs; Assets/Scripts/WIP/Legacy/MainScene/Achievement/AchievementRewardEntry.cs; Assets/Scripts/WIP/Legacy/MainScene/Achievement/Achievements.cs

## Assets/Scripts/WIP/Legacy/MainScene/Achievement/SkinRewardSO.cs
- 분류: WIP/Legacy 경로; 45행
- 타입: SkinRewardSO : RewardSO
- 메서드: OnEnable, Grant
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 2개: Assets/Scripts/WIP/Legacy/MainScene/Achievement/RewardDB.cs; Assets/Scripts/WIP/Legacy/MainScene/Skininventory/SkinInventoryManager.cs

## Assets/Scripts/WIP/Legacy/MainScene/Skininventory/SkinInventoryManager.cs
- 분류: WIP/Legacy 경로; 108행
- 타입: SkinInventoryManager : MonoBehaviour
- 메서드: Open
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 0개: 

## Assets/Scripts/WIP/Legacy/Presentation/CooldownHUD.cs
- 분류: WIP/Legacy 경로; 47행
- 타입: CooldownHUD : MonoBehaviour
- 메서드: 
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 0개: 

## Assets/Scripts/WIP/Legacy/Presentation/SurvivalSuccessManager.cs
- 분류: WIP/Legacy 경로; 173행
- 타입: SurvivalSuccessManager : MonoBehaviour
- 메서드: TriggerSuccess
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 1개: Assets/Scripts/WIP/Legacy/Presentation/SurvivalTimerHUD.cs

## Assets/Scripts/WIP/Legacy/Presentation/SurvivalTimerHUD.cs
- 분류: WIP/Legacy 경로; 75행
- 타입: SurvivalTimerHUD : MonoBehaviour
- 메서드: PauseClock, ElapsedSeconds
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 1개: Assets/Scripts/WIP/Legacy/Gameplay/NotUsing/Board/OcuppancyMinuteJudge.cs

## Assets/Scripts/WIP/Legacy/Presentation/ZoneHitHUD.cs
- 분류: WIP/Legacy 경로; 135행
- 타입: ZoneHitHUD : MonoBehaviour
- 메서드: 
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 0개: 

## Assets/Tests/EditMode/Editor/AudioGroundworkTests.cs
- 분류: 테스트; 235행
- 타입: AudioGroundworkTests
- 메서드: SetUp, TearDown, UiCommand_QueuesOneResultInsteadOfButtonClick, QuestOverride_ReplacesGenericClick_AndInvalidOverrideFallsBack, EmptyOrLoopingUiCue_IsNotPlayed, Button_BindsOnce_AndIgnoresRightClick, Search_OneLoop_StopsOnce_OnCancelOrCompletion, Search_PauseStopsLoop_ResumeRestarts_AndDisableCleansUp, Search_DoesNotPlayForReloadOrOtherCasts, Npc_RepeatedOpenCloseAndLineReplacement_DoNotStackVoices, AuthoredMixer_ContainsExpectedGroupsAndParameters, MakeUi, MakeSearch, Snapshot, Cue, Go, Get, Set, Call
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 0개: 

## Assets/Tests/EditMode/Editor/InventorySortUIOwnershipTests.cs
- 분류: 테스트; 141행
- 타입: InventorySortUIOwnershipTests
- 메서드: Cleanup, PrefabOwnsTwoButtonPairsWithCorrectPersistentSortArguments, GamePlayCanvasInheritsFourButtonsFromInventoryPrefab, RunSceneContainsNoSerializedSortButtonsOrInventoryPanel, MissingButtonsAreNeverCreatedByRefreshOrValidation, EditorPreviewDoesNotModifyPersistentPrefabHierarchy, SortRefreshDoesNotTouchAnotherPanelsSection, CleanupRecognizesOnlyExactLegacyButtonShape, Go, Invoke
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 0개: 

## Assets/Tests/EditMode/Editor/ItemHandAndCategoryTests.cs
- 분류: 테스트; 248행
- 타입: ItemHandAndCategoryTests
- 메서드: SetUp, TearDown, WorldVisualKeepsSavedTransformAndDoesNotRequireWeaponView, WorldVisualOffsetsAffectMountWithoutOverwritingModel, HeldDisplayDisablesOnlyClonedPhysics, OverrideKeepsLegacyAbsoluteOffsetsAndThrowableNeverUsesDisplayFallback, MissingVisualOrSocketDoesNotInstantiateAnything, TypeConversionPreservesIdentityCommonFieldsAndLootReferenceThenRestores, IncompatibleTypedQuestReferenceBlocksConversion, RestoreDoesNotOverwriteEditsMadeAfterTheMove, SameTypeMovePreservesSpecializedSubtype, UnrelatedMalformedPrefabGuidDoesNotRequireUnityDependencyParsing, TextReferenceFilterKeepsDirectReferencesEvenWithOtherMalformedGuids, NonYamlAssetsStillRequireDependencyDatabaseLookup, Prop, Set
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 0개: 

## Assets/Tests/EditMode/Editor/ItemMakerBatchTests.cs
- 분류: 테스트; 163행
- 타입: ItemMakerBatchTests
- 메서드: SetUp, TearDown, ControlClickTogglesAndDraggingSelectedCardKeepsTheGroup, ShiftSelectsVisibleRangeAndFilteringRemovesHiddenSelections, BatchMoveAndRestorePreserveBothIdentitiesAndLoadedLootReferences, CollisionInLaterItemPreventsEveryMove, IdenticalNamesInsideSelectionAreBlockedWithoutOverwriting, OneChangedItemBlocksTheWholeBatchRestore, IncompatibleLaterItemBlocksEarlierValidItem, Request
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 0개: 

## Assets/Tests/EditMode/Editor/ItemMakerWeaponCategoryTests.cs
- 분류: 테스트; 212행
- 타입: ItemMakerWeaponCategoryTests
- 메서드: SetUp, TearDown, FirearmsAppearInTheirChildAndParent, BulletsUseTheirExistingFamily, DedicatedMeleeRemainsWipAndConsumableHierarchyIsUnchanged, SidebarContainsEveryCategoryOnceAndChildrenFollowTheirParent, EveryCreateTemplateProducesItsDisplayedCategory, SameClassGunReclassificationAndRestorePreserveGuidAndTuning, ShotgunConversionAndRestorePreserveOriginalPelletSettings, ChangingToShotgunUsesTheActualShotgunClass, InvalidFixedWeaponTypeStopsWholeBatchBeforeAnyChange, BulletFamilyConversionKeepsDamageAndRestores, Request, SetFloat, CategoryEnum, Category, Matches
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 0개: 

## Assets/Tests/EditMode/Editor/ObstacleBulletImpactAudioTests.cs
- 분류: 테스트; 233행
- 타입: ObstacleBulletImpactAudioTests
- 메서드: SetUp, TearDown, BallisticAmmoRouting_ExcludesRocketsAndSpecialProjectiles, ChildCollider_UsesParentSurfaceAndExactContactPoint, ShotgunOverlap_IsLimitedPerObstacleAndCanBeDisabled, MissingEmptyOrLoopingSurfaceCue_UsesPlayableDefault, UntaggedOrDisabledTargetsAndMissingAudioListener_DoNotEmit, FailedPlayback_DoesNotConsumeObstacleCooldown, AuthoredCatalog_HasPlayableOneShotForEverySurface, ObstacleFolderPrefabs_HaveSurfaceAndBallisticCollisionLayer, MakeCue, MakeSurface, SetField, RouterField
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 0개: 

## Assets/Tests/EditMode/Editor/PlayerEmergencyMeleeTests.cs
- 분류: 테스트; 197행
- 타입: PlayerEmergencyMeleeTests
- 메서드: TearDown, EmptySlotAlwaysAllowsFists, GunOnlyAllowsFallbackWithoutFireableAmmo, LegacyPlayerMeleeWeaponIsNotAnEmergencyAttack, UninitializedAmmoIsNotMistakenForAnEmptyGun, ActualMagazineAndReserveDecideWhetherGunBashIsAvailable, PlayerDamageOverridesLegacyProfileDamageAndKeepsTiming, FixedDamageIgnoresDamageReductionAndAmplification, FixedDamageIgnoresArmorWithoutBreakingOrdinaryDamage, FixedDamageDoesNotBypassInvulnerabilityOrDeath, NegativeAndZeroDamageDoNotProduceHits, ConfigAndRuntimeHaveIndependentMeleeDamage, Target, Invoke, Set
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 0개: 

## Assets/Tests/EditMode/Editor/RaidMapFoundationTests.cs
- 분류: 테스트; 126행
- 타입: RaidMapFoundationTests
- 메서드: ThousandSeedsHaveValidDistinctReachableSpecialRooms, GenerationIsDeterministicAndBossRollHasCorrectBounds, ContentWeightsDoNotChangeSpecialRoomPlacementOrBossRoll, InvalidSizesAreRejected, ImpossibleDistanceFailsWithoutRetryAndExactDistanceSucceeds, StoredLayoutAndStateRoundTripWithoutReroll, OldRunMigrationDoesNotInventAMapOrLosePlayerState, UnknownGenerationOrCorruptGraphCannotSilentlyReroll, ExistingFullGridKeepsLegacyEdgeExitAndSerializedTypes
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 0개: 

## Assets/Tests/EditMode/Editor/RaidRuntimeContractTests.cs
- 분류: 테스트; 313행
- 타입: RaidRuntimeContractTests
- 메서드: ShieldVestAndBaseRollIndependentlyAndOnlyAbsorbedDamageWearsEachLayer, EqualPenetrationAndBrokenShieldPassFullDamage, ShieldBreakAffectsFollowingHitAndFixedMeleeNeverWearsIt, InvalidOrPausedClockDoesNotAdvance, ClockAndGasSurviveReloadAndStopAfterExtraction, SortPreservesStackInstancesConditionsBoundariesAndStableRarityOrder, CompactKeepsOriginalOrderWithoutMerging, GeneratorRejectsNoBatteryConsumesOnceAndRestoresPower, RolledEmptyContainerRemainsEmptyAndUnlockedAfterRestore, ExitRulesAreIndependentOfBossAndRejectDuplicatesAndWrongCoordinates, ContainerRestorePreservesSparseSlotsAndItemCondition, Es3RoundTripPreservesRaidAndContainerState, PublicLandmarksExistBeforeAnyOccupancyPublishesWithoutRevealingHatch, Go, Stack, Set
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 0개: 

## Assets/Tests/EditMode/Editor/RunLifecycleAndSaveTests.cs
- 분류: 테스트; 193행
- 타입: RunLifecycleAndSaveTests
- 메서드: ApplicationPauseRules_SuspendOnlyActiveRunsAndRestoreTheRecordedPhase, RunLifecycleRules_ProtectStageBoundsAndDuplicateDeathHandling, EnsureRuntimeDefaults_CleansBrokenEntriesWithoutRemovingValidPersistentProgress, LoadFromJson_RepairsLegacyNullCollectionsAndNegativeProfileValues, PartitionMigrations_UpgradeVersionZeroAndPreserveEachPartitionBoundary, PartitionMigrations_RejectFutureSchemaWithoutMutatingData
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 0개: 