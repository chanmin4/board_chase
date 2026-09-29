# StateMachine 스크립트 전수 정적 인덱스
검사일: 2026-09-22. 함수/타입/직렬화 연결은 정적 추출이며 실행 검증을 뜻하지 않습니다. 동적 생성, 리플렉션 및 Addressables 간접 참조는 별도 시스템 문서와 함께 확인하세요.

## Assets/Scripts/StateMachine/Core/ChildStateMachine.cs
- 분류: 일반 소스; 16행
- 타입: ChildStateMachine : StateMachine
- 메서드: 
- 직렬화 참조 10개 / 지정 씬 참조 그래프 내 2개: Assets/Prefabs/GameScene/Entity_WIP/Virus/NamedVirus/Mutarus/Mutarus_NamedVirus.prefab; Assets/Prefabs/GameScene/Entity_WIP/Virus/NormalVirus/Chaser/Chaser.prefab; Assets/Prefabs/GameScene/Entity_WIP/Virus/NormalVirus/Chaser/Chaser_Upgrade.prefab; Assets/Prefabs/GameScene/Entity_WIP/Virus/NormalVirus/Roller/Roller.prefab; Assets/Prefabs/GameScene/Entity_WIP/Virus/NormalVirus/Spitter/Spitter_ThreeShot_Virus.prefab; Assets/Prefabs/GameScene/Entity_WIP/Virus/NormalVirus/Spitter/Spitter_Virus.prefab; Assets/Prefabs/GameScene/Entity_WIP/Virus/NormalVirus/Worm/Worm.prefab; Assets/Prefabs/GameScene/Entity_WIP/Virus/NormalVirus/Worm/Worm_Upgrade.prefab
- 타입 이름을 참조하는 소스 2개: Assets/Scripts/Gameplay/Entities/Creature/StateMachineCoordinator.cs; Assets/Scripts/Gameplay/Entities/Creature/StateMachineGroupCoordinator.cs

## Assets/Scripts/StateMachine/Core/IStateComponent.cs
- 분류: 일반 소스; 22행
- 타입: IStateComponent
- 메서드: 
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 4개: Assets/Scripts/StateMachine/Core/State.cs; Assets/Scripts/StateMachine/Core/StateAction.cs; Assets/Scripts/StateMachine/Core/StateCondition.cs; Assets/Scripts/StateMachine/Core/StateTransition.cs

## Assets/Scripts/StateMachine/Core/ParentStateMachine.cs
- 분류: 일반 소스; 16행
- 타입: ParentStateMachine : StateMachine
- 메서드: 
- 직렬화 참조 10개 / 지정 씬 참조 그래프 내 2개: Assets/Prefabs/GameScene/Entity_WIP/Virus/NamedVirus/Mutarus/Mutarus_NamedVirus.prefab; Assets/Prefabs/GameScene/Entity_WIP/Virus/NormalVirus/Chaser/Chaser.prefab; Assets/Prefabs/GameScene/Entity_WIP/Virus/NormalVirus/Chaser/Chaser_Upgrade.prefab; Assets/Prefabs/GameScene/Entity_WIP/Virus/NormalVirus/Roller/Roller.prefab; Assets/Prefabs/GameScene/Entity_WIP/Virus/NormalVirus/Spitter/Spitter_ThreeShot_Virus.prefab; Assets/Prefabs/GameScene/Entity_WIP/Virus/NormalVirus/Spitter/Spitter_Virus.prefab; Assets/Prefabs/GameScene/Entity_WIP/Virus/NormalVirus/Worm/Worm.prefab; Assets/Prefabs/GameScene/Entity_WIP/Virus/NormalVirus/Worm/Worm_Upgrade.prefab
- 타입 이름을 참조하는 소스 3개: Assets/Scripts/Authoring/Editor/DebugMonitorWindow.cs; Assets/Scripts/Gameplay/Entities/Creature/StateMachineCoordinator.cs; Assets/Scripts/Gameplay/Entities/Creature/StateMachineGroupCoordinator.cs

