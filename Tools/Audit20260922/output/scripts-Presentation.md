# Presentation 스크립트 전수 정적 인덱스
검사일: 2026-09-22. 함수/타입/직렬화 연결은 정적 추출이며 실행 검증을 뜻하지 않습니다. 동적 생성, 리플렉션 및 Addressables 간접 참조는 별도 시스템 문서와 함께 확인하세요.

## Assets/Scripts/Presentation/Audio/AudioCatalog.cs
- 분류: 일반 소스; 184행
- 타입: AudioChannel, AudioCatalog : ScriptableObject, Entry
- 메서드: Build, TryGet
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 0개: Assets/Audio/Audio/Catalog/SoundCatalog.asset
- 타입 이름을 참조하는 소스 12개: Assets/Editor/LootProgressionEditorWindow.cs; Assets/Scripts/Authoring/Editor/SectorMakerWindow.cs; Assets/Scripts/Gameplay/Combat/PaintMark/MaskRenderTableSO.cs; Assets/Scripts/Gameplay/Progression/Stage/StageProgressionManager.cs; Assets/Scripts/Gameplay/World/Sectors/ScriptableObject/SectorExclusionRulesSO.cs; Assets/Scripts/Gameplay/World/Sectors/StageMap/StageTopologySO.cs; Assets/Scripts/Gameplay/World/Sectors/Treasure/TreasureRoomDropTableSO.cs; Assets/Scripts/Presentation/Audio/AudioMaster.cs

## Assets/Scripts/Presentation/Audio/AudioCue.cs
- 분류: 일반 소스; 70행
- 타입: AudioCue : MonoBehaviour
- 메서드: Start, OnDisable, PlayDelayed, PlayAudioCue, StopAudioCue, FinishAudioCue
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 6개: Assets/Scripts/Authoring/Editor/AudioCueLibraryWindow.cs; Assets/Scripts/Core/Events/ScriptableObject/AudioCueEventChannelSO.cs; Assets/Scripts/Gameplay/Entities/Common/StateMachine/Actions/PlayAudioCueActionSO.cs; Assets/Scripts/Presentation/Audio/AudioData/AudioCueSO.cs; Assets/Scripts/Presentation/Audio/AudioData/NpcAudioProfileSO.cs; Assets/Scripts/Presentation/Audio/AudioLoopEmitter.cs

## Assets/Scripts/Presentation/Audio/AudioData/AudioConfigurationSO.cs
- 분류: 일반 소스; 161행
- 타입: AudioConfigurationSO : DescriptionBaseSO
- 메서드: ApplyTo, ApplyTopDownDirectionAssist, ResolveDistanceRolloffCurve
- 직렬화 참조 10개 / 지정 씬 참조 그래프 내 10개: Assets/ScriptableObject/Audio/AudioConfiguration/3DSFX_Config.asset; Assets/ScriptableObject/Audio/AudioConfiguration/EnemyAlertTemporary_Config.asset; Assets/ScriptableObject/Audio/AudioConfiguration/EnemyDiscoveryTemporary_Config.asset; Assets/ScriptableObject/Audio/AudioConfiguration/EnemyFootstep3D_Config.asset; Assets/ScriptableObject/Audio/AudioConfiguration/EnemyGunshot3D_Config.asset; Assets/ScriptableObject/Audio/AudioConfiguration/Music_Config.asset; Assets/ScriptableObject/Audio/AudioConfiguration/NPCVoice_Config.asset; Assets/ScriptableObject/Audio/AudioConfiguration/PlayerSFX_Config.asset
- 타입 이름을 참조하는 소스 23개: Assets/Scripts/Authoring/Editor/AudioCueLibraryWindow.cs; Assets/Scripts/Core/Events/ScriptableObject/AudioCueEventChannelSO.cs; Assets/Scripts/Gameplay/Entities/Common/Core/CharacterAudio.cs; Assets/Scripts/Gameplay/Entities/Common/StateMachine/Actions/PlayAudioCueActionSO.cs; Assets/Scripts/Gameplay/Entities/Shooter/Common/Core/EntityShooterCharacterAudio.cs; Assets/Scripts/Gameplay/Entities/Shooter/Enemy/Common/EnemyShooterCharacterAudio.cs; Assets/Scripts/Gameplay/Entities/Shooter/Player/PlayerConditionAudio.cs; Assets/Scripts/Gameplay/World/Sectors/StageMap/RaidRuntimeSettings.cs

## Assets/Scripts/Presentation/Audio/AudioData/AudioCueKey.cs
- 분류: 일반 소스; 32행
- 타입: AudioCueKey : IEquatable<AudioCueKey>
- 메서드: Create, Equals, GetHashCode, ToString
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 9개: Assets/Scripts/Core/Events/ScriptableObject/AudioCueEventChannelSO.cs; Assets/Scripts/Gameplay/Progression/Quests/HomeQuestNpc.cs; Assets/Scripts/Presentation/Audio/AudioCue.cs; Assets/Scripts/Presentation/Audio/AudioLoopEmitter.cs; Assets/Scripts/Presentation/Audio/AudioManager.cs; Assets/Scripts/Presentation/Audio/BulletImpactAudio.cs; Assets/Scripts/Presentation/Audio/InteractionSearchAudio.cs; Assets/Tests/EditMode/Editor/AudioGroundworkTests.cs

## Assets/Scripts/Presentation/Audio/AudioData/AudioCueLibrarySO.cs
- 분류: 일반 소스; 81행
- 타입: AudioCueLibrarySO : DescriptionBaseSO
- 메서드: Contains, TryFindById, OnValidate
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 0개: Assets/ScriptableObject/Audio/AudioCueLibrary.asset
- 타입 이름을 참조하는 소스 2개: Assets/Scripts/Authoring/Editor/AudioCueLibraryWindow.cs; Assets/Scripts/Authoring/Editor/ContentPreflightWindow.cs

## Assets/Scripts/Presentation/Audio/AudioData/AudioCueSO.cs
- 분류: 일반 소스; 166행
- 타입: AudioCueSO : DescriptionBaseSO, AudioCueCategory, AudioClipsGroup, SequenceMode
- 메서드: OnValidate, GetClips, GetNextClip
- 직렬화 참조 73개 / 지정 씬 참조 그래프 내 56개: Assets/ScriptableObject/Audio/AudioCue/Music/BGM_Lab.asset; Assets/ScriptableObject/Audio/AudioCue/Music/BGM_PauseMenu.asset; Assets/ScriptableObject/Audio/AudioCue/SFX/Ambience/Ambience_BiochemicalGasIntensify_Temp.asset; Assets/ScriptableObject/Audio/AudioCue/SFX/Ambience/Ambience_BiochemicalGasLoop_Temp.asset; Assets/ScriptableObject/Audio/AudioCue/SFX/Character/CharacterDeathAudio.asset; Assets/ScriptableObject/Audio/AudioCue/SFX/Character/Dash.asset; Assets/ScriptableObject/Audio/AudioCue/SFX/Character/EnemyAlert_Temporary.asset; Assets/ScriptableObject/Audio/AudioCue/SFX/Character/EnemyBulletHit.asset
- 타입 이름을 참조하는 소스 43개: Assets/Scripts/Authoring/Editor/AudioCueLibraryWindow.cs; Assets/Scripts/Authoring/Editor/BoardChaseAuthoringHubWindow.cs; Assets/Scripts/Authoring/Editor/ContentPreflightWindow.cs; Assets/Scripts/Authoring/Editor/ContentTemplateInspectors.cs; Assets/Scripts/Core/Events/ScriptableObject/AudioCueEventChannelSO.cs; Assets/Scripts/Core/SceneFlow/ScriptableObject/GameSceneSO.cs; Assets/Scripts/Gameplay/Entities/Common/Core/CharacterAudio.cs; Assets/Scripts/Gameplay/Entities/Common/StateMachine/Actions/PlayAudioCueActionSO.cs

## Assets/Scripts/Presentation/Audio/AudioData/BulletImpactAudioCatalogSO.cs
- 분류: 일반 소스; 59행
- 타입: BulletImpactAudioCatalogSO : DescriptionBaseSO, SurfaceCue
- 메서드: ResolveCue
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 0개: Assets/Resources/BulletImpactAudioCatalog.asset
- 타입 이름을 참조하는 소스 2개: Assets/Scripts/Presentation/Audio/BulletImpactAudio.cs; Assets/Tests/EditMode/Editor/ObstacleBulletImpactAudioTests.cs

## Assets/Scripts/Presentation/Audio/AudioData/InteractableAudioProfileSO.cs
- 분류: 일반 소스; 34행
- 타입: InteractableAudioProfileSO : DescriptionBaseSO
- 메서드: 
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 1개: Assets/Scripts/Presentation/Audio/ObstacleAudio.cs

## Assets/Scripts/Presentation/Audio/AudioData/NpcAudioProfileSO.cs
- 분류: 일반 소스; 15행
- 타입: NpcAudioProfileSO : DescriptionBaseSO
- 메서드: 
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 1개: Assets/ScriptableObject/Audio/NpcAudioProfile_Default.asset
- 타입 이름을 참조하는 소스 2개: Assets/Scripts/Gameplay/Progression/Quests/HomeQuestNpc.cs; Assets/Tests/EditMode/Editor/AudioGroundworkTests.cs

## Assets/Scripts/Presentation/Audio/AudioData/ShooterAudioProfileSO.cs
- 분류: 일반 소스; 59행
- 타입: ShooterAudioProfileSO : DescriptionBaseSO, PlayerShooterAudioProfileSO : ShooterAudioProfileSO
- 메서드: 
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 3개: Assets/Scripts/Gameplay/Entities/Shooter/Common/Core/EntityShooterCharacterAudio.cs; Assets/Scripts/Gameplay/Entities/Shooter/Enemy/Common/EnemyShooterCharacterAudio.cs; Assets/Scripts/Gameplay/Entities/Shooter/Player/PlayerShooterCharacterAudio.cs

## Assets/Scripts/Presentation/Audio/AudioData/WeaponAudioProfileSO.cs
- 분류: 일반 소스; 54행
- 타입: WeaponAudioProfileSO : DescriptionBaseSO
- 메서드: 
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 3개: Assets/Scripts/Gameplay/Items/Weapons/Definitions/ThrowableUsableItemSO.cs; Assets/Scripts/Gameplay/Items/Weapons/Definitions/WeaponSO.cs; Assets/Scripts/Presentation/Audio/AudioData/ShooterAudioProfileSO.cs

## Assets/Scripts/Presentation/Audio/AudioEvent.cs
- 분류: 일반 소스; 35행
- 타입: AudioEvent : ScriptableObject
- 메서드: Pick
- 직렬화 참조 24개 / 지정 씬 참조 그래프 내 0개: Assets/Audio/Audio/EventSO/BGM/GameScene/BGM.gamescene_gameover.asset; Assets/Audio/Audio/EventSO/BGM/GameScene/BGM.gamescene_playing.asset; Assets/Audio/Audio/EventSO/BGM/GameScene/BGM.gamescene_success.asset; Assets/Audio/Audio/EventSO/BGM/MainScene/BGM.mainscene.asset; Assets/Audio/Audio/EventSO/BGM/RiskSelectionScene/BGM.riskselectionscene.asset; Assets/Audio/Audio/EventSO/SFX/GameScene/SFX_carduse.asset; Assets/Audio/Audio/EventSO/SFX/GameScene/SFX_contamspawn.asset; Assets/Audio/Audio/EventSO/SFX/GameScene/SFX_dragchrageon.asset
- 타입 이름을 참조하는 소스 2개: Assets/Scripts/Presentation/Audio/AudioCatalog.cs; Assets/Scripts/Presentation/Audio/AudioMaster.cs

## Assets/Scripts/Presentation/Audio/AudioLoopEmitter.cs
- 분류: 일반 소스; 69행
- 타입: AudioLoopEmitter : MonoBehaviour
- 메서드: OnEnable, OnDisable, StartLoop, StopLoop
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 0개: 

## Assets/Scripts/Presentation/Audio/AudioManager.cs
- 분류: 일반 소스; 416행
- 타입: AudioManager : MonoBehaviour
- 메서드: Awake, Start, OnEnable, OnDisable, OnValidate, ApplyAllVolumes, ChangeMasterVolume, ChangeMusicVolume, ChangeSFXVolume, SetGroupVolume, GetGroupVolume, MixerValueToNormalized, NormalizedToMixerValue, PlayMusicTrack, StopMusic, TimelineInterruptsMusic, PlayAudioCue, ResolvePlaybackConfiguration, FinishAudioCue, StopAudioCue, OnSoundEmitterFinishedPlaying, StopAndCleanEmitter, StopMusicEmitter
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 1개: Assets/Scenes/Manager/PersistenceManager.unity
- 타입 이름을 참조하는 소스 2개: Assets/Scripts/Presentation/Audio/AudioData/AudioCueSO.cs; Assets/Scripts/Presentation/Audio/BulletImpactAudio.cs

## Assets/Scripts/Presentation/Audio/AudioMaster.cs
- 분류: 일반 소스; 308행
- 타입: AudioMaster : MonoBehaviour
- 메서드: Start, PlayBGM, PlayBGMKey, PlayBGMEvent, PlaySFX2D, PlaySFXAt, PlayKey, PlayEvent, SetMasterVolume, SetMusicVolume, SetSfxVolume, PersistVolume, LinearToDb
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 0개: Assets/Prefabs/AudioMaster.prefab
- 타입 이름을 참조하는 소스 6개: Assets/Scripts/Presentation/Audio/Sound/UI/ButtonClick.cs; Assets/Scripts/Presentation/Audio/Sound/UI/UI_AchievementPopupClick.cs; Assets/Scripts/Presentation/Audio/Sound/UI/UI_AchievementRewardClick.cs; Assets/Scripts/Presentation/Audio/Sound/UI/UI_RiskOnOff.cs; Assets/Scripts/Presentation/Audio/Sound/UI/UI_SkinInventoryEquip.cs; Assets/Scripts/WIP/Legacy/MainScene/Achievement/SkinRewardSO.cs

## Assets/Scripts/Presentation/Audio/BulletImpactAudio.cs
- 분류: 일반 소스; 87행
- 타입: BulletImpactAudio
- 메서드: ResetRuntimeState, SupportsBullet, TryPlay
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 2개: Assets/Scripts/Gameplay/Items/Weapons/Runtime/BasicBullet.cs; Assets/Tests/EditMode/Editor/ObstacleBulletImpactAudioTests.cs

## Assets/Scripts/Presentation/Audio/CameraAlignedAudioListener.cs
- 분류: 일반 소스; 92행
- 타입: CameraAlignedAudioListener : MonoBehaviour
- 메서드: OnEnable, OnDisable, LateUpdate, HandleWorldCameraChanged, ResolveCameraFallback, AlignWithCameraScreen
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 1개: Assets/Prefabs/GameScene/Shooter/Player/Player.prefab
- 타입 이름을 참조하는 소스 0개: 

## Assets/Scripts/Presentation/Audio/InteractionSearchAudio.cs
- 분류: 일반 소스; 105행
- 타입: InteractionSearchAudio : MonoBehaviour
- 메서드: OnEnable, OnDisable, Update, HandleCast, StartLoop, StopLoop, PlayOneShot, EndSearch
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 1개: Assets/Scenes/Manager/PersistenceManager.unity
- 타입 이름을 참조하는 소스 1개: Assets/Tests/EditMode/Editor/AudioGroundworkTests.cs

## Assets/Scripts/Presentation/Audio/MusicPlayer.cs
- 분류: 일반 소스; 49행
- 타입: MusicPlayer : MonoBehaviour
- 메서드: OnEnable, OnDisable, PlayMusic, PlayPauseMusic
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 1개: Assets/Prefabs/UI/MusicPlayer.prefab
- 타입 이름을 참조하는 소스 0개: 

## Assets/Scripts/Presentation/Audio/ObstacleAudio.cs
- 분류: 일반 소스; 123행
- 타입: ObstacleAudio : MonoBehaviour
- 메서드: OnEnable, OnDisable, PlayOnHit, PlayOnHitAt, HandleObjectAudioRequested, HandleHitReceived, ResolveHitCue, IsTargetForThisObject, PlayAudio
- 직렬화 참조 241개 / 지정 씬 참조 그래프 내 6개: Assets/Prefabs/GameScene/GameItemObject/LootableObject/AmoBox_01.prefab; Assets/Prefabs/GameScene/GameItemObject/LootableObject/SM_Props_MetalBox_06.prefab; Assets/Prefabs/GameScene/GameItemObject/LootableObject/SM_Props_MetalBox_10.prefab; Assets/Prefabs/GameScene/Sector/SectorObject/Armchair_01.prefab; Assets/Prefabs/GameScene/Sector/SectorObject/ATM.prefab; Assets/Prefabs/GameScene/Sector/SectorObject/AutoService_01.prefab; Assets/Prefabs/GameScene/Sector/SectorObject/Bank_01.prefab; Assets/Prefabs/GameScene/Sector/SectorObject/Barrel_01 1.prefab
- 타입 이름을 참조하는 소스 1개: Assets/Tests/EditMode/Editor/ObstacleBulletImpactAudioTests.cs

## Assets/Scripts/Presentation/Audio/ObstacleSurface.cs
- 분류: 일반 소스; 47행
- 타입: ObstacleSurfaceType, ObstacleSurface : MonoBehaviour
- 메서드: OnEnable, CanPlayImpact, RecordImpact
- 직렬화 참조 23개 / 지정 씬 참조 그래프 내 15개: Assets/Prefabs/GameScene/Sector/SectorObject/ObstacleObject/Barrel_02.prefab; Assets/Prefabs/GameScene/Sector/SectorObject/ObstacleObject/Barrel_04.prefab; Assets/Prefabs/GameScene/Sector/SectorObject/ObstacleObject/Box_02 1.prefab; Assets/Prefabs/GameScene/Sector/SectorObject/ObstacleObject/Box_02.prefab; Assets/Prefabs/GameScene/Sector/SectorObject/ObstacleObject/Fountain_01.prefab; Assets/Prefabs/GameScene/Sector/SectorObject/ObstacleObject/MetalBox1.prefab; Assets/Prefabs/GameScene/Sector/SectorObject/ObstacleObject/MetalBox2.prefab; Assets/Prefabs/GameScene/Sector/SectorObject/ObstacleObject/MetalBox3.prefab
- 타입 이름을 참조하는 소스 3개: Assets/Scripts/Presentation/Audio/AudioData/BulletImpactAudioCatalogSO.cs; Assets/Scripts/Presentation/Audio/BulletImpactAudio.cs; Assets/Tests/EditMode/Editor/ObstacleBulletImpactAudioTests.cs

