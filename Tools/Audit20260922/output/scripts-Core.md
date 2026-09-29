# Core 스크립트 전수 정적 인덱스
검사일: 2026-09-22. 함수/타입/직렬화 연결은 정적 추출이며 실행 검증을 뜻하지 않습니다. 동적 생성, 리플렉션 및 Addressables 간접 참조는 별도 시스템 문서와 함께 확인하세요.

## Assets/Scripts/Core/Anchors/RuntimeAnchorBase.cs
- 분류: 일반 소스; 54행
- 타입: RuntimeAnchorBase
- 메서드: Provide, Unset, OnDisable
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 1개: Assets/Scripts/Core/Anchors/TransformAnchor.cs

## Assets/Scripts/Core/Anchors/TransformAnchor.cs
- 분류: 일반 소스; 12행
- 타입: TransformAnchor : RuntimeAnchorBase<Transform>
- 메서드: 
- 직렬화 참조 2개 / 지정 씬 참조 그래프 내 1개: Assets/ScriptableObject/RuntimeAnchor/GameplayCameraTransform.asset; Assets/ScriptableObject/RuntimeAnchor/PlayerTransform.asset
- 타입 이름을 참조하는 소스 5개: Assets/Scripts/Gameplay/Entities/Common/StateMachine/Actions/ChasingTargetActionSO.cs; Assets/Scripts/Gameplay/Entities/Common/StateMachine/Actions/CreatureFacePlayerSO.cs; Assets/Scripts/Gameplay/Entities/Creature/NamedEnemy/Mutarus/MutarusQTEPatternController.cs; Assets/Scripts/Gameplay/SpawnSystem.cs; Assets/Scripts/Gameplay/World/Sectors/StageMap/StageSectorInstantiator.cs

## Assets/Scripts/Core/Application/Pause/GamePause.cs
- 분류: 일반 소스; 34행
- 타입: GamePause
- 메서드: On, Off
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 15개: Assets/Scripts/Core/Application/Pause/PauseExceptionRouter.cs; Assets/Scripts/Core/SceneFlow/SceneLoader.cs; Assets/Scripts/Gameplay/Entities/Common/StateMachine/Actions/ApplyMovementVectorActionSO.cs; Assets/Scripts/Gameplay/Entities/Common/StateMachine/Actions/AscendActionSO.cs; Assets/Scripts/Gameplay/Entities/Common/StateMachine/Actions/CreatureRotateActionSO.cs; Assets/Scripts/Gameplay/Entities/Common/StateMachine/Actions/DescendActionSO.cs; Assets/Scripts/Gameplay/Entities/Common/StateMachine/Actions/GroundGravityActionSO.cs; Assets/Scripts/Gameplay/Entities/Common/StateMachine/Actions/HorizontalMoveActionSO.cs

## Assets/Scripts/Core/Application/Pause/PauseExceptionRouter.cs
- 분류: 일반 소스; 32행
- 타입: PauseExceptionRouter : MonoBehaviour
- 메서드: 
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 0개: 

## Assets/Scripts/Core/Events/IntEventListener.cs
- 분류: 일반 소스; 49행
- 타입: IntEvent : UnityEvent<int>, IntEventListener : MonoBehaviour
- 메서드: OnEnable, OnDisable, Respond
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 0개: 

## Assets/Scripts/Core/Events/NamedEnemyKilledEventChannelSO.cs
- 분류: 일반 소스; 23행
- 타입: NamedEnemyKilledEventChannelSO : ScriptableObject
- 메서드: RaiseEvent
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 0개: Assets/ScriptableObject/EventChannel/Character/Enemy/NamedEnemy/NamedEnemyKilledEventChannel.asset
- 타입 이름을 참조하는 소스 2개: Assets/Scripts/Gameplay/Entities/Creature/NamedEnemy/Action/RaiseNamedEnemyKilledActionSO.cs; Assets/Scripts/Gameplay/World/Sectors/Named_Boss/NamedSectorController.cs

## Assets/Scripts/Core/Events/ScriptableObject/AudioCueEventChannelSO.cs
- 분류: 일반 소스; 52행
- 타입: AudioCueEventChannelSO : DescriptionBaseSO
- 메서드: RaisePlayEvent, RaiseStopEvent, RaiseFinishEvent
- 직렬화 참조 2개 / 지정 씬 참조 그래프 내 2개: Assets/ScriptableObject/EventChannel/Audio/PlayMusic_Channel.asset; Assets/ScriptableObject/EventChannel/Audio/PlaySFX_Channel.asset
- 타입 이름을 참조하는 소스 16개: Assets/Scripts/Gameplay/Entities/Common/Core/CharacterAudio.cs; Assets/Scripts/Gameplay/Entities/Common/StateMachine/Actions/PlayAudioCueActionSO.cs; Assets/Scripts/Gameplay/Entities/Shooter/Player/PlayerConditionAudio.cs; Assets/Scripts/Gameplay/World/Sectors/StageMap/RaidRuntimeSettings.cs; Assets/Scripts/Presentation/Audio/AudioCue.cs; Assets/Scripts/Presentation/Audio/AudioData/BulletImpactAudioCatalogSO.cs; Assets/Scripts/Presentation/Audio/AudioData/NpcAudioProfileSO.cs; Assets/Scripts/Presentation/Audio/AudioLoopEmitter.cs

## Assets/Scripts/Core/Events/ScriptableObject/BoolEventChannelSO.cs
- 분류: 일반 소스; 17행
- 타입: BoolEventChannelSO : DescriptionBaseSO
- 메서드: RaiseEvent
- 직렬화 참조 5개 / 지정 씬 참조 그래프 내 3개: Assets/ScriptableObject/EventChannel/Sector/NamedWorldLockManager.asset; Assets/ScriptableObject/EventChannel/Sector/Named_Boss/NamedBattleActiveChannel.asset; Assets/ScriptableObject/EventChannel/UI/OnPauseOpened.asset; Assets/ScriptableObject/EventChannel/UI/ToggleLoadingScreen.asset; Assets/ScriptableObject/EventChannel/UI/WeaponAmmoSection/AmmoSellModeEventChannel.asset
- 타입 이름을 참조하는 소스 4개: Assets/Scripts/Core/SceneFlow/SceneLoader.cs; Assets/Scripts/Gameplay/World/Sectors/Named_Boss/NamedBattleManager.cs; Assets/Scripts/Presentation/Audio/MusicPlayer.cs; Assets/Scripts/Presentation/UI/Common/UIPause.cs

## Assets/Scripts/Core/Events/ScriptableObject/BossRewardRequestEventChannelSO.cs
- 분류: 일반 소스; 35행
- 타입: BossRewardRequest, BossRewardRequestEventChannelSO : ScriptableObject
- 메서드: RaiseEvent
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 1개: Assets/ScriptableObject/EventChannel/UI/BossReward/BossRewardRequestEventChannel.asset
- 타입 이름을 참조하는 소스 2개: Assets/Scripts/Gameplay/World/Sectors/Named_Boss/NamedSectorController.cs; Assets/Scripts/Presentation/UI/Gameplay/BossReward/BossRewardUIController.cs

## Assets/Scripts/Core/Events/ScriptableObject/BossRewardSelectedEventChannelSO.cs
- 분류: 일반 소스; 40행
- 타입: BossRewardSelection, BossRewardSelectedEventChannelSO : ScriptableObject
- 메서드: RaiseEvent
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 1개: Assets/ScriptableObject/EventChannel/UI/BossReward/BossRewardSelectedEventChannel.asset
- 타입 이름을 참조하는 소스 2개: Assets/Scripts/Presentation/UI/Gameplay/BossReward/BossRewardUIController.cs; Assets/Scripts/WIP/Legacy/Gameplay/NamedBoss/BossRewardResolver.cs

## Assets/Scripts/Core/Events/ScriptableObject/CooldownSnapshotEventChannelSO.cs
- 분류: 일반 소스; 49행
- 타입: CooldownSnapshot, CooldownSnapshotEventChannelSO : ScriptableObject
- 메서드: RaiseEvent
- 직렬화 참조 2개 / 지정 씬 참조 그래프 내 1개: Assets/ScriptableObject/EventChannel/UI/DashCooldownSnapshotEventChannel.asset; Assets/ScriptableObject/EventChannel/UI/ShockWaveCooldownSnapshotEventChannel.asset
- 타입 이름을 참조하는 소스 2개: Assets/Scripts/Gameplay/Entities/Shooter/Common/Core/ShooterDashController.cs; Assets/Scripts/Presentation/UI/Common/RadialCooldownWidget.cs

## Assets/Scripts/Core/Events/ScriptableObject/DamageNumberEventChannelSO.cs
- 분류: 일반 소스; 173행
- 타입: DamageNumberKind, DamageNumberRequest, DamageNumberEventChannelSO : DescriptionBaseSO
- 메서드: RaiseHealthDamage, RaiseInfectionDamage, RaiseHeal, RaiseDamage, RaiseDamageAt, RaiseHealthDamageAt, RaiseInfectionDamageAt, RaiseHealAt, RaiseEvent
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 1개: Assets/ScriptableObject/EventChannel/Character/DamageNumberEventChannel.asset
- 타입 이름을 참조하는 소스 4개: Assets/Scripts/Gameplay/Entities/Common/Core/Damageable.cs; Assets/Scripts/Gameplay/Items/Weapons/Runtime/BasicBullet.cs; Assets/Scripts/Gameplay/Items/Weapons/Runtime/ThrowableProjectile.cs; Assets/Scripts/Presentation/UI/Gameplay/DamageNumber/DamageNumberUIManager.cs

## Assets/Scripts/Core/Events/ScriptableObject/EnemyLootOpenRequestEventChannelSO.cs
- 분류: 일반 소스; 40행
- 타입: EnemyLootOpenRequest, EnemyLootOpenRequestEventChannelSO : ScriptableObject
- 메서드: RaiseEvent
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 1개: Assets/ScriptableObject/EventChannel/Character/Enemy/EnemyLootOpenRequestEventChannel.asset
- 타입 이름을 참조하는 소스 8개: Assets/Scripts/Gameplay/Entities/Shooter/Enemy/Common/EnemyLootInventoryRuntime.cs; Assets/Scripts/Gameplay/Items/Loot/LootableObject.cs; Assets/Scripts/Gameplay/World/Sectors/StageMap/StageSectorInstantiator.cs; Assets/Scripts/Gameplay/World/Sectors/Treasure/InheritanceWarehouseRuntime.cs; Assets/Scripts/Gameplay/World/Sectors/Treasure/SectorTreasure.cs; Assets/Scripts/Gameplay/World/Sectors/Treasure/StructureLootInventoryRuntime.cs; Assets/Scripts/Presentation/UI/Gameplay/PanelHub/Inventory/EnemyInventoryUI.cs; Assets/Scripts/Presentation/UI/Gameplay/PanelHub/PlayerPanelHub.cs