## Assets/Scripts/StateMachine/Core/State.cs
- 분류: 일반 소스; 87행
- 타입: State
- 메서드: OnStateEnter, OnUpdate, OnStateExit, TryGetTransition
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 153개: Assets/Scripts/Authoring/Editor/DebugMonitorWindow.cs; Assets/Scripts/Authoring/Editor/DebugMonitorWindow.Encounter.cs; Assets/Scripts/Authoring/Editor/DebugMonitorWindow.Operations.cs; Assets/Scripts/Authoring/Editor/DebugMonitorWindow.Quests.cs; Assets/Scripts/Core/Events/ScriptableObject/SectorStateManagerReadyEventChannelSO.cs; Assets/Scripts/Core/Persistence/PlayerRunStatePersistence.cs; Assets/Scripts/Core/Persistence/RunLifecycleCoordinator.cs; Assets/Scripts/Core/SceneFlow/SceneTransitioner.cs

## Assets/Scripts/StateMachine/Core/StateAction.cs
- 분류: 일반 소스; 47행
- 타입: StateAction : IStateComponent, is, as, SpecificMoment
- 메서드: OnUpdate, Awake, OnStateEnter, OnStateExit
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 456개: Assets/Editor/InventorySortButtonMigration.cs; Assets/Editor/ItemCategoryMoveUtility.cs; Assets/Editor/ItemMakerWindow.cs; Assets/Editor/LootProgressionEditorWindow.cs; Assets/Editor/QuestDesignTreeWindow.cs; Assets/Editor/SaveInspectorWindow.cs; Assets/Editor/ShooterCustomizeStationWindow.cs; Assets/Editor/ShooterMeleeAnimationSetup.cs

## Assets/Scripts/StateMachine/Core/StateCondition.cs
- 분류: 일반 소스; 89행
- 타입: Condition : IStateComponent, StateCondition
- 메서드: Statement, GetStatement, ClearStatementCache, Awake, OnStateEnter, OnStateExit, IsMet
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 50개: Assets/Scripts/Gameplay/Entities/Common/StateMachine/Conditions/AlwaysFalseConditionSO.cs; Assets/Scripts/Gameplay/Entities/Common/StateMachine/Conditions/HasHitHeadConditionSO.cs; Assets/Scripts/Gameplay/Entities/Common/StateMachine/Conditions/HasPendingKnockbackConditionSO.cs; Assets/Scripts/Gameplay/Entities/Common/StateMachine/Conditions/HasReceivedEventSO.cs; Assets/Scripts/Gameplay/Entities/Common/StateMachine/Conditions/IsActuallyMovingConditionSO.cs; Assets/Scripts/Gameplay/Entities/Common/StateMachine/Conditions/IsCharacterControllerGroundedConditionSO.cs; Assets/Scripts/Gameplay/Entities/Common/StateMachine/Conditions/IsDeadConditionSO.cs; Assets/Scripts/Gameplay/Entities/Common/StateMachine/Conditions/IsEntityGettingHitSO.cs

## Assets/Scripts/StateMachine/Core/StateMachine.cs
- 분류: 일반 소스; 217행
- 타입: StateMachine : MonoBehaviour
- 메서드: Awake, OnEnable, OnAfterAssemblyReload, OnDisable, Start, ResetToInitialState, EnterCurrentState, Update, Transition, GetStateName
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 157개: Assets/Scripts/Authoring/Editor/DebugMonitorWindow.cs; Assets/Scripts/Gameplay/Entities/Common/StateMachine/Actions/AnimatorParameterActionSO.cs; Assets/Scripts/Gameplay/Entities/Common/StateMachine/Actions/ApplyMovementVectorActionSO.cs; Assets/Scripts/Gameplay/Entities/Common/StateMachine/Actions/AscendActionSO.cs; Assets/Scripts/Gameplay/Entities/Common/StateMachine/Actions/CancelActiveKnockbackActionSO.cs; Assets/Scripts/Gameplay/Entities/Common/StateMachine/Actions/CancelDashActionSO.cs; Assets/Scripts/Gameplay/Entities/Common/StateMachine/Actions/ChangeGameStateActionSO.cs; Assets/Scripts/Gameplay/Entities/Common/StateMachine/Actions/ChasingTargetActionSO.cs

## Assets/Scripts/StateMachine/Core/StateTransition.cs
- 분류: 일반 소스; 89행
- 타입: StateTransition : IStateComponent
- 메서드: Init, TryGetTransiton, OnStateEnter, OnStateExit, ShouldTransition, ClearConditionsCache
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 3개: Assets/Scripts/StateMachine/Core/State.cs; Assets/Scripts/StateMachine/ScriptableObjects/StateSO.cs; Assets/Scripts/StateMachine/ScriptableObjects/TransitionTableSO.cs