## Assets/Scripts/Presentation/Audio/Sound/UI/ButtonClick.cs
- 분류: 일반 소스; 22행
- 타입: ButtonClick : MonoBehaviour
- 메서드: 
- 직렬화 참조 3개 / 지정 씬 참조 그래프 내 2개: Assets/Prefabs/UI/GamePlay/GameOverPanel.prefab; Assets/Prefabs/UI/GamePlay/Survival_Success_Panel.prefab; Assets/Prefabs/UI/MainMenu/Canvas-MainMenu.prefab
- 타입 이름을 참조하는 소스 6개: Assets/Scripts/Core/Events/ScriptableObject/UIAudioEventChannelSO.cs; Assets/Scripts/Presentation/Audio/UIAudio.cs; Assets/Scripts/Presentation/Audio/UIButtonAudioEventEmitter.cs; Assets/Scripts/Presentation/UI/Gameplay/PanelHub/Inventory/EquipmentInventorySlotUI.cs; Assets/Scripts/Presentation/UI/Gameplay/PanelHub/Inventory/InventoryItemSlotUI.cs; Assets/Tests/EditMode/Editor/AudioGroundworkTests.cs

## Assets/Scripts/Presentation/Audio/Sound/UI/UI_AchievementPopupClick.cs
- 분류: 일반 소스; 27행
- 타입: UI_AchievementPopupClick : MonoBehaviour
- 메서드: 
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 0개: 

## Assets/Scripts/Presentation/Audio/Sound/UI/UI_AchievementRewardClick.cs
- 분류: 일반 소스; 21행
- 타입: UI_AchievementRewardClick : MonoBehaviour
- 메서드: 
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 0개: Assets/Prefabs/MainScene/Reward/RewardEntryPrefab.prefab
- 타입 이름을 참조하는 소스 0개: 

## Assets/Scripts/Presentation/Audio/Sound/UI/UI_RiskOnOff.cs
- 분류: 일반 소스; 37행
- 타입: UI_RiskOnOff : MonoBehaviour
- 메서드: 
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 0개: Assets/Prefabs/RiskSelection/TogglePrefab.prefab
- 타입 이름을 참조하는 소스 0개: 

## Assets/Scripts/Presentation/Audio/Sound/UI/UI_SkinInventoryEquip.cs
- 분류: 일반 소스; 37행
- 타입: UI_SkinInventoryEquip : MonoBehaviour
- 메서드: 
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 0개: Assets/Prefabs/MainScene/SkinGridItem.prefab
- 타입 이름을 참조하는 소스 0개: 

## Assets/Scripts/Presentation/Audio/SoundEmitter/AudioGainFilter.cs
- 분류: 일반 소스; 37행
- 타입: AudioGainFilter : MonoBehaviour
- 메서드: SetGainDb, OnAudioFilterRead
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 1개: Assets/Prefabs/Audio/SoundEmitter.prefab
- 타입 이름을 참조하는 소스 1개: Assets/Scripts/Presentation/Audio/SoundEmitter/SoundEmitter.cs

## Assets/Scripts/Presentation/Audio/SoundEmitter/SoundEmitter.cs
- 분류: 일반 소스; 255행
- 타입: SoundEmitter : MonoBehaviour
- 메서드: Awake, PlayAudioCue, PlayAudioClip, FadeMusicIn, FadeMusicOut, GetClip, Resume, Pause, Stop, Finish, IsPlaying, IsLooping, FinishedPlaying, FadeVolumeRoutine, NotifyBeingDone, StopInternalRoutines, StopFinishRoutineOnly, ResolveListenerTransform
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 1개: Assets/Prefabs/Audio/SoundEmitter.prefab
- 타입 이름을 참조하는 소스 3개: Assets/Scripts/Presentation/Audio/AudioManager.cs; Assets/Scripts/Presentation/Audio/SoundEmitter/AudioGainFilter.cs; Assets/Scripts/Presentation/Audio/SoundEmitter/SoundEmitterPoolSO.cs

## Assets/Scripts/Presentation/Audio/SoundEmitter/SoundEmitterPoolSO.cs
- 분류: 일반 소스; 96행
- 타입: SoundEmitterPoolSO : ScriptableObject
- 메서드: SetParent, Prewarm, Request, Return, CreateAndStoreEmitter
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 1개: Assets/ScriptableObject/Audio/SoundEmitterPool.asset
- 타입 이름을 참조하는 소스 2개: Assets/Scripts/Presentation/Audio/AudioManager.cs; Assets/Scripts/Presentation/Audio/SoundEmitter/SoundEmitter.cs

## Assets/Scripts/Presentation/Audio/UIAudio.cs
- 분류: 일반 소스; 240행
- 타입: UIAudio : MonoBehaviour
- 메서드: OnEnable, OnDisable, Request, ReportResult, PlayQuestNotification, BindActiveSelectables, LateUpdate, Priority, PlayPanelOpen, PlayPanelClose, PlayButtonClick, PlayDenied, PlayInventoryMove, PlayEquip, PlayUnequip, PlaySplit, PlayBuy, PlaySell, PlayRepair, Play, ResolveCue, CanPlay
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 1개: Assets/Scenes/Manager/PersistenceManager.unity
- 타입 이름을 참조하는 소스 11개: Assets/Scripts/Gameplay/Progression/Quests/QuestDefinitionSO.cs; Assets/Scripts/Gameplay/Progression/Quests/QuestRuntime.cs; Assets/Scripts/Presentation/Audio/UIButtonAudioEventEmitter.cs; Assets/Scripts/Presentation/UI/Gameplay/PanelHub/Inventory/EnemyInventoryUI.cs; Assets/Scripts/Presentation/UI/Gameplay/PanelHub/Inventory/EquipmentInventorySlotUI.cs; Assets/Scripts/Presentation/UI/Gameplay/PanelHub/Inventory/InventoryItemSlotUI.cs; Assets/Scripts/Presentation/UI/Gameplay/PanelHub/Inventory/PlayerInventoryUI.cs; Assets/Scripts/Presentation/UI/Gameplay/PanelHub/QuestMissionPanelUI.cs

## Assets/Scripts/Presentation/Audio/UIButtonAudioEventEmitter.cs
- 분류: 일반 소스; 91행
- 타입: UIButtonAudioEventEmitter : MonoBehaviour, IPointerClickHandler,
- 메서드: Bind, OnEnable, OnDisable, Emit, HandleButtonClick, Navigate, OnPointerEnter, OnSelect, OnMove, OnDrag, OnCancel, OnSubmit, Reset, OnPointerClick
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 2개: Assets/Scripts/Presentation/Audio/UIAudio.cs; Assets/Tests/EditMode/Editor/AudioGroundworkTests.cs

## Assets/Scripts/Presentation/Audio/WorldGameAudio.cs
- 분류: 일반 소스; 15행
- 타입: WorldGameAudio : MonoBehaviour
- 메서드: 
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 1개: Assets/Scenes/Manager/PersistenceManager.unity
- 타입 이름을 참조하는 소스 0개: 

## Assets/Scripts/Presentation/Camera/CameraEntityOcclusionFadeController.cs
- 분류: 일반 소스; 854행
- 타입: CameraEntityOcclusionFadeController : MonoBehaviour, CharacterTarget, OverlayBinding
- 메서드: EnsureFor, Reset, Awake, OnEnable, OnDisable, OnDestroy, LateUpdate, RefreshCharacterTargets, RegisterCharacter, TryGetOrCreateBinding, CreateBinding, ConfigureOverlayRenderer, RefreshBindingGeometryAndMaterials, GetOrCreateSilhouetteMaterial, CreateSilhouetteMaterial, IsOccludedByStructure, HasStructureBetween, IsInsideCameraFrustum, RemoveStaleBindings, DestroyBinding, ApplySilhouetteSettings, ReleaseRuntimeResources, ResolveCameraAndShader, ResolveSourceTexture, ResolveAlphaCutoff, DestroyRuntimeObject, OnValidate, Add, TryGetVisibleBounds, SetSilhouetteEnabled, RefreshGeometry, RequiresMaterialRefresh, ApplyMaterials, CanSourceRender, SetEnabled
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 1개: Assets/Scripts/Presentation/Camera/CameraManager.cs

## Assets/Scripts/Presentation/Camera/CameraManager.cs
- 분류: 일반 소스; 290행
- 타입: CameraManager : MonoBehaviour
- 메서드: Awake, Reset, OnEnable, OnDisable, LateUpdate, SetFollowTarget, SnapToTarget, ResolveCameraPose, ResolveCameraRotation, UpdateLookAhead, ResetFollowState, TryResolveFollowTarget, ResolveCameraRefs, EnsureEntityOcclusionController
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 1개: Assets/Scenes/RUNScene.unity
- 타입 이름을 참조하는 소스 5개: Assets/Scripts/Authoring/Editor/DebugMonitorWindow.cs; Assets/Scripts/Core/SceneFlow/SceneLoader.cs; Assets/Scripts/Gameplay/Progression/Stage/StageProgressionManager.cs; Assets/Scripts/Gameplay/World/Sectors/Runtime/SectorPortalManager.cs; Assets/Scripts/Presentation/UI/Gameplay/Vision/PlayerVisionFogOverlayUI.cs

## Assets/Scripts/Presentation/UI/Common/CreditPanel.cs
- 분류: 일반 소스; 123행
- 타입: CreditPanel : PanelUI
- 메서드: Reset, Awake, OnEnable, OnDisable, OpenCreditPanel, CloseScreen, ResolveRefs, ApplyCreditText, ResetScrollToTop
- 직렬화 참조 2개 / 지정 씬 참조 그래프 내 2개: Assets/Prefabs/UI/MainMenu/Canvas-MainMenu.prefab; Assets/Prefabs/UI/MainMenu/CreditPanel.prefab
- 타입 이름을 참조하는 소스 1개: Assets/Scripts/Presentation/UI/Common/UIMenuManager.cs

## Assets/Scripts/Presentation/UI/Common/ESCMenu/EscPauseMenuController.cs
- 분류: 일반 소스; 529행
- 타입: EscPauseMenuController : MonoBehaviour, PauseConfirmationAction
- 메서드: Awake, OnEnable, OnDisable, OnDestroy, Update, OpenPause, ResumeGame, BeginPauseBackdropCapture, CapturePauseBackdropRoutine, EnsurePauseBackdropView, ResolvePauseBlurMaterial, CancelPauseBackdropCapture, SetPauseBackdropVisible, ReleasePauseBackdropTexture, CreatePauseRenderTexture, ReleaseIfNotDisplayed, OpenSettings, CloseSettings, ReturnToMainMenu, QuitGame, MovePlayerToCurrentSectorCenter, SetVisible, ShowBackToMenuConfirmation, ShowQuitConfirmation, ShowConfirmation, HandleConfirmationResponse, HideConfirmationPopup
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 1개: Assets/Prefabs/UI/GamePlay/Canvas-ESCMenu.prefab
- 타입 이름을 참조하는 소스 0개: 

## Assets/Scripts/Presentation/UI/Common/ESCMenu/PlayerSectorCenterRescue.cs
- 분류: 일반 소스; 254행
- 타입: PlayerSectorCenterRescue : MonoBehaviour
- 메서드: Awake, OnEnable, OnDisable, TryMovePlayerToCurrentSectorCenter, HandlePlayerReady, HandleCurrentSectorChanged, MovePlayer, ResolveTargetY, ResolveFallbackRefs, ResolvePlayerFallback, ResolveCurrentSectorFallback, ContainsXZ, Describe
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 1개: Assets/Prefabs/UI/GamePlay/Canvas-ESCMenu.prefab
- 타입 이름을 참조하는 소스 1개: Assets/Scripts/Presentation/UI/Common/ESCMenu/EscPauseMenuController.cs

## Assets/Scripts/Presentation/UI/Common/ESCMenu/ScriptableObjects/SettingsSO.cs
- 분류: 일반 소스; 103행
- 타입: SettingsSO : ScriptableObject
- 메서드: SaveGeneralSettings, SaveAudioSettings, SaveGraphicsSettings, LoadSavedSettings, SetLocale
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 1개: Assets/ScriptableObject/SaveSystem/Setting.asset
- 타입 이름을 참조하는 소스 5개: Assets/Scripts/Core/Persistence/SaveSystem.cs; Assets/Scripts/Presentation/UI/Common/ESCMenu/SettingsSystem.cs; Assets/Scripts/Presentation/UI/Common/ESCMenu/UISettingGeneralComponent.cs; Assets/Scripts/Presentation/UI/Common/ESCMenu/UISettingGraphicsComponent.cs; Assets/Scripts/Presentation/UI/Common/ESCMenu/UISettingsController.cs

## Assets/Scripts/Presentation/UI/Common/ESCMenu/SettingsSystem.cs
- 분류: 일반 소스; 113행
- 타입: SettingsSystem : MonoBehaviour
- 메서드: Awake, OnEnable, OnDisable, SetCurrentSettings, SaveSettings, ApplySavedLocaleAfterInitialization
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 1개: Assets/Scenes/Manager/PersistenceManager.unity
- 타입 이름을 참조하는 소스 1개: Assets/Scripts/Core/Persistence/SaveSystem.cs

## Assets/Scripts/Presentation/UI/Common/ESCMenu/UIPaginationFiller.cs
- 분류: 일반 소스; 97행
- 타입: UIPaginationFiller : MonoBehaviour
- 메서드: Awake, SetPagination, SetCurrentPagination
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 0개: 

## Assets/Scripts/Presentation/UI/Common/ESCMenu/UISettingAudioComponent.cs
- 분류: 일반 소스; 209행
- 타입: UISettingAudioComponent : MonoBehaviour
- 메서드: OnEnable, OnDisable, Setup, IncreaseMasterVolume, DecreaseMasterVolume, IncreaseMusicVolume, DecreaseMusicVolume, IncreaseSFXVolume, DecreaseSFXVolume, RefreshAllFields, RefreshMasterVolumeField, RefreshMusicVolumeField, RefreshSfxVolumeField, RefreshVolumeField, BroadcastAllVolumes, BroadcastMasterVolume, BroadcastMusicVolume, BroadcastSfxVolume, SaveCurrentVolumes
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 1개: Assets/Prefabs/UI/GamePlay/Settings/SettingsPanel.prefab
- 타입 이름을 참조하는 소스 1개: Assets/Scripts/Presentation/UI/Common/ESCMenu/UISettingsController.cs

## Assets/Scripts/Presentation/UI/Common/ESCMenu/UISettingGeneralComponent.cs
- 분류: 일반 소스; 356행
- 타입: UISettingGeneralComponent : MonoBehaviour
- 메서드: OnEnable, OnDisable, Setup, InitializeLanguage, FindLocale, ResolveLanguageIndex, SelectLanguage, ApplyLanguage, OnSelectedLocaleChanged, NextResolution, PreviousResolution, ApplyResolution, EnableFullscreen, DisableFullscreen, ApplyFullscreen, IncreaseAimSensitivity, DecreaseAimSensitivity, SaveCurrentSettings, RefreshLanguageField, RefreshResolutionField, RefreshFullscreenField, RefreshAimSensitivityField, GetResolutionsList, AimSensitivityToIndex, IndexToAimSensitivity
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 1개: Assets/Prefabs/UI/GamePlay/Settings/SettingsPanel.prefab
- 타입 이름을 참조하는 소스 1개: Assets/Scripts/Presentation/UI/Common/ESCMenu/UISettingsController.cs

## Assets/Scripts/Presentation/UI/Common/ESCMenu/UISettingGraphicsComponent.cs
- 분류: 일반 소스; 150행
- 타입: UISettingGraphicsComponent : MonoBehaviour
- 메서드: OnEnable, OnDisable, Setup, EnableVSync, DisableVSync, NextFrameRate, PreviousFrameRate, ApplyGraphics, RefreshFields, SaveCurrentSettings, FindFrameRateIndex
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 1개: Assets/Prefabs/UI/GamePlay/Settings/SettingsPanel.prefab
- 타입 이름을 참조하는 소스 1개: Assets/Scripts/Presentation/UI/Common/ESCMenu/UISettingsController.cs

## Assets/Scripts/Presentation/UI/Common/ESCMenu/UISettingItemFiller.cs
- 분류: 일반 소스; 437행
- 타입: UISettingControlType, UISettingItemFiller : MonoBehaviour
- 메서드: OnEnable, OnDisable, FillDropdown, FillDropdownLocalized, FillSlider, FillSettingField, FillSettingField_Localized, FillLocalizedDropdown, SetInteractable, SelectItem, UnselectItem, SetNavigation, FillDropdownInternal, FillDropdownLocalizedRoutine, HandleDropdownChanged, HandleSliderChanged, HandleLocaleChanged, RefreshTitle, RefreshTitleRoutine, RefreshSliderText, GetDefaultTitleEntryKey, BuildFallbackOptions
- 직렬화 참조 3개 / 지정 씬 참조 그래프 내 3개: Assets/Prefabs/UI/GamePlay/Settings/SettingsPanel.prefab; Assets/Prefabs/UI/GamePlay/Settings/Settings_Item_Dropdown.prefab; Assets/Prefabs/UI/GamePlay/Settings/Settings_Item_Sound.prefab
- 타입 이름을 참조하는 소스 4개: Assets/Scripts/Presentation/UI/Common/ESCMenu/UISettingAudioComponent.cs; Assets/Scripts/Presentation/UI/Common/ESCMenu/UISettingGeneralComponent.cs; Assets/Scripts/Presentation/UI/Common/ESCMenu/UISettingGraphicsComponent.cs; Assets/Scripts/Presentation/UI/Common/ESCMenu/UISettingLanguageComponent.cs

## Assets/Scripts/Presentation/UI/Common/ESCMenu/UISettingKeyBindingComponent.cs
- 분류: 일반 소스; 793행
- 타입: UISettingKeyBindingComponent : MonoBehaviour, DefaultBindingRowSpec, BindingRow
- 메서드: OnEnable, OnDisable, Setup, EnsureDefaultRowsRegistered, FindBindingRow, ApplyDefaultRowLocalization, CreateBindingRow, FindDescendantTransform, SubscribeRows, UnsubscribeRows, BeginRebind, FinishRebind, CancelActiveRebind, ResetSingleBinding, ResetAllToDefaults, RefreshView, RefreshRow, HandleLocaleChanged, RefreshLocalizedRowTitles, RefreshLocalizedRowTitlesRoutine, LoadBindingOverrides, SaveBindingOverrides, ResolveAction, ResolveInputActions, SetRebindOverlayVisible, SetRebindOverlayText, RefreshLocalizedRebindOverlay, RefreshLocalizedRebindOverlayRoutine
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 1개: Assets/Prefabs/UI/GamePlay/Settings/SettingsPanel.prefab
- 타입 이름을 참조하는 소스 1개: Assets/Scripts/Presentation/UI/Common/ESCMenu/UISettingsController.cs

## Assets/Scripts/Presentation/UI/Common/ESCMenu/UISettingLanguageComponent.cs
- 분류: 일반 소스; 127행
- 타입: UISettingLanguageComponent : MonoBehaviour
- 메서드: OnDisable, SaveSettings, ResetSettings
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 0개: 