## Assets/Scripts/Core/Events/ScriptableObject/FloatEventChannelSO.cs
- 분류: 일반 소스; 17행
- 타입: FloatEventChannelSO : DescriptionBaseSO
- 메서드: RaiseEvent
- 직렬화 참조 6개 / 지정 씬 참조 그래프 내 5개: Assets/ScriptableObject/EventChannel/Audio/ChangeMasterVolume_Channel.asset; Assets/ScriptableObject/EventChannel/Audio/ChangeMusicVolume_Channel.asset; Assets/ScriptableObject/EventChannel/Audio/ChangeSFXVolume_Channel.asset; Assets/ScriptableObject/EventChannel/Character/RestoreHealthEventChannel.asset; Assets/ScriptableObject/EventChannel/Sector/InfectionControlDeltaEvent.asset; Assets/ScriptableObject/EventChannel/Stage/StageInfectionControlRecoverEventChannel.asset
- 타입 이름을 참조하는 소스 5개: Assets/Scripts/Gameplay/Combat/Infection/InfectionControlManager.cs; Assets/Scripts/Gameplay/Entities/Common/Core/Damageable.cs; Assets/Scripts/Presentation/Audio/AudioManager.cs; Assets/Scripts/Presentation/UI/Common/ESCMenu/SettingsSystem.cs; Assets/Scripts/Presentation/UI/Common/ESCMenu/UISettingAudioComponent.cs

## Assets/Scripts/Core/Events/ScriptableObject/GameObjectEventChannelSO.cs
- 분류: 일반 소스; 27행
- 타입: is, GameObjectEventChannelSO : DescriptionBaseSO
- 메서드: RaiseEvent
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 379개: Assets/Editor/InventorySortButtonMigration.cs; Assets/Editor/ItemCategoryMoveUtility.cs; Assets/Editor/ItemMakerWindow.cs; Assets/Editor/LootProgressionEditorWindow.cs; Assets/Editor/QuestDesignTreeWindow.cs; Assets/Editor/SaveInspectorWindow.cs; Assets/Editor/ShooterCustomizeStationWindow.cs; Assets/Editor/ShooterMeleeAnimationSetup.cs

## Assets/Scripts/Core/Events/ScriptableObject/HideoutStageSelectOpenRequestEventChannelSO.cs
- 분류: 일반 소스; 37행
- 타입: HideoutStageSelectOpenRequest, HideoutStageSelectOpenRequestEventChannelSO : ScriptableObject
- 메서드: RaiseEvent
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 2개: Assets/Scripts/Gameplay/Progression/Hideout/HideoutStageSelectStation.cs; Assets/Scripts/Presentation/UI/Gameplay/Hideout/HideoutStageSelectPanel.cs

## Assets/Scripts/Core/Events/ScriptableObject/HitReceivedEventChannelSO.cs
- 분류: 일반 소스; 21행
- 타입: HitReceivedEventChannelSO : DescriptionBaseSO
- 메서드: RaiseEvent
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 1개: Assets/ScriptableObject/EventChannel/Character/Hit Received Event Channel SO.asset
- 타입 이름을 참조하는 소스 6개: Assets/Scripts/Gameplay/Entities/Common/Core/Damageable.cs; Assets/Scripts/Gameplay/Entities/Common/Core/DamageFlashController.cs; Assets/Scripts/Gameplay/Entities/Shooter/Common/Effects/ShooterEffectController.cs; Assets/Scripts/Gameplay/Entities/Shooter/Enemy/StateMachine/EnemyShooterCombatController.cs; Assets/Scripts/Gameplay/Progression/Stage/StageProgressionManager.cs; Assets/Scripts/Presentation/Audio/ObstacleAudio.cs

## Assets/Scripts/Core/Events/ScriptableObject/InfectionControlEventChannelSO.cs
- 분류: 일반 소스; 32행
- 타입: InfectionControlSnapshot, InfectionControlEventChannelSO : ScriptableObject
- 메서드: RaiseEvent
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 1개: Assets/ScriptableObject/EventChannel/Sector/InfectionControlChangedEvent.asset
- 타입 이름을 참조하는 소스 2개: Assets/Scripts/Gameplay/Combat/Infection/InfectionControlManager.cs; Assets/Scripts/Presentation/UI/Gameplay/InfectionControl/InfectionControlHUD.cs

## Assets/Scripts/Core/Events/ScriptableObject/InfectionControlManagerReadyEventChannelSO.cs
- 분류: 일반 소스; 37행
- 타입: InfectionControlManagerReadyEventChannelSO : ScriptableObject
- 메서드: RaiseEvent, Clear
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 1개: Assets/ScriptableObject/EventChannel/Sector/InfectionControlManagerReadyEventChannel.asset
- 타입 이름을 참조하는 소스 2개: Assets/Scripts/Gameplay/Combat/Infection/InfectionControlManager.cs; Assets/Scripts/Gameplay/World/Sectors/Named_Boss/NamedRewardApplier.cs

## Assets/Scripts/Core/Events/ScriptableObject/InteractionCastBarEventChannelSO.cs
- 분류: 일반 소스; 64행
- 타입: InteractionCastBarSnapshot, InteractionCastBarEventChannelSO : ScriptableObject
- 메서드: RaiseEvent, Clear
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 1개: Assets/ScriptableObject/EventChannel/Interaction/InteractionCastBarEventChannel.asset
- 타입 이름을 참조하는 소스 6개: Assets/Scripts/Gameplay/Entities/Shooter/Player/Inventory/PlayerConsumableUseRuntime.cs; Assets/Scripts/Gameplay/Entities/Shooter/Player/PlayerAimController.cs; Assets/Scripts/Gameplay/Interaction/Core/InteractionManager.cs; Assets/Scripts/Presentation/Audio/InteractionSearchAudio.cs; Assets/Scripts/Presentation/UI/Gameplay/Interaction/InteractionBarUIManager.cs; Assets/Tests/EditMode/Editor/AudioGroundworkTests.cs

## Assets/Scripts/Core/Events/ScriptableObject/InteractionPromptEventChannelSO.cs
- 분류: 일반 소스; 55행
- 타입: InteractionPromptSnapshot, InteractionPromptEventChannelSO : ScriptableObject
- 메서드: RaiseEvent, Clear
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 1개: Assets/ScriptableObject/EventChannel/Interaction/InteractionPromptEventChannel.asset
- 타입 이름을 참조하는 소스 3개: Assets/Scripts/Gameplay/Interaction/Core/InteractionManager.cs; Assets/Scripts/Presentation/UI/Gameplay/Interaction/InteractionPromptScreenSpaceUIManager.cs; Assets/Scripts/Presentation/UI/Gameplay/Interaction/InteractionPromptWidget.cs

## Assets/Scripts/Core/Events/ScriptableObject/InteractionUIEventChannelSO.cs
- 분류: 일반 소스; 28행
- 타입: is, via, InteractionUIEventChannelSO : DescriptionBaseSO
- 메서드: RaiseEvent
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 1개: Assets/ScriptableObject/EventChannel/Interaction/ToggleInteractUI.asset
- 타입 이름을 참조하는 소스 379개: Assets/Editor/InventorySortButtonMigration.cs; Assets/Editor/ItemCategoryMoveUtility.cs; Assets/Editor/ItemMakerWindow.cs; Assets/Editor/LootProgressionEditorWindow.cs; Assets/Editor/QuestDesignTreeWindow.cs; Assets/Editor/SaveInspectorWindow.cs; Assets/Editor/ShooterCustomizeStationWindow.cs; Assets/Editor/ShooterMeleeAnimationSetup.cs

## Assets/Scripts/Core/Events/ScriptableObject/IntEventChannelSO.cs
- 분류: 일반 소스; 17행
- 타입: IntEventChannelSO : DescriptionBaseSO
- 메서드: RaiseEvent
- 직렬화 참조 6개 / 지정 씬 참조 그래프 내 1개: Assets/ScriptableObject/EventChannel/Health/Cure.asset; Assets/ScriptableObject/EventChannel/Health/InflictDamageEvent.asset; Assets/ScriptableObject/EventChannel/Health/SetHealthbarEvent.asset; Assets/ScriptableObject/EventChannel/Sector/RequestUnlockNextStageEvent.asset; Assets/ScriptableObject/EventChannel/Sector/StageApplied.asset; Assets/ScriptableObject/EventChannel/UI/WeaponAmmoSection/WeaponSlotSelectEventChannel.asset
- 타입 이름을 참조하는 소스 3개: Assets/Scripts/Core/Events/IntEventListener.cs; Assets/Scripts/Gameplay/Entities/Shooter/Player/PlayerBulletLoadoutRuntime.cs; Assets/Scripts/Gameplay/World/Sectors/Named_Boss/NamedSectorController.cs

## Assets/Scripts/Core/Events/ScriptableObject/ItemEventChannelSO.cs
- 분류: 일반 소스; 28행
- 타입: is, ItemEventChannelSO : DescriptionBaseSO
- 메서드: RaiseEvent
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 1개: Assets/ScriptableObject/EventChannel/Interaction/OnObjectPickup.asset
- 타입 이름을 참조하는 소스 379개: Assets/Editor/InventorySortButtonMigration.cs; Assets/Editor/ItemCategoryMoveUtility.cs; Assets/Editor/ItemMakerWindow.cs; Assets/Editor/LootProgressionEditorWindow.cs; Assets/Editor/QuestDesignTreeWindow.cs; Assets/Editor/SaveInspectorWindow.cs; Assets/Editor/ShooterCustomizeStationWindow.cs; Assets/Editor/ShooterMeleeAnimationSetup.cs

## Assets/Scripts/Core/Events/ScriptableObject/LoadEventChannelSO.cs
- 분류: 일반 소스; 34행
- 타입: is, LoadEventChannelSO : DescriptionBaseSO
- 메서드: RaiseEvent
- 직렬화 참조 3개 / 지정 씬 참조 그래프 내 3개: Assets/ScriptableObject/EventChannel/LoadMenuChannel.asset; Assets/ScriptableObject/EventChannel/SceneManagement/ColdStartup_Channel.asset; Assets/ScriptableObject/EventChannel/SceneManagement/LoadGameScene_Channel.asset
- 타입 이름을 참조하는 소스 379개: Assets/Editor/InventorySortButtonMigration.cs; Assets/Editor/ItemCategoryMoveUtility.cs; Assets/Editor/ItemMakerWindow.cs; Assets/Editor/LootProgressionEditorWindow.cs; Assets/Editor/QuestDesignTreeWindow.cs; Assets/Editor/SaveInspectorWindow.cs; Assets/Editor/ShooterCustomizeStationWindow.cs; Assets/Editor/ShooterMeleeAnimationSetup.cs

## Assets/Scripts/Core/Events/ScriptableObject/MaskRenderManagerEventChannelSO.cs
- 분류: 일반 소스; 38행
- 타입: MaskRenderManagerEventChannelSO : ScriptableObject
- 메서드: RaiseEvent, Clear
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 1개: Assets/ScriptableObject/EventChannel/Paint/MaskRenderManagerReadyEventChannel.asset
- 타입 이름을 참조하는 소스 18개: Assets/Scripts/Gameplay/Entities/Creature/CommonEnemyStateMachine/Action/EnemyFireArcBombActionSO.cs; Assets/Scripts/Gameplay/Entities/Creature/EnemyArcBombProjectile.cs; Assets/Scripts/Gameplay/Entities/Creature/EnemyVirusTrail.cs; Assets/Scripts/Gameplay/Entities/Creature/NamedEnemy/Mutarus/Action/MutarusQTEPatternActionSO.cs; Assets/Scripts/Gameplay/Entities/Creature/NamedEnemy/Mutarus/ScriptableObject/MutarusPatternConfigSO.cs; Assets/Scripts/Gameplay/Entities/Creature/NamedEnemy/NamedBattleSectorResetter.cs; Assets/Scripts/Gameplay/Entities/Creature/NormalEnemy/Chaser/ScriptableObject/ChaserSelfDestructConfigSO.cs; Assets/Scripts/Gameplay/Entities/Creature/NormalEnemy/Chaser/StateMachine/Action/ChaserVirusSelfDestructActionSO.cs