## Assets/Scripts/StateMachine/Debugging/StateMachineDebugger.cs
- 분류: 일반 소스; 135행
- 타입: StateMachineDebugger
- 메서드: Awake, TransitionEvaluationBegin, TransitionConditionResult, TransitionEvaluationEnd, LogActions, PrintDebugLog
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 1개: Assets/Scripts/StateMachine/Core/StateMachine.cs

## Assets/Scripts/StateMachine/Editor/HFSMTransitionGraphWindow.cs
- 분류: 에디터; 653행
- 타입: HFSMTransitionGraphWindow : EditorWindow
- 메서드: Open, OnEnable, OnDisable, OnSelectionChange, OnGUI, DrawWorkspaceHeader, DrawToolbar, DrawGraph, SetEnemyShooterBaseTable, IsSharedEnemyShooterBaseTable, DrawStateBrowser, DrawInspector, RebuildLayout, AddState, GetOutgoingCount, HandleGraphPan, DrawGrid, SetTable, EnsureSerializedTable, CommitTableChanges, RefreshGraph
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 0개: 

## Assets/Scripts/StateMachine/Editor/StateEditor.cs
- 분류: 에디터; 121행
- 타입: StateEditor : UnityEditor.Editor
- 메서드: OnEnable, OnDisable, OnInspectorGUI, DoUndo, SetupActionsList
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 1개: Assets/Scripts/StateMachine/Editor/TransitionTableEditor.cs

## Assets/Scripts/StateMachine/Editor/Templates/ScriptTemplates.cs
- 분류: 에디터; 72행
- 타입: ScriptTemplates, DoCreateStateMachineScriptAsset : EndNameEditAction
- 메서드: CreateActionScript, CreateConditionScript, Action
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 0개: 

## Assets/Scripts/StateMachine/Editor/TransitionTableEditor.cs
- 분류: 에디터; 410행
- 타입: TransitionTableEditor : UnityEditor.Editor, to
- 메서드: OnEnable, OnDisable, Reset, OnInspectorGUI, StateEditorGUI, TransitionTableGUI, DisplayStateEditor, ReorderState, AddTransition, ReorderTransition, RemoveTransition, GetStateTransitions, CopyConditions, TryGetExistingTransition, GroupByFromState, ApplyModifications
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 538개: Assets/Editor/InventorySortButtonMigration.cs; Assets/Editor/ItemCategoryMoveUtility.cs; Assets/Editor/ItemMakerWindow.cs; Assets/Editor/LootProgressionEditorWindow.cs; Assets/Editor/QuestDesignTreeWindow.cs; Assets/Editor/ShooterCustomizeStationWindow.cs; Assets/Editor/ShooterMeleeAnimationSetup.cs; Assets/Editor/ShopProgressionTreeWindow.cs

## Assets/Scripts/StateMachine/Editor/TransitionTableEditorWindow.cs
- 분류: 에디터; 178행
- 타입: TransitionTableEditorWindow : EditorWindow
- 메서드: Display, OnEnable, OnDisable, OnPlayModeStateChanged, OnFocus, OnLostFocus, Update, CreateListView, OnListSelectionChanged, FindAssets
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 0개: 

## Assets/Scripts/StateMachine/Editor/Utilities/AddTransitionHelper.cs
- 분류: 에디터; 186행
- 타입: AddTransitionHelper : IDisposable, TransitionItemSO : ScriptableObject
- 메서드: Display, Dispose, SetupConditionsList
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 1개: Assets/Scripts/StateMachine/Editor/TransitionTableEditor.cs

## Assets/Scripts/StateMachine/Editor/Utilities/ContentStyle.cs
- 분류: 에디터; 67행
- 타입: ContentStyle
- 메서드: Initialize
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 4개: Assets/Scripts/StateMachine/Editor/StateEditor.cs; Assets/Scripts/StateMachine/Editor/TransitionTableEditor.cs; Assets/Scripts/StateMachine/Editor/Utilities/AddTransitionHelper.cs; Assets/Scripts/StateMachine/Editor/Utilities/TransitionDisplayHelper.cs

## Assets/Scripts/StateMachine/Editor/Utilities/InitOnlyAttributeDrawer.cs
- 분류: 에디터; 76행
- 타입: InitOnlyAttributeDrawer : PropertyDrawer
- 메서드: OnGUI, GetPropertyHeight
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 0개: 

