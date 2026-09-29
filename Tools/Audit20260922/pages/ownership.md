# 기술 A. 씬·서비스·SO·런타임 데이터 소유권

검사 기준: 2026-09-22 / E:/github/board_chase/board_chase. 현재 파일의 정적 연결·핵심 실행 경로 기준이며, 별도 표시 없는 항목은 이번 감사에서 플레이 테스트한 결과가 아닙니다.

## 씬별 경계

- Initialization: 부팅 진입과 지속 매니저/메뉴 준비.
- PersistenceManager: RunLifecycle, SceneLoader, 전환/시야 서비스, AudioManager/UIAudio/WorldGameAudio/InteractionSearchAudio, SettingsSystem 등 지속 서비스.
- GamePlay: GameManager, UIOverlay, HUD/인벤토리/퀘스트 등 플레이 UI·관리. 실제 요소 상당수는 중첩 프리팹.
- HomeScene: 창고·NPC·상점·스테이지 선택 환경. 씬 직접 script 개수만으로 내부 프리팹의 기능을 누락하면 안 된다.
- RUNScene: 스폰·섹터·포탈·진행·보스·투사체 루트·월드 아이템·카메라·마스크 등 레이드 월드 서비스.
- MainMenu / GameOver: 메뉴와 종료 표현 경계.

이번 그래프의 루트는 위 7개 씬이다. 포함된 자산이라고 항상 활성 GameObject/선택된 pool이라는 의미는 아니다. 반대로 동적 생성/Resources/Addressables 간접 참조는 그래프만으로 충분히 잡히지 않는다.

## SO는 정의, Runtime은 현재 상태

- ItemSO/WeaponSO/BulletSO/Armor/Consumable: 정적 정의. 개별 수량·내구·탄창은 인벤토리/런타임.
- StageDefinition/Preset/Encounter/Content: 스테이지 제작 데이터. 현재 방·클리어·레이드 시간·전력은 월드 런타임.
- Enemy Config/Loadout/StateMachine SO: 적 정의와 행동 부품. 현재 목표·쿨다운·HP는 적 인스턴스.
- QuestDefinition/Line: 설계 데이터. 진행/수령 영수증은 QuestRuntime/Profile.
- ShopConfig: 상품 후보와 정책. 현재 제안/재고는 ShopRun, 영구 해금은 Profile.
- AudioCue/Configuration: 소리 정의. AudioManager/SoundEmitter는 재생 인스턴스.
- SettingsSO: 현재 설정의 적용 인터페이스. 기기 지속 데이터는 settings.es3.

## 이벤트와 준비 상태

Core/Events의 ScriptableObject 이벤트 채널은 UI/서비스 간 연결과 Ready 알림을 제공한다. 실제 SO 자산을 양쪽이 같은 것으로 참조해야 한다. 같은 타입의 채널 두 개를 만들면 알림이 분리될 수 있다. OnEnable/OnDisable 구독 짝과 현재 상태 재발행을 점검한다.

RuntimeSceneDependencyValidator는 서비스 누락을 보고하는 계약 검사다. 누락 서비스 전부를 임의로 새로 생성하는 구조로 이해하지 않는다. RunLifecycle/SceneLoader 준비 게이트는 로드 순서와 복원 타이밍을 조정한다.

## 동적 컴포넌트 판정

RaidRuntime, QuestRuntime, 일부 시체 드롭과 시야/카메라 보조 컴포넌트는 Ensure/AddComponent 경로가 있다. 자산 참조 0≠미사용. ShooterVisualRig·ShieldArmorRuntime은 이번 검사에서 대응하는 실제 연결을 확인하지 못했으므로 별도 미완성으로 표시했다.

## 저장 경계

Settings/Profile/Run을 나누되 인벤토리→창고, 아이템 제출→퀘스트 보상, 배터리 소비→발전기 가동처럼 경계를 넘는 작업은 원자적 업무 단위로 다뤄야 한다. 이벤트를 중간에 발행하고 즉시 저장하면 양쪽 상태가 불일치할 수 있다.

## 의존성 변경 시 확인

새 클래스만 만든 뒤 끝내지 말고 소유 씬/프리팹, SO 연결, 생성·해제, 이벤트, UI, 저장/마이그레이션, 테스트, Notion 기준서까지 같은 변경 목록에 포함한다.

## 코드·데이터 근거

- `E:/github/board_chase/board_chase/Assets/Scripts/Core/SceneFlow/RuntimeSceneDependencyValidator.cs`
- `E:/github/board_chase/board_chase/Assets/Scripts/Core/SceneFlow/InitializationLoader.cs`
- `E:/github/board_chase/board_chase/Assets/Scripts/Core/SceneFlow/SceneLoader.cs`
- `E:/github/board_chase/board_chase/Assets/Scripts/Core/Persistence/RunLifecycleCoordinator.cs`
- `E:/github/board_chase/board_chase/Assets/Scenes/Manager/PersistenceManager.unity`
- `E:/github/board_chase/board_chase/Assets/Scenes/Manager/GamePlay.unity`
- `E:/github/board_chase/board_chase/Assets/Scenes/RUNScene.unity`