## Assets/Scripts/Core/Events/ScriptableObject/MetaProgressChangedEventChannelSO.cs
- 분류: 일반 소스; 36행
- 타입: MetaProgressChangedEventChannelSO : ScriptableObject
- 메서드: RaiseEvent, Clear
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 1개: Assets/ScriptableObject/EventChannel/METAUpgrade/MetaProgressChangedEventChannel.asset
- 타입 이름을 참조하는 소스 3개: Assets/Scripts/Gameplay/Progression/Meta/MetaProgressController.cs; Assets/Scripts/Presentation/UI/Gameplay/MetaSystem/MetaStatsPanelUI.cs; Assets/Scripts/Presentation/UI/Gameplay/MetaSystem/MetaUpgradePanelUI.cs

## Assets/Scripts/Core/Events/ScriptableObject/MutarusQTEPatternControllerEventChannelSO.cs
- 분류: 일반 소스; 38행
- 타입: MutarusQTEPatternControllerEventChannelSO : ScriptableObject
- 메서드: RaiseEvent, Clear
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 1개: Assets/ScriptableObject/EventChannel/Character/Enemy/NamedEnemy/Mutarus/MutarusQTEPatternControllerEventChannel.asset
- 타입 이름을 참조하는 소스 3개: Assets/Scripts/Gameplay/Entities/Creature/NamedEnemy/Mutarus/Action/MutarusQTEPatternActionSO.cs; Assets/Scripts/Gameplay/Entities/Creature/NamedEnemy/Mutarus/MutarusQTEPatternController.cs; Assets/Scripts/Gameplay/Entities/Creature/NamedEnemy/Mutarus/MutarusQTEStation.cs

## Assets/Scripts/Core/Events/ScriptableObject/MutarusQTEPatternResetRequestEventChannelSO.cs
- 분류: 일반 소스; 23행
- 타입: MutarusQTEPatternResetRequestEventChannelSO : ScriptableObject
- 메서드: RaiseEvent
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 1개: Assets/ScriptableObject/EventChannel/Character/Enemy/NamedEnemy/Mutarus/MutarusQTEPatternResetRequestEventChannel.asset
- 타입 이름을 참조하는 소스 3개: Assets/Scripts/Gameplay/Entities/Creature/NamedEnemy/Mutarus/MutarusNamedPatternRuntimeResetter.cs; Assets/Scripts/Gameplay/Entities/Creature/NamedEnemy/Mutarus/MutarusQTEPatternController.cs; Assets/Scripts/Gameplay/Entities/Creature/NamedEnemy/NamedBattleSectorResetter.cs

## Assets/Scripts/Core/Events/ScriptableObject/MutarusQTEStationGroupEventChannelSO.cs
- 분류: 일반 소스; 38행
- 타입: MutarusQTEStationGroupEventChannelSO : ScriptableObject
- 메서드: RaiseEvent, Clear
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 1개: Assets/ScriptableObject/EventChannel/Character/Enemy/NamedEnemy/Mutarus/MutarusQTEStationGroupEventChannel.asset
- 타입 이름을 참조하는 소스 2개: Assets/Scripts/Gameplay/Entities/Creature/NamedEnemy/Mutarus/MutarusQTEPatternController.cs; Assets/Scripts/Gameplay/Entities/Creature/NamedEnemy/Mutarus/MutarusQTEStationGroup.cs

## Assets/Scripts/Core/Events/ScriptableObject/NamedBattleSectorResetRequestEventChannelSO.cs
- 분류: 일반 소스; 23행
- 타입: NamedBattleSectorResetRequestEventChannelSO : ScriptableObject
- 메서드: RaiseEvent
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 0개: Assets/ScriptableObject/EventChannel/Sector/Named_Boss/NamedBattleSectorResetRequestEventChannel.asset
- 타입 이름을 참조하는 소스 2개: Assets/Scripts/Gameplay/Entities/Creature/NamedEnemy/Action/RaiseNamedEnemyKilledActionSO.cs; Assets/Scripts/Gameplay/Entities/Creature/NamedEnemy/NamedBattleSectorResetter.cs

## Assets/Scripts/Core/Events/ScriptableObject/NamedBattleSignalEventChannelSO.cs
- 분류: 일반 소스; 23행
- 타입: NamedBattleSignalEventChannelSO : ScriptableObject
- 메서드: RaiseEvent
- 직렬화 참조 2개 / 지정 씬 참조 그래프 내 2개: Assets/ScriptableObject/EventChannel/Sector/Named_Boss/NamedBattleEndSignalEventChannel.asset; Assets/ScriptableObject/EventChannel/Sector/Named_Boss/NamedBattleStartSignalEventChannel.asset
- 타입 이름을 참조하는 소스 2개: Assets/Scripts/Gameplay/World/Sectors/Named_Boss/NamedBattleManager.cs; Assets/Scripts/Gameplay/World/Sectors/Named_Boss/NamedSectorController.cs

## Assets/Scripts/Core/Events/ScriptableObject/NamedEnemySpawnInfoEventChannelSO.cs
- 분류: 일반 소스; 40행
- 타입: NamedEnemySpawnInfo, NamedEnemySpawnInfoEventChannelSO : ScriptableObject
- 메서드: RaiseEvent
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 0개: Assets/ScriptableObject/EventChannel/Sector/Named_Boss/NamedEnemySpawnInfoEventChannel.asset
- 타입 이름을 참조하는 소스 2개: Assets/Scripts/Gameplay/Entities/Creature/NamedEnemy/NamedEnemy.cs; Assets/Scripts/Gameplay/World/Sectors/Named_Boss/NamedSectorController.cs

## Assets/Scripts/Core/Events/ScriptableObject/NamedOutsidePressureRequestEventChannelSO.cs
- 분류: 일반 소스; 40행
- 타입: NamedOutsidePressureRequest, NamedOutsidePressureRequestEventChannelSO : ScriptableObject
- 메서드: RaiseEvent
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 0개: Assets/ScriptableObject/EventChannel/Sector/Named_Boss/NamedOutsidePressureRequestChannel.asset
- 타입 이름을 참조하는 소스 1개: Assets/Scripts/Gameplay/World/Sectors/Named_Boss/NamedOutsideController.cs

## Assets/Scripts/Core/Events/ScriptableObject/NamedPatternDurationEventChannelSO.cs
- 분류: 일반 소스; 63행
- 타입: NamedPatternDurationSnapshot, NamedPatternDurationEventChannelSO : ScriptableObject
- 메서드: RaiseEvent
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 1개: Assets/ScriptableObject/EventChannel/Character/Enemy/NamedEnemy/Mutarus/Mutarus_NamedPatternDurationEventChannel.asset
- 타입 이름을 참조하는 소스 2개: Assets/Scripts/Gameplay/Entities/Creature/NamedEnemy/NamedPatternController.cs; Assets/Scripts/Presentation/UI/Gameplay/PatternUI/NamedPatternDurationBarUI.cs

## Assets/Scripts/Core/Events/ScriptableObject/NamedSectorControllerReadyEventChannelSO.cs
- 분류: 일반 소스; 37행
- 타입: NamedSectorControllerReadyEventChannelSO : ScriptableObject
- 메서드: RaiseEvent, Clear
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 0개: Assets/ScriptableObject/EventChannel/Sector/Named_Boss/NamedSectorControllerReadyEventChannel.asset
- 타입 이름을 참조하는 소스 2개: Assets/Scripts/Gameplay/World/Sectors/Named_Boss/NamedRewardApplier.cs; Assets/Scripts/Gameplay/World/Sectors/Named_Boss/NamedSectorController.cs

## Assets/Scripts/Core/Events/ScriptableObject/NamedSectorPhaseEventChannelSO.cs
- 분류: 일반 소스; 48행
- 타입: NamedSectorPhase, NamedSectorPhaseChange, NamedSectorPhaseEventChannelSO : ScriptableObject
- 메서드: RaiseEvent
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 1개: Assets/ScriptableObject/EventChannel/Sector/Named_Boss/NamedSectorPhaseEventChannel.asset
- 타입 이름을 참조하는 소스 8개: Assets/Scripts/Core/Events/ScriptableObject/NamedSectorTimerSnapshotEventChannelSO.cs; Assets/Scripts/Gameplay/Combat/Infection/InfectionControlManager.cs; Assets/Scripts/Gameplay/Combat/Infection/ScriptableObject/InfectionControlRulesSO.cs; Assets/Scripts/Gameplay/World/Sectors/Named_Boss/NamedOutsideController.cs; Assets/Scripts/Gameplay/World/Sectors/Named_Boss/NamedSectorController.cs; Assets/Scripts/Gameplay/World/Sectors/Named_Boss/NamedSectorOccupancyProjection.cs; Assets/Scripts/Gameplay/World/Sectors/Runtime/SectorOccupancyManager.cs; Assets/Scripts/Presentation/UI/Gameplay/Map/MiniMap/MiniMapUI.cs

## Assets/Scripts/Core/Events/ScriptableObject/NamedSectorTimerSnapshotEventChannelSO.cs
- 분류: 일반 소스; 49행
- 타입: NamedSectorTimerSnapshot, NamedSectorTimerSnapshotEventChannelSO : ScriptableObject
- 메서드: RaiseEvent
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 1개: Assets/ScriptableObject/EventChannel/UI/NamedSectorTimerSnapshotEventChannel.asset
- 타입 이름을 참조하는 소스 2개: Assets/Scripts/Gameplay/World/Sectors/Named_Boss/NamedSectorController.cs; Assets/Scripts/Presentation/UI/Gameplay/Map/MiniMap/MiniMapUI.cs

## Assets/Scripts/Core/Events/ScriptableObject/ObjectAudioEventChannelSO.cs
- 분류: 일반 소스; 64행
- 타입: ObjectAudioCueType, ObjectAudioRequest, ObjectAudioEventChannelSO : DescriptionBaseSO
- 메서드: RaiseEvent
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 1개: Assets/ScriptableObject/EventChannel/Audio/ObjectAudioEventChannel.asset
- 타입 이름을 참조하는 소스 2개: Assets/Scripts/Gameplay/Entities/Shooter/Player/PlayerShooterCharacterAudio.cs; Assets/Scripts/Presentation/Audio/ObstacleAudio.cs

## Assets/Scripts/Core/Events/ScriptableObject/PanelHub/PlayerStatsChangedEventChannelSO.cs
- 분류: 일반 소스; 36행
- 타입: PlayerStatsChangedEventChannelSO : ScriptableObject
- 메서드: RaiseEvent, Clear
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 1개: Assets/ScriptableObject/EventChannel/PanelHub/PlayerStatsChangedEventChannel.asset
- 타입 이름을 참조하는 소스 3개: Assets/Scripts/Gameplay/Entities/Shooter/Player/PlayerStatsRuntime.cs; Assets/Scripts/Presentation/UI/Gameplay/PanelHub/PlayerStatsSummaryPanelUI.cs; Assets/Scripts/Presentation/UI/Gameplay/Slot/SlotHUD.cs