## Assets/Scripts/StateMachine/Editor/Utilities/SerializedTransition.cs
- 분류: 에디터; 55행
- 타입: SerializedTransition
- 메서드: ClearProperties
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 3개: Assets/Scripts/StateMachine/Editor/TransitionTableEditor.cs; Assets/Scripts/StateMachine/Editor/Utilities/AddTransitionHelper.cs; Assets/Scripts/StateMachine/Editor/Utilities/TransitionDisplayHelper.cs

## Assets/Scripts/StateMachine/Editor/Utilities/TransitionDisplayHelper.cs
- 분류: 에디터; 184행
- 타입: TransitionDisplayHelper
- 메서드: Display, SetupConditionsList
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 1개: Assets/Scripts/StateMachine/Editor/TransitionTableEditor.cs

## Assets/Scripts/StateMachine/ScriptableObjects/DescriptionSMActionBaseSO.cs
- 분류: 일반 소스; 19행
- 타입: for, DescriptionSMActionBaseSO : ScriptableObject
- 메서드: 
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 511개: Assets/Editor/InventorySortButtonMigration.cs; Assets/Editor/ItemCategoryMoveUtility.cs; Assets/Editor/ItemMakerSelection.cs; Assets/Editor/ItemMakerWindow.cs; Assets/Editor/LootProgressionEditorWindow.cs; Assets/Editor/QuestDesignTreeWindow.cs; Assets/Editor/SaveInspectorWindow.cs; Assets/Editor/ShooterCustomizeStationWindow.cs

## Assets/Scripts/StateMachine/ScriptableObjects/StateActionSO.cs
- 분류: 일반 소스; 38행
- 타입: StateActionSO : DescriptionSMActionBaseSO, StateActionSO
- 메서드: GetAction, CreateAction
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 77개: Assets/Scripts/Gameplay/Entities/Common/StateMachine/Actions/AnimatorParameterActionSO.cs; Assets/Scripts/Gameplay/Entities/Common/StateMachine/Actions/ApplyMovementVectorActionSO.cs; Assets/Scripts/Gameplay/Entities/Common/StateMachine/Actions/AscendActionSO.cs; Assets/Scripts/Gameplay/Entities/Common/StateMachine/Actions/CancelActiveKnockbackActionSO.cs; Assets/Scripts/Gameplay/Entities/Common/StateMachine/Actions/CancelDashActionSO.cs; Assets/Scripts/Gameplay/Entities/Common/StateMachine/Actions/ChangeGameStateActionSO.cs; Assets/Scripts/Gameplay/Entities/Common/StateMachine/Actions/ChasingTargetActionSO.cs; Assets/Scripts/Gameplay/Entities/Common/StateMachine/Actions/ClearInputCache_OnEnterSO.cs

## Assets/Scripts/StateMachine/ScriptableObjects/StateConditionSO.cs
- 분류: 일반 소스; 42행
- 타입: StateConditionSO : ScriptableObject, StateConditionSO
- 메서드: GetCondition, CreateCondition
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 43개: Assets/Scripts/Gameplay/Entities/Common/StateMachine/Conditions/AlwaysFalseConditionSO.cs; Assets/Scripts/Gameplay/Entities/Common/StateMachine/Conditions/HasHitHeadConditionSO.cs; Assets/Scripts/Gameplay/Entities/Common/StateMachine/Conditions/HasPendingKnockbackConditionSO.cs; Assets/Scripts/Gameplay/Entities/Common/StateMachine/Conditions/HasReceivedEventSO.cs; Assets/Scripts/Gameplay/Entities/Common/StateMachine/Conditions/IsActuallyMovingConditionSO.cs; Assets/Scripts/Gameplay/Entities/Common/StateMachine/Conditions/IsCharacterControllerGroundedConditionSO.cs; Assets/Scripts/Gameplay/Entities/Common/StateMachine/Conditions/IsDeadConditionSO.cs; Assets/Scripts/Gameplay/Entities/Common/StateMachine/Conditions/IsEntityGettingHitSO.cs