## Assets/Scripts/Presentation/UI/Common/ESCMenu/UISettingsController.cs
- 분류: 일반 소스; 320행
- 타입: SettingFieldType, SettingsType, UISettingsController : MonoBehaviour
- 메서드: OnEnable, OnDisable, CloseScreen, OpenSetting, SwitchTab, SaveGeneralSettings, SaveAudioSettings, SaveGraphicsSettings, SaveKeyBindingSettings, SetActive, OpenSettingsScreen, OpenInitialSettingTab, HandleLocaleChanged, RefreshSettingsTitle, RefreshSettingsTitleRoutine
- 직렬화 참조 4개 / 지정 씬 참조 그래프 내 3개: Assets/Prefabs/SettingPanel.prefab; Assets/Prefabs/UI/GamePlay/Canvas-ESCMenu.prefab; Assets/Prefabs/UI/GamePlay/Settings/SettingsPanel.prefab; Assets/Prefabs/UI/MainMenu/Canvas-MainMenu.prefab
- 타입 이름을 참조하는 소스 5개: Assets/Scripts/Presentation/UI/Common/ESCMenu/EscPauseMenuController.cs; Assets/Scripts/Presentation/UI/Common/ESCMenu/UISettingItemFiller.cs; Assets/Scripts/Presentation/UI/Common/ESCMenu/UISettingTabFiller.cs; Assets/Scripts/Presentation/UI/Common/ESCMenu/UISettingTabsFiller.cs; Assets/Scripts/Presentation/UI/Common/UIMenuManager.cs

## Assets/Scripts/Presentation/UI/Common/ESCMenu/UISettingTabFiller.cs
- 분류: 일반 소스; 136행
- 타입: UISettingTabFiller : MonoBehaviour
- 메서드: Awake, OnEnable, OnDisable, SetTab, SetSelected, Click, HandleLocaleChanged, RefreshTitle, RefreshTitleRoutine
- 직렬화 참조 2개 / 지정 씬 참조 그래프 내 2개: Assets/Prefabs/UI/GamePlay/Settings/SettingsPanel.prefab; Assets/Prefabs/UI/GamePlay/Settings/Setting_Generic_Tab.prefab
- 타입 이름을 참조하는 소스 1개: Assets/Scripts/Presentation/UI/Common/ESCMenu/UISettingTabsFiller.cs

## Assets/Scripts/Presentation/UI/Common/ESCMenu/UISettingTabsFiller.cs
- 분류: 일반 소스; 70행
- 타입: UISettingTabsFiller : MonoBehaviour
- 메서드: FillTabs, OnDisable, SelectTab, ChangeTab
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 1개: Assets/Prefabs/UI/GamePlay/Settings/SettingsPanel.prefab
- 타입 이름을 참조하는 소스 1개: Assets/Scripts/Presentation/UI/Common/ESCMenu/UISettingsController.cs

## Assets/Scripts/Presentation/UI/Common/PanelUI.cs
- 분류: 일반 소스; 91행
- 타입: PanelUI : MonoBehaviour, IUIOverlayLifecycle
- 메서드: Reset, Awake, OnOverlayShown, OnOverlayHidden, ShowPanel, HidePanel, OnPanelShown, OnPanelHidden, SetPanelVisible, EnsurePanelGroup
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 6개: Assets/Scripts/Presentation/UI/Common/CreditPanel.cs; Assets/Scripts/Presentation/UI/Gameplay/MetaSystem/MetaUpgradePanelUI.cs; Assets/Scripts/Presentation/UI/Gameplay/PanelHub/Inventory/InventoryPanelUIBase.cs; Assets/Scripts/Presentation/UI/Gameplay/PanelHub/PlayerPanelHub.cs; Assets/Scripts/Presentation/UI/Gameplay/PanelHub/PlayerTooltipUI.cs; Assets/Scripts/Presentation/UI/Gameplay/Shop/PlayerShopPanelUI.cs

## Assets/Scripts/Presentation/UI/Common/RadialCooldownWidget.cs
- 분류: 일반 소스; 117행
- 타입: RadialCooldownWidget : MonoBehaviour
- 메서드: Awake, OnEnable, OnDisable, OnCooldownChanged, Apply, ConfigureRadialFill, ApplyDefaultState
- 직렬화 참조 2개 / 지정 씬 참조 그래프 내 1개: Assets/Prefabs/UI/GamePlay/DashCooldownWidget.prefab; Assets/Prefabs/UI/GamePlay/ShockWaveCooldownWidget.prefab
- 타입 이름을 참조하는 소스 0개: 

## Assets/Scripts/Presentation/UI/Common/ScriptableObject/CooldownWidgetSettingsSO.cs
- 분류: 일반 소스; 35행
- 타입: CooldownWidgetSettingsSO : ScriptableObject
- 메서드: 
- 직렬화 참조 2개 / 지정 씬 참조 그래프 내 1개: Assets/ScriptableObject/GamePlay/UI/DashCooldownWidgetSettings.asset; Assets/ScriptableObject/GamePlay/UI/ShockWaveCooldownWidgetSettings 1.asset
- 타입 이름을 참조하는 소스 1개: Assets/Scripts/Presentation/UI/Common/RadialCooldownWidget.cs

## Assets/Scripts/Presentation/UI/Common/ScriptableObject/UIOpacitySettingsSO.cs
- 분류: 일반 소스; 31행
- 타입: UIOpacitySettingsSO : ScriptableObject
- 메서드: 
- 직렬화 참조 4개 / 지정 씬 참조 그래프 내 4개: Assets/ScriptableObject/GamePlay/UI/EnemyHealthBarUIOpacitySettings.asset; Assets/ScriptableObject/GamePlay/UI/InteractionPromptUIOpacitySettings.asset; Assets/ScriptableObject/GamePlay/UI/Map/FullMapUIOpacitySettings.asset; Assets/ScriptableObject/GamePlay/UI/Map/MiniMapUIOpacitySettings.asset
- 타입 이름을 참조하는 소스 1개: Assets/Scripts/Presentation/UI/Common/UICanvasGroupOpacity.cs

## Assets/Scripts/Presentation/UI/Common/UICanvasGroupOpacity.cs
- 분류: 일반 소스; 135행
- 타입: UICanvasGroupOpacity : MonoBehaviour
- 메서드: Reset, Awake, Update, ApplyImmediate, SetDefault, SetEmphasis, Show, Hide, HideImmediate, SetAlpha, ClampAlpha
- 직렬화 참조 6개 / 지정 씬 참조 그래프 내 6개: Assets/Prefabs/UI/GamePlay/Canvas-GamePlay.prefab; Assets/Prefabs/UI/GamePlay/HUD/EnemyHealthBarWidget.prefab; Assets/Prefabs/UI/GamePlay/InteractionCastBarWidget.prefab; Assets/Prefabs/UI/GamePlay/InteractionPromptWidget.prefab; Assets/Prefabs/UI/GamePlay/MAP/FullMapRoot.prefab; Assets/Prefabs/UI/GamePlay/MAP/MiniMapRoot.prefab
- 타입 이름을 참조하는 소스 6개: Assets/Scripts/Presentation/UI/Gameplay/EnemyBarUI/HealthBar/EnemyHealthBarWidget.cs; Assets/Scripts/Presentation/UI/Gameplay/EnemyBarUI/InfectionCastBar/EnemyInfectionCastBarWidget.cs; Assets/Scripts/Presentation/UI/Gameplay/Interaction/InteractionCastBarWidget.cs; Assets/Scripts/Presentation/UI/Gameplay/Map/FullMap/FullMapUI.cs; Assets/Scripts/Presentation/UI/Gameplay/Map/MiniMap/MiniMapUI.cs; Assets/Scripts/Presentation/UI/HUD/World/EnemyHealthUI/EnemyHealthBarUI.cs

## Assets/Scripts/Presentation/UI/Common/UIGenericButton.cs
- 분류: 일반 소스; 69행
- 타입: UIGenericButton : MonoBehaviour
- 메서드: OnDisable, SetButton, Click
- 직렬화 참조 6개 / 지정 씬 참조 그래프 내 6개: Assets/Prefabs/UI/GamePlay/Button/GenericButton.prefab; Assets/Prefabs/UI/GamePlay/Canvas-ESCMenu.prefab; Assets/Prefabs/UI/GamePlay/Popup/Confirmation_Popup.prefab; Assets/Prefabs/UI/GamePlay/Settings/SettingsPanel.prefab; Assets/Prefabs/UI/GamePlay/Settings/Settings_Item_KeyBind.prefab; Assets/Prefabs/UI/GamePlay/Settings/Settings_Item_Reset.prefab
- 타입 이름을 참조하는 소스 4개: Assets/Scripts/Presentation/UI/Common/ESCMenu/UISettingKeyBindingComponent.cs; Assets/Scripts/Presentation/UI/Common/ESCMenu/UISettingLanguageComponent.cs; Assets/Scripts/Presentation/UI/Common/UIPause.cs; Assets/Scripts/Presentation/UI/Common/UIPopup.cs

## Assets/Scripts/Presentation/UI/Common/UIMainMenu.cs
- 분류: 일반 소스; 196행
- 타입: UIMainMenu : MonoBehaviour
- 메서드: Awake, OnEnable, OnDisable, SetMenuScreen, ApplyTextKeys, ApplyTextKey, HandleNewGameClicked, HandleContinueClicked, HandlePlayerUpgradeClicked, HandleSettingsClicked, HandleAchievementClicked, HandleCreditClicked, HandleQuitClicked
- 직렬화 참조 2개 / 지정 씬 참조 그래프 내 2개: Assets/Prefabs/UI/MainMenu/Canvas-MainMenu.prefab; Assets/Prefabs/UI/MainMenu/MainMenuPanel.prefab
- 타입 이름을 참조하는 소스 1개: Assets/Scripts/Presentation/UI/Common/UIMenuManager.cs

## Assets/Scripts/Presentation/UI/Common/UIMenuManager.cs
- 분류: 일반 소스; 334행
- 타입: UIMenuManager : MonoBehaviour
- 메서드: Awake, Start, OnEnable, OnDisable, RefreshMenuScreen, ContinueGame, ButtonStartNewGameClicked, ConfirmStartNewGame, ShowStartNewGameConfirmationPopup, StartNewGamePopupResponse, HidePopup, OpenAchievementScreen, CloseAchievementScreen, OpenMetaSystemScreen, CloseMetaSystemScreen, OpenSettingsScreen, CloseSettingsScreen, OpenCreditScreen, CloseCreditScreen, ShowExitConfirmationPopup, HideExitConfirmationPopup, OnDestroy, SetVisible
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 1개: Assets/Prefabs/UI/MainMenu/Canvas-MainMenu.prefab
- 타입 이름을 참조하는 소스 0개: 

## Assets/Scripts/Presentation/UI/Common/UIPause.cs
- 분류: 일반 소스; 204행
- 타입: UIPause : MonoBehaviour
- 메서드: OnEnable, OnDisable, BindButtons, UnbindButtons, HandleLocaleChanged, RefreshTexts, RefreshTextsRoutine, SetLocalizedText, Resume, OpenSettingsScreen, BackToMainMenuConfirmation, RequestQuit, RequestSectorCenterRescue, CloseScreen
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 1개: Assets/Prefabs/UI/GamePlay/Canvas-ESCMenu.prefab
- 타입 이름을 참조하는 소스 1개: Assets/Scripts/Presentation/UI/Common/ESCMenu/EscPauseMenuController.cs

## Assets/Scripts/Presentation/UI/Common/UIPopup.cs
- 분류: 일반 소스; 211행
- 타입: PopupType, UIPopup : MonoBehaviour
- 메서드: Awake, OnEnable, OnDisable, Show, Hide, SetPopup, ClosePopupButtonClicked, HandleLocaleChanged, RefreshTexts, RefreshTextsRoutine, ResolveDescriptionKey, SetLocalizedText, ConfirmButtonClicked, CancelButtonClicked, UnbindButtons, SetVisible
- 직렬화 참조 3개 / 지정 씬 참조 그래프 내 3개: Assets/Prefabs/UI/GamePlay/Canvas-ESCMenu.prefab; Assets/Prefabs/UI/GamePlay/Popup/Confirmation_Popup.prefab; Assets/Prefabs/UI/MainMenu/Canvas-MainMenu.prefab
- 타입 이름을 참조하는 소스 2개: Assets/Scripts/Presentation/UI/Common/ESCMenu/EscPauseMenuController.cs; Assets/Scripts/Presentation/UI/Common/UIMenuManager.cs

## Assets/Scripts/Presentation/UI/Common/UIRuntimeLocalization.cs
- 분류: 일반 소스; 264행
- 타입: UIRuntimeLocalization, UIStaticTextLocalizationBootstrap : MonoBehaviour, StaticTextEntry
- 메서드: Get, Format, EnsureTableLoaded, EnsureBootstrap, OnEnable, OnDisable, HandleSceneLoaded, HandleLocaleChanged, RequestRefresh, RefreshRoutine
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 18개: Assets/Scripts/Gameplay/World/Sectors/StageMap/RaidMapLabels.cs; Assets/Scripts/Gameplay/World/Sectors/StageMap/RaidStation.cs; Assets/Scripts/Presentation/UI/Gameplay/GameOver/GameOverUI.cs; Assets/Scripts/Presentation/UI/Gameplay/Hideout/HideoutStageSelectPanel.cs; Assets/Scripts/Presentation/UI/Gameplay/Loot/LockedLootKeyPanel.cs; Assets/Scripts/Presentation/UI/Gameplay/Map/FullMap/FullMapUI.cs; Assets/Scripts/Presentation/UI/Gameplay/PanelHub/Inventory/KeyToolContainerPanel.cs; Assets/Scripts/Presentation/UI/Gameplay/PanelHub/Inventory/PlayerInventoryUI.cs

## Assets/Scripts/Presentation/UI/Common/UIVisibilityHelper.cs
- 분류: 일반 소스; 93행
- 타입: UIVisibilityHelper
- 메서드: EnsureCanvasGroup, SetVisible, ForceActive, SetImage, SetText
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 2개: Assets/Scripts/Presentation/UI/Gameplay/Map/FullMap/FullMapCellUI.cs; Assets/Scripts/Presentation/UI/Gameplay/Map/MiniMap/MiniMapCellUI.cs

## Assets/Scripts/Presentation/UI/Customization/SkinGridItem.cs
- 분류: 일반 소스; 73행
- 타입: SkinGridItem : MonoBehaviour
- 메서드: Bind, SetEquippedVisual
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 0개: Assets/Prefabs/MainScene/SkinGridItem.prefab
- 타입 이름을 참조하는 소스 1개: Assets/Scripts/WIP/Legacy/MainScene/Skininventory/SkinInventoryManager.cs

## Assets/Scripts/Presentation/UI/Display/ResolutionManager.cs
- 분류: 일반 소스; 255행
- 타입: ResolutionManager : MonoBehaviour, ResolutionSafeWin, RECT, MONITORINFO
- 메서드: ApplyAll, ApplySafeWindowed, RequestFullscreen, ApplyWindowedSafe
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 0개: 

## Assets/Scripts/Presentation/UI/Gameplay/BossReward/BossRewardChoiceButtonUI.cs
- 분류: 일반 소스; 75행
- 타입: BossRewardChoiceButtonUI : MonoBehaviour
- 메서드: Reset, OnEnable, OnDisable, Bind, HandleClicked
- 직렬화 참조 2개 / 지정 씬 참조 그래프 내 2개: Assets/Prefabs/UI/GamePlay/BossRewardChoicePanel.prefab; Assets/Prefabs/UI/GamePlay/BossRewardChoice_Area.prefab
- 타입 이름을 참조하는 소스 1개: Assets/Scripts/Presentation/UI/Gameplay/BossReward/BossRewardUIController.cs

## Assets/Scripts/Presentation/UI/Gameplay/BossReward/BossRewardUIController.cs
- 분류: 일반 소스; 143행
- 타입: BossRewardUIController : MonoBehaviour
- 메서드: Awake, OnEnable, OnDisable, HandleRewardRequested, ShowAfterDelayRoutine, BindChoices, HandleRewardClicked, SetVisible, LockInput, UnlockInput
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 1개: Assets/Prefabs/UI/GamePlay/BossRewardChoicePanel.prefab
- 타입 이름을 참조하는 소스 0개: 

## Assets/Scripts/Presentation/UI/Gameplay/Currency/PlayerCurrencyHUD.cs
- 분류: 일반 소스; 18행
- 타입: PlayerCurrencyHUD : MonoBehaviour
- 메서드: Awake
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 1개: Assets/Prefabs/UI/GamePlay/Currency.prefab
- 타입 이름을 참조하는 소스 0개: 

## Assets/Scripts/Presentation/UI/Gameplay/Currency/PlayerCurrencyRuntime.cs
- 분류: 일반 소스; 94행
- 타입: PlayerCurrencyRuntime : MonoBehaviour
- 메서드: Awake, ResetRunCurrencyForNewRun, RestoreRunCurrencyForPersistence, LoadRoguelikeCurrencyFromSaveSystem, WriteRoguelikeCurrencyToSaveSystem, AddCurrency, CanSpend, TrySpendCurrency, GetCurrency, SetCurrency, ClearLegacyCurrency
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 1개: Assets/Prefabs/GameScene/Shooter/Player/Player.prefab
- 타입 이름을 참조하는 소스 1개: Assets/Scripts/Core/Events/ScriptableObject/PlayerCurrencyRuntimeReadyEventChannelSO.cs

## Assets/Scripts/Presentation/UI/Gameplay/DamageNumber/DamageNumberAnchor.cs
- 분류: 일반 소스; 26행
- 타입: DamageNumberAnchor : MonoBehaviour
- 메서드: ResolveWorldPosition
- 직렬화 참조 2개 / 지정 씬 참조 그래프 내 2개: Assets/Prefabs/GameScene/Shooter/EnemyShooter/EnemyShooter_Base.prefab; Assets/Prefabs/GameScene/Shooter/Player/Player.prefab
- 타입 이름을 참조하는 소스 1개: Assets/Scripts/Presentation/UI/Gameplay/DamageNumber/DamageNumberUIManager.cs

## Assets/Scripts/Presentation/UI/Gameplay/DamageNumber/DamageNumberPopupUI.cs
- 분류: 일반 소스; 218행
- 타입: DamageNumberPopupUI : MonoBehaviour
- 메서드: Reset, Awake, OnDisable, Play, SetBasePosition, SetPairOffset, SetStackOffset, SetAmount, Update, ApplyPosition, ApplyScale, ResolveAlpha, ClearEntry, SetAlpha, EnsureRefs
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 1개: Assets/Prefabs/UI/GamePlay/HUD/DamageNumberPopup.prefab
- 타입 이름을 참조하는 소스 1개: Assets/Scripts/Presentation/UI/Gameplay/DamageNumber/DamageNumberUIManager.cs

## Assets/Scripts/Presentation/UI/Gameplay/DamageNumber/DamageNumberUIManager.cs
- 분류: 일반 소스; 476행
- 타입: DamageNumberUIManager : MonoBehaviour, ActivePopup
- 메서드: Reset, Awake, OnEnable, OnDisable, HandleWorldCameraChanged, LateUpdate, HandleDamageNumberRequested, FindDamagePair, UpdateActivePopupPositions, CountNewerPopupsForSameTarget, CleanupInactivePopups, ResolveWorldPosition, ResolveTargetWorldPosition, TryWorldToCanvasPosition, IsHealthOrInfection, ResolvePairOffset, ResolveIcon, ResolveTextColor, GetPopup, EnsureRefs, ResolveWorldCamera
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 1개: Assets/Prefabs/UI/GamePlay/Canvas_HUD.prefab
- 타입 이름을 참조하는 소스 0개: 