## Assets/Scripts/Core/Events/ScriptableObject/PanelHub/UIOverlayRequestEventChannelSO.cs
- 분류: 일반 소스; 68행
- 타입: UIOverlayRequestType, UIOverlayRequest, UIOverlayRequestEventChannelSO : ScriptableObject
- 메서드: Open, Close, Toggle, CloseAll, RaiseEvent
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 1개: Assets/ScriptableObject/EventChannel/UI/UIOverlayRequestEventChannel.asset
- 타입 이름을 참조하는 소스 12개: Assets/Scripts/Gameplay/Entities/Shooter/Enemy/Common/EnemyLootInventoryRuntime.cs; Assets/Scripts/Gameplay/Entities/Shooter/Player/Inventory/PlayerConsumableUseRuntime.cs; Assets/Scripts/Gameplay/Items/Loot/LootableObject.cs; Assets/Scripts/Gameplay/World/Sectors/Shop/SectorShop.cs; Assets/Scripts/Gameplay/World/Sectors/StageMap/StageSectorInstantiator.cs; Assets/Scripts/Gameplay/World/Sectors/Treasure/InheritanceWarehouseRuntime.cs; Assets/Scripts/Gameplay/World/Sectors/Treasure/SectorTreasure.cs; Assets/Scripts/Gameplay/World/Sectors/Treasure/StructureLootInventoryRuntime.cs

## Assets/Scripts/Core/Events/ScriptableObject/PanelHub/UIVisibilitySuppressRequestEventChannelSO.cs
- 분류: 일반 소스; 82행
- 타입: UIVisibilitySuppressRequest, UIVisibilitySuppressRequestEventChannelSO : ScriptableObject
- 메서드: RaiseEvent, IsSuppressed, ClearSource, NormalizeId
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 1개: Assets/ScriptableObject/EventChannel/UI/UIVisibilitySuppressRequestEventChannel.asset
- 타입 이름을 참조하는 소스 2개: Assets/Scripts/Presentation/UI/Gameplay/Tool/CanvasGroupVisibilitySuppressSource.cs; Assets/Scripts/Presentation/UI/Gameplay/Tool/CanvasGroupVisibilitySuppressTarget.cs

## Assets/Scripts/Core/Events/ScriptableObject/PanelHub/WeaponAmmoSellConfirmRequestEventChannelSO.cs
- 분류: 일반 소스; 23행
- 타입: WeaponAmmoSellConfirmRequestEventChannelSO : ScriptableObject
- 메서드: RaiseEvent
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 1개: Assets/ScriptableObject/EventChannel/UI/WeaponAmmoSection/WeaponAmmoSellConfirmRequestEventChannel.asset
- 타입 이름을 참조하는 소스 2개: Assets/Scripts/Gameplay/Entities/Shooter/Player/PlayerBulletLoadoutRuntime.cs; Assets/Scripts/Presentation/UI/Gameplay/Slot/AmmoSlotSellPopupUI.cs

## Assets/Scripts/Core/Events/ScriptableObject/PanelHub/WeaponAmmoSellPopupRequestEventChannelSO.cs
- 분류: 일반 소스; 23행
- 타입: WeaponAmmoSellPopupRequestEventChannelSO : ScriptableObject
- 메서드: RaiseEvent
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 0개: Assets/ScriptableObject/EventChannel/UI/WeaponAmmoSection/WeaponAmmoSellPopupRequestEventChannel.asset
- 타입 이름을 참조하는 소스 1개: Assets/Scripts/Presentation/UI/Gameplay/Slot/AmmoSlotSellPopupUI.cs

## Assets/Scripts/Core/Events/ScriptableObject/PanelHub/WeaponAmmoSellResultEventChannelSO.cs
- 분류: 일반 소스; 23행
- 타입: WeaponAmmoSellResultEventChannelSO : ScriptableObject
- 메서드: RaiseEvent
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 1개: Assets/ScriptableObject/EventChannel/UI/Shop/WeaponAmmoSellResultEventChannel.asset
- 타입 이름을 참조하는 소스 1개: Assets/Scripts/Gameplay/Entities/Shooter/Player/PlayerBulletLoadoutRuntime.cs

## Assets/Scripts/Core/Events/ScriptableObject/PanelHub/WeaponAmmoShopPurchaseRequestEventChannelSO.cs
- 분류: 일반 소스; 23행
- 타입: WeaponAmmoShopPurchaseRequestEventChannelSO : ScriptableObject
- 메서드: RaiseEvent
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 1개: Assets/ScriptableObject/EventChannel/UI/Shop/WeaponAmmoShopPurchaseRequestEventChannel.asset
- 타입 이름을 참조하는 소스 1개: Assets/Scripts/Gameplay/Entities/Shooter/Player/PlayerBulletLoadoutRuntime.cs

## Assets/Scripts/Core/Events/ScriptableObject/PanelHub/WeaponAmmoShopPurchaseResultEventChannelSO.cs
- 분류: 일반 소스; 23행
- 타입: WeaponAmmoShopPurchaseResultEventChannelSO : ScriptableObject
- 메서드: RaiseEvent
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 1개: Assets/ScriptableObject/EventChannel/UI/Shop/WeaponAmmoShopPurchaseResultEventChannel.asset
- 타입 이름을 참조하는 소스 1개: Assets/Scripts/Gameplay/Entities/Shooter/Player/PlayerBulletLoadoutRuntime.cs

## Assets/Scripts/Core/Events/ScriptableObject/PlayerBulletLoadoutRuntimeReadyEventChannelSO.cs
- 분류: 일반 소스; 34행
- 타입: PlayerBulletLoadoutRuntimeReadyEventChannelSO : ScriptableObject
- 메서드: RaiseEvent, Clear
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 1개: Assets/ScriptableObject/EventChannel/Character/Player/PlayerBulletLoadoutRuntimeReadyEventChannel.asset
- 타입 이름을 참조하는 소스 3개: Assets/Scripts/Gameplay/Entities/Shooter/Player/PlayerAimAction.cs; Assets/Scripts/Gameplay/Entities/Shooter/Player/PlayerAimController.cs; Assets/Scripts/Gameplay/Entities/Shooter/Player/PlayerBulletLoadoutRuntime.cs

## Assets/Scripts/Core/Events/ScriptableObject/PlayerCurrencyChangedEventChannelSO.cs
- 분류: 일반 소스; 55행
- 타입: PlayerCurrencyType, PlayerCurrencySnapshot, PlayerCurrencyChangedEventChannelSO : ScriptableObject
- 메서드: RaiseEvent, Clear
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 1개: Assets/ScriptableObject/EventChannel/Currency/PlayerCurrencyChangedEventChannel.asset
- 타입 이름을 참조하는 소스 1개: Assets/Scripts/Presentation/UI/Gameplay/Currency/PlayerCurrencyRuntime.cs

## Assets/Scripts/Core/Events/ScriptableObject/PlayerCurrencyRuntimeReadyEventChannelSO.cs
- 분류: 일반 소스; 34행
- 타입: PlayerCurrencyRuntimeReadyEventChannelSO : ScriptableObject
- 메서드: RaiseEvent, Clear
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 1개: Assets/ScriptableObject/EventChannel/Currency/PlayerCurrencyRuntimeReadyEventChannel.asset
- 타입 이름을 참조하는 소스 0개: 

## Assets/Scripts/Core/Events/ScriptableObject/PlayerHealthEventChannelSO.cs
- 분류: 일반 소스; 65행
- 타입: PlayerHealthSnapshot, PlayerHealthEventChannelSO : ScriptableObject
- 메서드: RaiseEvent, Clear
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 1개: Assets/ScriptableObject/EventChannel/Health/PlayerHealthEventChannel.asset
- 타입 이름을 참조하는 소스 3개: Assets/Scripts/Gameplay/Entities/Shooter/Player/PlayerShooterInfection.cs; Assets/Scripts/Presentation/UI/Gameplay/PlayerInfo/PlayerInfoHUD.cs; Assets/Scripts/Presentation/UI/Gameplay/ScreenEffect/LowHealthScreenEffectUI.cs

## Assets/Scripts/Core/Events/ScriptableObject/PlayerInfectionEventChannelSO.cs
- 분류: 일반 소스; 38행
- 타입: PlayerInfectionEventChannelSO : ScriptableObject
- 메서드: RaiseEvent, Clear
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 1개: Assets/ScriptableObject/EventChannel/Character/Player/PlayerInfection/PlayerInfectionReadyEventChannel.asset
- 타입 이름을 참조하는 소스 3개: Assets/Scripts/Gameplay/Entities/Shooter/Player/PlayerShooterInfection.cs; Assets/Scripts/Gameplay/World/Sectors/Named_Boss/NamedRewardApplier.cs; Assets/Scripts/Presentation/UI/Gameplay/PlayerInfo/PlayerInfoHUD.cs

## Assets/Scripts/Core/Events/ScriptableObject/PlayerInventoryRuntimeReadyEventChannelSO.cs
- 분류: 일반 소스; 34행
- 타입: PlayerInventoryRuntimeReadyEventChannelSO : ScriptableObject
- 메서드: RaiseEvent, Clear
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 1개: Assets/ScriptableObject/EventChannel/Character/Player/PlayerInventoryRuntimeReadyEventChannel.asset
- 타입 이름을 참조하는 소스 6개: Assets/Scripts/Gameplay/Entities/Shooter/Player/Inventory/PlayerInventoryRuntime.cs; Assets/Scripts/Gameplay/Entities/Shooter/Player/PlayerShooterCharacterAudio.cs; Assets/Scripts/Presentation/UI/Gameplay/PanelHub/Inventory/EnemyInventoryUI.cs; Assets/Scripts/Presentation/UI/Gameplay/PanelHub/Inventory/PlayerInventoryUI.cs; Assets/Scripts/Presentation/UI/Gameplay/Shop/PlayerShopPanelUI.cs; Assets/Scripts/Presentation/UI/Gameplay/Slot/SlotHUD.cs

## Assets/Scripts/Core/Events/ScriptableObject/PlayerRuntimeReadyEventChannelSO.cs
- 분류: 일반 소스; 38행
- 타입: PlayerRuntimeReadyEventChannelSO : ScriptableObject
- 메서드: RaiseEvent, Clear
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 1개: Assets/ScriptableObject/EventChannel/Character/Player/PlayerRuntimeReadyEventChannel.asset
- 타입 이름을 참조하는 소스 5개: Assets/Scripts/Gameplay/Entities/Creature/CommonEnemyStateMachine/Action/EnemyAcquirePlayerTargetActionSO.cs; Assets/Scripts/Gameplay/Entities/Creature/NamedEnemy/Action/NamedEnemyAcquirePlayerTargetActionSO.cs; Assets/Scripts/Gameplay/Entities/Shooter/Player/PlayerStatsRuntime.cs; Assets/Scripts/Gameplay/World/Sectors/Named_Boss/NamedSectorTransitionController.cs; Assets/Scripts/Presentation/UI/Common/ESCMenu/PlayerSectorCenterRescue.cs