## Assets/Scripts/StateMachine/ScriptableObjects/StateSO.cs
- 분류: 일반 소스; 61행
- 타입: StateSO : ScriptableObject
- 메서드: GetState, GetActions
- 직렬화 참조 72개 / 지정 씬 참조 그래프 내 20개: Assets/ScriptableObject/StateMachine/Enemy/CommonEnemyStates/ModeFSM/Alive_Enemy.asset; Assets/ScriptableObject/StateMachine/Enemy/CommonEnemyStates/ModeFSM/Dead_Enemy.asset; Assets/ScriptableObject/StateMachine/Enemy/CommonEnemyStates/ModeFSM/KnockBack_Enemy.asset; Assets/ScriptableObject/StateMachine/Enemy/CommonEnemyStates/ModeFSM/Spawning_Enemy.asset; Assets/ScriptableObject/StateMachine/Enemy/NamedEnemy/Mutarus/State/Alive_Mutarus_NamedEnemy.asset; Assets/ScriptableObject/StateMachine/Enemy/NamedEnemy/Mutarus/State/AttackEntry_Mutarus.asset; Assets/ScriptableObject/StateMachine/Enemy/NamedEnemy/Mutarus/State/ChargeAttack_mutarus.asset; Assets/ScriptableObject/StateMachine/Enemy/NamedEnemy/Mutarus/State/CombatIdle_NamedEnemy.asset
- 타입 이름을 참조하는 소스 8개: Assets/Scripts/Gameplay/Entities/Creature/StateMachineCoordinator.cs; Assets/Scripts/Gameplay/Entities/Creature/StateMachineGroupCoordinator.cs; Assets/Scripts/StateMachine/Core/State.cs; Assets/Scripts/StateMachine/Core/StateMachine.cs; Assets/Scripts/StateMachine/Editor/HFSMTransitionGraphWindow.cs; Assets/Scripts/StateMachine/Editor/StateEditor.cs; Assets/Scripts/StateMachine/Editor/TransitionTableEditor.cs; Assets/Scripts/StateMachine/ScriptableObjects/TransitionTableSO.cs

## Assets/Scripts/StateMachine/ScriptableObjects/TransitionTableSO.cs
- 분류: 일반 소스; 106행
- 타입: TransitionTableSO : ScriptableObject, TransitionItem, ConditionUsage, Result, Operator
- 메서드: GetInitialState, ProcessConditionUsages
- 직렬화 참조 22개 / 지정 씬 참조 그래프 내 4개: Assets/ScriptableObject/StateMachine/Enemy/NamedEnemy/Mutarus/Mutarus_NamedVirus_ALIVE.asset; Assets/ScriptableObject/StateMachine/Enemy/NamedEnemy/Mutarus/Mutarus_NamedVirus_Combat.asset; Assets/ScriptableObject/StateMachine/Enemy/NamedEnemy/Mutarus/Mutarus_NamedVirus_Intro.asset; Assets/ScriptableObject/StateMachine/Enemy/NamedEnemy/Mutarus/Mutarus_NamedVirus_MODE.asset; Assets/ScriptableObject/StateMachine/Enemy/NamedEnemy/Mutarus/Mutarus_NamedVirus_NormalAttack.asset; Assets/ScriptableObject/StateMachine/Enemy/NamedEnemy/Mutarus/Mutarus_NamedVirus_Pattern.asset; Assets/ScriptableObject/StateMachine/Enemy/NormalEnemy/Chaser/Chaser_Virus_ALIVE.asset; Assets/ScriptableObject/StateMachine/Enemy/NormalEnemy/Chaser/Chaser_Virus_MODE.asset
- 타입 이름을 참조하는 소스 19개: Assets/Scripts/Authoring/MonoBehaviours/EditorColdStartup.cs; Assets/Scripts/Core/Events/ScriptableObject/PanelHub/WeaponAmmoSellResultEventChannelSO.cs; Assets/Scripts/Core/Events/ScriptableObject/PanelHub/WeaponAmmoShopPurchaseResultEventChannelSO.cs; Assets/Scripts/Core/SceneFlow/InitializationLoader.cs; Assets/Scripts/Core/SceneFlow/SceneLoader.cs; Assets/Scripts/Presentation/Audio/UIAudio.cs; Assets/Scripts/Presentation/UI/Common/ESCMenu/UISettingItemFiller.cs; Assets/Scripts/Presentation/UI/Common/ESCMenu/UISettingKeyBindingComponent.cs

## Assets/Scripts/StateMachine/Utilities/InitOnlyAttribute.cs
- 분류: 일반 소스; 15행
- 타입: InitOnlyAttribute : PropertyAttribute
- 메서드: 
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 1개: Assets/Scripts/StateMachine/Editor/Utilities/InitOnlyAttributeDrawer.cs