## Assets/Scripts/Presentation/UI/Gameplay/EnemyBarUI/EnemyScreenSpaceHPUIAnchor.cs
- 분류: 일반 소스; 139행
- 타입: EnemyScreenSpaceHPUIAnchor : MonoBehaviour
- 메서드: Reset, OnEnable, Start, OnDisable, SetEnemyStatConfig, SetEnemyShooterConfig, SetVisionVisibilityTarget, GetHealthBarLayout, GetInfectionCastBarLayout, GetWorldUIPosition, ResolveVisionVisibilityTarget
- 직렬화 참조 9개 / 지정 씬 참조 그래프 내 1개: Assets/Prefabs/GameScene/Entity_WIP/Virus/NamedVirus/Mutarus/Mutarus_NamedVirus.prefab; Assets/Prefabs/GameScene/Entity_WIP/Virus/NormalVirus/Chaser/Chaser.prefab; Assets/Prefabs/GameScene/Entity_WIP/Virus/NormalVirus/Chaser/Chaser_Upgrade.prefab; Assets/Prefabs/GameScene/Entity_WIP/Virus/NormalVirus/Roller/Roller.prefab; Assets/Prefabs/GameScene/Entity_WIP/Virus/NormalVirus/Spitter/Spitter_ThreeShot_Virus.prefab; Assets/Prefabs/GameScene/Entity_WIP/Virus/NormalVirus/Spitter/Spitter_Virus.prefab; Assets/Prefabs/GameScene/Entity_WIP/Virus/NormalVirus/Worm/Worm.prefab; Assets/Prefabs/GameScene/Entity_WIP/Virus/NormalVirus/Worm/Worm_Upgrade.prefab
- 타입 이름을 참조하는 소스 7개: Assets/Scripts/Gameplay/Entities/Shooter/Common/Vision/VisionVisibilitySystem.cs; Assets/Scripts/Gameplay/World/Sectors/Named_Boss/NamedSectorController.cs; Assets/Scripts/Gameplay/World/Sectors/SectorEnemySpawner.cs; Assets/Scripts/Presentation/UI/Gameplay/EnemyBarUI/EnemyScreenSpaceHPUIManager.cs; Assets/Scripts/Presentation/UI/Gameplay/EnemyBarUI/HealthBar/EnemyHealthBarWidget.cs; Assets/Scripts/Presentation/UI/Gameplay/EnemyBarUI/InfectionCastBar/EnemyInfectionCastBarWidget.cs; Assets/Scripts/Presentation/UI/Gameplay/EnemyBarUI/ShooterStateIconUI.cs

## Assets/Scripts/Presentation/UI/Gameplay/EnemyBarUI/EnemyScreenSpaceHPUIManager.cs
- 분류: 일반 소스; 216행
- 타입: EnemyScreenSpaceHPUIManager : MonoBehaviour, EnemyUIEntry
- 메서드: Awake, OnEnable, OnDisable, OnDestroy, LateUpdate, OnWorldCameraChanged, Register, Unregister, UpdateWidget, SetEntryVisible, TryGetLocalPoint
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 1개: Assets/Prefabs/UI/GamePlay/Canvas_HUD.prefab
- 타입 이름을 참조하는 소스 1개: Assets/Scripts/Presentation/UI/Gameplay/EnemyBarUI/EnemyScreenSpaceHPUIAnchor.cs

## Assets/Scripts/Presentation/UI/Gameplay/EnemyBarUI/HealthBar/EnemyHealthBarWidget.cs
- 분류: 일반 소스; 661행
- 타입: EnemyHealthBarWidget : MonoBehaviour, text
- 메서드: Awake, Bind, Unbind, TickVisualState, SetScreenPosition, SetManagerVisible, OnHealthChanged, OnPaintMarkChanged, OnInfectionChanged, RefreshImmediate, RefreshInfection, LogInfectionDebug, GetHierarchyPath, RefreshArmorClassText, RefreshPaintMark, ResolvePaintMarkIcon, ResolvePaintMarkController, ResolveStatusEffects, EnsureStatusEffectIcons, ResolveInfection, LogPaintMarkDebug, ForceHiddenUntilPositioned, OnDisable, EnsureFillImageMode, EnsurePaintMarkImageMode, EnsureInfectionImageMode
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 1개: Assets/Prefabs/UI/GamePlay/HUD/EnemyHealthBarWidget.prefab
- 타입 이름을 참조하는 소스 88개: Assets/Editor/ItemMakerWindow.cs; Assets/Editor/LootProgressionEditorWindow.cs; Assets/Editor/StructureLootInventoryRuntimeEditor.cs; Assets/Scripts/Authoring/Editor/DebugMonitorWindow.cs; Assets/Scripts/Authoring/Editor/ReplaceObject_ContextualHierarchy/PrefabSelectionTreeView.cs; Assets/Scripts/Core/SceneFlow/SceneLoader.cs; Assets/Scripts/Gameplay/Entities/Shooter/Common/StateMachine/Action/ShooterAnimatorParameterActionSO.cs; Assets/Scripts/Gameplay/Entities/Shooter/Enemy/Common/EnemyLootInventoryRuntime.cs

## Assets/Scripts/Presentation/UI/Gameplay/EnemyBarUI/InfectionCastBar/EnemyInfectionCastBarWidget.cs
- 분류: 일반 소스; 141행
- 타입: EnemyInfectionCastBarWidget : MonoBehaviour
- 메서드: Awake, Bind, SetScreenPosition, TickVisualState, RefreshImmediate, EnsureFillImageMode, SetManagerVisible
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 0개: 

## Assets/Scripts/Presentation/UI/Gameplay/EnemyBarUI/ShooterStateIconUI.cs
- 분류: 일반 소스; 297행
- 타입: ShooterStateIconUI : MonoBehaviour
- 메서드: Bind, Unbind, Reset, Awake, OnEnable, Update, ResolveRefs, RefreshForGoal, Show, FadeOrHide, HideImmediate
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 1개: Assets/Prefabs/UI/GamePlay/HUD/EnemyHealthBarWidget.prefab
- 타입 이름을 참조하는 소스 1개: Assets/Scripts/Presentation/UI/Gameplay/EnemyBarUI/HealthBar/EnemyHealthBarWidget.cs

## Assets/Scripts/Presentation/UI/Gameplay/EntityStatusEffectWorldGridUI.cs
- 분류: 일반 소스; 359행
- 타입: EntityStatusEffectWorldGridUI : MonoBehaviour, StatusIconView
- 메서드: SetActive, Bind, CreateImage, Awake, OnEnable, OnDisable, LateUpdate, ResolveDependencies, ResolveUIAnchor, CreateFootStatusEffectAnchor, ResolveFootAnchorPosition, EnsureCanvas, RefreshViews, EnsureViewCount, FaceActiveCamera, Subscribe, Unsubscribe, HandleStatusEffectsChanged
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 1개: Assets/Scripts/Gameplay/Entities/Common/StatusEffects/EntityStatusEffectRuntime.cs

## Assets/Scripts/Presentation/UI/Gameplay/GameOver/GameOverUI.cs
- 분류: 일반 소스; 103행
- 타입: GameOverUI : MonoBehaviour
- 메서드: Awake, Start, OnDestroy, HandleLocaleChanged, RefreshGameOverText, OnRetryClicked, OnMainMenuClicked
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 1개: Assets/Prefabs/UI/GamePlay/GameOverPanel.prefab
- 타입 이름을 참조하는 소스 1개: Assets/Scripts/WIP/Legacy/Gameplay/NotUsing/Board/OcuppancyMinuteJudge.cs

## Assets/Scripts/Presentation/UI/Gameplay/Hideout/HideoutStageSelectPanel.cs
- 분류: 일반 소스; 420행
- 타입: HideoutStageSelectPanel : MonoBehaviour, IUIOverlayLifecycle
- 메서드: EnsureInstance, Awake, OnEnable, OnDisable, OnDestroy, Update, OnOverlayShown, OnOverlayHidden, HandleStageSelectRequested, HandleInteractInput, Open, Close, TryEnterStage, ResolveRuntimeReferences, SubscribeInputReader, UnsubscribeInputReader, RefreshStageButtons, BuildRuntimeUi, CreateRect, CreateImage, CreateButton, CreateText, SetFullStretch
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 1개: Assets/Scripts/Gameplay/Progression/Hideout/HideoutStageSelectStation.cs

## Assets/Scripts/Presentation/UI/Gameplay/InfectionControl/InfectionControlHUD.cs
- 분류: 일반 소스; 125행
- 타입: InfectionControlHUD : MonoBehaviour
- 메서드: OnEnable, OnDisable, Update, OnInfectionControlChanged, ApplyTexts, ApplyColor
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 1개: Assets/Prefabs/UI/GamePlay/InfectionControlHUD.prefab
- 타입 이름을 참조하는 소스 0개: 

## Assets/Scripts/Presentation/UI/Gameplay/Interaction/InteractionBarUIManager.cs
- 분류: 일반 소스; 158행
- 타입: InteractionBarUIManager : MonoBehaviour
- 메서드: Awake, OnEnable, OnDisable, LateUpdate, HandleCastChanged, OnWorldCameraChanged, EnsureWidget
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 1개: Assets/Prefabs/UI/GamePlay/Canvas_HUD.prefab
- 타입 이름을 참조하는 소스 0개: 

## Assets/Scripts/Presentation/UI/Gameplay/Interaction/InteractionCastBarWidget.cs
- 분류: 일반 소스; 118행
- 타입: InteractionCastBarWidget : MonoBehaviour
- 메서드: Reset, Awake, SetScreenPosition, SetProgress, SetVisible, EnsureFillImageMode
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 1개: Assets/Prefabs/UI/GamePlay/InteractionCastBarWidget.prefab
- 타입 이름을 참조하는 소스 1개: Assets/Scripts/Presentation/UI/Gameplay/Interaction/InteractionBarUIManager.cs

## Assets/Scripts/Presentation/UI/Gameplay/Interaction/InteractionPromptAnchor.cs
- 분류: 일반 소스; 51행
- 타입: InteractionPromptAnchor : MonoBehaviour
- 메서드: Awake, OnDisable, SetInteractDotVisible
- 직렬화 참조 7개 / 지정 씬 참조 그래프 내 6개: Assets/Prefabs/GameScene/Article/Portal/Portal.prefab; Assets/Prefabs/GameScene/GameItemObject/ItemPickup.prefab; Assets/Prefabs/GameScene/PatternUI/QTE/Mutarus_QTEstation.prefab; Assets/Prefabs/GameScene/Sector/HomeSector.prefab; Assets/Prefabs/GameScene/Sector/SectorObject/ShopMachine.prefab; Assets/Prefabs/GameScene/Sector/Stage1/Sector_Treasure.prefab; Assets/Prefabs/GameScene/Shooter/EnemyShooter/EnemyShooter_Base.prefab
- 타입 이름을 참조하는 소스 2개: Assets/Scripts/Gameplay/Interaction/Core/InteractionManager.cs; Assets/Scripts/Gameplay/Items/World/WorldItemPickupSpawner.cs

## Assets/Scripts/Presentation/UI/Gameplay/Interaction/InteractionPromptScreenSpaceUIManager.cs
- 분류: 일반 소스; 146행
- 타입: InteractionPromptScreenSpaceUIManager : MonoBehaviour
- 메서드: Awake, OnEnable, OnDisable, LateUpdate, HandlePromptChanged, OnWorldCameraChanged, EnsureWidget
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 1개: Assets/Prefabs/UI/GamePlay/Canvas_HUD.prefab
- 타입 이름을 참조하는 소스 0개: 

## Assets/Scripts/Presentation/UI/Gameplay/Interaction/InteractionPromptWidget.cs
- 분류: 일반 소스; 54행
- 타입: InteractionPromptWidget : MonoBehaviour
- 메서드: Reset, Bind, SetScreenPosition, SetVisible
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 1개: Assets/Prefabs/UI/GamePlay/InteractionPromptWidget.prefab
- 타입 이름을 참조하는 소스 1개: Assets/Scripts/Presentation/UI/Gameplay/Interaction/InteractionPromptScreenSpaceUIManager.cs

## Assets/Scripts/Presentation/UI/Gameplay/Loot/LockedLootKeyPanel.cs
- 분류: 일반 소스; 723행
- 타입: LockedLootKeyPanel : MonoBehaviour, IUIOverlayLifecycle, KeyTierRow
- 메서드: EnsureInstance, TryOpen, Awake, OnEnable, OnDisable, OnDestroy, Update, OnOverlayShown, OnOverlayHidden, BeginTierDrag, EndTierDrag, TrySubmitDraggedTier, TrySubmitTier, ConfirmSelectedKey, Open, PlayKeyInsertFeedback, PlayKeySuccessFeedback, PlayKeyRejectedFeedback, ResolvePlayerAudio, Close, HandleInteractInput, ResolveRuntimeReferences, SubscribeInputReader, UnsubscribeInputReader, RefreshView, RefreshSocketText, RefreshConfirmButton, SetFeedback, LocalizeTier, Get, Format, BuildRuntimeUi, CreateRect, CreateImage, CreateButton, CreateText, SetFullStretch, SetAnchors
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 3개: Assets/Scripts/Gameplay/Items/Loot/LockableLootObject.cs; Assets/Scripts/Presentation/UI/Gameplay/Loot/LockedLootKeyPanelDropSlotUI.cs; Assets/Scripts/Presentation/UI/Gameplay/Loot/LockedLootKeyPanelEntryUI.cs

## Assets/Scripts/Presentation/UI/Gameplay/Loot/LockedLootKeyPanelDropSlotUI.cs
- 분류: 일반 소스; 28행
- 타입: LockedLootKeyPanelDropSlotUI : MonoBehaviour, IDropHandler
- 메서드: Configure, OnDrop
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 1개: Assets/Scripts/Presentation/UI/Gameplay/Loot/LockedLootKeyPanel.cs

## Assets/Scripts/Presentation/UI/Gameplay/Loot/LockedLootKeyPanelEntryUI.cs
- 분류: 일반 소스; 56행
- 타입: LockedLootKeyPanelEntryUI : MonoBehaviour,
- 메서드: Configure, OnPointerClick, OnBeginDrag, OnDrag, OnEndDrag
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 1개: Assets/Scripts/Presentation/UI/Gameplay/Loot/LockedLootKeyPanel.cs

## Assets/Scripts/Presentation/UI/Gameplay/Map/FullMap/FullMapCellUI.cs
- 분류: 일반 소스; 148행
- 타입: FullMapCellUI : MonoBehaviour
- 메서드: Reset, Awake, ApplyLayout, SetCell, SetMissing, SetVisible, SetImage, SetBorderVisible, SetText
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 1개: Assets/Prefabs/UI/GamePlay/MAP/FullMapCell.prefab
- 타입 이름을 참조하는 소스 2개: Assets/Scripts/Presentation/UI/Gameplay/Map/FullMap/FullMapUI.cs; Assets/Scripts/Presentation/UI/Gameplay/Map/FullMap/ScriptableObject/FullMapUISettingsSO.cs

## Assets/Scripts/Presentation/UI/Gameplay/Map/FullMap/FullMapUI.cs
- 분류: 일반 소스; 597행
- 타입: FullMapUI : MonoBehaviour
- 메서드: Reset, Awake, OnEnable, OnDisable, SetOpen, SetVisible, ResolveVisibleAlpha, OnAlphaSliderChanged, OnMapSnapshotChanged, RequestSnapshotAndRefresh, RefreshMap, GetOrCreateCell, ApplyCell, CalculateCellPosition, CalculateLayoutRect, CalculateAutoScale, CalculateCenterOffset, TransformBoardPosition, ApplyContentSize, HideAllCells, CalculateBounds, GetCellAlpha, ShouldShowUndiscoveredIcon, FormatRoomLabel, FormatCellObjectName, FormatCoordinateLabel, FormatAlphabetIndex, ApplyOpacityLabelText, HandleLocaleChanged, RequestLocalizationRefresh, RefreshLocalizedTextWhenReady, Localize, SetMiniMapVisible
- 직렬화 참조 2개 / 지정 씬 참조 그래프 내 2개: Assets/Prefabs/UI/GamePlay/Canvas-GamePlay.prefab; Assets/Prefabs/UI/GamePlay/MAP/FullMapRoot.prefab
- 타입 이름을 참조하는 소스 2개: Assets/Scripts/Presentation/UI/Gameplay/Map/FullMap/FullMapCellUI.cs; Assets/Scripts/Presentation/UI/Gameplay/PanelHub/PlayerPanelHub.cs

## Assets/Scripts/Presentation/UI/Gameplay/Map/FullMap/ScriptableObject/FullMapUISettingsSO.cs
- 분류: 일반 소스; 185행
- 타입: FullMapUISettingsSO : ScriptableObject
- 메서드: GetBackgroundSprite, GetRoomIcon
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 1개: Assets/ScriptableObject/GamePlay/UI/Map/FullMapUISettings.asset
- 타입 이름을 참조하는 소스 1개: Assets/Scripts/Presentation/UI/Gameplay/Map/FullMap/FullMapUI.cs

## Assets/Scripts/Presentation/UI/Gameplay/Map/MiniMap/MiniMapCellUI.cs
- 분류: 일반 소스; 222행
- 타입: MiniMapCellUI : MonoBehaviour
- 메서드: Reset, Awake, SetMissing, SetLocked, SetStartSector, SetOpened, SetVisible, SetBackground, SetIcon, ClearIcon, SetRatios, ClearRatios, SetJudgeTime, ClearJudgeTime, SetSpecialTimer, SetRoot
- 직렬화 참조 2개 / 지정 씬 참조 그래프 내 2개: Assets/Prefabs/UI/GamePlay/MAP/MiniMapCell.prefab; Assets/Prefabs/UI/GamePlay/MAP/MiniMapRoot.prefab
- 타입 이름을 참조하는 소스 1개: Assets/Scripts/Presentation/UI/Gameplay/Map/MiniMap/MiniMapUI.cs

## Assets/Scripts/Presentation/UI/Gameplay/Map/MiniMap/MiniMapUI.cs
- 분류: 일반 소스; 714행
- 타입: MiniMapUI : MonoBehaviour, CellSlot, CoordinateLabelSlot
- 메서드: OnEnable, OnDisable, Update, ApplyInitialAlpha, OnAlphaSliderChanged, OnMapSnapshotChanged, CacheSnapshotCells, RefreshCells, ApplyCell, ApplyNamedTimer, RefreshCoordinateLabels, RefreshAxisLabels, HasAnyVisibleCellAtX, HasAnyVisibleCellAtZ, FormatXLabel, FormatZLabel, FormatAlphabetIndex, SetCoordinateLabel, ClearCoordinateLabels, ClearAxisLabels, GetBackgroundColor, GetCellAlpha, GetPlayerColor, GetNeutralColor, GetVirusColor, GetLockedColor, GetStartSectorColor, GetLockedIcon, GetNamedIcon, GetBossIcon, GetIcon, HasNamedOrBoss, ShouldShowJudgeTime, FormatJudgeTime, OnNamedTimerSnapshotChanged, ResolveDisplayCenterCoord, TryGetNamedBattleSourceCoord, ShouldPinMiniMapToNamedSource, InitializeTacticalMap, ResolveLegacyGridRoot, HideLegacyMapElements, HideAxisLabels
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 1개: Assets/Prefabs/UI/GamePlay/MAP/MiniMapRoot.prefab
- 타입 이름을 참조하는 소스 1개: Assets/Scripts/Presentation/UI/Gameplay/PlayerTimer/PlayerTimerHUD.cs