## Assets/Scripts/Core/Events/ScriptableObject/PlayerStatsRuntimeReadyEventChannelSO.cs
- 분류: 일반 소스; 34행
- 타입: PlayerStatsRuntimeReadyEventChannelSO : ScriptableObject
- 메서드: RaiseEvent, Clear
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 1개: Assets/ScriptableObject/EventChannel/Character/Player/PlayerStatsRuntimeReadyEventChannel.asset
- 타입 이름을 참조하는 소스 3개: Assets/Scripts/Gameplay/Entities/Shooter/Player/PlayerBulletLoadoutRuntime.cs; Assets/Scripts/Gameplay/Entities/Shooter/Player/PlayerStatsRuntime.cs; Assets/Scripts/Presentation/UI/Gameplay/Slot/SlotHUD.cs

## Assets/Scripts/Core/Events/ScriptableObject/ScreenTransitionRequestEventChannelSO.cs
- 분류: 일반 소스; 48행
- 타입: ScreenTransitionRequest, ScreenTransitionRequestEventChannelSO : ScriptableObject
- 메서드: RaiseEvent
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 1개: Assets/ScriptableObject/EventChannel/SceneManagement/ScreenTransitionRequestEventChannel.asset
- 타입 이름을 참조하는 소스 4개: Assets/Scripts/Core/SceneFlow/GameplayScreenTransitionRunner.cs; Assets/Scripts/Core/SceneFlow/TransitionInterfaceController.cs; Assets/Scripts/Gameplay/World/Sectors/Named_Boss/NamedSectorTransitionController.cs; Assets/Scripts/Gameplay/World/Sectors/Runtime/SectorPortalManager.cs

## Assets/Scripts/Core/Events/ScriptableObject/SectorMapSnapshotEventChannelSO.cs
- 분류: 일반 소스; 53행
- 타입: SectorMapCellSnapshot, SectorMapSnapshot, SectorMapSnapshotEventChannelSO : ScriptableObject
- 메서드: RaiseEvent
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 1개: Assets/ScriptableObject/EventChannel/Sector/SectorMapSnapshotChanged.asset
- 타입 이름을 참조하는 소스 4개: Assets/Scripts/Gameplay/World/Sectors/Runtime/SectorOccupancyManager.cs; Assets/Scripts/Presentation/UI/Gameplay/Map/FullMap/FullMapUI.cs; Assets/Scripts/Presentation/UI/Gameplay/Map/MiniMap/MiniMapUI.cs; Assets/Tests/EditMode/Editor/RaidRuntimeContractTests.cs

## Assets/Scripts/Core/Events/ScriptableObject/SectorOccupancyEventChannelSO.cs
- 분류: 일반 소스; 16행
- 타입: SectorOccupancyEventChannelSO : ScriptableObject
- 메서드: RaiseEvent
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 1개: Assets/ScriptableObject/EventChannel/Sector/SectorOccupancyChangedEvent.asset
- 타입 이름을 참조하는 소스 3개: Assets/Scripts/Gameplay/World/Sectors/Named_Boss/NamedSectorOccupancyProjection.cs; Assets/Scripts/Gameplay/World/Sectors/Runtime/SectorOccupancyManager.cs; Assets/Scripts/Gameplay/World/Sectors/SectorOccupancy.cs

## Assets/Scripts/Core/Events/ScriptableObject/SectorOccupancySummaryEventChannelSO.cs
- 분류: 일반 소스; 16행
- 타입: SectorOccupancySummaryEventChannelSO : ScriptableObject
- 메서드: RaiseEvent
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 1개: Assets/ScriptableObject/EventChannel/Sector/SectorOccupancySummaryChanged.asset
- 타입 이름을 참조하는 소스 2개: Assets/Scripts/Gameplay/Combat/Infection/InfectionControlManager.cs; Assets/Scripts/Gameplay/World/Sectors/Runtime/SectorOccupancyManager.cs

## Assets/Scripts/Core/Events/ScriptableObject/SectorShopOpenRequestEventChannelSO.cs
- 분류: 일반 소스; 121행
- 타입: SectorShopOpenRequest, SectorShopOpenRequestEventChannelSO : ScriptableObject
- 메서드: BuildMergedOffers, SplitOffers, RaiseEvent
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 1개: Assets/ScriptableObject/EventChannel/Sector/SectorShopOpenRequestEventChannel.asset
- 타입 이름을 참조하는 소스 3개: Assets/Scripts/Gameplay/World/Sectors/Shop/SectorShop.cs; Assets/Scripts/Presentation/UI/Gameplay/PanelHub/PlayerPanelHub.cs; Assets/Scripts/Presentation/UI/Gameplay/Shop/PlayerShopPanelUI.cs

## Assets/Scripts/Core/Events/ScriptableObject/SectorStateManagerReadyEventChannelSO.cs
- 분류: 일반 소스; 38행
- 타입: SectorStateManagerReadyEventChannelSO : ScriptableObject
- 메서드: RaiseEvent, Clear
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 1개: Assets/ScriptableObject/EventChannel/Sector/SectorStateManagerReadyEventChannel.asset
- 타입 이름을 참조하는 소스 10개: Assets/Scripts/Gameplay/Progression/Stage/StageProgressionManager.cs; Assets/Scripts/Gameplay/World/Sectors/Named_Boss/NamedBattleManager.cs; Assets/Scripts/Gameplay/World/Sectors/Named_Boss/NamedRewardApplier.cs; Assets/Scripts/Gameplay/World/Sectors/Named_Boss/NamedSectorController.cs; Assets/Scripts/Gameplay/World/Sectors/Runtime/SectorOccupancyManager.cs; Assets/Scripts/Gameplay/World/Sectors/Runtime/SectorPortalManager.cs; Assets/Scripts/Gameplay/World/Sectors/Runtime/SectorStateManager.cs; Assets/Scripts/Gameplay/World/Sectors/SectorEnemySpawner.cs

## Assets/Scripts/Core/Events/ScriptableObject/ShootHitConfirmedEventChannelSO.cs
- 분류: 일반 소스; 21행
- 타입: ShootHitConfirmedEventChannelSO : DescriptionBaseSO
- 메서드: RaiseEvent
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 1개: Assets/ScriptableObject/EventChannel/Character/Shoot Hit Confirmed Event Channel SO.asset
- 타입 이름을 참조하는 소스 6개: Assets/Scripts/Gameplay/Entities/Shooter/Common/Core/EntityShootCore.cs; Assets/Scripts/Gameplay/Entities/Shooter/Player/PlayerAimCursorUI.cs; Assets/Scripts/Gameplay/Entities/Shooter/Player/PlayerShooterCharacterAudio.cs; Assets/Scripts/Gameplay/Entities/Shooter/Player/PlayerShooterShoot.cs; Assets/Scripts/Gameplay/Items/Weapons/Runtime/BasicBullet.cs; Assets/Scripts/WIP/Legacy/Gameplay/Player/VSplatterAimUI.cs

## Assets/Scripts/Core/Events/ScriptableObject/SoundStimulusEventChannelSO.cs
- 분류: 일반 소스; 24행
- 타입: SoundStimulusEventChannelSO : DescriptionBaseSO
- 메서드: RaiseEvent
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 1개: Assets/ScriptableObject/EventChannel/Audio/Sound Stimulus Event Channel SO.asset
- 타입 이름을 참조하는 소스 12개: Assets/Scripts/Gameplay/Entities/Shooter/Common/Core/EntityFootstepSoundEmitter.cs; Assets/Scripts/Gameplay/Entities/Shooter/Common/Core/EntityShootCore.cs; Assets/Scripts/Gameplay/Entities/Shooter/Common/Core/ProjectileSoundStimulusEmitter.cs; Assets/Scripts/Gameplay/Entities/Shooter/Common/Core/ShooterThrowableAttack.cs; Assets/Scripts/Gameplay/Entities/Shooter/Enemy/Common/EnemyShooterShoot.cs; Assets/Scripts/Gameplay/Entities/Shooter/Enemy/StateMachine/EnemyShooterCombatController.cs; Assets/Scripts/Gameplay/Entities/Shooter/Player/PlayerShooterShoot.cs; Assets/Scripts/Gameplay/Entities/Shooter/Player/PlayerWorldSoundIndicator.cs

## Assets/Scripts/Core/Events/ScriptableObject/StageProgressSnapshotEventChannelSO.cs
- 분류: 일반 소스; 65행
- 타입: StageProgressSnapshot, StageProgressSnapshotEventChannelSO : ScriptableObject
- 메서드: RaiseEvent, Clear
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 1개: Assets/ScriptableObject/EventChannel/Sector/StageProgressSnapshotChanged.asset
- 타입 이름을 참조하는 소스 3개: Assets/Scripts/Gameplay/Progression/Stage/StageProgressionManager.cs; Assets/Scripts/Gameplay/World/Sectors/SectorEnemySpawner.cs; Assets/Scripts/Presentation/UI/Gameplay/PlayerTimer/PlayerTimerHUD.cs

## Assets/Scripts/Core/Events/ScriptableObject/SystemMessageEventChannelSO.cs
- 분류: 일반 소스; 36행
- 타입: SystemMessageRequest, SystemMessageEventChannelSO : ScriptableObject
- 메서드: RaiseEvent
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 1개: Assets/ScriptableObject/EventChannel/UI/SystemMessageEventChannel.asset
- 타입 이름을 참조하는 소스 10개: Assets/Scripts/Gameplay/Entities/Shooter/Player/Inventory/PlayerBackpackDropInput.cs; Assets/Scripts/Gameplay/Entities/Shooter/Player/Inventory/PlayerConsumableUseRuntime.cs; Assets/Scripts/Gameplay/Entities/Shooter/Player/Inventory/PlayerQuickSlotRuntime.cs; Assets/Scripts/Gameplay/Entities/Shooter/Player/PlayerShooterShoot.cs; Assets/Scripts/Gameplay/Progression/Quests/QuestRuntime.cs; Assets/Scripts/Presentation/UI/Gameplay/PanelHub/Inventory/EnemyInventoryUI.cs; Assets/Scripts/Presentation/UI/Gameplay/PanelHub/Inventory/PlayerInventoryUI.cs; Assets/Scripts/Presentation/UI/Gameplay/Shop/PlayerShopPanelUI.cs

## Assets/Scripts/Core/Events/ScriptableObject/TransformEventChannelSO.cs
- 분류: 일반 소스; 27행
- 타입: is, TransformEventChannelSO : DescriptionBaseSO
- 메서드: RaiseEvent
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 1개: Assets/ScriptableObject/EventChannel/Gameplay/PlayerInstantiated.asset
- 타입 이름을 참조하는 소스 380개: Assets/Editor/InventorySortButtonMigration.cs; Assets/Editor/ItemCategoryMoveUtility.cs; Assets/Editor/ItemMakerWindow.cs; Assets/Editor/LootProgressionEditorWindow.cs; Assets/Editor/QuestDesignTreeWindow.cs; Assets/Editor/SaveInspectorWindow.cs; Assets/Editor/ShooterCustomizeStationWindow.cs; Assets/Editor/ShooterMeleeAnimationSetup.cs

