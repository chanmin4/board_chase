# Gameplay 스크립트 전수 정적 인덱스
검사일: 2026-09-22. 함수/타입/직렬화 연결은 정적 추출이며 실행 검증을 뜻하지 않습니다. 동적 생성, 리플렉션 및 Addressables 간접 참조는 별도 시스템 문서와 함께 확인하세요.

## Assets/Scripts/Gameplay/Combat/Infection/InfectionControlManager.cs
- 분류: 일반 소스; 263행
- 타입: InfectionControlManager : MonoBehaviour
- 메서드: Awake, OnEnable, OnDisable, Update, OnSectorSummaryChanged, OnNamedSectorPhaseChanged, ApplyDamage, Recover, TryRecover, RecoverNamedDefeated, RecoverSectorExpanded, ResolveTotalDrainPerSecond, IsInHideout, ApplyMaximumPlayerInfection, Publish, ApplyNormalBattleTimerExpiredPenalty
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 1개: Assets/Scenes/RUNScene.unity
- 타입 이름을 참조하는 소스 3개: Assets/Scripts/Core/Events/ScriptableObject/InfectionControlManagerReadyEventChannelSO.cs; Assets/Scripts/Core/SceneFlow/RuntimeSceneDependencyValidator.cs; Assets/Scripts/Gameplay/World/Sectors/Named_Boss/NamedRewardApplier.cs

## Assets/Scripts/Gameplay/Combat/Infection/ScriptableObject/InfectionControlHUDSettingsSO.cs
- 분류: 일반 소스; 44행
- 타입: InfectionControlHUDSettingsSO : ScriptableObject
- 메서드: 
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 0개: Assets/ScriptableObject/GameScene/InfectionControl/InfectionControlHUDSettings.asset
- 타입 이름을 참조하는 소스 0개: 

## Assets/Scripts/Gameplay/Combat/Infection/ScriptableObject/InfectionControlRulesSO.cs
- 분류: 일반 소스; 92행
- 타입: InfectionControlRulesSO : ScriptableObject
- 메서드: CalculateDrainPerSecond, GetNamedPhaseDrainPerSecond
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 1개: Assets/ScriptableObject/GameScene/InfectionControl/InfectionControlRules.asset
- 타입 이름을 참조하는 소스 1개: Assets/Scripts/Gameplay/Combat/Infection/InfectionControlManager.cs

## Assets/Scripts/Gameplay/Combat/PaintMark/EntityPaintMarkController.cs
- 분류: 일반 소스; 422행
- 타입: EntityPaintMarkController : MonoBehaviour
- 메서드: Awake, Update, SetStatConfig, AddMark, ReduceMark, TickDecay, AddMarkAmount, AddInactiveAmount, AddActiveAmount, RemoveActiveAmount, ClearActive, ApplyInitialState, CalculateStacks, GetMaxStacks, HasMarkStateChanged, ResolveRefs, ResolveConfig, NotifyChanged
- 직렬화 참조 9개 / 지정 씬 참조 그래프 내 2개: Assets/Prefabs/GameScene/Entity_WIP/Virus/NormalVirus/Chaser/Chaser.prefab; Assets/Prefabs/GameScene/Entity_WIP/Virus/NormalVirus/Chaser/Chaser_Upgrade.prefab; Assets/Prefabs/GameScene/Entity_WIP/Virus/NormalVirus/Roller/Roller.prefab; Assets/Prefabs/GameScene/Entity_WIP/Virus/NormalVirus/Spitter/Spitter_ThreeShot_Virus.prefab; Assets/Prefabs/GameScene/Entity_WIP/Virus/NormalVirus/Spitter/Spitter_Virus.prefab; Assets/Prefabs/GameScene/Entity_WIP/Virus/NormalVirus/Worm/Worm.prefab; Assets/Prefabs/GameScene/Entity_WIP/Virus/NormalVirus/Worm/Worm_Upgrade.prefab; Assets/Prefabs/GameScene/Shooter/EnemyShooter/EnemyShooter_Base.prefab
- 타입 이름을 참조하는 소스 5개: Assets/Scripts/Authoring/Editor/DebugMonitorWindow.cs; Assets/Scripts/Gameplay/Entities/Common/Core/Damageable.cs; Assets/Scripts/Gameplay/Entities/Shooter/Player/Inventory/PlayerConsumableUseRuntime.cs; Assets/Scripts/Gameplay/Items/Weapons/Runtime/BasicBullet.cs; Assets/Scripts/Presentation/UI/Gameplay/EnemyBarUI/HealthBar/EnemyHealthBarWidget.cs

## Assets/Scripts/Gameplay/Combat/PaintMark/MaskRenderSO.cs
- 분류: 일반 소스; 79행
- 타입: MaskRenderSO : DescriptionBaseSO
- 메서드: TryValidateRuntimeMaterial, OnValidate
- 직렬화 참조 4개 / 지정 씬 참조 그래프 내 4개: Assets/ScriptableObject/GameScene/MaskRender/HealingMaskRenderSO.asset; Assets/ScriptableObject/GameScene/MaskRender/PoisonPuddleMaskRenderSO.asset; Assets/ScriptableObject/GameScene/MaskRender/VaccineMaskRenderSO.asset; Assets/ScriptableObject/GameScene/MaskRender/VirusMaskRenderSO.asset
- 타입 이름을 참조하는 소스 5개: Assets/Scripts/Gameplay/Combat/PaintMark/MaskRenderTableSO.cs; Assets/Scripts/Gameplay/Entities/Shooter/Common/Core/ShooterInfection.cs; Assets/Scripts/Gameplay/Items/Weapons/Definitions/HealingBottleUsableItemSO.cs; Assets/Scripts/Gameplay/Items/Weapons/Definitions/ThrowableUsableItemSO.cs; Assets/Scripts/Gameplay/MaskRenderManager.cs

## Assets/Scripts/Gameplay/Combat/PaintMark/MaskRenderTableSO.cs
- 분류: 일반 소스; 69행
- 타입: MaskRenderTableSO : DescriptionBaseSO, Entry
- 메서드: TryGet, OnValidate
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 1개: Assets/ScriptableObject/GameScene/MaskRender/MaskRenderTable.asset
- 타입 이름을 참조하는 소스 12개: Assets/Editor/LootProgressionEditorWindow.cs; Assets/Scripts/Authoring/Editor/SectorMakerWindow.cs; Assets/Scripts/Gameplay/MaskRenderManager.cs; Assets/Scripts/Gameplay/Progression/Stage/StageProgressionManager.cs; Assets/Scripts/Gameplay/World/Sectors/ScriptableObject/SectorExclusionRulesSO.cs; Assets/Scripts/Gameplay/World/Sectors/StageMap/StageTopologySO.cs; Assets/Scripts/Gameplay/World/Sectors/Treasure/TreasureRoomDropTableSO.cs; Assets/Scripts/Presentation/Audio/AudioCatalog.cs

## Assets/Scripts/Gameplay/Combat/PaintMark/PaintTypes.cs
- 분류: 일반 소스; 168행
- 타입: PaintFaction, PaintMarkFaction, PaintChannel, PaintSurfaceState, PaintTypeUtility
- 메서드: IsCoated, ToFaction, ToMarkFaction, ToSurfaceState, ToCoatedState, ToSectorOwner, IsOpposite, IsSameMarkFaction
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 31개: Assets/Scripts/Gameplay/Combat/PaintMark/EntityPaintMarkController.cs; Assets/Scripts/Gameplay/Combat/PaintMark/MaskRenderSO.cs; Assets/Scripts/Gameplay/Combat/PaintMark/MaskRenderTableSO.cs; Assets/Scripts/Gameplay/Entities/Creature/CommonEnemyStateMachine/Action/EnemyFireArcBombActionSO.cs; Assets/Scripts/Gameplay/Entities/Creature/CommonEnemyStateMachine/Action/EnemyFireProjectileActionSO.cs; Assets/Scripts/Gameplay/Entities/Creature/CommonEnemyStateMachine/AttackExecutor/EnemyArcBombAttackExecutor.cs; Assets/Scripts/Gameplay/Entities/Creature/CommonEnemyStateMachine/AttackExecutor/EnemyProjectileAttackExecutor.cs; Assets/Scripts/Gameplay/Entities/Creature/CommonEnemyStateMachine/AttackExecutor/EnemySelfDestructAttackExecutor.cs

## Assets/Scripts/Gameplay/Combat/PaintMark/TemporaryMaskCircleVisual.cs
- 분류: 일반 소스; 46행
- 타입: TemporaryMaskCircleVisual : MonoBehaviour
- 메서드: Initialize, InitializeWithLifetimeOwner, Update, OnDestroy
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 1개: Assets/Scripts/Gameplay/MaskRenderManager.cs

## Assets/Scripts/Gameplay/Combat/Projectiles/ProjectileRootRegistry.cs
- 분류: 일반 소스; 26행
- 타입: ProjectileRootRegistry : MonoBehaviour
- 메서드: OnEnable, OnDisable
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 1개: Assets/Scenes/RUNScene.unity
- 타입 이름을 참조하는 소스 5개: Assets/Scripts/Gameplay/Entities/Creature/CommonEnemyStateMachine/Action/EnemyFireArcBombActionSO.cs; Assets/Scripts/Gameplay/Entities/Creature/CommonEnemyStateMachine/Action/EnemyFireProjectileActionSO.cs; Assets/Scripts/Gameplay/Entities/Creature/CommonEnemyStateMachine/AttackExecutor/EnemyArcBombAttackExecutor.cs; Assets/Scripts/Gameplay/Entities/Creature/CommonEnemyStateMachine/AttackExecutor/EnemyProjectileAttackExecutor.cs; Assets/Scripts/Gameplay/Entities/Creature/NamedEnemy/Mutarus/Action/MutarusQTEPatternActionSO.cs

## Assets/Scripts/Gameplay/Entities/Animation/LegacySciFi/Actions.cs
- 분류: WIP/Legacy 경로; 87행
- 타입: Actions : MonoBehaviour
- 메서드: Stay, Walk, Run, Attack, Death, Damage, Jump, Aiming, Sitting
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 87개: Assets/Scripts/Authoring/Editor/ContentTemplateInspectors.cs; Assets/Scripts/Gameplay/Entities/Common/StateMachine/Actions/AnimatorParameterActionSO.cs; Assets/Scripts/Gameplay/Entities/Common/StateMachine/Actions/ApplyMovementVectorActionSO.cs; Assets/Scripts/Gameplay/Entities/Common/StateMachine/Actions/AscendActionSO.cs; Assets/Scripts/Gameplay/Entities/Common/StateMachine/Actions/CancelActiveKnockbackActionSO.cs; Assets/Scripts/Gameplay/Entities/Common/StateMachine/Actions/CancelDashActionSO.cs; Assets/Scripts/Gameplay/Entities/Common/StateMachine/Actions/ChangeGameStateActionSO.cs; Assets/Scripts/Gameplay/Entities/Common/StateMachine/Actions/ChasingTargetActionSO.cs

## Assets/Scripts/Gameplay/Entities/Common/Config/AttackConfigSO.cs
- 분류: 일반 소스; 18행
- 타입: AttackConfigSO : ScriptableObject
- 메서드: 
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 0개: 

## Assets/Scripts/Gameplay/Entities/Common/Config/CreatureMovementConfigSO.cs
- 분류: 일반 소스; 17행
- 타입: CreatureMovementConfigSO : ScriptableObject
- 메서드: 
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 2개: Assets/Scripts/Gameplay/Entities/Common/Config/PathwayConfigSO.cs; Assets/Scripts/Gameplay/Entities/Common/Config/RoamingAroundCenterConfigSO.cs

## Assets/Scripts/Gameplay/Entities/Common/Config/DamageFlash_GetHitConfigSO.cs
- 분류: 일반 소스; 20행
- 타입: DamageFlash_GetHitConfigSO : ScriptableObject
- 메서드: 
- 직렬화 참조 4개 / 지정 씬 참조 그래프 내 1개: Assets/ScriptableObject/GameScene/Character/Shooter/EnemyShooter/Config/HitEffect/EnemyGetHitEffectConfig.asset; Assets/ScriptableObject/GameScene/Character/Shooter/EnemyShooter/Config/HitEffect/RedGetHitEffect.asset; Assets/ScriptableObject/GameScene/Character/Shooter/Player/Config/HitEffect/PlayerGetHitEffectConfig.asset; Assets/ScriptableObject/GameScene/Character/Shooter/Player/Config/HitEffect/RedGetHitEffect.asset
- 타입 이름을 참조하는 소스 1개: Assets/Scripts/Gameplay/Entities/Common/Core/Damageable.cs

## Assets/Scripts/Gameplay/Entities/Common/Config/DroppableRewardConfigSO.cs
- 분류: 일반 소스; 27행
- 타입: DroppableRewardConfigSO : ScriptableObject
- 메서드: DropSpecialItem
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 1개: Assets/ScriptableObject/GameScene/Character/Shooter/Player/Config/Droppable/NoDrop.asset
- 타입 이름을 참조하는 소스 7개: Assets/Scripts/Gameplay/Entities/Common/Config/SpecialDroppableRewardConfigSO.cs; Assets/Scripts/Gameplay/Entities/Common/Core/Damageable.cs; Assets/Scripts/Gameplay/Entities/Common/Core/EnemyCommonSettings.cs; Assets/Scripts/Gameplay/Entities/Common/StateMachine/Actions/DropRewardSO.cs; Assets/Scripts/Gameplay/Entities/Creature/ScriptableObject/EnemyStatConfigSO.cs; Assets/Scripts/Gameplay/Entities/Shooter/Enemy/Definitions/EnemyShooterConfigSO.cs; Assets/Scripts/Gameplay/Items/Loot/EnemyLootDropOnDeath.cs

## Assets/Scripts/Gameplay/Entities/Common/Config/EntityStatConfigSO.cs
- 분류: 일반 소스; 231행
- 타입: EntityPaintMarkSettings, EntityVisionSettings, EntityInfectionSettings, EntityArmorSettings, EntityStatConfigSO : ScriptableObject, owned
- 메서드: CreateSharedEnemyShooterDetection, ResolveInitialHealth, ResolveReferenceMoveSpeed
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 130개: Assets/Editor/LootProgressionEditorWindow.cs; Assets/Editor/QuestDesignTreeWindow.cs; Assets/Editor/ShooterCustomizeStationWindow.cs; Assets/Editor/ShooterMeleeAnimationSetup.cs; Assets/Editor/ShopProgressionTreeWindow.cs; Assets/Scripts/Authoring/Editor/AudioCueLibraryWindow.cs; Assets/Scripts/Authoring/Editor/BulletTestPrefabGenerator.cs; Assets/Scripts/Authoring/Editor/ContentPreflightWindow.cs

## Assets/Scripts/Gameplay/Entities/Common/Config/PathwayConfigSO.cs
- 분류: 일반 소스; 63행
- 타입: WaypointData, PathwayConfigSO : CreatureMovementConfigSO
- 메서드: 
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 6개: Assets/Scripts/Authoring/Editor/Pathway/PathwayEditor.cs; Assets/Scripts/Authoring/Editor/Pathway/PathwayGizmos.cs; Assets/Scripts/Authoring/Editor/Pathway/PathwayHandles.cs; Assets/Scripts/Authoring/Editor/Pathway/PathwayNavMesh.cs; Assets/Scripts/Authoring/Editor/Pathway/PathWayNavMeshUI.cs; Assets/Scripts/Gameplay/Entities/Common/StateMachine/Actions/MovementActions/PathwayMovementAction.cs

## Assets/Scripts/Gameplay/Entities/Common/Config/RoamingAroundCenterConfigSO.cs
- 분류: 일반 소스; 23행
- 타입: RoamingAroundCenterConfigSO : CreatureMovementConfigSO
- 메서드: 
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 1개: Assets/Scripts/Gameplay/Entities/Common/StateMachine/Actions/MovementActions/RoamingMovementAction.cs

## Assets/Scripts/Gameplay/Entities/Common/Config/SpecialDroppableRewardConfigSO.cs
- 분류: 일반 소스; 29행
- 타입: SpecialDroppableRewardConfigSO : DroppableRewardConfigSO
- 메서드: DropSpecialItem
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 0개: 

## Assets/Scripts/Gameplay/Entities/Common/Core/CharacterAudio.cs
- 분류: 일반 소스; 45행
- 타입: CharacterAudio : MonoBehaviour
- 메서드: PlayAudio, CanPlayAudio
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 2개: Assets/Scripts/Gameplay/Entities/Creature/Audio/CreatureEnemyAudio.cs; Assets/Scripts/Gameplay/Entities/Shooter/Common/Core/EntityShooterCharacterAudio.cs

## Assets/Scripts/Gameplay/Entities/Common/Core/Damageable.cs
- 분류: 일반 소스; 774행
- 타입: EntityDeathCause, Damageable : MonoBehaviour, used, upgrade, delta, delta, needed
- 메서드: PreserveGameObjectOnDeath, Awake, OnEnable, OnDisable, ReceiveAnAttack, ReceiveFixedMeleeAttack, ApplyHealthHit, OnValidate, ReceiveInfectionDamageOverTime, Kill, Revive, ResetHealthFromConfig, ApplyMaxHealthFromStats, SetMaxHealth, SetCurrentHealth, Heal, SetDamageTakenMultiplier, SetDamageTakenMultiplierForSeconds, ResetDamageTakenMultiplier, InitializeHealthFromConfig, Die, ResolveInitialHealth, Cure, ClampDamageTakenMultiplier, TryDodgeIncomingAttack, ResolveHealthDamageMultiplierForPenetration, ResolvePaintMarkDamageMultiplierForPenetration, ResolveInfectionDamageMultiplierForPenetration, ResolveInfectionDamageOverTimeMultiplier, ResolveFinalHealthDamage, ApplyArmorInfectionDurabilityDamage, ApplyArmorHealthDurabilityDamage, ResolveArmorClassDeltaFromPaintMark, ResolveArmorClassDelta, ResolveBaseArmorClass, ResolveBaseHealthArmorMultiplier, ResolveBaseArmorMultiplier, SyncRuntimeHealthDebug, ReportDamageNumber, ResetDamageMultiplierAfter, StopDamageMultiplierTimer, NotifyHealthChanged, NotifyDamageMultiplierChanged
- 직렬화 참조 13개 / 지정 씬 참조 그래프 내 5개: Assets/Prefabs/GameScene/Entity_WIP/Virus/NamedVirus/Mutarus/Mutarus_NamedVirus.prefab; Assets/Prefabs/GameScene/Entity_WIP/Virus/NormalVirus/Chaser/Chaser.prefab; Assets/Prefabs/GameScene/Entity_WIP/Virus/NormalVirus/Chaser/Chaser_Upgrade.prefab; Assets/Prefabs/GameScene/Entity_WIP/Virus/NormalVirus/Roller/Roller.prefab; Assets/Prefabs/GameScene/Entity_WIP/Virus/NormalVirus/Spitter/Spitter_ThreeShot_Virus.prefab; Assets/Prefabs/GameScene/Entity_WIP/Virus/NormalVirus/Spitter/Spitter_Virus.prefab; Assets/Prefabs/GameScene/Entity_WIP/Virus/NormalVirus/Worm/Worm.prefab; Assets/Prefabs/GameScene/Entity_WIP/Virus/NormalVirus/Worm/Worm_Upgrade.prefab
- 타입 이름을 참조하는 소스 375개: Assets/Editor/ItemMakerWindow.cs; Assets/Editor/LootProgressionEditorWindow.cs; Assets/Editor/QuestDesignTreeWindow.cs; Assets/Editor/ShooterCustomizeStationWindow.cs; Assets/Editor/ShopProgressionTreeWindow.cs; Assets/Scripts/Authoring/Editor/AudioCueLibraryWindow.cs; Assets/Scripts/Authoring/Editor/AuthoringWorkspaceUi.cs; Assets/Scripts/Authoring/Editor/BoardChaseAuthoringHubWindow.cs

## Assets/Scripts/Gameplay/Entities/Common/Core/DamageFlashController.cs
- 분류: 일반 소스; 272행
- 타입: DamageFlashController : MonoBehaviour
- 메서드: Reset, Awake, OnEnable, OnDisable, Update, HandleHitReceived, CacheMaterials, ResolveTargetRenderers, AddRendererMaterials, TryResolveColorProperty, ApplyFlash, RestoreBaseColors
- 직렬화 참조 2개 / 지정 씬 참조 그래프 내 2개: Assets/Prefabs/GameScene/Shooter/EnemyShooter/EnemyShooter_Base.prefab; Assets/Prefabs/GameScene/Shooter/Player/Player.prefab
- 타입 이름을 참조하는 소스 0개: 

## Assets/Scripts/Gameplay/Entities/Common/Core/DropGroup.cs
- 분류: 일반 소스; 20행
- 타입: DropGroup
- 메서드: 
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 3개: Assets/Scripts/Gameplay/Entities/Common/Config/DroppableRewardConfigSO.cs; Assets/Scripts/Gameplay/Entities/Common/Config/SpecialDroppableRewardConfigSO.cs; Assets/Scripts/Gameplay/Entities/Common/StateMachine/Actions/DropRewardSO.cs

## Assets/Scripts/Gameplay/Entities/Common/Core/DropItem.cs
- 분류: 일반 소스; 19행
- 타입: DropItem
- 메서드: 
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 2개: Assets/Scripts/Gameplay/Entities/Common/Core/DropGroup.cs; Assets/Scripts/Gameplay/Entities/Common/StateMachine/Actions/DropRewardSO.cs

## Assets/Scripts/Gameplay/Entities/Common/Core/EnemyCommonSettings.cs
- 분류: 일반 소스; 39행
- 타입: EnemyCommonSettings
- 메서드: 
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 2개: Assets/Scripts/Gameplay/Entities/Creature/ScriptableObject/EnemyStatConfigSO.cs; Assets/Scripts/Gameplay/Entities/Shooter/Enemy/Definitions/EnemyShooterConfigSO.cs

## Assets/Scripts/Gameplay/Entities/Common/Core/EntityComponentResolver.cs
- 분류: 일반 소스; 68행
- 타입: EntityComponentResolver
- 메서드: ResolveEntityRoot, IsEntityRootCandidate
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 73개: Assets/Scripts/Authoring/Editor/DebugMonitorWindow.cs; Assets/Scripts/Gameplay/Combat/PaintMark/EntityPaintMarkController.cs; Assets/Scripts/Gameplay/Entities/Common/Core/Damageable.cs; Assets/Scripts/Gameplay/Entities/Common/Core/InvulnerabilityController.cs; Assets/Scripts/Gameplay/Entities/Common/Equipment/EntityEquipmentRuntime.cs; Assets/Scripts/Gameplay/Entities/Common/StateMachine/Actions/CreatureRotateActionSO.cs; Assets/Scripts/Gameplay/Entities/Common/StatusEffects/InfectionDamageOverTimeRuntime.cs; Assets/Scripts/Gameplay/Entities/Shooter/Common/Core/EntityFootstepSoundEmitter.cs

## Assets/Scripts/Gameplay/Entities/Common/Core/InvulnerabilityController.cs
- 분류: 일반 소스; 408행
- 타입: InvulnerabilityController : MonoBehaviour
- 메서드: Awake, Update, OnDisable, Begin, End, SetPersistentSource, GrantTimedSource, ClearTimedSource, ClearAllSources, UpdateOutlineVisual, ApplyOutline, EnsureOutlineInstances, EnsureOutlineSourceRenderers, HasValidOutlineSourceRenderer, ResolveVisualRoot, IsVisualBodyRenderer, CreateMeshOutline, CreateSkinnedOutline, SetupOutlineRenderer, SetOutlineVisible, IsConfigUsable, CleanupExpiredSources
- 직렬화 참조 10개 / 지정 씬 참조 그래프 내 2개: Assets/Prefabs/GameScene/Entity_WIP/Virus/NamedVirus/Mutarus/Mutarus_NamedVirus.prefab; Assets/Prefabs/GameScene/Entity_WIP/Virus/NormalVirus/Chaser/Chaser.prefab; Assets/Prefabs/GameScene/Entity_WIP/Virus/NormalVirus/Chaser/Chaser_Upgrade.prefab; Assets/Prefabs/GameScene/Entity_WIP/Virus/NormalVirus/Roller/Roller.prefab; Assets/Prefabs/GameScene/Entity_WIP/Virus/NormalVirus/Spitter/Spitter_ThreeShot_Virus.prefab; Assets/Prefabs/GameScene/Entity_WIP/Virus/NormalVirus/Spitter/Spitter_Virus.prefab; Assets/Prefabs/GameScene/Entity_WIP/Virus/NormalVirus/Worm/Worm.prefab; Assets/Prefabs/GameScene/Entity_WIP/Virus/NormalVirus/Worm/Worm_Upgrade.prefab
- 타입 이름을 참조하는 소스 3개: Assets/Scripts/Gameplay/Entities/Common/Core/Damageable.cs; Assets/Scripts/Gameplay/Entities/Shooter/Common/Core/ShooterDashController.cs; Assets/Tests/EditMode/Editor/PlayerEmergencyMeleeTests.cs

## Assets/Scripts/Gameplay/Entities/Common/Core/ZoneTriggerController.cs
- 분류: 일반 소스; 42행
- 타입: BoolEvent : UnityEvent<bool, GameObject>, for, ZoneTriggerController : MonoBehaviour
- 메서드: OnTriggerEnter, OnTriggerExit
- 직렬화 참조 3개 / 지정 씬 참조 그래프 내 1개: Assets/Prefabs/GameScene/Entity_WIP/Virus/NormalVirus/TriggerZone_Alert.prefab; Assets/Prefabs/GameScene/Entity_WIP/Virus/NormalVirus/TriggerZone_Attack.prefab; Assets/Prefabs/GameScene/Shooter/Interaction.prefab
- 타입 이름을 참조하는 소스 510개: Assets/Editor/InventorySortButtonMigration.cs; Assets/Editor/ItemCategoryMoveUtility.cs; Assets/Editor/ItemMakerSelection.cs; Assets/Editor/ItemMakerWindow.cs; Assets/Editor/LootProgressionEditorWindow.cs; Assets/Editor/QuestDesignTreeWindow.cs; Assets/Editor/SaveInspectorWindow.cs; Assets/Editor/ShooterCustomizeStationWindow.cs

## Assets/Scripts/Gameplay/Entities/Common/Definitions/InvulnerabilityConfigSO.cs
- 분류: 일반 소스; 60행
- 타입: InvulnerabilityGrantMode, InvulnerabilityConfigSO : ScriptableObject
- 메서드: 
- 직렬화 참조 5개 / 지정 씬 참조 그래프 내 3개: Assets/ScriptableObject/GameScene/Character/Shooter/EnemyShooter/Config/Invulnerable/EnemyDashInvulnerability.asset; Assets/ScriptableObject/GameScene/Character/Shooter/EnemyShooter/Config/Invulnerable/EnemyHitInvulnerability.asset; Assets/ScriptableObject/GameScene/Character/Shooter/Player/Config/Invulnerable/PlayerDashInvulnerability.asset; Assets/ScriptableObject/GameScene/Character/Shooter/Player/Config/Invulnerable/PlayerHitInvulnerability.asset; Assets/ScriptableObject/GameScene/Character/Shooter/ShooterHitInvulnerability.asset
- 타입 이름을 참조하는 소스 3개: Assets/Scripts/Gameplay/Entities/Common/Core/Damageable.cs; Assets/Scripts/Gameplay/Entities/Common/Core/InvulnerabilityController.cs; Assets/Scripts/Gameplay/Entities/Shooter/Common/Core/ShooterDashController.cs

## Assets/Scripts/Gameplay/Entities/Common/Equipment/ArmorItemSO.cs
- 분류: 일반 소스; 125행
- 타입: ArmorItemSO : ItemSO, is, is, above, gap
- 메서드: ResolveHealthDamageMultiplier, ResolveMarkDamageMultiplier, ApplyEquippedVisualTransform, OnValidate, SanitizeScale
- 직렬화 참조 9개 / 지정 씬 참조 그래프 내 7개: Assets/ScriptableObject/GameScene/ContentTemplates/Items/Armor_Template.asset; Assets/ScriptableObject/GameScene/GameItem/Armor/ArmorLV1.asset; Assets/ScriptableObject/GameScene/GameItem/Armor/ArmorLV2.asset; Assets/ScriptableObject/GameScene/GameItem/Armor/ArmorLV3.asset; Assets/ScriptableObject/GameScene/GameItem/Armor/ArmorLV4.asset; Assets/ScriptableObject/GameScene/GameItem/Armor/ArmorLV5.asset; Assets/ScriptableObject/GameScene/GameItem/Armor/ArmorLV6.asset; Assets/ScriptableObject/GameScene/GameItem/Armor/ArmorLV7.asset
- 타입 이름을 참조하는 소스 381개: Assets/Editor/InventorySortButtonMigration.cs; Assets/Editor/ItemCategoryMoveUtility.cs; Assets/Editor/ItemMakerWindow.cs; Assets/Editor/LootProgressionEditorWindow.cs; Assets/Editor/QuestDesignTreeWindow.cs; Assets/Editor/SaveInspectorWindow.cs; Assets/Editor/ShooterCustomizeStationWindow.cs; Assets/Editor/ShooterMeleeAnimationSetup.cs

## Assets/Scripts/Gameplay/Entities/Common/Equipment/ArmorPenetrationDamageCurve.cs
- 분류: 일반 소스; 48행
- 타입: ArmorPenetrationDamageCurve
- 메서드: ResolveMinimumMultiplier, RollMultiplier
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 5개: Assets/Scripts/Gameplay/Entities/Common/Core/Damageable.cs; Assets/Scripts/Gameplay/Entities/Common/Equipment/ArmorItemSO.cs; Assets/Scripts/Gameplay/Entities/Common/Equipment/EntityEquipmentRuntime.cs; Assets/Scripts/Gameplay/Entities/Common/Equipment/ShieldArmorRuntime.cs; Assets/Tests/EditMode/Editor/RaidRuntimeContractTests.cs

## Assets/Scripts/Gameplay/Entities/Common/Equipment/EntityEquipmentRuntime.cs
- 분류: 일반 소스; 831행
- 타입: EntityEquipmentRuntime : MonoBehaviour, needed
- 메서드: Awake, ApplyStartingLoadout, ApplyStartingLoadoutIfNeeded, TryGetLastStartingLoadoutSnapshot, EquipWeapon, EquipEnemySpawnWeapon, ClearWeapon, SetActiveWeaponSlot, GetWeapon, GetWeaponAmount, GetWeaponCurrentDurability, GetWeaponMaxDurability, GetWeaponCondition01, GetWeaponPerformanceMultiplier, ApplyCurrentWeaponFireWear, RepairWeaponToFull, TrySwapWeaponSlots, TryConsumeWeaponStack, EquipArmor, ClearArmor, ResolveEffectiveArmorClass, ResolveHealthDamageMultiplier, ResolveMarkDamageMultiplier, ApplyArmorAbsorbedDamage, RepairArmor, RepairArmorToFull, RebuildArmorVisual, EnsureWeaponSlots, EquipStartingWeapon, ResolveStartingDurability, ResolveEnemySpawnDurability, ResolveFactoryMaxDurability, ResolveDurabilityValues, PublishWeaponSlotChanged, ResolveWeaponSlotIndex, UsesShooterEquipmentControllerVisual, ResolveBaseArmorMultiplier
- 직렬화 참조 5개 / 지정 씬 참조 그래프 내 5개: Assets/Prefabs/GameScene/Shooter/EnemyShooter/EnemyShooter_Base.prefab; Assets/Prefabs/GameScene/Shooter/EnemyShooter/Raider/Raider.prefab; Assets/Prefabs/GameScene/Shooter/EnemyShooter/TempBossGuardShooter.prefab; Assets/Prefabs/GameScene/Shooter/EnemyShooter/TempBossShooter.prefab; Assets/Prefabs/GameScene/Shooter/Player/Player.prefab
- 타입 이름을 참조하는 소스 199개: Assets/Editor/LootProgressionEditorWindow.cs; Assets/Editor/ShooterCustomizeStationWindow.cs; Assets/Editor/ShopProgressionTreeWindow.cs; Assets/Scripts/Authoring/Editor/AudioCueLibraryWindow.cs; Assets/Scripts/Authoring/Editor/BoardChaseAuthoringHubWindow.cs; Assets/Scripts/Authoring/Editor/DebugMonitorWindow.cs; Assets/Scripts/Authoring/Editor/DebugMonitorWindow.Operations.cs; Assets/Scripts/Authoring/Editor/DebugMonitorWindow.Quests.cs

## Assets/Scripts/Gameplay/Entities/Common/Equipment/ShieldArmorRuntime.cs
- 분류: 일반 소스; 25행
- 타입: ShieldArmorRuntime : MonoBehaviour
- 메서드: ResolveMultiplier, Absorb, Restore, Refill
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 2개: Assets/Scripts/Gameplay/Entities/Common/Core/Damageable.cs; Assets/Tests/EditMode/Editor/RaidRuntimeContractTests.cs

## Assets/Scripts/Gameplay/Entities/Common/KnockBack/KnockbackReceiver.cs
- 분류: 일반 소스; 178행
- 타입: KnockbackReceiver : MonoBehaviour
- 메서드: Awake, Update, RequestKnockback, ConsumePendingRequest, ClearPendingRequest, CancelActiveKnockback, TickKnockback, ApplyDelta, FinishKnockback, RestoreNavMeshAgentState
- 직렬화 참조 10개 / 지정 씬 참조 그래프 내 2개: Assets/Prefabs/GameScene/Entity_WIP/Virus/NamedVirus/Mutarus/Mutarus_NamedVirus.prefab; Assets/Prefabs/GameScene/Entity_WIP/Virus/NormalVirus/Chaser/Chaser.prefab; Assets/Prefabs/GameScene/Entity_WIP/Virus/NormalVirus/Chaser/Chaser_Upgrade.prefab; Assets/Prefabs/GameScene/Entity_WIP/Virus/NormalVirus/Roller/Roller.prefab; Assets/Prefabs/GameScene/Entity_WIP/Virus/NormalVirus/Spitter/Spitter_ThreeShot_Virus.prefab; Assets/Prefabs/GameScene/Entity_WIP/Virus/NormalVirus/Spitter/Spitter_Virus.prefab; Assets/Prefabs/GameScene/Entity_WIP/Virus/NormalVirus/Worm/Worm.prefab; Assets/Prefabs/GameScene/Entity_WIP/Virus/NormalVirus/Worm/Worm_Upgrade.prefab
- 타입 이름을 참조하는 소스 5개: Assets/Scripts/Gameplay/Entities/Common/StateMachine/Actions/CancelActiveKnockbackActionSO.cs; Assets/Scripts/Gameplay/Entities/Common/StateMachine/Actions/ConsumePendingKnockbackActionSO.cs; Assets/Scripts/Gameplay/Entities/Common/StateMachine/Conditions/HasPendingKnockbackConditionSO.cs; Assets/Scripts/Gameplay/Entities/Common/StateMachine/Conditions/IsKnockbackActiveConditionSO.cs; Assets/Scripts/Gameplay/Entities/Creature/Enemy.cs

## Assets/Scripts/Gameplay/Entities/Common/KnockBack/KnockbackRequest.cs
- 분류: 일반 소스; 39행
- 타입: KnockbackRequest
- 메서드: FromSource
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 2개: Assets/Scripts/Gameplay/Entities/Common/KnockBack/KnockbackReceiver.cs; Assets/Scripts/Gameplay/Entities/Creature/Enemy.cs

## Assets/Scripts/Gameplay/Entities/Common/StateMachine/Actions/AnimatorParameterActionSO.cs
- 분류: 일반 소스; 333행
- 타입: AnimatorParameterActionSO : StateActionSO, ParameterType, ValueSource, AnimatorParameterAction : StateAction
- 메서드: CreateAction, Awake, OnStateEnter, OnStateExit, OnUpdate, SetParameter, ResolveBoolValue, ResolveIntValue, ResolveFloatValue, ResolveMovementSpeed, ResolveTransformDeltaSpeed, GetMagnitude, ResolveExpectedParameterType, HasAnimatorParameter
- 직렬화 참조 24개 / 지정 씬 참조 그래프 내 6개: Assets/ScriptableObject/StateMachine/Enemy/Actions/CreatureIsDead_True_OnEnter.asset; Assets/ScriptableObject/StateMachine/Enemy/Actions/CreatureIsHIt_True_On_Enter.asset; Assets/ScriptableObject/StateMachine/Enemy/Actions/DeadAnimatorParameterAction.asset; Assets/ScriptableObject/StateMachine/Enemy/Actions/Enemy_IdleAnimatorParameterAction.asset; Assets/ScriptableObject/StateMachine/Enemy/Actions/UnderAlert_False_OnEnter.asset; Assets/ScriptableObject/StateMachine/Enemy/Actions/UnderAlert_True_OnEnter.asset; Assets/ScriptableObject/StateMachine/Enemy/NamedEnemy/Action_Common/Idle_NamedAnimatorParameterAction.asset; Assets/ScriptableObject/StateMachine/Enemy/NamedEnemy/Mutarus/Action/ChargeAttack_MutarusAnimatorParameterAction.asset
- 타입 이름을 참조하는 소스 1개: Assets/Scripts/Gameplay/Entities/Common/StateMachine/Actions/Editor/AnimatorParameterActionSOEditor.cs

## Assets/Scripts/Gameplay/Entities/Common/StateMachine/Actions/ApplyMovementVectorActionSO.cs
- 분류: 일반 소스; 50행
- 타입: ApplyMovementVectorActionSO : StateActionSO<ApplyMovementVectorAction>, ApplyMovementVectorAction : StateAction
- 메서드: Awake, OnUpdate
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 1개: Assets/ScriptableObject/StateMachine/Shooter/Player/Action/MovementVector/ApplyMovementVector.asset
- 타입 이름을 참조하는 소스 2개: Assets/Scripts/Gameplay/Entities/Common/StateMachine/Actions/AscendActionSO.cs; Assets/Scripts/Gameplay/Entities/Common/StateMachine/Actions/DescendActionSO.cs

## Assets/Scripts/Gameplay/Entities/Common/StateMachine/Actions/AscendActionSO.cs
- 분류: 일반 소스; 58행
- 타입: AscendActionSO : StateActionSO<AscendAction>, AscendAction : StateAction
- 메서드: Awake, OnStateEnter, OnUpdate
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 0개: 

## Assets/Scripts/Gameplay/Entities/Common/StateMachine/Actions/CancelActiveKnockbackActionSO.cs
- 분류: 일반 소스; 37행
- 타입: CancelActiveKnockbackActionSO : StateActionSO<CancelActiveKnockbackAction>, CancelActiveKnockbackAction : StateAction
- 메서드: Awake, OnUpdate, OnStateExit
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 1개: Assets/ScriptableObject/StateMachine/Enemy/Actions/CancelActiveKnockbackAction.asset
- 타입 이름을 참조하는 소스 0개: 

## Assets/Scripts/Gameplay/Entities/Common/StateMachine/Actions/CancelDashActionSO.cs
- 분류: 일반 소스; 38행
- 타입: CancelDashActionSO : StateActionSO<CancelDashAction>, CancelDashAction : StateAction
- 메서드: Awake, OnUpdate, OnStateEnter
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 1개: Assets/ScriptableObject/StateMachine/Shooter/Player/Action/CancelDashAction.asset
- 타입 이름을 참조하는 소스 0개: 

## Assets/Scripts/Gameplay/Entities/Common/StateMachine/Actions/ChangeGameStateActionSO.cs
- 분류: 일반 소스; 86행
- 타입: ChangeGameStateActionSO : StateActionSO, ChangeGameStateAction : StateAction
- 메서드: CreateAction, Awake, OnStateEnter, OnStateExit, OnUpdate
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 0개: 

## Assets/Scripts/Gameplay/Entities/Common/StateMachine/Actions/ChasingTargetActionSO.cs
- 분류: 일반 소스; 138행
- 타입: ChasingTargetActionSO : StateActionSO, TargetSource, SpeedSource, ChasingTargetAction : StateAction
- 메서드: CreateAction, Awake, OnStateEnter, OnUpdate, OnStateExit, CanUseAgent, ResolveTarget, ResolveSpeed
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 0개: Assets/ScriptableObject/StateMachine/Enemy/Actions/ChasingTargetAction.asset
- 타입 이름을 참조하는 소스 1개: Assets/Scripts/Gameplay/Entities/Creature/NamedEnemy/Action/NamedEnemyRepositionAroundTargetActionSO.cs

## Assets/Scripts/Gameplay/Entities/Common/StateMachine/Actions/ClearInputCache_OnEnterSO.cs
- 분류: 일반 소스; 41행
- 타입: ClearInputCache_OnEnterSO : StateActionSO, ClearInputCache_OnEnter : StateAction
- 메서드: CreateAction, Awake, OnUpdate, OnStateEnter
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 0개: Assets/ScriptableObject/StateMachine/Shooter/Player/Action/ClearInputCache_OnEnter.asset
- 타입 이름을 참조하는 소스 0개: 

## Assets/Scripts/Gameplay/Entities/Common/StateMachine/Actions/ConsumePendingKnockbackActionSO.cs
- 분류: 일반 소스; 39행
- 타입: ConsumePendingKnockbackActionSO : StateActionSO<ConsumePendingKnockbackAction>, ConsumePendingKnockbackAction : StateAction
- 메서드: OnUpdate, Awake, OnStateEnter
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 1개: Assets/ScriptableObject/StateMachine/Shared_Action/ConsumePendingKnockbackAction.asset
- 타입 이름을 참조하는 소스 0개: 

## Assets/Scripts/Gameplay/Entities/Common/StateMachine/Actions/ControlWalkingParticlesActionSO.cs
- 분류: 일반 소스; 57행
- 타입: ControlWalkingParticlesActionSO : StateActionSO<ControlWalkingParticlesAction>, ControlWalkingParticlesAction : StateAction
- 메서드: Awake, OnStateEnter, OnStateExit, OnUpdate, ResolveEffects
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 0개: 

## Assets/Scripts/Gameplay/Entities/Common/StateMachine/Actions/CreatureFacePlayerSO.cs
- 분류: 일반 소스; 49행
- 타입: CreatureFacePlayerSO : StateActionSO, CreatureFacePlayer : StateAction
- 메서드: CreateAction, Awake, OnUpdate, OnStateEnter
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 0개: Assets/ScriptableObject/StateMachine/Enemy/Actions/CreatureFacePlayer.asset
- 타입 이름을 참조하는 소스 0개: 

## Assets/Scripts/Gameplay/Entities/Common/StateMachine/Actions/CreatureRotateActionSO.cs
- 분류: 일반 소스; 223행
- 타입: CreatureRotateActionSO : StateActionSO<CreatureRotateAction>, CreatureRotateAction : StateAction
- 메서드: Awake, OnUpdate, IsMouseInsideSelfScreenBounds, TryGetSelfBounds
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 0개: Assets/ScriptableObject/StateMachine/Shooter/Player/Action/MovementVector/RotateAction.asset
- 타입 이름을 참조하는 소스 0개: 

## Assets/Scripts/Gameplay/Entities/Common/StateMachine/Actions/DashMoveActionSO.cs
- 분류: 일반 소스; 50행
- 타입: DashMoveActionSO : StateActionSO<DashMoveAction>, DashMoveAction : StateAction
- 메서드: Awake, OnUpdate
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 1개: Assets/ScriptableObject/StateMachine/Shooter/Player/Action/MovementVector/DashMoveAction.asset
- 타입 이름을 참조하는 소스 0개: 

## Assets/Scripts/Gameplay/Entities/Common/StateMachine/Actions/DescendActionSO.cs
- 분류: 일반 소스; 56행
- 타입: DescendActionSO : StateActionSO<DescendAction>, DescendAction : StateAction
- 메서드: Awake, OnStateEnter, OnUpdate
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 0개: 

## Assets/Scripts/Gameplay/Entities/Common/StateMachine/Actions/DestroyEntitySO.cs
- 분류: 일반 소스; 40행
- 타입: DestroyEntitySO : StateActionSO, DestroyEntity : StateAction
- 메서드: CreateAction, Awake, OnUpdate, OnStateEnter
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 0개: 

## Assets/Scripts/Gameplay/Entities/Common/StateMachine/Actions/DropRewardSO.cs
- 분류: 일반 소스; 141행
- 타입: DropRewardSO : StateActionSO, DropReward : StateAction
- 메서드: CreateAction, Awake, OnUpdate, OnStateEnter, DropAllRewards, DropOneReward
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 1개: Assets/Scripts/Gameplay/Entities/Shooter/Enemy/Common/EnemyLootInventoryRuntime.cs

## Assets/Scripts/Gameplay/Entities/Common/StateMachine/Actions/Editor/AnimatorParameterActionSOEditor.cs
- 분류: 에디터; 52행
- 타입: AnimatorParameterActionSOEditor : CustomBaseEditor
- 메서드: OnInspectorGUI
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 0개: 

## Assets/Scripts/Gameplay/Entities/Common/StateMachine/Actions/GroundGravityActionSO.cs
- 분류: 일반 소스; 49행
- 타입: GroundGravityActionSO : StateActionSO<GroundGravityAction>, GroundGravityAction : StateAction
- 메서드: Awake, OnUpdate
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 1개: Assets/ScriptableObject/StateMachine/Shooter/Player/Action/MovementVector/GroundGravity.asset
- 타입 이름을 참조하는 소스 0개: 

## Assets/Scripts/Gameplay/Entities/Common/StateMachine/Actions/HorizontalMoveActionSO.cs
- 분류: 일반 소스; 62행
- 타입: HorizontalMoveActionSO : StateActionSO<HorizontalMoveAction>, HorizontalMoveAction : StateAction
- 메서드: Awake, OnUpdate
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 1개: Assets/ScriptableObject/StateMachine/Shooter/Player/Action/MovementVector/MoveOnTheGround.asset
- 타입 이름을 참조하는 소스 1개: Assets/Scripts/Gameplay/Entities/Shooter/Player/PlayerStatsRuntime.cs

## Assets/Scripts/Gameplay/Entities/Common/StateMachine/Actions/MovementActions/NPCMovementAction.cs
- 분류: 일반 소스; 21행
- 타입: NPCMovementAction
- 메서드: OnUpdate, OnStateEnter, OnStateExit
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 2개: Assets/Scripts/Gameplay/Entities/Common/StateMachine/Actions/MovementActions/PathwayMovementAction.cs; Assets/Scripts/Gameplay/Entities/Common/StateMachine/Actions/MovementActions/RoamingMovementAction.cs

## Assets/Scripts/Gameplay/Entities/Common/StateMachine/Actions/MovementActions/PathwayMovementAction.cs
- 분류: 일반 소스; 66행
- 타입: PathwayMovementAction : NPCMovementAction
- 메서드: OnUpdate, OnStateEnter, OnStateExit, GetNextDestination
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 0개: 

## Assets/Scripts/Gameplay/Entities/Common/StateMachine/Actions/MovementActions/RoamingMovementAction.cs
- 분류: 일반 소스; 69행
- 타입: RoamingMovementAction : NPCMovementAction
- 메서드: OnUpdate, OnStateEnter, OnStateExit, GetRoamingPositionAroundPosition
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 0개: 

## Assets/Scripts/Gameplay/Entities/Common/StateMachine/Actions/PlayAudioCueActionSO.cs
- 분류: 일반 소스; 51행
- 타입: PlayAudioCueActionSO : StateActionSO<PlayAudioCueAction>, PlayAudioCueAction : StateAction
- 메서드: Awake, OnUpdate, OnStateEnter
- 직렬화 참조 4개 / 지정 씬 참조 그래프 내 0개: Assets/ScriptableObject/StateMachine/Enemy/NormalEnemy/Chaser/Action/SFX/ChaserChargeAudioCueAction.asset; Assets/ScriptableObject/StateMachine/Enemy/NormalEnemy/Chaser/Action/SFX/ChaserExplosiveAudioCueAction.asset; Assets/ScriptableObject/StateMachine/Enemy/NormalEnemy/Roller/Action/SFX/ChaserChargeAudioCueAction.asset; Assets/ScriptableObject/StateMachine/Enemy/NormalEnemy/Roller/Action/SFX/ChaserExplosiveAudioCueAction.asset
- 타입 이름을 참조하는 소스 0개: 

## Assets/Scripts/Gameplay/Entities/Common/StateMachine/Actions/RaiseVoidEventActionSO.cs
- 분류: 일반 소스; 47행
- 타입: RaiseVoidEventActionSO : StateActionSO, RaiseVoidEventAction : StateAction
- 메서드: CreateAction, Awake, OnUpdate, OnStateEnter, OnStateExit
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 0개: 

## Assets/Scripts/Gameplay/Entities/Common/StateMachine/Actions/ResetGetHitStateSO.cs
- 분류: 일반 소스; 40행
- 타입: ResetGetHitStateSO : StateActionSO, ResetGetHitState : StateAction
- 메서드: CreateAction, Awake, OnUpdate, OnStateExit
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 0개: 

## Assets/Scripts/Gameplay/Entities/Common/StateMachine/Actions/ResetHealthSO.cs
- 분류: 일반 소스; 40행
- 타입: ResetHealthSO : StateActionSO, ResetHealth : StateAction
- 메서드: CreateAction, Awake, OnUpdate, OnStateExit
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 0개: 

## Assets/Scripts/Gameplay/Entities/Common/StateMachine/Actions/ShakeCamActionSO.cs
- 분류: 일반 소스; 44행
- 타입: ShakeCamActionSO : StateActionSO, ShakeCamAction : StateAction
- 메서드: CreateAction, Awake, OnUpdate, OnStateEnter, OnStateExit
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 0개: 

## Assets/Scripts/Gameplay/Entities/Common/StateMachine/Actions/SlideActionSO.cs
- 분류: 일반 소스; 47행
- 타입: SlideActionSO : StateActionSO<SlideAction>, SlideAction : StateAction
- 메서드: Awake, OnUpdate
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 0개: 

## Assets/Scripts/Gameplay/Entities/Common/StateMachine/Actions/StopAgentSO.cs
- 분류: 일반 소스; 46행
- 타입: StopAgentSO : StateActionSO, StopAgent : StateAction
- 메서드: CreateAction, Awake, OnUpdate, OnStateEnter
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 9개: Assets/Scripts/Gameplay/Entities/Creature/CommonEnemyStateMachine/Action/EnemyFireProjectileActionSO.cs; Assets/Scripts/Gameplay/Entities/Creature/CommonEnemyStateMachine/Action/EnemySpawnAlarmParticleActionSO .cs; Assets/Scripts/Gameplay/Entities/Creature/CommonEnemyStateMachine/AttackExecutor/EnemyChargeAttackExecutor.cs; Assets/Scripts/Gameplay/Entities/Creature/CommonEnemyStateMachine/AttackExecutor/EnemyProjectileAttackExecutor.cs; Assets/Scripts/Gameplay/Entities/Creature/CommonEnemyStateMachine/AttackExecutor/EnemySelfDestructAttackExecutor.cs; Assets/Scripts/Gameplay/Entities/Creature/NamedEnemy/Action/NamedEnemyRepositionAroundTargetActionSO.cs; Assets/Scripts/Gameplay/Entities/Creature/NamedEnemy/Mutarus/Action/MutarusChargeMoveActionSO.cs; Assets/Scripts/Gameplay/Entities/Creature/NormalEnemy/Chaser/StateMachine/Action/ChaserVirusSelfDestructActionSO.cs

## Assets/Scripts/Gameplay/Entities/Common/StateMachine/Actions/StopMovementActionSO.cs
- 분류: 일반 소스; 56행
- 타입: StopMovementActionSO : StateActionSO, StopMovement : StateAction
- 메서드: CreateAction, Awake, OnUpdate, OnStateEnter, OnStateExit
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 1개: Assets/ScriptableObject/StateMachine/Shooter/Player/Action/MovementVector/StopMovementAction.asset
- 타입 이름을 참조하는 소스 5개: Assets/Scripts/Gameplay/Entities/Shooter/Enemy/StateMachine/EnemyShooterChaseActionSO.cs; Assets/Scripts/Gameplay/Entities/Shooter/Enemy/StateMachine/EnemyShooterClearCombatActionSO.cs; Assets/Scripts/Gameplay/Entities/Shooter/Enemy/StateMachine/EnemyShooterCombatController.cs; Assets/Scripts/Gameplay/Entities/Shooter/Enemy/StateMachine/EnemyShooterMoveActionSO.cs; Assets/Scripts/Gameplay/Entities/Shooter/Enemy/StateMachine/EnemyShooterPatrolActionSO.cs

## Assets/Scripts/Gameplay/Entities/Common/StateMachine/Conditions/AlwaysFalseConditionSO.cs
- 분류: 일반 소스; 19행
- 타입: AlwaysFalseConditionSO : StateConditionSO<AlwaysFalseCondition>, AlwaysFalseCondition : Condition
- 메서드: Statement
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 0개: 

## Assets/Scripts/Gameplay/Entities/Common/StateMachine/Conditions/HasHitHeadConditionSO.cs
- 분류: 일반 소스; 47행
- 타입: HasHitHeadConditionSO : StateConditionSO<HasHitHeadCondition>, HasHitHeadCondition : Condition
- 메서드: Awake, Statement
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 0개: 

## Assets/Scripts/Gameplay/Entities/Common/StateMachine/Conditions/HasPendingKnockbackConditionSO.cs
- 분류: 일반 소스; 34행
- 타입: HasPendingKnockbackConditionSO : StateConditionSO<HasPendingKnockbackCondition>, HasPendingKnockbackCondition : Condition
- 메서드: Awake, Statement
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 1개: Assets/ScriptableObject/StateMachine/Shared_Condition/HasPendingKnockbackCondition.asset
- 타입 이름을 참조하는 소스 1개: Assets/Scripts/Gameplay/Entities/Creature/NormalEnemy/CommonNormalEnemyStateMachine/Conditions/IsKnockbackRequestedConditionSO.cs

## Assets/Scripts/Gameplay/Entities/Common/StateMachine/Conditions/HasReceivedEventSO.cs
- 분류: 일반 소스; 48행
- 타입: HasReceivedEventSO : StateConditionSO<HasReceivedEventCondition>, HasReceivedEventCondition : Condition
- 메서드: Awake, Statement, EventReceived, OnStateExit
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 0개: 

## Assets/Scripts/Gameplay/Entities/Common/StateMachine/Conditions/IsActuallyMovingConditionSO.cs
- 분류: 일반 소스; 42행
- 타입: IsActuallyMovingConditionSO : StateConditionSO, IsActuallyMovingCondition : Condition
- 메서드: CreateCondition, Awake, Statement
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 0개: 

## Assets/Scripts/Gameplay/Entities/Common/StateMachine/Conditions/IsCharacterControllerGroundedConditionSO.cs
- 분류: 일반 소스; 27행
- 타입: IsCharacterControllerGroundedConditionSO : StateConditionSO<IsCharacterControllerGroundedCondition>, IsCharacterControllerGroundedCondition : Condition
- 메서드: Awake, Statement
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 0개: Assets/ScriptableObject/StateMachine/Shooter/Player/Condition/IsCharacterControllerGroundedCondition.asset
- 타입 이름을 참조하는 소스 0개: 

## Assets/Scripts/Gameplay/Entities/Common/StateMachine/Conditions/IsDeadConditionSO.cs
- 분류: 일반 소스; 35행
- 타입: IsDeadConditionSO : StateConditionSO, IsDeadCondition : Condition
- 메서드: CreateCondition, Awake, Statement
- 직렬화 참조 2개 / 지정 씬 참조 그래프 내 2개: Assets/ScriptableObject/StateMachine/Enemy/Condition/IsDeadCondition.asset; Assets/ScriptableObject/StateMachine/Shooter/Player/Condition/IsPlayerDead.asset
- 타입 이름을 참조하는 소스 0개: 

## Assets/Scripts/Gameplay/Entities/Common/StateMachine/Conditions/IsEntityGettingHitSO.cs
- 분류: 일반 소스; 41행
- 타입: IsEntityGettingHitSO : StateConditionSO, IsEntityGettingHit : Condition
- 메서드: CreateCondition, Awake, Statement
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 0개: Assets/ScriptableObject/StateMachine/Shooter/Player/Condition/IsPlayerGettingHit.asset
- 타입 이름을 참조하는 소스 0개: 

## Assets/Scripts/Gameplay/Entities/Common/StateMachine/Conditions/IsHoldingAttackActionConditionSO.cs
- 분류: 일반 소스; 32행
- 타입: IsHoldingAttackActionConditionSO : StateConditionSO<IsHoldingAttackActionCondition>, IsHoldingAttackActionCondition : Condition
- 메서드: Awake, Statement
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 0개: Assets/ScriptableObject/StateMachine/Shooter/Player/Condition/IsPlayerHoldingAttackAction.asset
- 타입 이름을 참조하는 소스 0개: 

## Assets/Scripts/Gameplay/Entities/Common/StateMachine/Conditions/IsInSpecificGameStateSO.cs
- 분류: 일반 소스; 31행
- 타입: IsInSpecificGameStateSO : StateConditionSO<IsInSpecificGameStateCondition>, IsInSpecificGameStateCondition : Condition
- 메서드: CreateCondition, Statement
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 0개: 

## Assets/Scripts/Gameplay/Entities/Common/StateMachine/Conditions/IsKnockbackActiveConditionSO.cs
- 분류: 일반 소스; 34행
- 타입: IsKnockbackActiveConditionSO : StateConditionSO<IsKnockbackActiveCondition>, IsKnockbackActiveCondition : Condition
- 메서드: Awake, Statement
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 1개: Assets/ScriptableObject/StateMachine/Shared_Condition/IsKnockbackActiveCondition.asset
- 타입 이름을 참조하는 소스 0개: 

## Assets/Scripts/Gameplay/Entities/Common/StateMachine/Conditions/IsMovingConditionSO.cs
- 분류: 일반 소스; 36행
- 타입: IsMovingConditionSO : StateConditionSO<IsMovingCondition>, IsMovingCondition : Condition
- 메서드: Awake, Statement
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 1개: Assets/ScriptableObject/StateMachine/Shooter/Player/Condition/IsPlayerMoving.asset
- 타입 이름을 참조하는 소스 0개: 

## Assets/Scripts/Gameplay/Entities/Common/StateMachine/Conditions/IsPickingUpSO.cs
- 분류: 일반 소스; 40행
- 타입: IsPickingUpSO : StateConditionSO<IsPickingUpCondition>, IsPickingUpCondition : Condition
- 메서드: Awake, Statement
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 0개: Assets/ScriptableObject/StateMachine/Shooter/Player/Condition/IsPlayerPickingUp.asset
- 타입 이름을 참조하는 소스 0개: 

## Assets/Scripts/Gameplay/Entities/Common/StateMachine/Conditions/IsTargetDeadConditionSO.cs
- 분류: 일반 소스; 35행
- 타입: IsTargetDeadConditionSO : StateConditionSO, IsTargetDeadCondition : Condition
- 메서드: CreateCondition, Awake, Statement
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 0개: Assets/ScriptableObject/StateMachine/Enemy/Condition/IsTargetDeadCondition.asset
- 타입 이름을 참조하는 소스 0개: 

## Assets/Scripts/Gameplay/Entities/Common/StateMachine/Conditions/IsUsingDashConditionSO.cs
- 분류: 일반 소스; 28행
- 타입: IsUsingDashConditionSO : StateConditionSO<IsUsingDashCondition>, IsUsingDashCondition : Condition
- 메서드: Awake, Statement
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 1개: Assets/ScriptableObject/StateMachine/Shooter/Player/Condition/IsUsingDash.asset
- 타입 이름을 참조하는 소스 0개: 

## Assets/Scripts/Gameplay/Entities/Common/StateMachine/Conditions/PlayerIsInZoneSO.cs
- 분류: 일반 소스; 103행
- 타입: ZoneType, AlertDetectionMode, PlayerIsInZoneSO : StateConditionSO<PlayerIsInZone>, PlayerIsInZone : Condition
- 메서드: EvaluateForEnemy, CreateCondition, EvaluateAlert, IsPlayerInEnemySector, Awake, Statement
- 직렬화 참조 2개 / 지정 씬 참조 그래프 내 1개: Assets/ScriptableObject/StateMachine/Enemy/Condition/PlayerIsInAlertZone.asset; Assets/ScriptableObject/StateMachine/Enemy/Condition/PlayerIsInAttackZone.asset
- 타입 이름을 참조하는 소스 1개: Assets/Scripts/Gameplay/Entities/Creature/CommonEnemyStateMachine/Action/EnemyAcquirePlayerTargetActionSO.cs

## Assets/Scripts/Gameplay/Entities/Common/StateMachine/Conditions/TimeElapsedConditionSO.cs
- 분류: 일반 소스; 55행
- 타입: TimeElapsedConditionSO : StateConditionSO<TimeElapsedCondition>, DifficultyTimerScaleMode, TimeElapsedCondition : Condition
- 메서드: OnStateEnter, Statement
- 직렬화 참조 27개 / 지정 씬 참조 그래프 내 1개: Assets/ScriptableObject/StateMachine/Enemy/NamedEnemy/Mutarus/Condition/RepositionTimer_Time Elapsed Condition SO.asset; Assets/ScriptableObject/StateMachine/Enemy/NormalEnemy/Chaser/Condition/Timer_ChaserVirus_AttackDuration.asset; Assets/ScriptableObject/StateMachine/Enemy/NormalEnemy/Chaser/Condition/Timer_ChaserVirus_Fainting.asset; Assets/ScriptableObject/StateMachine/Enemy/NormalEnemy/Chaser/Condition/Timer_ChaserVirus_GettingHit.asset; Assets/ScriptableObject/StateMachine/Enemy/NormalEnemy/Chaser/Condition/Timer_ChaserVirus_Spawning.asset; Assets/ScriptableObject/StateMachine/Enemy/NormalEnemy/Roller/Condition/Timer_ChaserVirus_AttackDuration.asset; Assets/ScriptableObject/StateMachine/Enemy/NormalEnemy/Roller/Condition/Timer_ChaserVirus_Fainting.asset; Assets/ScriptableObject/StateMachine/Enemy/NormalEnemy/Roller/Condition/Timer_ChaserVirus_GettingHit.asset
- 타입 이름을 참조하는 소스 4개: Assets/Scripts/Gameplay/Entities/Creature/NormalEnemy/CommonNormalEnemyStateMachine/Action/CastInfectionActionSO.cs; Assets/Scripts/Gameplay/Entities/Creature/NormalEnemy/CommonNormalEnemyStateMachine/Conditions/MoveTimeoutConditionSO.cs; Assets/Scripts/Gameplay/Entities/Creature/NormalEnemy/CommonNormalEnemyStateMachine/Conditions/TimerCastInfectionConditionSO.cs; Assets/Scripts/Gameplay/Entities/Creature/NormalEnemy/CommonNormalEnemyStateMachine/Conditions/TimerKnockbackConditionSO.cs

## Assets/Scripts/Gameplay/Entities/Common/StateMachine/Conditions/TimeElapsedRandomConditionSO.cs
- 분류: 일반 소스; 34행
- 타입: TimeElapsedRandomConditionSO : StateConditionSO<TimeElapsedRandomCondition>, TimeElapsedRandomCondition : Condition
- 메서드: OnStateEnter, Statement
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 0개: 

## Assets/Scripts/Gameplay/Entities/Common/StatusEffects/EntityStatusEffectRuntime.cs
- 분류: 일반 소스; 330행
- 타입: EntityStatusEffectRuntime : MonoBehaviour, ActiveStatusEffect, delta
- 메서드: Refresh, Awake, Update, OnDisable, Apply, RemoveAll, Remove, CaptureRunState, TryRestoreRunState, RemoveEffectsClearedByFriendlyPaint, RemoveInjuries, Has, GetMultiplier, HasFlag, GetArmorClassDelta
- 직렬화 참조 2개 / 지정 씬 참조 그래프 내 2개: Assets/Prefabs/GameScene/Shooter/EnemyShooter/EnemyShooter_Base.prefab; Assets/Prefabs/GameScene/Shooter/Player/Player.prefab
- 타입 이름을 참조하는 소스 30개: Assets/Editor/ItemMakerWindow.cs; Assets/Scripts/Authoring/Editor/DebugMonitorWindow.cs; Assets/Scripts/Authoring/Editor/DebugMonitorWindow.StatusEffects.cs; Assets/Scripts/Core/Persistence/PlayerRunStatePersistence.cs; Assets/Scripts/Gameplay/Entities/Common/Core/Damageable.cs; Assets/Scripts/Gameplay/Entities/Common/KnockBack/KnockbackReceiver.cs; Assets/Scripts/Gameplay/Entities/Common/StateMachine/Actions/AnimatorParameterActionSO.cs; Assets/Scripts/Gameplay/Entities/Common/StatusEffects/InfectionDamageOverTimeRuntime.cs

## Assets/Scripts/Gameplay/Entities/Common/StatusEffects/InfectionDamageOverTimeRuntime.cs
- 분류: 일반 소스; 96행
- 타입: InfectionDamageOverTimeRuntime : MonoBehaviour
- 메서드: Apply, Update
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 1개: Assets/Scripts/Gameplay/Items/Weapons/Runtime/BasicBullet.cs

## Assets/Scripts/Gameplay/Entities/Common/StatusEffects/StatusEffectDefinitionSO.cs
- 분류: 일반 소스; 115행
- 타입: StatusEffectPolarity, StatusEffectIconKind, StatusEffectModifierType, StatusEffectModifier, StatusEffectDefinitionSO : ScriptableObject
- 메서드: 
- 직렬화 참조 11개 / 지정 씬 참조 그래프 내 10개: Assets/ScriptableObject/GameScene/ContentTemplates/StatusEffects/StatusEffect_Template.asset; Assets/ScriptableObject/GameScene/StatusEffects/FlashbangStun.asset; Assets/ScriptableObject/GameScene/StatusEffects/InjuryBrokenArm.asset; Assets/ScriptableObject/GameScene/StatusEffects/InjuryBrokenLeg.asset; Assets/ScriptableObject/GameScene/StatusEffects/InjuryCriticalDamage.asset; Assets/ScriptableObject/GameScene/StatusEffects/InjuryDeafness.asset; Assets/ScriptableObject/GameScene/StatusEffects/InjuryVisionBlur.asset; Assets/ScriptableObject/GameScene/StatusEffects/InjuryWound.asset
- 타입 이름을 참조하는 소스 22개: Assets/Scripts/Authoring/Editor/ContentTemplateInspectors.cs; Assets/Scripts/Authoring/Editor/DebugMonitorWindow.StatusEffects.cs; Assets/Scripts/Core/Persistence/PlayerRunStatePersistence.cs; Assets/Scripts/Core/Persistence/RuntimeContentCatalogSO.cs; Assets/Scripts/Gameplay/Combat/PaintMark/MaskRenderSO.cs; Assets/Scripts/Gameplay/Entities/Common/Core/Damageable.cs; Assets/Scripts/Gameplay/Entities/Common/StatusEffects/EntityStatusEffectRuntime.cs; Assets/Scripts/Gameplay/Entities/Common/StatusEffects/InfectionDamageOverTimeRuntime.cs

## Assets/Scripts/Gameplay/Entities/Creature/ArcBombRadiusSource.cs
- 분류: 일반 소스; 12행
- 타입: ArcBombRadiusSource
- 메서드: 
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 2개: Assets/Scripts/Gameplay/Entities/Creature/NamedEnemy/Mutarus/ScriptableObject/MutarusPatternConfigSO.cs; Assets/Scripts/Gameplay/Entities/Creature/ScriptableObject/EnemyArcBombAttackConfigSO.cs

## Assets/Scripts/Gameplay/Entities/Creature/AreaAttackTelegraph.cs
- 분류: 일반 소스; 272행
- 타입: AreaAttackTelegraphStyle, AreaAttackTelegraph : MonoBehaviour
- 메서드: SpawnCircle, SpawnPersistentCircle, InitializeCircle, SetPersistentCircle, Update, CompleteAndDestroy, CreateDisc, CreateTransparentMaterial, CreateDiscMesh, OnDestroy
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 6개: Assets/Scripts/Gameplay/Entities/Creature/CommonEnemyStateMachine/Action/EnemyFireArcBombActionSO.cs; Assets/Scripts/Gameplay/Entities/Creature/EnemyArcBombProjectile.cs; Assets/Scripts/Gameplay/Entities/Creature/NamedEnemy/Mutarus/Action/MutarusQTEPatternActionSO.cs; Assets/Scripts/Gameplay/Entities/Creature/NamedEnemy/Mutarus/ScriptableObject/MutarusPatternConfigSO.cs; Assets/Scripts/Gameplay/Entities/Creature/ScriptableObject/EnemyArcBombAttackConfigSO.cs; Assets/Scripts/Gameplay/Entities/Shooter/Player/PlayerClickController.cs

## Assets/Scripts/Gameplay/Entities/Creature/Audio/CreatureEnemyAudio.cs
- 분류: 일반 소스; 29행
- 타입: CreatureEnemyAudio : CharacterAudio
- 메서드: PlayIdle, PlayMove, PlayAttack, PlayGetHit, PlayDie
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 5개: Assets/Scripts/Gameplay/Entities/Creature/NamedEnemy/Mutarus/Audio/MutarusVirusAudio.cs; Assets/Scripts/Gameplay/Entities/Creature/NormalEnemy/Chaser/Audio/ChaserVirusAudio.cs; Assets/Scripts/Gameplay/Entities/Creature/NormalEnemy/Roller/Audio/RollerVirusAudio.cs; Assets/Scripts/Gameplay/Entities/Creature/NormalEnemy/Spitter/Audio/SpitterVirusAudio.cs; Assets/Scripts/Gameplay/Entities/Creature/NormalEnemy/Worm/Audio/WormVirusAudio.cs

## Assets/Scripts/Gameplay/Entities/Creature/CommonEnemyStateMachine/Action/ClearEnemyRuntimeFlagsActionSO.cs
- 분류: 일반 소스; 85행
- 타입: ClearEnemyRuntimeFlagsActionSO : StateActionSO, ClearEnemyRuntimeFlagsAction : StateAction
- 메서드: CreateAction, Awake, OnUpdate, OnStateEnter, OnStateExit, Apply
- 직렬화 참조 3개 / 지정 씬 참조 그래프 내 0개: Assets/ScriptableObject/StateMachine/Enemy/Actions/ChaseClearEnemyRuntimeFlagsAction.asset; Assets/ScriptableObject/StateMachine/Enemy/Actions/Idle_ClearEnemyRuntimeFlagsAction.asset; Assets/ScriptableObject/StateMachine/Enemy/Actions/SearchInfectionTarget_ClearEnemyRuntimeFlagsAction.asset
- 타입 이름을 참조하는 소스 0개: 

## Assets/Scripts/Gameplay/Entities/Creature/CommonEnemyStateMachine/Action/ControlEnemyMoveParticlesActionSO.cs
- 분류: 일반 소스; 79행
- 타입: ControlEnemyMoveParticlesActionSO : StateActionSO<ControlEnemyMoveParticlesAction>, ControlEnemyMoveParticlesAction : StateAction
- 메서드: Awake, OnStateEnter, OnStateExit, OnUpdate
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 0개: 

## Assets/Scripts/Gameplay/Entities/Creature/CommonEnemyStateMachine/Action/EnemyAcquirePlayerTargetActionSO.cs
- 분류: 일반 소스; 105행
- 타입: EnemyAcquirePlayerTargetActionSO : StateActionSO<EnemyAcquirePlayerTargetAction>, EnemyAcquirePlayerTargetAction : StateAction
- 메서드: IsAlert, ResolvePlayerDamageable, Awake, OnStateEnter, OnUpdate, SyncTarget
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 1개: Assets/ScriptableObject/StateMachine/Enemy/Actions/EnemyAcquirePlayerTargetAction.asset
- 타입 이름을 참조하는 소스 0개: 

## Assets/Scripts/Gameplay/Entities/Creature/CommonEnemyStateMachine/Action/EnemyFireArcBombActionSO.cs
- 분류: 일반 소스; 357행
- 타입: EnemyFireArcBombActionSO : StateActionSO<EnemyFireArcBombAction>, EnemyFireArcBombAction : StateAction
- 메서드: Awake, OnStateEnter, OnUpdate, StartCycle, TickCycle, TryFireOne, TriggerShotAnimator, ResolveShotAnimatorTriggerHash, ResolveAnimator, FaceCurrentTarget, FaceTargetPoint, BeginPostAttackDelay, TickPostAttackDelay, NotifyAttackFinished, ResolveTargetPoint
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 0개: Assets/ScriptableObject/StateMachine/Enemy/NamedEnemy/Mutarus/Action/Mutarus_PoisonpuddleAttackAction.asset
- 타입 이름을 참조하는 소스 0개: 

## Assets/Scripts/Gameplay/Entities/Creature/CommonEnemyStateMachine/Action/EnemyFireProjectileActionSO.cs
- 분류: 일반 소스; 371행
- 타입: EnemyFireProjectileActionSO : StateActionSO<EnemyFireProjectileAction>, EnemyFireProjectileAction : StateAction
- 메서드: Awake, OnStateEnter, OnUpdate, OnStateExit, TryFireOne, TriggerShotAnimator, ResolveShotAnimatorTriggerHash, ResolveAnimator, FaceCurrentTarget, ResolveSpawnPosition, ResolveDirection, ResolveTargetVelocity, ApplyRandomSpread, BeginPostAttackDelay, TickPostAttackDelay, NotifyAttackFinished, StopAgent
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 2개: Assets/Scripts/Gameplay/Entities/Creature/NamedEnemy/Action/NamedEnemyFireProjectileBurstActionSO.cs; Assets/Scripts/Gameplay/Entities/Creature/NormalEnemy/Spitter/StateMachine/Action/SpitterFireProjectileActionSO.cs

## Assets/Scripts/Gameplay/Entities/Creature/CommonEnemyStateMachine/Action/EnemySpawnAlarmParticleActionSO .cs
- 분류: 일반 소스; 238행
- 타입: EnemySpawnAlarmParticleActionSO : StateActionSO<EnemySpawnTelegraphAction>, EnemySpawnTelegraphAction : StateAction
- 메서드: Awake, OnStateEnter, OnUpdate, OnStateExit, CacheOriginalStates, SetRenderers, SetColliders, StopAgent, RestoreOriginalStates, RestoreAgent, PlayWarningEffect, PlaySpawnEffect, ResolveEffectParent
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 1개: Assets/ScriptableObject/StateMachine/Enemy/Actions/EnemySpawnAlarmParticleAction.asset
- 타입 이름을 참조하는 소스 0개: 

## Assets/Scripts/Gameplay/Entities/Creature/CommonEnemyStateMachine/Action/PlayEnemyCreatureDeathParticlesActionSO.cs
- 분류: 일반 소스; 82행
- 타입: PlayEnemyCreatureDeathParticlesActionSO : StateActionSO<PlayEnemyCreatureDeathParticlesAction>, PlayEnemyCreatureDeathParticlesAction : StateAction
- 메서드: Awake, OnStateEnter, OnUpdate
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 1개: Assets/ScriptableObject/StateMachine/Enemy/NormalEnemy/CommonAction/Particles/PlayEnemyDeathParticlesAction.asset
- 타입 이름을 참조하는 소스 0개: 

## Assets/Scripts/Gameplay/Entities/Creature/CommonEnemyStateMachine/Action/PlayEnemyCreatureGetHitParticlesActionSO.cs
- 분류: 일반 소스; 67행
- 타입: PlayEnemyCreatureGetHitParticlesActionSO : StateActionSO<PlayEnemyCreatureGetHitParticlesAction>, PlayEnemyCreatureGetHitParticlesAction : StateAction
- 메서드: Awake, OnStateEnter, OnUpdate
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 0개: Assets/ScriptableObject/StateMachine/Enemy/NormalEnemy/CommonAction/Particles/PlayEnemyGetHitParticlesAction.asset
- 타입 이름을 참조하는 소스 0개: 

## Assets/Scripts/Gameplay/Entities/Creature/CommonEnemyStateMachine/Action/PlayEnemyCreatureSpawnParticlesActionSO.cs
- 분류: 일반 소스; 82행
- 타입: PlayEnemyCreatureSpawnParticlesActionSO : StateActionSO<PlayEnemyCreatureSpawnParticlesAction>, PlayEnemyCreatureSpawnParticlesAction : StateAction
- 메서드: Awake, OnStateEnter, OnUpdate
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 0개: Assets/ScriptableObject/StateMachine/Enemy/NormalEnemy/CommonAction/Particles/PlayEnemySpawnParticlesAction.asset
- 타입 이름을 참조하는 소스 0개: 

## Assets/Scripts/Gameplay/Entities/Creature/CommonEnemyStateMachine/Action/SetEnemyContactDamageEnabledActionSO.cs
- 분류: 일반 소스; 88행
- 타입: SetEnemyContactDamageEnabledActionSO : StateActionSO, SetEnemyContactDamageEnabledAction : StateAction
- 메서드: CreateAction, Awake, OnUpdate, OnStateEnter, OnStateExit, Apply
- 직렬화 참조 2개 / 지정 씬 참조 그래프 내 2개: Assets/ScriptableObject/StateMachine/Enemy/Actions/SetEnemyContactDamageEnabledAction_False.asset; Assets/ScriptableObject/StateMachine/Enemy/Actions/SetEnemyContactDamageEnabledAction_True.asset
- 타입 이름을 참조하는 소스 0개: 

## Assets/Scripts/Gameplay/Entities/Creature/CommonEnemyStateMachine/Action/SetSpawnReadyActionSO.cs
- 분류: 일반 소스; 66행
- 타입: SetSpawnReadyActionSO : StateActionSO, SetSpawnReadyAction : StateAction
- 메서드: CreateAction, Awake, OnUpdate, OnStateEnter, OnStateExit, Apply
- 직렬화 참조 2개 / 지정 씬 참조 그래프 내 2개: Assets/ScriptableObject/StateMachine/Enemy/Actions/SetSpawnReadyAction_False.asset; Assets/ScriptableObject/StateMachine/Enemy/Actions/SetSpawnReadyAction_True.asset
- 타입 이름을 참조하는 소스 0개: 

## Assets/Scripts/Gameplay/Entities/Creature/CommonEnemyStateMachine/Action/StopNavMeshAgentActionSO.cs
- 분류: 일반 소스; 96행
- 타입: StopNavMeshAgentActionSO : StateActionSO, StopNavMeshAgentAction : StateAction
- 메서드: CreateAction, Awake, OnUpdate, OnStateEnter, OnStateExit, Apply
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 1개: Assets/ScriptableObject/StateMachine/Enemy/Actions/StopNavMeshAgentAction.asset
- 타입 이름을 참조하는 소스 0개: 

## Assets/Scripts/Gameplay/Entities/Creature/CommonEnemyStateMachine/AttackExecutor/EnemyArcBombAttackExecutor.cs
- 분류: 일반 소스; 274행
- 타입: EnemyArcBombAttackExecutor : EnemyAttackExecutor
- 메서드: Enter, Tick, StartCycle, TickCycle, TryFireOne, TriggerShotAnimator, ResolveShotAnimatorTriggerHash, FaceCurrentTarget, FaceTargetPoint, ResolveTargetPoint, BeginPostAttackDelay, TickPostAttackDelay
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 1개: Assets/Scripts/Gameplay/Entities/Creature/CommonEnemyStateMachine/AttackExecutor/EnemyAttackExecutorFactory.cs

## Assets/Scripts/Gameplay/Entities/Creature/CommonEnemyStateMachine/AttackExecutor/EnemyAttackExecutionContext.cs
- 분류: 일반 소스; 104행
- 타입: EnemyAttackExecutorContext, EnemyAttackExecutor
- 메서드: ResolveAnimator, Enter, Tick, Exit, Finish
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 6개: Assets/Scripts/Gameplay/Entities/Creature/CommonEnemyStateMachine/AttackExecutor/EnemyArcBombAttackExecutor.cs; Assets/Scripts/Gameplay/Entities/Creature/CommonEnemyStateMachine/AttackExecutor/EnemyAttackExecutorFactory.cs; Assets/Scripts/Gameplay/Entities/Creature/CommonEnemyStateMachine/AttackExecutor/EnemyChargeAttackExecutor.cs; Assets/Scripts/Gameplay/Entities/Creature/CommonEnemyStateMachine/AttackExecutor/EnemyProjectileAttackExecutor.cs; Assets/Scripts/Gameplay/Entities/Creature/CommonEnemyStateMachine/AttackExecutor/EnemySelfDestructAttackExecutor.cs; Assets/Scripts/Gameplay/Entities/Creature/NormalEnemy/CommonNormalEnemyStateMachine/Action/NormalEnemyAttackActionSO.cs

## Assets/Scripts/Gameplay/Entities/Creature/CommonEnemyStateMachine/AttackExecutor/EnemyAttackExecutorController.cs
- 분류: 일반 소스; 48행
- 타입: EnemyAttackExecutorController : MonoBehaviour
- 메서드: BeginAttack, FinishAttack, ClearAttack
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 3개: Assets/Scripts/Gameplay/Entities/Creature/CommonEnemyStateMachine/AttackExecutor/EnemyAttackExecutionContext.cs; Assets/Scripts/Gameplay/Entities/Creature/NormalEnemy/CommonNormalEnemyStateMachine/Action/NormalEnemyAttackActionSO.cs; Assets/Scripts/Gameplay/Entities/Creature/NormalEnemy/CommonNormalEnemyStateMachine/Conditions/NormalEnemyAttackFinishedConditionSO.cs

## Assets/Scripts/Gameplay/Entities/Creature/CommonEnemyStateMachine/AttackExecutor/EnemyAttackExecutorFactory.cs
- 분류: 일반 소스; 48행
- 타입: EnemyAttackExecutorFactory
- 메서드: TryCreate
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 1개: Assets/Scripts/Gameplay/Entities/Creature/NormalEnemy/CommonNormalEnemyStateMachine/Action/NormalEnemyAttackActionSO.cs

## Assets/Scripts/Gameplay/Entities/Creature/CommonEnemyStateMachine/AttackExecutor/EnemyChargeAttackExecutor.cs
- 분류: 일반 소스; 363행
- 타입: EnemyChargeAttackExecutor : EnemyAttackExecutor, ChargePhase
- 메서드: Enter, Tick, Exit, TickStartDelay, TickCharging, TickPause, BeginNextCharge, FinishCurrentCharge, FinishAllCharges, BeginPostAttackDelay, TickPostAttackDelay, ResolveChargeDirection, ResolvePredictedAimPoint, ResolveTargetVelocity, FaceAttackTarget, TryResolveCurrentTargetPoint, FaceChargeDirection, StopAgent
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 2개: Assets/Scripts/Gameplay/Entities/Creature/CommonEnemyStateMachine/AttackExecutor/EnemyAttackExecutorFactory.cs; Assets/Scripts/Gameplay/Entities/Creature/NamedEnemy/Mutarus/Action/MutarusChargeMoveActionSO.cs

## Assets/Scripts/Gameplay/Entities/Creature/CommonEnemyStateMachine/AttackExecutor/EnemyProjectileAttackExecutor.cs
- 분류: 일반 소스; 315행
- 타입: EnemyProjectileAttackExecutor : EnemyAttackExecutor
- 메서드: Enter, Tick, Exit, TryFireOne, TriggerShotAnimator, ResolveShotAnimatorTriggerHash, FaceCurrentTarget, ResolveSpawnPosition, ResolveDirection, ResolveTargetVelocity, ApplyRandomSpread, BeginPostAttackDelay, TickPostAttackDelay, StopAgent
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 1개: Assets/Scripts/Gameplay/Entities/Creature/CommonEnemyStateMachine/AttackExecutor/EnemyAttackExecutorFactory.cs

## Assets/Scripts/Gameplay/Entities/Creature/CommonEnemyStateMachine/AttackExecutor/EnemySelfDestructAttackExecutor.cs
- 분류: 일반 소스; 163행
- 타입: EnemySelfDestructAttackExecutor : EnemyAttackExecutor
- 메서드: Enter, Tick, Explode, PaintVirus, HitPlayers, StopAgent, KillSelf, ResolveMaskRenderManager
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 1개: Assets/Scripts/Gameplay/Entities/Creature/CommonEnemyStateMachine/AttackExecutor/EnemyAttackExecutorFactory.cs

## Assets/Scripts/Gameplay/Entities/Creature/Effect/EnemyEffectController.cs
- 분류: 일반 소스; 38행
- 타입: EnemyEffectController : EntityEffectController
- 메서드: Start, PlaySpawnParticles, PlayMoveParticles, StopMoveParticles, PlayGetHitParticles, PlayDeathParticles
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 9개: Assets/Scripts/Gameplay/Entities/Creature/CommonEnemyStateMachine/Action/ControlEnemyMoveParticlesActionSO.cs; Assets/Scripts/Gameplay/Entities/Creature/CommonEnemyStateMachine/Action/PlayEnemyCreatureDeathParticlesActionSO.cs; Assets/Scripts/Gameplay/Entities/Creature/CommonEnemyStateMachine/Action/PlayEnemyCreatureGetHitParticlesActionSO.cs; Assets/Scripts/Gameplay/Entities/Creature/CommonEnemyStateMachine/Action/PlayEnemyCreatureSpawnParticlesActionSO.cs; Assets/Scripts/Gameplay/Entities/Creature/NamedEnemy/Mutarus/Particle/MutarusVirusEffectController.cs; Assets/Scripts/Gameplay/Entities/Creature/NormalEnemy/Chaser/Effect/ChaserVirusEffectController.cs; Assets/Scripts/Gameplay/Entities/Creature/NormalEnemy/Roller/Effect/RollerVirusEffectController.cs; Assets/Scripts/Gameplay/Entities/Creature/NormalEnemy/Spitter/Effect/SpitterVirusEffectController.cs

## Assets/Scripts/Gameplay/Entities/Creature/Enemy.cs
- 분류: 일반 소스; 224행
- 타입: Enemy : MonoBehaviour
- 메서드: OnAlertTriggerChange, OnAttackTriggerChange, NotifyDamagedBy, SetCurrentSector, SetSpawnReady, SetInfectionTarget, ClearInfectionTarget, SetSearchSucceeded, SetInfectionTargetvalid, SetTarget, ClearTarget, OnTargetDead, OnDisable, RequestKnockback, ConsumeKnockbackRequest, ClearKnockbackRequest, BeginInfectionCast, CancelInfectionCast
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 196개: Assets/Editor/LootProgressionEditorWindow.cs; Assets/Editor/QuestDesignTreeWindow.cs; Assets/Scripts/Authoring/Editor/AudioCueLibraryWindow.cs; Assets/Scripts/Authoring/Editor/BossConfigPrefabLinkProvisioner.cs; Assets/Scripts/Authoring/Editor/ContentPreflightWindow.cs; Assets/Scripts/Authoring/Editor/ContentTemplateInspectors.cs; Assets/Scripts/Authoring/Editor/DebugMonitorWindow.cs; Assets/Scripts/Authoring/Editor/DebugMonitorWindow.Operations.cs

## Assets/Scripts/Gameplay/Entities/Creature/EnemyArcBombProjectile.cs
- 분류: 일반 소스; 296행
- 타입: EnemyArcBombProjectile : MonoBehaviour
- 메서드: Awake, Init, Update, DisableFlightCollision, SpawnImpactTelegraph, Explode, ApplyImpactDamage, StampPaint, SpawnImpactParticle, OnDestroy
- 직렬화 참조 2개 / 지정 씬 참조 그래프 내 0개: Assets/Prefabs/GameScene/Entity_WIP/Virus/Virus_Projectile/Spikey_35.prefab; Assets/Prefabs/GameScene/Entity_WIP/Virus/Virus_Projectile/Spikey_Big.prefab
- 타입 이름을 참조하는 소스 5개: Assets/Scripts/Gameplay/Entities/Creature/CommonEnemyStateMachine/Action/EnemyFireArcBombActionSO.cs; Assets/Scripts/Gameplay/Entities/Creature/CommonEnemyStateMachine/AttackExecutor/EnemyArcBombAttackExecutor.cs; Assets/Scripts/Gameplay/Entities/Creature/NamedEnemy/Mutarus/Action/MutarusQTEPatternActionSO.cs; Assets/Scripts/Gameplay/Entities/Creature/NamedEnemy/Mutarus/ScriptableObject/MutarusPatternConfigSO.cs; Assets/Scripts/Gameplay/Entities/Creature/ScriptableObject/EnemyArcBombAttackConfigSO.cs

## Assets/Scripts/Gameplay/Entities/Creature/EnemyAttackRig.cs
- 분류: 일반 소스; 23행
- 타입: EnemyAttackRig : MonoBehaviour
- 메서드: SetProjectileRoot
- 직렬화 참조 2개 / 지정 씬 참조 그래프 내 0개: Assets/Prefabs/GameScene/Entity_WIP/Virus/NormalVirus/Spitter/Spitter_ThreeShot_Virus.prefab; Assets/Prefabs/GameScene/Entity_WIP/Virus/NormalVirus/Spitter/Spitter_Virus.prefab
- 타입 이름을 참조하는 소스 5개: Assets/Scripts/Gameplay/Entities/Creature/CommonEnemyStateMachine/Action/EnemyFireArcBombActionSO.cs; Assets/Scripts/Gameplay/Entities/Creature/CommonEnemyStateMachine/Action/EnemyFireProjectileActionSO.cs; Assets/Scripts/Gameplay/Entities/Creature/CommonEnemyStateMachine/AttackExecutor/EnemyAttackExecutionContext.cs; Assets/Scripts/Gameplay/Entities/Creature/NamedEnemy/Mutarus/Action/MutarusQTEPatternActionSO.cs; Assets/Scripts/Gameplay/World/Sectors/SectorEnemySpawner.cs

## Assets/Scripts/Gameplay/Entities/Creature/EnemyContactAttack.cs
- 분류: 일반 소스; 28행
- 타입: EnemyContactAttack : MonoBehaviour
- 메서드: Initialize, OnTriggerStay
- 직렬화 참조 18개 / 지정 씬 참조 그래프 내 0개: Assets/Prefabs/GameScene/Entity_WIP/Virus/NamedVirus/Mutarus/Mutarus_NamedVirus.prefab; Assets/Prefabs/GameScene/Entity_WIP/Virus/NamedVirus/Mutarus/Visual_MutarusPurple_Named_Weak.prefab; Assets/Prefabs/GameScene/Entity_WIP/Virus/NormalVirus/Chaser/Chaser.prefab; Assets/Prefabs/GameScene/Entity_WIP/Virus/NormalVirus/Chaser/Chaser_Upgrade.prefab; Assets/Prefabs/GameScene/Entity_WIP/Virus/NormalVirus/Chaser/TestChaser.prefab; Assets/Prefabs/GameScene/Entity_WIP/Virus/NormalVirus/Chaser/VisualRoot_Chaser.prefab; Assets/Prefabs/GameScene/Entity_WIP/Virus/NormalVirus/Chaser/VisualRoot_ChaserUpgrade.prefab; Assets/Prefabs/GameScene/Entity_WIP/Virus/NormalVirus/Roller/Roller.prefab
- 타입 이름을 참조하는 소스 1개: Assets/Scripts/Gameplay/Entities/Creature/EnemyContactDamage.cs

## Assets/Scripts/Gameplay/Entities/Creature/EnemyContactDamage.cs
- 분류: 일반 소스; 113행
- 타입: EnemyContactDamage : MonoBehaviour
- 메서드: SetEnemyStatConfig, Reset, Awake, RegisterHitboxes, TryDamage, ResolveStatConfig, CacheMovementStatsProvider
- 직렬화 참조 8개 / 지정 씬 참조 그래프 내 0개: Assets/Prefabs/GameScene/Entity_WIP/Virus/NamedVirus/Mutarus/Mutarus_NamedVirus.prefab; Assets/Prefabs/GameScene/Entity_WIP/Virus/NormalVirus/Chaser/Chaser.prefab; Assets/Prefabs/GameScene/Entity_WIP/Virus/NormalVirus/Chaser/Chaser_Upgrade.prefab; Assets/Prefabs/GameScene/Entity_WIP/Virus/NormalVirus/Roller/Roller.prefab; Assets/Prefabs/GameScene/Entity_WIP/Virus/NormalVirus/Spitter/Spitter_ThreeShot_Virus.prefab; Assets/Prefabs/GameScene/Entity_WIP/Virus/NormalVirus/Spitter/Spitter_Virus.prefab; Assets/Prefabs/GameScene/Entity_WIP/Virus/NormalVirus/Worm/Worm.prefab; Assets/Prefabs/GameScene/Entity_WIP/Virus/NormalVirus/Worm/Worm_Upgrade.prefab
- 타입 이름을 참조하는 소스 3개: Assets/Scripts/Gameplay/Entities/Creature/CommonEnemyStateMachine/Action/SetEnemyContactDamageEnabledActionSO.cs; Assets/Scripts/Gameplay/Entities/Creature/EnemyContactAttack.cs; Assets/Scripts/Gameplay/World/Sectors/Named_Boss/NamedSectorController.cs

## Assets/Scripts/Gameplay/Entities/Creature/EnemyMovementStatsProvider.cs
- 분류: 일반 소스; 69행
- 타입: EnemyMovementStatsProvider : MonoBehaviour
- 메서드: Reset, Awake, SetEnemyStatConfig, ResolveRefs, ResolveEnemyStatConfig
- 직렬화 참조 8개 / 지정 씬 참조 그래프 내 0개: Assets/Prefabs/GameScene/Entity_WIP/Virus/NamedVirus/Mutarus/Mutarus_NamedVirus.prefab; Assets/Prefabs/GameScene/Entity_WIP/Virus/NormalVirus/Chaser/Chaser.prefab; Assets/Prefabs/GameScene/Entity_WIP/Virus/NormalVirus/Chaser/Chaser_Upgrade.prefab; Assets/Prefabs/GameScene/Entity_WIP/Virus/NormalVirus/Roller/Roller.prefab; Assets/Prefabs/GameScene/Entity_WIP/Virus/NormalVirus/Spitter/Spitter_ThreeShot_Virus.prefab; Assets/Prefabs/GameScene/Entity_WIP/Virus/NormalVirus/Spitter/Spitter_Virus.prefab; Assets/Prefabs/GameScene/Entity_WIP/Virus/NormalVirus/Worm/Worm.prefab; Assets/Prefabs/GameScene/Entity_WIP/Virus/NormalVirus/Worm/Worm_Upgrade.prefab
- 타입 이름을 참조하는 소스 9개: Assets/Scripts/Gameplay/Entities/Common/StateMachine/Actions/ChasingTargetActionSO.cs; Assets/Scripts/Gameplay/Entities/Creature/EnemyContactDamage.cs; Assets/Scripts/Gameplay/Entities/Creature/EnemyVirusTrail.cs; Assets/Scripts/Gameplay/Entities/Creature/NamedEnemy/Action/NamedEnemyRepositionAroundTargetActionSO.cs; Assets/Scripts/Gameplay/Entities/Creature/NormalEnemy/CommonNormalEnemyStateMachine/Action/MoveToInfectionTargetActionSO.cs; Assets/Scripts/Gameplay/Entities/Creature/NormalEnemy/CommonNormalEnemyStateMachine/Action/NormalEnemyAttackActionSO.cs; Assets/Scripts/Gameplay/Entities/Creature/NormalEnemy/CommonNormalEnemyStateMachine/Action/WanderRandomInSectorActionSO.cs; Assets/Scripts/Gameplay/Entities/Creature/NormalEnemy/CommonNormalEnemyStateMachine/Conditions/NormalEnemyHasSelectableAttackConditionSO.cs

## Assets/Scripts/Gameplay/Entities/Creature/EnemyProjectile.cs
- 분류: 일반 소스; 299행
- 타입: EnemyProjectile : MonoBehaviour, HitKind
- 메서드: Init, Update, TryGetClosestHit, TryScanMask, ApplyDamage, ResolveDamageTargets, StampVirus
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 0개: Assets/Prefabs/GameScene/Entity_WIP/Virus/Virus_Projectile/Spitter_Projectile.prefab
- 타입 이름을 참조하는 소스 3개: Assets/Scripts/Gameplay/Entities/Creature/CommonEnemyStateMachine/Action/EnemyFireProjectileActionSO.cs; Assets/Scripts/Gameplay/Entities/Creature/CommonEnemyStateMachine/AttackExecutor/EnemyProjectileAttackExecutor.cs; Assets/Scripts/Gameplay/Entities/Creature/ScriptableObject/EnemyFireProjectileConfigSO.cs

## Assets/Scripts/Gameplay/Entities/Creature/EnemyProjectileHurtbox.cs
- 분류: 일반 소스; 69행
- 타입: EnemyProjectileHurtbox : MonoBehaviour
- 메서드: Awake, Reset, AcceptsCollider, TryGetDamageable
- 직렬화 참조 8개 / 지정 씬 참조 그래프 내 0개: Assets/Prefabs/GameScene/Entity_WIP/Virus/NamedVirus/Mutarus/Mutarus_NamedVirus.prefab; Assets/Prefabs/GameScene/Entity_WIP/Virus/NormalVirus/Chaser/Chaser.prefab; Assets/Prefabs/GameScene/Entity_WIP/Virus/NormalVirus/Chaser/Chaser_Upgrade.prefab; Assets/Prefabs/GameScene/Entity_WIP/Virus/NormalVirus/Roller/Roller.prefab; Assets/Prefabs/GameScene/Entity_WIP/Virus/NormalVirus/Spitter/Spitter_ThreeShot_Virus.prefab; Assets/Prefabs/GameScene/Entity_WIP/Virus/NormalVirus/Spitter/Spitter_Virus.prefab; Assets/Prefabs/GameScene/Entity_WIP/Virus/NormalVirus/Worm/Worm.prefab; Assets/Prefabs/GameScene/Entity_WIP/Virus/NormalVirus/Worm/Worm_Upgrade.prefab
- 타입 이름을 참조하는 소스 2개: Assets/Scripts/Gameplay/Entities/Creature/EnemyProjectile.cs; Assets/Scripts/Gameplay/Items/Weapons/Runtime/BasicBullet.cs

## Assets/Scripts/Gameplay/Entities/Creature/EnemyVirusTrail.cs
- 분류: 일반 소스; 292행
- 타입: EnemyVirusTrail : MonoBehaviour
- 메서드: SetEnemyStatConfig, SetTrailEnabled, Awake, OnEnable, OnDisable, LateUpdate, ResolveStatConfig, CacheStatConfig, CacheMovementStatsProvider, ResetTrail, SetLastBodyPosition, SetLastTrailState, HandleMaskRenderManagerReady, ResolveMaskRenderManager
- 직렬화 참조 8개 / 지정 씬 참조 그래프 내 0개: Assets/Prefabs/GameScene/Entity_WIP/Virus/NamedVirus/Mutarus/Mutarus_NamedVirus.prefab; Assets/Prefabs/GameScene/Entity_WIP/Virus/NormalVirus/Chaser/Chaser.prefab; Assets/Prefabs/GameScene/Entity_WIP/Virus/NormalVirus/Chaser/Chaser_Upgrade.prefab; Assets/Prefabs/GameScene/Entity_WIP/Virus/NormalVirus/Roller/Roller.prefab; Assets/Prefabs/GameScene/Entity_WIP/Virus/NormalVirus/Spitter/Spitter_ThreeShot_Virus.prefab; Assets/Prefabs/GameScene/Entity_WIP/Virus/NormalVirus/Spitter/Spitter_Virus.prefab; Assets/Prefabs/GameScene/Entity_WIP/Virus/NormalVirus/Worm/Worm.prefab; Assets/Prefabs/GameScene/Entity_WIP/Virus/NormalVirus/Worm/Worm_Upgrade.prefab
- 타입 이름을 참조하는 소스 1개: Assets/Scripts/Gameplay/World/Sectors/Named_Boss/NamedSectorController.cs

## Assets/Scripts/Gameplay/Entities/Creature/NamedEnemy/Action/ApplyDamageMultiplierFromPatternActionSO.cs
- 분류: 일반 소스; 129행
- 타입: NamedPatternDamageMultiplierCommand, ApplyDamageMultiplierFromPatternActionSO : StateActionSO<ApplyDamageMultiplierFromPatternAction>, ApplyDamageMultiplierFromPatternAction : StateAction
- 메서드: Awake, OnStateEnter, OnUpdate, OnStateExit, Apply, ApplyActiveMultiplier, ApplyResultMultiplier
- 직렬화 참조 3개 / 지정 씬 참조 그래프 내 0개: Assets/ScriptableObject/StateMachine/Enemy/NamedEnemy/Mutarus/Action/Mutarus_ApplyActive_ApplyDamageMultiplierFromPatternAction.asset; Assets/ScriptableObject/StateMachine/Enemy/NamedEnemy/Mutarus/Action/Mutarus_ApplyResult_ApplyDamageMultiplierFromPatternAction.asset; Assets/ScriptableObject/StateMachine/Enemy/NamedEnemy/Mutarus/Action/Mutarus_ClearActive_ApplyDamageMultiplierFromPatternAction.asset
- 타입 이름을 참조하는 소스 0개: 

## Assets/Scripts/Gameplay/Entities/Creature/NamedEnemy/Action/ForceStopNamedPatternActionSO.cs
- 분류: 일반 소스; 57행
- 타입: ForceStopNamedPatternActionSO : StateActionSO<ForceStopNamedPatternAction>, ForceStopNamedPatternAction : StateAction
- 메서드: Awake, OnStateEnter, OnUpdate
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 0개: Assets/ScriptableObject/StateMachine/Enemy/NamedEnemy/Action_Common/ForceStopNamedPatternAction.asset
- 타입 이름을 참조하는 소스 0개: 

## Assets/Scripts/Gameplay/Entities/Creature/NamedEnemy/Action/MoveNamedEnemyToSpawnPointActionSO.cs
- 분류: 일반 소스; 77행
- 타입: MoveNamedEnemyToSpawnPointActionSO : StateActionSO<MoveNamedEnemyToSpawnPointAction>, MoveNamedEnemyToSpawnPointAction : StateAction
- 메서드: Awake, OnStateEnter, OnUpdate, OnStateExit, CanUseAgent
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 0개: Assets/ScriptableObject/StateMachine/Enemy/NamedEnemy/Action_Common/MoveNamedEnemyToSpawnPointAction.asset
- 타입 이름을 참조하는 소스 0개: 

## Assets/Scripts/Gameplay/Entities/Creature/NamedEnemy/Action/NamedEnemyAcquirePlayerTargetActionSO.cs
- 분류: 일반 소스; 79행
- 타입: NamedEnemyAcquirePlayerTargetActionSO : StateActionSO<NamedEnemyAcquirePlayerTargetAction>, NamedEnemyAcquirePlayerTargetAction : StateAction
- 메서드: ResolvePlayer, Awake, OnStateEnter, OnUpdate, TryAcquire
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 0개: Assets/ScriptableObject/StateMachine/Enemy/NamedEnemy/Action_Common/NamedEnemyAcquirePlayerTargetAction.asset
- 타입 이름을 참조하는 소스 0개: 

## Assets/Scripts/Gameplay/Entities/Creature/NamedEnemy/Action/NamedEnemyFireProjectileBurstActionSO.cs
- 분류: 일반 소스; 14행
- 타입: NamedEnemyFireProjectileBurstActionSO : EnemyFireProjectileActionSO
- 메서드: 
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 0개: Assets/ScriptableObject/StateMachine/Enemy/NamedEnemy/Mutarus/Action/Mutarus_ProjectileAttack_Action.asset
- 타입 이름을 참조하는 소스 0개: 

## Assets/Scripts/Gameplay/Entities/Creature/NamedEnemy/Action/NamedEnemyRepositionAroundTargetActionSO.cs
- 분류: 일반 소스; 271행
- 타입: NamedEnemyRepositionAroundTargetActionSO : StateActionSO<NamedEnemyRepositionAroundTargetAction>, SpeedSource, NamedEnemyRepositionAroundTargetAction : StateAction
- 메서드: ConvertSpeedSource, Awake, OnStateEnter, OnUpdate, OnStateExit, PickDestination, RotateToward, ResolveMoveSpeed, IsAgentReady, StopAgent, DrawDistanceDebug, DrawCircle
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 0개: Assets/ScriptableObject/StateMachine/Enemy/NamedEnemy/Mutarus/Action/Mutarus_NamedEnemyRepositionAroundTargetAction.asset
- 타입 이름을 참조하는 소스 1개: Assets/Scripts/Gameplay/Entities/Common/StateMachine/Actions/ChasingTargetActionSO.cs

## Assets/Scripts/Gameplay/Entities/Creature/NamedEnemy/Action/RaiseNamedEnemyKilledActionSO.cs
- 분류: 일반 소스; 79행
- 타입: RaiseNamedEnemyKilledActionSO : StateActionSO<RaiseNamedEnemyKilledAction>, RaiseNamedEnemyKilledAction : StateAction
- 메서드: Awake, OnStateEnter, OnUpdate, Raise
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 0개: Assets/ScriptableObject/StateMachine/Enemy/NamedEnemy/Action_Common/RaiseNamedEnemyKilledAction.asset
- 타입 이름을 참조하는 소스 0개: 

## Assets/Scripts/Gameplay/Entities/Creature/NamedEnemy/Action/SelectNamedAttackActionSO.cs
- 분류: 일반 소스; 85행
- 타입: SelectNamedAttackActionSO : StateActionSO<SelectNamedAttackAction>, SelectNamedAttackAction : StateAction
- 메서드: Awake, OnStateEnter, OnUpdate, TrySelect, ResolveTargetDistance
- 직렬화 참조 2개 / 지정 씬 참조 그래프 내 0개: Assets/ScriptableObject/StateMachine/Enemy/NamedEnemy/Action_Common/SelectNamedAttackAction.asset; Assets/ScriptableObject/StateMachine/Enemy/NamedEnemy/Mutarus/Action/MutarusSelectNamedAttackAction.asset
- 타입 이름을 참조하는 소스 0개: 

## Assets/Scripts/Gameplay/Entities/Creature/NamedEnemy/Action/SetNamedBlackboardBoolActionSO.cs
- 분류: 일반 소스; 116행
- 타입: NamedBlackboardBoolField, SetNamedBlackboardBoolActionSO : StateActionSO<SetNamedBlackboardBoolAction>, SetNamedBlackboardBoolAction : StateAction
- 메서드: Awake, OnStateEnter, OnUpdate, OnStateExit, Apply
- 직렬화 참조 4개 / 지정 씬 참조 그래프 내 0개: Assets/ScriptableObject/StateMachine/Enemy/NamedEnemy/Action_Common/CanEnterPatternBlackboardBoolAction.asset; Assets/ScriptableObject/StateMachine/Enemy/NamedEnemy/Action_Common/IntroRoarFinishedBlackboardBoolAction.asset; Assets/ScriptableObject/StateMachine/Enemy/NamedEnemy/Action_Common/ShouldStopChaseBlackboardBoolAction.asset; Assets/ScriptableObject/StateMachine/Enemy/NamedEnemy/Action_Common/SpawnInitializedBlackboardBoolAction.asset
- 타입 이름을 참조하는 소스 0개: 

## Assets/Scripts/Gameplay/Entities/Creature/NamedEnemy/Action/SetNamedPatternStateActionSO.cs
- 분류: 일반 소스; 121행
- 타입: NamedPatternActionCommand, SetNamedPatternStateActionSO : StateActionSO<SetNamedPatternStateAction>, SetNamedPatternStateAction : StateAction
- 메서드: Awake, OnStateEnter, OnUpdate, OnStateExit, Apply
- 직렬화 참조 7개 / 지정 씬 참조 그래프 내 0개: Assets/ScriptableObject/StateMachine/Enemy/NamedEnemy/Action_Common/BeginPatternStateAction.asset; Assets/ScriptableObject/StateMachine/Enemy/NamedEnemy/Action_Common/MarkActiveFailStateAction.asset; Assets/ScriptableObject/StateMachine/Enemy/NamedEnemy/Action_Common/MarkActiveSuccededStateAction.asset; Assets/ScriptableObject/StateMachine/Enemy/NamedEnemy/Action_Common/MarkPrepareFinishedStateAction.asset; Assets/ScriptableObject/StateMachine/Enemy/NamedEnemy/Action_Common/MarkResolvedStateAction.asset; Assets/ScriptableObject/StateMachine/Enemy/NamedEnemy/Action_Common/SetPatternReadyFalseStateAction.asset; Assets/ScriptableObject/StateMachine/Enemy/NamedEnemy/Action_Common/SetPatternReadyTrueStateAction.asset
- 타입 이름을 참조하는 소스 0개: 

## Assets/Scripts/Gameplay/Entities/Creature/NamedEnemy/Action/StartNamedPatternScheduleActionSO.cs
- 분류: 일반 소스; 72행
- 타입: NamedPatternScheduleStartMode, StartNamedPatternScheduleActionSO : StateActionSO<StartNamedPatternScheduleAction>, StartNamedPatternScheduleAction : StateAction
- 메서드: Awake, OnStateEnter, OnUpdate, OnStateExit, Apply
- 직렬화 참조 2개 / 지정 씬 참조 그래프 내 0개: Assets/ScriptableObject/StateMachine/Enemy/NamedEnemy/Mutarus/Action/First_Mutarus_StartNamedPatternScheduleAction.asset; Assets/ScriptableObject/StateMachine/Enemy/NamedEnemy/Mutarus/Action/Repeat_Mutarus_StartNamedPatternScheduleAction.asset
- 타입 이름을 참조하는 소스 0개: 

## Assets/Scripts/Gameplay/Entities/Creature/NamedEnemy/Condition/ArrivedToNamedAnchorConditionSO.cs
- 분류: 일반 소스; 93행
- 타입: ArrivedToNamedAnchorConditionSO : StateConditionSO<ArrivedToNamedAnchorCondition>, ArrivedToNamedAnchorCondition : Condition
- 메서드: Awake, Statement, ResolveAllowedDistance, CanUseAgent
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 0개: Assets/ScriptableObject/StateMachine/Enemy/NamedEnemy/Condition_Common/ArrivedToNamedAnchorCondition.asset
- 타입 이름을 참조하는 소스 0개: 

## Assets/Scripts/Gameplay/Entities/Creature/NamedEnemy/Condition/AttackFinishedConditionSO.cs
- 분류: 일반 소스; 34행
- 타입: AttackFinishedConditionSO : StateConditionSO<AttackFinishedCondition>, AttackFinishedCondition : Condition
- 메서드: Awake, Statement
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 0개: Assets/ScriptableObject/StateMachine/Enemy/NamedEnemy/Condition_Common/AttackFinishedCondition.asset
- 타입 이름을 참조하는 소스 0개: 

## Assets/Scripts/Gameplay/Entities/Creature/NamedEnemy/Condition/CanEnterPatternConditionSO.cs
- 분류: 일반 소스; 34행
- 타입: CanEnterPatternConditionSO : StateConditionSO<CanEnterPatternCondition>, CanEnterPatternCondition : Condition
- 메서드: Awake, Statement
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 0개: Assets/ScriptableObject/StateMachine/Enemy/NamedEnemy/Condition_Common/CanEnterPatternCondition.asset
- 타입 이름을 참조하는 소스 0개: 

## Assets/Scripts/Gameplay/Entities/Creature/NamedEnemy/Condition/HasSelectedAttackConditionSO.cs
- 분류: 일반 소스; 34행
- 타입: HasSelectedAttackConditionSO : StateConditionSO<HasSelectedAttackCondition>, HasSelectedAttackCondition : Condition
- 메서드: Awake, Statement
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 0개: Assets/ScriptableObject/StateMachine/Enemy/NamedEnemy/Condition_Common/HasSelectedAttackCondition.asset
- 타입 이름을 참조하는 소스 0개: 

## Assets/Scripts/Gameplay/Entities/Creature/NamedEnemy/Condition/NamedIntroFinishedConditionSO.cs
- 분류: 일반 소스; 34행
- 타입: NamedIntroFinishedConditionSO : StateConditionSO<NamedIntroFinishedCondition>, NamedIntroFinishedCondition : Condition
- 메서드: Awake, Statement
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 0개: Assets/ScriptableObject/StateMachine/Enemy/NamedEnemy/Condition_Common/NamedIntroFinishedCondition.asset
- 타입 이름을 참조하는 소스 0개: 

## Assets/Scripts/Gameplay/Entities/Creature/NamedEnemy/Condition/NamedPatternReadyConditionSO.cs
- 분류: 일반 소스; 34행
- 타입: NamedPatternReadyConditionSO : StateConditionSO<NamedPatternReadyCondition>, NamedPatternReadyCondition : Condition
- 메서드: Awake, Statement
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 0개: Assets/ScriptableObject/StateMachine/Enemy/NamedEnemy/Condition_Common/NamedPatternReadyCondition.asset
- 타입 이름을 참조하는 소스 0개: 

## Assets/Scripts/Gameplay/Entities/Creature/NamedEnemy/Condition/NamedPatternSequenceFinishedConditionSO.cs
- 분류: 일반 소스; 39행
- 타입: NamedPatternSequenceFinishedConditionSO : StateConditionSO<NamedPatternSequenceFinishedCondition>, NamedPatternSequenceFinishedCondition : Condition
- 메서드: Awake, Statement
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 0개: Assets/ScriptableObject/StateMachine/Enemy/NamedEnemy/Condition_Common/NamedPatternSequenceFinishedCondition.asset
- 타입 이름을 참조하는 소스 0개: 

## Assets/Scripts/Gameplay/Entities/Creature/NamedEnemy/Condition/NamedSpawnInitializedConditionSO.cs
- 분류: 일반 소스; 34행
- 타입: NamedSpawnInitializedConditionSO : StateConditionSO<NamedSpawnInitializedCondition>, NamedSpawnInitializedCondition : Condition
- 메서드: Awake, Statement
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 0개: Assets/ScriptableObject/StateMachine/Enemy/NamedEnemy/Condition_Common/NamedSpawnInitializedCondition.asset
- 타입 이름을 참조하는 소스 0개: 

## Assets/Scripts/Gameplay/Entities/Creature/NamedEnemy/Condition/NamedTargetDistanceConditionSO.cs
- 분류: 일반 소스; 83행
- 타입: NamedTargetDistanceCheckMode, NamedTargetDistanceConditionSO : StateConditionSO<NamedTargetDistanceCondition>, NamedTargetDistanceCondition : Condition
- 메서드: Awake, Statement, GetFlatDistance
- 직렬화 참조 3개 / 지정 씬 참조 그래프 내 0개: Assets/ScriptableObject/StateMachine/Enemy/NamedEnemy/Mutarus/Condition/Mutarus_BetweenRange_NamedTargetDistanceCondition.asset; Assets/ScriptableObject/StateMachine/Enemy/NamedEnemy/Mutarus/Condition/Mutarus_IsinBattleRange_NamedTargetDistanceCondition.asset; Assets/ScriptableObject/StateMachine/Enemy/NamedEnemy/Mutarus/Condition/Mutarus_IsNeedChase_NamedTargetDistanceCondition.asset
- 타입 이름을 참조하는 소스 0개: 

## Assets/Scripts/Gameplay/Entities/Creature/NamedEnemy/Condition/NoSelectedAttackConditionSO.cs
- 분류: 일반 소스; 34행
- 타입: NoSelectedAttackConditionSO : StateConditionSO<NoSelectedAttackCondition>, NoSelectedAttackCondition : Condition
- 메서드: Awake, Statement
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 0개: Assets/ScriptableObject/StateMachine/Enemy/NamedEnemy/Condition_Common/NoSelectedAttackCondition.asset
- 타입 이름을 참조하는 소스 0개: 

## Assets/Scripts/Gameplay/Entities/Creature/NamedEnemy/Condition/PatternActiveFinishedConditionSO.cs
- 분류: 일반 소스; 34행
- 타입: PatternActiveFinishedConditionSO : StateConditionSO<PatternActiveFinishedCondition>, PatternActiveFinishedCondition : Condition
- 메서드: Awake, Statement
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 0개: Assets/ScriptableObject/StateMachine/Enemy/NamedEnemy/Condition_Common/PatternActiveFinishedCondition.asset
- 타입 이름을 참조하는 소스 0개: 

## Assets/Scripts/Gameplay/Entities/Creature/NamedEnemy/Condition/PatternPrepareFinishedConditionSO.cs
- 분류: 일반 소스; 34행
- 타입: PatternPrepareFinishedConditionSO : StateConditionSO<PatternPrepareFinishedCondition>, PatternPrepareFinishedCondition : Condition
- 메서드: Awake, Statement
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 0개: Assets/ScriptableObject/StateMachine/Enemy/NamedEnemy/Condition_Common/PatternPrepareFinishedCondition.asset
- 타입 이름을 참조하는 소스 0개: 

## Assets/Scripts/Gameplay/Entities/Creature/NamedEnemy/Condition/PatternResolvedConditionSO.cs
- 분류: 일반 소스; 34행
- 타입: PatternResolvedConditionSO : StateConditionSO<PatternResolvedCondition>, PatternResolvedCondition : Condition
- 메서드: Awake, Statement
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 0개: Assets/ScriptableObject/StateMachine/Enemy/NamedEnemy/Condition_Common/PatternResolvedCondition.asset
- 타입 이름을 참조하는 소스 0개: 

## Assets/Scripts/Gameplay/Entities/Creature/NamedEnemy/Condition/SelectedNamedAttackConditionSO.cs
- 분류: 일반 소스; 42행
- 타입: SelectedNamedAttackConditionSO : StateConditionSO<SelectedNamedAttackCondition>, SelectedNamedAttackCondition : Condition
- 메서드: Awake, Statement
- 직렬화 참조 3개 / 지정 씬 참조 그래프 내 0개: Assets/ScriptableObject/StateMachine/Enemy/NamedEnemy/Mutarus/Condition/Charge_Mutarus_SelectedNamedAttackCondition.asset; Assets/ScriptableObject/StateMachine/Enemy/NamedEnemy/Mutarus/Condition/FireProjectile_Mutarus_SelectedNamedAttackCondition.asset; Assets/ScriptableObject/StateMachine/Enemy/NamedEnemy/Mutarus/Condition/PoisonPuddleBomb_Mutarus_SelectedNamedAttackCondition.asset
- 타입 이름을 참조하는 소스 0개: 

## Assets/Scripts/Gameplay/Entities/Creature/NamedEnemy/Condition/ShouldStopChaseConditionSO.cs
- 분류: 일반 소스; 34행
- 타입: ShouldStopChaseConditionSO : StateConditionSO<ShouldStopChaseCondition>, ShouldStopChaseCondition : Condition
- 메서드: Awake, Statement
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 0개: Assets/ScriptableObject/StateMachine/Enemy/NamedEnemy/Condition_Common/ShouldStopChaseCondition.asset
- 타입 이름을 참조하는 소스 0개: 

## Assets/Scripts/Gameplay/Entities/Creature/NamedEnemy/Interface/INamedPatternRuntimeResetter.cs
- 분류: 일반 소스; 14행
- 타입: INamedPatternRuntimeResetter
- 메서드: 
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 2개: Assets/Scripts/Gameplay/Entities/Creature/NamedEnemy/Action/ForceStopNamedPatternActionSO.cs; Assets/Scripts/Gameplay/Entities/Creature/NamedEnemy/Mutarus/MutarusNamedPatternRuntimeResetter.cs

## Assets/Scripts/Gameplay/Entities/Creature/NamedEnemy/Mutarus/Action/MutarusChargeMoveActionSO.cs
- 분류: 일반 소스; 399행
- 타입: MutarusChargeMoveActionSO : StateActionSO<MutarusChargeMoveAction>, MutarusChargeMoveAction : StateAction, ChargePhase
- 메서드: Awake, OnStateEnter, OnUpdate, OnStateExit, TickStartDelay, TickCharging, TickPause, BeginNextCharge, FinishCurrentCharge, FinishAllCharges, BeginPostAttackDelay, TickPostAttackDelay, NotifyAttackFinished, ResolveChargeDirection, ResolvePredictedAimPoint, ResolveTargetVelocity, FaceAttackTarget, TryResolveCurrentTargetPoint, FaceChargeDirection, StopAgent
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 0개: Assets/ScriptableObject/StateMachine/Enemy/NamedEnemy/Mutarus/Action/MutarusChargeMoveAction.asset
- 타입 이름을 참조하는 소스 1개: Assets/Scripts/Gameplay/Entities/Creature/CommonEnemyStateMachine/AttackExecutor/EnemyChargeAttackExecutor.cs

## Assets/Scripts/Gameplay/Entities/Creature/NamedEnemy/Mutarus/Action/MutarusQTEPatternActionSO.cs
- 분류: 일반 소스; 647행
- 타입: MutarusQTEPatternActionSO : StateActionSO<MutarusQTEPatternAction>, MutarusQTEPatternAction : StateAction
- 메서드: Awake, OnStateEnter, OnUpdate, OnStateExit, HandleRuntimeReady, HandlePatternResult, HandleObjectiveProgress, TickPeriodicArcBomb, StartBombBurst, TickBombBurst, FinishBombBurst, FirePeriodicArcBomb, FireFailureArcBomb, SpawnArcBomb, FaceCurrentBombTarget, FaceCurrentPatternTarget, TryResolveCurrentBombFacingPoint, FaceBombTarget, FacePatternTarget, ResolveBombTarget, ResolveFailureBombTarget, TriggerAnimator, ResolveAnimator, RefreshQTEController
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 0개: Assets/ScriptableObject/StateMachine/Enemy/NamedEnemy/Mutarus/Action/MutarusQTEPatternAction.asset
- 타입 이름을 참조하는 소스 1개: Assets/Scripts/Gameplay/Entities/Creature/NamedEnemy/Mutarus/MutarusQTEPatternController.cs

## Assets/Scripts/Gameplay/Entities/Creature/NamedEnemy/Mutarus/Audio/MutarusVirusAudio.cs
- 분류: 일반 소스; 12행
- 타입: MutarusVirusAudio : CreatureEnemyAudio
- 메서드: 
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 0개: Assets/Prefabs/GameScene/Entity_WIP/Virus/NamedVirus/Mutarus/Mutarus_NamedVirus.prefab
- 타입 이름을 참조하는 소스 0개: 

## Assets/Scripts/Gameplay/Entities/Creature/NamedEnemy/Mutarus/MutarusNamedPatternRuntimeResetter.cs
- 분류: 일반 소스; 21행
- 타입: MutarusNamedPatternRuntimeResetter : MonoBehaviour, INamedPatternRuntimeResetter
- 메서드: ForceResetNamedPatternRuntime
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 0개: Assets/Prefabs/GameScene/Entity_WIP/Virus/NamedVirus/Mutarus/Mutarus_NamedVirus.prefab
- 타입 이름을 참조하는 소스 0개: 

## Assets/Scripts/Gameplay/Entities/Creature/NamedEnemy/Mutarus/MutarusQTEPatternController.cs
- 분류: 일반 소스; 542행
- 타입: MutarusQTEPatternController : MonoBehaviour
- 메서드: Awake, OnEnable, OnDisable, Update, HandleResetRequest, EnsureQTEPanelsReady, BeginPattern, CancelPatternWithoutResult, HandleStationGroupReady, HandleInteract, HandleQTEResult, LockGameplayInputForQTE, UnlockGameplayInputForQTE, Finish, FindInteractableStation, PickRandomQTE, AreAllStationsCompleted, HideAllQTEPanels, SetQTEPanelVisible, SetPatternRootVisible, TryStartQTE, PublishObjectiveProgress, TryActivateStationsAndStartTimer, NotifyRuntimeReady, TryGetPlayerPosition
- 직렬화 참조 2개 / 지정 씬 참조 그래프 내 2개: Assets/Prefabs/GameScene/PatternUI/Canvas_Pattern.prefab; Assets/Prefabs/GameScene/PatternUI/QTE/QTEPatternRoot.prefab
- 타입 이름을 참조하는 소스 3개: Assets/Scripts/Core/Events/ScriptableObject/MutarusQTEPatternControllerEventChannelSO.cs; Assets/Scripts/Gameplay/Entities/Creature/NamedEnemy/Mutarus/Action/MutarusQTEPatternActionSO.cs; Assets/Scripts/Gameplay/Entities/Creature/NamedEnemy/Mutarus/MutarusQTEStation.cs

## Assets/Scripts/Gameplay/Entities/Creature/NamedEnemy/Mutarus/MutarusQTEStation.cs
- 분류: 일반 소스; 192행
- 타입: MutarusQTEStation : MonoBehaviour
- 메서드: Awake, OnEnable, OnDisable, TryInteract, SetPatternActive, MarkCompleted, Clear, HandleControllerReady, EnsureVisuals, SetVisualVisible, RegisterTimingSuccess, ResetTimingProgress
- 직렬화 참조 2개 / 지정 씬 참조 그래프 내 0개: Assets/Prefabs/GameScene/PatternUI/QTE/Mutarus_QTEstation.prefab; Assets/Prefabs/GameScene/Sector/QTEStation.prefab
- 타입 이름을 참조하는 소스 4개: Assets/Scripts/Gameplay/Entities/Creature/NamedEnemy/Mutarus/MutarusQTEPatternController.cs; Assets/Scripts/Gameplay/Entities/Creature/NamedEnemy/Mutarus/MutarusQTEStationGroup.cs; Assets/Scripts/Gameplay/Interaction/Core/InteractionManager.cs; Assets/Scripts/Gameplay/Interaction/QTE/QTE_Sub/QTE_Timing.cs

## Assets/Scripts/Gameplay/Entities/Creature/NamedEnemy/Mutarus/MutarusQTEStationGroup.cs
- 분류: 일반 소스; 60행
- 타입: MutarusQTEStationGroup : MonoBehaviour
- 메서드: Reset, OnEnable, OnDisable, SetPatternActive
- 직렬화 참조 2개 / 지정 씬 참조 그래프 내 0개: Assets/Prefabs/GameScene/Sector/QTEStation.prefab; Assets/Prefabs/GameScene/Sector/Stage1/Sector_Named.prefab
- 타입 이름을 참조하는 소스 3개: Assets/Scripts/Core/Events/ScriptableObject/MutarusQTEStationGroupEventChannelSO.cs; Assets/Scripts/Gameplay/Entities/Creature/NamedEnemy/Mutarus/MutarusQTEPatternController.cs; Assets/Scripts/Gameplay/Entities/Creature/NamedEnemy/NamedBattleSectorResetter.cs

## Assets/Scripts/Gameplay/Entities/Creature/NamedEnemy/Mutarus/Particle/MutarusVirusEffectController.cs
- 분류: 일반 소스; 13행
- 타입: MutarusVirusEffectController : EnemyEffectController
- 메서드: 
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 0개: Assets/Prefabs/GameScene/Entity_WIP/Virus/NamedVirus/Mutarus/Mutarus_NamedVirus.prefab
- 타입 이름을 참조하는 소스 0개: 

## Assets/Scripts/Gameplay/Entities/Creature/NamedEnemy/Mutarus/ScriptableObject/MutarusChargeAttackConfigSO.cs
- 분류: 일반 소스; 39행
- 타입: MutarusChargeAttackConfigSO : NamedAttackConfigSO
- 메서드: 
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 0개: Assets/ScriptableObject/GameScene/Character/Enemy/Virus/Definition_Config/NamedEnemy/Mutarus/Mutarus_ChargeAttack_Config.asset
- 타입 이름을 참조하는 소스 3개: Assets/Scripts/Gameplay/Entities/Creature/CommonEnemyStateMachine/AttackExecutor/EnemyAttackExecutorFactory.cs; Assets/Scripts/Gameplay/Entities/Creature/CommonEnemyStateMachine/AttackExecutor/EnemyChargeAttackExecutor.cs; Assets/Scripts/Gameplay/Entities/Creature/NamedEnemy/Mutarus/Action/MutarusChargeMoveActionSO.cs

## Assets/Scripts/Gameplay/Entities/Creature/NamedEnemy/Mutarus/ScriptableObject/MutarusPatternConfigSO.cs
- 분류: 일반 소스; 261행
- 타입: MutarusPatternConfigSO : NamedPatternConfigSO, QTEPatternBombSettings
- 메서드: ResolveRadius
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 0개: Assets/ScriptableObject/GameScene/Character/Enemy/Virus/Definition_Config/NamedEnemy/Mutarus/Mutarus_Pattern_Config.asset
- 타입 이름을 참조하는 소스 1개: Assets/Scripts/Gameplay/Entities/Creature/NamedEnemy/Mutarus/Action/MutarusQTEPatternActionSO.cs

## Assets/Scripts/Gameplay/Entities/Creature/NamedEnemy/Mutarus/ScriptableObject/MutarusPoisonPuddleAttackConfigSO.cs
- 분류: 일반 소스; 14행
- 타입: MutarusPoisonPuddleAttackConfigSO : EnemyArcBombAttackConfigSO
- 메서드: 
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 0개: Assets/ScriptableObject/GameScene/Character/Enemy/Virus/Definition_Config/NamedEnemy/Mutarus/Mutarus_PoisonPuddleAttack_Config.asset
- 타입 이름을 참조하는 소스 0개: 

## Assets/Scripts/Gameplay/Entities/Creature/NamedEnemy/Mutarus/ScriptableObject/MutarusProjectileAttackConfigSO.cs
- 분류: 일반 소스; 15행
- 타입: MutarusProjectileAttackConfigSO : NamedProjectileBurstConfigSO
- 메서드: 
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 0개: Assets/ScriptableObject/GameScene/Character/Enemy/Virus/Definition_Config/NamedEnemy/Mutarus/Mutarus_ProjectileAttack_Config.asset
- 타입 이름을 참조하는 소스 0개: 

## Assets/Scripts/Gameplay/Entities/Creature/NamedEnemy/Mutarus/ScriptableObject/MutarusStatConfigSO.cs
- 분류: 일반 소스; 14행
- 타입: MutarusStatConfigSO : NamedEnemyStatConfigSO
- 메서드: 
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 0개: Assets/ScriptableObject/GameScene/Character/Enemy/Virus/Definition_Config/NamedEnemy/Mutarus/Mutarus_Stat_Config.asset
- 타입 이름을 참조하는 소스 0개: 

## Assets/Scripts/Gameplay/Entities/Creature/NamedEnemy/NamedBattleSectorResetter.cs
- 분류: 일반 소스; 134행
- 타입: NamedBattleSectorResetter : MonoBehaviour
- 메서드: OnEnable, OnDisable, HandleBattleSectorResetRequested, ResetBattleSector, HandleMaskRenderManagerReady, CancelPatternRuntime, ClearQTEStations, DestroyRuntimeObjects, ClearBattleSectorPaint
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 0개: Assets/Prefabs/GameScene/Sector/Stage1/Sector_Named.prefab
- 타입 이름을 참조하는 소스 1개: Assets/Scripts/Gameplay/World/Sectors/Named_Boss/NamedSectorController.cs

## Assets/Scripts/Gameplay/Entities/Creature/NamedEnemy/NamedEnemy.cs
- 분류: 일반 소스; 59행
- 타입: NamedEnemy : Enemy
- 메서드: OnEnable, OnDisable, HandleSpawnContext
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 0개: Assets/Prefabs/GameScene/Entity_WIP/Virus/NamedVirus/Mutarus/Mutarus_NamedVirus.prefab
- 타입 이름을 참조하는 소스 57개: Assets/Scripts/Core/Events/NamedEnemyKilledEventChannelSO.cs; Assets/Scripts/Core/Events/ScriptableObject/BossRewardRequestEventChannelSO.cs; Assets/Scripts/Core/Events/ScriptableObject/BossRewardSelectedEventChannelSO.cs; Assets/Scripts/Core/Events/ScriptableObject/NamedEnemySpawnInfoEventChannelSO.cs; Assets/Scripts/Gameplay/Entities/Creature/NamedEnemy/Action/ApplyDamageMultiplierFromPatternActionSO.cs; Assets/Scripts/Gameplay/Entities/Creature/NamedEnemy/Action/ForceStopNamedPatternActionSO.cs; Assets/Scripts/Gameplay/Entities/Creature/NamedEnemy/Action/MoveNamedEnemyToSpawnPointActionSO.cs; Assets/Scripts/Gameplay/Entities/Creature/NamedEnemy/Action/NamedEnemyAcquirePlayerTargetActionSO.cs

## Assets/Scripts/Gameplay/Entities/Creature/NamedEnemy/NamedEnemyBlackboard.cs
- 분류: 일반 소스; 61행
- 타입: NamedEnemyBlackboard : MonoBehaviour
- 메서드: SelectAttack, FinishSelectedAttack, ClearSelectedAttack, SelectedAttackIs
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 0개: Assets/Prefabs/GameScene/Entity_WIP/Virus/NamedVirus/Mutarus/Mutarus_NamedVirus.prefab
- 타입 이름을 참조하는 소스 13개: Assets/Scripts/Gameplay/Entities/Creature/CommonEnemyStateMachine/Action/EnemyFireArcBombActionSO.cs; Assets/Scripts/Gameplay/Entities/Creature/CommonEnemyStateMachine/Action/EnemyFireProjectileActionSO.cs; Assets/Scripts/Gameplay/Entities/Creature/NamedEnemy/Action/SelectNamedAttackActionSO.cs; Assets/Scripts/Gameplay/Entities/Creature/NamedEnemy/Action/SetNamedBlackboardBoolActionSO.cs; Assets/Scripts/Gameplay/Entities/Creature/NamedEnemy/Condition/AttackFinishedConditionSO.cs; Assets/Scripts/Gameplay/Entities/Creature/NamedEnemy/Condition/CanEnterPatternConditionSO.cs; Assets/Scripts/Gameplay/Entities/Creature/NamedEnemy/Condition/HasSelectedAttackConditionSO.cs; Assets/Scripts/Gameplay/Entities/Creature/NamedEnemy/Condition/NamedIntroFinishedConditionSO.cs

## Assets/Scripts/Gameplay/Entities/Creature/NamedEnemy/NamedPatternController.cs
- 분류: 일반 소스; 267행
- 타입: NamedPatternResult, NamedPatternController : MonoBehaviour
- 메서드: Update, StartFirstPatternSchedule, StartRepeatPatternSchedule, StartPatternActiveTimer, StopSchedule, BeginPattern, MarkPrepareFinished, MarkActiveFinished, MarkResolved, SetPatternReady, StartSchedule, TickSchedule, TickPatternDurationUI, PublishPatternDurationUI, SetObjectiveProgress, ForceStopPattern
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 0개: Assets/Prefabs/GameScene/Entity_WIP/Virus/NamedVirus/Mutarus/Mutarus_NamedVirus.prefab
- 타입 이름을 참조하는 소스 11개: Assets/Scripts/Gameplay/Entities/Creature/NamedEnemy/Action/ApplyDamageMultiplierFromPatternActionSO.cs; Assets/Scripts/Gameplay/Entities/Creature/NamedEnemy/Action/ForceStopNamedPatternActionSO.cs; Assets/Scripts/Gameplay/Entities/Creature/NamedEnemy/Action/SetNamedPatternStateActionSO.cs; Assets/Scripts/Gameplay/Entities/Creature/NamedEnemy/Action/StartNamedPatternScheduleActionSO.cs; Assets/Scripts/Gameplay/Entities/Creature/NamedEnemy/Condition/NamedPatternReadyConditionSO.cs; Assets/Scripts/Gameplay/Entities/Creature/NamedEnemy/Condition/NamedPatternSequenceFinishedConditionSO.cs; Assets/Scripts/Gameplay/Entities/Creature/NamedEnemy/Condition/PatternActiveFinishedConditionSO.cs; Assets/Scripts/Gameplay/Entities/Creature/NamedEnemy/Condition/PatternPrepareFinishedConditionSO.cs

## Assets/Scripts/Gameplay/Entities/Creature/NamedEnemy/ScriptableObject/BossRewardCatalogSO.cs
- 분류: 일반 소스; 47행
- 타입: BossRewardCatalogSO : ScriptableObject
- 메서드: PickChoices
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 1개: Assets/ScriptableObject/GameScene/Character/Enemy/Virus/Definition_Config/NamedEnemy/BossRewardCatalog.asset
- 타입 이름을 참조하는 소스 1개: Assets/Scripts/Presentation/UI/Gameplay/BossReward/BossRewardUIController.cs

## Assets/Scripts/Gameplay/Entities/Creature/NamedEnemy/ScriptableObject/BossRewardOptionSO.cs
- 분류: 일반 소스; 28행
- 타입: BossRewardOptionSO : ScriptableObject
- 메서드: 
- 직렬화 참조 3개 / 지정 씬 참조 그래프 내 3개: Assets/ScriptableObject/GameScene/Character/Enemy/Virus/Definition_Config/NamedEnemy/Temp1_BossRewardOption.asset; Assets/ScriptableObject/GameScene/Character/Enemy/Virus/Definition_Config/NamedEnemy/Temp2_BossRewardOption.asset; Assets/ScriptableObject/GameScene/Character/Enemy/Virus/Definition_Config/NamedEnemy/Temp3_BossRewardOption.asset
- 타입 이름을 참조하는 소스 4개: Assets/Scripts/Core/Events/ScriptableObject/BossRewardSelectedEventChannelSO.cs; Assets/Scripts/Gameplay/Entities/Creature/NamedEnemy/ScriptableObject/BossRewardCatalogSO.cs; Assets/Scripts/Presentation/UI/Gameplay/BossReward/BossRewardChoiceButtonUI.cs; Assets/Scripts/Presentation/UI/Gameplay/BossReward/BossRewardUIController.cs

## Assets/Scripts/Gameplay/Entities/Creature/NamedEnemy/ScriptableObject/NamedAttackConfigSO.cs
- 분류: 일반 소스; 13행
- 타입: NamedAttackConfigSO : EnemyAttackConfigSO
- 메서드: 
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 3개: Assets/Scripts/Gameplay/Entities/Creature/NamedEnemy/Action/SetNamedBlackboardBoolActionSO.cs; Assets/Scripts/Gameplay/Entities/Creature/NamedEnemy/Mutarus/ScriptableObject/MutarusChargeAttackConfigSO.cs; Assets/Scripts/Gameplay/Entities/Creature/ScriptableObject/PoisonPuddleDamageConfigSO.cs

## Assets/Scripts/Gameplay/Entities/Creature/NamedEnemy/ScriptableObject/NamedEnemyStatConfigSO.cs
- 분류: 일반 소스; 51행
- 타입: NamedEnemyStatConfigSO : CreatureEnemyStatConfigSO, RepositionSpeedSource
- 메서드: 
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 2개: Assets/Scripts/Gameplay/Entities/Creature/NamedEnemy/Action/NamedEnemyRepositionAroundTargetActionSO.cs; Assets/Scripts/Gameplay/Entities/Creature/NamedEnemy/Mutarus/ScriptableObject/MutarusStatConfigSO.cs

## Assets/Scripts/Gameplay/Entities/Creature/NamedEnemy/ScriptableObject/NamedNormalAttackConfigSO.cs
- 분류: 일반 소스; 107행
- 타입: NamedNormalAttackOption, NamedNormalAttackConfigSO : ScriptableObject
- 메서드: IsSelectable, IsInPreferredRange, TryPickAttack
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 0개: Assets/ScriptableObject/GameScene/Character/Enemy/Virus/Definition_Config/NamedEnemy/Mutarus/Mutarus_NamedNormalAttack_Config.asset
- 타입 이름을 참조하는 소스 1개: Assets/Scripts/Gameplay/Entities/Creature/NamedEnemy/Action/SelectNamedAttackActionSO.cs

## Assets/Scripts/Gameplay/Entities/Creature/NamedEnemy/ScriptableObject/NamedPatternConfigSO.cs
- 분류: 일반 소스; 69행
- 타입: NamedPatternConfigSO : ScriptableObject
- 메서드: 
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 3개: Assets/Scripts/Gameplay/Entities/Creature/NamedEnemy/Action/ApplyDamageMultiplierFromPatternActionSO.cs; Assets/Scripts/Gameplay/Entities/Creature/NamedEnemy/Mutarus/ScriptableObject/MutarusPatternConfigSO.cs; Assets/Scripts/Gameplay/Entities/Creature/NamedEnemy/NamedPatternController.cs

## Assets/Scripts/Gameplay/Entities/Creature/NamedEnemy/ScriptableObject/NamedProjectileBurstConfigSO.cs
- 분류: 일반 소스; 14행
- 타입: NamedProjectileBurstConfigSO : EnemyFireProjectileConfigSO
- 메서드: 
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 1개: Assets/Scripts/Gameplay/Entities/Creature/NamedEnemy/Mutarus/ScriptableObject/MutarusProjectileAttackConfigSO.cs

## Assets/Scripts/Gameplay/Entities/Creature/NormalEnemy/Chaser/Audio/ChaserVirusAudio.cs
- 분류: 일반 소스; 27행
- 타입: ChaserVirusAudio : CreatureEnemyAudio
- 메서드: PlaySelfDestructCharge, PlaySelfDestructExplosion
- 직렬화 참조 2개 / 지정 씬 참조 그래프 내 0개: Assets/Prefabs/GameScene/Entity_WIP/Virus/NormalVirus/Chaser/Chaser.prefab; Assets/Prefabs/GameScene/Entity_WIP/Virus/NormalVirus/Chaser/Chaser_Upgrade.prefab
- 타입 이름을 참조하는 소스 0개: 

## Assets/Scripts/Gameplay/Entities/Creature/NormalEnemy/Chaser/Effect/ChaserVirusEffectController.cs
- 분류: 일반 소스; 42행
- 타입: ChaserVirusEffectController : EnemyEffectController
- 메서드: Start, PlaySelfDestructChargeParticles, StopSelfDestructChargeParticles, PlaySelfDestructExplosionParticles
- 직렬화 참조 2개 / 지정 씬 참조 그래프 내 0개: Assets/Prefabs/GameScene/Entity_WIP/Virus/NormalVirus/Chaser/Chaser.prefab; Assets/Prefabs/GameScene/Entity_WIP/Virus/NormalVirus/Chaser/Chaser_Upgrade.prefab
- 타입 이름을 참조하는 소스 3개: Assets/Scripts/Gameplay/Entities/Creature/NormalEnemy/Chaser/StateMachine/Action/PlayChaserSelfDestructChargeParticlesActionSO.cs; Assets/Scripts/Gameplay/Entities/Creature/NormalEnemy/Chaser/StateMachine/Action/PlayChaserSelfDestructExplosionParticlesActionSO.cs; Assets/Scripts/Gameplay/Entities/Creature/NormalEnemy/Chaser/StateMachine/Action/StopChaserSelfDestructChargeParticlesActionSO.cs

## Assets/Scripts/Gameplay/Entities/Creature/NormalEnemy/Chaser/ScriptableObject/ChaserSelfDestructConfigSO.cs
- 분류: 일반 소스; 49행
- 타입: ChaserSelfDestructConfigSO : EnemyAttackConfigSO
- 메서드: 
- 직렬화 참조 2개 / 지정 씬 참조 그래프 내 0개: Assets/ScriptableObject/GameScene/Character/Enemy/Virus/Definition_Config/NormalEnemy/Chaser/ChaserSelfDestructConfig.asset; Assets/ScriptableObject/GameScene/Character/Enemy/Virus/Definition_Config/NormalEnemy/Chaser_Upgrade/ChaserUpgrade_SelfDestructConfig.asset
- 타입 이름을 참조하는 소스 3개: Assets/Scripts/Gameplay/Entities/Creature/CommonEnemyStateMachine/AttackExecutor/EnemyAttackExecutorFactory.cs; Assets/Scripts/Gameplay/Entities/Creature/CommonEnemyStateMachine/AttackExecutor/EnemySelfDestructAttackExecutor.cs; Assets/Scripts/Gameplay/Entities/Creature/NormalEnemy/Chaser/StateMachine/Action/ChaserVirusSelfDestructActionSO.cs

## Assets/Scripts/Gameplay/Entities/Creature/NormalEnemy/Chaser/ScriptableObject/ChaserStatConfigSO.cs
- 분류: 일반 소스; 15행
- 타입: ChaserStatConfigSO : CreatureEnemyStatConfigSO
- 메서드: 
- 직렬화 참조 2개 / 지정 씬 참조 그래프 내 0개: Assets/ScriptableObject/GameScene/Character/Enemy/Virus/Definition_Config/NormalEnemy/Chaser/Chaser_Stat_Config.asset; Assets/ScriptableObject/GameScene/Character/Enemy/Virus/Definition_Config/NormalEnemy/Chaser_Upgrade/ChaserUpgrade_Stat_Config.asset
- 타입 이름을 참조하는 소스 0개: 

## Assets/Scripts/Gameplay/Entities/Creature/NormalEnemy/Chaser/StateMachine/Action/ChaserVirusSelfDestructActionSO.cs
- 분류: 일반 소스; 210행
- 타입: ChaserVirusSelfDestructActionSO : StateActionSO, ChaserVirusSelfDestructAction : StateAction
- 메서드: CreateAction, Awake, OnStateEnter, OnUpdate, Explode, PaintVirus, HitPlayers, StopAgent, KillSelf, ResolveMaskRenderManager
- 직렬화 참조 2개 / 지정 씬 참조 그래프 내 0개: Assets/ScriptableObject/StateMachine/Enemy/NormalEnemy/Chaser/Action/ChaserVirusSelfDestructAction.asset; Assets/ScriptableObject/StateMachine/Enemy/NormalEnemy/Roller/Action/ChaserVirusSelfDestructAction.asset
- 타입 이름을 참조하는 소스 0개: 

## Assets/Scripts/Gameplay/Entities/Creature/NormalEnemy/Chaser/StateMachine/Action/PlayChaserSelfDestructChargeParticlesActionSO.cs
- 분류: 일반 소스; 65행
- 타입: PlayChaserSelfDestructChargeParticlesActionSO : StateActionSO<PlayChaserSelfDestructChargeParticlesAction>, PlayChaserSelfDestructChargeParticlesAction : StateAction
- 메서드: Awake, OnStateEnter, OnUpdate, ResolveEffects
- 직렬화 참조 2개 / 지정 씬 참조 그래프 내 0개: Assets/ScriptableObject/StateMachine/Enemy/NormalEnemy/Chaser/Action/Particle/PlayChaserSelfDestructChargeParticlesAction.asset; Assets/ScriptableObject/StateMachine/Enemy/NormalEnemy/Roller/Action/Particle/PlayChaserSelfDestructChargeParticlesAction.asset
- 타입 이름을 참조하는 소스 0개: 

## Assets/Scripts/Gameplay/Entities/Creature/NormalEnemy/Chaser/StateMachine/Action/PlayChaserSelfDestructExplosionParticlesActionSO.cs
- 분류: 일반 소스; 65행
- 타입: PlayChaserSelfDestructExplosionParticlesActionSO : StateActionSO<PlayChaserSelfDestructExplosionParticlesAction>, PlayChaserSelfDestructExplosionParticlesAction : StateAction
- 메서드: Awake, OnStateEnter, OnUpdate, ResolveEffects
- 직렬화 참조 2개 / 지정 씬 참조 그래프 내 0개: Assets/ScriptableObject/StateMachine/Enemy/NormalEnemy/Chaser/Action/Particle/PlayChaserSelfDestructExplosionParticlesAction.asset; Assets/ScriptableObject/StateMachine/Enemy/NormalEnemy/Roller/Action/Particle/PlayChaserSelfDestructExplosionParticlesAction.asset
- 타입 이름을 참조하는 소스 0개: 

## Assets/Scripts/Gameplay/Entities/Creature/NormalEnemy/Chaser/StateMachine/Action/StopChaserSelfDestructChargeParticlesActionSO.cs
- 분류: 일반 소스; 65행
- 타입: StopChaserSelfDestructChargeParticlesActionSO : StateActionSO<StopChaserSelfDestructChargeParticlesAction>, StopChaserSelfDestructChargeParticlesAction : StateAction
- 메서드: Awake, OnStateEnter, OnUpdate, ResolveEffects
- 직렬화 참조 2개 / 지정 씬 참조 그래프 내 0개: Assets/ScriptableObject/StateMachine/Enemy/NormalEnemy/Chaser/Action/Particle/StopChaserSelfDestructChargeParticlesAction.asset; Assets/ScriptableObject/StateMachine/Enemy/NormalEnemy/Roller/Action/Particle/StopChaserSelfDestructChargeParticlesAction.asset
- 타입 이름을 참조하는 소스 0개: 

## Assets/Scripts/Gameplay/Entities/Creature/NormalEnemy/CommonNormalEnemyStateMachine/Action/CastInfectionActionSO.cs
- 분류: 일반 소스; 153행
- 타입: CastInfectionActionSO : StateActionSO, CastInfectionAction : StateAction
- 메서드: CreateAction, Awake, OnStateEnter, OnUpdate, OnStateExit, ApplyInfection, CacheMaskRenderManager, DrawImpactMarker
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 0개: Assets/ScriptableObject/StateMachine/Enemy/Actions/CastInfectionAction.asset
- 타입 이름을 참조하는 소스 0개: 

## Assets/Scripts/Gameplay/Entities/Creature/NormalEnemy/CommonNormalEnemyStateMachine/Action/DestroyNormalEnemyAfterDelayActionSO.cs
- 분류: 일반 소스; 124행
- 타입: DestroyNormalEnemyAfterDelayActionSO : StateActionSO<DestroyEnemyAfterDelayAction>, DestroyEnemyAfterDelayAction : StateAction
- 메서드: Awake, OnStateEnter, OnUpdate, SetColliders, StopAgent, DestroyNow
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 1개: Assets/ScriptableObject/StateMachine/Enemy/NormalEnemy/CommonAction/DestroyNormalEnemyAfterDelayAction.asset
- 타입 이름을 참조하는 소스 0개: 

## Assets/Scripts/Gameplay/Entities/Creature/NormalEnemy/CommonNormalEnemyStateMachine/Action/MoveToInfectionTargetActionSO.cs
- 분류: 일반 소스; 108행
- 타입: MoveToInfectionTargetActionSO : StateActionSO, MoveToInfectionTargetAction : StateAction
- 메서드: CreateAction, Awake, OnStateEnter, OnUpdate, OnStateExit
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 0개: Assets/ScriptableObject/StateMachine/Enemy/Actions/MoveToInfectionTargetAction.asset
- 타입 이름을 참조하는 소스 0개: 

## Assets/Scripts/Gameplay/Entities/Creature/NormalEnemy/CommonNormalEnemyStateMachine/Action/NormalEnemyAttackActionSO.cs
- 분류: 일반 소스; 158행
- 타입: NormalEnemyAttackActionSO : StateActionSO<NormalEnemyAttackAction>, NormalEnemyAttackAction : StateAction
- 메서드: Awake, OnStateEnter, OnUpdate, OnStateExit, TryPickAttack, TryStartRuntime, FinishInvalidAttack, ResolveTargetDistance
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 0개: Assets/ScriptableObject/StateMachine/Enemy/NormalEnemy/CommonAction/NormalEnemyAttackAction.asset
- 타입 이름을 참조하는 소스 0개: 

## Assets/Scripts/Gameplay/Entities/Creature/NormalEnemy/CommonNormalEnemyStateMachine/Action/SearchInfectionTargetActionSO.cs
- 분류: 일반 소스; 360행
- 타입: SearchInfectionTargetActionSO : StateActionSO, SearchInfectionTargetAction : StateAction
- 메서드: CreateAction, Awake, OnStateEnter, OnUpdate, Search, TryFindTarget, TryBuildCandidate, ScoreCandidate, ScoreSample, ResolveSampleScore, CacheMaskRenderManager, DrawCandidate, DrawMarker, IsInsideBoundsXZ, FlatDistance
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 0개: Assets/ScriptableObject/StateMachine/Enemy/Actions/SearchInfectionTargetAction.asset
- 타입 이름을 참조하는 소스 0개: 

## Assets/Scripts/Gameplay/Entities/Creature/NormalEnemy/CommonNormalEnemyStateMachine/Action/WanderRandomInSectorActionSO.cs
- 분류: 일반 소스; 198행
- 타입: WanderRandomInSectorActionSO : StateActionSO, WanderRandomInSectorAction : StateAction
- 메서드: CreateAction, Awake, OnStateEnter, OnUpdate, OnStateExit, TryPickAndApplyDestination, TryPickDestination, IsInsideBoundsXZ
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 0개: Assets/ScriptableObject/StateMachine/Enemy/Actions/WanderRandomInSectorAction.asset
- 타입 이름을 참조하는 소스 0개: 

## Assets/Scripts/Gameplay/Entities/Creature/NormalEnemy/CommonNormalEnemyStateMachine/Conditions/ArrivedToInfectionTargetConditionSO.cs
- 분류: 일반 소스; 62행
- 타입: ArrivedToInfectionTargetConditionSO : StateConditionSO<ArrivedToInfectionTargetCondition>, ArrivedToInfectionTargetCondition : Condition
- 메서드: Awake, Statement
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 0개: Assets/ScriptableObject/StateMachine/Enemy/Condition/ArrivedToInfectionTargetCondition.asset
- 타입 이름을 참조하는 소스 0개: 

## Assets/Scripts/Gameplay/Entities/Creature/NormalEnemy/CommonNormalEnemyStateMachine/Conditions/HasCurrentTargetConditionSO.cs
- 분류: 일반 소스; 36행
- 타입: HasCurrentTargetConditionSO : StateConditionSO<HasCurrentTargetCondition>, HasCurrentTargetCondition : Condition
- 메서드: Awake, Statement
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 0개: Assets/ScriptableObject/StateMachine/Enemy/Condition/HasCurrentTargetCondition.asset
- 타입 이름을 참조하는 소스 0개: 

## Assets/Scripts/Gameplay/Entities/Creature/NormalEnemy/CommonNormalEnemyStateMachine/Conditions/HasInfectionTargetConditionSO.cs
- 분류: 일반 소스; 32행
- 타입: HasInfectionTargetConditionSO : StateConditionSO<HasInfectionTargetCondition>, HasInfectionTargetCondition : Condition
- 메서드: Awake, Statement
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 0개: Assets/ScriptableObject/StateMachine/Enemy/Condition/HasInfectionTargetCondition.asset
- 타입 이름을 참조하는 소스 0개: 

## Assets/Scripts/Gameplay/Entities/Creature/NormalEnemy/CommonNormalEnemyStateMachine/Conditions/InfectionTargetValidConditionSO.cs
- 분류: 일반 소스; 32행
- 타입: InfectionTargetValidConditionSO : StateConditionSO<InfectionTargetValidCondition>, InfectionTargetValidCondition : Condition
- 메서드: Awake, Statement
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 0개: Assets/ScriptableObject/StateMachine/Enemy/Condition/InfectionTargetValidCondition.asset
- 타입 이름을 참조하는 소스 0개: 

## Assets/Scripts/Gameplay/Entities/Creature/NormalEnemy/CommonNormalEnemyStateMachine/Conditions/IsKnockbackRequestedConditionSO.cs
- 분류: 일반 소스; 13행
- 타입: IsKnockbackRequestedConditionSO : HasPendingKnockbackConditionSO
- 메서드: 
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 0개: Assets/ScriptableObject/StateMachine/Enemy/Condition/IsKnockbackRequestedCondition.asset
- 타입 이름을 참조하는 소스 0개: 

## Assets/Scripts/Gameplay/Entities/Creature/NormalEnemy/CommonNormalEnemyStateMachine/Conditions/MoveTimeoutConditionSO.cs
- 분류: 일반 소스; 13행
- 타입: MoveTimeoutConditionSO : TimeElapsedConditionSO
- 메서드: 
- 직렬화 참조 2개 / 지정 씬 참조 그래프 내 0개: Assets/ScriptableObject/StateMachine/Enemy/Condition/InfectionSearchMoveTimeoutCondition.asset; Assets/ScriptableObject/StateMachine/Enemy/Condition/WanderMoveTimeoutCondition.asset
- 타입 이름을 참조하는 소스 0개: 

## Assets/Scripts/Gameplay/Entities/Creature/NormalEnemy/CommonNormalEnemyStateMachine/Conditions/NormalEnemyAttackFinishedConditionSO.cs
- 분류: 일반 소스; 34행
- 타입: NormalEnemyAttackFinishedConditionSO : StateConditionSO<NormalEnemyAttackFinishedCondition>, NormalEnemyAttackFinishedCondition : Condition
- 메서드: Awake, Statement
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 0개: Assets/ScriptableObject/StateMachine/Enemy/Condition/NormalEnemyAttackFinishedCondition.asset
- 타입 이름을 참조하는 소스 0개: 

## Assets/Scripts/Gameplay/Entities/Creature/NormalEnemy/CommonNormalEnemyStateMachine/Conditions/NormalEnemyHasSelectableAttackConditionSO.cs
- 분류: 일반 소스; 58행
- 타입: NormalEnemyHasSelectableAttackConditionSO : StateConditionSO<NormalEnemyHasSelectableAttackCondition>, NormalEnemyHasSelectableAttackCondition : Condition
- 메서드: Awake, Statement, ResolveTargetDistance
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 0개: Assets/ScriptableObject/StateMachine/Enemy/Condition/NormalEnemyHasSelectableAttackCondition.asset
- 타입 이름을 참조하는 소스 0개: 

## Assets/Scripts/Gameplay/Entities/Creature/NormalEnemy/CommonNormalEnemyStateMachine/Conditions/SearchSucceededConditionSO.cs
- 분류: 일반 소스; 32행
- 타입: SearchSucceededConditionSO : StateConditionSO<SearchSucceededCondition>, SearchSucceededCondition : Condition
- 메서드: Awake, Statement
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 0개: Assets/ScriptableObject/StateMachine/Enemy/Condition/SearchSucceededCondition.asset
- 타입 이름을 참조하는 소스 0개: 

## Assets/Scripts/Gameplay/Entities/Creature/NormalEnemy/CommonNormalEnemyStateMachine/Conditions/TimerCastInfectionConditionSO.cs
- 분류: 일반 소스; 13행
- 타입: TimerCastInfectionConditionSO : TimeElapsedConditionSO
- 메서드: 
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 0개: Assets/ScriptableObject/StateMachine/Enemy/Condition/TimerCastInfectionCondition.asset
- 타입 이름을 참조하는 소스 0개: 

## Assets/Scripts/Gameplay/Entities/Creature/NormalEnemy/CommonNormalEnemyStateMachine/Conditions/TimerKnockbackConditionSO.cs
- 분류: 일반 소스; 13행
- 타입: TimerKnockbackConditionSO : TimeElapsedConditionSO
- 메서드: 
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 0개: Assets/ScriptableObject/StateMachine/Enemy/Condition/TimerKnockbackCondition.asset
- 타입 이름을 참조하는 소스 0개: 

## Assets/Scripts/Gameplay/Entities/Creature/NormalEnemy/NormalEnemy.cs
- 분류: 일반 소스; 10행
- 타입: NormalEnemy : Enemy
- 메서드: 
- 직렬화 참조 7개 / 지정 씬 참조 그래프 내 0개: Assets/Prefabs/GameScene/Entity_WIP/Virus/NormalVirus/Chaser/Chaser.prefab; Assets/Prefabs/GameScene/Entity_WIP/Virus/NormalVirus/Chaser/Chaser_Upgrade.prefab; Assets/Prefabs/GameScene/Entity_WIP/Virus/NormalVirus/Roller/Roller.prefab; Assets/Prefabs/GameScene/Entity_WIP/Virus/NormalVirus/Spitter/Spitter_ThreeShot_Virus.prefab; Assets/Prefabs/GameScene/Entity_WIP/Virus/NormalVirus/Spitter/Spitter_Virus.prefab; Assets/Prefabs/GameScene/Entity_WIP/Virus/NormalVirus/Worm/Worm.prefab; Assets/Prefabs/GameScene/Entity_WIP/Virus/NormalVirus/Worm/Worm_Upgrade.prefab
- 타입 이름을 참조하는 소스 43개: Assets/Scripts/Authoring/Editor/StandardEnemyConfigPrefabLinkRepair.cs; Assets/Scripts/Gameplay/Entities/Creature/NormalEnemy/Chaser/Audio/ChaserVirusAudio.cs; Assets/Scripts/Gameplay/Entities/Creature/NormalEnemy/Chaser/Effect/ChaserVirusEffectController.cs; Assets/Scripts/Gameplay/Entities/Creature/NormalEnemy/Chaser/ScriptableObject/ChaserSelfDestructConfigSO.cs; Assets/Scripts/Gameplay/Entities/Creature/NormalEnemy/Chaser/ScriptableObject/ChaserStatConfigSO.cs; Assets/Scripts/Gameplay/Entities/Creature/NormalEnemy/Chaser/StateMachine/Action/ChaserVirusSelfDestructActionSO.cs; Assets/Scripts/Gameplay/Entities/Creature/NormalEnemy/Chaser/StateMachine/Action/PlayChaserSelfDestructChargeParticlesActionSO.cs; Assets/Scripts/Gameplay/Entities/Creature/NormalEnemy/Chaser/StateMachine/Action/PlayChaserSelfDestructExplosionParticlesActionSO.cs

## Assets/Scripts/Gameplay/Entities/Creature/NormalEnemy/Roller/Audio/RollerVirusAudio.cs
- 분류: 일반 소스; 20행
- 타입: RollerVirusAudio : CreatureEnemyAudio
- 메서드: PlayRolling
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 0개: Assets/Prefabs/GameScene/Entity_WIP/Virus/NormalVirus/Roller/Roller.prefab
- 타입 이름을 참조하는 소스 0개: 

## Assets/Scripts/Gameplay/Entities/Creature/NormalEnemy/Roller/Effect/RollerVirusEffectController.cs
- 분류: 일반 소스; 33행
- 타입: RollerVirusEffectController : EnemyEffectController
- 메서드: Start, PlayRollingParticles, StopRollingParticles
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 0개: Assets/Prefabs/GameScene/Entity_WIP/Virus/NormalVirus/Roller/Roller.prefab
- 타입 이름을 참조하는 소스 0개: 

## Assets/Scripts/Gameplay/Entities/Creature/NormalEnemy/Roller/ScriptableObject/RollerAttackConfigSO.cs
- 분류: 일반 소스; 69행
- 타입: RollerAttackConfigSO : EnemyAttackConfigSO
- 메서드: 
- 직렬화 참조 2개 / 지정 씬 참조 그래프 내 0개: Assets/ScriptableObject/GameScene/Character/Enemy/Virus/Definition_Config/NormalEnemy/Roller/RollerAttackConfig.asset; Assets/ScriptableObject/GameScene/Character/Enemy/Virus/Definition_Config/NormalEnemy/RollerUpgrade/RollerUpgrade_AttackConfig.asset
- 타입 이름을 참조하는 소스 1개: Assets/Scripts/Gameplay/Entities/Creature/NormalEnemy/Roller/StateMachine/Action/RollerRollingActionSO.cs

## Assets/Scripts/Gameplay/Entities/Creature/NormalEnemy/Roller/ScriptableObject/RollerStatConfigSO.cs
- 분류: 일반 소스; 16행
- 타입: RollerStatConfigSO : CreatureEnemyStatConfigSO
- 메서드: 
- 직렬화 참조 2개 / 지정 씬 참조 그래프 내 0개: Assets/ScriptableObject/GameScene/Character/Enemy/Virus/Definition_Config/NormalEnemy/Roller/Roller_Stat_Config.asset; Assets/ScriptableObject/GameScene/Character/Enemy/Virus/Definition_Config/NormalEnemy/RollerUpgrade/RollerUpgrade_Stat_Config.asset
- 타입 이름을 참조하는 소스 0개: 

## Assets/Scripts/Gameplay/Entities/Creature/NormalEnemy/Roller/StateMachine/Action/RollerRollingActionSO.cs
- 분류: 일반 소스; 495행
- 타입: RollerRollingActionSO : StateActionSO<RollerRollingAction>, RollerRollingAction : StateAction
- 메서드: Awake, OnStateEnter, OnUpdate, OnStateExit, MoveWithBounces, TryGetBounce, TryGetPhysicsBounce, TryGetSectorBoundsBounce, TestAxisBoundary, HandleBounce, TryRetargetToPlayerOnBounce, TryGetTargetDirection, PickInitialDirection, ScheduleNextAttackReadyTime, ResolveCurrentSpeed, RotateAlongDirection, SetPosition, EnsureHitBuffer, StopRigidbodyDrift, DisableAgentIfNeeded, RestoreAgentIfNeeded, IsSelfCollider, PickRandomPlanarDirection, FlattenDirection
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 0개: Assets/ScriptableObject/StateMachine/Enemy/NormalEnemy/Roller/Action/RollerRollingAction.asset
- 타입 이름을 참조하는 소스 0개: 

## Assets/Scripts/Gameplay/Entities/Creature/NormalEnemy/ScriptableObjects/DefinitionConfig/NormalEnemyBehaviorConfigSO.cs
- 분류: 일반 소스; 109행
- 타입: NormalEnemyBehaviorConfigSO : ScriptableObject
- 메서드: 
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 0개: Assets/ScriptableObject/GameScene/Character/Enemy/Virus/Definition_Config/NormalEnemy/NormalEnemyBehaviorConfig.asset
- 타입 이름을 참조하는 소스 4개: Assets/Scripts/Gameplay/Entities/Creature/NormalEnemy/CommonNormalEnemyStateMachine/Action/CastInfectionActionSO.cs; Assets/Scripts/Gameplay/Entities/Creature/NormalEnemy/CommonNormalEnemyStateMachine/Action/MoveToInfectionTargetActionSO.cs; Assets/Scripts/Gameplay/Entities/Creature/NormalEnemy/CommonNormalEnemyStateMachine/Action/SearchInfectionTargetActionSO.cs; Assets/Scripts/Gameplay/Entities/Creature/NormalEnemy/CommonNormalEnemyStateMachine/Action/WanderRandomInSectorActionSO.cs

## Assets/Scripts/Gameplay/Entities/Creature/NormalEnemy/Spitter/Audio/SpitterVirusAudio.cs
- 분류: 일반 소스; 12행
- 타입: SpitterVirusAudio : CreatureEnemyAudio
- 메서드: 
- 직렬화 참조 2개 / 지정 씬 참조 그래프 내 0개: Assets/Prefabs/GameScene/Entity_WIP/Virus/NormalVirus/Spitter/Spitter_ThreeShot_Virus.prefab; Assets/Prefabs/GameScene/Entity_WIP/Virus/NormalVirus/Spitter/Spitter_Virus.prefab
- 타입 이름을 참조하는 소스 0개: 

## Assets/Scripts/Gameplay/Entities/Creature/NormalEnemy/Spitter/Effect/SpitterVirusEffectController.cs
- 분류: 일반 소스; 12행
- 타입: SpitterVirusEffectController : EnemyEffectController
- 메서드: 
- 직렬화 참조 2개 / 지정 씬 참조 그래프 내 0개: Assets/Prefabs/GameScene/Entity_WIP/Virus/NormalVirus/Spitter/Spitter_ThreeShot_Virus.prefab; Assets/Prefabs/GameScene/Entity_WIP/Virus/NormalVirus/Spitter/Spitter_Virus.prefab
- 타입 이름을 참조하는 소스 0개: 

## Assets/Scripts/Gameplay/Entities/Creature/NormalEnemy/Spitter/ScriptableObjects/SpitterAttackConfigSO.cs
- 분류: 일반 소스; 51행
- 타입: SpitterAttackConfigSO : EnemyFireProjectileConfigSO
- 메서드: 
- 직렬화 참조 2개 / 지정 씬 참조 그래프 내 0개: Assets/ScriptableObject/GameScene/Character/Enemy/Virus/Definition_Config/NormalEnemy/Spitter/SpitterProjectileAttackConfig.asset; Assets/ScriptableObject/GameScene/Character/Enemy/Virus/Definition_Config/NormalEnemy/Spitter_ThreeShot/Spitter_ThreeShot_ProjectileAttackConfig.asset
- 타입 이름을 참조하는 소스 2개: Assets/Scripts/Gameplay/Entities/Creature/NormalEnemy/Spitter/StateMachine/Action/SpitterAttackRecoverActionSO.cs; Assets/Scripts/Gameplay/Entities/Creature/NormalEnemy/Spitter/StateMachine/Action/SpitterAttackWindupActionSO.cs

## Assets/Scripts/Gameplay/Entities/Creature/NormalEnemy/Spitter/ScriptableObjects/SpitterStatConfigSO.cs
- 분류: 일반 소스; 15행
- 타입: SpitterStatConfigSO : CreatureEnemyStatConfigSO
- 메서드: 
- 직렬화 참조 2개 / 지정 씬 참조 그래프 내 0개: Assets/ScriptableObject/GameScene/Character/Enemy/Virus/Definition_Config/NormalEnemy/Spitter/Spitter_Stat_Config.asset; Assets/ScriptableObject/GameScene/Character/Enemy/Virus/Definition_Config/NormalEnemy/Spitter_ThreeShot/Spitter_ThreeShot_Stat_Config.asset
- 타입 이름을 참조하는 소스 0개: 

## Assets/Scripts/Gameplay/Entities/Creature/NormalEnemy/Spitter/StateMachine/Action/SpitterAttackRecoverActionSO.cs
- 분류: 일반 소스; 244행
- 타입: SpitterAttackRecoverActionSO : StateActionSO<SpitterAttackRecoverAction>, SpitterAttackRecoverAction : StateAction
- 메서드: Awake, OnStateEnter, OnUpdate, OnStateExit, PickRecoverDestination, RotateToward, IsAgentReady, GetFlatDistance, DrawRecoverDebug, DrawDestinationDebug, DrawCircle
- 직렬화 참조 2개 / 지정 씬 참조 그래프 내 0개: Assets/ScriptableObject/StateMachine/Enemy/NormalEnemy/Spitter/Action/SpitterAttackRecoverAction.asset; Assets/ScriptableObject/StateMachine/Enemy/NormalEnemy/Spitter_ThreeShot/Action/Spitter_ThreeShot_AttackRecoverAction.asset
- 타입 이름을 참조하는 소스 0개: 

## Assets/Scripts/Gameplay/Entities/Creature/NormalEnemy/Spitter/StateMachine/Action/SpitterAttackWindupActionSO.cs
- 분류: 일반 소스; 121행
- 타입: SpitterAttackWindupActionSO : StateActionSO<SpitterAttackWindupAction>, SpitterAttackWindupAction : StateAction
- 메서드: Awake, OnStateEnter, OnUpdate, OnStateExit, PickStrafePoint, RotateToward, IsAgentReady
- 직렬화 참조 2개 / 지정 씬 참조 그래프 내 0개: Assets/ScriptableObject/StateMachine/Enemy/NormalEnemy/Spitter/Action/SpitterAttackWindupAction.asset; Assets/ScriptableObject/StateMachine/Enemy/NormalEnemy/Spitter_ThreeShot/Action/Spitter_ThreeShot_AttackWindupAction.asset
- 타입 이름을 참조하는 소스 0개: 

## Assets/Scripts/Gameplay/Entities/Creature/NormalEnemy/Spitter/StateMachine/Action/SpitterFireProjectileActionSO.cs
- 분류: 일반 소스; 14행
- 타입: SpitterFireProjectileActionSO : EnemyFireProjectileActionSO
- 메서드: 
- 직렬화 참조 2개 / 지정 씬 참조 그래프 내 0개: Assets/ScriptableObject/StateMachine/Enemy/NormalEnemy/Spitter/Action/SpitterFireProjectileAction.asset; Assets/ScriptableObject/StateMachine/Enemy/NormalEnemy/Spitter_ThreeShot/Action/Spitter_ThreeShot_FireProjectileAction.asset
- 타입 이름을 참조하는 소스 0개: 

## Assets/Scripts/Gameplay/Entities/Creature/NormalEnemy/Worm/Audio/WormVirusAudio.cs
- 분류: 일반 소스; 12행
- 타입: WormVirusAudio : CreatureEnemyAudio
- 메서드: 
- 직렬화 참조 2개 / 지정 씬 참조 그래프 내 0개: Assets/Prefabs/GameScene/Entity_WIP/Virus/NormalVirus/Worm/Worm.prefab; Assets/Prefabs/GameScene/Entity_WIP/Virus/NormalVirus/Worm/Worm_Upgrade.prefab
- 타입 이름을 참조하는 소스 0개: 

## Assets/Scripts/Gameplay/Entities/Creature/NormalEnemy/Worm/Effect/WormVirusEffectController.cs
- 분류: 일반 소스; 12행
- 타입: WormVirusEffectController : EnemyEffectController
- 메서드: 
- 직렬화 참조 2개 / 지정 씬 참조 그래프 내 0개: Assets/Prefabs/GameScene/Entity_WIP/Virus/NormalVirus/Worm/Worm.prefab; Assets/Prefabs/GameScene/Entity_WIP/Virus/NormalVirus/Worm/Worm_Upgrade.prefab
- 타입 이름을 참조하는 소스 0개: 

## Assets/Scripts/Gameplay/Entities/Creature/NormalEnemy/Worm/ScriptableObject/WormStatConfigSO.cs
- 분류: 일반 소스; 15행
- 타입: WormStatConfigSO : CreatureEnemyStatConfigSO
- 메서드: 
- 직렬화 참조 2개 / 지정 씬 참조 그래프 내 0개: Assets/ScriptableObject/GameScene/Character/Enemy/Virus/Definition_Config/NormalEnemy/Worm/Worm_Stat_Config.asset; Assets/ScriptableObject/GameScene/Character/Enemy/Virus/Definition_Config/NormalEnemy/Worm_Upgrade/Worm_Upgrade_Stat_Config.asset
- 타입 이름을 참조하는 소스 0개: 

## Assets/Scripts/Gameplay/Entities/Creature/ScriptableObject/CreatureEnemyStatConfigSO.cs
- 분류: 일반 소스; 152행
- 타입: EnemyAttackSelectionOption, CreatureEnemyStatConfigSO : EnemyStatConfigSO
- 메서드: IsSelectable, ResolveInitialHealth, HasSelectableAttack, TryPickAttack
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 10개: Assets/Scripts/Gameplay/Entities/Creature/EnemyContactDamage.cs; Assets/Scripts/Gameplay/Entities/Creature/EnemyMovementStatsProvider.cs; Assets/Scripts/Gameplay/Entities/Creature/EnemyVirusTrail.cs; Assets/Scripts/Gameplay/Entities/Creature/NamedEnemy/ScriptableObject/NamedEnemyStatConfigSO.cs; Assets/Scripts/Gameplay/Entities/Creature/NormalEnemy/Chaser/ScriptableObject/ChaserStatConfigSO.cs; Assets/Scripts/Gameplay/Entities/Creature/NormalEnemy/CommonNormalEnemyStateMachine/Action/NormalEnemyAttackActionSO.cs; Assets/Scripts/Gameplay/Entities/Creature/NormalEnemy/Roller/ScriptableObject/RollerStatConfigSO.cs; Assets/Scripts/Gameplay/Entities/Creature/NormalEnemy/Spitter/ScriptableObjects/SpitterStatConfigSO.cs

## Assets/Scripts/Gameplay/Entities/Creature/ScriptableObject/EnemyArcBombAttackConfigSO.cs
- 분류: 일반 소스; 95행
- 타입: EnemyArcBombAttackConfigSO : PoisonPuddleDamageConfigSO
- 메서드: ResolveRadius
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 4개: Assets/Scripts/Gameplay/Entities/Creature/CommonEnemyStateMachine/Action/EnemyFireArcBombActionSO.cs; Assets/Scripts/Gameplay/Entities/Creature/CommonEnemyStateMachine/AttackExecutor/EnemyArcBombAttackExecutor.cs; Assets/Scripts/Gameplay/Entities/Creature/CommonEnemyStateMachine/AttackExecutor/EnemyAttackExecutorFactory.cs; Assets/Scripts/Gameplay/Entities/Creature/NamedEnemy/Mutarus/ScriptableObject/MutarusPoisonPuddleAttackConfigSO.cs

## Assets/Scripts/Gameplay/Entities/Creature/ScriptableObject/EnemyAttackConfigSO.cs
- 분류: 일반 소스; 113행
- 타입: EnemyAttackBehaviorType, EnemyAttackConfigSO : ScriptableObject
- 메서드: IsInActivationRange, TryFaceWorldPoint, TryFaceDirection
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 14개: Assets/Scripts/Gameplay/Entities/Creature/CommonEnemyStateMachine/AttackExecutor/EnemyAttackExecutorController.cs; Assets/Scripts/Gameplay/Entities/Creature/CommonEnemyStateMachine/AttackExecutor/EnemyAttackExecutorFactory.cs; Assets/Scripts/Gameplay/Entities/Creature/NamedEnemy/Action/SelectNamedAttackActionSO.cs; Assets/Scripts/Gameplay/Entities/Creature/NamedEnemy/Condition/SelectedNamedAttackConditionSO.cs; Assets/Scripts/Gameplay/Entities/Creature/NamedEnemy/Mutarus/ScriptableObject/MutarusChargeAttackConfigSO.cs; Assets/Scripts/Gameplay/Entities/Creature/NamedEnemy/NamedEnemyBlackboard.cs; Assets/Scripts/Gameplay/Entities/Creature/NamedEnemy/ScriptableObject/NamedAttackConfigSO.cs; Assets/Scripts/Gameplay/Entities/Creature/NamedEnemy/ScriptableObject/NamedNormalAttackConfigSO.cs

## Assets/Scripts/Gameplay/Entities/Creature/ScriptableObject/EnemyFireProjectileConfigSO.cs
- 분류: 일반 소스; 78행
- 타입: EnemyFireProjectileConfigSO : EnemyAttackConfigSO
- 메서드: 
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 5개: Assets/Scripts/Gameplay/Entities/Creature/CommonEnemyStateMachine/Action/EnemyFireProjectileActionSO.cs; Assets/Scripts/Gameplay/Entities/Creature/CommonEnemyStateMachine/AttackExecutor/EnemyAttackExecutorFactory.cs; Assets/Scripts/Gameplay/Entities/Creature/CommonEnemyStateMachine/AttackExecutor/EnemyProjectileAttackExecutor.cs; Assets/Scripts/Gameplay/Entities/Creature/NamedEnemy/ScriptableObject/NamedProjectileBurstConfigSO.cs; Assets/Scripts/Gameplay/Entities/Creature/NormalEnemy/Spitter/ScriptableObjects/SpitterAttackConfigSO.cs

## Assets/Scripts/Gameplay/Entities/Creature/ScriptableObject/EnemyPollutionTrailSettingsSO.cs
- 분류: 일반 소스; 30행
- 타입: EnemyPollutionTrailSettingsSO : ScriptableObject
- 메서드: 
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 0개: 

## Assets/Scripts/Gameplay/Entities/Creature/ScriptableObject/EnemyScreenSpaceWidgetLayout.cs
- 분류: 일반 소스; 35행
- 타입: EnemyScreenSpaceWidgetLayout
- 메서드: 
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 5개: Assets/Scripts/Gameplay/Entities/Common/Core/EnemyCommonSettings.cs; Assets/Scripts/Gameplay/Entities/Creature/ScriptableObject/EnemyStatConfigSO.cs; Assets/Scripts/Gameplay/Entities/Shooter/Enemy/Definitions/EnemyShooterConfigSO.cs; Assets/Scripts/Presentation/UI/Gameplay/EnemyBarUI/EnemyScreenSpaceHPUIAnchor.cs; Assets/Scripts/Presentation/UI/Gameplay/EnemyBarUI/EnemyScreenSpaceHPUIManager.cs

## Assets/Scripts/Gameplay/Entities/Creature/ScriptableObject/EnemyStatConfigSO.cs
- 분류: 일반 소스; 23행
- 타입: EnemyStatConfigSO : EntityStatConfigSO
- 메서드: 
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 7개: Assets/Scripts/Gameplay/Entities/Common/Core/Damageable.cs; Assets/Scripts/Gameplay/Entities/Creature/EnemyVirusTrail.cs; Assets/Scripts/Gameplay/Entities/Creature/NormalEnemy/CommonNormalEnemyStateMachine/Action/NormalEnemyAttackActionSO.cs; Assets/Scripts/Gameplay/Entities/Creature/ScriptableObject/CreatureEnemyStatConfigSO.cs; Assets/Scripts/Gameplay/Progression/Level/EnemyKillRewardSource.cs; Assets/Scripts/Gameplay/World/Sectors/Named_Boss/NamedSectorController.cs; Assets/Scripts/Presentation/UI/Gameplay/EnemyBarUI/EnemyScreenSpaceHPUIAnchor.cs

## Assets/Scripts/Gameplay/Entities/Creature/ScriptableObject/PoisonPuddleDamageConfigSO.cs
- 분류: 일반 소스; 24행
- 타입: PoisonPuddleDamageConfigSO : NamedAttackConfigSO
- 메서드: 
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 8개: Assets/Scripts/Gameplay/Entities/Creature/CommonEnemyStateMachine/Action/EnemyFireArcBombActionSO.cs; Assets/Scripts/Gameplay/Entities/Creature/EnemyArcBombProjectile.cs; Assets/Scripts/Gameplay/Entities/Creature/NamedEnemy/Mutarus/Action/MutarusQTEPatternActionSO.cs; Assets/Scripts/Gameplay/Entities/Creature/NamedEnemy/Mutarus/ScriptableObject/MutarusPatternConfigSO.cs; Assets/Scripts/Gameplay/Entities/Creature/ScriptableObject/EnemyArcBombAttackConfigSO.cs; Assets/Scripts/Gameplay/Entities/Shooter/Common/Core/ShooterInfection.cs; Assets/Scripts/Gameplay/MaskRenderManager.cs; Assets/Scripts/Gameplay/World/Sectors/SectorPaint.cs

## Assets/Scripts/Gameplay/Entities/Creature/StateMachineCoordinator.cs
- 분류: 일반 소스; 113행
- 타입: StateMachineCoordinator : MonoBehaviour, ParentChildRule
- 메서드: Awake, Start, Update, RefreshChildStateMachine, GetCurrentParentState, ResolveChildEnabled, ApplyChildStateMachineState
- 직렬화 참조 9개 / 지정 씬 참조 그래프 내 2개: Assets/Prefabs/GameScene/Entity_WIP/Virus/NormalVirus/Chaser/Chaser.prefab; Assets/Prefabs/GameScene/Entity_WIP/Virus/NormalVirus/Chaser/Chaser_Upgrade.prefab; Assets/Prefabs/GameScene/Entity_WIP/Virus/NormalVirus/Roller/Roller.prefab; Assets/Prefabs/GameScene/Entity_WIP/Virus/NormalVirus/Spitter/Spitter_ThreeShot_Virus.prefab; Assets/Prefabs/GameScene/Entity_WIP/Virus/NormalVirus/Spitter/Spitter_Virus.prefab; Assets/Prefabs/GameScene/Entity_WIP/Virus/NormalVirus/Worm/Worm.prefab; Assets/Prefabs/GameScene/Entity_WIP/Virus/NormalVirus/Worm/Worm_Upgrade.prefab; Assets/Prefabs/GameScene/Shooter/EnemyShooter/EnemyShooter_Base.prefab
- 타입 이름을 참조하는 소스 0개: 

## Assets/Scripts/Gameplay/Entities/Creature/StateMachineGroupCoordinator.cs
- 분류: 일반 소스; 153행
- 타입: StateMachineGroupCoordinator : MonoBehaviour, ChildRule
- 메서드: Awake, Start, Update, RefreshChildren, GetCurrentParentState, ResolveChildEnabled, SetAllManagedChildren, ApplyChildStateMachineState
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 0개: Assets/Prefabs/GameScene/Entity_WIP/Virus/NamedVirus/Mutarus/Mutarus_NamedVirus.prefab
- 타입 이름을 참조하는 소스 0개: 

## Assets/Scripts/Gameplay/Entities/Shooter/Common/Core/EntityFootstepSoundEmitter.cs
- 분류: 일반 소스; 196행
- 타입: EntityFootstepSoundEmitter : MonoBehaviour
- 메서드: Reset, Awake, Configure, Update, LateUpdate, ResolveRefs, ResolveRadius, ResolveInterval, ResolveMoveSpeed, ResolveMovementPosition
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 2개: Assets/Scripts/Gameplay/Entities/Shooter/Enemy/Common/EnemyShooterShoot.cs; Assets/Scripts/Gameplay/Entities/Shooter/Player/PlayerShooterShoot.cs

## Assets/Scripts/Gameplay/Entities/Shooter/Common/Core/EntityRange.cs
- 분류: 일반 소스; 82행
- 타입: EntityRange : MonoBehaviour
- 메서드: Reset, Awake, ResolveEntityRangeRefs, HasValidWeapon, IsWithinRange, ClampToRange
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 1개: Assets/Prefabs/GameScene/Shooter/EnemyShooter/EnemyShooter_Base.prefab
- 타입 이름을 참조하는 소스 6개: Assets/Scripts/Gameplay/Entities/Common/Config/EntityStatConfigSO.cs; Assets/Scripts/Gameplay/Entities/Shooter/Common/Vision/EntityVisionController.cs; Assets/Scripts/Gameplay/Entities/Shooter/Player/PlayerAimController.cs; Assets/Scripts/Gameplay/Entities/Shooter/Player/PlayerShooterShoot.cs; Assets/Scripts/Gameplay/Entities/Shooter/Player/VSplatterRange.cs; Assets/Scripts/WIP/Legacy/Gameplay/Player/ExistingWIP/PlayerWorldRangeRing.cs

## Assets/Scripts/Gameplay/Entities/Shooter/Common/Core/EntityShootCore.cs
- 분류: 일반 소스; 489행
- 타입: EntityShootStats, EntityShootCore : MonoBehaviour
- 메서드: TryFirePrimary, ResolvePelletAngleDegrees, TryFireSpecial, ValidatePrimaryBullet, IsActionBlocked, ValidateSpecialBullet, TryResolveShotFromAimPoints, TryResolveSpecialShotFromAimPoints, ResolveMaskRenderManager, AttachProjectileTrail, ResolveHitMarkAmounts, PublishFired
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 5개: Assets/Scripts/Gameplay/Entities/Shooter/Common/Core/EntityShooterCharacterAudio.cs; Assets/Scripts/Gameplay/Entities/Shooter/Enemy/Common/EnemyShooterShoot.cs; Assets/Scripts/Gameplay/Entities/Shooter/Enemy/StateMachine/EnemyShooterCombatController.cs; Assets/Scripts/Gameplay/Entities/Shooter/Player/PlayerShooterShoot.cs; Assets/Scripts/Presentation/Camera/CameraEntityOcclusionFadeController.cs

## Assets/Scripts/Gameplay/Entities/Shooter/Common/Core/EntityShooterCharacterAudio.cs
- 분류: 일반 소스; 183행
- 타입: EntityShooterCharacterAudio : CharacterAudio
- 메서드: Reset, Awake, OnEnable, OnDisable, ResolveRefs, SubscribeAdditionalEvents, UnsubscribeAdditionalEvents, PlayShoot, PlayEmptyShoot, PlayPlayerReload, PlayEnemyReload, PlaySpecial, PlayMeleeSwing, PlayThrowableThrow, PlayThrowableExplode, PlayDie
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 6개: Assets/Scripts/Gameplay/Entities/Shooter/Common/Core/ShooterMeleeAttack.cs; Assets/Scripts/Gameplay/Entities/Shooter/Common/Core/ShooterThrowableAttack.cs; Assets/Scripts/Gameplay/Entities/Shooter/Enemy/Common/EnemyShooterCharacterAudio.cs; Assets/Scripts/Gameplay/Entities/Shooter/Player/PlayerShooterCharacterAudio.cs; Assets/Scripts/Gameplay/Entities/Shooter/Player/PlayerShooterShoot.cs; Assets/Scripts/Gameplay/Items/Weapons/Runtime/ThrowableProjectile.cs

## Assets/Scripts/Gameplay/Entities/Shooter/Common/Core/EntityWeaponHolder.cs
- 분류: 일반 소스; 13행
- 타입: EntityWeaponHolder : ShooterEquipmentController
- 메서드: 
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 1개: Assets/Scripts/WIP/Legacy/Gameplay/Player/VSplatterWeaponHolder.cs

## Assets/Scripts/Gameplay/Entities/Shooter/Common/Core/IMeleeAttackProfile.cs
- 분류: 일반 소스; 27행
- 타입: IMeleeAttackProfile
- 메서드: 
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 4개: Assets/Scripts/Gameplay/Entities/Shooter/Common/Core/ShooterMeleeAttack.cs; Assets/Scripts/Gameplay/Entities/Shooter/Common/Core/UnarmedAttackProfileSO.cs; Assets/Scripts/Gameplay/Entities/Shooter/Player/PlayerEmergencyMeleeProfile.cs; Assets/Scripts/Gameplay/Items/Weapons/Definitions/MeleeWeaponSO.cs

## Assets/Scripts/Gameplay/Entities/Shooter/Common/Core/InventoryRuntimeBase.cs
- 분류: 일반 소스; 91행
- 타입: InventoryRuntimeBase : MonoBehaviour
- 메서드: NormalizeInventoryStorage, TryFindEmptyInventorySlot, CountUsedInventorySlots, ResolveInventoryMaxStack, CanStackInventoryItem
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 2개: Assets/Scripts/Gameplay/Entities/Shooter/Enemy/Common/EnemyLootInventoryRuntime.cs; Assets/Scripts/Gameplay/Entities/Shooter/Player/Inventory/PlayerInventoryRuntime.cs

## Assets/Scripts/Gameplay/Entities/Shooter/Common/Core/ProjectileSoundStimulusEmitter.cs
- 분류: 일반 소스; 105행
- 타입: ProjectileSoundStimulusEmitter
- 메서드: Configure, EmitTravelSegment, EmitImpact
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 2개: Assets/Scripts/Gameplay/Items/Weapons/Runtime/BasicBullet.cs; Assets/Scripts/Gameplay/Items/Weapons/Runtime/ThrowableProjectile.cs

## Assets/Scripts/Gameplay/Entities/Shooter/Common/Core/ShooterAimTargetAnchor.cs
- 분류: 일반 소스; 17행
- 타입: ShooterAimTargetAnchor : MonoBehaviour
- 메서드: 
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 1개: Assets/Prefabs/GameScene/Shooter/Player/Player.prefab
- 타입 이름을 참조하는 소스 1개: Assets/Scripts/Gameplay/Entities/Shooter/Enemy/StateMachine/EnemyShooterCombatController.cs

## Assets/Scripts/Gameplay/Entities/Shooter/Common/Core/ShooterDashController.cs
- 분류: 일반 소스; 411행
- 타입: ShooterDashController : MonoBehaviour
- 메서드: Reset, Awake, OnEnable, OnDisable, Update, TryStartDash, CancelDash, BuildDashMovementVector, CanStartDash, UseForwardWhenNoMovementInput, ResolveDashDuration, ResolveDashCooldown, ResolveDashSpeed, ResolveRefs, ResolveVisualLeanTarget, TickDashRuntime, FinishDash, RaiseDashEvent, UpdateCooldownRuntime, PublishCooldownSnapshot, PublishCooldownSnapshotIfNeeded, CacheLeanBaseRotation, UpdateVisualLean, ResetLeanTarget
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 12개: Assets/Scripts/Gameplay/Entities/Common/StateMachine/Actions/CancelDashActionSO.cs; Assets/Scripts/Gameplay/Entities/Common/StateMachine/Actions/DashMoveActionSO.cs; Assets/Scripts/Gameplay/Entities/Common/StateMachine/Conditions/IsUsingDashConditionSO.cs; Assets/Scripts/Gameplay/Entities/Shooter/Common/Core/EntityFootstepSoundEmitter.cs; Assets/Scripts/Gameplay/Entities/Shooter/Common/Core/ShooterThrowableAttack.cs; Assets/Scripts/Gameplay/Entities/Shooter/Common/StateMachine/Action/ShooterLocomotionActionBase.cs; Assets/Scripts/Gameplay/Entities/Shooter/Enemy/Common/EnemyShooterCharacterAudio.cs; Assets/Scripts/Gameplay/Entities/Shooter/Enemy/Common/EnemyShooterDashController.cs

## Assets/Scripts/Gameplay/Entities/Shooter/Common/Core/ShooterDeathCorpseController.cs
- 분류: 일반 소스; 239행
- 타입: ShooterDeathCorpseController : MonoBehaviour
- 메서드: Reset, Awake, OnEnable, OnDisable, ConvertToCorpse, BeforeConvertToCorpse, AfterConvertToCorpse, ResolveRefs, SetObjectsActive, CanToggleRoot, SetCollidersEnabled, LogCorpseState, DescribeObject, DescribeColliders
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 2개: Assets/Scripts/Gameplay/Entities/Shooter/Enemy/Common/EnemyShooterDeathCorpseController.cs; Assets/Scripts/Gameplay/Entities/Shooter/Player/PlayerShooterDeathCorpseController.cs

## Assets/Scripts/Gameplay/Entities/Shooter/Common/Core/ShooterEquipmentController.cs
- 분류: 일반 소스; 939행
- 타입: ShooterEquipmentController : MonoBehaviour
- 메서드: Reset, Awake, OnEnable, Start, OnDisable, OnDestroy, SetProjectilesRoot, SetVisible, SetHeldItemVisual, SetWeaponViewSuppressed, UpdateVisualAim, Equip, EquipEnemySpawnWeapon, SetActiveWeaponSlot, GetWeapon, GetWeaponAmount, TryConsumeWeaponStack, ClearWeapon, EquipArmor, ClearArmor, SyncArmorVisual, SyncGasMaskVisual, SyncWeaponView, PlayMuzzleParticle, PlayMeleeSwing, SpawnMeleeParticle, FollowMeleeParticlePosition, ResolveRefs, HandleWeaponChanged, RebuildWeaponView, ResolveWeaponVisualRoot, FindVisualTransformByName, RebuildArmorVisual, ResolveArmorVisualRoot, HandleArmorChanged, HandleGasMaskChanged, RebuildGasMaskVisual, ResolveGasMaskVisualRoot, SyncVisualOccupancy, GetVisualOccupancySourceId, ResolveParticleLifetime, ClearCurrentWeaponView, RefreshWeaponViewVisibility, ClearArmorVisual, ClearGasMaskVisual, RefreshGasMaskVisualVisibility, RegisterRuntimeVisualWithVision, RefreshVisionRendererRegistration
- 직렬화 참조 2개 / 지정 씬 참조 그래프 내 2개: Assets/Prefabs/GameScene/Shooter/EnemyShooter/EnemyShooter_Base.prefab; Assets/Prefabs/GameScene/Shooter/Player/Player.prefab
- 타입 이름을 참조하는 소스 22개: Assets/Scripts/Authoring/Editor/DebugMonitorWindow.cs; Assets/Scripts/Gameplay/Entities/Common/Equipment/EntityEquipmentRuntime.cs; Assets/Scripts/Gameplay/Entities/Common/StateMachine/Actions/CreatureRotateActionSO.cs; Assets/Scripts/Gameplay/Entities/Shooter/Common/Core/EntityRange.cs; Assets/Scripts/Gameplay/Entities/Shooter/Common/Core/EntityShooterCharacterAudio.cs; Assets/Scripts/Gameplay/Entities/Shooter/Common/Core/EntityWeaponHolder.cs; Assets/Scripts/Gameplay/Entities/Shooter/Common/Core/ShooterMeleeAttack.cs; Assets/Scripts/Gameplay/Entities/Shooter/Common/Core/ShooterThrowableAttack.cs

## Assets/Scripts/Gameplay/Entities/Shooter/Common/Core/ShooterInfection.cs
- 분류: 일반 소스; 610행
- 타입: ShooterInfection : MonoBehaviour, ExposureKind
- 메서드: Reset, Awake, OnEnable, OnDisable, Update, HandleMaskRenderManagerReady, AddInfection, ReduceInfection, ClearInfection, RestoreCurrentInfection, RecoverOnSectorCaptured, RecoverOnNamedKilled, RecoverOnBossKilled, PublishCurrentSnapshot, ResolveRefs, OnBecameDead, HandleDamageableHealthChanged, ForceInfectionToMaximumHealth, HandleHealthDamageReceived, StartRecoveryWait, TickNaturalDecay, TickZoneExposure, ResolveCurrentExposure, ApplyExposureTick, ApplyPoisonPuddleTick, ResetZoneTick, ApplyPaintContact, ClampInfectionToCurrentHealth, CheckDeathState, MarkDead, HandleStatusEffectsChanged, LogInfectionLifecycle, GetHierarchyPath
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 6개: Assets/Scripts/Authoring/Editor/DebugMonitorWindow.cs; Assets/Scripts/Gameplay/Entities/Shooter/Enemy/Common/EnemyShooterInfection.cs; Assets/Scripts/Gameplay/Entities/Shooter/Player/PlayerShooterInfection.cs; Assets/Scripts/Gameplay/Items/Weapons/Runtime/BasicBullet.cs; Assets/Scripts/Gameplay/Items/Weapons/Runtime/ThrowableProjectile.cs; Assets/Scripts/Presentation/UI/Gameplay/EnemyBarUI/HealthBar/EnemyHealthBarWidget.cs

## Assets/Scripts/Gameplay/Entities/Shooter/Common/Core/ShooterLoadoutSO.cs
- 분류: 일반 소스; 592행
- 타입: ShooterLoadoutAmmoStack, ShooterLoadoutItemStack, ShooterLoadoutWeaponStack, ShooterLoadoutSnapshot, EnemyShooterLoadout, ShooterLoadoutAmmoCandidate, ShooterLoadoutAmmoPool, ShooterLoadoutWeaponEntry, ShooterLoadoutArmorEntry, ShooterLoadoutItemEntry, ShooterLoadoutSO : ScriptableObject
- 메서드: ToEnemyShooterLoadout, ResolvePrimaryWeapon, GetWeapon, GetBulletForWeaponSlot, IsCompatibleWith, Roll, MatchesWeaponSet, TryRoll, ResolveDurability, PassesProbability, PickWeaponEntry, RollAmmoStacks, RollInventoryStacks
- 직렬화 참조 21개 / 지정 씬 참조 그래프 내 18개: Assets/ScriptableObject/GameScene/Character/Shooter/EnemyShooter/Boss/PunisherLoadout.asset; Assets/ScriptableObject/GameScene/Character/Shooter/EnemyShooter/Boss/Stage1RevolverBossLoadout.asset; Assets/ScriptableObject/GameScene/Character/Shooter/EnemyShooter/Boss/Stage2SniperBossLoadout.asset; Assets/ScriptableObject/GameScene/Character/Shooter/EnemyShooter/Boss/Stage3RocketBossLoadout.asset; Assets/ScriptableObject/GameScene/Character/Shooter/EnemyShooter/Boss/Stage4LMGBossLoadout.asset; Assets/ScriptableObject/GameScene/Character/Shooter/EnemyShooter/Boss/Stage5BossTemplateLoadout.asset; Assets/ScriptableObject/GameScene/Character/Shooter/EnemyShooter/BossGuard/Stage1BossGuardLoadout.asset; Assets/ScriptableObject/GameScene/Character/Shooter/EnemyShooter/BossGuard/Stage2BossGuardLoadout.asset
- 타입 이름을 참조하는 소스 11개: Assets/Scripts/Authoring/Editor/ShooterLoadoutSOEditor.cs; Assets/Scripts/Gameplay/Entities/Common/Equipment/EntityEquipmentRuntime.cs; Assets/Scripts/Gameplay/Entities/Shooter/Common/Core/ShooterLoadoutTableSO.cs; Assets/Scripts/Gameplay/Entities/Shooter/Common/Core/ShooterStatConfigSO.cs; Assets/Scripts/Gameplay/Entities/Shooter/Enemy/Common/EnemyLootInventoryRuntime.cs; Assets/Scripts/Gameplay/Entities/Shooter/Enemy/Common/EnemyShooter.cs; Assets/Scripts/Gameplay/Entities/Shooter/Enemy/Definitions/EnemyShooterConfigSO.cs; Assets/Scripts/Gameplay/Entities/Shooter/Enemy/Raider/ScriptableObjects/RaiderShooterConfigSO.cs

## Assets/Scripts/Gameplay/Entities/Shooter/Common/Core/ShooterLoadoutTableSO.cs
- 분류: 일반 소스; 78행
- 타입: ShooterLoadoutTableEntry, ShooterLoadoutTableSO : ScriptableObject
- 메서드: TryRoll, PickLoadout
- 직렬화 참조 21개 / 지정 씬 참조 그래프 내 18개: Assets/ScriptableObject/GameScene/Character/Shooter/EnemyShooter/Boss/PunisherLoadoutTable.asset; Assets/ScriptableObject/GameScene/Character/Shooter/EnemyShooter/Boss/Stage1RevolverBossLoadoutTable.asset; Assets/ScriptableObject/GameScene/Character/Shooter/EnemyShooter/Boss/Stage2SniperBossLoadoutTable.asset; Assets/ScriptableObject/GameScene/Character/Shooter/EnemyShooter/Boss/Stage3RocketBossLoadoutTable.asset; Assets/ScriptableObject/GameScene/Character/Shooter/EnemyShooter/Boss/Stage4LMGBossLoadoutTable.asset; Assets/ScriptableObject/GameScene/Character/Shooter/EnemyShooter/Boss/Stage5BossTemplateLoadoutTable.asset; Assets/ScriptableObject/GameScene/Character/Shooter/EnemyShooter/BossGuard/Stage1BossGuardLoadoutTable.asset; Assets/ScriptableObject/GameScene/Character/Shooter/EnemyShooter/BossGuard/Stage2BossGuardLoadoutTable.asset
- 타입 이름을 참조하는 소스 1개: Assets/Scripts/Gameplay/Entities/Shooter/Common/Core/ShooterStatConfigSO.cs

## Assets/Scripts/Gameplay/Entities/Shooter/Common/Core/ShooterMeleeAttack.cs
- 분류: 일반 소스; 879행
- 타입: ShooterMeleeAttack : MonoBehaviour, MeleeShapeSnapshot
- 메서드: Reset, Awake, Update, OnValidate, OnDisable, ResolveRefs, RefreshRuntimeShapeFromEquippedWeapon, RefreshRuntimeShapeIfWeaponChanged, EnsureRuntimeShapeSource, SetRuntimeShapeSource, CaptureRuntimeShape, ApplyRuntimeShapeToHitZone, TryAttackCurrentWeapon, TryAttack, TryAttackProfile, RunAttackWindow, SampleHitZone, ResolveDamageable, ResolveColliderPath, ResolveLayerName, Reject, IsOwnDamageable, ResolveOwnerEntityRoot, ResolveOwnerEntityGameObject, ResolveOwnerEntityName, ResolveParticleScale, ResolveParticlePose, ResolveParticleFollowTarget, ResolveParticleLocalPosition, ResolveParticleLocalEuler, ResolveParticleContainerName, OnDrawGizmosSelected
- 직렬화 참조 2개 / 지정 씬 참조 그래프 내 2개: Assets/Prefabs/GameScene/Shooter/EnemyShooter/EnemyShooter_Base.prefab; Assets/Prefabs/GameScene/Shooter/Player/Player.prefab
- 타입 이름을 참조하는 소스 5개: Assets/Scripts/Gameplay/Entities/Shooter/Common/Core/ShooterMeleeHitZone.cs; Assets/Scripts/Gameplay/Entities/Shooter/Common/StateMachine/Action/ShooterAnimatorParameterActionSO.cs; Assets/Scripts/Gameplay/Entities/Shooter/Enemy/StateMachine/EnemyShooterCombatController.cs; Assets/Scripts/Gameplay/Entities/Shooter/Player/PlayerClickController.cs; Assets/Scripts/Gameplay/Entities/Shooter/Player/PlayerShooterShoot.cs

## Assets/Scripts/Gameplay/Entities/Shooter/Common/Core/ShooterMeleeHitZone.cs
- 분류: 일반 소스; 326행
- 타입: ShooterMeleeHitZone : MonoBehaviour
- 메서드: Awake, OnValidate, OnDisable, ConfigureShape, BeginSwing, SetHitWindowActive, Overlap, ResolveRaisedOrigin, DrawGizmos, ConfigureCollider, LockWorldPose, DrawAttackArc, NormalizeDirection, Abs
- 직렬화 참조 2개 / 지정 씬 참조 그래프 내 2개: Assets/Prefabs/GameScene/Shooter/EnemyShooter/EnemyShooter_Base.prefab; Assets/Prefabs/GameScene/Shooter/Player/Player.prefab
- 타입 이름을 참조하는 소스 1개: Assets/Scripts/Gameplay/Entities/Shooter/Common/Core/ShooterMeleeAttack.cs

## Assets/Scripts/Gameplay/Entities/Shooter/Common/Core/ShooterStatConfigSO.cs
- 분류: 일반 소스; 166행
- 타입: ShooterStatSettings, ShooterStatConfigSO : EntityStatConfigSO
- 메서드: TryRollLoadout
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 3개: Assets/Scripts/Gameplay/Entities/Shooter/Common/Core/ShooterStatsRuntime.cs; Assets/Scripts/Gameplay/Entities/Shooter/Enemy/Definitions/EnemyShooterConfigSO.cs; Assets/Scripts/Gameplay/Entities/Shooter/Player/Definitions/PlayerStatsConfigSO.cs

## Assets/Scripts/Gameplay/Entities/Shooter/Common/Core/ShooterStatsRuntime.cs
- 분류: 일반 소스; 318행
- 타입: ShooterStatsRuntime : MonoBehaviour, before, needed, StatAccumulator
- 메서드: ResolveAttackDamage, ApplyWeaponConditionToDamage, ApplyWeaponArchetypeToDamage, ApplyWeaponArchetypeToShotsPerSecond, ApplyWeaponConditionToSpread, ResolveWeaponConditionPerformanceMultiplier, ResolvePaintMarkDamage, ResolveInfectionDamage, ResolveShotsPerSecond, ResolveMagazineSize, ResolvePenetrationClass, ResolveArmorHealthDurabilityLossMultiplier, ResolveArmorInfectionDurabilityLossMultiplier, ResolveStatWithWeaponAndBulletModifiers, ApplyStatusMoveSpeed, ApplyMatchingModifiers, Apply, Resolve
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 397개: Assets/Editor/ItemCategoryMoveUtility.cs; Assets/Editor/ItemMakerWindow.cs; Assets/Editor/LootProgressionEditorWindow.cs; Assets/Editor/QuestDesignTreeWindow.cs; Assets/Editor/ShooterCustomizeStationWindow.cs; Assets/Editor/ShopProgressionTreeWindow.cs; Assets/Scripts/Authoring/Editor/AudioCueLibraryWindow.cs; Assets/Scripts/Authoring/Editor/BoardChaseAuthoringHubWindow.cs

## Assets/Scripts/Gameplay/Entities/Shooter/Common/Core/ShooterStuckRecovery.cs
- 분류: 일반 소스; 430행
- 타입: ShooterStuckRecovery : MonoBehaviour
- 메서드: EnsureOn, Awake, LateUpdate, TryResolvePenetration, ResolveRefs, ResolveRadius, TryResolveSafePosition, TryFindSupportingGround, PlaceEntity, IsBlockingCollider, ClampInsideSector
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 3개: Assets/Scripts/Authoring/Editor/DebugMonitorWindow.cs; Assets/Scripts/Gameplay/Entities/Shooter/Enemy/Common/EnemyShooter.cs; Assets/Scripts/Gameplay/Entities/Shooter/Player/PlayerCharacter.cs

## Assets/Scripts/Gameplay/Entities/Shooter/Common/Core/ShooterThrowableAttack.cs
- 분류: 일반 소스; 667행
- 타입: ShooterThrowableAttack : MonoBehaviour
- 메서드: Reset, Awake, OnEnable, OnDisable, TryThrowUsable, TryThrowEnemyUsableWithSafeArc, TryThrowUsableInternal, TryGetPrediction, TryResolveThrowSolution, TryResolveSafeEnemyThrowSolution, TryResolveHighArcThrowSolution, TryFindEnemyArcBlocker, IsIgnoredEnemyArcCollider, DescribeArcBlocker, ResolveNaturalFlightSeconds, TryConsumeThrowable, ResolveThrowOrigin, ResolveRefs, HandleMaskRenderManagerReady
- 직렬화 참조 2개 / 지정 씬 참조 그래프 내 2개: Assets/Prefabs/GameScene/Shooter/EnemyShooter/EnemyShooter_Base.prefab; Assets/Prefabs/GameScene/Shooter/Player/Player.prefab
- 타입 이름을 참조하는 소스 2개: Assets/Scripts/Gameplay/Entities/Shooter/Enemy/StateMachine/EnemyShooterCombatController.cs; Assets/Scripts/Gameplay/Entities/Shooter/Player/PlayerClickController.cs

## Assets/Scripts/Gameplay/Entities/Shooter/Common/Core/SoundStimulus.cs
- 분류: 일반 소스; 93행
- 타입: SoundStimulusType, SoundStimulus
- 메서드: 
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 9개: Assets/Scripts/Core/Events/ScriptableObject/SoundStimulusEventChannelSO.cs; Assets/Scripts/Gameplay/Entities/Shooter/Common/Core/EntityFootstepSoundEmitter.cs; Assets/Scripts/Gameplay/Entities/Shooter/Common/Core/ProjectileSoundStimulusEmitter.cs; Assets/Scripts/Gameplay/Entities/Shooter/Enemy/Common/EnemyShooterShoot.cs; Assets/Scripts/Gameplay/Entities/Shooter/Enemy/StateMachine/EnemyShooterCombatController.cs; Assets/Scripts/Gameplay/Entities/Shooter/Player/PlayerShooterShoot.cs; Assets/Scripts/Gameplay/Entities/Shooter/Player/PlayerWorldSoundIndicator.cs; Assets/Scripts/Gameplay/Items/Weapons/Runtime/ThrowableProjectile.cs

## Assets/Scripts/Gameplay/Entities/Shooter/Common/Core/StatModifierTypes.cs
- 분류: 일반 소스; 87행
- 타입: PlayerStatId, PlayerFeatureFlagId, StatModifierType, PlayerStatModifier, PlayerFeatureFlagModifier
- 메서드: 
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 15개: Assets/Editor/QuestDesignTreeWindow.cs; Assets/Scripts/Core/Persistence/Save.cs; Assets/Scripts/Gameplay/Entities/Common/Equipment/ArmorItemSO.cs; Assets/Scripts/Gameplay/Entities/Shooter/Common/Core/ShooterStatsRuntime.cs; Assets/Scripts/Gameplay/Entities/Shooter/Enemy/Common/EnemyShooterStatsRuntime.cs; Assets/Scripts/Gameplay/Entities/Shooter/Player/PlayerStatsRuntime.cs; Assets/Scripts/Gameplay/Items/Definitions/PassiveItemSO.cs; Assets/Scripts/Gameplay/Items/Weapons/Definitions/BulletSO.cs

## Assets/Scripts/Gameplay/Entities/Shooter/Common/Core/UnarmedAttackProfileSO.cs
- 분류: 일반 소스; 62행
- 타입: UnarmedAttackProfileSO : ScriptableObject, IMeleeAttackProfile
- 메서드: ResolveDamageTargetMask
- 직렬화 참조 2개 / 지정 씬 참조 그래프 내 2개: Assets/ScriptableObject/GameScene/Character/Shooter/Player/Config/PlayerEmergencyMelee.asset; Assets/ScriptableObject/GameScene/Character/Shooter/UnarmedAttackProfile.asset
- 타입 이름을 참조하는 소스 2개: Assets/Scripts/Gameplay/Entities/Shooter/Common/Core/ShooterMeleeAttack.cs; Assets/Tests/EditMode/Editor/PlayerEmergencyMeleeTests.cs

## Assets/Scripts/Gameplay/Entities/Shooter/Common/Effects/EntityEffectController.cs
- 분류: 일반 소스; 53행
- 타입: EntityEffectController : MonoBehaviour
- 메서드: PlayIfAssigned, StopIfAssigned, StopAndClearIfAssigned, CanUseParticles
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 2개: Assets/Scripts/Gameplay/Entities/Creature/Effect/EnemyEffectController.cs; Assets/Scripts/Gameplay/Entities/Shooter/Common/Effects/ShooterEffectController.cs

## Assets/Scripts/Gameplay/Entities/Shooter/Common/Effects/ShooterEffectController.cs
- 분류: 일반 소스; 241행
- 타입: ShooterEffectController : EntityEffectController
- 메서드: Reset, Awake, OnEnable, OnDisable, PlayDashParticles, StopDashParticles, PlayFootTrailParticles, StopFootTrailParticles, SetFootTrailActive, PlayGetHitParticles, PlayDeathParticles, ResolveRefs, StopAllShooterParticles, HandleHitReceived, HandleStatusEffectsChanged, SyncStatusVfx, ClearStatusVfx
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 8개: Assets/Scripts/Gameplay/Entities/Creature/CommonEnemyStateMachine/Action/ControlEnemyMoveParticlesActionSO.cs; Assets/Scripts/Gameplay/Entities/Creature/CommonEnemyStateMachine/Action/PlayEnemyCreatureDeathParticlesActionSO.cs; Assets/Scripts/Gameplay/Entities/Creature/CommonEnemyStateMachine/Action/PlayEnemyCreatureGetHitParticlesActionSO.cs; Assets/Scripts/Gameplay/Entities/Shooter/Common/Core/ShooterDashController.cs; Assets/Scripts/Gameplay/Entities/Shooter/Common/StateMachine/Action/ShooterLocomotionActionBase.cs; Assets/Scripts/Gameplay/Entities/Shooter/Enemy/Effects/EnemyShooterEffectController.cs; Assets/Scripts/Gameplay/Entities/Shooter/Enemy/StateMachine/EnemyShooterCombatController.cs; Assets/Scripts/Gameplay/Entities/Shooter/Player/Effects/PlayerShooterEffectController.cs

## Assets/Scripts/Gameplay/Entities/Shooter/Common/StateMachine/Action/SetEntityWeaponVisibleActionSO.cs
- 분류: 일반 소스; 69행
- 타입: SetEntityWeaponVisibleActionSO : StateActionSO<SetEntityWeaponVisibleAction>, SetEntityWeaponVisibleAction : StateAction
- 메서드: Awake, OnStateEnter, OnStateExit, OnUpdate, Apply
- 직렬화 참조 2개 / 지정 씬 참조 그래프 내 2개: Assets/ScriptableObject/StateMachine/Shooter/EnemyShooter/Action/SetEntityWeaponUnVisibleAction.asset; Assets/ScriptableObject/StateMachine/Shooter/EnemyShooter/Action/SetEntityWeaponVisibleAction.asset
- 타입 이름을 참조하는 소스 0개: 

## Assets/Scripts/Gameplay/Entities/Shooter/Common/StateMachine/Action/ShooterAnimatorParameterActionSO.cs
- 분류: 일반 소스; 369행
- 타입: ShooterAnimatorParameterActionSO : StateActionSO<ShooterAnimatorParameterAction>, ShooterAnimatorParameterAction : StateAction
- 메서드: Awake, OnUpdate, OnStateExit, ResolveWorldMoveVector, ResolveAnimatorSpeed, ResolveLocalDirection, Apply, ResolveAnimator, ResolvePreferredChildAnimator, CacheParameters, ApplyPendingMeleeAttackTrigger, CacheFloatParameter, CacheTriggerParameter
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 1개: Assets/ScriptableObject/StateMachine/Shooter/Action/ShooterAnimatorParameter.asset
- 타입 이름을 참조하는 소스 1개: Assets/Scripts/Gameplay/Entities/Shooter/Common/Core/ShooterMeleeAttack.cs

## Assets/Scripts/Gameplay/Entities/Shooter/Common/StateMachine/Action/ShooterLocomotionActionBase.cs
- 분류: 일반 소스; 93행
- 타입: ShooterLocomotionActionBase : StateAction
- 메서드: Awake, OnAwakeLocomotion, IsUpdateBlocked, ResolveMoveSpeed, TryBuildDashVector, SetFootTrailByPlanarMotion, StopFootTrail
- 직렬화 참조 2개 / 지정 씬 참조 그래프 내 0개: Assets/ScriptableObject/StateMachine/Shooter/Player/Action/MovementVector/ShooterIdleLocomotion.asset; Assets/ScriptableObject/StateMachine/Shooter/Player/Action/MovementVector/ShooterLocomotion.asset
- 타입 이름을 참조하는 소스 1개: Assets/Scripts/Gameplay/Entities/Shooter/Player/Action/PlayerShooterLocomotionActionSO.cs

## Assets/Scripts/Gameplay/Entities/Shooter/Common/StateMachine/Action/ShooterRotateActionSO.cs
- 분류: 일반 소스; 477행
- 타입: ShooterRotateSource, ShooterRotateActionSO : StateActionSO<ShooterRotateAction>, ShooterRotateAction : StateAction
- 메서드: Awake, OnUpdate, TryResolveLookPoint, ResolveSource, TryResolveEnemyGoalPoint, TryResolvePlayerAimPoint, TryResolveEnemyCombatPoint, TryResolveNavMeshMovementPoint, TryResolveMovementInputPoint, ResolveBodyPivotPosition, TryNormalizePlanar, ApplyRotation, IsMouseSelfLocked, IsMouseInsideSelfScreenBounds, TryGetSelfBounds
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 1개: Assets/ScriptableObject/StateMachine/Shooter/Action/ShooterRotate.asset
- 타입 이름을 참조하는 소스 0개: 

## Assets/Scripts/Gameplay/Entities/Shooter/Common/Vision/EntityVisionController.cs
- 분류: 일반 소스; 490행
- 타입: EntityVisionController : MonoBehaviour
- 메서드: Reset, Awake, CanSeeTarget, CanSeeDamageable, CanSeeBounds, CanRevealVisualPoint, CanRevealVisualBounds, CanSeeWorldPoint, FlatDistance, HasLineOfSightTo, GetVisibleDistance, GetVisibleDistanceFrom, IsInsideVisionRange, ResolveRefs, ResolveAim01, ResolveForwardDirection, ResolveSettings, ResolveVisionRange, ResolveCloseVisionRadius, ResolvePlayerRoomTraitVisionMultiplier, IsInsideFlatRange, IsInsideForwardArc, ResolveForwardArcAngle, OnDrawGizmosSelected, DrawFlatCircle, DrawForwardArc
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 10개: Assets/Scripts/Authoring/Editor/DebugMonitorWindow.cs; Assets/Scripts/Gameplay/Entities/Shooter/Common/Vision/VisionDiscoveredVisibilityTarget.cs; Assets/Scripts/Gameplay/Entities/Shooter/Common/Vision/VisionVisibilitySystem.cs; Assets/Scripts/Gameplay/Entities/Shooter/Enemy/Common/EnemyShooterAimLaserController.cs; Assets/Scripts/Gameplay/Entities/Shooter/Enemy/Common/EnemyShooterVisionController.cs; Assets/Scripts/Gameplay/Entities/Shooter/Enemy/StateMachine/EnemyShooterCombatController.cs; Assets/Scripts/Gameplay/Entities/Shooter/Player/PlayerShooterVisionController.cs; Assets/Scripts/Gameplay/Entities/Shooter/Player/PlayerWorldSoundIndicator.cs

## Assets/Scripts/Gameplay/Entities/Shooter/Common/Vision/VisionDiscoveredVisibilityTarget.cs
- 분류: 일반 소스; 164행
- 타입: VisionDiscoveredVisibilityTarget : MonoBehaviour
- 메서드: Awake, OnEnable, LateUpdate, OnDestroy, RefreshRenderers, MarkDiscovered, CacheRenderersIfNeeded, ApplyVisibility, SetRenderersVisible
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 3개: Assets/Scripts/Gameplay/Items/World/WorldItemPickup.cs; Assets/Scripts/Gameplay/World/Sectors/Treasure/SectorTreasure.cs; Assets/Scripts/Presentation/UI/Gameplay/Map/MiniMap/SectorTacticalMiniMapGraphic.cs

## Assets/Scripts/Gameplay/Entities/Shooter/Common/Vision/VisionVisibilitySystem.cs
- 분류: 일반 소스; 303행
- 타입: VisionVisibilitySystem : MonoBehaviour
- 메서드: Awake, OnEnable, OnDisable, OnDestroy, Update, LateUpdate, Register, Unregister, IsVisible, UpdateVisibilityImmediate, ScanTargets, ResolveViewer, IsRevealedByVaccinePaint, ResolveMaskRenderManager, OnMaskRenderManagerReady
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 1개: Assets/Scenes/Manager/PersistenceManager.unity
- 타입 이름을 참조하는 소스 7개: Assets/Scripts/Gameplay/Entities/Shooter/Common/Vision/VisionDiscoveredVisibilityTarget.cs; Assets/Scripts/Gameplay/Entities/Shooter/Common/Vision/VisionVisibilityTarget.cs; Assets/Scripts/Gameplay/Entities/Shooter/Enemy/Common/EnemyShooterAimLaserController.cs; Assets/Scripts/Gameplay/Entities/Shooter/Player/PlayerWorldSoundIndicator.cs; Assets/Scripts/Presentation/UI/Gameplay/Map/MiniMap/SectorTacticalMiniMapGraphic.cs; Assets/Scripts/Presentation/UI/Gameplay/Vision/PlayerVisionFogOverlayUI.cs; Assets/Scripts/WIP/Legacy/Gameplay/Player/ExistingWIP/PlayerWorldRangeRing.cs

## Assets/Scripts/Gameplay/Entities/Shooter/Common/Vision/VisionVisibilityTarget.cs
- 분류: 일반 소스; 262행
- 타입: VisionVisibilityTarget : MonoBehaviour
- 메서드: Reset, Awake, OnEnable, OnDisable, SetVisionVisible, RefreshRenderers, RegisterRuntimeVisualRoot, ExcludeRendererFromVisionGate, HideRenderersByVision, RestoreRenderersHiddenByVision, ResolveRefs, CacheRenderersIfNeeded, HasConfiguredRenderer, RegisterRenderers, PruneRendererRegistrations
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 10개: Assets/Scripts/Gameplay/Entities/Shooter/Common/Core/ShooterDeathCorpseController.cs; Assets/Scripts/Gameplay/Entities/Shooter/Common/Core/ShooterEquipmentController.cs; Assets/Scripts/Gameplay/Entities/Shooter/Common/Vision/VisionVisibilitySystem.cs; Assets/Scripts/Gameplay/Entities/Shooter/Common/Visuals/ShooterVisualRig.cs; Assets/Scripts/Gameplay/Entities/Shooter/Enemy/Common/EnemyShooterAimLaserController.cs; Assets/Scripts/Gameplay/Entities/Shooter/Player/PlayerWorldSoundIndicator.cs; Assets/Scripts/Presentation/Camera/CameraEntityOcclusionFadeController.cs; Assets/Scripts/Presentation/UI/Gameplay/EnemyBarUI/EnemyScreenSpaceHPUIAnchor.cs

## Assets/Scripts/Gameplay/Entities/Shooter/Common/Visuals/ShooterCustomizePoolSO.cs
- 분류: 일반 소스; 28행
- 타입: ShooterCustomizePoolSO : ScriptableObject
- 메서드: GetRandom
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 0개: Assets/ScriptableObject/GameScene/Character/Shooter/EnemyShooter/NormalEnemy/EnemyShooterCustomizePool.asset
- 타입 이름을 참조하는 소스 0개: 

## Assets/Scripts/Gameplay/Entities/Shooter/Common/Visuals/ShooterCustomizeSO.cs
- 분류: 일반 소스; 90행
- 타입: ShooterCustomizeAttachment, ShooterCustomizeSO : ScriptableObject
- 메서드: Capture
- 직렬화 참조 12개 / 지정 씬 참조 그래프 내 0개: Assets/ScriptableObject/GameScene/Character/Shooter/EnemyShooter/Boss/Stage1RevolverBossCustomize.asset; Assets/ScriptableObject/GameScene/Character/Shooter/EnemyShooter/Boss/Stage2UniqueSniperBossCustomize.asset; Assets/ScriptableObject/GameScene/Character/Shooter/EnemyShooter/Boss/Stage3RocketBossCustomize.asset; Assets/ScriptableObject/GameScene/Character/Shooter/EnemyShooter/Boss/Stage4LMGBossCustomize.asset; Assets/ScriptableObject/GameScene/Character/Shooter/EnemyShooter/Boss/Stage5BossTemplateCustomize.asset; Assets/ScriptableObject/GameScene/Character/Shooter/EnemyShooter/BossGuard/Stage1BossGuardCustomize.asset; Assets/ScriptableObject/GameScene/Character/Shooter/EnemyShooter/BossGuard/Stage2BossGuardCustomize.asset; Assets/ScriptableObject/GameScene/Character/Shooter/EnemyShooter/BossGuard/Stage3BossGuardCustomize.asset
- 타입 이름을 참조하는 소스 5개: Assets/Editor/ShooterCustomizeStationWindow.cs; Assets/Scripts/Authoring/Editor/BoardChaseAuthoringHubWindow.cs; Assets/Scripts/Gameplay/Entities/Shooter/Common/Core/ShooterEquipmentController.cs; Assets/Scripts/Gameplay/Entities/Shooter/Common/Visuals/ShooterCustomizePoolSO.cs; Assets/Scripts/Gameplay/Entities/Shooter/Common/Visuals/ShooterVisualRig.cs

## Assets/Scripts/Gameplay/Entities/Shooter/Common/Visuals/ShooterVisualAppearanceProfileSO.cs
- 분류: 일반 소스; 34행
- 타입: ShooterVisualAppearanceProfileSO : ScriptableObject
- 메서드: 
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 1개: Assets/Scripts/Gameplay/Entities/Shooter/Common/Visuals/ShooterVisualRig.cs

## Assets/Scripts/Gameplay/Entities/Shooter/Common/Visuals/ShooterVisualAttachmentSO.cs
- 분류: 일반 소스; 58행
- 타입: ShooterVisualAttachmentSO : ScriptableObject
- 메서드: ApplyLocalTransform, SetPlacement, SetOccupiedRegions
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 2개: Assets/Scripts/Gameplay/Entities/Shooter/Common/Visuals/ShooterVisualAppearanceProfileSO.cs; Assets/Scripts/Gameplay/Entities/Shooter/Common/Visuals/ShooterVisualRig.cs

## Assets/Scripts/Gameplay/Entities/Shooter/Common/Visuals/ShooterVisualBodyProfileSO.cs
- 분류: 일반 소스; 60행
- 타입: ShooterVisualBodyProfileSO : ScriptableObject
- 메서드: SetBodyVisualTransform, SetSocketPose
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 2개: Assets/Scripts/Gameplay/Entities/Shooter/Common/Visuals/ShooterVisualAppearanceProfileSO.cs; Assets/Scripts/Gameplay/Entities/Shooter/Common/Visuals/ShooterVisualRig.cs

## Assets/Scripts/Gameplay/Entities/Shooter/Common/Visuals/ShooterVisualLocalTransform.cs
- 분류: 일반 소스; 72행
- 타입: ShooterVisualLocalTransform, ShooterVisualSocketPose
- 메서드: FromTransform, ApplyTo, SanitizeScale
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 6개: Assets/Editor/ShooterCustomizeStationWindow.cs; Assets/Scripts/Gameplay/Entities/Shooter/Common/Visuals/ShooterCustomizeSO.cs; Assets/Scripts/Gameplay/Entities/Shooter/Common/Visuals/ShooterVisualAttachmentSO.cs; Assets/Scripts/Gameplay/Entities/Shooter/Common/Visuals/ShooterVisualBodyProfileSO.cs; Assets/Scripts/Gameplay/Entities/Shooter/Common/Visuals/ShooterVisualRig.cs; Assets/Scripts/Gameplay/Items/Definitions/GasMaskItemSO.cs

## Assets/Scripts/Gameplay/Entities/Shooter/Common/Visuals/ShooterVisualOccupancy.cs
- 분류: 일반 소스; 24행
- 타입: ShooterVisualOccupancy
- 메서드: 
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 6개: Assets/Scripts/Gameplay/Entities/Common/Equipment/ArmorItemSO.cs; Assets/Scripts/Gameplay/Entities/Shooter/Common/Core/ShooterEquipmentController.cs; Assets/Scripts/Gameplay/Entities/Shooter/Common/Visuals/ShooterCustomizeSO.cs; Assets/Scripts/Gameplay/Entities/Shooter/Common/Visuals/ShooterVisualAttachmentSO.cs; Assets/Scripts/Gameplay/Entities/Shooter/Common/Visuals/ShooterVisualRig.cs; Assets/Scripts/Gameplay/Items/Definitions/GasMaskItemSO.cs

## Assets/Scripts/Gameplay/Entities/Shooter/Common/Visuals/ShooterVisualRig.cs
- 분류: 일반 소스; 567행
- 타입: ShooterVisualRig : MonoBehaviour, CosmeticInstance, CustomizeCosmeticInstance
- 메서드: Awake, OnDestroy, AutoResolveReferences, SetDefaultAppearance, SetDefaultCustomize, ResolveSocket, SetEquipmentOccupancy, ClearEquipmentOccupancy, SetEquipmentFaceSlotOccupied, ApplyAppearance, ApplyCustomize, ApplyBodyProfile, ApplyBodyTint, TryGetCosmeticInstance, AttachCosmetic, AttachCustomizeCosmetic, RegisterRuntimeVisualWithVision, ClearCosmeticAttachments, RefreshCosmeticOccupancy, CacheBasePose, ResetBasePose, CachePose, CacheBaseVisualRenderers, FindDescendant, RefreshFaceChildSocketVisibility, HasFaceCosmeticAttachment, SetSocketVisible
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 4개: Assets/Editor/ShooterCustomizeStationWindow.cs; Assets/Scripts/Gameplay/Entities/Shooter/Common/Core/ShooterEquipmentController.cs; Assets/Scripts/Gameplay/Entities/Shooter/Common/Visuals/ShooterCustomizeSO.cs; Assets/Scripts/Gameplay/Entities/Shooter/Common/Visuals/ShooterVisualBodyProfileSO.cs

## Assets/Scripts/Gameplay/Entities/Shooter/Common/Visuals/ShooterVisualSocketId.cs
- 분류: 일반 소스; 26행
- 타입: ShooterVisualSocketId
- 메서드: 
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 8개: Assets/Editor/ShooterCustomizeStationWindow.cs; Assets/Scripts/Gameplay/Entities/Shooter/Common/Core/ShooterEquipmentController.cs; Assets/Scripts/Gameplay/Entities/Shooter/Common/Visuals/ShooterCustomizeSO.cs; Assets/Scripts/Gameplay/Entities/Shooter/Common/Visuals/ShooterVisualAttachmentSO.cs; Assets/Scripts/Gameplay/Entities/Shooter/Common/Visuals/ShooterVisualBodyProfileSO.cs; Assets/Scripts/Gameplay/Entities/Shooter/Common/Visuals/ShooterVisualLocalTransform.cs; Assets/Scripts/Gameplay/Entities/Shooter/Common/Visuals/ShooterVisualRig.cs; Assets/Scripts/Gameplay/Items/Definitions/GasMaskItemSO.cs

## Assets/Scripts/Gameplay/Entities/Shooter/Enemy/Common/EnemyLootInventoryRuntime.cs
- 분류: 일반 소스; 1269행
- 타입: EnemyLootBulletStack, EnemyLootWeaponStack, EnemyLootInventoryRuntime : InventoryRuntimeBase
- 메서드: Set, Clear, SetSuppressRandomDropReward, Reset, Awake, Initialize, GetWeapon, GetWeaponAmount, GetWeaponStack, SetWeapon, SetArmor, SetBulletStack, SetItemStack, TryAddItemStack, TryConsumeCarriedItem, ClearWeapon, TrySwapWeaponSlots, ClearArmor, ClearBulletStack, ClearItemStack, ClearAllLoot, ConfigureInteractionChannels, ConfigureInventoryCapacity, TrySplitBulletStack, TrySplitItemStack, TryInteract, OpenAfterDelay, OpenLootPanel, BuildInteractionDebugInfo, TryTakeWeapon, TryTakeArmor, TryTakeBulletStack, TryTakeItemStack, SyncArmorDurabilityFromEquipment, SyncEquipmentFromRuntime, ResolveRefs, NormalizeBulletStacks, NormalizeWeaponSlots, NormalizeItemStacks, HasBulletLoot, IsSameItem, HasWeaponLoot, BuildWeaponDebugText, HasItemLoot, ResolveArmorDurability, Fail, ResolveItemName
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 1개: Assets/Prefabs/GameScene/Shooter/EnemyShooter/EnemyShooter_Base.prefab
- 타입 이름을 참조하는 소스 15개: Assets/Scripts/Authoring/Editor/DebugMonitorWindow.cs; Assets/Scripts/Authoring/Editor/DebugMonitorWindow.Operations.cs; Assets/Scripts/Core/Events/ScriptableObject/EnemyLootOpenRequestEventChannelSO.cs; Assets/Scripts/Gameplay/Entities/Common/StateMachine/Actions/DropRewardSO.cs; Assets/Scripts/Gameplay/Entities/Shooter/Common/Core/ShooterThrowableAttack.cs; Assets/Scripts/Gameplay/Entities/Shooter/Enemy/Common/EnemyShooter.cs; Assets/Scripts/Gameplay/Entities/Shooter/Enemy/Common/EnemyShooterDeathCorpseController.cs; Assets/Scripts/Gameplay/Entities/Shooter/Enemy/StateMachine/EnemyShooterCombatController.cs

## Assets/Scripts/Gameplay/Entities/Shooter/Enemy/Common/EnemyShooter.cs
- 분류: 일반 소스; 281행
- 타입: EnemyShooter : Enemy
- 메서드: Reset, Awake, SetSpawnReady, SetEnemyShooterConfig, EquipWeaponLoadout, OnAlertTriggerChange, OnAttackTriggerChange, NotifyDamagedBy, OnDisable, ResolveRefs, ApplySpawnVisualVisibility, HideRenderersUnder, ResolveSpawnLoadout
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 1개: Assets/Prefabs/GameScene/Shooter/EnemyShooter/EnemyShooter_Base.prefab
- 타입 이름을 참조하는 소스 15개: Assets/Editor/LootProgressionEditorWindow.cs; Assets/Editor/ShooterCustomizeStationWindow.cs; Assets/Scripts/Authoring/Editor/BossConfigPrefabLinkProvisioner.cs; Assets/Scripts/Authoring/Editor/ContentPreflightWindow.cs; Assets/Scripts/Authoring/Editor/DebugMonitorWindow.cs; Assets/Scripts/Authoring/Editor/StandardEnemyConfigPrefabLinkRepair.cs; Assets/Scripts/Gameplay/Entities/Shooter/Enemy/Definitions/EnemyShooterConfigSO.cs; Assets/Scripts/Gameplay/Entities/Shooter/Enemy/StateMachine/EnemyShooterCoverPoint.cs

## Assets/Scripts/Gameplay/Entities/Shooter/Enemy/Common/EnemyShooterAimLaserController.cs
- 분류: 일반 소스; 258행
- 타입: EnemyShooterAimLaserController : MonoBehaviour
- 메서드: Reset, Awake, OnDisable, LateUpdate, ShouldRender, ShouldRenderForPlayerVision, IsAimingAtPlayer, TryResolveAlertAimPoint, CacheAimPoint, EnsureLineRenderer, SetVisible, ResolveRefs
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 1개: Assets/Prefabs/GameScene/Shooter/EnemyShooter/EnemyShooter_Base.prefab
- 타입 이름을 참조하는 소스 0개: 

## Assets/Scripts/Gameplay/Entities/Shooter/Enemy/Common/EnemyShooterCharacterAudio.cs
- 분류: 일반 소스; 208행
- 타입: EnemyShooterCharacterAudio : EntityShooterCharacterAudio
- 메서드: ResolveRefs, SubscribeAdditionalEvents, UnsubscribeAdditionalEvents, Update, TickAlertCue, PlayDash, HandleHealthChanged, TickFootsteps, ResolveFootstepInterval
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 1개: Assets/Prefabs/GameScene/Shooter/EnemyShooter/EnemyShooter_Base.prefab
- 타입 이름을 참조하는 소스 0개: 

## Assets/Scripts/Gameplay/Entities/Shooter/Enemy/Common/EnemyShooterDashController.cs
- 분류: 일반 소스; 95행
- 타입: EnemyShooterDashController : ShooterDashController
- 메서드: TryStartDefensiveDash, Reset, Awake, CanStartDash, UseForwardWhenNoMovementInput, ResolveDashDuration, ResolveDashCooldown, ResolveDashSpeed, ResolveRefs
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 1개: Assets/Prefabs/GameScene/Shooter/EnemyShooter/EnemyShooter_Base.prefab
- 타입 이름을 참조하는 소스 1개: Assets/Scripts/Gameplay/Entities/Shooter/Enemy/StateMachine/EnemyShooterCombatController.cs

## Assets/Scripts/Gameplay/Entities/Shooter/Enemy/Common/EnemyShooterDeathCorpseController.cs
- 분류: 일반 소스; 89행
- 타입: EnemyShooterDeathCorpseController : ShooterDeathCorpseController
- 메서드: Reset, Awake, BeforeConvertToCorpse, AfterConvertToCorpse, ResolveRefs, StopNavMeshAgent
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 1개: Assets/Prefabs/GameScene/Shooter/EnemyShooter/EnemyShooter_Base.prefab
- 타입 이름을 참조하는 소스 0개: 

## Assets/Scripts/Gameplay/Entities/Shooter/Enemy/Common/EnemyShooterInfection.cs
- 분류: 일반 소스; 43행
- 타입: EnemyShooterInfection : ShooterInfection
- 메서드: ResolveRefs
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 1개: Assets/Prefabs/GameScene/Shooter/EnemyShooter/EnemyShooter_Base.prefab
- 타입 이름을 참조하는 소스 0개: 

## Assets/Scripts/Gameplay/Entities/Shooter/Enemy/Common/EnemyShooterShoot.cs
- 분류: 일반 소스; 600행
- 타입: EnemyShooterShoot : EntityShootCore
- 메서드: TryGetAimLaserRay, TryGetAimOrigin, Reset, Awake, SetConfig, SetRuntimeBullet, HasClearBallisticPathTo, HasClearBallisticPathFromRootPosition, TryFireAt, TryFirePrimaryAt, TryFireSpecialAt, BuildStats, ResolveConfig, ResolveRefs, EnsureFootstepSoundEmitter, EnsureMagazineReady, BeginReload, TickReload, ResolveMagazineSize, ResolveReloadDuration, PublishGunshotSound, IsColliderBelowRoot
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 1개: Assets/Prefabs/GameScene/Shooter/EnemyShooter/EnemyShooter_Base.prefab
- 타입 이름을 참조하는 소스 6개: Assets/Scripts/Gameplay/Entities/Shooter/Common/Core/ShooterThrowableAttack.cs; Assets/Scripts/Gameplay/Entities/Shooter/Enemy/Common/EnemyShooter.cs; Assets/Scripts/Gameplay/Entities/Shooter/Enemy/Common/EnemyShooterAimLaserController.cs; Assets/Scripts/Gameplay/Entities/Shooter/Enemy/Common/EnemyShooterCharacterAudio.cs; Assets/Scripts/Gameplay/Entities/Shooter/Enemy/Common/EnemyShooterDashController.cs; Assets/Scripts/Gameplay/Entities/Shooter/Enemy/StateMachine/EnemyShooterCombatController.cs

## Assets/Scripts/Gameplay/Entities/Shooter/Enemy/Common/EnemyShooterStatsRuntime.cs
- 분류: 일반 소스; 377행
- 타입: EnemyShooterStatsRuntime : ShooterStatsRuntime, bonus, StatAccumulator
- 메서드: Reset, Awake, SetConfig, SetRuntimeBullet, ResolveAttackDamage, ResolvePaintMarkDamage, ResolveInfectionDamage, ResolveShotsPerSecond, ResolveMagazineSize, ResolveArmorHealthDurabilityLossMultiplier, ResolveArmorInfectionDurabilityLossMultiplier, ResolveConfig, ResolveRefs, ResolveSectorTraitModifier, ResolveSectorTraitAttackDamageMultiplier, ResolveSectorTraitMaxHealthMultiplier, ResolveSectorTraitMoveSpeedMultiplier, ResolveSectorTraitArmorClassBonus, ResolveWithRuntimeModifiers, ResolveWithExtraModifiers, ApplyMatchingModifiers, Apply, Resolve
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 1개: Assets/Prefabs/GameScene/Shooter/EnemyShooter/EnemyShooter_Base.prefab
- 타입 이름을 참조하는 소스 10개: Assets/Scripts/Gameplay/Entities/Shooter/Common/Core/ShooterStatsRuntime.cs; Assets/Scripts/Gameplay/Entities/Shooter/Enemy/Common/EnemyShooter.cs; Assets/Scripts/Gameplay/Entities/Shooter/Enemy/Common/EnemyShooterInfection.cs; Assets/Scripts/Gameplay/Entities/Shooter/Enemy/Common/EnemyShooterShoot.cs; Assets/Scripts/Gameplay/Entities/Shooter/Enemy/StateMachine/EnemyShooterCombatController.cs; Assets/Scripts/Gameplay/Entities/Shooter/Player/PlayerStatsRuntime.cs; Assets/Scripts/Gameplay/Items/Loot/LockableLootObject.cs; Assets/Scripts/Gameplay/Items/Loot/LockedLootProfileSO.cs

## Assets/Scripts/Gameplay/Entities/Shooter/Enemy/Common/EnemyShooterVisionController.cs
- 분류: 일반 소스; 74행
- 타입: EnemyShooterVisionController : EntityVisionController
- 메서드: Reset, Awake, ResolveRefs, ResolveAim01, ResolveForwardDirection
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 1개: Assets/Prefabs/GameScene/Shooter/EnemyShooter/EnemyShooter_Base.prefab
- 타입 이름을 참조하는 소스 0개: 

## Assets/Scripts/Gameplay/Entities/Shooter/Enemy/Definitions/EnemyShooterConfigSO.cs
- 분류: 일반 소스; 504행
- 타입: EnemyShooterRole, EnemyShooterAttackMovementMode, EnemyShooterFsmArchetype, EnemyShooterConfigSO : ShooterStatConfigSO
- 메서드: PickEnemyLoadout
- 직렬화 참조 2개 / 지정 씬 참조 그래프 내 1개: Assets/ScriptableObject/GameScene/Character/Shooter/EnemyShooter/Boss/PunisherConfig.asset; Assets/ScriptableObject/GameScene/Character/Shooter/EnemyShooter/NormalEnemy/Butcher/ButcherShooterConfig.asset
- 타입 이름을 참조하는 소스 25개: Assets/Editor/LootProgressionEditorWindow.cs; Assets/Scripts/Authoring/Editor/BossConfigPrefabLinkProvisioner.cs; Assets/Scripts/Authoring/Editor/ContentPreflightWindow.cs; Assets/Scripts/Authoring/Editor/DebugMonitorWindow.cs; Assets/Scripts/Authoring/Editor/DebugMonitorWindow.Operations.cs; Assets/Scripts/Authoring/Editor/SectorMakerWindow.cs; Assets/Scripts/Authoring/Editor/StandardEnemyConfigPrefabLinkRepair.cs; Assets/Scripts/Gameplay/Entities/Common/Equipment/EntityEquipmentRuntime.cs

## Assets/Scripts/Gameplay/Entities/Shooter/Enemy/Effects/EnemyShooterEffectController.cs
- 분류: 일반 소스; 24행
- 타입: EnemyShooterEffectController : ShooterEffectController
- 메서드: Awake, PlaySpawnParticles
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 3개: Assets/Scripts/Gameplay/Entities/Creature/CommonEnemyStateMachine/Action/PlayEnemyCreatureSpawnParticlesActionSO.cs; Assets/Scripts/Gameplay/Entities/Shooter/Enemy/Common/EnemyShooter.cs; Assets/Scripts/Gameplay/Entities/Shooter/Enemy/Raider/RaiderEffectController.cs

## Assets/Scripts/Gameplay/Entities/Shooter/Enemy/Raider/RaiderEffectController.cs
- 분류: 일반 소스; 13행
- 타입: RaiderEffectController : EnemyShooterEffectController
- 메서드: 
- 직렬화 참조 3개 / 지정 씬 참조 그래프 내 3개: Assets/Prefabs/GameScene/Shooter/EnemyShooter/Raider/Raider.prefab; Assets/Prefabs/GameScene/Shooter/EnemyShooter/TempBossGuardShooter.prefab; Assets/Prefabs/GameScene/Shooter/EnemyShooter/TempBossShooter.prefab
- 타입 이름을 참조하는 소스 0개: 

## Assets/Scripts/Gameplay/Entities/Shooter/Enemy/Raider/ScriptableObjects/RaiderShooterConfigSO.cs
- 분류: 일반 소스; 20행
- 타입: RaiderShooterConfigSO : EnemyShooterConfigSO
- 메서드: PickLoadout
- 직렬화 참조 20개 / 지정 씬 참조 그래프 내 18개: Assets/ScriptableObject/GameScene/Character/Shooter/EnemyShooter/Boss/Stage1RevolverBossConfig.asset; Assets/ScriptableObject/GameScene/Character/Shooter/EnemyShooter/Boss/Stage2SniperBossConfig.asset; Assets/ScriptableObject/GameScene/Character/Shooter/EnemyShooter/Boss/Stage3RocketBossConfig.asset; Assets/ScriptableObject/GameScene/Character/Shooter/EnemyShooter/Boss/Stage4LMGBossConfig.asset; Assets/ScriptableObject/GameScene/Character/Shooter/EnemyShooter/Boss/Stage5BossTemplateConfig.asset; Assets/ScriptableObject/GameScene/Character/Shooter/EnemyShooter/Boss/TempBossShooterConfig.asset; Assets/ScriptableObject/GameScene/Character/Shooter/EnemyShooter/BossGuard/Stage1BossGuardConfig.asset; Assets/ScriptableObject/GameScene/Character/Shooter/EnemyShooter/BossGuard/Stage2BossGuardConfig.asset
- 타입 이름을 참조하는 소스 0개: 

## Assets/Scripts/Gameplay/Entities/Shooter/Enemy/StateMachine/EnemyShooterAcquireTargetActionSO.cs
- 분류: 일반 소스; 47행
- 타입: EnemyShooterAcquireTargetActionSO : StateActionSO<EnemyShooterAcquireTargetAction>, EnemyShooterAcquireTargetAction : StateAction
- 메서드: Awake, OnStateEnter, OnUpdate
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 1개: Assets/ScriptableObject/StateMachine/Shooter/EnemyShooter/Action/EnemyShooter_AcquireTargetAction.asset
- 타입 이름을 참조하는 소스 0개: 

## Assets/Scripts/Gameplay/Entities/Shooter/Enemy/StateMachine/EnemyShooterAttackActionSO.cs
- 분류: 일반 소스; 50행
- 타입: EnemyShooterAttackActionSO : StateActionSO<EnemyShooterAttackAction>, EnemyShooterAttackAction : StateAction
- 메서드: Awake, OnStateEnter, OnUpdate, OnStateExit
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 1개: Assets/ScriptableObject/StateMachine/Shooter/EnemyShooter/Action/EnemyShooter_AttackAction.asset
- 타입 이름을 참조하는 소스 0개: 

## Assets/Scripts/Gameplay/Entities/Shooter/Enemy/StateMachine/EnemyShooterChaseActionSO.cs
- 분류: 일반 소스; 77행
- 타입: EnemyShooterChaseActionSO : StateActionSO<EnemyShooterChaseAction>, ChaseMode, EnemyShooterChaseAction : StateAction
- 메서드: Awake, OnStateEnter, OnUpdate, OnStateExit
- 직렬화 참조 2개 / 지정 씬 참조 그래프 내 2개: Assets/ScriptableObject/StateMachine/Shooter/EnemyShooter/Action/EnemyShooter_ChaseTargetAction.asset; Assets/ScriptableObject/StateMachine/Shooter/EnemyShooter/Action/EnemyShooter_InvestigateAction.asset
- 타입 이름을 참조하는 소스 0개: 

## Assets/Scripts/Gameplay/Entities/Shooter/Enemy/StateMachine/EnemyShooterClearCombatActionSO.cs
- 분류: 일반 소스; 58행
- 타입: EnemyShooterClearCombatActionSO : StateActionSO<EnemyShooterClearCombatAction>, EnemyShooterClearCombatAction : StateAction
- 메서드: Awake, OnStateEnter, OnUpdate
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 1개: Assets/ScriptableObject/StateMachine/Shooter/EnemyShooter/Action/EnemyShooter_ClearCombatAction.asset
- 타입 이름을 참조하는 소스 0개: 

## Assets/Scripts/Gameplay/Entities/Shooter/Enemy/StateMachine/EnemyShooterCombatConditionSO.cs
- 분류: 일반 소스; 96행
- 타입: EnemyShooterCombatConditionSO : StateConditionSO<EnemyShooterCombatCondition>, Query, EnemyShooterCombatCondition : Condition
- 메서드: Awake, Statement
- 직렬화 참조 12개 / 지정 씬 참조 그래프 내 11개: Assets/ScriptableObject/StateMachine/Shooter/EnemyShooter/Condition/EnemyShooter_CanAttackTargetCondition.asset; Assets/ScriptableObject/StateMachine/Shooter/EnemyShooter/Condition/EnemyShooter_InvestigationFinishedCondition.asset; Assets/ScriptableObject/StateMachine/Shooter/EnemyShooter/Condition/EnemyShooter_MeleeActionFinishedCondition.asset; Assets/ScriptableObject/StateMachine/Shooter/EnemyShooter/Condition/EnemyShooter_PatrolDestinationReachedCondition.asset; Assets/ScriptableObject/StateMachine/Shooter/EnemyShooter/Condition/EnemyShooter_PatrolIdleFinishedCondition.asset; Assets/ScriptableObject/StateMachine/Shooter/EnemyShooter/Condition/EnemyShooter_ShouldAttackCondition.asset; Assets/ScriptableObject/StateMachine/Shooter/EnemyShooter/Condition/EnemyShooter_ShouldChaseTargetCondition.asset; Assets/ScriptableObject/StateMachine/Shooter/EnemyShooter/Condition/EnemyShooter_ShouldInvestigateCondition.asset
- 타입 이름을 참조하는 소스 2개: Assets/Scripts/Authoring/Editor/ReplaceObject_ContextualHierarchy/ReplaceContextMenu.cs; Assets/Scripts/StateMachine/Editor/TransitionTableEditorWindow.cs

## Assets/Scripts/Gameplay/Entities/Shooter/Enemy/StateMachine/EnemyShooterCombatController.cs
- 분류: 일반 소스; 4934행
- 타입: EnemyShooterGoal, EnemyShooterInvestigationType, EnemyShooterAttackPhase, EnemyShooterCoverTacticPhase, EnemyShooterCombatController : MonoBehaviour
- 메서드: TryGetCurrentAimDirection, TryGetCurrentAimPoint, TryGetCurrentLookPoint, TryGetCurrentMoveDirection, CanUseCurrentRangedWeapon, TryActivateRangedWeaponForCombat, TryActivateMeleeWeaponForCombat, CanActivateConfiguredCombatWeaponSlot, TryActivateConfiguredCombatWeaponSlot, TryGetConfiguredMeleeRange, IsTargetInsideConfiguredMeleeRange, ResolveAttackDebugStatus, Reset, Awake, OnEnable, OnDisable, LateUpdate, TickAwareness, ForceAcquirePlayer, AlertToPlayer, TryAlertToPlayerFromDamage, ClearCombat, BeginPatrol, TickPatrol, BeginChase, TickChase, TryResolveTacticalChaseDestination, RefreshTacticalIntent, TryResolveTacticalFlankDestination, TryStartCombatTactics, TryAcquireCoverTactic, TryCreateDebugRuntimeCoverPoint, IsDebugCoverObstacle, TryResolveDebugCoverPeek, TryResolveCoverPeekDestination, TryTickCoverTactic, TryTickActiveCombatFlank, ReleaseCoverTactic, ApplyCoverApproachSpeed, ResetTacticalIntent, RecoverStalledChase, BeginInvestigate, TickInvestigate, BeginInvestigationSearchWait, TryCompleteFinishedInvestigation, StopMovement, TryHoldForStatusActionBlock, ResolveStatusActionBlocked, TryReadBooleanProperty, BeginAttack, TickAttack, EndAttack, BeginMeleeAttack, TickMeleeAttack, EndMeleeAttack, BeginThrowableAttack, TickThrowableAttack, EndThrowableAttack, CanUseMeleeAttack, TryExecuteMeleeAttack, CanUseThrowableAttack, IsThrowablePointUnsafeForAllies, TryExecuteThrowableAttack, TryResolveThrowableTargetPoint, GetPlanarDistance, TryAcquireVisiblePlayer, SetTarget, ClearTarget, ClearSoundInvestigation, ClearLastKnownInvestigation, ClearInvestigation, ClearInvestigationReentryCooldown, ClearQueuedSoundInvestigation, SetLastKnownPosition, BeginDirectSightReaction, ClearActiveReaction, SetSoundInvestigation, QueueSoundInvestigation, TryPromoteQueuedSoundInvestigation, RefreshTargetLock, UpdateAimDirection, ResolveTargetPoint, UpdateBurstFire, TryFireWhileMovingToCover, TickAttackMovement, TickMuzzleBlockedReposition, TryFireOnce, ResetBurstTracking, TryStartDefensiveDash, TickDashMovement, ResolveDashDirection, TryDashWhenHealthLow, ResolveShotsPerSecond, TryPickPatrolDestination, RefreshIncomingThrowableThreat, TryTickIncomingThrowableEvasion, AbortCurrentAttackForIncomingThrowable, TryResolveIncomingThrowableEscapeDestination, ClearIncomingThrowableEvasion, TryDashFromIncomingBullet, ResolveIncomingBulletDashDirection, CanDashInDirection, TryGetHarmfulAreaAvoidanceDestination, EvaluateHarmfulAreaThreat, HasReached, HasForgottenTarget, RefreshGoal, ResolveInvestigationType, TryResolveInvestigationDestination, IsTargetBeyondChaseLoseRange, TryNormalizePlanar, ResolveInvestigateArriveDistance, ResolveInvestigationWaitSeconds, ResolveBurstCooldownSeconds, TryResolveBurstIntervalMoveDestination, TryResolveMuzzleClearanceDestination, HasClearMuzzlePathToTarget, GetPlanarDistanceToTarget, ResolveMoveSpeed, ResolveChaseStoppingDistance, ResolveAttackStartRange, ResolveAttackKeepRange, ResolveDirectAttackRange, CanUseAgent, ConfigureAgent, StopAgentHard, FaceDirection, OnHealthChanged, OnHitReceived, OnSoundStimulus, ShouldIgnoreSoundStimulus, ResolveSoundStimulusPriority, CanBypassInvestigationReentry, TryRegisterSoundStimulus, CleanupExpiredSoundStimulusKeys, BuildSoundStimulusKey, ExpireSoundInvestigationIfNeeded, DebugSoundStimulus, IsThisShooter, IsOwnSource, TryResolvePlayerTransform, IsWithinAwarenessRange, DebugMovement, ResolveRefs, TrySetAgentDestination, HasUsableNavigationPath, ResolveInvestigationArrivalDestination, ClearNavigationRequest, PlanarSqrDistance, TryProjectToNavMesh, BuildAgentDebugInfo
- 직렬화 참조 2개 / 지정 씬 참조 그래프 내 1개: Assets/Prefabs/GameScene/Shooter/EnemyShooter/EnemyShooter_Base.prefab; Assets/Scenes/BattleTestScene.unity
- 타입 이름을 참조하는 소스 22개: Assets/Scripts/Authoring/Editor/DebugMonitorWindow.cs; Assets/Scripts/Authoring/Editor/DebugMonitorWindow.Operations.cs; Assets/Scripts/Gameplay/Entities/Shooter/Common/StateMachine/Action/ShooterRotateActionSO.cs; Assets/Scripts/Gameplay/Entities/Shooter/Enemy/Common/EnemyShooter.cs; Assets/Scripts/Gameplay/Entities/Shooter/Enemy/Common/EnemyShooterAimLaserController.cs; Assets/Scripts/Gameplay/Entities/Shooter/Enemy/Common/EnemyShooterCharacterAudio.cs; Assets/Scripts/Gameplay/Entities/Shooter/Enemy/Common/EnemyShooterDashController.cs; Assets/Scripts/Gameplay/Entities/Shooter/Enemy/StateMachine/EnemyShooterAcquireTargetActionSO.cs

## Assets/Scripts/Gameplay/Entities/Shooter/Enemy/StateMachine/EnemyShooterCombatController.Diagnostics.cs
- 분류: 일반 소스; 343행
- 타입: EnemyShooterCombatDiagnosticsSnapshot, EnemyShooterCombatController
- 메서드: GetDiagnosticsSnapshot, SetDebugCombatOverridesEnabled, SetDebugCombatToggles, SetDebugCombatTuning, DebugTryStartDefensiveDash, DebugTryThrowCarriedThrowable, CopyConfigIntoDebugCombatOverrides, CopySharedDashConfigIntoDebugOverrides
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 22개: Assets/Scripts/Authoring/Editor/DebugMonitorWindow.cs; Assets/Scripts/Authoring/Editor/DebugMonitorWindow.Operations.cs; Assets/Scripts/Gameplay/Entities/Shooter/Common/StateMachine/Action/ShooterRotateActionSO.cs; Assets/Scripts/Gameplay/Entities/Shooter/Enemy/Common/EnemyShooter.cs; Assets/Scripts/Gameplay/Entities/Shooter/Enemy/Common/EnemyShooterAimLaserController.cs; Assets/Scripts/Gameplay/Entities/Shooter/Enemy/Common/EnemyShooterCharacterAudio.cs; Assets/Scripts/Gameplay/Entities/Shooter/Enemy/Common/EnemyShooterDashController.cs; Assets/Scripts/Gameplay/Entities/Shooter/Enemy/StateMachine/EnemyShooterAcquireTargetActionSO.cs

## Assets/Scripts/Gameplay/Entities/Shooter/Enemy/StateMachine/EnemyShooterCoverPoint.cs
- 분류: 일반 소스; 161행
- 타입: EnemyShooterCoverPoint : MonoBehaviour
- 메서드: CreateRuntime, OnEnable, OnDisable, TryReserve, Release, TryGetPeekPosition, IsReservationValid, OnDrawGizmosSelected, DrawPeekGizmo
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 3개: Assets/Scripts/Authoring/Editor/DebugMonitorWindow.Operations.cs; Assets/Scripts/Gameplay/Entities/Shooter/Enemy/Definitions/EnemyShooterConfigSO.cs; Assets/Scripts/Gameplay/Entities/Shooter/Enemy/StateMachine/EnemyShooterCombatController.cs

## Assets/Scripts/Gameplay/Entities/Shooter/Enemy/StateMachine/EnemyShooterMeleeActionSO.cs
- 분류: 일반 소스; 50행
- 타입: EnemyShooterMeleeActionSO : StateActionSO<EnemyShooterMeleeAction>, EnemyShooterMeleeAction : StateAction
- 메서드: Awake, OnStateEnter, OnUpdate, OnStateExit
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 1개: Assets/ScriptableObject/StateMachine/Shooter/EnemyShooter/Action/EnemyShooter_MeleeAction.asset
- 타입 이름을 참조하는 소스 0개: 

## Assets/Scripts/Gameplay/Entities/Shooter/Enemy/StateMachine/EnemyShooterMoveActionSO.cs
- 분류: 일반 소스; 78행
- 타입: EnemyShooterMoveActionSO : StateActionSO<EnemyShooterMoveAction>, EnemyShooterMoveAction : StateAction
- 메서드: Awake, OnStateEnter, OnUpdate, OnStateExit
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 0개: Assets/ScriptableObject/StateMachine/Shooter/EnemyShooter/Action/EnemyShooter_MoveAction.asset
- 타입 이름을 참조하는 소스 0개: 

## Assets/Scripts/Gameplay/Entities/Shooter/Enemy/StateMachine/EnemyShooterPatrolActionSO.cs
- 분류: 일반 소스; 57행
- 타입: EnemyShooterPatrolActionSO : StateActionSO<EnemyShooterPatrolAction>, EnemyShooterPatrolAction : StateAction
- 메서드: Awake, OnStateEnter, OnUpdate, OnStateExit
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 1개: Assets/ScriptableObject/StateMachine/Shooter/EnemyShooter/Action/EnemyShooter_PatrolAction.asset
- 타입 이름을 참조하는 소스 0개: 

## Assets/Scripts/Gameplay/Entities/Shooter/Enemy/StateMachine/EnemyShooterStateMachineUtility.cs
- 분류: 일반 소스; 53행
- 타입: EnemyShooterStateMachineUtility
- 메서드: ResolveCombatController
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 8개: Assets/Scripts/Gameplay/Entities/Shooter/Enemy/StateMachine/EnemyShooterAcquireTargetActionSO.cs; Assets/Scripts/Gameplay/Entities/Shooter/Enemy/StateMachine/EnemyShooterAttackActionSO.cs; Assets/Scripts/Gameplay/Entities/Shooter/Enemy/StateMachine/EnemyShooterChaseActionSO.cs; Assets/Scripts/Gameplay/Entities/Shooter/Enemy/StateMachine/EnemyShooterCombatConditionSO.cs; Assets/Scripts/Gameplay/Entities/Shooter/Enemy/StateMachine/EnemyShooterMeleeActionSO.cs; Assets/Scripts/Gameplay/Entities/Shooter/Enemy/StateMachine/EnemyShooterMoveActionSO.cs; Assets/Scripts/Gameplay/Entities/Shooter/Enemy/StateMachine/EnemyShooterPatrolActionSO.cs; Assets/Scripts/Gameplay/Entities/Shooter/Enemy/StateMachine/EnemyShooterThrowableActionSO.cs

## Assets/Scripts/Gameplay/Entities/Shooter/Enemy/StateMachine/EnemyShooterTacticalIntentSelector.cs
- 분류: 일반 소스; 123행
- 타입: EnemyShooterTacticalIntent, EnemyShooterTacticalIntentSelector
- 메서드: TryClaimDuelHolder, HasDuelHolder, IsDuelHolder, ReleaseDuelHolder, IsValidHolder
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 2개: Assets/Scripts/Gameplay/Entities/Shooter/Enemy/StateMachine/EnemyShooterCombatController.cs; Assets/Scripts/Gameplay/Entities/Shooter/Enemy/StateMachine/EnemyShooterCombatController.Diagnostics.cs

## Assets/Scripts/Gameplay/Entities/Shooter/Enemy/StateMachine/EnemyShooterThrowableActionSO.cs
- 분류: 일반 소스; 50행
- 타입: EnemyShooterThrowableActionSO : StateActionSO<EnemyShooterThrowableAction>, EnemyShooterThrowableAction : StateAction
- 메서드: Awake, OnStateEnter, OnUpdate, OnStateExit
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 1개: Assets/ScriptableObject/StateMachine/Shooter/EnemyShooter/Action/EnemyShooter_ThrowableAction.asset
- 타입 이름을 참조하는 소스 0개: 

## Assets/Scripts/Gameplay/Entities/Shooter/Player/Action/PlayerShooterLocomotionActionSO.cs
- 분류: 일반 소스; 120행
- 타입: PlayerShooterLocomotionActionSO : StateActionSO<PlayerShooterLocomotionAction>, PlayerShooterLocomotionAction : ShooterLocomotionActionBase
- 메서드: OnAwakeLocomotion, OnUpdate, OnStateExit, ResolvePlayerInput, ApplyCharacterControllerMovement
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 1개: Assets/ScriptableObject/StateMachine/Shooter/Player/Action/PlayerShooterLocomotion.asset
- 타입 이름을 참조하는 소스 0개: 

## Assets/Scripts/Gameplay/Entities/Shooter/Player/Definitions/DashConfigSO.cs
- 분류: 일반 소스; 59행
- 타입: DashConfigSO : ScriptableObject
- 메서드: 
- 직렬화 참조 2개 / 지정 씬 참조 그래프 내 1개: Assets/ScriptableObject/GameScene/Character/Shooter/EnemyShooter/Config/EnemyDashConfig.asset; Assets/ScriptableObject/GameScene/Character/Shooter/Player/Config/Dash/PlayerDashConfig.asset
- 타입 이름을 참조하는 소스 3개: Assets/Scripts/Gameplay/Entities/Shooter/Common/Core/ShooterDashController.cs; Assets/Scripts/Gameplay/Entities/Shooter/Enemy/StateMachine/EnemyShooterCombatController.Diagnostics.cs; Assets/Scripts/Gameplay/Entities/Shooter/Player/PlayerStatsRuntime.cs

## Assets/Scripts/Gameplay/Entities/Shooter/Player/Definitions/DashEventChannelSO.cs
- 분류: 일반 소스; 57행
- 타입: DashEventType, DashEvent, DashEventChannelSO : ScriptableObject
- 메서드: RaiseEvent
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 1개: Assets/ScriptableObject/EventChannel/Dash/DashEventChannel.asset
- 타입 이름을 참조하는 소스 4개: Assets/Scripts/Core/Input/InputReader.cs; Assets/Scripts/Gameplay/Entities/Shooter/Common/Core/ShooterDashController.cs; Assets/Scripts/Gameplay/Entities/Shooter/Player/Inventory/PlayerConsumableUseRuntime.cs; Assets/Scripts/Gameplay/Entities/Shooter/Player/PlayerCharacter.cs

## Assets/Scripts/Gameplay/Entities/Shooter/Player/Definitions/PlayerInfectionRulesSO.cs
- 분류: 일반 소스; 40행
- 타입: PlayerInfectionRulesSO : ScriptableObject
- 메서드: 
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 0개: 

## Assets/Scripts/Gameplay/Entities/Shooter/Player/Definitions/PlayerStatsConfigSO.cs
- 분류: 일반 소스; 44행
- 타입: PlayerStatsConfigSO : ShooterStatConfigSO
- 메서드: TryRollPlayerLoadout
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 1개: Assets/ScriptableObject/GameScene/Character/Shooter/Player/Config/PlayerBaseStatsConfig.asset
- 타입 이름을 참조하는 소스 6개: Assets/Scripts/Gameplay/Entities/Shooter/Player/PlayerBulletLoadoutRuntime.cs; Assets/Scripts/Gameplay/Entities/Shooter/Player/PlayerStatsRuntime.cs; Assets/Scripts/Gameplay/Interaction/Core/InteractionManager.cs; Assets/Scripts/Presentation/UI/Gameplay/MetaSystem/MetaStatsPanelUI.cs; Assets/Tests/EditMode/Editor/PlayerEmergencyMeleeTests.cs; Assets/Tests/EditMode/Editor/RaidRuntimeContractTests.cs

## Assets/Scripts/Gameplay/Entities/Shooter/Player/Effects/PlayerEffectController.cs
- 분류: 일반 소스; 31행
- 타입: PlayerEffectController : PlayerShooterEffectController
- 메서드: Start, EnableWalkParticles, DisableWalkParticles
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 1개: Assets/Prefabs/GameScene/Shooter/Player/Player.prefab
- 타입 이름을 참조하는 소스 1개: Assets/Scripts/Gameplay/Entities/Common/StateMachine/Actions/ControlWalkingParticlesActionSO.cs

## Assets/Scripts/Gameplay/Entities/Shooter/Player/Effects/PlayerShooterEffectController.cs
- 분류: 일반 소스; 10행
- 타입: PlayerShooterEffectController : ShooterEffectController
- 메서드: 
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 1개: Assets/Scripts/Gameplay/Entities/Shooter/Player/Effects/PlayerEffectController.cs

## Assets/Scripts/Gameplay/Entities/Shooter/Player/Inventory/PlayerBackpackDropInput.cs
- 분류: 일반 소스; 143행
- 타입: PlayerBackpackDropInput : MonoBehaviour
- 메서드: OnEnable, OnDisable, Update, TryDropEquippedBackpack, ResolveRefs, ResolveInputReader, RaiseMessage
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 1개: Assets/Prefabs/GameScene/Shooter/Player/Player.prefab
- 타입 이름을 참조하는 소스 0개: 

## Assets/Scripts/Gameplay/Entities/Shooter/Player/Inventory/PlayerConsumableUseRuntime.cs
- 분류: 일반 소스; 598행
- 타입: PlayerConsumableUseRuntime : MonoBehaviour
- 메서드: Reset, Awake, OnEnable, OnDisable, Update, TryBeginUse, TryBeginUseFromQuickSlot, UseRoutine, CompleteUse, CanContinueUse, HasExpectedStack, HasExpectedQuickSlotStack, HandleActionInput, HandleRightClickCancel, HandleDamaged, CancelActiveUse, FinishRoutine, ClearActiveState, PublishProgress, SubscribeInterruptions, UnsubscribeInterruptions, ResolveRefs, CanApplyAnyEffect, IsActionBlocked, ResolveNoApplicableEffectMessage, ResolveActor, ResolveItemName
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 1개: Assets/Prefabs/GameScene/Shooter/Player/Player.prefab
- 타입 이름을 참조하는 소스 5개: Assets/Scripts/Gameplay/Entities/Shooter/Player/Inventory/PlayerInventoryRuntime.Sorting.cs; Assets/Scripts/Gameplay/Entities/Shooter/Player/Inventory/PlayerQuickSlotRuntime.cs; Assets/Scripts/Gameplay/Entities/Shooter/Player/PlayerAimController.cs; Assets/Scripts/Gameplay/Entities/Shooter/Player/PlayerConditionAudio.cs; Assets/Scripts/Presentation/UI/Gameplay/PanelHub/Inventory/PlayerInventoryUI.cs

## Assets/Scripts/Gameplay/Entities/Shooter/Player/Inventory/PlayerInventoryRuntime.Backpack.cs
- 분류: 일반 소스; 258행
- 타입: PlayerInventoryRuntime
- 메서드: ResolveBackpackInventoryCapacity, ResolveLegacyInventoryCapacity, TryEquipBackpackFromInventory, TryApplyStartingLoadoutBackpack, TryDetachEquippedBackpack, TryEquipBackpackFromWorld, SetEquippedBackpack, ClearEquippedBackpackForRunLoss, CanFitCurrentInventoryInCapacity, CloneStack
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 43개: Assets/Scripts/Authoring/Editor/DebugMonitorWindow.cs; Assets/Scripts/Authoring/Editor/DebugMonitorWindow.Operations.cs; Assets/Scripts/Core/Events/ScriptableObject/PlayerInventoryRuntimeReadyEventChannelSO.cs; Assets/Scripts/Core/Persistence/PlayerRunStatePersistence.cs; Assets/Scripts/Core/Persistence/RunLifecycleCoordinator.cs; Assets/Scripts/Gameplay/Entities/Shooter/Common/Core/ShooterEquipmentController.cs; Assets/Scripts/Gameplay/Entities/Shooter/Enemy/Common/EnemyLootInventoryRuntime.cs; Assets/Scripts/Gameplay/Entities/Shooter/Player/Inventory/PlayerBackpackDropInput.cs

## Assets/Scripts/Gameplay/Entities/Shooter/Player/Inventory/PlayerInventoryRuntime.cs
- 분류: 일반 소스; 3362행
- 타입: PlayerInventoryItemStack, PlayerInventoryRemovedStack, PlayerInventoryEquipmentSlotKind, PlayerInventoryRuntime : InventoryRuntimeBase
- 메서드: Set, Clear, Add, Remove, GetEquippedWeaponCurrentDurability, GetEquippedWeaponMaxDurability, GetInventoryItemAmount, GetTotalOwnedItemAmount, TryFindInventoryItemSlot, TryConsumeInventoryItem, TryValidateInventoryItemRequirements, TryConsumeInventoryItemRequirements, TryRestoreInventoryItemRequirements, TryAggregateItemRequirements, Reset, Awake, OnEnable, Start, OnDisable, HandleEquipmentChanged, ApplyStartingLoadoutInventoryIfNeeded, TryPickup, TryPickupPassiveItem, TryPickupArmorLoot, TryPickupGasMaskLoot, TryPickupWeaponLoot, TryEquipInventoryItem, TryQuickEquipInventoryItem, TryEquipInventoryItemToSlot, TrySwapWeaponSlots, TryStoreExternalItemToInventorySlot, TryEquipExternalItemToSlot, TryMoveInventoryItem, TryExchangeInventorySlotWithExternalStack, TryFindFirstEmptyInventorySlot, TryDiscardInventoryItem, TryRemoveInventoryItem, TrySplitInventoryItem, TryUnequipToInventory, TryUnequipToInventorySlot, TryDiscardEquipped, TryRemoveEquipped, TryRepairInventoryArmorToFull, TryRepairEquippedArmorToFull, TryRepairInventoryWeaponToFull, TryRepairEquippedWeaponToFull, GetPassiveItems, ContainsPassive, ClearPassiveItems, ClearForRunLoss, TryPickupBullet, TryPickupWeapon, TryPickupArmor, TryEquipWeaponFromInventory, TryEquipExternalWeapon, TryEquipArmorFromInventory, TryEquipExternalArmor, TryEquipBulletFromInventory, TryEquipBulletFromInventoryToFirstAmmoSlot, TryStoreItem, TryRestoreRemovedStack, ReplaceOrRemoveInventorySlot, TryUnequipWeaponToInventorySlot, TryUnequipArmorToInventorySlot, TryPlaceItemIntoInventorySlot, TryFindStack, TryFindStackWithSpace, CanStack, IsSameItemIdentity, LogExternalLootStacking, ResolveMaxStack, CalculateStoreCapacity, IsValidIndex, IsSlotInCapacity, CountUsedSlots, NormalizeStorage, TryFindEmptySlotIndex, ResolveRefs, CanAcquireOwnedAmount, EquipRuntimeWeapon, ResolveAutomaticWeaponSlotIndex, TryAddWeaponToMatchingEquipmentStack, ClearEquippedWeapon, GetEquippedWeapon, GetEquippedWeaponAmount, TryConsumeEquippedWeapon, NotifyPickedUp, NotifyChanged, Reject, TraceBulletInventoryTransaction, Fail, ResolveItemName
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 1개: Assets/Prefabs/GameScene/Shooter/Player/Player.prefab
- 타입 이름을 참조하는 소스 56개: Assets/Scripts/Authoring/Editor/DebugMonitorWindow.cs; Assets/Scripts/Authoring/Editor/DebugMonitorWindow.Operations.cs; Assets/Scripts/Core/Events/ScriptableObject/PlayerInventoryRuntimeReadyEventChannelSO.cs; Assets/Scripts/Core/Persistence/PlayerRunStatePersistence.cs; Assets/Scripts/Core/Persistence/RunLifecycleCoordinator.cs; Assets/Scripts/Gameplay/Entities/Shooter/Common/Core/InventoryRuntimeBase.cs; Assets/Scripts/Gameplay/Entities/Shooter/Common/Core/ShooterEquipmentController.cs; Assets/Scripts/Gameplay/Entities/Shooter/Enemy/Common/EnemyLootInventoryRuntime.cs

## Assets/Scripts/Gameplay/Entities/Shooter/Player/Inventory/PlayerInventoryRuntime.GasMask.cs
- 분류: 일반 소스; 313행
- 타입: PlayerInventoryRuntime
- 메서드: TryEquipGasMaskFromInventory, TryEquipExternalGasMask, TryUnequipGasMaskToInventorySlot, TryRemoveEquippedGasMask, ResolveBiochemicalGasExposure, PreviewBiochemicalGasExposure, TryRepairInventoryGasMaskToFull, TryRepairEquippedGasMaskToFull, ClearEquippedGasMask
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 43개: Assets/Scripts/Authoring/Editor/DebugMonitorWindow.cs; Assets/Scripts/Authoring/Editor/DebugMonitorWindow.Operations.cs; Assets/Scripts/Core/Events/ScriptableObject/PlayerInventoryRuntimeReadyEventChannelSO.cs; Assets/Scripts/Core/Persistence/PlayerRunStatePersistence.cs; Assets/Scripts/Core/Persistence/RunLifecycleCoordinator.cs; Assets/Scripts/Gameplay/Entities/Shooter/Common/Core/ShooterEquipmentController.cs; Assets/Scripts/Gameplay/Entities/Shooter/Enemy/Common/EnemyLootInventoryRuntime.cs; Assets/Scripts/Gameplay/Entities/Shooter/Player/Inventory/PlayerBackpackDropInput.cs

## Assets/Scripts/Gameplay/Entities/Shooter/Player/Inventory/PlayerInventoryRuntime.KeyTool.cs
- 분류: 일반 소스; 430행
- 타입: PlayerKeyToolSlotState, PlayerKeyToolSlotSnapshot, PlayerKeyToolTierSnapshot, PlayerInventoryRuntime
- 메서드: Set, TryConsumeUse, GetKeyToolSlotSnapshot, GetKeyToolSlotSnapshots, GetKeyToolTierSnapshot, GetKeyToolTierSnapshots, HasUsableKey, TryAddKey, TryMoveInventoryKeyToKeyTool, TryWithdrawKeyToolSlot, TryConsumeKeyUse, TryPickupKeyTool, AbsorbCarriedInventoryKeys, NormalizeKeyToolStorage, ClearKeyToolStorage, GetStoredKeyCount, FindCarriedKeyTool, CreateKeyToolSlot, NotifyKeyToolChanged
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 43개: Assets/Scripts/Authoring/Editor/DebugMonitorWindow.cs; Assets/Scripts/Authoring/Editor/DebugMonitorWindow.Operations.cs; Assets/Scripts/Core/Events/ScriptableObject/PlayerInventoryRuntimeReadyEventChannelSO.cs; Assets/Scripts/Core/Persistence/PlayerRunStatePersistence.cs; Assets/Scripts/Core/Persistence/RunLifecycleCoordinator.cs; Assets/Scripts/Gameplay/Entities/Shooter/Common/Core/ShooterEquipmentController.cs; Assets/Scripts/Gameplay/Entities/Shooter/Enemy/Common/EnemyLootInventoryRuntime.cs; Assets/Scripts/Gameplay/Entities/Shooter/Player/Inventory/PlayerBackpackDropInput.cs

## Assets/Scripts/Gameplay/Entities/Shooter/Player/Inventory/PlayerInventoryRuntime.RefactorBoundary.cs
- 분류: 일반 소스; 62행
- 타입: PlayerInventoryRuntimeDiagnosticsSnapshot, PlayerInventoryRuntime
- 메서드: GetDiagnosticsSnapshot
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 43개: Assets/Scripts/Authoring/Editor/DebugMonitorWindow.cs; Assets/Scripts/Authoring/Editor/DebugMonitorWindow.Operations.cs; Assets/Scripts/Core/Events/ScriptableObject/PlayerInventoryRuntimeReadyEventChannelSO.cs; Assets/Scripts/Core/Persistence/PlayerRunStatePersistence.cs; Assets/Scripts/Core/Persistence/RunLifecycleCoordinator.cs; Assets/Scripts/Gameplay/Entities/Shooter/Common/Core/ShooterEquipmentController.cs; Assets/Scripts/Gameplay/Entities/Shooter/Enemy/Common/EnemyLootInventoryRuntime.cs; Assets/Scripts/Gameplay/Entities/Shooter/Player/Inventory/PlayerBackpackDropInput.cs

## Assets/Scripts/Gameplay/Entities/Shooter/Player/Inventory/PlayerInventoryRuntime.RunPersistence.cs
- 분류: 일반 소스; 357행
- 타입: PlayerInventoryRuntime
- 메서드: CaptureRunState, TryRestoreRunState, ValidateRunStateReferences, RestoreKeyToolSlots, ResetRunOwnershipForRestore
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 43개: Assets/Scripts/Authoring/Editor/DebugMonitorWindow.cs; Assets/Scripts/Authoring/Editor/DebugMonitorWindow.Operations.cs; Assets/Scripts/Core/Events/ScriptableObject/PlayerInventoryRuntimeReadyEventChannelSO.cs; Assets/Scripts/Core/Persistence/PlayerRunStatePersistence.cs; Assets/Scripts/Core/Persistence/RunLifecycleCoordinator.cs; Assets/Scripts/Gameplay/Entities/Shooter/Common/Core/ShooterEquipmentController.cs; Assets/Scripts/Gameplay/Entities/Shooter/Enemy/Common/EnemyLootInventoryRuntime.cs; Assets/Scripts/Gameplay/Entities/Shooter/Player/Inventory/PlayerBackpackDropInput.cs

## Assets/Scripts/Gameplay/Entities/Shooter/Player/Inventory/PlayerInventoryRuntime.Sorting.cs
- 분류: 일반 소스; 51행
- 타입: PlayerInventoryRuntime
- 메서드: SortInventory, SortStackRange, RarityRank
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 43개: Assets/Scripts/Authoring/Editor/DebugMonitorWindow.cs; Assets/Scripts/Authoring/Editor/DebugMonitorWindow.Operations.cs; Assets/Scripts/Core/Events/ScriptableObject/PlayerInventoryRuntimeReadyEventChannelSO.cs; Assets/Scripts/Core/Persistence/PlayerRunStatePersistence.cs; Assets/Scripts/Core/Persistence/RunLifecycleCoordinator.cs; Assets/Scripts/Gameplay/Entities/Shooter/Common/Core/ShooterEquipmentController.cs; Assets/Scripts/Gameplay/Entities/Shooter/Enemy/Common/EnemyLootInventoryRuntime.cs; Assets/Scripts/Gameplay/Entities/Shooter/Player/Inventory/PlayerBackpackDropInput.cs

## Assets/Scripts/Gameplay/Entities/Shooter/Player/Inventory/PlayerQuickSlotRuntime.cs
- 분류: 일반 소스; 1751행
- 타입: PlayerQuickSlotBindingKind, PlayerQuickSlotBinding, PlayerQuickSlotSnapshot, PlayerQuickSlotRuntime : MonoBehaviour
- 메서드: Set, SetUsable, Remove, Clear, Reset, Awake, OnEnable, OnDisable, Activate, DeactivateActiveUsable, TryUseActiveConsumable, TryConsumeUsable, TryGetSnapshot, GetMatchingUsableStackSpace, TryAddToMatchingUsableStacks, BindWeapon, BindAmmo, BindUsable, TryAssignInventoryItem, TryUnequipQuickSlot, SwapBindings, ClearBinding, ClearAllBindingsForRunLoss, RaiseMessage, SetBinding, SetBindingInternal, HandleOwnedDataChanged, ResolveWeaponTargetSlot, ResolveAmmoTargetSlot, EnsureEquippedItemsHaveBindings, FindAvailableBindingSlot, IsBindingDisplayEmpty, HasSourceBinding, ClearDuplicateSourceBindings, ClearBindingsForSource, PruneUnavailableUsableBindings, TryAutoBindUsables, HasUsableBinding, HasAnyStoredUsable, CreateStoredStack, ResolveItemName, IsSameUsableIdentity, ResolveActiveUsable, RefreshSelectionVisual, ClearHeldUsableView, Subscribe, Unsubscribe, HandleSelectedBulletChanged, HandleActiveWeaponChanged, ResolveRefs, NormalizeBindings, CreateDefaultBinding, IsValidSlot
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 1개: Assets/Prefabs/GameScene/Shooter/Player/Player.prefab
- 타입 이름을 참조하는 소스 11개: Assets/Scripts/Gameplay/Entities/Shooter/Common/Core/ShooterThrowableAttack.cs; Assets/Scripts/Gameplay/Entities/Shooter/Player/Inventory/PlayerConsumableUseRuntime.cs; Assets/Scripts/Gameplay/Entities/Shooter/Player/Inventory/PlayerInventoryRuntime.cs; Assets/Scripts/Gameplay/Entities/Shooter/Player/PlayerAimController.cs; Assets/Scripts/Gameplay/Entities/Shooter/Player/PlayerBulletLoadoutRuntime.cs; Assets/Scripts/Gameplay/Entities/Shooter/Player/PlayerClickController.cs; Assets/Scripts/Presentation/UI/Gameplay/PanelHub/Inventory/EquipmentInventoryAmmoSlotUI.cs; Assets/Scripts/Presentation/UI/Gameplay/PanelHub/Inventory/EquipmentInventoryWeaponSlotUI.cs

## Assets/Scripts/Gameplay/Entities/Shooter/Player/Passive/PlayerPassiveInventoryRuntime.cs
- 분류: 일반 소스; 570행
- 타입: PlayerPassiveInventoryRuntime : MonoBehaviour
- 메서드: Reset, Awake, OnEnable, OnDisable, IsSlotUnlocked, TryGetEquippedPassive, CaptureRunState, TryRestoreRunState, TryEquipFromInventory, TryEquipFirstAvailable, TryEquip, TryUnequip, TryUnlockNextSlot, SetUnlockedSlotCount, TryAdd, Clear, ResolveRefs, HandleInventoryChanged, HasAvailableOwnedCopy, GetOwnedCount, CountAssignedCopies, PruneSlotsWithoutOwnedCopies, EnsureSlotStorage, RebuildEquippedItems, StartChangeCooldown, GetCooldownMessage, ResolveItemName, Fail
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 1개: Assets/Prefabs/GameScene/Shooter/Player/Player.prefab
- 타입 이름을 참조하는 소스 4개: Assets/Scripts/Core/Persistence/PlayerRunStatePersistence.cs; Assets/Scripts/Core/Persistence/Save.cs; Assets/Scripts/Gameplay/Entities/Shooter/Player/PlayerStatsRuntime.cs; Assets/Scripts/Presentation/UI/Gameplay/PanelHub/Inventory/PlayerInventoryUI.cs

## Assets/Scripts/Gameplay/Entities/Shooter/Player/PlayerAimAction.cs
- 분류: 일반 소스; 628행
- 타입: PlayerAimAction : MonoBehaviour, ShootLane
- 메서드: Reset, Awake, OnEnable, OnDisable, Update, CanFireNowFor, ApplyDryFireCooldown, RequestReload, TryGetAimPoint, TryGetFlatAimDirection, ApplyAccuracyAndRecoil, ApplyShotRecoil, ResolveRefs, UpdateAimState, SetAiming, OnShotExecuted, UpdateReloadState, TryGetSelectedAimBullet, UpdateAimPoint, EnsureAimCamera, GetNextFireTime, SetNextFireTime, GetShotsPerSecond, GetCooldown01, ResolveLane, HandleBulletLoadoutReady, OnReloadRequested
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 3개: Assets/Scripts/Gameplay/Entities/Shooter/Player/PlayerAimController.cs; Assets/Scripts/Gameplay/Entities/Shooter/Player/VSplatterRange.cs; Assets/Scripts/WIP/Legacy/Gameplay/Player/VSplatterAimUI.cs

## Assets/Scripts/Gameplay/Entities/Shooter/Player/PlayerAimController.cs
- 분류: 일반 소스; 863행
- 타입: PlayerAimController : EntityRange, ShootLane
- 메서드: Reset, Awake, OnEnable, OnDisable, Update, CanFireNowFor, BlockAimUntilRightMouseReleased, ApplyDryFireCooldown, RequestReload, StartReload, TryGetAimPoint, TryGetRawAimPoint, TryGetFlatAimDirection, CancelReload, ApplyAccuracyAndRecoil, ApplyShotRecoil, ResolveRefs, CaptureHeldItemSelection, HandleEquipmentSelectionChanged, HandleQuickSlotSelectionChanged, UpdateAimState, SetAiming, OnShotExecuted, UpdateReloadState, PublishReloadProgress, TryGetSelectedAimBullet, UpdateAimPoint, EnsureAimCamera, GetNextFireTime, SetNextFireTime, GetShotsPerSecond, GetCooldown01, ResolveLane, HandleBulletLoadoutReady, BindBulletLoadoutEvents, HandleAmmoSelectionReloadRequested, OnReloadRequested
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 1개: Assets/Prefabs/GameScene/Shooter/Player/Player.prefab
- 타입 이름을 참조하는 소스 10개: Assets/Scripts/Gameplay/Entities/Common/StateMachine/Actions/CreatureRotateActionSO.cs; Assets/Scripts/Gameplay/Entities/Common/StateMachine/Actions/HorizontalMoveActionSO.cs; Assets/Scripts/Gameplay/Entities/Shooter/Common/StateMachine/Action/ShooterRotateActionSO.cs; Assets/Scripts/Gameplay/Entities/Shooter/Player/Inventory/PlayerConsumableUseRuntime.cs; Assets/Scripts/Gameplay/Entities/Shooter/Player/PlayerAimAction.cs; Assets/Scripts/Gameplay/Entities/Shooter/Player/PlayerAimCursorUI.cs; Assets/Scripts/Gameplay/Entities/Shooter/Player/PlayerClickController.cs; Assets/Scripts/Gameplay/Entities/Shooter/Player/PlayerShooterCharacterAudio.cs

## Assets/Scripts/Gameplay/Entities/Shooter/Player/PlayerAimCursorUI.cs
- 분류: 일반 소스; 334행
- 타입: PlayerAimCursorUI : MonoBehaviour
- 메서드: Reset, Awake, OnEnable, OnDisable, Update, EnsureCursorCanvasRoot, EnsureCursorUI, DisablePointerRaycasts, CacheCursorUIComponents, UpdateCursorUIPosition, RefreshCursorUI, SetCursorVisible, CacheHitCrosshair, HandleAttackHitConfirmed, UpdateHitCrosshair, HideHitCrosshairImmediate, FindChildRecursive
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 1개: Assets/Prefabs/GameScene/Shooter/Player/Player.prefab
- 타입 이름을 참조하는 소스 0개: 

## Assets/Scripts/Gameplay/Entities/Shooter/Player/PlayerBulletLoadoutRuntime.cs
- 분류: 일반 소스; 2763행
- 타입: CompatibleAmmoDisplayEntry, PlayerBulletLoadoutRuntime : MonoBehaviour, AmmoSlotState, WeaponMagazineState
- 메서드: Clear, GetMagazineSize, OnEnable, OnDisable, HandleStatsRuntimeReady, ApplyStartingLoadoutAmmoIfNeeded, SelectSlot, TryGetSelectedBullet, TryGetActiveWeaponMagazineState, TryGetSelectedAmmoType, HasLoadedAmmo, TryGetActiveBullet, TryGetActivePrimaryBullet, TryConsumePrimaryAmmo, TryConsumeAmmo, TrySwapSlots, TryGetSlotSnapshot, TryAddBulletToMatchingSlot, TryAddBulletToSlot, TryRemoveAmmoFromSlot, TryClearRemovableSlot, ClearForRunLoss, CaptureRunState, TryRestoreRunState, TryGrantBullet, TryAcquireTreasureBullet, TryAcquireBulletToAvailableSlot, TryEquipBulletToSlot, TryEquipBulletToSlotAndReturnPrevious, EquipBulletToSlot, AddAmmoToSlot, TryFindNonInfiniteSameBulletSlot, TryFindEmptyTreasureCompatibleSlot, TryFindTreasureReplaceSlot, CanReplaceSlotWithTreasureBullet, CanReloadSlot, ReloadSlot, GetActiveBullet, CycleCompatibleInventoryAmmo, TrySelectCompatibleInventoryAmmo, GetCompatibleInventoryAmmoEntries, FindDisplayEntryIndex, ResolveInventoryRefs, BindInventoryEvents, UnbindInventoryEvents, HandleInventoryChanged, HandleWeaponChanged, HandleEquipmentChanged, EnsureWeaponMagazineStates, GetActiveWeaponMagazine, EnsureCompatibleSelectionForActiveWeapon, TryReturnLoadedAmmoToInventory, TrySwitchLoadedAmmo, BuildAmmoSelectionMessage, TryStoreAmmoOrDrop, TryFindFirstCompatibleInventoryBullet, CollectCompatibleInventoryBullets, BuildRuntimeSlots, HandleSlotSelectRequested, HandleSlotSwapRequested, HandleShopPurchaseRequested, HandleSellConfirmRequested, ClampAllMagazineAmmo, TryFindEmptyCompatibleSlot, CanMoveSlotTo, CanEquipBulletInSlot, GetActiveSlotIndex, SetActiveSlot, EnsureActiveSlotsValid, IsActiveSlotValid, FindFirstSlotOfType, FindFirstOccupiedSlot, IsSlotSelected, SwapActiveIndex, PublishSnapshot, RaisePurchaseResult, RaiseSellResult, IsRuntimeReady, IsValidSlot, IsCompatibleAmmoType, CanReloadActiveAmmo, ReloadActiveAmmo, TryGetActiveSpecialBullet, TryConsumeSpecialAmmo, TraceAmmoTransaction, BuildAmmoTraceState, AppendAmmoTraceBulletState, AppendAmmoTraceQuickSlots, ResolveAmmoTraceCaller, ResolveAmmoTraceBulletName
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 1개: Assets/Prefabs/GameScene/Shooter/Player/Player.prefab
- 타입 이름을 참조하는 소스 15개: Assets/Scripts/Core/Events/ScriptableObject/PlayerBulletLoadoutRuntimeReadyEventChannelSO.cs; Assets/Scripts/Core/Events/ScriptableObject/WeaponAmmoSlotSwapRequestEventChannelSO.cs; Assets/Scripts/Core/Persistence/PlayerRunStatePersistence.cs; Assets/Scripts/Gameplay/Entities/Shooter/Common/Core/ShooterMeleeAttack.cs; Assets/Scripts/Gameplay/Entities/Shooter/Player/Inventory/PlayerInventoryRuntime.cs; Assets/Scripts/Gameplay/Entities/Shooter/Player/Inventory/PlayerQuickSlotRuntime.cs; Assets/Scripts/Gameplay/Entities/Shooter/Player/PlayerAimAction.cs; Assets/Scripts/Gameplay/Entities/Shooter/Player/PlayerAimController.cs

## Assets/Scripts/Gameplay/Entities/Shooter/Player/PlayerCharacter.cs
- 분류: 일반 소스; 144행
- 타입: PlayerCharacter : MonoBehaviour
- 메서드: Awake, OnControllerColliderHit, OnEnable, OnDisable, Update, RecalculateMovement, OnMove, OnDashInitiated, OnDashCanceled, OnShootStarted, OnShootCanceled
- 직렬화 참조 2개 / 지정 씬 참조 그래프 내 1개: Assets/Prefabs/GameScene/Shooter/Player/Player.prefab; Assets/Scenes/BattleTestScene.unity
- 타입 이름을 참조하는 소스 39개: Assets/Scripts/Authoring/Editor/DebugMonitorWindow.cs; Assets/Scripts/Authoring/Editor/DebugMonitorWindow.Operations.cs; Assets/Scripts/Core/SceneFlow/SceneLoader.cs; Assets/Scripts/Gameplay/Entities/Common/Core/EntityComponentResolver.cs; Assets/Scripts/Gameplay/Entities/Common/StateMachine/Actions/AnimatorParameterActionSO.cs; Assets/Scripts/Gameplay/Entities/Common/StateMachine/Actions/ApplyMovementVectorActionSO.cs; Assets/Scripts/Gameplay/Entities/Common/StateMachine/Actions/AscendActionSO.cs; Assets/Scripts/Gameplay/Entities/Common/StateMachine/Actions/ClearInputCache_OnEnterSO.cs

## Assets/Scripts/Gameplay/Entities/Shooter/Player/PlayerClickController.cs
- 분류: 일반 소스; 502행
- 타입: PlayerClickController : MonoBehaviour
- 메서드: Reset, Awake, OnEnable, OnDisable, LateUpdate, HandleShootStarted, TryFireSelectedRanged, HandleShootCanceled, TryReleaseThrowable, HandleRightClick, TickThrowableAimPreview, CancelThrowableAim, CanUseEmergencyMelee, CanUseThrowable, DrawThrowablePreview, SetThrowablePreviewVisible, TickRangeControlZonePreview, SetRangeControlZonePreviewVisible, ResolveRefs, HandleMeleeAttackInput, TryStartMeleeAttack, TryGetSelectedShootAmmoType, CanShoot
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 1개: Assets/Prefabs/GameScene/Shooter/Player/Player.prefab
- 타입 이름을 참조하는 소스 0개: 

## Assets/Scripts/Gameplay/Entities/Shooter/Player/PlayerConditionAudio.cs
- 분류: 일반 소스; 104행
- 타입: PlayerConditionAudio : MonoBehaviour
- 메서드: Awake, OnEnable, OnDisable, Update, HandleConsumableUseStarted, HandleConsumableUseCompleted, PlayOneShot, ResolveRefs
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 1개: Assets/Prefabs/GameScene/Shooter/Player/Player.prefab
- 타입 이름을 참조하는 소스 0개: 

## Assets/Scripts/Gameplay/Entities/Shooter/Player/PlayerEmergencyMeleeProfile.cs
- 분류: 일반 소스; 32행
- 타입: PlayerEmergencyMeleeProfile : IMeleeAttackProfile
- 메서드: IsAvailable, ResolveDamageTargetMask
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 3개: Assets/Scripts/Gameplay/Entities/Shooter/Common/Core/ShooterMeleeAttack.cs; Assets/Scripts/Gameplay/Entities/Shooter/Player/PlayerClickController.cs; Assets/Tests/EditMode/Editor/PlayerEmergencyMeleeTests.cs

## Assets/Scripts/Gameplay/Entities/Shooter/Player/PlayerShooterCharacterAudio.cs
- 분류: 일반 소스; 335행
- 타입: PlayerShooterCharacterAudio : EntityShooterCharacterAudio
- 메서드: ResolveRefs, SubscribeAdditionalEvents, UnsubscribeAdditionalEvents, Update, PlayDash, PlayHitConfirm, PlayAim, PlayPickup, PlayGenericInteract, PlayPortalInteract, PlayCorpseInteract, PlayTreasurePickup, PlayKeyInsert, PlayKeySuccess, PlayKeyRejected, HandleObjectAudioRequested, ResolvePlayerWorldActionCue, HandleInventoryRuntimeReady, HandlePlayerHealthChanged, TickFootsteps
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 1개: Assets/Prefabs/GameScene/Shooter/Player/Player.prefab
- 타입 이름을 참조하는 소스 4개: Assets/Scripts/Gameplay/Items/Loot/LootableObject.cs; Assets/Scripts/Presentation/Audio/AudioData/ShooterAudioProfileSO.cs; Assets/Scripts/Presentation/Audio/WorldGameAudio.cs; Assets/Scripts/Presentation/UI/Gameplay/Loot/LockedLootKeyPanel.cs

## Assets/Scripts/Gameplay/Entities/Shooter/Player/PlayerShooterDashController.cs
- 분류: 일반 소스; 94행
- 타입: PlayerShooterDashController : ShooterDashController
- 메서드: Reset, Awake, OnDisable, Update, TryStartDash, ResolveRefs, TickPlayerDashInput
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 1개: Assets/Scripts/Gameplay/Entities/Shooter/Player/VSplatterDashController.cs

## Assets/Scripts/Gameplay/Entities/Shooter/Player/PlayerShooterDeathCorpseController.cs
- 분류: 일반 소스; 71행
- 타입: PlayerShooterDeathCorpseController : ShooterDeathCorpseController
- 메서드: AfterConvertToCorpse, ClearRunBoundPossessions
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 1개: Assets/Prefabs/GameScene/Shooter/Player/Player.prefab
- 타입 이름을 참조하는 소스 0개: 

## Assets/Scripts/Gameplay/Entities/Shooter/Player/PlayerShooterInfection.cs
- 분류: 일반 소스; 144행
- 타입: PlayerShooterInfection : ShooterInfection
- 메서드: OnEnable, OnDisable, Update, PublishCurrentSnapshot, RecoverOnSectorCaptured, RecoverOnNamedKilled, RecoverOnBossKilled
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 1개: Assets/Prefabs/GameScene/Shooter/Player/Player.prefab
- 타입 이름을 참조하는 소스 12개: Assets/Scripts/Core/Events/ScriptableObject/PlayerInfectionEventChannelSO.cs; Assets/Scripts/Core/Persistence/PlayerRunStatePersistence.cs; Assets/Scripts/Gameplay/Combat/Infection/InfectionControlManager.cs; Assets/Scripts/Gameplay/Entities/Creature/CommonEnemyStateMachine/AttackExecutor/EnemySelfDestructAttackExecutor.cs; Assets/Scripts/Gameplay/Entities/Creature/EnemyArcBombProjectile.cs; Assets/Scripts/Gameplay/Entities/Creature/EnemyContactDamage.cs; Assets/Scripts/Gameplay/Entities/Creature/EnemyProjectile.cs; Assets/Scripts/Gameplay/Entities/Creature/NormalEnemy/Chaser/StateMachine/Action/ChaserVirusSelfDestructActionSO.cs

## Assets/Scripts/Gameplay/Entities/Shooter/Player/PlayerShooterShoot.cs
- 분류: 일반 소스; 644행
- 타입: PlayerShooterShoot : EntityShootCore
- 메서드: Reset, Awake, OnEnable, OnDisable, LateUpdate, TryFireOnce, CanAttemptFire, PlayEmptyShoot, TryFirePrimary, TryFireSpecial, ShowIncompatibleAmmoFeedback, ShowMissingAmmoFeedback, ShowBrokenWeaponFeedback, ShowInvalidActionIcon, EnsureInvalidActionIcon, UpdateInvalidActionIconTransform, FindDescendant, BuildPrimaryStats, HandleMaskRenderManagerReady, ResolveRefs, EnsureFootstepSoundEmitter, PublishGunshotSound, ApplyAimError
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 1개: Assets/Prefabs/GameScene/Shooter/Player/Player.prefab
- 타입 이름을 참조하는 소스 4개: Assets/Scripts/Gameplay/Entities/Shooter/Common/Core/ShooterThrowableAttack.cs; Assets/Scripts/Gameplay/Entities/Shooter/Player/PlayerAimAction.cs; Assets/Scripts/Gameplay/Entities/Shooter/Player/PlayerAimController.cs; Assets/Scripts/Gameplay/Entities/Shooter/Player/PlayerClickController.cs

## Assets/Scripts/Gameplay/Entities/Shooter/Player/PlayerShooterVisionController.cs
- 분류: 일반 소스; 56행
- 타입: PlayerShooterVisionController : EntityVisionController
- 메서드: Reset, Awake, ResolveRefs, ResolveAim01, ResolveForwardDirection
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 1개: Assets/Prefabs/GameScene/Shooter/Player/Player.prefab
- 타입 이름을 참조하는 소스 0개: 

## Assets/Scripts/Gameplay/Entities/Shooter/Player/PlayerStatsRuntime.cs
- 분류: 일반 소스; 830행
- 타입: PlayerStatsRuntime : ShooterStatsRuntime, StatAccumulator
- 메서드: GetPersistentQuestFlatAdd, GetPersistentQuestPercentAdd, Reset, Awake, OnEnable, OnDisable, ResolveRefs, OnBackpackChanged, RebuildAndPublish, ApplyHealthStats, Rebuild, ApplyQuestRewardEffects, GetPersistentQuestModifierTotal, ApplyPassiveItemEffects, ApplyEquipmentEffects, ApplyModifiers, ApplyModifier, ApplyFeatureFlags, Resolve, OnWeaponChanged, OnSelectedBulletChanged, ResolveAttackDamage, ResolvePaintMarkDamage, ResolveInfectionDamage, ResolveShotsPerSecond, ResolveMagazineSize, ResolveArmorHealthDurabilityLossMultiplier, ResolveArmorInfectionDurabilityLossMultiplier, ResolveWithExtraModifiers, TryApplyEnemyBulletInjuries, ApplyConsumableStatusEffect, TryApplyEnemyBulletInjury, ApplyStatusSpread, Apply
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 1개: Assets/Prefabs/GameScene/Shooter/Player/Player.prefab
- 타입 이름을 참조하는 소스 23개: Assets/Scripts/Core/Events/ScriptableObject/PlayerStatsRuntimeReadyEventChannelSO.cs; Assets/Scripts/Gameplay/Entities/Common/StateMachine/Actions/HorizontalMoveActionSO.cs; Assets/Scripts/Gameplay/Entities/Shooter/Common/Core/ShooterMeleeAttack.cs; Assets/Scripts/Gameplay/Entities/Shooter/Common/Core/ShooterStatsRuntime.cs; Assets/Scripts/Gameplay/Entities/Shooter/Common/Vision/EntityVisionController.cs; Assets/Scripts/Gameplay/Entities/Shooter/Common/Vision/VisionVisibilitySystem.cs; Assets/Scripts/Gameplay/Entities/Shooter/Enemy/Common/EnemyShooterStatsRuntime.cs; Assets/Scripts/Gameplay/Entities/Shooter/Enemy/StateMachine/EnemyShooterCombatController.cs

## Assets/Scripts/Gameplay/Entities/Shooter/Player/PlayerWorldSoundIndicator.cs
- 분류: 일반 소스; 653행
- 타입: PlayerWorldSoundIndicator : MonoBehaviour, IndicatorInstance
- 메서드: Reset, Awake, OnEnable, OnDisable, LateUpdate, ResolveRefs, DisableLegacyRangeLine, OnSoundStimulus, UpdateIndicatorInstances, UpdateIndicatorTransform, ShouldShow, AreSoundIndicatorsSuppressed, IsSourceVisible, IsOwnSource, ResolveTrackedSource, ResolveSourceKey, CreateIndicatorInstance, BringIndicatorToFront, RebaseIndicatorSortingOrders, SetIndicatorVisible, SetIndicatorColor, RemoveIndicator, ClearIndicators, SetTemplateVisible, FindChildByName
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 1개: Assets/Prefabs/GameScene/Shooter/Player/Player.prefab
- 타입 이름을 참조하는 소스 0개: 

## Assets/Scripts/Gameplay/Entities/Shooter/Player/Upgrade/PlayerStatsSnapshot.cs
- 분류: 일반 소스; 106행
- 타입: PlayerWeaponStats, PlayerPaintStats, PlayerMovementStats, PlayerSurvivalStats, PlayerVisionStats, PlayerAimStats, PlayerSoundStats, PlayerFeatureFlags, PlayerStatsSnapshot
- 메서드: 
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 4개: Assets/Scripts/Core/Events/ScriptableObject/PanelHub/PlayerStatsChangedEventChannelSO.cs; Assets/Scripts/Gameplay/Entities/Shooter/Player/PlayerStatsRuntime.cs; Assets/Scripts/Presentation/UI/Gameplay/PanelHub/PlayerStatsSummaryPanelUI.cs; Assets/Scripts/Presentation/UI/Gameplay/Slot/SlotHUD.cs

## Assets/Scripts/Gameplay/Entities/Shooter/Player/VSplatterAimUtility.cs
- 분류: 일반 소스; 172행
- 타입: VSplatterAimUtility
- 메서드: TryGetAimPoint, TryGetAimPointOnPlane, IsWithinFlatRange, ClampFlatPointToRange, TryGetPointOnYPlane, TryGetFlatCircleExitDistance
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 7개: Assets/Scripts/Gameplay/Entities/Common/StateMachine/Actions/CreatureRotateActionSO.cs; Assets/Scripts/Gameplay/Entities/Shooter/Common/Core/EntityRange.cs; Assets/Scripts/Gameplay/Entities/Shooter/Common/Core/EntityShootCore.cs; Assets/Scripts/Gameplay/Entities/Shooter/Common/StateMachine/Action/ShooterRotateActionSO.cs; Assets/Scripts/Gameplay/Entities/Shooter/Player/PlayerAimAction.cs; Assets/Scripts/Gameplay/Entities/Shooter/Player/PlayerAimController.cs; Assets/Scripts/Gameplay/Entities/Shooter/Player/VSplatterRange.cs

## Assets/Scripts/Gameplay/Entities/Shooter/Player/VSplatterDashController.cs
- 분류: 일반 소스; 14행
- 타입: VSplatterDashController : PlayerShooterDashController
- 메서드: 
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 1개: Assets/Prefabs/GameScene/Shooter/Player/Player.prefab
- 타입 이름을 참조하는 소스 0개: 

## Assets/Scripts/Gameplay/Entities/Shooter/Player/VSplatterRange.cs
- 분류: 일반 소스; 393행
- 타입: VSplatterRange : EntityRange
- 메서드: Reset, Awake, OnEnable, OnDisable, LateUpdate, ResolveRefs, HasValidWeapon, IsWithinRange, ClampToRange, IsShowingRangeNow, EnsureLineRenderer, RefreshVisualImmediate, LogRangeDebug, TryDrawCameraProjectedRange, DrawWorldFlatRange, OnWorldCameraReady, OnShootStarted, OnShootCanceled
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 1개: Assets/Scripts/Gameplay/Entities/Shooter/Player/PlayerAimAction.cs

## Assets/Scripts/Gameplay/GameManager.cs
- 분류: 일반 소스; 37행
- 타입: GameManager : MonoBehaviour
- 메서드: Start, OnEnable, OnDisable
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 1개: Assets/Scenes/Manager/GamePlay.unity
- 타입 이름을 참조하는 소스 0개: 

## Assets/Scripts/Gameplay/GameStateSO.cs
- 분류: 일반 소스; 86행
- 타입: GameState, GameStateSO : DescriptionBaseSO
- 메서드: Start, AddAlertEnemy, RemoveAlertEnemy, UpdateGameState, ResetToPreviousGameState
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 1개: Assets/ScriptableObject/GamePlay/GameState.asset
- 타입 이름을 참조하는 소스 10개: Assets/Scripts/Core/Input/InputReader.cs; Assets/Scripts/Gameplay/Entities/Common/Core/CharacterAudio.cs; Assets/Scripts/Gameplay/Entities/Common/StateMachine/Actions/ChangeGameStateActionSO.cs; Assets/Scripts/Gameplay/Entities/Common/StateMachine/Conditions/IsInSpecificGameStateSO.cs; Assets/Scripts/Gameplay/GameManager.cs; Assets/Scripts/Presentation/Audio/AudioData/BulletImpactAudioCatalogSO.cs; Assets/Scripts/Presentation/Audio/BulletImpactAudio.cs; Assets/Scripts/Presentation/Audio/ObstacleAudio.cs

## Assets/Scripts/Gameplay/Interaction/Core/Interaction.cs
- 분류: 일반 소스; 20행
- 타입: Interaction
- 메서드: 
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 21개: Assets/Scripts/Authoring/Editor/ContentTemplateInspectors.cs; Assets/Scripts/Core/Events/ScriptableObject/InteractionCastBarEventChannelSO.cs; Assets/Scripts/Core/Events/ScriptableObject/InteractionPromptEventChannelSO.cs; Assets/Scripts/Core/Events/ScriptableObject/InteractionUIEventChannelSO.cs; Assets/Scripts/Gameplay/Entities/Shooter/Common/Core/ShooterDeathCorpseController.cs; Assets/Scripts/Gameplay/Entities/Shooter/Enemy/Common/EnemyLootInventoryRuntime.cs; Assets/Scripts/Gameplay/Entities/Shooter/Player/Definitions/PlayerStatsConfigSO.cs; Assets/Scripts/Gameplay/Interaction/Core/InteractionManager.cs

## Assets/Scripts/Gameplay/Interaction/Core/InteractionManager.cs
- 분류: 일반 소스; 1194행
- 타입: InteractionType, InteractionManager : MonoBehaviour
- 메서드: OnEnable, OnDisable, Collect, OnInteractionButtonPress, ExecuteInteraction, CastAndExecuteInteraction, TryUsePortal, TryUseQuestNpc, TryUseHideoutStageSelectStation, TryUseSectorSkipStation, TryUsePickup, TryUseShop, TryUseLoot, TryUseTreasure, TryUseQTEStation, OnTriggerChangeDetected, AddPotentialInteraction, RemovePotentialInteraction, TryCreateInteraction, ResolveInteractableObject, ContainsInteractionObject, RequestUpdateUI, OnInteractionEnd, ResetPotentialInteractions, PublishInteractionPrompt, ResolvePromptAnchor, ResolvePromptAnchorComponent, SetActivePromptAnchor, ResolveCastBarAnchor, ResolveInteractionCastSeconds, ResolvePlayerInteractConfig, IsInteractionStillValid, PublishInteractionCast, CancelInteractionCast, ClearInteractionCastState, ResolveInteractionKeyLabel, ResolveActionLabel, RefreshPotentialInteractions, ResolvePortalInteractCooldown, LogResolvedInteraction, BuildHierarchyPath
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 1개: Assets/Prefabs/GameScene/Shooter/Interaction.prefab
- 타입 이름을 참조하는 소스 12개: Assets/Scripts/Core/Events/ScriptableObject/InteractionCastBarEventChannelSO.cs; Assets/Scripts/Core/Events/ScriptableObject/InteractionPromptEventChannelSO.cs; Assets/Scripts/Core/Events/ScriptableObject/InteractionUIEventChannelSO.cs; Assets/Scripts/Gameplay/Entities/Common/StateMachine/Actions/ClearInputCache_OnEnterSO.cs; Assets/Scripts/Gameplay/Entities/Common/StateMachine/Conditions/IsPickingUpSO.cs; Assets/Scripts/Gameplay/Entities/Shooter/Player/Inventory/PlayerConsumableUseRuntime.cs; Assets/Scripts/Gameplay/Entities/Shooter/Player/PlayerAimController.cs; Assets/Scripts/Gameplay/Interaction/Core/Interaction.cs

## Assets/Scripts/Gameplay/Interaction/Core/PlayerInteractConfigSO.cs
- 분류: 일반 소스; 50행
- 타입: InteractionCastTimeRule, PlayerInteractConfigSO : ScriptableObject
- 메서드: ResolveCastSeconds
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 1개: Assets/ScriptableObject/GameScene/Character/Shooter/Player/Config/PlayerInteractConfig.asset
- 타입 이름을 참조하는 소스 2개: Assets/Scripts/Gameplay/Entities/Shooter/Player/Definitions/PlayerStatsConfigSO.cs; Assets/Scripts/Gameplay/Interaction/Core/InteractionManager.cs

## Assets/Scripts/Gameplay/Interaction/QTE/QTEBase.cs
- 분류: 일반 소스; 88행
- 타입: QTEType, QTEResult, QTEBase : MonoBehaviour
- 메서드: Begin, Cancel, BeginInternal, ShouldIgnoreInputThisFrame, IsKeyboardCancelPressed, Finish
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 4개: Assets/Scripts/Gameplay/Entities/Creature/NamedEnemy/Mutarus/MutarusQTEPatternController.cs; Assets/Scripts/Gameplay/Interaction/QTE/QTE_Sub/QTE_Gauge.cs; Assets/Scripts/Gameplay/Interaction/QTE/QTE_Sub/QTE_KeyPrompt.cs; Assets/Scripts/Gameplay/Interaction/QTE/QTE_Sub/QTE_Timing.cs

## Assets/Scripts/Gameplay/Interaction/QTE/QTE_Sub/QTE_Gauge.cs
- 분류: 일반 소스; 183행
- 타입: QTE_Gauge : QTEBase
- 메서드: Begin, Update, TickDecay, HandleInput, HandleCorrectPress, HandleWrongPress, CheckResult, PickNewKey, SetNextKeyChangeThreshold, RefreshUI
- 직렬화 참조 2개 / 지정 씬 참조 그래프 내 2개: Assets/Prefabs/GameScene/PatternUI/QTE/QTEGaugePanel.prefab; Assets/Prefabs/GameScene/PatternUI/QTE/QTEPatternRoot.prefab
- 타입 이름을 참조하는 소스 0개: 

## Assets/Scripts/Gameplay/Interaction/QTE/QTE_Sub/QTE_KeyPrompt.cs
- 분류: 일반 소스; 147행
- 타입: QTE_KeyPrompt : QTEBase
- 메서드: Begin, Update, HandleKeyPressed, BuildSequence, BuildUI, ClearItems, OnDisable
- 직렬화 참조 2개 / 지정 씬 참조 그래프 내 2개: Assets/Prefabs/GameScene/PatternUI/QTE/QTEKeyPromptPanel.prefab; Assets/Prefabs/GameScene/PatternUI/QTE/QTEPatternRoot.prefab
- 타입 이름을 참조하는 소스 0개: 

## Assets/Scripts/Gameplay/Interaction/QTE/QTE_Sub/QTE_Timing.cs
- 분류: 일반 소스; 310행
- 타입: QTE_Timing : QTEBase
- 메서드: Begin, BeginForStation, Cancel, Update, TickMarker, RefreshMarkerPosition, EvaluateAndFinish, RegisterSuccess, IsMarkerInsideSuccessZone, RandomizeSuccessZonePosition, RefreshProgressText, GetLocalXRange, GetRectHalfWidth
- 직렬화 참조 3개 / 지정 씬 참조 그래프 내 3개: Assets/Prefabs/GameScene/PatternUI/Canvas_Pattern.prefab; Assets/Prefabs/GameScene/PatternUI/QTE/QTEPatternRoot.prefab; Assets/Prefabs/GameScene/PatternUI/QTE/QTETimingPanel.prefab
- 타입 이름을 참조하는 소스 1개: Assets/Scripts/Gameplay/Entities/Creature/NamedEnemy/Mutarus/MutarusQTEPatternController.cs

## Assets/Scripts/Gameplay/Items/Definitions/BackpackItemSO.cs
- 분류: 일반 소스; 33행
- 타입: BackpackItemSO : ItemSO
- 메서드: 
- 직렬화 참조 6개 / 지정 씬 참조 그래프 내 0개: Assets/ScriptableObject/GameScene/ContentTemplates/Items/Backpack_Template.asset; Assets/ScriptableObject/GameScene/GameItem/Backpack/BackPack_LV1.asset; Assets/ScriptableObject/GameScene/GameItem/Backpack/BackPack_LV2.asset; Assets/ScriptableObject/GameScene/GameItem/Backpack/BackPack_LV3.asset; Assets/ScriptableObject/GameScene/GameItem/Backpack/BackPack_LV4.asset; Assets/ScriptableObject/GameScene/GameItem/Backpack/BackPack_LV5.asset
- 타입 이름을 참조하는 소스 10개: Assets/Editor/ItemMakerWindow.cs; Assets/Scripts/Authoring/Editor/ContentTemplateInspectors.cs; Assets/Scripts/Core/Persistence/PlayerRunStatePersistence.cs; Assets/Scripts/Gameplay/Entities/Shooter/Player/Inventory/PlayerBackpackDropInput.cs; Assets/Scripts/Gameplay/Entities/Shooter/Player/Inventory/PlayerInventoryRuntime.Backpack.cs; Assets/Scripts/Gameplay/Entities/Shooter/Player/Inventory/PlayerInventoryRuntime.cs; Assets/Scripts/Gameplay/Entities/Shooter/Player/Inventory/PlayerInventoryRuntime.RunPersistence.cs; Assets/Scripts/Gameplay/Entities/Shooter/Player/PlayerStatsRuntime.cs

## Assets/Scripts/Gameplay/Items/Definitions/ConsumableItemSO.cs
- 분류: 일반 소스; 43행
- 타입: ConsumableItemSO : UsableItemSO
- 메서드: 
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 0개: Assets/ScriptableObject/GameScene/ContentTemplates/Items/Consumable_Template.asset
- 타입 이름을 참조하는 소스 9개: Assets/Editor/ItemMakerWindow.cs; Assets/Scripts/Gameplay/Entities/Shooter/Player/Inventory/PlayerConsumableUseRuntime.cs; Assets/Scripts/Gameplay/Entities/Shooter/Player/Inventory/PlayerQuickSlotRuntime.cs; Assets/Scripts/Gameplay/Entities/Shooter/Player/PlayerConditionAudio.cs; Assets/Scripts/Gameplay/Entities/Shooter/Player/PlayerStatsRuntime.cs; Assets/Scripts/Gameplay/Items/Definitions/MedicalItemSO.cs; Assets/Scripts/Presentation/UI/Gameplay/PanelHub/Inventory/InventoryItemContextMenuUI.cs; Assets/Scripts/Presentation/UI/Gameplay/PanelHub/Inventory/PlayerInventoryUI.cs

## Assets/Scripts/Gameplay/Items/Definitions/CurrencyItemSO.cs
- 분류: 일반 소스; 25행
- 타입: CurrencyItemSO : ValuableItemSO
- 메서드: 
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 1개: Assets/ScriptableObject/GameScene/GameItem/Currency/Money.asset
- 타입 이름을 참조하는 소스 11개: Assets/Editor/ItemMakerWindow.cs; Assets/Scripts/Core/Persistence/Save.cs; Assets/Scripts/Gameplay/Entities/Common/Core/EnemyCommonSettings.cs; Assets/Scripts/Gameplay/Entities/Shooter/Common/Core/ShooterLoadoutSO.cs; Assets/Scripts/Gameplay/Items/World/WorldItemPickup.cs; Assets/Scripts/Gameplay/World/Sectors/Shop/SectorShop.cs; Assets/Scripts/Gameplay/World/Sectors/Shop/ShopConfigSO.cs; Assets/Scripts/Gameplay/World/Sectors/Treasure/TreasureRoomDropTableSO.cs

## Assets/Scripts/Gameplay/Items/Definitions/GasMaskItemSO.cs
- 분류: 일반 소스; 120행
- 타입: GasMaskTier, GasMaskItemSO : ItemSO
- 메서드: ApplyEquippedVisualTransform, SetEquippedVisualPrefab, SetVisualOccupancy, SetEquippedVisualTransform
- 직렬화 참조 5개 / 지정 씬 참조 그래프 내 4개: Assets/ScriptableObject/GameScene/ContentTemplates/Items/GasMask_Template.asset; Assets/ScriptableObject/GameScene/GameItem/GasMask/GasMaskLV1.asset; Assets/ScriptableObject/GameScene/GameItem/GasMask/GasMaskLV2.asset; Assets/ScriptableObject/GameScene/GameItem/GasMask/GasMaskLV3.asset; Assets/ScriptableObject/GameScene/GameItem/GasMask/GasMaskLV4.asset
- 타입 이름을 참조하는 소스 12개: Assets/Editor/ItemMakerWindow.cs; Assets/Editor/ShooterCustomizeStationWindow.cs; Assets/Scripts/Authoring/Editor/ContentTemplateInspectors.cs; Assets/Scripts/Core/Persistence/PlayerRunStatePersistence.cs; Assets/Scripts/Gameplay/Entities/Shooter/Common/Core/ShooterEquipmentController.cs; Assets/Scripts/Gameplay/Entities/Shooter/Player/Inventory/PlayerInventoryRuntime.cs; Assets/Scripts/Gameplay/Entities/Shooter/Player/Inventory/PlayerInventoryRuntime.GasMask.cs; Assets/Scripts/Gameplay/Entities/Shooter/Player/Inventory/PlayerInventoryRuntime.RunPersistence.cs

## Assets/Scripts/Gameplay/Items/Definitions/InfectionControlRecoveryItemSO.cs
- 분류: 일반 소스; 33행
- 타입: InfectionControlRecoveryItemSO : ItemSO
- 메서드: ResolvePurchasePrice
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 0개: 

## Assets/Scripts/Gameplay/Items/Definitions/InfectionCureItemSO.cs
- 분류: 일반 소스; 27행
- 타입: InfectionCureItemSO : MedicalItemSO
- 메서드: 
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 1개: Assets/ScriptableObject/GameScene/GameItem/Consumable/Medical/InfectionCure.asset
- 타입 이름을 참조하는 소스 4개: Assets/Scripts/Gameplay/Entities/Shooter/Player/Inventory/PlayerConsumableUseRuntime.cs; Assets/Scripts/Gameplay/Entities/Shooter/Player/PlayerConditionAudio.cs; Assets/Scripts/Gameplay/Entities/Shooter/Player/PlayerShooterInfection.cs; Assets/Tests/EditMode/Editor/ItemHandAndCategoryTests.cs

## Assets/Scripts/Gameplay/Items/Definitions/ItemSO.cs
- 분류: 일반 소스; 72행
- 타입: ItemKind, ItemSO : SerializableScriptableObject
- 메서드: 
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 0개: Assets/ScriptableObject/GameScene/ContentTemplates/Items/Item_Misc_Template.asset
- 타입 이름을 참조하는 소스 76개: Assets/Editor/ItemCategoryMoveUtility.cs; Assets/Editor/ItemMakerSelection.cs; Assets/Editor/ItemMakerWindow.cs; Assets/Editor/LootProgressionEditorWindow.cs; Assets/Editor/QuestDesignTreeWindow.cs; Assets/Editor/ShooterCustomizeStationWindow.cs; Assets/Editor/ShopProgressionTreeWindow.cs; Assets/Scripts/Authoring/Editor/ContentPreflightWindow.cs

## Assets/Scripts/Gameplay/Items/Definitions/KeyItemSO.cs
- 분류: 일반 소스; 51행
- 타입: LootKeyTier, KeyItemSO : ItemSO
- 메서드: 
- 직렬화 참조 5개 / 지정 씬 참조 그래프 내 2개: Assets/ScriptableObject/GameScene/GameItem/Key/Key.asset; Assets/ScriptableObject/GameScene/GameItem/Key/Key_Legendary.asset; Assets/ScriptableObject/GameScene/GameItem/Key/Key_Normal.asset; Assets/ScriptableObject/GameScene/GameItem/Key/Key_Rare.asset; Assets/ScriptableObject/GameScene/GameItem/Key/Key_Unique.asset
- 타입 이름을 참조하는 소스 11개: Assets/Editor/ItemMakerWindow.cs; Assets/Scripts/Core/Persistence/PlayerRunStatePersistence.cs; Assets/Scripts/Gameplay/Entities/Shooter/Player/Inventory/PlayerInventoryRuntime.cs; Assets/Scripts/Gameplay/Entities/Shooter/Player/Inventory/PlayerInventoryRuntime.KeyTool.cs; Assets/Scripts/Gameplay/Entities/Shooter/Player/Inventory/PlayerInventoryRuntime.RunPersistence.cs; Assets/Scripts/Gameplay/Items/Loot/LockableLootObject.cs; Assets/Scripts/Gameplay/Items/Loot/LockedLootProfileSO.cs; Assets/Scripts/Presentation/UI/Gameplay/Loot/LockedLootKeyPanel.cs

## Assets/Scripts/Gameplay/Items/Definitions/KeyToolItemSO.cs
- 분류: 일반 소스; 37행
- 타입: KeyToolItemSO : ItemSO
- 메서드: 
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 1개: Assets/ScriptableObject/GameScene/GameItem/Key/KeyTool_Basic.asset
- 타입 이름을 참조하는 소스 6개: Assets/Editor/ItemMakerWindow.cs; Assets/Scripts/Gameplay/Entities/Shooter/Player/Inventory/PlayerInventoryRuntime.cs; Assets/Scripts/Gameplay/Entities/Shooter/Player/Inventory/PlayerInventoryRuntime.KeyTool.cs; Assets/Scripts/Presentation/UI/Gameplay/PanelHub/Inventory/InventoryUIContracts.cs; Assets/Scripts/Presentation/UI/Gameplay/PanelHub/Inventory/KeyToolContainerPanel.cs; Assets/Scripts/Presentation/UI/Gameplay/PanelHub/Inventory/PlayerInventoryUI.cs

## Assets/Scripts/Gameplay/Items/Definitions/MedicalItemSO.cs
- 분류: 일반 소스; 21행
- 타입: MedicalItemSO : ConsumableItemSO
- 메서드: 
- 직렬화 참조 11개 / 지정 씬 참조 그래프 내 11개: Assets/ScriptableObject/GameScene/GameItem/Consumable/Medical/BandageLV1.asset; Assets/ScriptableObject/GameScene/GameItem/Consumable/Medical/BandageLV2.asset; Assets/ScriptableObject/GameScene/GameItem/Consumable/Medical/Booster1.asset; Assets/ScriptableObject/GameScene/GameItem/Consumable/Medical/Booster2.asset; Assets/ScriptableObject/GameScene/GameItem/Consumable/Medical/Booster3.asset; Assets/ScriptableObject/GameScene/GameItem/Consumable/Medical/BoosterInjector.asset; Assets/ScriptableObject/GameScene/GameItem/Consumable/Medical/Medkit.asset; Assets/ScriptableObject/GameScene/GameItem/Consumable/Medical/Medkit_Copy.asset
- 타입 이름을 참조하는 소스 5개: Assets/Editor/ItemMakerWindow.cs; Assets/Scripts/Gameplay/Items/Definitions/InfectionCureItemSO.cs; Assets/Tests/EditMode/Editor/ItemHandAndCategoryTests.cs; Assets/Tests/EditMode/Editor/ItemMakerBatchTests.cs; Assets/Tests/EditMode/Editor/ItemMakerWeaponCategoryTests.cs

## Assets/Scripts/Gameplay/Items/Definitions/PassiveItemSO.cs
- 분류: 일반 소스; 30행
- 타입: PassiveItemSO : ItemSO
- 메서드: 
- 직렬화 참조 15개 / 지정 씬 참조 그래프 내 9개: Assets/ScriptableObject/GameScene/ContentTemplates/Items/Passive_Template.asset; Assets/ScriptableObject/GameScene/GameItem/PassiveItem/Compass.asset; Assets/ScriptableObject/GameScene/GameItem/PassiveItem/FlashLight.asset; Assets/ScriptableObject/GameScene/GameItem/PassiveItem/FryingPan.asset; Assets/ScriptableObject/GameScene/GameItem/PassiveItem/MagazineLarge.asset; Assets/ScriptableObject/GameScene/GameItem/PassiveItem/MagazineMiddle.asset; Assets/ScriptableObject/GameScene/GameItem/PassiveItem/MagazineSmall.asset; Assets/ScriptableObject/GameScene/GameItem/PassiveItem/Scopex2.asset
- 타입 이름을 참조하는 소스 14개: Assets/Editor/ItemMakerWindow.cs; Assets/Scripts/Core/Persistence/PlayerRunStatePersistence.cs; Assets/Scripts/Gameplay/Entities/Shooter/Enemy/Common/EnemyLootInventoryRuntime.cs; Assets/Scripts/Gameplay/Entities/Shooter/Player/Inventory/PlayerInventoryRuntime.cs; Assets/Scripts/Gameplay/Entities/Shooter/Player/Passive/PlayerPassiveInventoryRuntime.cs; Assets/Scripts/Gameplay/Entities/Shooter/Player/PlayerStatsRuntime.cs; Assets/Scripts/Gameplay/World/Sectors/Shop/ShopRoomDropTableSO.cs; Assets/Scripts/Gameplay/World/Sectors/Treasure/SectorTreasure.cs

## Assets/Scripts/Gameplay/Items/Definitions/UsableItemSO.cs
- 분류: 일반 소스; 77행
- 타입: UsableItemSO : ItemSO
- 메서드: ApplyHeldViewTransform, ResolveHeldViewScale, SanitizeScale
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 8개: Assets/Editor/ItemMakerWindow.cs; Assets/Scripts/Gameplay/Entities/Shooter/Player/Inventory/PlayerInventoryRuntime.cs; Assets/Scripts/Gameplay/Entities/Shooter/Player/Inventory/PlayerQuickSlotRuntime.cs; Assets/Scripts/Gameplay/Entities/Shooter/Player/PlayerAimController.cs; Assets/Scripts/Gameplay/Entities/Shooter/Player/PlayerClickController.cs; Assets/Scripts/Gameplay/Items/Definitions/ConsumableItemSO.cs; Assets/Scripts/Gameplay/Items/Weapons/Definitions/ThrowableUsableItemSO.cs; Assets/Scripts/Gameplay/Items/Weapons/Runtime/UsableHeldVisualUtility.cs

## Assets/Scripts/Gameplay/Items/Definitions/ValuableItemSO.cs
- 분류: 일반 소스; 21행
- 타입: ValuableItemSO : ItemSO
- 메서드: 
- 직렬화 참조 35개 / 지정 씬 참조 그래프 내 35개: Assets/ScriptableObject/GameScene/GameItem/Valuable/Battery_Big.asset; Assets/ScriptableObject/GameScene/GameItem/Valuable/Battery_Small.asset; Assets/ScriptableObject/GameScene/GameItem/Valuable/Boots.asset; Assets/ScriptableObject/GameScene/GameItem/Valuable/Cable.asset; Assets/ScriptableObject/GameScene/GameItem/Valuable/Cake.asset; Assets/ScriptableObject/GameScene/GameItem/Valuable/Candle.asset; Assets/ScriptableObject/GameScene/GameItem/Valuable/CannedFood.asset; Assets/ScriptableObject/GameScene/GameItem/Valuable/Canteen.asset
- 타입 이름을 참조하는 소스 3개: Assets/Editor/ItemMakerWindow.cs; Assets/Scripts/Gameplay/Items/Definitions/CurrencyItemSO.cs; Assets/Tests/EditMode/Editor/RaidRuntimeContractTests.cs

## Assets/Scripts/Gameplay/Items/Loot/EnemyLootDropOnDeath.cs
- 분류: 일반 소스; 202행
- 타입: EnemyLootDropOnDeath : MonoBehaviour
- 메서드: Awake, OnDestroy, Configure, HandleDeath, ResolveRefs, Subscribe, RefreshLegacyDropSuppression, Unsubscribe, ResolveRollSeed
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 1개: Assets/Scripts/Gameplay/Entities/Shooter/Enemy/Common/EnemyShooter.cs

## Assets/Scripts/Gameplay/Items/Loot/LockableLootObject.cs
- 분류: 일반 소스; 146행
- 타입: LockableLootObject : MonoBehaviour
- 메서드: RestoreRaidUnlocked, Awake, OnValidate, TryInteract, TryUnlockAndOpen, ResolveRefs
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 3개: Assets/Scripts/Gameplay/Items/Loot/LootableObject.cs; Assets/Scripts/Presentation/UI/Gameplay/Loot/LockedLootKeyPanel.cs; Assets/Tests/EditMode/Editor/RaidRuntimeContractTests.cs

## Assets/Scripts/Gameplay/Items/Loot/LockedLootProfileSO.cs
- 분류: 일반 소스; 80행
- 타입: LockedLootProfileSO : ScriptableObject
- 메서드: GetAdditionalRollCount, GetRarityWeightMultiplier, OnValidate
- 직렬화 참조 4개 / 지정 씬 참조 그래프 내 0개: Assets/ScriptableObject/GameScene/ContentTemplates/Loot/LockedLootProfiles/LockedLootProfile_Legendary.asset; Assets/ScriptableObject/GameScene/ContentTemplates/Loot/LockedLootProfiles/LockedLootProfile_Normal.asset; Assets/ScriptableObject/GameScene/ContentTemplates/Loot/LockedLootProfiles/LockedLootProfile_Rare.asset; Assets/ScriptableObject/GameScene/ContentTemplates/Loot/LockedLootProfiles/LockedLootProfile_Unique.asset
- 타입 이름을 참조하는 소스 3개: Assets/Scripts/Gameplay/Items/Loot/LockableLootObject.cs; Assets/Scripts/Gameplay/Items/Loot/LootProgressionConfigSO.cs; Assets/Scripts/Gameplay/Items/Loot/LootTableSO.cs

## Assets/Scripts/Gameplay/Items/Loot/LootableObject.cs
- 분류: 일반 소스; 320행
- 타입: LootableObject : MonoBehaviour
- 메서드: BindRaidState, CaptureRaidState, ConfigureRuntimeSource, TryGetLootObject, Awake, TryInteract, TryPrepareLoot, TryOpenPreparedLoot, TryOpenLootHub, TryRollIntoLootHub, ResolveRefs, ConfigureLootInventoryFromTable, PlayLootOpenFeedback, ResolveRollSeed
- 직렬화 참조 29개 / 지정 씬 참조 그래프 내 4개: Assets/Prefabs/GameScene/GameItemObject/LootableObject/AmoBox_01.prefab; Assets/Prefabs/GameScene/GameItemObject/LootableObject/EnergyBox_01.prefab; Assets/Prefabs/GameScene/GameItemObject/LootableObject/HealthBox_01.prefab; Assets/Prefabs/GameScene/GameItemObject/LootableObject/HealthBox_02.prefab; Assets/Prefabs/GameScene/GameItemObject/LootableObject/HealthBox_03.prefab; Assets/Prefabs/GameScene/GameItemObject/LootableObject/MilitaryCase_01_WornWhite.prefab; Assets/Prefabs/GameScene/GameItemObject/LootableObject/MilitaryCase_02_NewWhite.prefab; Assets/Prefabs/GameScene/GameItemObject/LootableObject/MilitaryCase_03_WornGreen.prefab
- 타입 이름을 참조하는 소스 14개: Assets/Editor/LootProgressionEditorWindow.cs; Assets/Editor/StructureLootInventoryRuntimeEditor.cs; Assets/Scripts/Authoring/Editor/ContentPreflightWindow.cs; Assets/Scripts/Authoring/Editor/SectorMakerWindow.cs; Assets/Scripts/Gameplay/Interaction/Core/InteractionManager.cs; Assets/Scripts/Gameplay/Items/Loot/LockableLootObject.cs; Assets/Scripts/Gameplay/Items/Loot/LootProgressionConfigSO.cs; Assets/Scripts/Gameplay/World/Sectors/StageMap/RaidRoomContentSpawner.cs

## Assets/Scripts/Gameplay/Items/Loot/LootObjectSO.cs
- 분류: 일반 소스; 78행
- 타입: LootObjectSO : ScriptableObject
- 메서드: ResolveDisplayName, AssignLootObjectType, OnValidate
- 직렬화 참조 32개 / 지정 씬 참조 그래프 내 7개: Assets/ScriptableObject/GameScene/GameItem/Loot/LootObject/LootObject_AmoBox_01.asset; Assets/ScriptableObject/GameScene/GameItem/Loot/LootObject/LootObject_EnergyBox_01.asset; Assets/ScriptableObject/GameScene/GameItem/Loot/LootObject/LootObject_HealthBox_01.asset; Assets/ScriptableObject/GameScene/GameItem/Loot/LootObject/LootObject_HealthBox_02.asset; Assets/ScriptableObject/GameScene/GameItem/Loot/LootObject/LootObject_HealthBox_03.asset; Assets/ScriptableObject/GameScene/GameItem/Loot/LootObject/LootObject_LootChest_Template.asset; Assets/ScriptableObject/GameScene/GameItem/Loot/LootObject/LootObject_MilitaryCase_01_WornWhite.asset; Assets/ScriptableObject/GameScene/GameItem/Loot/LootObject/LootObject_MilitaryCase_02_NewWhite.asset
- 타입 이름을 참조하는 소스 10개: Assets/Editor/LootProgressionEditorWindow.cs; Assets/Editor/StructureLootInventoryRuntimeEditor.cs; Assets/Scripts/Authoring/Editor/ContentPreflightWindow.cs; Assets/Scripts/Authoring/Editor/SectorMakerWindow.cs; Assets/Scripts/Gameplay/Items/Loot/LootableObject.cs; Assets/Scripts/Gameplay/Items/Loot/LootObjectTypeSO.cs; Assets/Scripts/Gameplay/Items/Loot/LootProgressionConfigSO.cs; Assets/Scripts/Gameplay/Items/Loot/LootTableSO.cs

## Assets/Scripts/Gameplay/Items/Loot/LootObjectTypeSO.cs
- 분류: 일반 소스; 47행
- 타입: LootObjectTypeSO : ScriptableObject
- 메서드: Configure, OnValidate
- 직렬화 참조 3개 / 지정 씬 참조 그래프 내 1개: Assets/ScriptableObject/GameScene/GameItem/Loot/LootObjectType/LootObjectType_AbandonedBag.asset; Assets/ScriptableObject/GameScene/GameItem/Loot/LootObjectType/LootObjectType_AmmoContainer.asset; Assets/ScriptableObject/GameScene/GameItem/Loot/LootObjectType/LootObjectType_Safe.asset
- 타입 이름을 참조하는 소스 3개: Assets/Editor/ItemMakerWindow.cs; Assets/Editor/LootProgressionEditorWindow.cs; Assets/Scripts/Gameplay/Items/Loot/LootObjectSO.cs

## Assets/Scripts/Gameplay/Items/Loot/LootProgressionConfigSO.cs
- 분류: 일반 소스; 807행
- 타입: LootSourceKind, LootItemEntry, LootRarityTier, LootStagePool, LootSourceDefinition, LootRollResult, LootProgressionConfigSO : ScriptableObject
- 메서드: EnsureDefaultTiers, FindTier, EnsureTier, Clone, CopyFrom, ResolveDisplayName, EnsureDefaultPool, ResolvePool, HasStageOverride, GetOrCreatePool, RemoveStageOverride, CopyPool, FindExactPool, CreateSource, RemoveSource, FindEnemySource, FindLootObjectSource, TryRollEnemy, TryRollLootObject, TryRoll, TryAppendGlobalValuable, TryRollValuable, CountValuables, GetResolvedItemSharePercent, TryRollOne, PickTier, GetTierWeight, PickItem, HasValidItems, OnValidate
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 0개: Assets/ScriptableObject/GameScene/GameItem/LootProgressionConfig.asset
- 타입 이름을 참조하는 소스 11개: Assets/Editor/ItemMakerWindow.cs; Assets/Editor/LootProgressionEditorWindow.cs; Assets/Scripts/Authoring/Editor/ContentPreflightWindow.cs; Assets/Scripts/Authoring/Editor/SectorMakerWindow.cs; Assets/Scripts/Gameplay/Items/Loot/EnemyLootDropOnDeath.cs; Assets/Scripts/Gameplay/Items/Loot/LockedLootProfileSO.cs; Assets/Scripts/Gameplay/Items/Loot/LootableObject.cs; Assets/Scripts/Gameplay/Items/Loot/LootProgressionRuntime.cs

## Assets/Scripts/Gameplay/Items/Loot/LootProgressionRuntime.cs
- 분류: 일반 소스; 95행
- 타입: LootProgressionRuntime : MonoBehaviour
- 메서드: Awake, OnDestroy, TryGetConfig, ResolveSectorStateManager
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 1개: Assets/Scripts/Gameplay/Items/Loot/EnemyLootDropOnDeath.cs

## Assets/Scripts/Gameplay/Items/Loot/LootTableSO.cs
- 분류: 일반 소스; 255행
- 타입: LootObjectPool, LootTableSO : ScriptableObject
- 메서드: EnsureStandardTiers, FindTier, AddOrNormalizeTier, EnsurePool, TryRoll, TryRollOne, PickTier, PickItem, GetTierWeight, HasValidItems, OnValidate
- 직렬화 참조 35개 / 지정 씬 참조 그래프 내 8개: Assets/ScriptableObject/GameScene/GameItem/Loot/LootTable/LootTable_AbandonedBag.asset; Assets/ScriptableObject/GameScene/GameItem/Loot/LootTable/LootTable_AmmoContainer.asset; Assets/ScriptableObject/GameScene/GameItem/Loot/LootTable/LootTable_AmoBox_01.asset; Assets/ScriptableObject/GameScene/GameItem/Loot/LootTable/LootTable_EnergyBox_01.asset; Assets/ScriptableObject/GameScene/GameItem/Loot/LootTable/LootTable_HealthBox_01.asset; Assets/ScriptableObject/GameScene/GameItem/Loot/LootTable/LootTable_HealthBox_02.asset; Assets/ScriptableObject/GameScene/GameItem/Loot/LootTable/LootTable_HealthBox_03.asset; Assets/ScriptableObject/GameScene/GameItem/Loot/LootTable/LootTable_LootChest_Template.asset
- 타입 이름을 참조하는 소스 12개: Assets/Editor/ItemMakerWindow.cs; Assets/Editor/LootProgressionEditorWindow.cs; Assets/Editor/StructureLootInventoryRuntimeEditor.cs; Assets/Scripts/Authoring/Editor/ContentPreflightWindow.cs; Assets/Scripts/Authoring/Editor/SectorMakerWindow.cs; Assets/Scripts/Gameplay/Items/Definitions/ValuableItemSO.cs; Assets/Scripts/Gameplay/Items/Loot/LootableObject.cs; Assets/Scripts/Gameplay/Items/Loot/LootObjectSO.cs

## Assets/Scripts/Gameplay/Items/Weapons/Definitions/BasicBulletSO.cs
- 분류: 일반 소스; 59행
- 타입: BasicBulletSO : BulletSO
- 메서드: ResolveDamageTargetMask
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 9개: Assets/Scripts/Gameplay/Items/Weapons/Definitions/HeavyBulletSO.cs; Assets/Scripts/Gameplay/Items/Weapons/Definitions/LightBulletSO.cs; Assets/Scripts/Gameplay/Items/Weapons/Definitions/MediumBulletSO.cs; Assets/Scripts/Gameplay/Items/Weapons/Definitions/RocketBulletSO.cs; Assets/Scripts/Gameplay/Items/Weapons/Definitions/ShotgunShellSO.cs; Assets/Scripts/Gameplay/Items/Weapons/Definitions/SniperBulletSO.cs; Assets/Tests/EditMode/Editor/ItemMakerWeaponCategoryTests.cs; Assets/Tests/EditMode/Editor/ObstacleBulletImpactAudioTests.cs

## Assets/Scripts/Gameplay/Items/Weapons/Definitions/BulletSO.cs
- 분류: 일반 소스; 156행
- 타입: BulletAmmoType, ShooterFaction, BulletTier, BulletFamily, RangedWeaponType, RangedFirePattern, BulletSO : ItemSO
- 메서드: IsBasicAmmoType, ResolveDamageTargetMask
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 64개: Assets/Editor/ItemCategoryMoveUtility.cs; Assets/Editor/ItemMakerWindow.cs; Assets/Scripts/Authoring/Editor/ContentPreflightWindow.cs; Assets/Scripts/Authoring/Editor/ContentTemplateInspectors.cs; Assets/Scripts/Authoring/Editor/DebugMonitorWindow.cs; Assets/Scripts/Authoring/Editor/ShooterLoadoutSOEditor.cs; Assets/Scripts/Core/Events/ScriptableObject/WeaponAmmoLoadoutEventChannelSO.cs; Assets/Scripts/Core/Persistence/PlayerRunStatePersistence.cs

## Assets/Scripts/Gameplay/Items/Weapons/Definitions/HealingBottleUsableItemSO.cs
- 분류: 일반 소스; 53행
- 타입: HealingBottleUsableItemSO : ThrowableUsableItemSO
- 메서드: 
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 1개: Assets/Scripts/Gameplay/Items/Weapons/Runtime/ThrowableProjectile.cs

## Assets/Scripts/Gameplay/Items/Weapons/Definitions/HeavyBulletSO.cs
- 분류: 일반 소스; 16행
- 타입: HeavyBulletSO : BasicBulletSO
- 메서드: 
- 직렬화 참조 8개 / 지정 씬 참조 그래프 내 6개: Assets/ScriptableObject/GameScene/GameItem/Bullet/HeavyBulletLV1.asset; Assets/ScriptableObject/GameScene/GameItem/Bullet/HeavyBulletLV2.asset; Assets/ScriptableObject/GameScene/GameItem/Bullet/HeavyBulletLV3.asset; Assets/ScriptableObject/GameScene/GameItem/Bullet/HeavyBulletLV4.asset; Assets/ScriptableObject/GameScene/GameItem/Bullet/HeavyBulletLV5.asset; Assets/ScriptableObject/GameScene/GameItem/Bullet/HeavyBulletLV6.asset; Assets/ScriptableObject/GameScene/GameItem/Bullet/HeavyBulletLV7.asset; Assets/ScriptableObject/GameScene/GameItem/Bullet/HeavyBulletLV8.asset
- 타입 이름을 참조하는 소스 3개: Assets/Editor/ItemMakerWindow.cs; Assets/Tests/EditMode/Editor/ItemMakerWeaponCategoryTests.cs; Assets/Tests/EditMode/Editor/ObstacleBulletImpactAudioTests.cs

## Assets/Scripts/Gameplay/Items/Weapons/Definitions/InfectionInjectionBulletSO.cs
- 분류: 일반 소스; 143행
- 타입: InfectionInjectionBulletSO : SpecialBulletSO
- 메서드: ResolveDamageTargetMask, ValidateSpecialConfiguration, TryInitializeProjectile, ResolveSourceFaction
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 2개: Assets/Tests/EditMode/Editor/ItemMakerWeaponCategoryTests.cs; Assets/Tests/EditMode/Editor/ObstacleBulletImpactAudioTests.cs

## Assets/Scripts/Gameplay/Items/Weapons/Definitions/LightBulletSO.cs
- 분류: 일반 소스; 16행
- 타입: LightBulletSO : BasicBulletSO
- 메서드: 
- 직렬화 참조 9개 / 지정 씬 참조 그래프 내 6개: Assets/ScriptableObject/GameScene/ContentTemplates/Items/Bullet_Light_Template.asset; Assets/ScriptableObject/GameScene/GameItem/Bullet/LightBulletLV1.asset; Assets/ScriptableObject/GameScene/GameItem/Bullet/LightBulletLV2.asset; Assets/ScriptableObject/GameScene/GameItem/Bullet/LightBulletLV3.asset; Assets/ScriptableObject/GameScene/GameItem/Bullet/LightBulletLV4.asset; Assets/ScriptableObject/GameScene/GameItem/Bullet/LightBulletLV5.asset; Assets/ScriptableObject/GameScene/GameItem/Bullet/LightBulletLV6.asset; Assets/ScriptableObject/GameScene/GameItem/Bullet/LightBulletLV7.asset
- 타입 이름을 참조하는 소스 3개: Assets/Editor/ItemMakerWindow.cs; Assets/Tests/EditMode/Editor/ItemMakerWeaponCategoryTests.cs; Assets/Tests/EditMode/Editor/ObstacleBulletImpactAudioTests.cs

## Assets/Scripts/Gameplay/Items/Weapons/Definitions/MediumBulletSO.cs
- 분류: 일반 소스; 16행
- 타입: MediumBulletSO : BasicBulletSO
- 메서드: 
- 직렬화 참조 8개 / 지정 씬 참조 그래프 내 6개: Assets/ScriptableObject/GameScene/GameItem/Bullet/MediumBulletLV1.asset; Assets/ScriptableObject/GameScene/GameItem/Bullet/MediumBulletLV2.asset; Assets/ScriptableObject/GameScene/GameItem/Bullet/MediumBulletLV3.asset; Assets/ScriptableObject/GameScene/GameItem/Bullet/MediumBulletLV4.asset; Assets/ScriptableObject/GameScene/GameItem/Bullet/MediumBulletLV5.asset; Assets/ScriptableObject/GameScene/GameItem/Bullet/MediumBulletLV6.asset; Assets/ScriptableObject/GameScene/GameItem/Bullet/MediumBulletLV7.asset; Assets/ScriptableObject/GameScene/GameItem/Bullet/MediumBulletLV8.asset
- 타입 이름을 참조하는 소스 3개: Assets/Editor/ItemMakerWindow.cs; Assets/Tests/EditMode/Editor/ItemMakerWeaponCategoryTests.cs; Assets/Tests/EditMode/Editor/ObstacleBulletImpactAudioTests.cs

## Assets/Scripts/Gameplay/Items/Weapons/Definitions/MeleeWeaponSO.cs
- 분류: 일반 소스; 92행
- 타입: MeleeWeaponSO : WeaponSO, IMeleeAttackProfile, used
- 메서드: ResolveDamageTargetMask
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 1개: Assets/ScriptableObject/GameScene/GameItem/Weapon/WIP/CombatKnife.asset
- 타입 이름을 참조하는 소스 221개: Assets/Editor/ItemMakerWindow.cs; Assets/Editor/LootProgressionEditorWindow.cs; Assets/Scripts/Authoring/Editor/AuthoringWorkspaceUi.cs; Assets/Scripts/Authoring/Editor/ContentPreflightWindow.cs; Assets/Scripts/Authoring/Editor/ContentTemplateInspectors.cs; Assets/Scripts/Authoring/Editor/DebugMonitorWindow.Operations.cs; Assets/Scripts/Authoring/Editor/SectorMakerWindow.cs; Assets/Scripts/Authoring/RendererBoundsColliderFitter.cs

## Assets/Scripts/Gameplay/Items/Weapons/Definitions/RangedWeaponSO.cs
- 분류: 일반 소스; 157행
- 타입: RangedWeaponSO : WeaponSO
- 메서드: IsCompatibleWith, ResolveProjectileCount, ResolveDamageMultiplierPerProjectile, ResolveGunshotSoundRadius
- 직렬화 참조 12개 / 지정 씬 참조 그래프 내 11개: Assets/ScriptableObject/GameScene/ContentTemplates/Items/Weapon_Pistol_Template.asset; Assets/ScriptableObject/GameScene/GameItem/Weapon/AssaultRifle1.asset; Assets/ScriptableObject/GameScene/GameItem/Weapon/AssaultRifle2.asset; Assets/ScriptableObject/GameScene/GameItem/Weapon/LMG.asset; Assets/ScriptableObject/GameScene/GameItem/Weapon/Pistol1.asset; Assets/ScriptableObject/GameScene/GameItem/Weapon/Pistol2.asset; Assets/ScriptableObject/GameScene/GameItem/Weapon/Revolver.asset; Assets/ScriptableObject/GameScene/GameItem/Weapon/RocketLauncher.asset
- 타입 이름을 참조하는 소스 37개: Assets/Editor/ItemCategoryMoveUtility.cs; Assets/Editor/ItemMakerWindow.cs; Assets/Scripts/Authoring/Editor/DebugMonitorWindow.Operations.cs; Assets/Scripts/Authoring/Editor/ShooterLoadoutSOEditor.cs; Assets/Scripts/Gameplay/Entities/Common/Equipment/EntityEquipmentRuntime.cs; Assets/Scripts/Gameplay/Entities/Shooter/Common/Core/EntityShootCore.cs; Assets/Scripts/Gameplay/Entities/Shooter/Common/Core/EntityShooterCharacterAudio.cs; Assets/Scripts/Gameplay/Entities/Shooter/Common/Core/ShooterEquipmentController.cs

## Assets/Scripts/Gameplay/Items/Weapons/Definitions/RocketBulletSO.cs
- 분류: 일반 소스; 16행
- 타입: RocketBulletSO : BasicBulletSO
- 메서드: 
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 1개: Assets/ScriptableObject/GameScene/GameItem/Bullet/Rocket.asset
- 타입 이름을 참조하는 소스 3개: Assets/Editor/ItemMakerWindow.cs; Assets/Tests/EditMode/Editor/ItemMakerWeaponCategoryTests.cs; Assets/Tests/EditMode/Editor/ObstacleBulletImpactAudioTests.cs

## Assets/Scripts/Gameplay/Items/Weapons/Definitions/ShotgunShellSO.cs
- 분류: 일반 소스; 21행
- 타입: ShotgunShellSO : BasicBulletSO
- 메서드: 
- 직렬화 참조 8개 / 지정 씬 참조 그래프 내 6개: Assets/ScriptableObject/GameScene/GameItem/Bullet/ShotgunShellLV1.asset; Assets/ScriptableObject/GameScene/GameItem/Bullet/ShotgunShellLV2.asset; Assets/ScriptableObject/GameScene/GameItem/Bullet/ShotgunShellLV3.asset; Assets/ScriptableObject/GameScene/GameItem/Bullet/ShotgunShellLV4.asset; Assets/ScriptableObject/GameScene/GameItem/Bullet/ShotgunShellLV5.asset; Assets/ScriptableObject/GameScene/GameItem/Bullet/ShotgunShellLV6.asset; Assets/ScriptableObject/GameScene/GameItem/Bullet/ShotgunShellLV7.asset; Assets/ScriptableObject/GameScene/GameItem/Bullet/ShotgunShellLV8.asset
- 타입 이름을 참조하는 소스 4개: Assets/Editor/ItemMakerWindow.cs; Assets/Scripts/Gameplay/Items/Weapons/Definitions/ShotgunWeaponSO.cs; Assets/Tests/EditMode/Editor/ItemMakerWeaponCategoryTests.cs; Assets/Tests/EditMode/Editor/ObstacleBulletImpactAudioTests.cs

## Assets/Scripts/Gameplay/Items/Weapons/Definitions/ShotgunWeaponSO.cs
- 분류: 일반 소스; 28행
- 타입: ShotgunWeaponSO : RangedWeaponSO
- 메서드: ResolveProjectileCount
- 직렬화 참조 2개 / 지정 씬 참조 그래프 내 2개: Assets/ScriptableObject/GameScene/GameItem/Weapon/Shotgun1.asset; Assets/ScriptableObject/GameScene/GameItem/Weapon/Shotgun2.asset
- 타입 이름을 참조하는 소스 2개: Assets/Editor/ItemMakerWindow.cs; Assets/Tests/EditMode/Editor/ItemMakerWeaponCategoryTests.cs

## Assets/Scripts/Gameplay/Items/Weapons/Definitions/SniperBulletSO.cs
- 분류: 일반 소스; 16행
- 타입: SniperBulletSO : BasicBulletSO
- 메서드: 
- 직렬화 참조 8개 / 지정 씬 참조 그래프 내 6개: Assets/ScriptableObject/GameScene/GameItem/Bullet/SniperBulletLV1.asset; Assets/ScriptableObject/GameScene/GameItem/Bullet/SniperBulletLV2.asset; Assets/ScriptableObject/GameScene/GameItem/Bullet/SniperBulletLV3.asset; Assets/ScriptableObject/GameScene/GameItem/Bullet/SniperBulletLV4.asset; Assets/ScriptableObject/GameScene/GameItem/Bullet/SniperBulletLV5.asset; Assets/ScriptableObject/GameScene/GameItem/Bullet/SniperBulletLV6.asset; Assets/ScriptableObject/GameScene/GameItem/Bullet/SniperBulletLV7.asset; Assets/ScriptableObject/GameScene/GameItem/Bullet/SniperBulletLV8.asset
- 타입 이름을 참조하는 소스 3개: Assets/Editor/ItemMakerWindow.cs; Assets/Tests/EditMode/Editor/ItemMakerWeaponCategoryTests.cs; Assets/Tests/EditMode/Editor/ObstacleBulletImpactAudioTests.cs

## Assets/Scripts/Gameplay/Items/Weapons/Definitions/SpecialBulletSO.cs
- 분류: 일반 소스; 67행
- 타입: SpecialBulletLaunchContext, SpecialBulletSO : BulletSO
- 메서드: ValidateSpecialConfiguration, TryInitializeProjectile
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 6개: Assets/Scripts/Gameplay/Entities/Shooter/Common/Core/EntityShootCore.cs; Assets/Scripts/Gameplay/Entities/Shooter/Enemy/Common/EnemyShooterShoot.cs; Assets/Scripts/Gameplay/Entities/Shooter/Player/PlayerBulletLoadoutRuntime.cs; Assets/Scripts/Gameplay/Entities/Shooter/Player/PlayerClickController.cs; Assets/Scripts/Gameplay/Entities/Shooter/Player/PlayerShooterShoot.cs; Assets/Scripts/Gameplay/Items/Weapons/Definitions/InfectionInjectionBulletSO.cs

## Assets/Scripts/Gameplay/Items/Weapons/Definitions/ThrowableUsableItemSO.cs
- 분류: 일반 소스; 157행
- 타입: ThrowableUsableItemSO : UsableItemSO
- 메서드: ResolveDamageTargetMask, ResolveMaskRender, ResolveFallbackPaintChannel, ShouldDetonateImmediately
- 직렬화 참조 2개 / 지정 씬 참조 그래프 내 2개: Assets/ScriptableObject/GameScene/GameItem/Consumable/Throwable/Flashbang.asset; Assets/ScriptableObject/GameScene/GameItem/Consumable/Throwable/Grenade.asset
- 타입 이름을 참조하는 소스 13개: Assets/Editor/ItemMakerWindow.cs; Assets/Scripts/Authoring/Editor/DebugMonitorWindow.Operations.cs; Assets/Scripts/Gameplay/Entities/Shooter/Common/Core/EntityShooterCharacterAudio.cs; Assets/Scripts/Gameplay/Entities/Shooter/Common/Core/ShooterThrowableAttack.cs; Assets/Scripts/Gameplay/Entities/Shooter/Enemy/Definitions/EnemyShooterConfigSO.cs; Assets/Scripts/Gameplay/Entities/Shooter/Enemy/StateMachine/EnemyShooterCombatController.cs; Assets/Scripts/Gameplay/Entities/Shooter/Player/Inventory/PlayerQuickSlotRuntime.cs; Assets/Scripts/Gameplay/Entities/Shooter/Player/PlayerClickController.cs

## Assets/Scripts/Gameplay/Items/Weapons/Definitions/WeaponSO.cs
- 분류: 일반 소스; 71행
- 타입: WeaponSO : ItemSO
- 메서드: ApplyWeaponViewTransform, SanitizeScale
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 38개: Assets/Editor/ItemMakerWindow.cs; Assets/Scripts/Authoring/Editor/ContentTemplateInspectors.cs; Assets/Scripts/Authoring/Editor/DebugMonitorWindow.Operations.cs; Assets/Scripts/Core/Events/ScriptableObject/WeaponAmmoEventChannelSO.cs; Assets/Scripts/Core/Persistence/PlayerRunStatePersistence.cs; Assets/Scripts/Gameplay/Entities/Common/Equipment/EntityEquipmentRuntime.cs; Assets/Scripts/Gameplay/Entities/Shooter/Common/Core/EntityRange.cs; Assets/Scripts/Gameplay/Entities/Shooter/Common/Core/EntityShooterCharacterAudio.cs

## Assets/Scripts/Gameplay/Items/Weapons/Runtime/BasicBullet.cs
- 분류: 일반 소스; 889행
- 타입: BasicBullet : MonoBehaviour
- 메서드: Init, ConfigureInfectionInjection, Update, Complete, OnDisable, TryGetClosestHostileIncomingThreat, TryGetClosestHit, IsHostileTo, WillHitSubjectWithin, TryGetClosestImpactHit, TryGetClosestDamageHit, IsDamageTargetLayer, TrySelectClosestHit, IsSourceEntityCollider, TryApplyDamage, TryApplyPaintMark, TryApplyInfectionDamage, TryApplyInfectionInjection, TryApplyEnemyBulletInjuries, TryGetProjectileHurtbox, TryResolveDamageableTarget, IsDamageTarget, HasConfiguredProjectileHurtbox, IsAcceptedHurtbox, ResolveColliderPath, ResolveObjectName, ResolveLayerName, FormatLayerMask, RegisterActiveProjectile, UnregisterActiveProjectile, PlanarSqrDistance
- 직렬화 참조 49개 / 지정 씬 참조 그래프 내 30개: Assets/Prefabs/GameScene/GameItemObject/Bullet/BulletLV5.prefab; Assets/Prefabs/GameScene/GameItemObject/Bullet/BulletLV6.prefab; Assets/Prefabs/GameScene/GameItemObject/Bullet/BulletLV7.prefab; Assets/Prefabs/GameScene/GameItemObject/Bullet/BulletLV8.prefab; Assets/Prefabs/GameScene/GameItemObject/Bullet/HeavyBullet/HeavyBullet.prefab; Assets/Prefabs/GameScene/GameItemObject/Bullet/HeavyBullet/HeavyBulletLV1.prefab; Assets/Prefabs/GameScene/GameItemObject/Bullet/HeavyBullet/HeavyBulletLV2.prefab; Assets/Prefabs/GameScene/GameItemObject/Bullet/HeavyBullet/HeavyBulletLV3.prefab
- 타입 이름을 참조하는 소스 5개: Assets/Scripts/Authoring/Editor/ContentPreflightWindow.cs; Assets/Scripts/Gameplay/Entities/Shooter/Common/Core/EntityShootCore.cs; Assets/Scripts/Gameplay/Entities/Shooter/Enemy/StateMachine/EnemyShooterCombatController.cs; Assets/Scripts/Gameplay/Items/Weapons/Definitions/InfectionInjectionBulletSO.cs; Assets/Scripts/Presentation/Audio/BulletImpactAudio.cs

## Assets/Scripts/Gameplay/Items/Weapons/Runtime/HealingZoneRuntime.cs
- 분류: 일반 소스; 82행
- 타입: HealingZoneRuntime : SpecialAreaRuntime
- 메서드: Configure, TickArea, OnDrawGizmosSelected
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 0개: Assets/Prefabs/GameScene/Article/CharacterEffect/Healling/HealingZone.prefab
- 타입 이름을 참조하는 소스 2개: Assets/Scripts/Gameplay/Items/Weapons/Definitions/HealingBottleUsableItemSO.cs; Assets/Scripts/Gameplay/Items/Weapons/Runtime/ThrowableProjectile.cs

## Assets/Scripts/Gameplay/Items/Weapons/Runtime/MeleeWeaponView.cs
- 분류: 일반 소스; 18행
- 타입: MeleeWeaponView : WeaponView
- 메서드: 
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 1개: Assets/Prefabs/GameScene/GameItemObject/Weapon/Combat.prefab
- 타입 이름을 참조하는 소스 2개: Assets/Scripts/Gameplay/Entities/Shooter/Common/Core/ShooterEquipmentController.cs; Assets/Scripts/Gameplay/Items/Weapons/Definitions/MeleeWeaponSO.cs

## Assets/Scripts/Gameplay/Items/Weapons/Runtime/ProjectileCombatFeatureFlags.cs
- 분류: 일반 소스; 17행
- 타입: ProjectileCombatFeatureFlags
- 메서드: 
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 5개: Assets/Scripts/Gameplay/Entities/Shooter/Common/Core/ShooterStatsRuntime.cs; Assets/Scripts/Gameplay/Entities/Shooter/Enemy/Common/EnemyShooterStatsRuntime.cs; Assets/Scripts/Gameplay/Entities/Shooter/Player/PlayerStatsRuntime.cs; Assets/Scripts/Gameplay/Items/Weapons/Definitions/BasicBulletSO.cs; Assets/Scripts/Gameplay/Items/Weapons/Runtime/BasicBullet.cs

## Assets/Scripts/Gameplay/Items/Weapons/Runtime/SpecialAreaRuntime.cs
- 분류: 일반 소스; 161행
- 타입: SpecialAreaPolarity, SpecialAreaRuntime : MonoBehaviour
- 메서드: ConfigureArea, OnEnable, OnDisable, Update, TickArea, EvaluateThreat, IsHarmfulTo, IsInside, EvaluateCombinedThreat, RegisterActiveArea
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 3개: Assets/Scripts/Gameplay/Entities/Shooter/Enemy/Definitions/EnemyShooterConfigSO.cs; Assets/Scripts/Gameplay/Entities/Shooter/Enemy/StateMachine/EnemyShooterCombatController.cs; Assets/Scripts/Gameplay/Items/Weapons/Runtime/HealingZoneRuntime.cs

## Assets/Scripts/Gameplay/Items/Weapons/Runtime/ThrowableProjectile.cs
- 분류: 일반 소스; 860행
- 타입: ThrowableProjectile : MonoBehaviour
- 메서드: Awake, Update, FixedUpdate, OnCollisionEnter, PrepareForEquipped, Launch, Explode, OnDisable, TryGetClosestHostileThreat, EvaluateHostileThreatAt, IsHostileThreatTo, ResolveDangerCenter, ResolveDangerRadius, RegisterActiveProjectile, UnregisterActiveProjectile, PlanarSqrDistance, PublishExplosionSound, StampMask, SpawnHealingZone, SpawnReadableImpactZone, ApplyExplosionDamage, ApplyInfectionDamage, ApplyImpactStatusEffect, IsSourceDamageable, CachePhysicsRefs, SetPhysicsCollidersEnabled, IgnoreSourceCollisions
- 직렬화 참조 2개 / 지정 씬 참조 그래프 내 1개: Assets/Prefabs/GameScene/GameItemObject/Consumable/Throwable/Grenade.prefab; Assets/Prefabs/GameScene/GameItemObject/Consumable/WIP/HealingBottle.prefab
- 타입 이름을 참조하는 소스 5개: Assets/Editor/ItemMakerWindow.cs; Assets/Scripts/Gameplay/Entities/Shooter/Common/Core/ShooterThrowableAttack.cs; Assets/Scripts/Gameplay/Entities/Shooter/Enemy/StateMachine/EnemyShooterCombatController.cs; Assets/Scripts/Gameplay/Entities/Shooter/Player/Inventory/PlayerQuickSlotRuntime.cs; Assets/Scripts/Gameplay/Items/Definitions/UsableItemSO.cs

## Assets/Scripts/Gameplay/Items/Weapons/Runtime/ThrowableZoneVisualRuntime.cs
- 분류: 일반 소스; 233행
- 타입: ThrowableZoneVisualRuntime : MonoBehaviour
- 메서드: Create, Build, BuildCircleMesh, BuildTransparentMaterial, WithMinimumAlpha, OnDestroy
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 1개: Assets/Scripts/Gameplay/Items/Weapons/Runtime/ThrowableProjectile.cs

## Assets/Scripts/Gameplay/Items/Weapons/Runtime/UsableHeldVisualUtility.cs
- 분류: 일반 소스; 40행
- 타입: UsableHeldVisualUtility
- 메서드: Create
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 2개: Assets/Scripts/Gameplay/Entities/Shooter/Player/Inventory/PlayerQuickSlotRuntime.cs; Assets/Tests/EditMode/Editor/ItemHandAndCategoryTests.cs

## Assets/Scripts/Gameplay/Items/Weapons/Runtime/WeaponView.cs
- 분류: 일반 소스; 33행
- 타입: WeaponView : MonoBehaviour
- 메서드: 
- 직렬화 참조 17개 / 지정 씬 참조 그래프 내 14개: Assets/Prefabs/GameScene/GameItemObject/Consumable/Throwable/Grenade.prefab; Assets/Prefabs/GameScene/GameItemObject/Consumable/WIP/HealingBottle.prefab; Assets/Prefabs/GameScene/GameItemObject/Weapon/AssaultRifle1.prefab; Assets/Prefabs/GameScene/GameItemObject/Weapon/AssaultRifle2.prefab; Assets/Prefabs/GameScene/GameItemObject/Weapon/LMG1.prefab; Assets/Prefabs/GameScene/GameItemObject/Weapon/Pistol/Pistol1.prefab; Assets/Prefabs/GameScene/GameItemObject/Weapon/Pistol/Pistol1_Laser.prefab; Assets/Prefabs/GameScene/GameItemObject/Weapon/Pistol/Pistol2.prefab
- 타입 이름을 참조하는 소스 9개: Assets/Scripts/Gameplay/Entities/Shooter/Common/Core/ShooterEquipmentController.cs; Assets/Scripts/Gameplay/Entities/Shooter/Common/Core/ShooterThrowableAttack.cs; Assets/Scripts/Gameplay/Entities/Shooter/Player/Inventory/PlayerQuickSlotRuntime.cs; Assets/Scripts/Gameplay/Entities/Shooter/Player/PlayerShooterShoot.cs; Assets/Scripts/Gameplay/Items/Definitions/UsableItemSO.cs; Assets/Scripts/Gameplay/Items/Weapons/Definitions/RangedWeaponSO.cs; Assets/Scripts/Gameplay/Items/Weapons/Definitions/WeaponSO.cs; Assets/Scripts/Gameplay/Items/Weapons/Runtime/MeleeWeaponView.cs

## Assets/Scripts/Gameplay/Items/World/BackpackWorldPickupPayload.cs
- 분류: 일반 소스; 61행
- 타입: BackpackWorldPickupPayload : MonoBehaviour
- 메서드: Initialize, TryEquipInto
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 2개: Assets/Scripts/Gameplay/Items/World/WorldItemPickup.cs; Assets/Scripts/Gameplay/Items/World/WorldItemPickupSpawner.cs

## Assets/Scripts/Gameplay/Items/World/RotatingPickupVisual.cs
- 분류: 일반 소스; 138행
- 타입: RotatingPickupVisual : MonoBehaviour
- 메서드: OnEnable, OnDisable, Update, EditorUpdate, ApplyPose, CaptureCurrentAsBasePose, RestoreBasePose
- 직렬화 참조 5개 / 지정 씬 참조 그래프 내 5개: Assets/Prefabs/GameScene/Article/Loot/PickUp_Legendary.prefab; Assets/Prefabs/GameScene/Article/Loot/PickUp_Normal.prefab; Assets/Prefabs/GameScene/Article/Loot/PickUp_Rare.prefab; Assets/Prefabs/GameScene/Article/Loot/PickUp_Unique.prefab; Assets/Prefabs/GameScene/GameItemObject/ItemPickup.prefab
- 타입 이름을 참조하는 소스 0개: 

## Assets/Scripts/Gameplay/Items/World/WorldItemPickup.cs
- 분류: 일반 소스; 420행
- 타입: WorldItemPickup : MonoBehaviour
- 메서드: Awake, Initialize, SetInteractionEnabled, MarkDiscovered, TryPickup, RefreshVisual, ClearVisual, DisableVisualPhysics, LogPickupFailure, ResolveDisplayLabel
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 1개: Assets/Prefabs/GameScene/GameItemObject/ItemPickup.prefab
- 타입 이름을 참조하는 소스 9개: Assets/Scripts/Authoring/Editor/DebugMonitorWindow.Operations.cs; Assets/Scripts/Gameplay/Entities/Shooter/Player/Inventory/PlayerBackpackDropInput.cs; Assets/Scripts/Gameplay/Entities/Shooter/Player/PlayerBulletLoadoutRuntime.cs; Assets/Scripts/Gameplay/Interaction/Core/InteractionManager.cs; Assets/Scripts/Gameplay/Items/World/WorldItemPickupArcMotion.cs; Assets/Scripts/Gameplay/Items/World/WorldItemPickupSpawner.cs; Assets/Scripts/Presentation/UI/Gameplay/Map/MiniMap/SectorTacticalMiniMapGraphic.cs; Assets/Scripts/Presentation/UI/Gameplay/PanelHub/Inventory/PlayerInventoryUI.cs

## Assets/Scripts/Gameplay/Items/World/WorldItemPickupArcMotion.cs
- 분류: 일반 소스; 115행
- 타입: WorldItemPickupArcMotion : MonoBehaviour
- 메서드: Configure, Update, Complete, CacheAndDisableColliders, RestoreColliders
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 0개: 

## Assets/Scripts/Gameplay/Items/World/WorldItemPickupSpawner.cs
- 분류: 일반 소스; 325행
- 타입: WorldItemPickupSpawner : MonoBehaviour
- 메서드: Awake, OnDestroy, TrySpawn, TrySpawnBackpack, ResolveActiveSpawner, TrySpawnInternal, ResolveFieldItemRoot, LogMissingConfiguration
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 1개: Assets/Scenes/RUNScene.unity
- 타입 이름을 참조하는 소스 10개: Assets/Scripts/Authoring/Editor/DebugMonitorWindow.Operations.cs; Assets/Scripts/Gameplay/Entities/Common/StateMachine/Actions/DropRewardSO.cs; Assets/Scripts/Gameplay/Entities/Shooter/Player/Inventory/PlayerBackpackDropInput.cs; Assets/Scripts/Gameplay/Entities/Shooter/Player/PlayerBulletLoadoutRuntime.cs; Assets/Scripts/Gameplay/Items/Loot/EnemyLootDropOnDeath.cs; Assets/Scripts/Gameplay/Items/Loot/LootableObject.cs; Assets/Scripts/Gameplay/Progression/Quests/QuestRuntime.cs; Assets/Scripts/Presentation/UI/Gameplay/PanelHub/Inventory/EnemyInventoryUI.cs

## Assets/Scripts/Gameplay/MaskRenderManager.cs
- 분류: 일반 소스; 1374행
- 타입: MaskRenderManager : MonoBehaviour, CirclePaintRequest, CirclePaintImpact
- 메서드: OnEnable, OnDisable, RegisterSector, UnregisterSector, CanRequestCircle, RequestVirusTrailSegment, RequestCircle, CreateTemporaryCircleVisual, RequestCircleInternal, RequestVirusTrailCircleInternal, HasValidMaskRender, CanSectorAcceptCircle, TryConsumeTrailCircleBudget, RefreshSector, StampCircle, SpawnPaintBubbleParticle, ResolveParticleLifetime, TryGetPoisonPuddleAtWorld, ResolvePoisonPuddleDamageConfig, TryGetStateAtWorld, GetStateAtWorld, ClearAllToNeutral, FillSector, BuildCircleRequest, NeedRenderTextureRebuild, BuildOrUpdateOverlayObjects, ResolveDefaultMaskRender, TryGetMaskRender, ResolveBaseMaterial, BuildOrRebuildMaskRenderTextures, BindTexturesToMaterials, PaintGpuCircle, EnsurePaintStampMaterial, GetTemporaryCircleMesh, GetTemporaryCircleMask, CreateMaskRenderTexture, ClearRenderTexture, CreateOverlayIfNeeded, CreateUnitQuadXZMesh, UpdateOverlayTransform, ReleaseObject, EvaluateCircleImpact, CreateEmptyImpact, AddImpact
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 1개: Assets/Scenes/RUNScene.unity
- 타입 이름을 참조하는 소스 24개: Assets/Scripts/Core/Events/ScriptableObject/MaskRenderManagerEventChannelSO.cs; Assets/Scripts/Gameplay/Combat/PaintMark/MaskRenderSO.cs; Assets/Scripts/Gameplay/Entities/Creature/CommonEnemyStateMachine/AttackExecutor/EnemySelfDestructAttackExecutor.cs; Assets/Scripts/Gameplay/Entities/Creature/EnemyArcBombProjectile.cs; Assets/Scripts/Gameplay/Entities/Creature/EnemyProjectile.cs; Assets/Scripts/Gameplay/Entities/Creature/EnemyVirusTrail.cs; Assets/Scripts/Gameplay/Entities/Creature/NamedEnemy/NamedBattleSectorResetter.cs; Assets/Scripts/Gameplay/Entities/Creature/NormalEnemy/Chaser/ScriptableObject/ChaserSelfDestructConfigSO.cs

## Assets/Scripts/Gameplay/Progression/Difficulty/DifficultyRuntime.cs
- 분류: 일반 소스; 161행
- 타입: DifficultyRuntime : MonoBehaviour
- 메서드: Awake, SetDifficulty, ApplyPlayerDamage, ApplyEnemyHealth, ApplyEnemyDamage, ApplyPlayerInfectionGain, ApplyEnemyNormalMoveSpeed, ApplyEnemyChaseMoveSpeed, ApplyEnemyProjectileSpeed, ApplyEnemyInfectionCastDuration, ApplyEnemyVirusPaintRadius, ApplyPlayerVaccinePaintRadius, ApplyEnemySpawnInterval, ApplyNamedFirstReservationDelay, ApplyNamedReservationDuration, ApplyNamedRespawnCooldown, ApplyNamedRetryDelay, ApplyNamedPatternFirstDelay, ApplyNamedPatternRepeatDelay, ApplyNamedPatternActiveDuration
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 15개: Assets/Scripts/Gameplay/Entities/Common/StateMachine/Conditions/TimeElapsedConditionSO.cs; Assets/Scripts/Gameplay/Entities/Creature/NamedEnemy/Mutarus/ScriptableObject/MutarusChargeAttackConfigSO.cs; Assets/Scripts/Gameplay/Entities/Creature/NamedEnemy/Mutarus/ScriptableObject/MutarusPatternConfigSO.cs; Assets/Scripts/Gameplay/Entities/Creature/NamedEnemy/NamedPatternController.cs; Assets/Scripts/Gameplay/Entities/Creature/NamedEnemy/ScriptableObject/NamedEnemyStatConfigSO.cs; Assets/Scripts/Gameplay/Entities/Creature/NormalEnemy/Chaser/ScriptableObject/ChaserSelfDestructConfigSO.cs; Assets/Scripts/Gameplay/Entities/Creature/NormalEnemy/Roller/ScriptableObject/RollerAttackConfigSO.cs; Assets/Scripts/Gameplay/Entities/Creature/NormalEnemy/ScriptableObjects/DefinitionConfig/NormalEnemyBehaviorConfigSO.cs

## Assets/Scripts/Gameplay/Progression/Difficulty/GameDifficulty.cs
- 분류: 일반 소스; 13행
- 타입: GameDifficulty
- 메서드: 
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 1개: Assets/Scripts/Gameplay/Progression/Difficulty/ScriptableObject/DifficultyRulesSO.cs

## Assets/Scripts/Gameplay/Progression/Difficulty/ScriptableObject/DifficultyRulesSO.cs
- 분류: 일반 소스; 289행
- 타입: DifficultyRulesSO : ScriptableObject
- 메서드: ApplyPlayerDamage, ApplyEnemyHealth, ApplyEnemyDamage, ApplyPlayerInfectionGain, ApplyEnemyNormalMoveSpeed, ApplyEnemyChaseMoveSpeed, ApplyEnemyProjectileSpeed, ApplyEnemyInfectionCastDuration, ApplyEnemyVirusPaintRadius, ApplyPlayerVaccinePaintRadius, ApplyEnemySpawnInterval, ApplyNamedFirstReservationDelay, ApplyNamedReservationDuration, ApplyNamedRespawnCooldown, ApplyNamedRetryDelay, ApplyNamedPatternFirstDelay, ApplyNamedPatternRepeatDelay, ApplyNamedPatternActiveDuration, ApplyTime
- 직렬화 참조 3개 / 지정 씬 참조 그래프 내 1개: Assets/ScriptableObject/Difficulty/HardDifficultyRules 1.asset; Assets/ScriptableObject/Difficulty/NormalDifficultyRules.asset; Assets/ScriptableObject/Difficulty/VeryHardDifficultyRules 2.asset
- 타입 이름을 참조하는 소스 2개: Assets/Scripts/Gameplay/Entities/Shooter/Player/PlayerShooterInfection.cs; Assets/Scripts/Gameplay/Progression/Difficulty/DifficultyRuntime.cs

## Assets/Scripts/Gameplay/Progression/Hideout/HideoutRuntime.cs
- 분류: 일반 소스; 23행
- 타입: HideoutRuntime : MonoBehaviour
- 메서드: Initialize
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 2개: Assets/Scripts/Gameplay/World/Sectors/StageMap/StageSectorInstantiator.cs; Assets/Scripts/Presentation/UI/Gameplay/PlayerTimer/PlayerTimerHUD.cs

## Assets/Scripts/Gameplay/Progression/Hideout/HideoutStageEntryPortal.cs
- 분류: 일반 소스; 19행
- 타입: HideoutStageEntryPortal : MonoBehaviour
- 메서드: 
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 0개: 

## Assets/Scripts/Gameplay/Progression/Hideout/HideoutStageSelectStation.cs
- 분류: 일반 소스; 49행
- 타입: HideoutStageSelectStation : MonoBehaviour
- 메서드: TryInteract
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 1개: Assets/Prefabs/GameScene/Sector/HomeServices/HideoutStageSelectStation.prefab
- 타입 이름을 참조하는 소스 5개: Assets/Scripts/Core/Events/ScriptableObject/HideoutStageSelectOpenRequestEventChannelSO.cs; Assets/Scripts/Gameplay/Interaction/Core/InteractionManager.cs; Assets/Scripts/Gameplay/Progression/Hideout/HideoutStageEntryPortal.cs; Assets/Scripts/Gameplay/World/Sectors/StageMap/StageSectorInstantiator.cs; Assets/Scripts/Presentation/UI/Gameplay/Hideout/HideoutStageSelectPanel.cs

## Assets/Scripts/Gameplay/Progression/Hideout/HomeSector.cs
- 분류: 일반 소스; 61행
- 타입: HomeSector : MonoBehaviour
- 메서드: Initialize, ResetHomeResidents, OnValidate
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 1개: Assets/Prefabs/GameScene/Sector/HomeSector.prefab
- 타입 이름을 참조하는 소스 10개: Assets/Scripts/Core/SceneFlow/RuntimeSceneDependencyValidator.cs; Assets/Scripts/Core/SceneFlow/SceneLoader.cs; Assets/Scripts/Gameplay/Combat/Infection/InfectionControlManager.cs; Assets/Scripts/Gameplay/Items/World/WorldItemPickupSpawner.cs; Assets/Scripts/Gameplay/Progression/Hideout/HideoutStageSelectStation.cs; Assets/Scripts/Gameplay/Progression/Quests/HomeQuestNpc.cs; Assets/Scripts/Gameplay/Progression/Quests/QuestRuntime.cs; Assets/Scripts/Gameplay/Progression/Stage/StageProgressionManager.cs

## Assets/Scripts/Gameplay/Progression/Level/EnemyKillRewardSource.cs
- 분류: 일반 소스; 22행
- 타입: EnemyKillRewardSource : MonoBehaviour
- 메서드: SetEnemyStatConfig, SetEnemyShooterConfig
- 직렬화 참조 7개 / 지정 씬 참조 그래프 내 0개: Assets/Prefabs/GameScene/Entity_WIP/Virus/NormalVirus/Chaser/Chaser.prefab; Assets/Prefabs/GameScene/Entity_WIP/Virus/NormalVirus/Chaser/Chaser_Upgrade.prefab; Assets/Prefabs/GameScene/Entity_WIP/Virus/NormalVirus/Roller/Roller.prefab; Assets/Prefabs/GameScene/Entity_WIP/Virus/NormalVirus/Spitter/Spitter_ThreeShot_Virus.prefab; Assets/Prefabs/GameScene/Entity_WIP/Virus/NormalVirus/Spitter/Spitter_Virus.prefab; Assets/Prefabs/GameScene/Entity_WIP/Virus/NormalVirus/Worm/Worm.prefab; Assets/Prefabs/GameScene/Entity_WIP/Virus/NormalVirus/Worm/Worm_Upgrade.prefab
- 타입 이름을 참조하는 소스 2개: Assets/Scripts/Gameplay/World/Sectors/Named_Boss/NamedSectorController.cs; Assets/Scripts/Gameplay/World/Sectors/SectorEnemySpawner.cs

## Assets/Scripts/Gameplay/Progression/Meta/MetaProgressController.cs
- 분류: 일반 소스; 122행
- 타입: MetaProgressController : MonoBehaviour
- 메서드: OnEnable, OnDisable, GetLevel, TryPurchase, AddRoguelikeCurrency, SetRoguelikeCurrency, CreateSnapshot, PublishSnapshot, PublishChanged, LoadSave, EnsureSaveData, SaveIfNeeded
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 1개: Assets/Prefabs/UI/MainMenu/Canvas-MainMenu.prefab
- 타입 이름을 참조하는 소스 3개: Assets/Scripts/Presentation/UI/Gameplay/MetaSystem/MetaSystemPanelUI.cs; Assets/Scripts/Presentation/UI/Gameplay/MetaSystem/MetaUpgradePanelUI.cs; Assets/Scripts/Presentation/UI/Gameplay/MetaSystem/MetaUpgradeRowUI.cs

## Assets/Scripts/Gameplay/Progression/Meta/MetaUpgradeTypes.cs
- 분류: 일반 소스; 49행
- 타입: MetaUpgradeId, MetaUpgradeSnapshot, MetaProgressSnapshot
- 메서드: 
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 7개: Assets/Scripts/Core/Events/ScriptableObject/MetaProgressChangedEventChannelSO.cs; Assets/Scripts/Core/Persistence/Save.cs; Assets/Scripts/Gameplay/Progression/Meta/MetaProgressController.cs; Assets/Scripts/Gameplay/Progression/Meta/ScriptableObjects/MetaUpgradeCatalogSO.cs; Assets/Scripts/Presentation/UI/Gameplay/MetaSystem/MetaStatsPanelUI.cs; Assets/Scripts/Presentation/UI/Gameplay/MetaSystem/MetaUpgradePanelUI.cs; Assets/Scripts/Presentation/UI/Gameplay/MetaSystem/MetaUpgradeRowUI.cs

## Assets/Scripts/Gameplay/Progression/Meta/RoguelikeCurrencyRewardApplier.cs
- 분류: 일반 소스; 37행
- 타입: RoguelikeCurrencyRewardApplier : MonoBehaviour
- 메서드: OnEnable, ResetRunRewardHistory, GrantMidBossDefeatedReward, GrantFinalBossDefeatedReward
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 0개: 

## Assets/Scripts/Gameplay/Progression/Meta/ScriptableObjects/MetaUpgradeCatalogSO.cs
- 분류: 일반 소스; 269행
- 타입: MetaUpgradeCatalogSO : ScriptableObject, MetaUpgradeDefinition
- 메서드: GetUpgradeAt, TryGetUpgrade, BuildSnapshot, ApplyMetaModifiers, GetCostForNextLevel, GetEffectForNextLevel, GetTotalEffect, TryCreateModifier
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 1개: Assets/ScriptableObject/METAUpgrade/METAUpgradeCatalog.asset
- 타입 이름을 참조하는 소스 2개: Assets/Scripts/Gameplay/Progression/Meta/MetaProgressController.cs; Assets/Scripts/Presentation/UI/Gameplay/MetaSystem/MetaStatsPanelUI.cs

## Assets/Scripts/Gameplay/Progression/Meta/ScriptableObjects/RoguelikeCurrencyRewardRulesSO.cs
- 분류: 일반 소스; 40행
- 타입: RoguelikeCurrencyRewardRulesSO : ScriptableObject
- 메서드: GetStageTransitionReward
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 0개: Assets/ScriptableObject/GameScene/GameItem/Currency/METARoguelikeCurrencyRewardRules.asset
- 타입 이름을 참조하는 소스 0개: 

## Assets/Scripts/Gameplay/Progression/ProgressManager.cs
- 분류: 일반 소스; 274행
- 타입: ProgressManager : MonoBehaviour
- 메서드: DebugApplyInspectorBestScore, GameOverReportRunScore, GameOverReportRunTimeMs, GameSuccessReportRunScore, EquipSkin, ResetProgress, IsAchievementEligible_Score, IsAchievementClaimable, IsUnlockable, UnlockByAchievementId, ClaimAchievement
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 9개: Assets/Scripts/Presentation/UI/HUD/Legacy/AchievementPopupManager.cs; Assets/Scripts/WIP/Legacy/MainScene/Achievement/AchievementManager.cs; Assets/Scripts/WIP/Legacy/MainScene/Achievement/AchievementRewardEntry.cs; Assets/Scripts/WIP/Legacy/MainScene/Achievement/CardRewardSO.cs; Assets/Scripts/WIP/Legacy/MainScene/Achievement/RewardDB.cs; Assets/Scripts/WIP/Legacy/MainScene/Achievement/RewardSO.cs; Assets/Scripts/WIP/Legacy/MainScene/Achievement/SkinRewardSO.cs; Assets/Scripts/WIP/Legacy/MainScene/Skininventory/SkinInventoryManager.cs

## Assets/Scripts/Gameplay/Progression/Quests/HomeQuestNpc.cs
- 분류: 일반 소스; 154행
- 타입: HomeQuestNpc : MonoBehaviour
- 메서드: CanOffer, Awake, TryInteract, BeginConversation, EndConversation, PlayChoiceResponse, PlayDialogueCue, StopVoice, OnDisable, ResetForHomeVisit, DisableQuestOnlyRuntime, DisableLootRuntime
- 직렬화 참조 3개 / 지정 씬 참조 그래프 내 3개: Assets/Prefabs/GameScene/Shooter/HomeNPC/HomeItemNpc.prefab; Assets/Prefabs/GameScene/Shooter/HomeNPC/HomeQuestNpc.prefab; Assets/Prefabs/GameScene/Shooter/HomeNPC/HomeStatsNpc.prefab
- 타입 이름을 참조하는 소스 7개: Assets/Scripts/Authoring/Editor/DebugMonitorWindow.Quests.cs; Assets/Scripts/Gameplay/Interaction/Core/InteractionManager.cs; Assets/Scripts/Gameplay/Progression/Hideout/HomeSector.cs; Assets/Scripts/Gameplay/Progression/Quests/QuestRuntime.cs; Assets/Scripts/Presentation/UI/Gameplay/PanelHub/PlayerPanelHub.cs; Assets/Scripts/Presentation/UI/Gameplay/PanelHub/QuestMissionPanelUI.cs; Assets/Tests/EditMode/Editor/AudioGroundworkTests.cs

## Assets/Scripts/Gameplay/Progression/Quests/QuestDefinitionSO.cs
- 분류: 일반 소스; 224행
- 타입: QuestObjectiveType, QuestDistanceRule, QuestRewardType, QuestBranch, QuestRewardDefinition, QuestObjectiveDefinition, QuestDefinitionSO : ScriptableObject
- 메서드: BuildSummary, Matches
- 직렬화 참조 37개 / 지정 씬 참조 그래프 내 9개: Assets/ScriptableObject/GameScene/ContentTemplates/Quests/Quest_Template_Collect.asset; Assets/ScriptableObject/GameScene/ContentTemplates/Quests/Quest_Template_Hit.asset; Assets/ScriptableObject/GameScene/ContentTemplates/Quests/Quest_Template_Kill.asset; Assets/ScriptableObject/GameScene/ContentTemplates/Quests/Quest_Template_StatReward.asset; Assets/ScriptableObject/GameScene/Quest/Campaign_01_Raider_Contact.asset; Assets/ScriptableObject/GameScene/Quest/Campaign_02_Raider_Sweep.asset; Assets/ScriptableObject/GameScene/Quest/Campaign_03_Hit_Confirmation.asset; Assets/ScriptableObject/GameScene/Quest/Campaign_04_Stage1_Boss.asset
- 타입 이름을 참조하는 소스 12개: Assets/Editor/QuestDesignTreeWindow.cs; Assets/Scripts/Authoring/Editor/ContentPreflightWindow.cs; Assets/Scripts/Authoring/Editor/ContentTemplateInspectors.cs; Assets/Scripts/Authoring/Editor/DebugMonitorWindow.Quests.cs; Assets/Scripts/Gameplay/Progression/Quests/HomeQuestNpc.cs; Assets/Scripts/Gameplay/Progression/Quests/QuestLineSO.cs; Assets/Scripts/Gameplay/Progression/Quests/QuestRuntime.cs; Assets/Scripts/Gameplay/Progression/Quests/RaidProgressionDraftSO.cs

## Assets/Scripts/Gameplay/Progression/Quests/QuestLineSO.cs
- 분류: 일반 소스; 20행
- 타입: QuestLineSO : ScriptableObject
- 메서드: 
- 직렬화 참조 3개 / 지정 씬 참조 그래프 내 0개: Assets/ScriptableObject/GameScene/ContentTemplates/Quests/QuestLine_Templates.asset; Assets/ScriptableObject/GameScene/Quest/QuestLine.asset; Assets/ScriptableObject/GameScene/Quest/QuestLine_CombatCampaign_20.asset
- 타입 이름을 참조하는 소스 2개: Assets/Editor/QuestDesignTreeWindow.cs; Assets/Scripts/Authoring/Editor/ContentTemplateInspectors.cs

## Assets/Scripts/Gameplay/Progression/Quests/QuestRuntime.cs
- 분류: 일반 소스; 931행
- 타입: QuestRuntimeState, QuestRuntime : MonoBehaviour
- 메서드: Accept, TryAddProgress, TrySetProgress, TryComplete, DebugComplete, IsRewardGranted, TryMarkRewardGranted, Restore, ToSaveData, EnsureRewardStateLength, EnsureRuntime, Awake, OnDestroy, Update, SelectQuestGiver, BuildHomeQuestCatalog, AddOfferedQuests, RestorePersistentStates, CanAccept, TryAccept, ReportEntityHit, ReportEntityKilled, IsCompleted, ReportCombatEvent, FindState, IsPlayerSource, ResolvePlayerWeapon, ResolvePlayerInventory, HandlePlayerInventoryChanged, RefreshCollectObjectives, HandleQuestCompleted, ProcessPendingRewardGrants, TryGrantReward, TryGrantItemReward, TryGrantPersistentStatReward, TryGrantShopUnlockReward, TryGrantStageUnlockReward, TryGrantWarehouseSlotReward, ApplyWarehouseCapacityToLiveRuntime, RebuildLivePlayerStats, NotifyStateChanged, PersistStates, PublishSystemMessage
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 6개: Assets/Scripts/Authoring/Editor/DebugMonitorWindow.Quests.cs; Assets/Scripts/Gameplay/Entities/Common/Core/Damageable.cs; Assets/Scripts/Gameplay/Progression/Quests/HomeQuestNpc.cs; Assets/Scripts/Gameplay/Progression/Quests/QuestDefinitionSO.cs; Assets/Scripts/Presentation/UI/Gameplay/PanelHub/PlayerPanelHub.cs; Assets/Scripts/Presentation/UI/Gameplay/PanelHub/QuestMissionPanelUI.cs

## Assets/Scripts/Gameplay/Progression/Quests/RaidProgressionDraftSO.cs
- 분류: 일반 소스; 43행
- 타입: RaidProgressionDraftSO : ScriptableObject, RaidMainQuestDraft, RaidSideQuestDraft
- 메서드: 
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 0개: Assets/ScriptableObject/GameScene/Stage/RaidProgressionDraft_WIP.asset
- 타입 이름을 참조하는 소스 0개: 

## Assets/Scripts/Gameplay/Progression/Risk/RiskDef.cs
- 분류: 일반 소스; 67행
- 타입: RiskType, ZoneLayout, RiskDef : ScriptableObject
- 메서드: 
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 2개: Assets/Scripts/Gameplay/Progression/Risk/RiskSession.cs; Assets/Scripts/Gameplay/Progression/Risk/RiskSet.cs

## Assets/Scripts/Gameplay/Progression/Risk/RiskSession.cs
- 분류: 일반 소스; 30행
- 타입: RiskSession
- 메서드: SetSelection, Clear
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 1개: Assets/Scripts/WIP/Legacy/Presentation/SurvivalSuccessManager.cs

## Assets/Scripts/Gameplay/Progression/Risk/RiskSet.cs
- 분류: 일반 소스; 107행
- 타입: TypeCap, RiskSet : ScriptableObject
- 메서드: CanToggle, SumPoints, ComputeTargetSeconds
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 1개: Assets/Scripts/Gameplay/Progression/Risk/RiskSession.cs

## Assets/Scripts/Gameplay/Progression/Stage/LegacyDefinitions/NewMonoBehaviourScript.cs
- 분류: WIP/Legacy 경로; 28행
- 타입: StageLevel, GameStage : ScriptableObject
- 메서드: 
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 0개: 

## Assets/Scripts/Gameplay/Progression/Stage/StageProgressionManager.cs
- 분류: 일반 소스; 1413행
- 타입: StageProgressionManager : MonoBehaviour, SectorJudgePhase
- 메서드: Awake, OnEnable, OnDisable, InitializeInitialHomeEntry, RequestHomeSceneForInitialEntry, OnDirectHomeSceneLoaded, Update, CompleteCurrentSector, TrySkipActiveSector, RecoverInfectionControlOnBattleRoomClear, CompleteStageAfterGoalRoomClear, RaiseStageCompleteRewards, TryCompleteStageFromBossExit, TryExtractToHomeFromNormalExit, CompleteFinalStage, IsStageUnlocked, TryUnlockStageFromQuest, TryGetNextUnlockedStage, EnterHideout, TryEnterUnlockedStage, BeginStage, SnapCameraToPlayer, SetActiveSceneForSector, PersistHighestUnlockedStage, DebugTrySetHighestUnlockedStage, DebugTryBeginStage, DebugTryBeginNextStage, SyncActiveSector, BeginSector, IsRequirementMet, PublishSnapshot, HandleSectorStateManagerReady, OnPlayerHitReceived, RecordCurrentStageResultForTreasureRoll, ShouldUseTimedNormalBattleJudge, ShouldCompleteEncounterOnEnemyClear, TryCompleteEncounterByEnemyClear, TickNormalBattleJudge, ApplyNormalBattleTimerExpiredPenalty, ResolveNormalBattleJudge, BeginSuccessCountdown, TickSuccessCountdown, BeginFailureCountdown, TickFailureCountdown, ResolveSuccessCountdown, ResolveFailureCountdown, TickLegacyStartSectorTimer, ShouldShowPlayerTimerForSector, IsTimedNormalSector, IsEnemyClearEncounterRoom
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 1개: Assets/Scenes/RUNScene.unity
- 타입 이름을 참조하는 소스 10개: Assets/Scripts/Authoring/Editor/DebugMonitorWindow.cs; Assets/Scripts/Authoring/Editor/DebugMonitorWindow.Operations.cs; Assets/Scripts/Core/SceneFlow/RuntimeSceneDependencyValidator.cs; Assets/Scripts/Core/SceneFlow/SceneLoader.cs; Assets/Scripts/Gameplay/Progression/Quests/QuestRuntime.cs; Assets/Scripts/Gameplay/World/Sectors/Runtime/SectorPortalManager.cs; Assets/Scripts/Gameplay/World/Sectors/Runtime/SectorStateManager.cs; Assets/Scripts/Gameplay/World/Sectors/SectorSkipStation.cs

## Assets/Scripts/Gameplay/SpawnSystem.cs
- 분류: 일반 소스; 173행
- 타입: SpawnSystem : MonoBehaviour
- 메서드: Awake, OnEnable, OnDisable, SpawnPlayer, HandleStartSectorReady, ResolveStartSector, ResolveSpawnLocation, ResolveSpawnParent, MovePlayerTo, ResolveSpawnPosition
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 1개: Assets/Scenes/RUNScene.unity
- 타입 이름을 참조하는 소스 2개: Assets/Scripts/Core/SceneFlow/SceneLoader.cs; Assets/Scripts/Gameplay/Items/World/WorldItemPickupSpawner.cs

## Assets/Scripts/Gameplay/World/Sectors/Content/SectorContentProfileSO.cs
- 분류: 일반 소스; 186행
- 타입: SectorContentSlotKind, WeightedSectorContentPrefab, SectorContentSlotDefinition, SectorContentProfileSO : ScriptableObject
- 메서드: TryRollPrefab, HasValidCandidate, GetOrCreateSlotDefinition, TryGetSlotDefinition, TryRollContentForSlot
- 직렬화 참조 3개 / 지정 씬 참조 그래프 내 0개: Assets/ScriptableObject/GameScene/ContentTemplates/Sector/ContentProfile_NormalLoot_Template.asset; Assets/ScriptableObject/GameScene/ContentTemplates/Sector/ContentProfile_RareCache_Template.asset; Assets/ScriptableObject/GameScene/ContentTemplates/Sector/ContentProfile_SpecialLoot_Template.asset
- 타입 이름을 참조하는 소스 7개: Assets/Scripts/Authoring/Editor/ContentPreflightWindow.cs; Assets/Scripts/Authoring/Editor/ContentTemplateInspectors.cs; Assets/Scripts/Authoring/Editor/SectorMakerWindow.cs; Assets/Scripts/Gameplay/World/Sectors/Content/SectorContentSlotAnchor.cs; Assets/Scripts/Gameplay/World/Sectors/Content/SectorContentSlotSpawner.cs; Assets/Scripts/Gameplay/World/Sectors/ScriptableObject/SectorPresetSO.cs; Assets/Scripts/Gameplay/World/Sectors/StageMap/RaidRuntimeSettings.cs

## Assets/Scripts/Gameplay/World/Sectors/Content/SectorContentSlotAnchor.cs
- 분류: 일반 소스; 61행
- 타입: SectorContentSlotAnchor : MonoBehaviour
- 메서드: ConfigureForAuthoring, OnValidate
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 7개: Assets/Scripts/Authoring/Editor/ContentPreflightWindow.cs; Assets/Scripts/Authoring/Editor/ContentTemplateInspectors.cs; Assets/Scripts/Authoring/Editor/SectorMakerWindow.cs; Assets/Scripts/Gameplay/World/Sectors/Content/SectorContentProfileSO.cs; Assets/Scripts/Gameplay/World/Sectors/Content/SectorContentSlotSpawner.cs; Assets/Scripts/Gameplay/World/Sectors/ScriptableObject/SectorPresetSO.cs; Assets/Scripts/Gameplay/World/Sectors/SectorRuntime.cs

## Assets/Scripts/Gameplay/World/Sectors/Content/SectorContentSlotSpawner.cs
- 분류: 일반 소스; 172행
- 타입: SectorContentRuntimeInstance : MonoBehaviour, SectorContentSlotSpawner
- 메서드: Initialize, Populate, ClearGeneratedContent, CompareAnchors, BuildSlotSeed
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 3개: Assets/Scripts/Core/SceneFlow/SceneLoader.cs; Assets/Scripts/Gameplay/World/Sectors/Content/SectorContentSlotAnchor.cs; Assets/Scripts/Gameplay/World/Sectors/StageMap/StageSectorInstantiator.cs

## Assets/Scripts/Gameplay/World/Sectors/EnemySpawnPoint.cs
- 분류: 일반 소스; 35행
- 타입: EnemySpawnPoint : MonoBehaviour
- 메서드: OnDrawGizmos
- 직렬화 참조 5개 / 지정 씬 참조 그래프 내 4개: Assets/Prefabs/GameScene/Sector/SectorBattlePreset/Sector_BattlePreset_1.prefab; Assets/Prefabs/GameScene/Sector/SectorBattlePreset/Sector_BattlePreset_2.prefab; Assets/Prefabs/GameScene/Sector/SectorBattlePreset/Sector_BattlePreset_3.prefab; Assets/Prefabs/GameScene/Sector/SectorBattlePreset/Sector_BattlePreset_4.prefab; Assets/Prefabs/GameScene/Sector/SectorBattlePreset/Sector_BattlePreset_5.prefab
- 타입 이름을 참조하는 소스 3개: Assets/Scripts/Authoring/Editor/SectorMakerWindow.cs; Assets/Scripts/Gameplay/World/Sectors/SectorEnemySpawner.cs; Assets/Scripts/Gameplay/World/Sectors/SectorRuntime.cs

## Assets/Scripts/Gameplay/World/Sectors/Named_Boss/NamedBattleManager.cs
- 분류: 일반 소스; 91행
- 타입: NamedBattleManager : MonoBehaviour
- 메서드: Awake, OnEnable, OnDisable, HandleSectorStateManagerReady, HandleBattleStarted, HandleBattleEnded
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 1개: Assets/Scenes/RUNScene.unity
- 타입 이름을 참조하는 소스 0개: 

## Assets/Scripts/Gameplay/World/Sectors/Named_Boss/NamedOutsideController.cs
- 분류: 일반 소스; 87행
- 타입: NamedOutsideController : MonoBehaviour
- 메서드: OnEnable, OnDisable, Update, HandleNamedSectorPhaseChanged, TickOutsidePressure
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 0개: Assets/Prefabs/GameScene/Sector/Stage1/Sector_Named.prefab
- 타입 이름을 참조하는 소스 0개: 

## Assets/Scripts/Gameplay/World/Sectors/Named_Boss/NamedRewardApplier.cs
- 분류: 일반 소스; 251행
- 타입: NamedRewardApplier : MonoBehaviour
- 메서드: Reset, Awake, OnEnable, OnDisable, ApplyReward, HandleSectorStateManagerReady, HandleMaskRenderManagerReady, HandlePlayerInfectionReady, ApplySectorRewards, TryApplySectorReward, ApplyPlayerPaintFull, ApplyPlayerOccupancyFull, RecoverInfectionControl, RecoverPlayerInfection, HandleInfectionControlManagerReady, HandleNamedSectorControllerReady
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 0개: Assets/Prefabs/GameScene/Sector/Stage1/Sector_Named.prefab
- 타입 이름을 참조하는 소스 0개: 

## Assets/Scripts/Gameplay/World/Sectors/Named_Boss/NamedSectorController.cs
- 분류: 일반 소스; 699행
- 타입: NamedSectorRuntimeUnityEvent : UnityEvent<SectorRuntime>, NamedSectorController : MonoBehaviour
- 메서드: Reset, Awake, OnEnable, OnDisable, Update, StartReservationTimer, ForceReserveNow, ForcePresentNow, RequestStartBattle, TryStartBattleFromPortal, NotifyNamedKilled, ConfirmNamedRewardAndEndBattle, ResetForStageTransition, HandleNamedEnemyKilled, HandleSectorStateManagerReady, HandleStageApplied, TryStartFirstCycle, HandlePlayerEnteredSector, ReserveRandomSector, PresentNamedSector, StartBattle, StartBattleRoutine, PrepareBattleWhileCovered, CompleteBattleStart, FinishBattleRoutine, ApplyBattleEndWhileCovered, CompleteBattleEnd, SpawnNamed, BindEnemyStatConfig, ResolveCurrentStage, TryPickRandomOpenedSector, TryPickStageGoalSector, SetPhase, PublishTimerSnapshot, IsTimerPhase, TickTimerSnapshot, TickDebugLog
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 0개: Assets/Prefabs/GameScene/Sector/Stage1/Sector_Named.prefab
- 타입 이름을 참조하는 소스 3개: Assets/Scripts/Core/Events/ScriptableObject/NamedSectorControllerReadyEventChannelSO.cs; Assets/Scripts/Gameplay/World/Sectors/Named_Boss/NamedRewardApplier.cs; Assets/Scripts/WIP/Legacy/Gameplay/NamedBoss/BossRewardResolver.cs

## Assets/Scripts/Gameplay/World/Sectors/Named_Boss/NamedSectorOccupancyProjection.cs
- 분류: 일반 소스; 100행
- 타입: NamedSectorOccupancyProjection : MonoBehaviour
- 메서드: OnEnable, OnDisable, HandleNamedSectorPhaseChanged, HandleOccupancyChanged, PublishCurrentProjection, PublishProjection
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 0개: 

## Assets/Scripts/Gameplay/World/Sectors/Named_Boss/NamedSectorTransitionController.cs
- 분류: 일반 소스; 185행
- 타입: NamedSectorTransitionController : MonoBehaviour, request
- 메서드: OnEnable, OnDisable, Reset, HandlePlayerRuntimeReady, PlayEnterTransition, PlayExitTransition, PlayTransition, TeleportToBattleSector, TryResolvePlayerRoot, TeleportBackToSourceSector, TeleportPlayer
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 0개: Assets/Prefabs/GameScene/Sector/Stage1/Sector_Named.prefab
- 타입 이름을 참조하는 소스 205개: Assets/Editor/ItemCategoryMoveUtility.cs; Assets/Editor/ItemMakerWindow.cs; Assets/Scripts/Authoring/Editor/DebugMonitorWindow.cs; Assets/Scripts/Authoring/Editor/DebugMonitorWindow.Operations.cs; Assets/Scripts/Authoring/Editor/SectorMakerWindow.cs; Assets/Scripts/Authoring/RendererBoundsColliderFitter.cs; Assets/Scripts/Core/Events/ScriptableObject/BossRewardRequestEventChannelSO.cs; Assets/Scripts/Core/Events/ScriptableObject/DamageNumberEventChannelSO.cs

## Assets/Scripts/Gameplay/World/Sectors/Named_Boss/SectorNamedStateApplier.cs
- 분류: 일반 소스; 114행
- 타입: SectorNamedStateApplier : MonoBehaviour
- 메서드: OnEnable, OnDisable, SetReserved, SetPresented, ClearNamedState, HandleMaskRenderManagerReady, ClearSectorPaint, GetOccupancy, GetSectorName
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 0개: Assets/Prefabs/GameScene/Sector/Stage1/Sector_Named.prefab
- 타입 이름을 참조하는 소스 1개: Assets/Scripts/Gameplay/World/Sectors/Named_Boss/NamedSectorController.cs

## Assets/Scripts/Gameplay/World/Sectors/Portal/PortalDirectionUtility.cs
- 분류: 일반 소스; 61행
- 타입: SectorPortalDirection, SectorPortalDirectionUtility
- 메서드: ToCoordOffset, Opposite
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 6개: Assets/Scripts/Gameplay/World/Sectors/Portal/SectorPortal.cs; Assets/Scripts/Gameplay/World/Sectors/Runtime/SectorPortalManager.cs; Assets/Scripts/Gameplay/World/Sectors/StageMap/StageMapGenerator.cs; Assets/Scripts/Gameplay/World/Sectors/StageMap/StageMapLayout.cs; Assets/Scripts/Presentation/UI/Gameplay/Map/MiniMap/SectorTacticalMiniMapGraphic.cs; Assets/Tests/EditMode/Editor/RaidMapFoundationTests.cs

## Assets/Scripts/Gameplay/World/Sectors/Portal/SectorEdge.cs
- 분류: 일반 소스; 17행
- 타입: SectorEdge
- 메서드: 
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 3개: Assets/Scripts/Gameplay/World/Sectors/Runtime/SectorPortalManager.cs; Assets/Scripts/Gameplay/World/Sectors/SectorRuntime.cs; Assets/Scripts/Presentation/UI/Gameplay/Map/MiniMap/SectorTacticalMiniMapGraphic.cs

## Assets/Scripts/Gameplay/World/Sectors/Portal/SectorPortal.cs
- 분류: 일반 소스; 109행
- 타입: SectorPortal : MonoBehaviour
- 메서드: Reset, Initialize, SetLink, SetExtraction, SetAvailable, TryInteract
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 1개: Assets/Prefabs/GameScene/Article/Portal/Portal.prefab
- 타입 이름을 참조하는 소스 4개: Assets/Scripts/Gameplay/Entities/Shooter/Common/Core/ShooterStuckRecovery.cs; Assets/Scripts/Gameplay/Interaction/Core/InteractionManager.cs; Assets/Scripts/Gameplay/World/Sectors/Runtime/SectorPortalManager.cs; Assets/Scripts/Presentation/UI/Gameplay/Map/MiniMap/SectorTacticalMiniMapGraphic.cs

## Assets/Scripts/Gameplay/World/Sectors/Runtime/SectorOccupancyManager.cs
- 분류: 일반 소스; 480행
- 타입: SectorOccupancySummary, SectorOccupancyManager : MonoBehaviour
- 메서드: Awake, OnEnable, OnDisable, OnSectorStateManagerReady, CacheInitialSnapshots, OnCurrentSectorChanged, OnSectorOccupancyChanged, PublishSummary, PublishMapSnapshot, RefreshRaidMarkers, AddMissingRaidMapCells, AddStartSectorMapCellIfMissing, ResolveIsRevealed, IsStartSector, GetSectorCoord, ShouldHideFromMap, ResolveRoomType, OnNamedSectorPhaseChanged, GetMapSnapshotCenterCoord, ShouldPinMiniMapToNamedSource, ShouldReleaseMiniMapNamedPin
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 1개: Assets/Scenes/RUNScene.unity
- 타입 이름을 참조하는 소스 6개: Assets/Scripts/Core/Events/ScriptableObject/SectorOccupancySummaryEventChannelSO.cs; Assets/Scripts/Gameplay/Combat/Infection/InfectionControlManager.cs; Assets/Scripts/Gameplay/Combat/Infection/ScriptableObject/InfectionControlRulesSO.cs; Assets/Scripts/Gameplay/World/Sectors/StageMap/RaidRuntime.cs; Assets/Scripts/Presentation/UI/Gameplay/Map/FullMap/FullMapUI.cs; Assets/Tests/EditMode/Editor/RaidRuntimeContractTests.cs

## Assets/Scripts/Gameplay/World/Sectors/Runtime/SectorPortalManager.cs
- 분류: 일반 소스; 733행
- 타입: SectorPortalManager : MonoBehaviour
- 메서드: Awake, OnEnable, OnDisable, Start, OnSectorOpened, CacheSectors, RebuildPortalLinks, BindPortals, BindPortal, HasDeclaredStageConnection, RefreshAllPortals, RefreshPortal, CanUsePortal, TryMoveThroughPortal, IsStartHubStageEntryPortal, IsExtractionPortal, TryStartBossEntryTransition, PlayBossEntryTransition, TryStartBossExitTransition, PlayBossExitTransition, MovePlayerToHideout, MovePlayerThroughPortalNow, ResolvePlayerRoot, GetPortal, GetEdge, IsBossSector, IsCurrentGoalSector, IsBossExitPortal, HandleSectorStateManagerReady
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 1개: Assets/Scenes/RUNScene.unity
- 타입 이름을 참조하는 소스 2개: Assets/Scripts/Gameplay/World/Sectors/Portal/SectorPortal.cs; Assets/Scripts/Gameplay/World/Sectors/StageMap/StageSectorInstantiator.cs

## Assets/Scripts/Gameplay/World/Sectors/Runtime/SectorStateManager.cs
- 분류: 일반 소스; 862행
- 타입: SectorStateManager : MonoBehaviour, RuntimeSectorStateEntry
- 메서드: SetPlayerCurrentSector, SetExternalCurrentSector, OnEnable, OnDisable, Awake, Start, EnsureInitialized, InitializeGeneratedMapState, ApplyInitialOpenState, CloseAllSectors, ProgressNextStage, ApplyStage, ConfigureStageMap, RegisterGeneratedStageMap, BroadcastGeneratedStartSectorReady, OpenSector, SetSectorOpened, CompleteSector, OpenAdjacentSectors, OpenConnectedRoom, OpenSectorAt, HandleCurrentSectorChanged, OpenAdjacentSectorsForFreePassNormalSector, CompleteNonCombatSectorOnEntry, RevealSector, TryGetSector, TryGetSectorCoord, IsManagedSector, IsActiveStageSector, TryGetStageRoomType, TryGetStageGoalSector, GetSectorCoord, IsStartSector, IsStartSectorCoord, IsOpened, TryGetSectorRevealed, FailSector, IsSectorFailed, ClearSectorFailure, DebugTryEnterSector, RefreshRuntimeSectorStates, OnValidate
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 1개: Assets/Scenes/RUNScene.unity
- 타입 이름을 참조하는 소스 25개: Assets/Scripts/Authoring/Editor/DebugMonitorWindow.cs; Assets/Scripts/Authoring/Editor/DebugMonitorWindow.Operations.cs; Assets/Scripts/Core/Events/ScriptableObject/SectorStateManagerReadyEventChannelSO.cs; Assets/Scripts/Core/SceneFlow/RuntimeSceneDependencyValidator.cs; Assets/Scripts/Core/SceneFlow/SceneLoader.cs; Assets/Scripts/Gameplay/Combat/Infection/InfectionControlManager.cs; Assets/Scripts/Gameplay/Items/Loot/LootProgressionRuntime.cs; Assets/Scripts/Gameplay/Progression/Stage/StageProgressionManager.cs

## Assets/Scripts/Gameplay/World/Sectors/ScriptableObject/EnemyStageObjectConfigSO.cs
- 분류: 일반 소스; 15행
- 타입: EnemyStageObjectConfigSO : StageObjectConfigSO
- 메서드: 
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 2개: Assets/Scripts/Gameplay/World/Sectors/ScriptableObject/EnemyStageObjectRoomPresetSO.cs; Assets/Scripts/WIP/Legacy/Gameplay/Sectors/SectorObjectConfigSO.cs

## Assets/Scripts/Gameplay/World/Sectors/ScriptableObject/EnemyStageObjectRoomPresetSO.cs
- 분류: 일반 소스; 57행
- 타입: EnemyStageObjectRoomPresetSO : ScriptableObject, ObjectSpawn
- 메서드: 
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 1개: Assets/Scripts/Gameplay/World/Sectors/ScriptableObject/PlayerStageObjectRoomPresetSO.cs

## Assets/Scripts/Gameplay/World/Sectors/ScriptableObject/NormalSectorPolicySO.cs
- 분류: 일반 소스; 88행
- 타입: NormalSectorEncounterPolicy, NormalSectorPortalPolicy, NormalSectorTimerPolicy, NormalSectorPolicySO : ScriptableObject
- 메서드: ResolveUsesEnemyEncounter, ResolveRequiresEnemyClearForProgression, ResolveAllowsPortalTravelBeforeClear, ResolveUsesStageDefaultTimer
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 4개: Assets/Scripts/Gameplay/World/Sectors/ScriptableObject/SectorPresetSO.cs; Assets/Scripts/Gameplay/World/Sectors/ScriptableObject/StageDefinitionSO.cs; Assets/Scripts/Gameplay/World/Sectors/SectorRuntime.cs; Assets/Scripts/Gameplay/World/Sectors/StageMap/StageSectorInstantiator.cs

## Assets/Scripts/Gameplay/World/Sectors/ScriptableObject/PlayerStageObjectConfigSO.cs
- 분류: 일반 소스; 15행
- 타입: PlayerStageObjectConfigSO : StageObjectConfigSO
- 메서드: 
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 2개: Assets/Scripts/Gameplay/World/Sectors/ScriptableObject/PlayerStageObjectRoomPresetSO.cs; Assets/Scripts/WIP/Legacy/Gameplay/Sectors/SectorObjectConfigSO.cs

## Assets/Scripts/Gameplay/World/Sectors/ScriptableObject/PlayerStageObjectRoomPresetSO.cs
- 분류: 일반 소스; 57행
- 타입: PlayerStageObjectRoomPresetSO : ScriptableObject, ObjectSpawn
- 메서드: 
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 1개: Assets/Scripts/Gameplay/World/Sectors/ScriptableObject/EnemyStageObjectRoomPresetSO.cs

## Assets/Scripts/Gameplay/World/Sectors/ScriptableObject/SectorBattlePresetSO.cs
- 분류: 일반 소스; 14행
- 타입: SectorBattlePresetSO : SectorPresetSO
- 메서드: 
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 2개: Assets/Scripts/Gameplay/World/Sectors/ScriptableObject/SectorBossBattlePresetSO.cs; Assets/Scripts/Gameplay/World/Sectors/ScriptableObject/SectorNormalBattlePresetSO.cs

## Assets/Scripts/Gameplay/World/Sectors/ScriptableObject/SectorBossBattlePresetSO.cs
- 분류: 일반 소스; 24행
- 타입: SectorBossBattlePresetSO : SectorBattlePresetSO
- 메서드: OnValidate
- 직렬화 참조 6개 / 지정 씬 참조 그래프 내 5개: Assets/ScriptableObject/GameScene/ContentTemplates/Sector/BossBattlePreset_Template.asset; Assets/ScriptableObject/GameScene/Stage/Sector/Preset/BossPreset/SectorPreset_Boss_Stage1.asset; Assets/ScriptableObject/GameScene/Stage/Sector/Preset/BossPreset/SectorPreset_Boss_Stage2.asset; Assets/ScriptableObject/GameScene/Stage/Sector/Preset/BossPreset/SectorPreset_Boss_Stage3.asset; Assets/ScriptableObject/GameScene/Stage/Sector/Preset/BossPreset/SectorPreset_Boss_Stage4.asset; Assets/ScriptableObject/GameScene/Stage/Sector/Preset/BossPreset/SectorPreset_Boss_Stage5.asset
- 타입 이름을 참조하는 소스 3개: Assets/Scripts/Authoring/Editor/ContentTemplateInspectors.cs; Assets/Scripts/Gameplay/World/Sectors/ScriptableObject/StageDefinitionSO.cs; Assets/Scripts/Gameplay/World/Sectors/StageMap/StageSectorInstantiator.cs

## Assets/Scripts/Gameplay/World/Sectors/ScriptableObject/SectorExclusionRulesSO.cs
- 분류: 일반 소스; 74행
- 타입: SectorExclusionRulesSO : ScriptableObject, Entry
- 메서드: ExcludeFromEnemySpawn, ExcludeFromNamedReservation, ExcludeFromOccupancySummary, HideFromMiniMapAlways, TryGetEntry
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 1개: Assets/ScriptableObject/GameScene/Stage/Sector/SectorExclusionRules.asset
- 타입 이름을 참조하는 소스 14개: Assets/Editor/LootProgressionEditorWindow.cs; Assets/Scripts/Authoring/Editor/SectorMakerWindow.cs; Assets/Scripts/Gameplay/Combat/PaintMark/MaskRenderTableSO.cs; Assets/Scripts/Gameplay/Progression/Stage/StageProgressionManager.cs; Assets/Scripts/Gameplay/World/Sectors/Named_Boss/NamedSectorController.cs; Assets/Scripts/Gameplay/World/Sectors/Runtime/SectorOccupancyManager.cs; Assets/Scripts/Gameplay/World/Sectors/SectorEnemySpawner.cs; Assets/Scripts/Gameplay/World/Sectors/StageMap/StageTopologySO.cs

## Assets/Scripts/Gameplay/World/Sectors/ScriptableObject/SectorNormalBattlePresetSO.cs
- 분류: 일반 소스; 24행
- 타입: name, SectorNormalBattlePresetSO : SectorBattlePresetSO
- 메서드: OnValidate
- 직렬화 참조 9개 / 지정 씬 참조 그래프 내 5개: Assets/ScriptableObject/GameScene/ContentTemplates/Sector/NormalBattlePreset_Template.asset; Assets/ScriptableObject/GameScene/ContentTemplates/Sector/RoomPreset_NormalLoot_Template.asset; Assets/ScriptableObject/GameScene/ContentTemplates/Sector/RoomPreset_RareCache_Template.asset; Assets/ScriptableObject/GameScene/ContentTemplates/Sector/RoomPreset_SpecialLoot_Template.asset; Assets/ScriptableObject/GameScene/Stage/Sector/Preset/BattleMapPreset/SectorBattlePreset_1.asset; Assets/ScriptableObject/GameScene/Stage/Sector/Preset/BattleMapPreset/SectorBattlePreset_2.asset; Assets/ScriptableObject/GameScene/Stage/Sector/Preset/BattleMapPreset/SectorBattlePreset_3.asset; Assets/ScriptableObject/GameScene/Stage/Sector/Preset/BattleMapPreset/SectorBattlePreset_4.asset
- 타입 이름을 참조하는 소스 206개: Assets/Editor/InventorySortButtonMigration.cs; Assets/Editor/ItemCategoryMoveUtility.cs; Assets/Editor/ItemMakerWindow.cs; Assets/Editor/LootProgressionEditorWindow.cs; Assets/Editor/QuestDesignTreeWindow.cs; Assets/Editor/ShooterCustomizeStationWindow.cs; Assets/Editor/ShooterMeleeAnimationSetup.cs; Assets/Editor/ShopProgressionTreeWindow.cs

## Assets/Scripts/Gameplay/World/Sectors/ScriptableObject/SectorPresetSO.cs
- 분류: 일반 소스; 58행
- 타입: SectorPresetSO : ScriptableObject
- 메서드: SetContentProfile, SetSerializedRoomType
- 직렬화 참조 4개 / 지정 씬 참조 그래프 내 3개: Assets/ScriptableObject/GameScene/Stage/Sector/Preset/ShopPreset/SectorPreset_Shop.asset; Assets/ScriptableObject/GameScene/Stage/Sector/Preset/StartPreset/SectorPreset_Home.asset; Assets/ScriptableObject/GameScene/Stage/Sector/Preset/StartPreset/SectorPreset_Start.asset; Assets/ScriptableObject/GameScene/Stage/Sector/Preset/TreasurePreset/SectorPreset_Treasure.asset
- 타입 이름을 참조하는 소스 8개: Assets/Scripts/Authoring/Editor/ContentPreflightWindow.cs; Assets/Scripts/Authoring/Editor/ContentTemplateInspectors.cs; Assets/Scripts/Authoring/Editor/SectorMakerWindow.cs; Assets/Scripts/Gameplay/World/Sectors/ScriptableObject/SectorBattlePresetSO.cs; Assets/Scripts/Gameplay/World/Sectors/ScriptableObject/StageDefinitionSO.cs; Assets/Scripts/Gameplay/World/Sectors/SectorRuntime.cs; Assets/Scripts/Gameplay/World/Sectors/StageMap/RaidRuntimeSettings.cs; Assets/Scripts/Gameplay/World/Sectors/StageMap/StageSectorInstantiator.cs

## Assets/Scripts/Gameplay/World/Sectors/ScriptableObject/SectorRulesSO.cs
- 분류: 일반 소스; 47행
- 타입: SectorRulesSO : ScriptableObject
- 메서드: 
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 1개: Assets/ScriptableObject/GameScene/Stage/Sector/SectorRules.asset
- 타입 이름을 참조하는 소스 2개: Assets/Scripts/Gameplay/Progression/Stage/StageProgressionManager.cs; Assets/Scripts/Gameplay/World/Sectors/SectorOccupancy.cs

## Assets/Scripts/Gameplay/World/Sectors/ScriptableObject/StageBattleEncounterPresetSO.cs
- 분류: 일반 소스; 152행
- 타입: StageBattleEncounterPresetSO : ScriptableObject, EnemyWavePresetCandidate, EncounterWave
- 메서드: TryPickEnemyWavePreset, IsWaveValid, IsCandidateValid
- 직렬화 참조 12개 / 지정 씬 참조 그래프 내 11개: Assets/ScriptableObject/GameScene/ContentTemplates/Sector/Encounter_Template.asset; Assets/ScriptableObject/GameScene/Stage/EnemyWavePreset/Boss/Stage1PunisherEncounter.asset; Assets/ScriptableObject/GameScene/Stage/EnemyWavePreset/Boss/Stage1TempBossEncounter.asset; Assets/ScriptableObject/GameScene/Stage/EnemyWavePreset/Boss/Stage2TempBossEncounter.asset; Assets/ScriptableObject/GameScene/Stage/EnemyWavePreset/Boss/Stage3TempBossEncounter.asset; Assets/ScriptableObject/GameScene/Stage/EnemyWavePreset/Boss/Stage4TempBossEncounter.asset; Assets/ScriptableObject/GameScene/Stage/EnemyWavePreset/Boss/Stage5TempBossEncounter.asset; Assets/ScriptableObject/GameScene/Stage/EnemyWavePreset/Stage1 BattleEncounterPreset/Stage1BattleEncounterPreset.asset
- 타입 이름을 참조하는 소스 6개: Assets/Scripts/Authoring/Editor/ContentTemplateInspectors.cs; Assets/Scripts/Authoring/Editor/SectorMakerWindow.cs; Assets/Scripts/Gameplay/World/Sectors/ScriptableObject/StageDefinitionSO.cs; Assets/Scripts/Gameplay/World/Sectors/SectorEnemySpawner.cs; Assets/Scripts/Gameplay/World/Sectors/StageMap/RaidRuntimeSettings.cs; Assets/Scripts/Gameplay/World/Sectors/StageMap/StageSectorInstantiator.cs

## Assets/Scripts/Gameplay/World/Sectors/ScriptableObject/StageDefinitionSO.cs
- 분류: 일반 소스; 882행
- 타입: StageDefinitionSO : ScriptableObject, WeightedSectorPreset, WeightedEncounterPreset, BossBattleEntry, WeightedBranchTopology, NormalBattleSettings, BiochemicalGasSettings, TreasureSettings
- 메서드: AppliesTo, CreateTreasureRoomGenerationSettings, TryPickSectorPreset, TryPickBranchTopology, TryValidateBranchTopologyPool, AddOrUpdateBranchTopology, TryPickNormalSectorEncounter, TryPickNormalBattleEncounter, HasNormalSectorThatUsesEnemyEncounter, TryPickBossBattle, BuildSectorSeed, TryPickWeightedSector, TryPickWeightedEncounter, IsSectorCandidateValid, IsEncounterCandidateValid, IsBossBattleValid, DescribeBossBattleInvalidReason, DescribeEncounterInvalidReason, DescribeEnemyWaveInvalidReason, IsBranchTopologyCandidateValid, OnValidate, ValidateSectorPool
- 직렬화 참조 5개 / 지정 씬 참조 그래프 내 5개: Assets/ScriptableObject/GameScene/Stage/Stage1Definition.asset; Assets/ScriptableObject/GameScene/Stage/Stage2Definition.asset; Assets/ScriptableObject/GameScene/Stage/Stage3Definition.asset; Assets/ScriptableObject/GameScene/Stage/Stage4Definition.asset; Assets/ScriptableObject/GameScene/Stage/Stage5Definition.asset
- 타입 이름을 참조하는 소스 21개: Assets/Editor/QuestDesignTreeWindow.cs; Assets/Scripts/Authoring/Editor/ContentPreflightWindow.cs; Assets/Scripts/Authoring/Editor/DebugMonitorWindow.Operations.cs; Assets/Scripts/Authoring/Editor/SectorMakerWindow.cs; Assets/Scripts/Authoring/Editor/StageDefinitionRaidEditor.cs; Assets/Scripts/Core/SceneFlow/SceneLoader.cs; Assets/Scripts/Gameplay/Combat/Infection/InfectionControlManager.cs; Assets/Scripts/Gameplay/Progression/Quests/QuestDefinitionSO.cs

## Assets/Scripts/Gameplay/World/Sectors/ScriptableObject/StageEnemyWavePresetSO.cs
- 분류: 일반 소스; 69행
- 타입: StageEnemyWavePresetSO : ScriptableObject, EnemySpawn
- 메서드: IsSpawnValid
- 직렬화 참조 29개 / 지정 씬 참조 그래프 내 25개: Assets/ScriptableObject/GameScene/Stage/EnemyWavePreset/3Roller.asset; Assets/ScriptableObject/GameScene/Stage/EnemyWavePreset/3Worm.asset; Assets/ScriptableObject/GameScene/Stage/EnemyWavePreset/Boss/Stage1BossWave.asset; Assets/ScriptableObject/GameScene/Stage/EnemyWavePreset/Boss/Stage1PunisherWave.asset; Assets/ScriptableObject/GameScene/Stage/EnemyWavePreset/Boss/Stage2BossWave.asset; Assets/ScriptableObject/GameScene/Stage/EnemyWavePreset/Boss/Stage3BossWave.asset; Assets/ScriptableObject/GameScene/Stage/EnemyWavePreset/Boss/Stage4BossWave.asset; Assets/ScriptableObject/GameScene/Stage/EnemyWavePreset/Boss/Stage5BossWaveTemplate.asset
- 타입 이름을 참조하는 소스 4개: Assets/Scripts/Authoring/Editor/SectorMakerWindow.cs; Assets/Scripts/Gameplay/World/Sectors/ScriptableObject/StageBattleEncounterPresetSO.cs; Assets/Scripts/Gameplay/World/Sectors/ScriptableObject/StageDefinitionSO.cs; Assets/Scripts/Gameplay/World/Sectors/SectorEnemySpawner.cs

## Assets/Scripts/Gameplay/World/Sectors/ScriptableObject/StageObjectConfigSO.cs
- 분류: 일반 소스; 44행
- 타입: StageObjectConfigSO : ScriptableObject
- 메서드: ResolveSpawnRotation
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 3개: Assets/Scripts/Gameplay/World/Sectors/ScriptableObject/EnemyStageObjectConfigSO.cs; Assets/Scripts/Gameplay/World/Sectors/ScriptableObject/PlayerStageObjectConfigSO.cs; Assets/Scripts/WIP/Legacy/Gameplay/Sectors/SectorObjectConfigSO.cs

## Assets/Scripts/Gameplay/World/Sectors/ScriptableObject/StageRunFlowSO.cs
- 분류: 일반 소스; 120행
- 타입: StageRunFlowSO : ScriptableObject
- 메서드: TryGetStage, TryGetNextStage, HasNextStage, IsFinalStage, OnValidate
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 1개: Assets/ScriptableObject/GameScene/Stage/StageRunFlow.asset
- 타입 이름을 참조하는 소스 3개: Assets/Scripts/Authoring/Editor/ContentPreflightWindow.cs; Assets/Scripts/Core/Persistence/Save.cs; Assets/Scripts/Gameplay/Progression/Stage/StageProgressionManager.cs

## Assets/Scripts/Gameplay/World/Sectors/SectorCleanupApplier.cs
- 분류: 일반 소스; 15행
- 타입: SectorCleanupApplier : MonoBehaviour
- 메서드: 
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 0개: 

## Assets/Scripts/Gameplay/World/Sectors/SectorEnemySpawner.cs
- 분류: 일반 소스; 1159행
- 타입: SectorEnemySpawner : MonoBehaviour
- 메서드: CancelEncounterForSectorSkip, ConfigureEncounter, ResolveConfiguredEncounterAfterStageMapRegistered, Reset, Awake, OnEnable, OnDisable, Update, OnSectorOpened, OnStageProgressChanged, ApplyStageSnapshot, TryRefreshResolvedSettings, RefreshResolvedSettings, SelectDeterministicPresets, TryPickEncounterPreset, ResolveEnemyWavePresetPlan, ValidateResolvedEnemyRoles, ResolveStageSeed, GetEncounterWaveCount, ResetEncounterState, EnsureEncounterStarted, TickEnemyEncounterWaves, EnqueueEnemyWavePreset, TickPendingEnemySpawnQueue, RefreshEncounterSpawnFinished, RefreshEncounterCompletion, CanRunEncounter, UsesEnemyEncounter, RefreshSectorActivity, TrySpawnOne, BindShooterEnemyConfig, BindProjectileRoot, TryGetEnemySpawnPoint, TrackEnemy, OnTrackedEnemyDied, CleanupTrackedEnemies, TrackEncounterTarget, OnTrackedEncounterTargetDied, CleanupEncounterTargets, UntrackEncounterTarget, RefreshAliveEncounterTargetDebugCount, RemoveDestroyedEnemyAt, UntrackEnemy, UnbindAllTrackedEnemies, HandleSectorStateManagerReady, LogPendingResolveOnce, LogResolveSummaryOnce
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 1개: Assets/Prefabs/GameScene/Sector/Sector_Base.prefab
- 타입 이름을 참조하는 소스 4개: Assets/Scripts/Authoring/Editor/DebugMonitorWindow.Encounter.cs; Assets/Scripts/Gameplay/Progression/Stage/StageProgressionManager.cs; Assets/Scripts/Gameplay/World/Sectors/SectorRuntime.cs; Assets/Scripts/Gameplay/World/Sectors/StageMap/StageSectorInstantiator.cs

## Assets/Scripts/Gameplay/World/Sectors/SectorObjectSpawnPoint.cs
- 분류: 일반 소스; 76행
- 타입: SectorObjectSpawnPointUsage, SectorObjectSpawnPoint : MonoBehaviour
- 메서드: Configure, CanUseFor, OnDrawGizmos
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 0개: 

## Assets/Scripts/Gameplay/World/Sectors/SectorOccupancy.cs
- 분류: 일반 소스; 210행
- 타입: SectorOwner, SectorContestState, SectorSpecialState, SectorOccupancySnapshot, SectorOccupancy : MonoBehaviour
- 메서드: Awake, OnEnable, Update, RefreshNow, SampleRatios, ApplyImmediateOwner, GetDominantOwner, BuildSnapshot, Publish, SetSpecialState, AddSpecialState, RemoveSpecialState, ForceOwnerAndRatios, ResetToNeutral
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 1개: Assets/Prefabs/GameScene/Sector/Sector_Base.prefab
- 타입 이름을 참조하는 소스 14개: Assets/Scripts/Core/Events/ScriptableObject/SectorMapSnapshotEventChannelSO.cs; Assets/Scripts/Core/Events/ScriptableObject/SectorOccupancyEventChannelSO.cs; Assets/Scripts/Core/Events/ScriptableObject/StageProgressSnapshotEventChannelSO.cs; Assets/Scripts/Gameplay/Combat/PaintMark/PaintTypes.cs; Assets/Scripts/Gameplay/Entities/Creature/NamedEnemy/NamedBattleSectorResetter.cs; Assets/Scripts/Gameplay/Progression/Stage/StageProgressionManager.cs; Assets/Scripts/Gameplay/World/Sectors/Named_Boss/NamedRewardApplier.cs; Assets/Scripts/Gameplay/World/Sectors/Named_Boss/NamedSectorController.cs

## Assets/Scripts/Gameplay/World/Sectors/SectorPaint.cs
- 분류: 일반 소스; 732행
- 타입: SectorPaint : MonoBehaviour, StoredCirclePaint
- 메서드: Reset, Awake, OnEnable, OnDisable, GetWorldBounds, CanPaintNow, CanQueryPaintNow, ContainsPoint, IntersectsCircle, CanAcceptCircle, ApplyCircle, GetStoredPaints, ClearAllStoredPaint, ClearStoredPaint, ClearRuntimeData, ApplyGameplayCircle, AddVirusTrailCircle, FillGameplay, ClearAllPaintCoverage, HasPaintAtWorld, GetCoverageAtWorld, GetCoverageRatio, GetPaintStateAtWorld, TryWorldToMaskUV, EstimatePaintCoverage, TryGetPaintCellRange, GetCoverageAtCell, PaintCoverageCircle, EnsurePaintGrid, HasCoverageGrid, ClearCoverage, FillCoverage, GetCoverage, SetCoverage, GetCoverageArray, GetCoverageSum, AddCoverageSum, SetCoverageSum, EstimateCircleCellCoverage, TryWorldToPaintCell, PaintCellCenterToWorld, IsInsideCircleXZ, ApproximatelySameBoundsXZ
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 1개: Assets/Prefabs/GameScene/Sector/Sector_Base.prefab
- 타입 이름을 참조하는 소스 7개: Assets/Scripts/Gameplay/Entities/Creature/NamedEnemy/NamedBattleSectorResetter.cs; Assets/Scripts/Gameplay/Items/Weapons/Definitions/ThrowableUsableItemSO.cs; Assets/Scripts/Gameplay/Items/Weapons/Runtime/ThrowableZoneVisualRuntime.cs; Assets/Scripts/Gameplay/MaskRenderManager.cs; Assets/Scripts/Gameplay/World/Sectors/Named_Boss/NamedRewardApplier.cs; Assets/Scripts/Gameplay/World/Sectors/Named_Boss/SectorNamedStateApplier.cs; Assets/Scripts/Gameplay/World/Sectors/SectorOccupancy.cs

## Assets/Scripts/Gameplay/World/Sectors/SectorRuntime.cs
- 분류: 일반 소스; 248행
- 타입: SectorRuntime : MonoBehaviour
- 메서드: SetRuntimeInfo, ConfigureNormalSectorPolicy, SetOpened, SetCleared, SetEnemySpawnPointRoot, CollectEnemySpawnPoints, CollectContentSlotAnchors, GetWorldBounds, HasAllSideBounds
- 직렬화 참조 5개 / 지정 씬 참조 그래프 내 4개: Assets/Prefabs/GameScene/Sector/HomeSector.prefab; Assets/Prefabs/GameScene/Sector/Sector_Base.prefab; Assets/Prefabs/GameScene/Sector/Stage1/Sector_Named.prefab; Assets/Prefabs/GameScene/Sector/Stage1/Sector_Shop.prefab; Assets/Prefabs/GameScene/Sector/Stage1/Sector_Treasure.prefab
- 타입 이름을 참조하는 소스 59개: Assets/Scripts/Authoring/Editor/ContentPreflightWindow.cs; Assets/Scripts/Authoring/Editor/ContentTemplateInspectors.cs; Assets/Scripts/Authoring/Editor/DebugMonitorWindow.cs; Assets/Scripts/Authoring/Editor/DebugMonitorWindow.Operations.cs; Assets/Scripts/Authoring/Editor/SectorMakerWindow.cs; Assets/Scripts/Core/Events/ScriptableObject/BossRewardRequestEventChannelSO.cs; Assets/Scripts/Core/Events/ScriptableObject/BossRewardSelectedEventChannelSO.cs; Assets/Scripts/Core/Events/ScriptableObject/NamedBattleSignalEventChannelSO.cs

## Assets/Scripts/Gameplay/World/Sectors/SectorRuntimeEventChannelSO.cs
- 분류: 일반 소스; 42행
- 타입: SectorRuntimeEventChannelSO : ScriptableObject
- 메서드: RaiseEvent, Clear
- 직렬화 참조 4개 / 지정 씬 참조 그래프 내 4개: Assets/ScriptableObject/EventChannel/Camera/MoveSectorCamera.asset; Assets/ScriptableObject/EventChannel/Sector/CurrentSectorChanged.asset; Assets/ScriptableObject/EventChannel/Sector/SectorOpended.asset; Assets/ScriptableObject/EventChannel/Sector/StartSectorReady.asset
- 타입 이름을 참조하는 소스 9개: Assets/Scripts/Gameplay/Entities/Common/StateMachine/Conditions/PlayerIsInZoneSO.cs; Assets/Scripts/Gameplay/SpawnSystem.cs; Assets/Scripts/Gameplay/World/Sectors/Named_Boss/NamedSectorController.cs; Assets/Scripts/Gameplay/World/Sectors/Named_Boss/NamedSectorTransitionController.cs; Assets/Scripts/Gameplay/World/Sectors/Runtime/SectorOccupancyManager.cs; Assets/Scripts/Gameplay/World/Sectors/Runtime/SectorPortalManager.cs; Assets/Scripts/Gameplay/World/Sectors/Runtime/SectorStateManager.cs; Assets/Scripts/Gameplay/World/Sectors/SectorEnemySpawner.cs

## Assets/Scripts/Gameplay/World/Sectors/SectorSkipStation.cs
- 분류: 일반 소스; 86행
- 타입: SectorSkipStation : MonoBehaviour
- 메서드: Reset, TryInteract, ResolveRefs, TryGetRoomType
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 2개: Assets/Scripts/Gameplay/Interaction/Core/InteractionManager.cs; Assets/Scripts/Gameplay/Progression/Stage/StageProgressionManager.cs

## Assets/Scripts/Gameplay/World/Sectors/Shop/SectorShop.cs
- 분류: 일반 소스; 649행
- 타입: SectorShop : MonoBehaviour
- 메서드: Configure, ConfigureForHome, DebugTryEnsureOffers, DebugTryAddOffer, DebugClearOffers, ResolveDebugOfferKind, CaptureRunState, TryRestoreRunState, TryGetRunStateId, CaptureOffers, CapturePaymentRequirements, TryRestoreOffers, TryRestorePaymentRequirements, OnEnable, OnDisable, TryInteract, HandleSectorStateManagerReady, HandleRunReset, TryEnsureOffers, TryRestockIfDue, RollOffers, IsShopRoom, ResolveSectorCoord, ResolveOfferSeed, ResolveRefs, LogWarning, LogError
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 1개: Assets/Prefabs/GameScene/Sector/SectorShop.prefab
- 타입 이름을 참조하는 소스 4개: Assets/Scripts/Authoring/Editor/DebugMonitorWindow.Operations.cs; Assets/Scripts/Gameplay/Interaction/Core/InteractionManager.cs; Assets/Scripts/Gameplay/World/Sectors/Shop/ShopRunRuntime.cs; Assets/Scripts/Gameplay/World/Sectors/StageMap/StageSectorInstantiator.cs

## Assets/Scripts/Gameplay/World/Sectors/Shop/ShopConfigSO.cs
- 분류: 일반 소스; 372행
- 타입: ShopConfigSO : ScriptableObject, ProductDefinition, UnlockNode
- 메서드: ResolvePrice, CreateBulletOffers, CreateItemOffers, TryCreateBulletOffer, TryCreateItemOffer, TryGetNode, CreateOffers, TryCreateOffer, IsCandidateValid, ResolveTradeRequirements, CreateDefaultCurrencyPayment, HasValidPhysicalPayment, CreateOffer
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 1개: Assets/ScriptableObject/GameScene/Shop/HomeShopConfig.asset
- 타입 이름을 참조하는 소스 11개: Assets/Editor/QuestDesignTreeWindow.cs; Assets/Editor/ShopProgressionTreeWindow.cs; Assets/Scripts/Authoring/Editor/ContentPreflightWindow.cs; Assets/Scripts/Core/Events/ScriptableObject/SectorShopOpenRequestEventChannelSO.cs; Assets/Scripts/Gameplay/Progression/Quests/QuestDefinitionSO.cs; Assets/Scripts/Gameplay/World/Sectors/Shop/SectorShop.cs; Assets/Scripts/Gameplay/World/Sectors/Shop/ShopRoomDropTableSO.cs; Assets/Scripts/Gameplay/World/Sectors/Shop/ShopUnlockRuntime.cs

## Assets/Scripts/Gameplay/World/Sectors/Shop/ShopRoomDropTableSO.cs
- 분류: 일반 소스; 555행
- 타입: ShopRoomItemKind, ShopRoomDropTableSO : ScriptableObject, RarityWeight, ItemKindWeight, BulletShopEntry, ItemShopEntry
- 메서드: CreateBulletOffers, CreateItemOffers, CreateOffers, TryCreateBulletOffer, TryCreateItemOffer, CreateBulletOffer, CreateItemOffer, TryPickBulletEntry, TryPickItemEntry, BuildMergedBulletPool, BuildMergedItemPool, AddBulletPool, AddItemPool, RollRarity, RollItemKind, IsBulletCandidateValid, IsItemCandidateValid, IsSupportedItem, IsExcluded, ResolveItemKind, ResolveTreasureKind
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 1개: Assets/ScriptableObject/GameScene/Stage/ShopRoomDropTable.asset
- 타입 이름을 참조하는 소스 1개: Assets/Scripts/Gameplay/World/Sectors/Treasure/TreasureRoomDropTableSO.cs

## Assets/Scripts/Gameplay/World/Sectors/Shop/ShopRunRuntime.cs
- 분류: 일반 소스; 146행
- 타입: ShopRunRuntime : MonoBehaviour
- 메서드: EnsureRuntime, Awake, ResetForRunLoss, CaptureRunState, RestoreRunState, TryApplyPendingState, OnDestroy
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 4개: Assets/Scripts/Core/Persistence/PlayerRunStatePersistence.cs; Assets/Scripts/Core/Persistence/RunLifecycleCoordinator.cs; Assets/Scripts/Gameplay/Entities/Shooter/Player/PlayerShooterDeathCorpseController.cs; Assets/Scripts/Gameplay/World/Sectors/Shop/SectorShop.cs

## Assets/Scripts/Gameplay/World/Sectors/Shop/ShopTradeRequirement.cs
- 분류: 일반 소스; 33행
- 타입: ShopTradeRequirement
- 메서드: 
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 6개: Assets/Scripts/Gameplay/Entities/Shooter/Player/Inventory/PlayerInventoryRuntime.cs; Assets/Scripts/Gameplay/World/Sectors/Shop/SectorShop.cs; Assets/Scripts/Gameplay/World/Sectors/Shop/ShopConfigSO.cs; Assets/Scripts/Presentation/UI/Gameplay/Shop/PlayerShopOffer.cs; Assets/Scripts/Presentation/UI/Gameplay/Shop/PlayerShopPanelUI.cs; Assets/Scripts/Presentation/UI/Gameplay/Shop/ShopPurchaseTradePanel.cs

## Assets/Scripts/Gameplay/World/Sectors/Shop/ShopUnlockRuntime.cs
- 분류: 일반 소스; 277행
- 타입: ShopUnlockSaveData, ShopUnlockRuntime : MonoBehaviour
- 메서드: EnsureRuntime, Awake, Configure, IsProductAvailable, IsNodeUnlocked, TryUnlockNode, RefreshAutomaticUnlocks, Load, Save, FindProfileData, AddUnlockedNodeIds, BuildLegacySaveKey, OnDestroy
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 3개: Assets/Scripts/Gameplay/Progression/Quests/QuestRuntime.cs; Assets/Scripts/Gameplay/World/Sectors/Shop/SectorShop.cs; Assets/Scripts/Presentation/UI/Gameplay/Shop/PlayerShopPanelUI.cs

## Assets/Scripts/Gameplay/World/Sectors/StageMap/RaidMapGenerator.cs
- 분류: 일반 소스; 145행
- 타입: RaidMapGenerator
- 메서드: TryGenerate, AddNode, Random, PickRole
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 7개: Assets/Scripts/Authoring/Editor/ContentPreflightWindow.cs; Assets/Scripts/Authoring/Editor/StageDefinitionRaidEditor.cs; Assets/Scripts/Core/Persistence/RaidRunSaveData.cs; Assets/Scripts/Gameplay/World/Sectors/StageMap/RaidMapValidation.cs; Assets/Scripts/Gameplay/World/Sectors/StageMap/StageSectorInstantiator.cs; Assets/Tests/EditMode/Editor/RaidMapFoundationTests.cs; Assets/Tests/EditMode/Editor/RaidRuntimeContractTests.cs

## Assets/Scripts/Gameplay/World/Sectors/StageMap/RaidMapLabels.cs
- 분류: 일반 소스; 5행
- 타입: RaidMapLabels
- 메서드: ForRole
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 2개: Assets/Scripts/Presentation/UI/Gameplay/Map/FullMap/FullMapUI.cs; Assets/Scripts/Presentation/UI/Gameplay/Map/MiniMap/MiniMapUI.cs

## Assets/Scripts/Gameplay/World/Sectors/StageMap/RaidMapSettings.cs
- 분류: 일반 소스; 39행
- 타입: StageMapPolicy, RaidRoomRole, RaidMapSettings
- 메서드: TryValidate
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 15개: Assets/Scripts/Authoring/Editor/ContentPreflightWindow.cs; Assets/Scripts/Authoring/Editor/StageDefinitionRaidEditor.cs; Assets/Scripts/Core/Events/ScriptableObject/SectorMapSnapshotEventChannelSO.cs; Assets/Scripts/Gameplay/Progression/Stage/StageProgressionManager.cs; Assets/Scripts/Gameplay/World/Sectors/Runtime/SectorOccupancyManager.cs; Assets/Scripts/Gameplay/World/Sectors/ScriptableObject/StageDefinitionSO.cs; Assets/Scripts/Gameplay/World/Sectors/StageMap/RaidMapGenerator.cs; Assets/Scripts/Gameplay/World/Sectors/StageMap/RaidMapLabels.cs

## Assets/Scripts/Gameplay/World/Sectors/StageMap/RaidMapValidation.cs
- 분류: 일반 소스; 55행
- 타입: RaidMapValidation
- 메서드: TryValidate, HasRole, UpperRight, Fail
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 2개: Assets/Scripts/Core/Persistence/RaidRunSaveData.cs; Assets/Tests/EditMode/Editor/RaidMapFoundationTests.cs

## Assets/Scripts/Gameplay/World/Sectors/StageMap/RaidRoomContentSpawner.cs
- 분류: 일반 소스; 23행
- 타입: RaidRoomContentSpawner
- 메서드: Populate
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 1개: Assets/Scripts/Gameplay/World/Sectors/StageMap/StageSectorInstantiator.cs

## Assets/Scripts/Gameplay/World/Sectors/StageMap/RaidRuntime.cs
- 분류: 일반 소스; 181행
- 타입: RaidRuntime : MonoBehaviour
- 메서드: Initialize, BindContainers, MarkReady, Suspend, IsHatchRevealedFor, IsInSector, Update, TickClock, TryPowerGenerator, TryExtract, AllowsExtraction, Capture, EndRaid, OnDestroy, Play
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 12개: Assets/Scripts/Authoring/Editor/ContentPreflightWindow.cs; Assets/Scripts/Authoring/Editor/StageDefinitionRaidEditor.cs; Assets/Scripts/Core/Persistence/PlayerRunStatePersistence.cs; Assets/Scripts/Gameplay/Progression/Stage/StageProgressionManager.cs; Assets/Scripts/Gameplay/World/Sectors/Runtime/SectorOccupancyManager.cs; Assets/Scripts/Gameplay/World/Sectors/ScriptableObject/StageDefinitionSO.cs; Assets/Scripts/Gameplay/World/Sectors/StageMap/RaidStation.cs; Assets/Scripts/Gameplay/World/Sectors/StageMap/RaidStationFactory.cs

## Assets/Scripts/Gameplay/World/Sectors/StageMap/RaidRuntimeSettings.cs
- 분류: 일반 소스; 42행
- 타입: RaidRuntimeSettings, RaidRoomPresetBinding
- 메서드: FindRoom
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 4개: Assets/Scripts/Gameplay/World/Sectors/ScriptableObject/StageDefinitionSO.cs; Assets/Scripts/Gameplay/World/Sectors/StageMap/RaidRuntime.cs; Assets/Scripts/Gameplay/World/Sectors/StageMap/RaidStationFactory.cs; Assets/Scripts/Gameplay/World/Sectors/StageMap/StageSectorInstantiator.cs

## Assets/Scripts/Gameplay/World/Sectors/StageMap/RaidStation.cs
- 분류: 일반 소스; 48행
- 타입: RaidStationKind, RaidStation : MonoBehaviour
- 메서드: SetPrototypeLabel, Update, Configure, TryInteract, OnDrawGizmos
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 5개: Assets/Scripts/Gameplay/Interaction/Core/InteractionManager.cs; Assets/Scripts/Gameplay/World/Sectors/StageMap/RaidRuntime.cs; Assets/Scripts/Gameplay/World/Sectors/StageMap/RaidRuntimeSettings.cs; Assets/Scripts/Gameplay/World/Sectors/StageMap/RaidStationFactory.cs; Assets/Tests/EditMode/Editor/RaidRuntimeContractTests.cs

## Assets/Scripts/Gameplay/World/Sectors/StageMap/RaidStationFactory.cs
- 분류: 일반 소스; 46행
- 타입: RaidStationFactory
- 메서드: Attach
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 1개: Assets/Scripts/Gameplay/World/Sectors/StageMap/StageSectorInstantiator.cs

## Assets/Scripts/Gameplay/World/Sectors/StageMap/StageGoalRoomType.cs
- 분류: 일반 소스; 28행
- 타입: StageGoalRoomType, StageGoalRoomTypeExtensions
- 메서드: ToStageRoomType
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 0개: 

## Assets/Scripts/Gameplay/World/Sectors/StageMap/StageMapGenerator.cs
- 분류: 일반 소스; 534행
- 타입: StageMapGenerator
- 메서드: TryGenerateBranchTopology, GenerateFullGrid, AssignSingleOuterExtraction, TryAssignOuterExtraction, BuildBranchConnections, BuildStageSeed, BuildFullGridConnections, ApplyTreasureRooms, BuildTreasureCandidates, IsTreasureCandidate, ApplyShopRooms, BuildSpecialRoomCandidates, IsSpecialRoomCandidate, BuildSaltedSeed
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 4개: Assets/Scripts/Gameplay/World/Sectors/SectorEnemySpawner.cs; Assets/Scripts/Gameplay/World/Sectors/StageMap/RaidMapGenerator.cs; Assets/Scripts/Gameplay/World/Sectors/StageMap/StageSectorInstantiator.cs; Assets/Tests/EditMode/Editor/RaidMapFoundationTests.cs

## Assets/Scripts/Gameplay/World/Sectors/StageMap/StageMapLayout.cs
- 분류: 일반 소스; 75행
- 타입: StageMapLayout
- 메서드: TryGetRoom, TryGetGoalRoom, IsExtractionPortal, IsExtractionSector
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 13개: Assets/Scripts/Authoring/Editor/StageDefinitionRaidEditor.cs; Assets/Scripts/Core/Persistence/RaidRunSaveData.cs; Assets/Scripts/Core/SceneFlow/SceneLoader.cs; Assets/Scripts/Gameplay/Progression/Stage/StageProgressionManager.cs; Assets/Scripts/Gameplay/World/Sectors/Runtime/SectorOccupancyManager.cs; Assets/Scripts/Gameplay/World/Sectors/Runtime/SectorPortalManager.cs; Assets/Scripts/Gameplay/World/Sectors/Runtime/SectorStateManager.cs; Assets/Scripts/Gameplay/World/Sectors/StageMap/RaidMapGenerator.cs

## Assets/Scripts/Gameplay/World/Sectors/StageMap/StageRoomNode.cs
- 분류: 일반 소스; 38행
- 타입: StageRoomNode
- 메서드: HasConnection
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 11개: Assets/Scripts/Authoring/Editor/StageDefinitionRaidEditor.cs; Assets/Scripts/Core/SceneFlow/SceneLoader.cs; Assets/Scripts/Gameplay/World/Sectors/Runtime/SectorOccupancyManager.cs; Assets/Scripts/Gameplay/World/Sectors/Runtime/SectorPortalManager.cs; Assets/Scripts/Gameplay/World/Sectors/Runtime/SectorStateManager.cs; Assets/Scripts/Gameplay/World/Sectors/StageMap/RaidMapGenerator.cs; Assets/Scripts/Gameplay/World/Sectors/StageMap/RaidMapValidation.cs; Assets/Scripts/Gameplay/World/Sectors/StageMap/RaidStationFactory.cs

## Assets/Scripts/Gameplay/World/Sectors/StageMap/StageRoomType.cs
- 분류: 일반 소스; 33행
- 타입: StageRoomType, StageRoomConnectionFlags
- 메서드: 
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 31개: Assets/Scripts/Authoring/Editor/ContentPreflightWindow.cs; Assets/Scripts/Authoring/Editor/DebugMonitorWindow.cs; Assets/Scripts/Authoring/Editor/SectorMakerWindow.cs; Assets/Scripts/Core/Events/ScriptableObject/SectorMapSnapshotEventChannelSO.cs; Assets/Scripts/Core/SceneFlow/SceneLoader.cs; Assets/Scripts/Gameplay/Progression/Stage/StageProgressionManager.cs; Assets/Scripts/Gameplay/World/Sectors/Named_Boss/NamedSectorController.cs; Assets/Scripts/Gameplay/World/Sectors/Runtime/SectorOccupancyManager.cs

## Assets/Scripts/Gameplay/World/Sectors/StageMap/StageSectorInstantiator.cs
- 분류: 일반 소스; 742행
- 타입: StageSectorInstantiator : MonoBehaviour
- 메서드: Awake, BuildStage, EnsureHideout, TryResolveAuthoredHomeSector, ConfigureAuthoredHomeSectorServices, ConfigureAuthoredHomeShop, ConfigureAuthoredInheritanceWarehouse, RebuildGeneratedSectorNavMeshes, ResolveNavMeshTemplate, HasRuntimeNavMeshColliderSource, ClearGeneratedSectors, DetachPlayerFromGeneratedSectors, ResolveRoomStep, ResolveWorldPosition
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 1개: Assets/Scenes/RUNScene.unity
- 타입 이름을 참조하는 소스 7개: Assets/Scripts/Authoring/Editor/SectorMakerWindow.cs; Assets/Scripts/Core/SceneFlow/RuntimeSceneDependencyValidator.cs; Assets/Scripts/Gameplay/Progression/Stage/StageProgressionManager.cs; Assets/Scripts/Gameplay/World/Sectors/Runtime/SectorPortalManager.cs; Assets/Scripts/Gameplay/World/Sectors/Runtime/SectorStateManager.cs; Assets/Scripts/Gameplay/World/Sectors/SectorEnemySpawner.cs; Assets/Scripts/Gameplay/World/Sectors/StageMap/StageTopologySO.cs

## Assets/Scripts/Gameplay/World/Sectors/StageMap/StageShopRoomGenerationSettings.cs
- 분류: 일반 소스; 57행
- 타입: StageShopRoomGenerationSettings
- 메서드: RollShopRoomCount
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 2개: Assets/Scripts/Gameplay/World/Sectors/StageMap/StageMapGenerator.cs; Assets/Scripts/Gameplay/World/Sectors/StageMap/StageSectorInstantiator.cs

## Assets/Scripts/Gameplay/World/Sectors/StageMap/StageTopologySO.cs
- 분류: 일반 소스; 265행
- 타입: StageTopologyRoom, StageTopologySO : ScriptableObject
- 메서드: TryGetRoom, TryGetGoalRoomCoord, TryGetBounds, TryValidate, SetEntryRoomCoord, UpsertRoom, RemoveRoom, ReplaceWithBranchingExample, VisitAdjacent
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 4개: Assets/Scripts/Authoring/Editor/SectorMakerWindow.cs; Assets/Scripts/Gameplay/World/Sectors/ScriptableObject/StageDefinitionSO.cs; Assets/Scripts/Gameplay/World/Sectors/StageMap/StageMapGenerator.cs; Assets/Scripts/Gameplay/World/Sectors/StageMap/StageSectorInstantiator.cs

## Assets/Scripts/Gameplay/World/Sectors/StageMap/StageTreasureRoomGenerationSettings.cs
- 분류: 일반 소스; 66행
- 타입: StageTreasureRoomGenerationSettings
- 메서드: RollTreasureRoomCount
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 3개: Assets/Scripts/Gameplay/World/Sectors/ScriptableObject/StageDefinitionSO.cs; Assets/Scripts/Gameplay/World/Sectors/StageMap/StageMapGenerator.cs; Assets/Scripts/Gameplay/World/Sectors/StageMap/StageSectorInstantiator.cs

## Assets/Scripts/Gameplay/World/Sectors/Traits/SectorEnemyTraitModifier.cs
- 분류: 일반 소스; 74행
- 타입: SectorEnemyTraitModifier : MonoBehaviour
- 메서드: Configure, Awake, ResolveRefs, ApplyHealthMultiplier
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 2개: Assets/Scripts/Gameplay/Entities/Shooter/Enemy/Common/EnemyShooterStatsRuntime.cs; Assets/Scripts/Gameplay/World/Sectors/Traits/SectorRoomTraitRuntime.cs

## Assets/Scripts/Gameplay/World/Sectors/Traits/SectorRoomTraitRuntime.cs
- 분류: 일반 소스; 210행
- 타입: SectorRoomTraitRuntime : MonoBehaviour
- 메서드: SetCurrentPlayerSector, Configure, TryAddTimerFailureGasLeak, ApplyToSpawnedEnemy, SetDebugRoomTraitOverride, ConfigureAuthoredRoomTrait, ClearDebugRoomTraitOverride, RefreshSpawnedEnemyTraitModifiers, OnDisable
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 13개: Assets/Scripts/Authoring/Editor/DebugMonitorWindow.Operations.cs; Assets/Scripts/Gameplay/Entities/Shooter/Common/Vision/EntityVisionController.cs; Assets/Scripts/Gameplay/Entities/Shooter/Player/PlayerBulletLoadoutRuntime.cs; Assets/Scripts/Gameplay/Entities/Shooter/Player/PlayerConditionAudio.cs; Assets/Scripts/Gameplay/Entities/Shooter/Player/PlayerShooterInfection.cs; Assets/Scripts/Gameplay/Entities/Shooter/Player/PlayerStatsRuntime.cs; Assets/Scripts/Gameplay/Progression/Stage/StageProgressionManager.cs; Assets/Scripts/Gameplay/World/Sectors/Runtime/SectorStateManager.cs

## Assets/Scripts/Gameplay/World/Sectors/Traits/SectorRoomTraitTableSO.cs
- 분류: 일반 소스; 128행
- 타입: SectorRoomTraitType, SectorRoomTraitWeight, SectorRoomTraitTableSO : ScriptableObject
- 메서드: IsEligibleFor, PickTrait
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 1개: Assets/ScriptableObject/GameScene/Stage/SectorRoomTraitTable.asset
- 타입 이름을 참조하는 소스 6개: Assets/Scripts/Authoring/Editor/DebugMonitorWindow.Operations.cs; Assets/Scripts/Gameplay/World/Sectors/ScriptableObject/StageDefinitionSO.cs; Assets/Scripts/Gameplay/World/Sectors/StageMap/StageSectorInstantiator.cs; Assets/Scripts/Gameplay/World/Sectors/Traits/SectorEnemyTraitModifier.cs; Assets/Scripts/Gameplay/World/Sectors/Traits/SectorRoomTraitRuntime.cs; Assets/Scripts/Presentation/UI/Gameplay/SectorTraits/SectorTraitHUD.cs

## Assets/Scripts/Gameplay/World/Sectors/Treasure/InheritanceWarehouseRuntime.cs
- 분류: 일반 소스; 616행
- 타입: InheritanceWarehouseRuntime : EnemyLootInventoryRuntime
- 메서드: Awake, Update, OnDisable, ConfigurePersistence, TryApplyPersistentCapacity, DebugTryAddItem, DebugClearContents, DebugReloadFromSave, TryValidateDebugPersistence, DebugPersistImmediately, IsSameDebugItem, IsDebugCatalogItem, TryTakeItemStack, LoadContents, AddItemsToLookup, PersistContents, BuildFingerprint
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 1개: Assets/Prefabs/GameScene/Sector/HomeSector.prefab
- 타입 이름을 참조하는 소스 4개: Assets/Scripts/Authoring/Editor/DebugMonitorWindow.Operations.cs; Assets/Scripts/Gameplay/Entities/Shooter/Player/Inventory/PlayerInventoryRuntime.cs; Assets/Scripts/Gameplay/Progression/Quests/QuestRuntime.cs; Assets/Scripts/Gameplay/World/Sectors/StageMap/StageSectorInstantiator.cs

## Assets/Scripts/Gameplay/World/Sectors/Treasure/SectorTreasure.cs
- 분류: 일반 소스; 391행
- 타입: SectorTreasure : MonoBehaviour
- 메서드: Configure, Awake, OnEnable, OnDisable, TryInteract, TryRollRewardIntoLoot, HandleSectorStateManagerReady, IsTreasureRoom, ResolveSectorCoord, ResolveRewardSeed, ResolveOwnedPassiveItems, ResolveRefs, HidePlaceholder, EnsureTreasureVisual, RefreshDiscoveryVisibility, DisableVisualPhysics, SetInteractionEnabled, SpawnRewardParticle, ClearRewardParticle, LogWarning
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 1개: Assets/Prefabs/GameScene/Sector/Stage1/Sector_Treasure.prefab
- 타입 이름을 참조하는 소스 5개: Assets/Editor/StructureLootInventoryRuntimeEditor.cs; Assets/Scripts/Gameplay/Interaction/Core/InteractionManager.cs; Assets/Scripts/Gameplay/World/Sectors/StageMap/StageSectorInstantiator.cs; Assets/Scripts/Gameplay/World/Sectors/Treasure/StructureLootInventoryRuntime.cs; Assets/Scripts/Presentation/UI/Gameplay/Map/MiniMap/SectorTacticalMiniMapGraphic.cs

## Assets/Scripts/Gameplay/World/Sectors/Treasure/StructureLootInventoryRuntime.cs
- 분류: 일반 소스; 40행
- 타입: StructureLootInventoryRuntime : EnemyLootInventoryRuntime
- 메서드: Configure
- 직렬화 참조 29개 / 지정 씬 참조 그래프 내 4개: Assets/Prefabs/GameScene/GameItemObject/LootableObject/AmoBox_01.prefab; Assets/Prefabs/GameScene/GameItemObject/LootableObject/EnergyBox_01.prefab; Assets/Prefabs/GameScene/GameItemObject/LootableObject/HealthBox_01.prefab; Assets/Prefabs/GameScene/GameItemObject/LootableObject/HealthBox_02.prefab; Assets/Prefabs/GameScene/GameItemObject/LootableObject/HealthBox_03.prefab; Assets/Prefabs/GameScene/GameItemObject/LootableObject/MilitaryCase_01_WornWhite.prefab; Assets/Prefabs/GameScene/GameItemObject/LootableObject/MilitaryCase_02_NewWhite.prefab; Assets/Prefabs/GameScene/GameItemObject/LootableObject/MilitaryCase_03_WornGreen.prefab
- 타입 이름을 참조하는 소스 3개: Assets/Editor/StructureLootInventoryRuntimeEditor.cs; Assets/Scripts/Gameplay/Items/Loot/LootableObject.cs; Assets/Scripts/Gameplay/World/Sectors/Treasure/SectorTreasure.cs

## Assets/Scripts/Gameplay/World/Sectors/Treasure/TreasureRoomDropTableSO.cs
- 분류: 일반 소스; 946행
- 타입: TreasureRewardKind, TreasureRoomReward, TreasureLootMigrationEntry, TreasureRoomDropTableSO : ScriptableObject, RarityWeight, RewardKindWeight, PassiveItemEntry, BulletItemEntry, ArmorItemEntry, RarityRewardPool, PassiveCandidate, BulletCandidate, ArmorCandidate
- 메서드: CollectLootMigrationEntries, GetMigrationRarityWeight, TryRollReward, TryCreateFallbackReward, TryCreateReward, TryCreatePassiveReward, TryCreateBulletReward, TryCreateArmorReward, TryPickPassive, TryPickBullet, TryPickArmor, TryPickPassiveFromAnyRarity, TryPickBulletFromAnyRarity, TryPickArmorFromAnyRarity, AddPassiveCandidates, AddBulletCandidates, AddArmorCandidates, BuildPassiveCandidates, BuildBulletCandidates, BuildArmorCandidates, GetPool, AppendMigrationEntries, GetRewardKindWeight, RollRarity, RollRewardKind, IsPassiveCandidateValid, IsBulletCandidateValid, IsArmorCandidateValid
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 1개: Assets/ScriptableObject/GameScene/Stage/TreasureRoomDropTable.asset
- 타입 이름을 참조하는 소스 6개: Assets/Scripts/Gameplay/World/Sectors/ScriptableObject/StageDefinitionSO.cs; Assets/Scripts/Gameplay/World/Sectors/Shop/SectorShop.cs; Assets/Scripts/Gameplay/World/Sectors/Shop/ShopConfigSO.cs; Assets/Scripts/Gameplay/World/Sectors/Shop/ShopRoomDropTableSO.cs; Assets/Scripts/Gameplay/World/Sectors/Treasure/SectorTreasure.cs; Assets/Scripts/Presentation/UI/Gameplay/Shop/PlayerShopOffer.cs