## Assets/Scripts/Presentation/UI/Gameplay/Map/MiniMap/ScriptableObject/MiniMapUISettingsSO.cs
- 분류: 일반 소스; 39행
- 타입: MiniMapUISettingsSO : ScriptableObject
- 메서드: 
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 1개: Assets/ScriptableObject/GamePlay/UI/Map/MiniMapUISettings.asset
- 타입 이름을 참조하는 소스 1개: Assets/Scripts/Presentation/UI/Gameplay/Map/MiniMap/MiniMapUI.cs

## Assets/Scripts/Presentation/UI/Gameplay/Map/MiniMap/SectorTacticalMiniMapGraphic.cs
- 분류: 일반 소스; 478행
- 타입: SectorTacticalMiniMapGraphic : MaskableGraphic
- 메서드: Configure, Tick, OnPopulateMesh, Scan, ResolveViewer, CacheDynamicTargets, CachePortal, DrawVision, DrawClippedFan, DrawStructures, DrawVisibleEnemies, DrawDiscoveredLoot, DrawPlayer, DrawAvailablePortals, WorldToMap, Inset, AddRectOutline, AddDiamond, AddCircle, AddQuad, AddTriangle
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 1개: Assets/Scripts/Presentation/UI/Gameplay/Map/MiniMap/MiniMapUI.cs

## Assets/Scripts/Presentation/UI/Gameplay/MetaSystem/MetaStatsPanelUI.cs
- 분류: 일반 소스; 235행
- 타입: MetaStatsPanelUI : MonoBehaviour, MetaSummaryStatId, MetaSummaryRowDefinition, StatAccumulator
- 메서드: Awake, OnEnable, OnDisable, RefreshRows, HandleMetaProgressChanged, BuildRows, RebuildAccumulators, ResolveValue, ApplyModifier, Resolve, FormatNumber, Apply
- 직렬화 참조 2개 / 지정 씬 참조 그래프 내 2개: Assets/Prefabs/UI/MainMenu/Canvas-MainMenu.prefab; Assets/Prefabs/UI/MainMenu/METAStatsPanel.prefab
- 타입 이름을 참조하는 소스 4개: Assets/Scripts/Gameplay/Entities/Shooter/Common/Core/ShooterStatsRuntime.cs; Assets/Scripts/Gameplay/Entities/Shooter/Enemy/Common/EnemyShooterStatsRuntime.cs; Assets/Scripts/Gameplay/Entities/Shooter/Player/PlayerStatsRuntime.cs; Assets/Scripts/Presentation/UI/Gameplay/MetaSystem/MetaSystemPanelUI.cs

## Assets/Scripts/Presentation/UI/Gameplay/MetaSystem/MetaSystemPanelUI.cs
- 분류: 일반 소스; 54행
- 타입: MetaSystemPanelUI : MonoBehaviour
- 메서드: OnEnable, OnDisable, OpenMetaSystemScreen, Close
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 1개: Assets/Prefabs/UI/MainMenu/Canvas-MainMenu.prefab
- 타입 이름을 참조하는 소스 1개: Assets/Scripts/Presentation/UI/Common/UIMenuManager.cs

## Assets/Scripts/Presentation/UI/Gameplay/MetaSystem/MetaUpgradePanelUI.cs
- 분류: 일반 소스; 123행
- 타입: MetaUpgradePanelUI : PanelUI
- 메서드: OnEnable, OnDisable, OpenPanel, ClosePanel, HandleBackClicked, HandleProgressChanged, EnsureRowCount
- 직렬화 참조 2개 / 지정 씬 참조 그래프 내 2개: Assets/Prefabs/UI/MainMenu/Canvas-MainMenu.prefab; Assets/Prefabs/UI/MainMenu/METAUpgrade/METAUpgradePanel.prefab
- 타입 이름을 참조하는 소스 1개: Assets/Scripts/Presentation/UI/Gameplay/MetaSystem/MetaSystemPanelUI.cs

## Assets/Scripts/Presentation/UI/Gameplay/MetaSystem/MetaUpgradeRowUI.cs
- 분류: 일반 소스; 80행
- 타입: MetaUpgradeRowUI : MonoBehaviour
- 메서드: OnEnable, OnDisable, Bind, HandlePurchaseClicked, SetText
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 1개: Assets/Prefabs/UI/MainMenu/METAUpgrade/METAUpgradeRow.prefab
- 타입 이름을 참조하는 소스 1개: Assets/Scripts/Presentation/UI/Gameplay/MetaSystem/MetaUpgradePanelUI.cs

## Assets/Scripts/Presentation/UI/Gameplay/Overlay/GameplayAttackInputBlocker.cs
- 분류: 일반 소스; 52행
- 타입: GameplayActionInputBlocker, GameplayAttackInputBlocker
- 메서드: SetBlocked, Clear
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 7개: Assets/Scripts/Gameplay/Entities/Shooter/Common/Core/ShooterThrowableAttack.cs; Assets/Scripts/Gameplay/Entities/Shooter/Player/PlayerAimAction.cs; Assets/Scripts/Gameplay/Entities/Shooter/Player/PlayerAimController.cs; Assets/Scripts/Gameplay/Entities/Shooter/Player/PlayerClickController.cs; Assets/Scripts/Gameplay/Entities/Shooter/Player/PlayerShooterDashController.cs; Assets/Scripts/Gameplay/Entities/Shooter/Player/PlayerShooterShoot.cs; Assets/Scripts/Presentation/UI/Gameplay/Overlay/UIOverlayManager.cs

## Assets/Scripts/Presentation/UI/Gameplay/Overlay/IUIOverlayLifecycle.cs
- 분류: 일반 소스; 12행
- 타입: IUIOverlayLifecycle
- 메서드: 
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 5개: Assets/Scripts/Presentation/UI/Common/PanelUI.cs; Assets/Scripts/Presentation/UI/Gameplay/Hideout/HideoutStageSelectPanel.cs; Assets/Scripts/Presentation/UI/Gameplay/Loot/LockedLootKeyPanel.cs; Assets/Scripts/Presentation/UI/Gameplay/Overlay/UIOverlayPanel.cs; Assets/Scripts/Presentation/UI/Gameplay/PanelHub/ContextualHubOverlayLifecycle.cs

## Assets/Scripts/Presentation/UI/Gameplay/Overlay/UIOverlayId.cs
- 분류: 일반 소스; 21행
- 타입: UIOverlayId
- 메서드: 
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 10개: Assets/Scripts/Core/Events/ScriptableObject/PanelHub/UIOverlayRequestEventChannelSO.cs; Assets/Scripts/Gameplay/Entities/Shooter/Enemy/Common/EnemyLootInventoryRuntime.cs; Assets/Scripts/Gameplay/World/Sectors/Shop/SectorShop.cs; Assets/Scripts/Presentation/UI/Gameplay/Hideout/HideoutStageSelectPanel.cs; Assets/Scripts/Presentation/UI/Gameplay/Loot/LockedLootKeyPanel.cs; Assets/Scripts/Presentation/UI/Gameplay/Overlay/UIOverlayManager.cs; Assets/Scripts/Presentation/UI/Gameplay/Overlay/UIOverlayPanel.cs; Assets/Scripts/Presentation/UI/Gameplay/Overlay/UIOverlayRequestEmitter.cs

## Assets/Scripts/Presentation/UI/Gameplay/Overlay/UIOverlayManager.cs
- 분류: 일반 소스; 346행
- 타입: UIOverlayManager : MonoBehaviour
- 메서드: Awake, OnEnable, OnDisable, Open, Close, Toggle, CloseAll, RestoreInputForOpenOverlays, RegisterPanel, CachePanels, RegisterPanels, HandleOverlayRequest, CloseAllInternal, PlayOverlayAudio, ShouldBlockCombatInput, RefreshOpenOverlayState, RefreshPanelSorting, ApplyGameplayInputMode, RestoreGameplayInputIfNeeded
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 1개: Assets/Scenes/Manager/GamePlay.unity
- 타입 이름을 참조하는 소스 5개: Assets/Scripts/Gameplay/Entities/Shooter/Player/Inventory/PlayerBackpackDropInput.cs; Assets/Scripts/Presentation/UI/Common/ESCMenu/EscPauseMenuController.cs; Assets/Scripts/Presentation/UI/Gameplay/Hideout/HideoutStageSelectPanel.cs; Assets/Scripts/Presentation/UI/Gameplay/Loot/LockedLootKeyPanel.cs; Assets/Scripts/Presentation/UI/Gameplay/PanelHub/PlayerPanelHub.cs

## Assets/Scripts/Presentation/UI/Gameplay/Overlay/UIOverlayPanel.cs
- 분류: 일반 소스; 166행
- 타입: UIOverlayPanel : MonoBehaviour
- 메서드: ConfigureRuntime, Reset, Awake, Show, Hide, SetSortingOrder, EnsureCanvasGroup, EnsureSortingCanvas, SetVisible, CacheLifecycleTargets, NotifyShown, NotifyHidden
- 직렬화 참조 5개 / 지정 씬 참조 그래프 내 5개: Assets/Prefabs/UI/GamePlay/BossRewardChoicePanel.prefab; Assets/Prefabs/UI/GamePlay/Canvas-GamePlay.prefab; Assets/Prefabs/UI/GamePlay/Settings/SettingsPanel.prefab; Assets/Prefabs/UI/MainMenu/CreditPanel.prefab; Assets/Scenes/Manager/GamePlay.unity
- 타입 이름을 참조하는 소스 4개: Assets/Scripts/Presentation/UI/Gameplay/Hideout/HideoutStageSelectPanel.cs; Assets/Scripts/Presentation/UI/Gameplay/Loot/LockedLootKeyPanel.cs; Assets/Scripts/Presentation/UI/Gameplay/Overlay/UIOverlayManager.cs; Assets/Scripts/Presentation/UI/Gameplay/PanelHub/PlayerPanelHub.cs

## Assets/Scripts/Presentation/UI/Gameplay/Overlay/UIOverlayRequestEmitter.cs
- 분류: 일반 소스; 53행
- 타입: UIOverlayRequestEmitter : MonoBehaviour
- 메서드: Raise, Open, Close, Toggle, CloseAll
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 0개: 

## Assets/Scripts/Presentation/UI/Gameplay/PanelHub/CollapsibleCanvasPanelUI.cs
- 분류: 일반 소스; 96행
- 타입: CollapsibleCanvasPanelUI : MonoBehaviour
- 메서드: Awake, OnEnable, OnDisable, ResetToDefault, Toggle, SetExpanded, ApplyExpanded
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 0개: 

## Assets/Scripts/Presentation/UI/Gameplay/PanelHub/ContextualHubOverlayLifecycle.cs
- 분류: 일반 소스; 34행
- 타입: ContextualHubOverlayLifecycle : MonoBehaviour, IUIOverlayLifecycle
- 메서드: Configure, OnOverlayShown, OnOverlayHidden
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 1개: Assets/Prefabs/UI/GamePlay/Canvas-GamePlay.prefab
- 타입 이름을 참조하는 소스 1개: Assets/Scripts/Presentation/UI/Gameplay/PanelHub/PlayerPanelHub.cs

## Assets/Scripts/Presentation/UI/Gameplay/PanelHub/Inventory/EnemyInventoryUI.cs
- 분류: 일반 소스; 2219행
- 타입: EnemyInventoryUI : InventoryPanelUIBase,
- 메서드: Awake, OnValidate, OnEnable, OnDisable, OnPanelHidden, CloseContextMenu, HandleItemTooltipEntered, HandleItemTooltipExited, HandleInventorySlotLeftClicked, HandleInventorySlotRightClicked, HandleInventorySlotDroppedOnInventory, HandleInventorySlotDroppedOnEquipment, HandleInventorySlotDroppedOnAmmo, HandleEquipmentSlotLeftClicked, HandleEquipmentSlotDroppedOnInventory, HandleEquipmentSlotRightClicked, HandleWeaponSlotRightClicked, HandleAmmoSlotDroppedOnInventory, HandleAmmoSlotDroppedOnAmmo, HandleAmmoSlotLeftClicked, HandleAmmoSlotRightClicked, HandleContextMenuEquipInventoryItem, HandleContextMenuSplitInventoryItem, HandleContextMenuDiscardInventoryItem, HandleContextMenuUnequipEquipmentSlot, HandleContextMenuDiscardEquipmentSlot, HandleContextMenuUnequipAmmoSlot, HandleContextMenuSplitAmmoSlot, HandleContextMenuDiscardAmmoSlot, TryTakeWeapon, TryTakeArmor, TryTakeAmmo, TryTakeItem, TryTakeBullet, TryTakeSlotToPlayerInventorySlot, TryTakeSlotToPlayerEquipmentSlot, TryTakeAmmoToPlayerAmmoSlot, TryReceiveDragPayloadToEnemyInventorySlot, TryReceiveDragPayloadToEnemyEquipmentSlot, TryReceiveDragPayloadToEnemyAmmoSlot, ClosePanel, HandleOpenRequested, HandlePlayerInventoryRuntimeReady, RefreshAfterTake, RefreshDragSources, Refresh, ApplyContextPresentation, ConfigureEquipmentRow, FindChildRect, Stretch, AnchorTop, RefreshHeader, RefreshEquippedSlots, RefreshAmmoSlots, RefreshItemSlots, ResolveAmmoStack, ResolveItemStack, ResolveRawItemStack, TryPeekCurrentDragPayload, TryRemoveCurrentDragPayload, TryPeekPlayerInventoryPayload, TryPeekPlayerEquipmentPayload, TryPeekPlayerAmmoPayload, TryPeekEnemySlotPayload, TryRemoveEnemySlotPayload, CanEquipPayloadToEnemyEquipmentSlot, ResolveEnemyEquipmentSlotPayload, CanStoreStackInEnemyInventory, TryPlaceStackInFirstEnemyInventorySlot, CanPlaceStackInEnemyInventorySlot, TryPlaceStackInEnemyInventorySlot, ResolveEnemyItemIndex, EncodeWeaponSlotIndex, TryDecodeWeaponSlotIndex, IsCurrentEnemyInventorySource, IsCurrentEnemyEquipmentSource, IsCurrentEnemyAmmoSource, ResolveCurrentEnemyInventorySourceFreeIndex, ResolvePreferredEquipmentSlotKind, InitializeSlots, InitializeItemSlots, EnsureItemSlots, RebuildInventoryLayout, ResetInventoryScrollToTop, HideSlotTemplateIfNeeded, ResolveRefs, GetOrCreateTooltip, DestroyTooltip, RaiseMessage, Fail, TrySpawnWorldDrop
- 직렬화 참조 2개 / 지정 씬 참조 그래프 내 2개: Assets/Prefabs/UI/GamePlay/Canvas-GamePlay.prefab; Assets/Prefabs/UI/GamePlay/PanelHub/Inventory/EnemyInventoryPanel.prefab
- 타입 이름을 참조하는 소스 7개: Assets/Scripts/Presentation/UI/Gameplay/PanelHub/Inventory/EquipmentInventoryAmmoSlotUI.cs; Assets/Scripts/Presentation/UI/Gameplay/PanelHub/Inventory/EquipmentInventoryArmorSlotUI.cs; Assets/Scripts/Presentation/UI/Gameplay/PanelHub/Inventory/EquipmentInventorySlotUI.cs; Assets/Scripts/Presentation/UI/Gameplay/PanelHub/Inventory/EquipmentInventoryWeaponSlotUI.cs; Assets/Scripts/Presentation/UI/Gameplay/PanelHub/Inventory/InventoryItemSlotUI.cs; Assets/Scripts/Presentation/UI/Gameplay/PanelHub/Inventory/PlayerInventoryDragContext.cs; Assets/Scripts/Presentation/UI/Gameplay/PanelHub/PlayerPanelHub.cs

## Assets/Scripts/Presentation/UI/Gameplay/PanelHub/Inventory/EquipmentInventoryAmmoSlotUI.cs
- 분류: 일반 소스; 259행
- 타입: EquipmentInventoryAmmoSlotUI : EquipmentInventorySlotUI
- 메서드: InitializeQuickSlot, BindQuickSlot, Initialize, Bind, HandleRightClick, HandleLeftClick, HandleDoubleLeftClick, BeginDragPayload, HandleDropPayload, SetAmmoText
- 직렬화 참조 3개 / 지정 씬 참조 그래프 내 3개: Assets/Prefabs/UI/GamePlay/PanelHub/Inventory/EnemyInventoryPanel.prefab; Assets/Prefabs/UI/GamePlay/PanelHub/Inventory/PlayerInventoryPanel.prefab; Assets/Prefabs/UI/GamePlay/Slot/InventorySlotAmmo.prefab
- 타입 이름을 참조하는 소스 2개: Assets/Scripts/Presentation/UI/Gameplay/PanelHub/Inventory/EnemyInventoryUI.cs; Assets/Scripts/Presentation/UI/Gameplay/PanelHub/Inventory/PlayerInventoryUI.cs

## Assets/Scripts/Presentation/UI/Gameplay/PanelHub/Inventory/EquipmentInventoryArmorSlotUI.cs
- 분류: 일반 소스; 177행
- 타입: EquipmentInventoryArmorSlotUI : EquipmentInventorySlotUI
- 메서드: Bind, OnBound, PublishItemDetailSelection, HandleRightClick, HandleLeftClick, HandleDoubleLeftClick, BeginDragPayload, HandleDropPayload, SetDurabilityText
- 직렬화 참조 3개 / 지정 씬 참조 그래프 내 3개: Assets/Prefabs/UI/GamePlay/PanelHub/Inventory/EnemyInventoryPanel.prefab; Assets/Prefabs/UI/GamePlay/PanelHub/Inventory/PlayerInventoryPanel.prefab; Assets/Prefabs/UI/GamePlay/Slot/InventorySlotArmor.prefab
- 타입 이름을 참조하는 소스 2개: Assets/Scripts/Presentation/UI/Gameplay/PanelHub/Inventory/EnemyInventoryUI.cs; Assets/Scripts/Presentation/UI/Gameplay/PanelHub/Inventory/PlayerInventoryUI.cs

## Assets/Scripts/Presentation/UI/Gameplay/PanelHub/Inventory/EquipmentInventoryGasMaskSlotUI.cs
- 분류: 일반 소스; 118행
- 타입: EquipmentInventoryGasMaskSlotUI : EquipmentInventorySlotUI
- 메서드: Awake, Bind, OnBound, PublishItemDetailSelection, HandleLeftClick, HandleDoubleLeftClick, HandleRightClick, BeginDragPayload, HandleDropPayload, SetFilterText
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 1개: Assets/Prefabs/UI/GamePlay/PanelHub/Inventory/PlayerInventoryPanel.prefab
- 타입 이름을 참조하는 소스 1개: Assets/Scripts/Presentation/UI/Gameplay/PanelHub/Inventory/PlayerInventoryUI.cs