## Assets/Scripts/Core/Events/ScriptableObject/UI/FadeChannelSO.cs
- 분류: 일반 소스; 46행
- 타입: FadeChannelSO : DescriptionBaseSO
- 메서드: FadeIn, FadeOut, Fade
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 1개: Assets/ScriptableObject/EventChannel/UI/FadeEventChannel.asset
- 타입 이름을 참조하는 소스 1개: Assets/Scripts/Core/SceneFlow/SceneLoader.cs

## Assets/Scripts/Core/Events/ScriptableObject/UI/PlayableDirectorChannelSO.cs
- 분류: 일반 소스; 23행
- 타입: PlayableDirectorChannelSO : DescriptionBaseSO
- 메서드: RaiseEvent
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 1개: Assets/ScriptableObject/EventChannel/Cutscene/PlayCutsceneEvent.asset
- 타입 이름을 참조하는 소스 1개: Assets/Scripts/Gameplay/Interaction/Core/InteractionManager.cs

## Assets/Scripts/Core/Events/ScriptableObject/UIAudioEventChannelSO.cs
- 분류: 일반 소스; 42행
- 타입: UIAudioCueType, UIAudioEventChannelSO : DescriptionBaseSO
- 메서드: RaiseEvent
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 1개: Assets/ScriptableObject/EventChannel/Audio/UIAudioEventChannel.asset
- 타입 이름을 참조하는 소스 11개: Assets/Scripts/Presentation/Audio/UIAudio.cs; Assets/Scripts/Presentation/Audio/UIButtonAudioEventEmitter.cs; Assets/Scripts/Presentation/UI/Gameplay/Overlay/UIOverlayManager.cs; Assets/Scripts/Presentation/UI/Gameplay/PanelHub/Inventory/EnemyInventoryUI.cs; Assets/Scripts/Presentation/UI/Gameplay/PanelHub/Inventory/EquipmentInventorySlotUI.cs; Assets/Scripts/Presentation/UI/Gameplay/PanelHub/Inventory/InventoryItemSlotUI.cs; Assets/Scripts/Presentation/UI/Gameplay/PanelHub/Inventory/PlayerInventoryUI.cs; Assets/Scripts/Presentation/UI/Gameplay/PanelHub/QuestMissionPanelUI.cs

## Assets/Scripts/Core/Events/ScriptableObject/VoidEventChannelSO.cs
- 분류: 일반 소스; 17행
- 타입: VoidEventChannelSO : DescriptionBaseSO
- 메서드: RaiseEvent
- 직렬화 참조 17개 / 지정 씬 참조 그래프 내 14개: Assets/ScriptableObject/EventChannel/Camera/CameraShake.asset; Assets/ScriptableObject/EventChannel/Currency/RequestCurrencySnapshotEventChannel.asset; Assets/ScriptableObject/EventChannel/Gameplay/OnInteractionEnded.asset; Assets/ScriptableObject/EventChannel/Health/DeathEvent.asset; Assets/ScriptableObject/EventChannel/Health/UpdateHealthUI.asset; Assets/ScriptableObject/EventChannel/MainMenu/ContinueGameEvent.asset; Assets/ScriptableObject/EventChannel/MainMenu/StartNewGame.asset; Assets/ScriptableObject/EventChannel/METAUpgrade/METAProgressRefreshRequestedEventChannel.asset
- 타입 이름을 참조하는 소스 28개: Assets/Scripts/Authoring/MonoBehaviours/EditorColdStartup.cs; Assets/Scripts/Core/Events/VoidEventListener.cs; Assets/Scripts/Core/Persistence/SaveSystem.cs; Assets/Scripts/Core/SceneFlow/SceneLoader.cs; Assets/Scripts/Core/SceneFlow/StartGame.cs; Assets/Scripts/Gameplay/Combat/Infection/InfectionControlManager.cs; Assets/Scripts/Gameplay/Entities/Common/Core/Damageable.cs; Assets/Scripts/Gameplay/Entities/Common/StateMachine/Actions/RaiseVoidEventActionSO.cs

## Assets/Scripts/Core/Events/ScriptableObject/WeaponAmmoEventChannelSO.cs
- 분류: 일반 소스; 49행
- 타입: WeaponAmmoSnapshot, WeaponAmmoEventChannelSO : ScriptableObject
- 메서드: RaiseEvent
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 0개: Assets/ScriptableObject/EventChannel/UI/WeaponAmmoSection/WeaponAmmoEventChannel.asset
- 타입 이름을 참조하는 소스 0개: 

## Assets/Scripts/Core/Events/ScriptableObject/WeaponAmmoLoadoutEventChannelSO.cs
- 분류: 일반 소스; 95행
- 타입: WeaponAmmoSlotSnapshot, WeaponAmmoLoadoutSnapshot, WeaponAmmoLoadoutEventChannelSO : ScriptableObject
- 메서드: RaiseEvent
- 직렬화 참조 2개 / 지정 씬 참조 그래프 내 1개: Assets/ScriptableObject/EventChannel/UI/WeaponAmmoSection/WeaponAmmoLoadoutEventChannel.asset; Assets/ScriptableObject/EventChannel/UI/WeaponAmmoSection/WeaponAmmoSlotLoadoutEventChannel.asset
- 타입 이름을 참조하는 소스 11개: Assets/Scripts/Gameplay/Entities/Shooter/Player/Inventory/PlayerInventoryRuntime.cs; Assets/Scripts/Gameplay/Entities/Shooter/Player/Inventory/PlayerQuickSlotRuntime.cs; Assets/Scripts/Gameplay/Entities/Shooter/Player/PlayerBulletLoadoutRuntime.cs; Assets/Scripts/Presentation/UI/Gameplay/PanelHub/Inventory/EnemyInventoryUI.cs; Assets/Scripts/Presentation/UI/Gameplay/PanelHub/Inventory/EquipmentInventoryAmmoSlotUI.cs; Assets/Scripts/Presentation/UI/Gameplay/PanelHub/Inventory/PlayerInventoryUI.cs; Assets/Scripts/Presentation/UI/Gameplay/Shop/WeaponAmmoSellTypes.cs; Assets/Scripts/Presentation/UI/Gameplay/Slot/AmmoSlotSellPopupUI.cs

## Assets/Scripts/Core/Events/ScriptableObject/WeaponAmmoSlotSwapRequestEventChannelSO.cs
- 분류: 일반 소스; 43행
- 타입: WeaponAmmoSlotSwapRequest, WeaponAmmoSlotSwapRequestEventChannelSO : ScriptableObject
- 메서드: RaiseEvent
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 1개: Assets/ScriptableObject/EventChannel/UI/WeaponAmmoSection/WeaponAmmoSlotSwapRequestEventChannel.asset
- 타입 이름을 참조하는 소스 1개: Assets/Scripts/Gameplay/Entities/Shooter/Player/PlayerBulletLoadoutRuntime.cs

## Assets/Scripts/Core/Events/ScriptableObject/WorldCameraEventChannelSO.cs
- 분류: 일반 소스; 38행
- 타입: WorldCameraEventChannelSO : ScriptableObject
- 메서드: RaiseEvent, Clear
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 1개: Assets/ScriptableObject/EventChannel/Camera/WorldCameraReadyEventChannel.asset
- 타입 이름을 참조하는 소스 8개: Assets/Scripts/Gameplay/Entities/Shooter/Player/VSplatterRange.cs; Assets/Scripts/Presentation/Audio/CameraAlignedAudioListener.cs; Assets/Scripts/Presentation/Camera/CameraManager.cs; Assets/Scripts/Presentation/UI/Gameplay/DamageNumber/DamageNumberUIManager.cs; Assets/Scripts/Presentation/UI/Gameplay/EnemyBarUI/EnemyScreenSpaceHPUIManager.cs; Assets/Scripts/Presentation/UI/Gameplay/Interaction/InteractionBarUIManager.cs; Assets/Scripts/Presentation/UI/Gameplay/Interaction/InteractionPromptScreenSpaceUIManager.cs; Assets/Scripts/Presentation/UI/Gameplay/Vision/PlayerVisionFogOverlayUI.cs

## Assets/Scripts/Core/Events/VoidEventListener.cs
- 분류: 일반 소스; 40행
- 타입: VoidEventListener : MonoBehaviour
- 메서드: OnEnable, OnDisable, Respond
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 0개: 

## Assets/Scripts/Core/Foundation/DescriptionBaseSO.cs
- 분류: 일반 소스; 16행
- 타입: for, DescriptionBaseSO : SerializableScriptableObject
- 메서드: 
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 524개: Assets/Editor/InventorySortButtonMigration.cs; Assets/Editor/ItemCategoryMoveUtility.cs; Assets/Editor/ItemMakerSelection.cs; Assets/Editor/ItemMakerWindow.cs; Assets/Editor/LootProgressionEditorWindow.cs; Assets/Editor/QuestDesignTreeWindow.cs; Assets/Editor/SaveInspectorWindow.cs; Assets/Editor/ShooterCustomizeStationWindow.cs

## Assets/Scripts/Core/Input/InputReader.cs
- 분류: 일반 소스; 530행
- 타입: InputReader : ScriptableObject, GameInput.IGameplayActions, GameInput.IUIActions
- 메서드: OnEnable, EnsureInputActionsCreated, EnsureAuxiliaryGameplayActionsCreated, FindOrAddAuxiliaryButton, GetBindingDisplayString, FindRuntimeAction, LoadBindingOverrides, SaveBindingOverrides, OnDisable, EnableDialogueInput, EnableGameplayInput, EnableMenuInput, DisableAllInput, EnablePlayerMenuInput, ReleaseGameplayInputState, LeftMouseDown, OnMove, OnShoot, OnMeleeAttack, OnMeeleAttack, OnInteract, OnDash, OnReload, OnSlot1, OnSlot2, OnSlot3, OnSlot4, OnSlot5, HandleCycleAmmoPerformed, HandleDropBackpackPerformed, OnMap, OnInventory, OnPlayerMenu, OnNavigate, OnSubmit, OnCancel, OnPoint, OnClick, OnRightClick, OnMiddleClick, OnScrollWheel
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 1개: Assets/ScriptableObject/Input/InputReader.asset
- 타입 이름을 참조하는 소스 27개: Assets/Scripts/Core/SceneFlow/GameplayScreenTransitionRunner.cs; Assets/Scripts/Core/SceneFlow/SceneLoader.cs; Assets/Scripts/Gameplay/Entities/Creature/NamedEnemy/Mutarus/MutarusQTEPatternController.cs; Assets/Scripts/Gameplay/Entities/Shooter/Player/Inventory/PlayerBackpackDropInput.cs; Assets/Scripts/Gameplay/Entities/Shooter/Player/Inventory/PlayerConsumableUseRuntime.cs; Assets/Scripts/Gameplay/Entities/Shooter/Player/PlayerAimAction.cs; Assets/Scripts/Gameplay/Entities/Shooter/Player/PlayerAimController.cs; Assets/Scripts/Gameplay/Entities/Shooter/Player/PlayerCharacter.cs

## Assets/Scripts/Core/Persistence/FileManager.cs
- 분류: 일반 소스; 83행
- 타입: FileManager
- 메서드: WriteToFile, LoadFromFile, MoveFile
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 1개: Assets/Scripts/Core/Persistence/SaveSystem.cs

## Assets/Scripts/Core/Persistence/PlayerRunStatePersistence.cs
- 분류: 일반 소스; 390행
- 타입: PlayerRunStatePersistence : MonoBehaviour
- 메서드: Initialize, CaptureCurrentPlayerState, ClearRunState, TryRestorePendingRunState, ResolveRunData, ResolveCatalog, TryGetRequiredPlayerComponents, PruneUnavailableReferences, AddRemovedKind
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 4개: Assets/Scripts/Core/Persistence/RunLifecycleCoordinator.cs; Assets/Scripts/Gameplay/Entities/Common/StatusEffects/EntityStatusEffectRuntime.cs; Assets/Scripts/Gameplay/Entities/Shooter/Player/Inventory/PlayerInventoryRuntime.RunPersistence.cs; Assets/Scripts/Gameplay/Entities/Shooter/Player/Passive/PlayerPassiveInventoryRuntime.cs

## Assets/Scripts/Core/Persistence/RaidRunSaveData.cs
- 분류: 일반 소스; 50행
- 타입: RaidRunSaveData, RaidLootContainerSaveData
- 메서드: TryValidate
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 9개: Assets/Scripts/Core/Persistence/PlayerRunStatePersistence.cs; Assets/Scripts/Core/Persistence/Save.cs; Assets/Scripts/Gameplay/Items/Loot/LootableObject.cs; Assets/Scripts/Gameplay/Progression/Stage/StageProgressionManager.cs; Assets/Scripts/Gameplay/World/Sectors/StageMap/RaidRuntime.cs; Assets/Scripts/Gameplay/World/Sectors/StageMap/StageSectorInstantiator.cs; Assets/Scripts/Presentation/UI/Gameplay/PlayerTimer/PlayerTimerHUD.cs; Assets/Tests/EditMode/Editor/RaidMapFoundationTests.cs

## Assets/Scripts/Core/Persistence/RunLifecycleCoordinator.cs
- 분류: 일반 소스; 307행
- 타입: RunLifecyclePhase, RunLifecycleCoordinator : MonoBehaviour
- 메서드: Awake, Update, OnDestroy, OnApplicationPause, OnApplicationQuit, NotifyHomeEntered, NotifyRunStarted, NotifyRunCompleted, TryHandlePlayerRunLoss, CaptureRunStateForExternalSave, HandleHomeEntered, HandleRunStarted, HandleRunCompleted, HandlePlayerRunLoss, EnsurePersistentProfileLoaded, SavePersistentProfile, ResolveSaveSystem, EnsureSceneDependencyValidator, EnsurePlayerRunStatePersistence, TransitionTo
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 1개: Assets/Scenes/Manager/PersistenceManager.unity
- 타입 이름을 참조하는 소스 8개: Assets/Scripts/Core/Persistence/PlayerRunStatePersistence.cs; Assets/Scripts/Core/Persistence/RunLifecyclePolicy.cs; Assets/Scripts/Core/Persistence/Save.cs; Assets/Scripts/Core/Persistence/SaveSystem.cs; Assets/Scripts/Core/SceneFlow/RuntimeSceneDependencyValidator.cs; Assets/Scripts/Gameplay/Entities/Shooter/Player/PlayerShooterDeathCorpseController.cs; Assets/Scripts/Gameplay/Progression/Stage/StageProgressionManager.cs; Assets/Tests/EditMode/Editor/RunLifecycleAndSaveTests.cs

## Assets/Scripts/Core/Persistence/RunLifecyclePolicy.cs
- 분류: 일반 소스; 43행
- 타입: RunLifecyclePolicy
- 메서드: ResolveApplicationPausePhase, NormalizeActiveStageIndex, CanProcessRunLoss
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 2개: Assets/Scripts/Core/Persistence/RunLifecycleCoordinator.cs; Assets/Tests/EditMode/Editor/RunLifecycleAndSaveTests.cs

## Assets/Scripts/Core/Persistence/RuntimeContentCatalogSO.cs
- 분류: 일반 소스; 91행
- 타입: RuntimeContentCatalogSO : ScriptableObject
- 메서드: TryResolveItem, TryResolveStatusEffect, OnEnable, EnsureLookup
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 0개: Assets/Resources/RuntimeContentCatalog.asset
- 타입 이름을 참조하는 소스 11개: Assets/Scripts/Core/Persistence/PlayerRunStatePersistence.cs; Assets/Scripts/Core/Persistence/RunLifecycleCoordinator.cs; Assets/Scripts/Gameplay/Entities/Common/StatusEffects/EntityStatusEffectRuntime.cs; Assets/Scripts/Gameplay/Entities/Shooter/Player/Inventory/PlayerInventoryRuntime.RunPersistence.cs; Assets/Scripts/Gameplay/Entities/Shooter/Player/Passive/PlayerPassiveInventoryRuntime.cs; Assets/Scripts/Gameplay/Entities/Shooter/Player/PlayerBulletLoadoutRuntime.cs; Assets/Scripts/Gameplay/Items/Loot/LootableObject.cs; Assets/Scripts/Gameplay/World/Sectors/Shop/SectorShop.cs

## Assets/Scripts/Core/Persistence/Save.cs
- 분류: 일반 소스; 1054행
- 타입: Save, ShopUnlockProgressSaveData, QuestProgressSaveData, PlayerRunStateSaveData, PlayerRunShopStateSaveData, PlayerRunShopOfferSaveData, PlayerRunShopPaymentRequirementSaveData, PlayerRunInventorySlotSaveData, PlayerRunWeaponSaveData, PlayerRunKeyToolSlotSaveData, PlayerRunMagazineSaveData, PlayerRunStatusEffectSaveData, QuestStatRewardSaveData, QuestWarehouseSlotRewardSaveData, InheritanceWarehouseSaveData, InheritanceWarehouseItemSaveData, MetaUpgradeSaveData, DeviceSettingsSaveData, RunSaveData, LegacySaveData, SaveMigration
- 메서드: ToJson, LoadFromJson, EnsureRuntimeDefaults, EnsureDefaults, Clear, NormalizeDurability, RemoveInvalidInventorySlots, RemoveInvalidShopStates, RemoveInvalidWeaponSlots, RemoveInvalidKeySlots, RemoveInvalidMagazineSlots, RemoveInvalidPassiveSlots, RemoveInvalidStatusEffects, RemoveInvalidOffers, Normalize, GetLevel, SetLevel, IncreaseLevel, ClampNonNegative, ToProfileData, ToRunData, ToDeviceSettingsData, TryMigrateProfile, TryMigrateRun, TryMigrateSettings, TryGetSupportedVersion
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 37개: Assets/Editor/ItemCategoryMoveUtility.cs; Assets/Editor/LootProgressionEditorWindow.cs; Assets/Editor/QuestDesignTreeWindow.cs; Assets/Editor/SaveInspectorWindow.cs; Assets/Editor/ShooterCustomizeStationWindow.cs; Assets/Scripts/Authoring/Editor/BoardChaseAuthoringHubWindow.cs; Assets/Scripts/Authoring/Editor/ContentTemplateInspectors.cs; Assets/Scripts/Authoring/Editor/DebugMonitorWindow.Operations.cs

## Assets/Scripts/Core/Persistence/SaveSystem.cs
- 분류: 일반 소스; 676행
- 타입: SaveSystem : ScriptableObject, DeviceAudioChannel
- 메서드: OnEnable, OnDisable, LoadSaveDataFromDisk, SaveDataToDisk, TrySaveRunDataToDisk, RequestSettingsSave, MarkRuntimeSettingsInitialized, ScheduleSettingsWrite, FlushPendingWrites, SetInputBindingOverrides, GetInputBindingOverrides, SetDeviceAudioVolume, WriteEmptySaveFile, SetNewGameData, GetHighestUnlockedStageIndex, SetHighestUnlockedStageIndex, GetBackupFilename, GetAbsoluteSavePath, WriteAllPartitions, CaptureCurrentSettings, TryLoadProfile, TryLoadRun, TryLoadSettings, TryLoadLegacyMonolith, ImportLegacyPlayerPrefs, WriteProfilePartition, WriteRunPartition, WriteSettingsPartition, WriteJsonAtomically, RotateBackups, TryReadJson, EnumeratePartitionCandidates, DeletePartitionFiles, DeleteFileIfPresent, LogBackupRecoveryIfNeeded
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 1개: Assets/ScriptableObject/SaveSystem/SaveSystem.asset
- 타입 이름을 참조하는 소스 25개: Assets/Editor/SaveInspectorWindow.cs; Assets/Scripts/Authoring/MonoBehaviours/EditorColdStartup.cs; Assets/Scripts/Core/Input/InputReader.cs; Assets/Scripts/Core/Persistence/PlayerRunStatePersistence.cs; Assets/Scripts/Core/Persistence/RunLifecycleCoordinator.cs; Assets/Scripts/Core/SceneFlow/RuntimeSceneDependencyValidator.cs; Assets/Scripts/Core/SceneFlow/StartGame.cs; Assets/Scripts/Gameplay/Entities/Shooter/Player/PlayerStatsRuntime.cs

## Assets/Scripts/Core/Persistence/SerializableScriptableObject.cs
- 분류: 일반 소스; 26행
- 타입: SerializableScriptableObject : ScriptableObject
- 메서드: 
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 2개: Assets/Scripts/Core/Foundation/DescriptionBaseSO.cs; Assets/Scripts/Gameplay/Items/Definitions/ItemSO.cs

## Assets/Scripts/Core/SceneFlow/CanvasVisionRuntimeActivator.cs
- 분류: 일반 소스; 68행
- 타입: CanvasVisionRuntimeActivator : MonoBehaviour
- 메서드: Awake, OnDestroy, HandleSceneLoaded, RestoreCanvasVisionInLoadedScenes
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 1개: Assets/Scenes/Manager/PersistenceManager.unity
- 타입 이름을 참조하는 소스 0개: 

## Assets/Scripts/Core/SceneFlow/GameplayScreenTransitionRunner.cs
- 분류: 일반 소스; 110행
- 타입: GameplayScreenTransitionRunner : MonoBehaviour
- 메서드: PlayTransition
- 직렬화 참조 2개 / 지정 씬 참조 그래프 내 1개: Assets/Prefabs/GameScene/Sector/Stage1/Sector_Named.prefab; Assets/Scenes/RUNScene.unity
- 타입 이름을 참조하는 소스 2개: Assets/Scripts/Gameplay/World/Sectors/Named_Boss/NamedSectorTransitionController.cs; Assets/Scripts/Gameplay/World/Sectors/Runtime/SectorPortalManager.cs

## Assets/Scripts/Core/SceneFlow/InitializationLoader.cs
- 분류: 일반 소스; 54행
- 타입: is, InitializationLoader : MonoBehaviour
- 메서드: Start, LoadEventChannel, LoadMainMenu
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 1개: Assets/Scenes/Manager/Initialization.unity
- 타입 이름을 참조하는 소스 379개: Assets/Editor/InventorySortButtonMigration.cs; Assets/Editor/ItemCategoryMoveUtility.cs; Assets/Editor/ItemMakerWindow.cs; Assets/Editor/LootProgressionEditorWindow.cs; Assets/Editor/QuestDesignTreeWindow.cs; Assets/Editor/SaveInspectorWindow.cs; Assets/Editor/ShooterCustomizeStationWindow.cs; Assets/Editor/ShooterMeleeAnimationSetup.cs