## Assets/Scripts/Presentation/UI/Gameplay/PanelHub/Inventory/EquipmentInventorySlotUI.cs
- 분류: 일반 소스; 442행
- 타입: EquipmentInventorySlotUI : MonoBehaviour,, RuntimeVisualBindings
- 메서드: Awake, CaptureRuntimeVisualBindings, ApplyRuntimeVisualBindings, OnEnable, OnDisable, Initialize, Bind, OnPointerClick, OnPointerEnter, OnPointerExit, OnBeginDrag, OnEndDrag, OnDrag, OnDrop, OnBound, PublishItemDetailSelection, HandleLeftClick, HandleDoubleLeftClick, HandleRightClick, BeginDragPayload, HandleDropPayload, SetRuntimeSlotTypeOverride, SetSlotTypeTextVisible, SetDisplayNameTextVisible, SetIconImage, TryBeginEnemyInventoryDrag, EnsureRaycastTargets, RefreshSlotTypeName, HandleSlotTypeNameChanged, BindDisplayName, UnbindDisplayName, HandleDisplayNameChanged, ShowDragGhost
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 5개: Assets/Scripts/Presentation/UI/Gameplay/PanelHub/Inventory/EquipmentInventoryAmmoSlotUI.cs; Assets/Scripts/Presentation/UI/Gameplay/PanelHub/Inventory/EquipmentInventoryArmorSlotUI.cs; Assets/Scripts/Presentation/UI/Gameplay/PanelHub/Inventory/EquipmentInventoryGasMaskSlotUI.cs; Assets/Scripts/Presentation/UI/Gameplay/PanelHub/Inventory/EquipmentInventoryWeaponSlotUI.cs; Assets/Scripts/Presentation/UI/Gameplay/PanelHub/Inventory/PassiveInventorySlotUI.cs

## Assets/Scripts/Presentation/UI/Gameplay/PanelHub/Inventory/EquipmentInventoryWeaponSlotUI.cs
- 분류: 일반 소스; 297행
- 타입: EquipmentInventoryWeaponSlotUI : EquipmentInventorySlotUI
- 메서드: Initialize, InitializeQuickSlot, BindQuickSlot, Bind, OnBound, ResolveQuickSlotSupplementalText, PublishItemDetailSelection, EnsureDurabilityText, HandleRightClick, HandleLeftClick, HandleDoubleLeftClick, BeginDragPayload, HandleDropPayload
- 직렬화 참조 3개 / 지정 씬 참조 그래프 내 3개: Assets/Prefabs/UI/GamePlay/PanelHub/Inventory/EnemyInventoryPanel.prefab; Assets/Prefabs/UI/GamePlay/PanelHub/Inventory/PlayerInventoryPanel.prefab; Assets/Prefabs/UI/GamePlay/Slot/InventorySlotWeapon.prefab
- 타입 이름을 참조하는 소스 2개: Assets/Scripts/Presentation/UI/Gameplay/PanelHub/Inventory/EnemyInventoryUI.cs; Assets/Scripts/Presentation/UI/Gameplay/PanelHub/Inventory/PlayerInventoryUI.cs

## Assets/Scripts/Presentation/UI/Gameplay/PanelHub/Inventory/InventoryDragGhostUI.cs
- 분류: 일반 소스; 235행
- 타입: InventoryDragGhostUI : MonoBehaviour
- 메서드: Awake, OnDisable, Show, Move, Hide, ResolveInstance, ResolveCanvas, ShowInternal, MoveInternal, SetVisible, EnsureRefs
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 3개: Assets/Scripts/Presentation/UI/Gameplay/PanelHub/Inventory/EquipmentInventorySlotUI.cs; Assets/Scripts/Presentation/UI/Gameplay/PanelHub/Inventory/InventoryItemSlotUI.cs; Assets/Scripts/Presentation/UI/Gameplay/PanelHub/Inventory/PlayerInventoryDragContext.cs

## Assets/Scripts/Presentation/UI/Gameplay/PanelHub/Inventory/InventoryItemContextMenuUI.cs
- 분류: 일반 소스; 1284행
- 타입: InventoryContextMenuTargetKind, InventoryContextMenuActionKind, InventoryContextMenuActionDefinition, InventoryItemContextMenuUI : MonoBehaviour, LocalizedButtonBinding
- 메서드: ResolveLabel, Release, Reset, Awake, OnEnable, OnDisable, Update, Initialize, ShowInventoryItem, ShowEquipmentSlot, ShowAmmoSlot, Hide, ShowInternal, RebuildButtons, CanRunAction, ExecuteAction, HasSplitAmountPanel, ExecuteDefaultSplit, RequestDiscardConfirmation, ExecuteConfirmedDiscard, HandleDiscardConfirmYesClicked, HandleDiscardConfirmNoClicked, HideDiscardConfirmPanel, ResolveDiscardConfirmFallbackLabel, BindDiscardConfirmLabel, UnbindDiscardConfirmLabel, HandleDiscardConfirmLabelChanged, ShowSplitPanel, HideSplitPanel, ConfigureSplitSlider, HandleSplitSliderChanged, HandleSplitInputChanged, SetSplitAmount, RefreshSplitAmountViews, HandleSplitConfirmClicked, BindButtonLabel, ResolveButtonRoot, ClearButtons, ClearLocalizedButtonBindings, SetScreenPosition, BringOverlayToFront, EnsureOverlayInputLayer, IsPointerInsideMenuRect, EnsureSplitPanelInputLayer, ClampRootInsideParent, SetVisible, IsVisible, TryGetDismissPointerDown, IsPointerOverMenu, IsMenuTransform, SetCanvasGroup, EnsureRefs, EnsureSplitPanelRefs, EnsureDiscardConfirmRefs, FindNamedChild, HideButtonPrefabIfNeeded
- 직렬화 참조 2개 / 지정 씬 참조 그래프 내 2개: Assets/Prefabs/UI/GamePlay/PanelHub/Inventory/EnemyInventoryPanel.prefab; Assets/Prefabs/UI/GamePlay/PanelHub/Inventory/PlayerInventoryPanel.prefab
- 타입 이름을 참조하는 소스 2개: Assets/Scripts/Presentation/UI/Gameplay/PanelHub/Inventory/EnemyInventoryUI.cs; Assets/Scripts/Presentation/UI/Gameplay/PanelHub/Inventory/PlayerInventoryUI.cs

## Assets/Scripts/Presentation/UI/Gameplay/PanelHub/Inventory/InventoryItemSlotUI.cs
- 분류: 일반 소스; 426행
- 타입: InventoryItemSlotUI : MonoBehaviour,
- 메서드: Awake, OnEnable, OnDisable, Initialize, Bind, BindItem, BindKeyToolItem, OnPointerClick, OnBeginDrag, OnPointerEnter, OnPointerExit, OnEndDrag, OnDrag, OnDrop, RefreshItemInfoText, SetDurabilityInfo, EnsureRaycastTargets, BindDisplayName, UnbindDisplayName, HandleDisplayNameChanged, ShowDragGhost
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 1개: Assets/Prefabs/UI/GamePlay/PanelHub/Inventory/InventorySlot.prefab
- 타입 이름을 참조하는 소스 3개: Assets/Scripts/Presentation/UI/Gameplay/PanelHub/Inventory/EnemyInventoryUI.cs; Assets/Scripts/Presentation/UI/Gameplay/PanelHub/Inventory/KeyToolContainerPanel.cs; Assets/Scripts/Presentation/UI/Gameplay/PanelHub/Inventory/PlayerInventoryUI.cs

## Assets/Scripts/Presentation/UI/Gameplay/PanelHub/Inventory/InventoryPanelUIBase.cs
- 분류: 일반 소스; 13행
- 타입: InventoryPanelUIBase : PanelUI
- 메서드: 
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 1개: Assets/Scripts/Presentation/UI/Gameplay/PanelHub/Inventory/EnemyInventoryUI.cs

## Assets/Scripts/Presentation/UI/Gameplay/PanelHub/Inventory/InventoryUIContracts.cs
- 분류: 일반 소스; 83행
- 타입: IInventorySlotOwner, IInventoryItemContextMenuOwner, IConsumableInventoryContextMenuOwner, InventoryItemDisplayUtility
- 메서드: TryGetItemNameReference, ResolveItemName
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 20개: Assets/Scripts/Authoring/Editor/DebugMonitorWindow.Operations.cs; Assets/Scripts/Gameplay/Entities/Shooter/Enemy/Common/EnemyLootInventoryRuntime.cs; Assets/Scripts/Gameplay/Entities/Shooter/Player/Inventory/PlayerInventoryRuntime.cs; Assets/Scripts/Gameplay/Entities/Shooter/Player/Passive/PlayerPassiveInventoryRuntime.cs; Assets/Scripts/Gameplay/Items/World/WorldItemPickup.cs; Assets/Scripts/Presentation/UI/Gameplay/PanelHub/Inventory/EnemyInventoryUI.cs; Assets/Scripts/Presentation/UI/Gameplay/PanelHub/Inventory/EquipmentInventoryAmmoSlotUI.cs; Assets/Scripts/Presentation/UI/Gameplay/PanelHub/Inventory/EquipmentInventorySlotUI.cs

## Assets/Scripts/Presentation/UI/Gameplay/PanelHub/Inventory/ItemDetailSelectionBus.cs
- 분류: 일반 소스; 95행
- 타입: ItemDetailSelection, ItemDetailSelectionBus
- 메서드: Publish, Clear
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 8개: Assets/Scripts/Presentation/UI/Gameplay/PanelHub/Inventory/EquipmentInventoryArmorSlotUI.cs; Assets/Scripts/Presentation/UI/Gameplay/PanelHub/Inventory/EquipmentInventoryGasMaskSlotUI.cs; Assets/Scripts/Presentation/UI/Gameplay/PanelHub/Inventory/EquipmentInventorySlotUI.cs; Assets/Scripts/Presentation/UI/Gameplay/PanelHub/Inventory/EquipmentInventoryWeaponSlotUI.cs; Assets/Scripts/Presentation/UI/Gameplay/PanelHub/Inventory/InventoryItemSlotUI.cs; Assets/Scripts/Presentation/UI/Gameplay/PanelHub/Inventory/PlayerItemDetailPanelUI.cs; Assets/Scripts/Presentation/UI/Gameplay/PanelHub/PlayerPanelHub.cs; Assets/Scripts/Presentation/UI/Gameplay/Shop/PlayerShopItemUI.cs

## Assets/Scripts/Presentation/UI/Gameplay/PanelHub/Inventory/KeyToolContainerPanel.cs
- 분류: 일반 소스; 419행
- 타입: KeyToolContainerPanel : MonoBehaviour, IInventorySlotOwner
- 메서드: TryOpen, Update, OnDestroy, Open, Close, Refresh, EnsureSlots, HandleInventorySlotLeftClicked, HandleInventorySlotRightClicked, HandleInventorySlotDroppedOnInventory, HandleInventorySlotDroppedOnEquipment, HandleEquipmentSlotDroppedOnInventory, HandleInventorySlotDroppedOnAmmo, HandleAmmoSlotDroppedOnInventory, HandleAmmoSlotDroppedOnAmmo, HandleEquipmentSlotLeftClicked, HandleEquipmentSlotRightClicked, HandleAmmoSlotLeftClicked, HandleAmmoSlotRightClicked, HandleItemTooltipEntered, HandleItemTooltipExited, BuildRuntimeUi, SetVisible, SetFeedback, UnsubscribeInventory, CreateRect, CreateImage, CreateButton, CreateText, SetFullStretch, SetAnchors
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 1개: Assets/Scripts/Presentation/UI/Gameplay/PanelHub/Inventory/PlayerInventoryUI.cs

## Assets/Scripts/Presentation/UI/Gameplay/PanelHub/Inventory/PassiveInventorySlotUI.cs
- 분류: 일반 소스; 114행
- 타입: PassiveInventorySlotUI : EquipmentInventorySlotUI
- 메서드: Awake, InitializePassiveSlot, BindPassive, HandleLeftClick, HandleRightClick, HandleDropPayload, SetVisible, ResolveInheritedIconImage
- 직렬화 참조 2개 / 지정 씬 참조 그래프 내 2개: Assets/Prefabs/UI/GamePlay/PanelHub/Inventory/PlayerInventoryPanel.prefab; Assets/Prefabs/UI/GamePlay/Slot/PassiveSlot.prefab
- 타입 이름을 참조하는 소스 1개: Assets/Scripts/Presentation/UI/Gameplay/PanelHub/Inventory/PlayerInventoryUI.cs

## Assets/Scripts/Presentation/UI/Gameplay/PanelHub/Inventory/PlayerInventoryDragContext.cs
- 분류: 일반 소스; 186행
- 타입: InventoryDragSourceKind, InventoryDragContext, PlayerInventoryDragSourceKind, PlayerInventoryDragContext
- 메서드: BeginInventoryDrag, BeginEquipmentDrag, BeginEnemyInventoryDrag, BeginEquipmentAmmoDrag, BeginQuickSlotDrag, Clear
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 16개: Assets/Scripts/Presentation/UI/Gameplay/PanelHub/Inventory/EnemyInventoryUI.cs; Assets/Scripts/Presentation/UI/Gameplay/PanelHub/Inventory/EquipmentInventoryAmmoSlotUI.cs; Assets/Scripts/Presentation/UI/Gameplay/PanelHub/Inventory/EquipmentInventoryArmorSlotUI.cs; Assets/Scripts/Presentation/UI/Gameplay/PanelHub/Inventory/EquipmentInventoryGasMaskSlotUI.cs; Assets/Scripts/Presentation/UI/Gameplay/PanelHub/Inventory/EquipmentInventorySlotUI.cs; Assets/Scripts/Presentation/UI/Gameplay/PanelHub/Inventory/EquipmentInventoryWeaponSlotUI.cs; Assets/Scripts/Presentation/UI/Gameplay/PanelHub/Inventory/InventoryItemSlotUI.cs; Assets/Scripts/Presentation/UI/Gameplay/PanelHub/Inventory/KeyToolContainerPanel.cs

## Assets/Scripts/Presentation/UI/Gameplay/PanelHub/Inventory/PlayerInventoryItemSelection.cs
- 분류: 일반 소스; 91행
- 타입: PlayerInventoryItemSelectionKind, PlayerInventoryItemSelection
- 메서드: 
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 2개: Assets/Scripts/Presentation/UI/Gameplay/PanelHub/Inventory/PlayerInventoryUI.cs; Assets/Scripts/Presentation/UI/Gameplay/Shop/PlayerShopPanelUI.cs

## Assets/Scripts/Presentation/UI/Gameplay/PanelHub/Inventory/PlayerInventoryUI.cs
- 분류: 일반 소스; 2369행
- 타입: PlayerInventoryInteractionMode, PlayerInventoryUI : MonoBehaviour,
- 메서드: Reset, Awake, OnValidate, ApplyEditorPreview, OnEnable, OnDisable, HandleInventorySlotLeftClicked, HandleItemTooltipEntered, HandleItemTooltipExited, HandleInventorySlotRightClicked, HandleContextMenuEquipInventoryItem, HandleContextMenuUseInventoryItem, HandleContextMenuDiscardInventoryItem, HandleContextMenuSplitInventoryItem, HandleContextMenuUnequipEquipmentSlot, HandleContextMenuDiscardEquipmentSlot, HandleContextMenuUnequipAmmoSlot, HandleContextMenuDiscardAmmoSlot, HandleContextMenuSplitAmmoSlot, HandleEquipmentSlotRightClicked, HandleWeaponSlotRightClicked, HandleInventorySlotDroppedOnInventory, HandleInventorySlotDroppedOnEquipment, HandleInventorySlotDroppedOnWeaponSlot, HandleWeaponSlotDroppedOnWeaponSlot, HandleEquipmentSlotLeftClicked, HandleWeaponSlotLeftClicked, HandleEquipmentSlotDroppedOnInventory, HandleInventorySlotDroppedOnAmmo, HandleInventorySlotDroppedOnPassive, HandlePassiveSlotLeftClicked, HandlePassiveSlotRightClicked, HandleAmmoSlotDroppedOnInventory, HandleAmmoSlotDroppedOnAmmo, HandleAmmoSlotLeftClicked, HandleAmmoSlotRightClicked, HandleQuickSlotLeftClicked, HandleQuickSlotRightClicked, HandleQuickSlotDoubleClicked, HandleQuickSlotDroppedOnInventory, HandleQuickSlotDrop, HandleEquipmentSlotDoubleClicked, HandleAmmoSlotDoubleClicked, Refresh, SetInteractionMode, TryGetSelectedInventoryStack, TryGetSelectedItemSelection, ClearSelectedInventorySlot, CloseContextMenu, ClearSelectedItemSelection, ResolveItemName, InitializeStaticSlots, InitializeEquipmentSlots, RefreshEquipmentSlots, EnsurePassiveSlots, RefreshPassiveSlots, ApplyCompactPanelLayout, FindDirectChildRect, StretchRect, AnchorTopRect, ConfigureEquipmentRow, EnsureInventorySlots, EnsureSeparatedInventorySectionLayout, RefreshSortButtons, SortPlayerInventory, DisableObsoleteRuntimeStorageObjects, CreateOrFindStorageSectionHeader, FindOrCreatePocketSlotGrid, ConfigureStorageGrid, MoveSlotToStorageSection, RefreshInventorySectionLayout, ConfigureInventoryScrollView, GetStorageGridHeight, GetInventorySlotName, ConfigureInventorySlots, ConfigureInventorySlot, HideSlotTemplateIfNeeded, EnsureHeadSlot, ResolveRefs, AreAllWeaponSlotsMissing, TryGetInventoryStack, AreAllAmmoSlotsMissing, SelectInventorySlot, SelectEquipmentSlot, SelectWeaponSlot, SelectAmmoSlot, CreateInventorySelection, ResolveSelectedStack, ValidateSelection, HandleInventoryRuntimeReady, ClearBoundSlots, SubscribePassiveRuntime, UnsubscribePassiveRuntime, RaiseMessage, HideContextMenu, GetOrCreateTooltip, DestroyTooltip, TrySpawnWorldDrop, CompleteWorldDropOrRestore
- 직렬화 참조 2개 / 지정 씬 참조 그래프 내 2개: Assets/Prefabs/UI/GamePlay/Canvas-GamePlay.prefab; Assets/Prefabs/UI/GamePlay/PanelHub/Inventory/PlayerInventoryPanel.prefab
- 타입 이름을 참조하는 소스 12개: Assets/Editor/InventorySortButtonMigration.cs; Assets/Scripts/Presentation/UI/Gameplay/PanelHub/Inventory/EquipmentInventoryAmmoSlotUI.cs; Assets/Scripts/Presentation/UI/Gameplay/PanelHub/Inventory/EquipmentInventoryArmorSlotUI.cs; Assets/Scripts/Presentation/UI/Gameplay/PanelHub/Inventory/EquipmentInventoryGasMaskSlotUI.cs; Assets/Scripts/Presentation/UI/Gameplay/PanelHub/Inventory/EquipmentInventoryWeaponSlotUI.cs; Assets/Scripts/Presentation/UI/Gameplay/PanelHub/Inventory/InventoryItemSlotUI.cs; Assets/Scripts/Presentation/UI/Gameplay/PanelHub/Inventory/KeyToolContainerPanel.cs; Assets/Scripts/Presentation/UI/Gameplay/PanelHub/Inventory/PassiveInventorySlotUI.cs

## Assets/Scripts/Presentation/UI/Gameplay/PanelHub/Inventory/PlayerItemDetailPanelUI.cs
- 분류: 일반 소스; 720행
- 타입: PlayerItemDetailPanelUI : MonoBehaviour, RectTransformLayout
- 메서드: Awake, OnValidate, OnEnable, OnDisable, HandleSelectionChanged, ShowEmpty, RefreshText, BuildSummary, AppendGameplayDetails, ResolveWeaponMaximum, ResolveWeaponCurrent, ResolveRepairedMaximum, IsBrokenWeapon, BindLocalizedStrings, UnbindLocalizedStrings, HandleLocalizedNameChanged, HandleLocalizedDescriptionChanged, BuildView, ConfigureDetailsViewport, ResolveExistingViewRefs, ApplyEditorPreview, ResolveAuthoredRefs, SetContextOverlayPresentation, CaptureAuthoredLayout, SetVisible, CreateIcon, CreateDetailsScroll, CreateText
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 1개: Assets/Prefabs/UI/GamePlay/Canvas-GamePlay.prefab
- 타입 이름을 참조하는 소스 1개: Assets/Scripts/Presentation/UI/Gameplay/PanelHub/PlayerPanelHub.cs

## Assets/Scripts/Presentation/UI/Gameplay/PanelHub/PlayerPanelHub.cs
- 분류: 일반 소스; 1046행
- 타입: PlayerPanelHubMode, PlayerPanelHub : PanelUI, RectTransformLayout, PrimaryTabVisual
- 메서드: Reset, OnValidate, Awake, OnEnable, OnDisable, OnOverlayShown, OnOverlayHidden, OpenInventory, OpenCharacterStats, OpenFullMap, OpenMissions, OpenShop, OpenEnemyLoot, HandleShopOpenRequested, HandleEnemyLootOpenRequested, HandlePlayerMenuInput, HandleMapInput, HandlePrimaryPageInput, SetMode, ApplyMode, CloseChildContextMenus, SetHubVisible, CaptureInventoryHome, EnsureContextHubs, EnsureContextHubRoot, ConfigureContextHubRect, ResolveOverlayManager, MovePanelToHub, OpenContextHub, ApplyContextPanelLayout, ConfigureContextPlayerPanelRect, ConfigureLootTransferPlayerPanelRect, ConfigureContextRightPanelRect, CloseActiveContextHub, HandleContextHubShown, HandleContextHubHidden, RestorePlayerInventoryHome, CaptureLayout, RestoreLayout, SetItemDetailContextPresentation, ResolveRefs, IsHubVisible, IsPrimaryHubOpen, EnsurePrimaryTabBar, CreatePrimaryTab, RefreshPrimaryTabVisuals, GetOrAddCanvasGroup, SetCanvasGroup
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 1개: Assets/Prefabs/UI/GamePlay/Canvas-GamePlay.prefab
- 타입 이름을 참조하는 소스 8개: Assets/Scripts/Gameplay/Progression/Quests/HomeQuestNpc.cs; Assets/Scripts/Gameplay/World/Sectors/Shop/SectorShop.cs; Assets/Scripts/Presentation/UI/Gameplay/Overlay/UIOverlayId.cs; Assets/Scripts/Presentation/UI/Gameplay/Overlay/UIOverlayManager.cs; Assets/Scripts/Presentation/UI/Gameplay/PanelHub/ContextualHubOverlayLifecycle.cs; Assets/Scripts/Presentation/UI/Gameplay/PanelHub/Inventory/PlayerItemDetailPanelUI.cs; Assets/Scripts/Presentation/UI/Gameplay/PanelHub/PlayerTooltipUI.cs; Assets/Scripts/Presentation/UI/Gameplay/Tool/CanvasGroupVisibilitySuppressSource.cs

## Assets/Scripts/Presentation/UI/Gameplay/PanelHub/PlayerStatsStatusDetailPanelUI.cs
- 분류: 일반 소스; 422행
- 타입: PlayerStatsStatusDetailPanelUI : MonoBehaviour, EffectRowView, names
- 메서드: Awake, OnEnable, OnDisable, Update, BuildPanel, CreatePreviewSlot, ResolveAndBindRuntime, BindRuntime, HandleStatusChanged, Refresh, EnsureRowCount, CreateEffectRow, SetAdditionalRowsActive, BuildDescription, ResolveModifierName, CreateText
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 1개: Assets/Prefabs/UI/GamePlay/PanelHub/Stats/StatsPanel.prefab
- 타입 이름을 참조하는 소스 7개: Assets/Editor/ItemMakerWindow.cs; Assets/Scripts/Authoring/Editor/BulletTestPrefabGenerator.cs; Assets/Scripts/Diagnostics/Runtime/UI/UIRaycastDebugProbe.cs; Assets/Scripts/Gameplay/Entities/Shooter/Enemy/Common/EnemyShooter.cs; Assets/Scripts/Presentation/UI/Common/ESCMenu/UISettingKeyBindingComponent.cs; Assets/Scripts/Presentation/UI/Gameplay/PanelHub/Inventory/InventoryItemContextMenuUI.cs; Assets/Tests/EditMode/Editor/ItemMakerWeaponCategoryTests.cs

## Assets/Scripts/Presentation/UI/Gameplay/PanelHub/PlayerStatsSummaryPanelUI.cs
- 분류: 일반 소스; 302행
- 타입: PlayerStatsSummaryPanelUI : MonoBehaviour, SummaryStatId, SummaryRowDefinition, including, after
- 메서드: Awake, OnEnable, OnDisable, Bind, HandleStatsChanged, BuildRows, RefreshRows, ResolveValue, ResolvePlayerRuntimeReferences, SubscribeStatusEffectChanges, UnsubscribeStatusEffectChanges, HandlePlayerStatusEffectsChanged, GetQuestPercentAdd, ResolveArmorClassValue, FormatNumber
- 직렬화 참조 2개 / 지정 씬 참조 그래프 내 2개: Assets/Prefabs/UI/GamePlay/Canvas-GamePlay.prefab; Assets/Prefabs/UI/GamePlay/PanelHub/Stats/StatsPanel.prefab
- 타입 이름을 참조하는 소스 161개: Assets/Editor/ItemCategoryMoveUtility.cs; Assets/Editor/ItemMakerWindow.cs; Assets/Editor/LootProgressionEditorWindow.cs; Assets/Editor/QuestDesignTreeWindow.cs; Assets/Editor/ShooterCustomizeStationWindow.cs; Assets/Editor/ShopProgressionTreeWindow.cs; Assets/Scripts/Authoring/Editor/BossConfigPrefabLinkProvisioner.cs; Assets/Scripts/Authoring/Editor/ContentTemplateInspectors.cs

## Assets/Scripts/Presentation/UI/Gameplay/PanelHub/PlayerStatsSummaryRowUI.cs
- 분류: 일반 소스; 50행
- 타입: PlayerStatsSummaryRowUI : MonoBehaviour
- 메서드: Awake, Bind
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 1개: Assets/Prefabs/UI/GamePlay/PanelHub/Stats/StatRow.prefab
- 타입 이름을 참조하는 소스 2개: Assets/Scripts/Presentation/UI/Gameplay/MetaSystem/MetaStatsPanelUI.cs; Assets/Scripts/Presentation/UI/Gameplay/PanelHub/PlayerStatsSummaryPanelUI.cs

## Assets/Scripts/Presentation/UI/Gameplay/PanelHub/PlayerTooltipUI.cs
- 분류: 일반 소스; 449행
- 타입: TooltipPlacementSide, PlayerTooltipUI : MonoBehaviour
- 메서드: Reset, Awake, ShowText, ShowItem, Hide, GetOrCreatePanelTooltipRoot, CleanupLegacyTooltipClones, ResolveOwningPanelRoot, CacheRefs, SetVisible, RebuildFlexibleSize, ResolvePreferredWidth, SetPosition, SetPositionBesideAnchor, ClampToParent, BringToFront, ClampToScreen, SetText
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 1개: Assets/Prefabs/UI/GamePlay/Tooltip/TooltipBox.prefab
- 타입 이름을 참조하는 소스 3개: Assets/Scripts/Presentation/UI/Gameplay/PanelHub/Inventory/EnemyInventoryUI.cs; Assets/Scripts/Presentation/UI/Gameplay/PanelHub/Inventory/PlayerInventoryUI.cs; Assets/Scripts/Presentation/UI/Gameplay/Shop/PlayerShopItemUI.cs

## Assets/Scripts/Presentation/UI/Gameplay/PanelHub/QuestMissionPanelUI.cs
- 분류: 일반 소스; 400행
- 타입: QuestMissionPanelUI : MonoBehaviour
- 메서드: CreateRuntime, Awake, OnEnable, OnDisable, SetQuestGiver, EndConversation, AcceptQuest, Refresh, Subscribe, Unsubscribe, AddQuestRow, HandleLocaleChanged, RequestLocalizationRefresh, RefreshLocalizedTextWhenReady, Get, Format, AddButton, AddLabel, BuildRuntimeView, ClearRows, FindState
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 1개: Assets/Scenes/Manager/GamePlay.unity
- 타입 이름을 참조하는 소스 1개: Assets/Scripts/Presentation/UI/Gameplay/PanelHub/PlayerPanelHub.cs

## Assets/Scripts/Presentation/UI/Gameplay/PatternUI/NamedPatternDurationBarUI.cs
- 분류: 일반 소스; 89행
- 타입: NamedPatternDurationBarUI : MonoBehaviour
- 메서드: Awake, OnEnable, OnDisable, HandleDurationChanged, SetVisible
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 1개: Assets/Prefabs/GameScene/PatternUI/Canvas_Pattern.prefab
- 타입 이름을 참조하는 소스 0개: 

## Assets/Scripts/Presentation/UI/Gameplay/PlayerInfo/PlayerInfoHUD.cs
- 분류: 일반 소스; 432행
- 타입: PlayerInfoHUD : MonoBehaviour, ReloadKeyHintView
- 메서드: OnEnable, OnDisable, Update, OnHealthChanged, OnPlayerInfectionChanged, RequestSnapshotRefresh, Apply, RefreshInfectionRateText, HandleLocaleChanged, RequestLocalizationRefresh, RefreshLocalizedTextWhenReady, EnsureStatusEffectIcons, RefreshInfectionBlink, OnInputActionChanged, RefreshReloadHints, RefreshReloadHint, RefreshReloadIcon, RefreshReloadTitle, RefreshReloadKeyText, GetBindingDisplayString, SafeFormat
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 1개: Assets/Prefabs/UI/GamePlay/Player_Info.prefab
- 타입 이름을 참조하는 소스 0개: 

## Assets/Scripts/Presentation/UI/Gameplay/PlayerTimer/PlayerTimerHUD.cs
- 분류: 일반 소스; 580행
- 타입: PlayerTimerHUD : MonoBehaviour
- 메서드: Awake, Start, OnEnable, OnDisable, Update, OnStageProgressSnapshotChanged, ApplyTexts, ApplyStageText, ApplyStageLocalizedKey, OnStageLocalizedStringUpdated, HandleLocaleChanged, RequestLocalizationRefresh, RefreshLocalizedTextWhenReady, SetStageText, ApplyColor, FormatTime, FormatClockTime, EnsureCompactMiniMapPresentation, CreateCompactText, RefreshCompactPresentation, FormatCompactStage, LocalizeCompact, FormatRoomCoordinate, FormatRoomType, ApplyInstant
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 1개: Assets/Prefabs/UI/GamePlay/PlayerTimerHUD.prefab
- 타입 이름을 참조하는 소스 0개: 

## Assets/Scripts/Presentation/UI/Gameplay/PlayerTimer/StageClockIconGraphic.cs
- 분류: 일반 소스; 72행
- 타입: StageClockIconGraphic : MaskableGraphic
- 메서드: OnPopulateMesh, AddLine
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 1개: Assets/Scripts/Presentation/UI/Gameplay/PlayerTimer/PlayerTimerHUD.cs

## Assets/Scripts/Presentation/UI/Gameplay/ScreenEffect/BiochemicalGasScreenEffectUI.cs
- 분류: 일반 소스; 167행
- 타입: BiochemicalGasScreenEffectUI : MonoBehaviour
- 메서드: Awake, Update, OnDisable, CalculateVisualPressure01, CalculateTargetAlpha, ResolveRefs, ForceOff, ApplyMaterialAlpha, CachePropertyIds
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 1개: Assets/Prefabs/UI/GamePlay/Canvas_ScreenEffect.prefab
- 타입 이름을 참조하는 소스 1개: Assets/Scripts/Presentation/UI/Gameplay/ScreenEffect/LowHealthScreenEffectUI.cs

## Assets/Scripts/Presentation/UI/Gameplay/ScreenEffect/LowHealthScreenEffectUI.cs
- 분류: 일반 소스; 202행
- 타입: LowHealthScreenEffectUI : MonoBehaviour
- 메서드: Awake, OnEnable, OnDisable, OnValidate, Update, HandleHealthChanged, RefreshFromCurrentSnapshot, RecalculateDanger, CalculateLowHealthDanger01, RefreshGasPriority, ForceOff, ApplyAlpha, CachePropertyIds
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 1개: Assets/Prefabs/UI/GamePlay/Canvas_ScreenEffect.prefab
- 타입 이름을 참조하는 소스 1개: Assets/Scripts/Presentation/UI/Gameplay/ScreenEffect/BiochemicalGasScreenEffectUI.cs

## Assets/Scripts/Presentation/UI/Gameplay/SectorTraits/SectorTraitHUD.cs
- 분류: 일반 소스; 423행
- 타입: SectorTraitHUD : MonoBehaviour, TraitRowView
- 메서드: Start, Update, OnDisable, RefreshPresentation, BuildTraitSignature, BuildActiveTraitText, EnsureTraitList, RefreshTraitRows, CreateTraitRow, EnsureWarningBanner, CreateWarningText, ShowWarning, UpdateWarningFade, BuildWarningDescription
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 1개: Assets/Prefabs/UI/GamePlay/PlayerTimerHUD.prefab
- 타입 이름을 참조하는 소스 0개: 

## Assets/Scripts/Presentation/UI/Gameplay/Shop/PlayerShopItemRarity.cs
- 분류: 일반 소스; 14행
- 타입: PlayerShopItemRarity
- 메서드: 
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 15개: Assets/Editor/LootProgressionEditorWindow.cs; Assets/Scripts/Authoring/Editor/ContentPreflightWindow.cs; Assets/Scripts/Gameplay/Entities/Shooter/Player/Inventory/PlayerInventoryRuntime.Sorting.cs; Assets/Scripts/Gameplay/Items/Definitions/ItemSO.cs; Assets/Scripts/Gameplay/Items/Loot/LockedLootProfileSO.cs; Assets/Scripts/Gameplay/Items/Loot/LootProgressionConfigSO.cs; Assets/Scripts/Gameplay/Items/Loot/LootTableSO.cs; Assets/Scripts/Gameplay/Progression/Quests/RaidProgressionDraftSO.cs

## Assets/Scripts/Presentation/UI/Gameplay/Shop/PlayerShopItemUI.cs
- 분류: 일반 소스; 572행
- 타입: PlayerShopItemUI : MonoBehaviour
- 메서드: Awake, OnEnable, OnDisable, Bind, Clear, Refresh, ClearVisuals, RefreshSoldOut, SetButtonsInteractable, SetContentAlpha, SetSoldOutVisible, BindItemDetailClickEvent, UnbindTooltipEvents, HandleTooltipPointerEnter, HandleTooltipPointerExit, SetTooltipHoverAlpha, ShowTooltip, HideTooltip, GetOrCreateTooltip, ResolveTooltipAnchor, HandleBuyClicked, PublishItemDetailSelection, HandleRerollClicked, GetRarityColor, BindLocalizedName, BindLocalizedDescription, UnbindLocalizedText, UnbindLocalizedName, UnbindLocalizedDescription, HandleNameStringChanged, HandleDescriptionStringChanged
- 직렬화 참조 2개 / 지정 씬 참조 그래프 내 2개: Assets/Prefabs/UI/GamePlay/PanelHub/Shop/ShopItem.prefab; Assets/Prefabs/UI/GamePlay/PanelHub/Shop/ShopPanel.prefab
- 타입 이름을 참조하는 소스 1개: Assets/Scripts/Presentation/UI/Gameplay/Shop/PlayerShopPanelUI.cs

## Assets/Scripts/Presentation/UI/Gameplay/Shop/PlayerShopOffer.cs
- 분류: 일반 소스; 177행
- 타입: PlayerShopOffer
- 메서드: TryConsumeStock, RestoreConsumedStock, SetPrice, BuildTradeSummary, RestoreRemainingStock, ResolveDisplayName, ResolveItemDescription, CopyTradeRequirements
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 7개: Assets/Scripts/Core/Events/ScriptableObject/SectorShopOpenRequestEventChannelSO.cs; Assets/Scripts/Gameplay/World/Sectors/Shop/SectorShop.cs; Assets/Scripts/Gameplay/World/Sectors/Shop/ShopConfigSO.cs; Assets/Scripts/Gameplay/World/Sectors/Shop/ShopRoomDropTableSO.cs; Assets/Scripts/Presentation/UI/Gameplay/Shop/PlayerShopItemUI.cs; Assets/Scripts/Presentation/UI/Gameplay/Shop/PlayerShopPanelUI.cs; Assets/Scripts/Presentation/UI/Gameplay/Shop/ShopPurchaseTradePanel.cs

## Assets/Scripts/Presentation/UI/Gameplay/Shop/PlayerShopPanelUI.cs
- 분류: 일반 소스; 1364행
- 타입: PlayerShopPanelUI : PanelUI, OfferSection
- 메서드: OnEnable, OnDisable, BindPlaceholder, ApplyContextPresentation, FindChildRect, ConfigureOfferColumn, Stretch, AnchorTop, AnchorBottom, AnchorFillHalf, OpenWithOffers, ClosePanel, OnOverlayShown, OnPanelHidden, SetSellSelectionSource, SetPlayerItemSelectionSource, TrySellInventorySlot, TrySellEquipmentSlot, ResetShopForNewRun, EnsureInitialized, HandleSectorShopOpenRequested, HandleInventoryRuntimeReady, RollAllOffers, RefreshAll, RefreshOfferViews, HandleBuyRequested, TryCompletePhysicalPurchase, HandleRerollRequested, HandleConfigReroll, TryFindOfferSlot, ShouldAllowDuplicate, ResolveOfferList, BuildExcludedItems, ReplaceOffers, HandlePlayerItemSelectionChanged, HandleSellClicked, HandleRepairClicked, TrySellSelectedPlayerItem, TryRepairSelectedArmor, TryRepairSelectedItem, RefreshPlayerItemSelectionView, RefreshPlayerItemNameText, RefreshPlayerItemAmountText, ResolveRepairPrice, ResolveRerollCost, ResolveInventoryRuntime, ResolveInventorySellUnitPrice, ResolveInventoryItem, TryConsumePhysicalMoney, RefundPhysicalPayment, TryGrantPhysicalMoney, TryCompletePhysicalSale, TryResolvePhysicalMoneyItem, TrySpawnMoneyWorldDrop, RaiseMessage, PlayUiAudio, SetCanvasGroup
- 직렬화 참조 2개 / 지정 씬 참조 그래프 내 2개: Assets/Prefabs/UI/GamePlay/Canvas-GamePlay.prefab; Assets/Prefabs/UI/GamePlay/PanelHub/Shop/ShopPanel.prefab
- 타입 이름을 참조하는 소스 2개: Assets/Scripts/Presentation/UI/Gameplay/PanelHub/PlayerPanelHub.cs; Assets/Scripts/Presentation/UI/Gameplay/Shop/ShopPurchaseTradePanel.cs