## Assets/Scripts/Core/SceneFlow/RunResult.cs
- 분류: 일반 소스; 28행
- 타입: RunResult
- 메서드: SetStage, Clear
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 3개: Assets/Scripts/Core/SceneFlow/SceneLoader.cs; Assets/Scripts/Gameplay/Progression/Stage/StageProgressionManager.cs; Assets/Scripts/Presentation/UI/Gameplay/GameOver/GameOverUI.cs

## Assets/Scripts/Core/SceneFlow/RuntimeSceneDependencyValidator.cs
- 분류: 일반 소스; 312행
- 타입: RuntimeDependencySeverity, RuntimeDependencyIssue, RuntimeSceneDependencyValidator : MonoBehaviour
- 메서드: OnEnable, OnDisable, ValidateNow, HandleSceneLoaded, HandleSceneUnloaded, QueueValidation, ValidateAfterSceneSettle, ValidatePersistentDependencies, ValidateHomeDependencies, ValidateRunDependencies, AddIssue, PublishReport, IsSceneLoaded
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 1개: Assets/Scripts/Core/Persistence/RunLifecycleCoordinator.cs

## Assets/Scripts/Core/SceneFlow/SceneLoader.cs
- 분류: 일반 소스; 1110행
- 타입: SceneLoader : MonoBehaviour
- 메서드: Awake, OnDestroy, OnEnable, OnDisable, CurrentSceneColdStartup, OnColdStartupGameplayManagersLoaded, LoadGameScene, ShouldLoadGameplayManager, OnGameplayManagersLoaded, LoadMenu, HandleGameOverRequested, LoadGameOverSceneRoutine, UnloadPreviousScene, LoadNewScene, LoadRequestedScene, ShouldMountHomeEnvironment, EnsureHomeEnvironmentForRun, EnsureHomeEnvironmentLoaded, OnHomeEnvironmentLoaded, IsHomeEnvironmentLoaded, UnloadHomeEnvironment, OnNewSceneLoaded, BeginFinishLoadingRoutine, FinishSceneLoadRoutine, RequestStageTransition, StageTransitionRoutine, StartGameplay, SnapCameraToPlayer, WaitForRuntimeReadiness, WaitForPlayerReadiness, HasGeneratedMapReady, IsGameplayRuntimeReady, HasActivePlayer, ReportRuntimeReadinessTimeout, HandleRuntimeReadinessFailure, InitializeLoadingScreen, CanPresentStartConfirmation, ResolveLoadingMessage, SetLoadingMessage, WaitForStartConfirmation, IsStartConfirmationHeld, WasStartConfirmationPressedThisFrame, AcquireLoadingPause, ReleaseLoadingPause, SafeFormatStageReadyText, SetLoadingScreenVisible, ExitGame
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 1개: Assets/Scenes/Manager/PersistenceManager.unity
- 타입 이름을 참조하는 소스 5개: Assets/Scripts/Authoring/MonoBehaviours/EditorColdStartup.cs; Assets/Scripts/Core/Events/ScriptableObject/LoadEventChannelSO.cs; Assets/Scripts/Core/SceneFlow/RuntimeSceneDependencyValidator.cs; Assets/Scripts/Gameplay/Entities/Shooter/Player/PlayerShooterDeathCorpseController.cs; Assets/Scripts/Gameplay/Progression/Stage/StageProgressionManager.cs

## Assets/Scripts/Core/SceneFlow/SceneTransitioner.cs
- 분류: 일반 소스; 140행
- 타입: SceneTransitioner : MonoBehaviour
- 메서드: Awake, Initialize, LoadScene, TransitionRoutine
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 0개: Assets/Prefabs/SceneTransition/SceneTransitioner.prefab
- 타입 이름을 참조하는 소스 0개: 

## Assets/Scripts/Core/SceneFlow/ScriptableObject/GameplaySO.cs
- 분류: 일반 소스; 16행
- 타입: contains, GameplaySO : GameSceneSO
- 메서드: 
- 직렬화 참조 2개 / 지정 씬 참조 그래프 내 2개: Assets/ScriptableObject/SceneData/GamePlay.asset; Assets/ScriptableObject/SceneData/HomeScene.asset
- 타입 이름을 참조하는 소스 31개: Assets/Editor/LootProgressionEditorWindow.cs; Assets/Editor/QuestDesignTreeWindow.cs; Assets/Editor/ShooterCustomizeStationWindow.cs; Assets/Editor/ShopProgressionTreeWindow.cs; Assets/Scripts/Authoring/Editor/ContentPreflightWindow.cs; Assets/Scripts/Authoring/Editor/DebugMonitorWindow.cs; Assets/Scripts/Authoring/Editor/SectorMakerWindow.cs; Assets/Scripts/Core/SceneFlow/SceneLoader.cs

## Assets/Scripts/Core/SceneFlow/ScriptableObject/GameSceneSO.cs
- 분류: 일반 소스; 37행
- 타입: is, which, GameSceneSO : DescriptionBaseSO, GameSceneType
- 메서드: 
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 389개: Assets/Editor/InventorySortButtonMigration.cs; Assets/Editor/ItemCategoryMoveUtility.cs; Assets/Editor/ItemMakerWindow.cs; Assets/Editor/LootProgressionEditorWindow.cs; Assets/Editor/QuestDesignTreeWindow.cs; Assets/Editor/SaveInspectorWindow.cs; Assets/Editor/ShooterCustomizeStationWindow.cs; Assets/Editor/ShooterMeleeAnimationSetup.cs

## Assets/Scripts/Core/SceneFlow/ScriptableObject/LocationSO.cs
- 분류: 일반 소스; 19행
- 타입: contains, LocationSO : GameSceneSO
- 메서드: 
- 직렬화 참조 2개 / 지정 씬 참조 그래프 내 2개: Assets/ScriptableObject/SceneData/GameOverScene.asset; Assets/ScriptableObject/SceneData/SplatterScene.asset
- 타입 이름을 참조하는 소스 31개: Assets/Editor/LootProgressionEditorWindow.cs; Assets/Editor/QuestDesignTreeWindow.cs; Assets/Editor/ShooterCustomizeStationWindow.cs; Assets/Editor/ShopProgressionTreeWindow.cs; Assets/Scripts/Authoring/Editor/ContentPreflightWindow.cs; Assets/Scripts/Authoring/Editor/DebugMonitorWindow.cs; Assets/Scripts/Authoring/Editor/SectorMakerWindow.cs; Assets/Scripts/Core/SceneFlow/SceneLoader.cs

## Assets/Scripts/Core/SceneFlow/ScriptableObject/MenuSO.cs
- 분류: 일반 소스; 14행
- 타입: contains, MenuSO : GameSceneSO
- 메서드: 
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 1개: Assets/ScriptableObject/SceneData/Menus/MainMenu.asset
- 타입 이름을 참조하는 소스 31개: Assets/Editor/LootProgressionEditorWindow.cs; Assets/Editor/QuestDesignTreeWindow.cs; Assets/Editor/ShooterCustomizeStationWindow.cs; Assets/Editor/ShopProgressionTreeWindow.cs; Assets/Scripts/Authoring/Editor/ContentPreflightWindow.cs; Assets/Scripts/Authoring/Editor/DebugMonitorWindow.cs; Assets/Scripts/Authoring/Editor/SectorMakerWindow.cs; Assets/Scripts/Core/SceneFlow/SceneLoader.cs

## Assets/Scripts/Core/SceneFlow/ScriptableObject/PathSO.cs
- 분류: 일반 소스; 18행
- 타입: PathSO : ScriptableObject
- 메서드: 
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 0개: 

## Assets/Scripts/Core/SceneFlow/ScriptableObject/PersistentManagerSO.cs
- 분류: 일반 소스; 12행
- 타입: PersistentManagerSO : GameSceneSO
- 메서드: 
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 1개: Assets/ScriptableObject/SceneData/Managers/PersistentManager.asset
- 타입 이름을 참조하는 소스 1개: Assets/Scripts/WIP/Legacy/Authoring/Editor/SceneSelector/SceneSelector.Helper.cs

## Assets/Scripts/Core/SceneFlow/StartGame.cs
- 분류: 일반 소스; 134행
- 타입: StartGame : MonoBehaviour
- 메서드: Start, OnDestroy, RefreshSaveState, StartNewGame, ContinuePreviousGame, LoadSaveGame, HasSaveData, ResetSaveDataFlag
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 1개: Assets/Scenes/MainMenu.unity
- 타입 이름을 참조하는 소스 1개: Assets/Scripts/Gameplay/GameManager.cs

## Assets/Scripts/Core/SceneFlow/TransitionEffect.cs
- 분류: 일반 소스; 43행
- 타입: TransitionEffect : ScriptableObject
- 메서드: AnimateOut, AnimateIn, SetEffectProperties, AnimateCutoff
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 7개: Assets/Scripts/Core/Events/ScriptableObject/ScreenTransitionRequestEventChannelSO.cs; Assets/Scripts/Core/SceneFlow/GameplayScreenTransitionRunner.cs; Assets/Scripts/Core/SceneFlow/SceneTransitioner.cs; Assets/Scripts/Core/SceneFlow/TransitionInterfaceController.cs; Assets/Scripts/Core/SceneFlow/WipeEffect.cs; Assets/Scripts/Gameplay/World/Sectors/Named_Boss/NamedSectorTransitionController.cs; Assets/Scripts/Gameplay/World/Sectors/Runtime/SectorPortalManager.cs

## Assets/Scripts/Core/SceneFlow/TransitionInterfaceController.cs
- 분류: 일반 소스; 184행
- 타입: TransitionInterfaceController : MonoBehaviour
- 메서드: Awake, OnEnable, OnDisable, HandleTransitionRequest, RunTransition, EnsureTransitionImage, CreateTransitionCanvas, StretchToFullScreen, SetupMaterial, HideTransitionImage, CleanupMaterial
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 1개: Assets/Scenes/Manager/PersistenceManager.unity
- 타입 이름을 참조하는 소스 2개: Assets/Scripts/Core/SceneFlow/GameplayScreenTransitionRunner.cs; Assets/Scripts/Gameplay/World/Sectors/Runtime/SectorPortalManager.cs

## Assets/Scripts/Core/SceneFlow/UIMaterialPropertyUpdater.cs
- 분류: 일반 소스; 95행
- 타입: UIMaterialPropertyUpdater : MonoBehaviour
- 메서드: OnEnable, OnRectTransformDimensionsChange, Update, RefreshMaterial, UpdateMaterialProperties, OnDisable
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 1개: Assets/Prefabs/SceneTransition/TransitionImage.prefab
- 타입 이름을 참조하는 소스 0개: 

## Assets/Scripts/Core/SceneFlow/WipeEffect.cs
- 분류: 일반 소스; 77행
- 타입: WipeEffect : TransitionEffect, WipeDirection
- 메서드: SetEffectProperties, AnimateOut, AnimateIn, GetAngleForDirection
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 1개: Assets/ScriptableObject/SceneManagement/WipeEffect_Scene.asset
- 타입 이름을 참조하는 소스 0개: 

## Assets/Scripts/Core/Utilities/TimeUtils.cs
- 분류: 일반 소스; 50행
- 타입: TimeUtils
- 메서드: ParseClockToSeconds, SecondsToMs, MsToSeconds, FormatMsClock
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 0개: 