## Assets/Scripts/Presentation/UI/Gameplay/Shop/ScriptableObjects/PlayerShopCatalogSO.cs
- 분류: 일반 소스; 13행
- 타입: PlayerShopCatalogSO : ScriptableObject
- 메서드: 
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 0개: 

## Assets/Scripts/Presentation/UI/Gameplay/Shop/ShopPurchaseTradePanel.cs
- 분류: 일반 소스; 567행
- 타입: ShopPurchaseTradePanel : MonoBehaviour, RequirementRow, ShopPurchaseTradeDropSlotUI : MonoBehaviour, IDropHandler, IPointerClickHandler
- 메서드: EnsureInstance, TryOpen, CloseIfOwned, Awake, OnDestroy, Update, HandleInventoryTradeRequested, HandleRequirementDrop, ClearRequirement, Open, Close, HandleConfirmClicked, SubmitItemToFirstMatchingRequirement, SubmitItemToRequirement, GetAvailableUnreservedAmount, RebuildRequirementRows, RefreshView, RefreshRows, SetFeedback, IsSameItem, BuildRuntimeUi, CreateRect, CreateImage, CreateButton, CreateText, SetFullStretch, SetAnchors, Configure, OnDrop, OnPointerClick
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 1개: Assets/Scripts/Presentation/UI/Gameplay/Shop/PlayerShopPanelUI.cs

## Assets/Scripts/Presentation/UI/Gameplay/Shop/WeaponAmmoSellTypes.cs
- 분류: 일반 소스; 56행
- 타입: WeaponAmmoSellPopupRequest, WeaponAmmoSellConfirmRequest, WeaponAmmoSellResult
- 메서드: 
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 5개: Assets/Scripts/Core/Events/ScriptableObject/PanelHub/WeaponAmmoSellConfirmRequestEventChannelSO.cs; Assets/Scripts/Core/Events/ScriptableObject/PanelHub/WeaponAmmoSellPopupRequestEventChannelSO.cs; Assets/Scripts/Core/Events/ScriptableObject/PanelHub/WeaponAmmoSellResultEventChannelSO.cs; Assets/Scripts/Gameplay/Entities/Shooter/Player/PlayerBulletLoadoutRuntime.cs; Assets/Scripts/Presentation/UI/Gameplay/Slot/AmmoSlotSellPopupUI.cs

## Assets/Scripts/Presentation/UI/Gameplay/Shop/WeaponAmmoShopPurchaseTypes.cs
- 분류: 일반 소스; 56행
- 타입: WeaponAmmoShopPurchaseRequest, WeaponAmmoShopPurchaseResult
- 메서드: 
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 3개: Assets/Scripts/Core/Events/ScriptableObject/PanelHub/WeaponAmmoShopPurchaseRequestEventChannelSO.cs; Assets/Scripts/Core/Events/ScriptableObject/PanelHub/WeaponAmmoShopPurchaseResultEventChannelSO.cs; Assets/Scripts/Gameplay/Entities/Shooter/Player/PlayerBulletLoadoutRuntime.cs

## Assets/Scripts/Presentation/UI/Gameplay/Slot/AmmoCycleListHUD.cs
- 분류: 일반 소스; 386행
- 타입: AmmoCycleListHUD : MonoBehaviour
- 메서드: Refresh, FindSelectedIndex, CalculateContentSignature, EnsureRoot, CreateRow, CreateCycleKeyBlock, CreateText, CreatePanel, ClearRows, OnDestroy
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 1개: Assets/Scripts/Presentation/UI/Gameplay/Slot/SlotHUD.cs

## Assets/Scripts/Presentation/UI/Gameplay/Slot/AmmoSlotSellPopupUI.cs
- 분류: 일반 소스; 158행
- 타입: AmmoSlotSellPopupUI : MonoBehaviour
- 메서드: Awake, OnEnable, OnDisable, Show, HandleSliderChanged, RefreshTexts, Confirm, Cancel, SetVisible, EnsureCanvasGroup
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 0개: 

## Assets/Scripts/Presentation/UI/Gameplay/Slot/AmmoSlotUI.cs
- 분류: 일반 소스; 194행
- 타입: AmmoSlotUI : SlotUI
- 메서드: Initialize, Bind, Clear, OnPointerClick, OnBeginDrag, OnEndDrag, OnDrop, RefreshSupplementalVisuals, RefreshAmmoTypeIcon, RefreshAmmoTexts, SetAmmoText, GetAmmoTypeSprite
- 직렬화 참조 2개 / 지정 씬 참조 그래프 내 2개: Assets/Prefabs/UI/GamePlay/Canvas-GamePlay.prefab; Assets/Prefabs/UI/GamePlay/Slot/AmmoSlot.prefab
- 타입 이름을 참조하는 소스 1개: Assets/Scripts/Presentation/UI/Gameplay/Slot/SlotHUD.cs

## Assets/Scripts/Presentation/UI/Gameplay/Slot/ArmorSlotUI.cs
- 분류: 일반 소스; 145행
- 타입: ArmorSlotUI : SlotUI
- 메서드: Bind, OnPointerClick, OnBeginDrag, OnEndDrag, OnDrop, RefreshDurability, RefreshFinalArmorClass
- 직렬화 참조 2개 / 지정 씬 참조 그래프 내 2개: Assets/Prefabs/UI/GamePlay/Canvas-GamePlay.prefab; Assets/Prefabs/UI/GamePlay/Slot/ArmorSlot.prefab
- 타입 이름을 참조하는 소스 1개: Assets/Scripts/Presentation/UI/Gameplay/Slot/SlotHUD.cs

## Assets/Scripts/Presentation/UI/Gameplay/Slot/SlotHUD.cs
- 분류: 일반 소스; 892행
- 타입: SlotHUD : MonoBehaviour
- 메서드: Awake, OnEnable, OnDisable, Update, RequestActivateQuickSlot, RequestActivateWeaponSlot, RequestHandleWeaponSlotDrop, RequestClearQuickSlot, RequestUnequipQuickSlot, RequestBindQuickSlotFromDrag, RequestEquipInventoryItemToEquipmentSlot, RequestUnequipEquipmentSlot, ResolveRefs, InitializeSlots, SubscribeRuntimeEvents, UnsubscribeRuntimeEvents, HandleInventoryRuntimeReady, HandleStatsRuntimeReady, HandleStatsChanged, HandleWeaponAmmoLoadoutChanged, RefreshAllSlots, RefreshQuickSlots, EnsureAmmoCycleListHUD, RefreshAmmoCycleList, RefreshWeaponSlots, RefreshUsableQuickSlots, GetQuickSlotSnapshot, RefreshArmorSlot, BindInputReader, UnbindInputReader, SelectInputSlot1, SelectInputSlot2, SelectInputSlot3, SelectInputSlot4, SelectInputSlot5, HandleAmmoSelectionInput, OpenAmmoSelection, MoveAmmoSelection, ConfirmAmmoSelection, ResolveHighlightedAmmoIndex, CloseAmmoSelection, GetGlobalSlotKeyLabel, RaiseMessage
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 1개: Assets/Prefabs/UI/GamePlay/Canvas-GamePlay.prefab
- 타입 이름을 참조하는 소스 5개: Assets/Scripts/Gameplay/Entities/Shooter/Player/Inventory/PlayerQuickSlotRuntime.cs; Assets/Scripts/Presentation/UI/Gameplay/PanelHub/Inventory/PlayerInventoryUI.cs; Assets/Scripts/Presentation/UI/Gameplay/Slot/AmmoSlotUI.cs; Assets/Scripts/Presentation/UI/Gameplay/Slot/SlotUI.cs; Assets/Scripts/Presentation/UI/Gameplay/Slot/WeaponSlotUI.cs

## Assets/Scripts/Presentation/UI/Gameplay/Slot/SlotUI.cs
- 분류: 일반 소스; 187행
- 타입: SlotUI : MonoBehaviour,
- 메서드: OnEnable, OnDisable, Initialize, ClearVisual, SetItemVisual, SetKeyLabel, SetStaticLabel, SetSelected, OnPointerClick, OnBeginDrag, OnEndDrag, OnDrop, ResolveItemName, BindDisplayName, UnbindDisplayName, HandleDisplayNameChanged
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 3개: Assets/Scripts/Presentation/UI/Gameplay/Slot/AmmoSlotUI.cs; Assets/Scripts/Presentation/UI/Gameplay/Slot/ArmorSlotUI.cs; Assets/Scripts/Presentation/UI/Gameplay/Slot/WeaponSlotUI.cs

## Assets/Scripts/Presentation/UI/Gameplay/Slot/WeaponSlotUI.cs
- 분류: 일반 소스; 203행
- 타입: WeaponSlotUI : SlotUI
- 메서드: Initialize, Bind, RefreshMagazine, EnsureMagazine, BuildWeaponLabel, OnPointerClick, OnBeginDrag, OnEndDrag, OnDrop
- 직렬화 참조 2개 / 지정 씬 참조 그래프 내 2개: Assets/Prefabs/UI/GamePlay/Canvas-GamePlay.prefab; Assets/Prefabs/UI/GamePlay/Slot/WeaponSlot.prefab
- 타입 이름을 참조하는 소스 1개: Assets/Scripts/Presentation/UI/Gameplay/Slot/SlotHUD.cs

## Assets/Scripts/Presentation/UI/Gameplay/StatusEffectIconStripUI.cs
- 분류: 일반 소스; 328행
- 타입: StatusEffectIconStripUI : MonoBehaviour, LayoutMode
- 메서드: Create, Awake, OnDestroy, ConfigureAsPlayerHealthGrid, Bind, HandleChanged, ConfigureLayout, Refresh, SpawnIcon, ClearIcons, GetIcon, ResolveIcon, BuildProceduralIcon, DrawDisc, DrawRect, SetPixel
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 1개: Assets/Prefabs/UI/GamePlay/Player_Info.prefab
- 타입 이름을 참조하는 소스 4개: Assets/Scripts/Presentation/UI/Gameplay/EnemyBarUI/HealthBar/EnemyHealthBarWidget.cs; Assets/Scripts/Presentation/UI/Gameplay/EntityStatusEffectWorldGridUI.cs; Assets/Scripts/Presentation/UI/Gameplay/PanelHub/PlayerStatsStatusDetailPanelUI.cs; Assets/Scripts/Presentation/UI/Gameplay/PlayerInfo/PlayerInfoHUD.cs

## Assets/Scripts/Presentation/UI/Gameplay/SystemMessageUI/SystemMessageUI.cs
- 분류: 일반 소스; 197행
- 타입: SystemMessageUI : MonoBehaviour
- 메서드: Awake, OnEnable, OnDisable, AddMessage, ClearMessages, TrimOldMessages, RequestScrollToBottom, ScrollToBottomNextFrame, ScrollToBottomImmediate, SetVisible, EnsureCanvasGroup
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 1개: Assets/Prefabs/UI/GamePlay/Canvas-GamePlay.prefab
- 타입 이름을 참조하는 소스 0개: 

## Assets/Scripts/Presentation/UI/Gameplay/Tool/CanvasGroupVisibilitySuppressSource.cs
- 분류: 일반 소스; 134행
- 타입: CanvasGroupVisibilitySuppressSource : MonoBehaviour
- 메서드: Reset, Awake, OnEnable, LateUpdate, OnDisable, RefreshNow, Refresh, IsSourceVisible, RaiseSuppress
- 직렬화 참조 5개 / 지정 씬 참조 그래프 내 5개: Assets/Prefabs/GameScene/PatternUI/QTE/QTETimingPanel.prefab; Assets/Prefabs/UI/GamePlay/BossRewardChoicePanel.prefab; Assets/Prefabs/UI/GamePlay/Canvas-ESCMenu.prefab; Assets/Prefabs/UI/GamePlay/Canvas-GamePlay.prefab; Assets/Prefabs/UI/GamePlay/MAP/FullMapRoot.prefab
- 타입 이름을 참조하는 소스 0개: 

## Assets/Scripts/Presentation/UI/Gameplay/Tool/CanvasGroupVisibilitySuppressTarget.cs
- 분류: 일반 소스; 167행
- 타입: CanvasGroupVisibilitySuppressTarget : MonoBehaviour, CanvasGroupState
- 메서드: Reset, Awake, OnEnable, OnDisable, HandleSuppressRequest, RefreshFromChannel, ApplySuppressed, EnsureTargetGroup, CaptureInitialState, RestoreInitialState
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 1개: Assets/Scenes/RUNScene.unity
- 타입 이름을 참조하는 소스 0개: 

## Assets/Scripts/Presentation/UI/Gameplay/Tool/VerticalLocalizedTMPText.cs
- 분류: 일반 소스; 78행
- 타입: VerticalLocalizedTMPText : MonoBehaviour
- 메서드: Reset, Awake, SetLocalizedText, ToVerticalText
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 0개: Assets/Prefabs/UI/GamePlay/SellSection.prefab
- 타입 이름을 참조하는 소스 0개: 

## Assets/Scripts/Presentation/UI/Gameplay/Vision/PlayerVisionFogOverlayUI.cs
- 분류: 일반 소스; 439행
- 타입: PlayerVisionFogOverlayUI : MonoBehaviour
- 메서드: Reset, Awake, OnEnable, OnDisable, LateUpdate, OnDestroy, UpdateFogMaterial, LogDebugVisionValues, ResolveWorldDistanceForPixels, UpdateOcclusionDistances, ResolveRefs, ResolveViewer, EnsureMaterial, SetImageVisible, OnWorldCameraReady
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 1개: Assets/Prefabs/UI/GamePlay/Canvas-Vision.prefab
- 타입 이름을 참조하는 소스 0개: 

## Assets/Scripts/Presentation/UI/HUD/Legacy/AchievementPopupManager.cs
- 분류: WIP/Legacy 경로; 231행
- 타입: AchievementPopupManager : MonoBehaviour
- 메서드: ShowEligibleUnlocksFromProgress, ShowOnce, Enqueue
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 2개: Assets/Scripts/Presentation/Audio/Sound/UI/UI_AchievementPopupClick.cs; Assets/Scripts/WIP/Legacy/Presentation/SurvivalSuccessManager.cs

## Assets/Scripts/Presentation/UI/HUD/Legacy/AchievementPopupPrefab.cs
- 분류: WIP/Legacy 경로; 27행
- 타입: AchievementPopupPrefab : MonoBehaviour
- 메서드: 
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 0개: Assets/Prefabs/GameScene/AchievePopup_Prefab.prefab
- 타입 이름을 참조하는 소스 1개: Assets/Scripts/Presentation/UI/HUD/Legacy/AchievementPopupManager.cs

## Assets/Scripts/Presentation/UI/HUD/Legacy/ScriptableObject/EnemyHealthBarSettingsSO.cs
- 분류: WIP/Legacy 경로; 23행
- 타입: EnemyHealthBarSettingsSO : ScriptableObject
- 메서드: 
- 직렬화 참조 1개 / 지정 씬 참조 그래프 내 1개: Assets/ScriptableObject/GamePlay/UI/EnemyHealthBarSettings.asset
- 타입 이름을 참조하는 소스 2개: Assets/Scripts/Presentation/UI/Gameplay/EnemyBarUI/EnemyScreenSpaceHPUIAnchor.cs; Assets/Scripts/Presentation/UI/Gameplay/EnemyBarUI/HealthBar/EnemyHealthBarWidget.cs

## Assets/Scripts/Presentation/UI/HUD/World/EnemyHealthUI/EnemyHealthBarUI.cs
- 분류: 일반 소스; 88행
- 타입: EnemyHealthBarUI : MonoBehaviour
- 메서드: OnEnable, OnDisable, Update, OnHealthChanged, RefreshImmediate, SetTarget
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 0개: 

## Assets/Scripts/Presentation/UI/Menu/MainMenuController.cs
- 분류: 일반 소스; 32행
- 타입: MainMenuController : MonoBehaviour
- 메서드: OpenMenu, UnpauseMenu
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 0개: 

## Assets/Scripts/Presentation/UI/Menu/MenuSelectionHandler.cs
- 분류: 일반 소스; 151행
- 타입: MenuSelectionHandler : MonoBehaviour
- 메서드: OnEnable, OnDisable, UpdateDefault, SelectDefault, Unselect, HandleMoveSelection, HandleMoveCursor, HandleMouseEnter, HandleMouseExit, AllowsSubmit, UpdateSelection, Update
- 직렬화 참조 3개 / 지정 씬 참조 그래프 내 2개: Assets/Prefabs/SettingPanel.prefab; Assets/Prefabs/UI/GamePlay/Canvas-ESCMenu.prefab; Assets/Prefabs/UI/MainMenu/Canvas-MainMenu.prefab
- 타입 이름을 참조하는 소스 2개: Assets/Scripts/Presentation/UI/Menu/MultiInputButton.cs; Assets/Scripts/Presentation/UI/Menu/MultiInputSelectableElement.cs

## Assets/Scripts/Presentation/UI/Menu/MultiInputButton.cs
- 분류: 일반 소스; 82행
- 타입: MultiInputButton : Button
- 메서드: Awake, OnPointerEnter, OnPointerExit, OnSelect, UpdateSelected, OnSubmit, ResolveMenuSelectionHandler
- 직렬화 참조 10개 / 지정 씬 참조 그래프 내 9개: Assets/Prefabs/UI/GamePlay/Button/GenericButton.prefab; Assets/Prefabs/UI/GamePlay/Settings/SettingsPanel.prefab; Assets/Prefabs/UI/GamePlay/Settings/Settings_Item_KeyBind.prefab; Assets/Prefabs/UI/GamePlay/Settings/Settings_Item_Reset.prefab; Assets/Prefabs/UI/GamePlay/Settings/Setting_Generic_Tab.prefab; Assets/Prefabs/UI/MainMenu/ButtonMenu.prefab; Assets/Prefabs/UI/MainMenu/Canvas-MainMenu.prefab; Assets/Prefabs/UI/MainMenu/CreditPanel.prefab
- 타입 이름을 참조하는 소스 3개: Assets/Scripts/Presentation/UI/Common/ESCMenu/UISettingItemFiller.cs; Assets/Scripts/Presentation/UI/Common/UIGenericButton.cs; Assets/Scripts/Presentation/UI/Menu/MenuSelectionHandler.cs

## Assets/Scripts/Presentation/UI/Menu/MultiInputSelectableElement.cs
- 분류: 일반 소스; 40행
- 타입: MultiInputSelectableElement : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler
- 메서드: Awake, OnPointerEnter, OnPointerExit, OnSelect
- 직렬화 참조 0개 / 지정 씬 참조 그래프 내 0개: 
- 타입 이름을 참조하는 소스 1개: Assets/Scripts/Presentation/UI/Menu/MenuSelectionHandler.cs