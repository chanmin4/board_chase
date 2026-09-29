const fs=require('fs');
const {pages,doc}=require('./docs.cjs');
doc('render',null,'10. 시야·카메라·렌더링·물리·성능 경계',`
## AI 감지와 화면 표시

EntityVisionController는 거리·방향·LOS를 판단한다. VisionVisibilitySystem은 플레이어 시야를 기준으로 대상 Renderer 표시를 관리한다. 소리 인지는 SoundStimulus 계열이다. 하나를 끄면 나머지도 자동으로 꺼지는 단일 시스템이 아니다.

VisionVisibilitySystem의 기본 적 등록 스캔은 0.25초, 시야 갱신 기본 간격은 0으로 LateUpdate 매 프레임 판정을 허용한다. 스폰 준비 중인 적을 별도 취급한다. 백신 페인트 강조 옵션이 남아 있지만 기본적으로 시야 밖까지 페인트로 강제 공개하지 않는 옵션값이다. 실제 씬 override는 자산에서 확인한다.

## 카메라 가림 표현

CameraEntityOcclusionFadeController라는 이름과 달리 현재 코드의 핵심은 구조물 뒤 Character에 깊이 기반 실루엣 overlay를 붙이는 것이다. 모든 장애물 머티리얼을 반투명으로 바꾸는 기능으로 설명하지 않는다.

Resources/CameraOccluderDither 셰이더와 캐릭터 Renderer 추적, 깊이 차이, SectorStructure 레이어를 사용한다. 런타임 EnsureFor로 생성될 수 있다. 임시 머티리얼/렌더러는 비활성·파괴 때 정리한다. 손 무기와 시체의 Renderer도 뒤늦게 갱신될 수 있다.

## 월드 생성과 내비게이션

StageSectorInstantiator가 생성한 지형·장애물·입구와 NavMesh 준비 상태는 적 AI, 포탈 이동, 로딩 준비 게이트의 전제다. 프리팹이 예뻐 보여도 collider/레이어/내비게이션 통로가 잘못되면 실제 방 기능은 완료가 아니다.

투사체의 충돌 레이어와 표면 소리 분류, 시야 가림 레이어, 카메라 실루엣 대상 레이어는 별개 정책이다. Material 또는 Layer 하나로 모든 의미를 겸하지 않는다.

## URP 현재 주의점

Unity 6000.0.47f1 / URP 17.0.4 환경이다. DefaultVolumeProfile에는 CopyPasteTestComponent·VolumeComponentSupported 계열 및 fileID 0인 테스트 컴포넌트가 남아 있고 일부 미해결 스크립트 GUID도 있다. 현재 파일에서 재확인한 항목이며 이전 감사의 추측만 가져온 것이 아니다. 정리와 임포트/실렌더 검증이 필요하다.

머티리얼 파일의 미해결 GUID 중에는 이전 업그레이더 메타데이터도 있으므로 모두 분홍 셰이더라고 단정하지 않는다. Renderer가 참조하는 Material → 실제 shader/texture → URP 호환 → 화면 출력 순서로 검사한다.

## 성능 검증 범위

이번 감사에서는 Unity Profiler 측정이나 목표 하드웨어 FPS 수치를 만들지 않았다. 큰 13×13 맵, 활성 적 수, LOS raycast, 마스크 샘플링, 시체/아이템 수, UI rebuild, 오디오 동시 발음, 저장 디스크 쓰기를 실제 부하 조건에서 측정해야 한다. 풀링 코드가 있다는 것만으로 성능 완료라고 표시하지 않는다.
`,['EntityVisionController.cs','VisionVisibilitySystem.cs','CameraEntityOcclusionFadeController.cs','CameraManager.cs','StageSectorInstantiator.cs','ProjectileRootRegistry.cs','Assets/Settings/DefaultVolumeProfile.asset']);

doc('tools',null,'11. 제작 도구·작업 루틴·에디터 자동 변경',`
## 도구와 실제 데이터 소유권

- Tools/Authoring/Authoring Hub: 제작 진입점과 관련 도구 안내.
- Item Maker: ItemSO 카탈로그·타입·미리보기·루팅 연결·다중 선택/이동.
- Loot Progression Table: 적/source별 드롭 풀과 단계별 override.
- Quest Design Tree: 퀘스트 그래프·요구·보상·선행 관계.
- Shop Progression Tree: 상품과 해금/거래 조건.
- Sector Maker: 섹터 지형·배치·프리셋 제작.
- Shooter Customize Station: 외형 SO·소켓/프리뷰. 실제 프리팹 리그 연결을 별도로 확인.
- Audio Cue Library: Cue 등록·카테고리·설정·재생 확인.
- Tools/Board Chase/Save Inspector: 저장 파티션·백업 조사.
- Tools/Debug/Debug Monitor: 플레이 중 상태/적/퀘스트/인벤토리 등 진단.
- ContentPreflightValidator/Window: 콘텐츠 연결 검사를 위한 코드 기반 도구. 이번 감사에서 모든 검사 항목을 Unity에서 실행한 것은 아니다.

## 아이템 카테고리 이동은 파일 이동만이 아니다

ItemCategoryMoveUtility는 목표 타입, typed reference 호환성, 기존 GUID, 공통 필드 복사, 임시 백업과 Restore Last Move를 다룬다. 무기 특화 클래스가 강제하는 무기 타입은 임의 카테고리 변환과 충돌할 수 있다. 다중 이동은 실패/부분 적용·복원 결과를 확인해야 한다.

Item Maker의 ‘카테고리’는 계층 표시와 필터 성격도 있다. 예를 들어 ThrowableUsableItemSO를 Consumables 아래 표시하는 것이 ConsumableItemSO로 강제 상속 변환됐다는 뜻은 아니다. 아이템별 공격/소비 동작은 실제 타입과 데이터가 결정한다.

## 자동 수정에 주의

BossConfigPrefabLinkProvisioner에는 InitializeOnLoad → delayCall → 링크 복구 → SaveAssets 경로가 있다. InventorySortButtonMigration도 로드 후 열린 씬을 정리하는 에디터 경로가 있다. 기타 postprocessor와 공급사 도구도 파일/임포트 시 반응할 수 있다.

따라서 ‘Unity에서 보기만 했다’와 ‘파일이 절대 바뀌지 않았다’는 항상 같지 않다. 작업 전후 diff를 확인하고 자동 복구가 무엇을 바꿨는지 기록한다. 현재 워크트리에 삭제·수정·미추적 항목이 많이 있어 이를 사용자 변경과 구별해야 한다.

## Notion → Unity 제작 루틴

1. Notion 현재 기준서에서 관련 책임·현재 상태·알려진 결함을 읽는다.
2. 변경안에 목표/범위/비범위/데이터 소유자/저장 영향/승인 테스트를 작성한다.
3. 기존 도구에서 SO와 프리팹을 작성한다. 런타임 코드가 필요한 경우에만 경로를 수정한다.
4. 컴파일 → 정적 참조 검사 → 관련 테스트 → 실제 플레이/빌드 확인을 수행한다.
5. 실제 파일 경로, 변경값, 테스트 결과를 기록하고 현재 기준서를 갱신한다.

현재 문서 상태와 미래 작업 카드를 같은 ‘완료’ 표시로 쓰지 않는다. 제안/구현중/연결확인/검증완료를 나눈다.
`,['BoardChaseAuthoringHubWindow.cs','ItemMakerWindow.cs','ItemCategoryMoveUtility.cs','LootProgressionEditorWindow.cs','QuestDesignTreeWindow.cs','ShopProgressionTreeWindow.cs','ContentPreflightWindow.cs','BossConfigPrefabLinkProvisioner.cs','InventorySortButtonMigration.cs']);

doc('technical','3d03fd19-ccd6-818a-9104-c0aef475e63f','기술 부록 | 현재 Unity·Assets·코드 감사 (2026-09-22)',`
## 감사 기준

대상 체크아웃은 E:/github/board_chase/board_chase 하나다. 다른 체크아웃, 과거 Notion 설명, 이전 대화의 완료 주장보다 이번 현재 파일을 우선했다. 프로덕션 스크립트·씬·프리팹·SO는 이번 작업에서 패치하지 않았다. 감사 스크립트와 결과만 Tools/Audit20260922 아래에 생성했다.

## 정적 전수 범위

- Assets 전체 파일 69,188개: meta 35,792개 포함, 비-meta 33,396개.
- C# 1,221개: 자사 검사 범위 Assets/Scripts·Editor·Tests 866개, 168,936행. 외부 패키지·지원·생성 등 나머지 355개는 별도 인덱스.
- 프리팹 8,689개, 씬 127개, .asset 1,386개, 머티리얼 744개. 공급사 데모/템플릿/WIP도 포함한 파일 수이지 실사용 콘텐츠 수가 아니다.
- 텍스트 직렬화 파일 10,986개에서 GUID·m_Script와 파일 간 참조를 추출했다.
- 자사 스크립트별 경로·행수·SHA256·타입·메서드·소스 이름 참조·직렬화 자산·지정 씬 참조 그래프를 만들었다.
- GUID 형식 불량 meta 0, 중복 GUID 0, 자산이 없는 고아 meta 타깃 0. YAML 전체 문법/Unity import 성공을 뜻하지 않는다.
- 미해결 GUID 참조 쌍 1,433개: 공급사/WIP/업그레이더 메타 포함. ‘현재 플레이 오류 1,433개’라고 읽지 않는다. 기능 필드와 실제 루트 연결을 구분해 우선순위를 정했다.

## 수동으로 이어 본 핵심 경로

부팅/씬 준비, 런 수명주기, 홀수 격자/탈출, 방/레이드 시간, 가스/감염, 피해/근접, AI/장비, 인벤토리/루팅, 상점/퀘스트/창고, 저장, UI/현지화, 오디오, 소켓, 에디터 자동화 경로를 관련 코드와 자산으로 대조했다. 866개 전체를 한 줄씩 수동 실행·리뷰한 것으로 과장하지 않는다.

## 이번 실행 검증

Unity 설치의 Roslyn 및 현재 Bee response file로 Assembly-CSharp와 Assembly-CSharp-Editor 컴파일을 감사 output으로 별도 실행했다. 둘 다 종료 코드 0. 런타임 경고 15개(TMP 구식 API/미사용 필드 등), 에디터 출력 경고 없음.

Player 빌드, 모든 테스트 재실행, 전 자산 임포트, 전 프리팹 육안 검사, 모든 오디오 청취, 모든 텍스트 화면 검사는 이번에 수행하지 않았다. 현재 열린 Unity 씬을 저장/전환하거나 실세이브를 초기화하지 않았다.

## 발견한 우선 문제

Build Settings 전 씬 비활성, 메뉴 Addressables 이전 주소/참조, RuntimeContentCatalog의 잘못된 YAML와 누락 GUID, 초기 장비/상점 누락 참조, DefaultVolumeProfile 테스트 컴포넌트 잔여, 루팅 중간 저장 일관성 위험이 있다. 아래 빌드·테스트 문서에서 증거와 후속 확인을 분리한다.

## 문서와 실제 파일의 관계

설명 본문은 동작/정책, 이 기술 부록은 소유권/파일/검증, 스크립트 카탈로그는 각 파일별 추출 증거다. 변경 후 인덱스 해시가 바뀌면 이 감사는 과거 스냅샷이 되므로 해당 시스템 문서를 다시 갱신한다.
`,['ProjectSettings/ProjectVersion.txt','ProjectSettings/EditorBuildSettings.asset','Packages/manifest.json','Tools/Audit20260922/inventory.cjs','Tools/Audit20260922/supplement.cjs']);

doc('ownership','3d03fd19-ccd6-8124-8644-f8a7d2710087','기술 A. 씬·서비스·SO·런타임 데이터 소유권',`
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
`,['RuntimeSceneDependencyValidator.cs','InitializationLoader.cs','SceneLoader.cs','RunLifecycleCoordinator.cs','Assets/Scenes/Manager/PersistenceManager.unity','Assets/Scenes/Manager/GamePlay.unity','Assets/Scenes/RUNScene.unity']);

doc('build','3d03fd19-ccd6-8191-bfab-d97c4fe7dd05','기술 B. Unity·빌드·자산 연결 문제와 근거',`
## 확인한 환경

Unity 6000.0.47f1, URP 17.0.4, Input System 1.14.2, Localization 1.5.9, AI Navigation 2.0.12, Cinemachine 3.1.4, Test Framework 1.5.1. Easy Save 3는 프로젝트 공급사 코드로 포함된다. 버전은 현재 파일 기준이며 과거 Notion 숫자를 복사하지 않았다.

## 우선순위 1 — 부팅/빌드

EditorBuildSettings에는 Initialization/RUNScene/PersistenceManager/GamePlay/GameOver 5개가 있고 모두 enabled:0이다. 기본 Build Settings 기준 활성 부팅 씬이 없다. 별도 빌드 스크립트가 씬을 명시하는 예외를 확인하지 않은 채 출시 가능하다고 결론내리지 않는다.

메뉴 Addressables 그룹에는 Assets/Scenes/MainMenuScene.unity 주소와 GUID 93f36624d85896646a4a01afa034899e 항목이 남아 있지만 현재 메뉴 파일은 MainMenu.unity다. 메뉴 SceneSO/Addressable 엔트리/실제 GUID를 한 세트로 복구하고 독립 Player에서 부팅해야 한다.

## 우선순위 1 — 런타임 콘텐츠 카탈로그

Assets/Resources/RuntimeContentCatalog.asset 16행에 '+- {fileID: ...}' 형태의 잘못된 YAML 줄이 실제 존재한다. 컴파일러는 .asset 문법을 검사하지 않으므로 C# 컴파일 성공과 양립한다.

같은 카탈로그에 25개의 미해결 GUID 참조 쌍이 있다. 예: 59d36ae99b6e4d539f72ccff0dc92d7e. 이 GUID는 Player.prefab의 equippedBackpack과 초기 로드아웃에도 남아 있다. 8261ecedbebf48c4ebbde438e62a742b도 초기 로드아웃/카탈로그에서 해석되지 않는다. 카탈로그 재구성과 항목별 실제 타입/저장 호환 확인이 필요하다.

HomeShopConfig는 a8c7bc4bfdd34874cb8cea35a09a004a, 75e18ec3889747049b9209e8710ab651 미해결 참조가 있다. 모든 누락 항목을 임의의 다른 아이템으로 대체하지 않는다. 원래 의도와 저장 ID를 확인한다.

## 우선순위 2 — 렌더링/임포트

DefaultVolumeProfile의 CopyPasteTestComponent1/2/3, VolumeComponentSupportedOnAnySRP 등 테스트/에디터 타입과 fileID 0, 미해결 GUID 4개를 확인했다. URP 프로필의 유효 런타임 컴포넌트만 남기는 별도 정리가 필요하다.

열린 Unity Editor 로그 스냅샷에는 폴더 meta YAML 오류와 EditorUpdateCheck 네트워크 오류가 있었다. 하지만 현재 확인한 일부 해당 meta 본문은 정상 형태였고 재임포트는 하지 않았다. 과거 로그 기록을 현재 모든 파일이 계속 깨졌다는 확정으로 쓰지 않는다. 새 Refresh/임포트의 최신 로그 시점을 기준으로 재검증한다.

## 미해결 GUID 수의 해석

전체 1,433쌍은 파일별 고유 GUID 참조 수다. Vendor 데모, WIP, 이전 머티리얼 업그레이더 데이터가 포함되어 있으므로 일괄 자동 삭제/교체하면 안 된다. 실행 루트·Resources·Addressables의 기능 필드부터 우선 분류한다.

## Git 기준선

감사 시작부터 다수의 삭제/수정/미추적 상태가 있었다. 이를 이번 감사 변경으로 오인하거나 reset/clean으로 정리하지 않았다. 안정된 커밋/백업 기준점을 먼저 확보하고 기능 수정은 작은 단위로 검증해야 한다.

## 검증의 한계

Assembly-CSharp와 Editor 컴파일은 통과했다. Player 빌드·Addressables 콘텐츠 빌드·전 자산 재임포트는 미실행이다. 부팅/카탈로그 문제를 해결하기 전 ‘시스템 전체 완료’ 또는 퍼센트 완료율을 승인 지표로 삼지 않는다.
`,['ProjectSettings/EditorBuildSettings.asset','ProjectSettings/ProjectVersion.txt','Packages/manifest.json','Assets/Resources/RuntimeContentCatalog.asset','Assets/Prefabs/GameScene/Shooter/Player/Player.prefab','Assets/ScriptableObject/GameScene/Character/Shooter/Player/Config/PlayerBasicShooterLoadout.asset','Assets/ScriptableObject/GameScene/Shop/HomeShopConfig.asset','Assets/Settings/DefaultVolumeProfile.asset']);

doc('tests','3d03fd19-ccd6-81bc-ad6d-c96f0a3b889a','기술 C. 검증 기록·승인 테스트·남은 범위',`
## 이번 감사에서 수행

- Assets 파일 목록/확장자, meta GUID 형식·중복·실제 대상 존재 확인.
- 직렬화 GUID/m_Script 연결 및 7개 게임 씬 루트 참조 그래프 생성.
- 자사 스크립트 866개 타입/메서드/참조/해시 인덱스, 외부/지원 355개 파일 인덱스.
- 대표 실행 경로와 Stage/Item/Quest/Shop/Audio/Localization SO 대조.
- Unity 설치의 Roslyn + 현재 Bee rsp로 런타임 및 에디터 어셈블리 별도 컴파일: 종료0. 런타임 경고15, 에디터 경고 출력0.
- 실세이브 삭제, 프로덕션 코드 수정, 열린 씬 저장, 임의 에디터 복구 도구 실행은 하지 않음.

## 존재하지만 이번에 재실행하지 않은 테스트 소스 10개

RunLifecycleAndSaveTests, RaidRuntimeContractTests, RaidMapFoundationTests, PlayerEmergencyMeleeTests, ObstacleBulletImpactAudioTests, ItemMakerWeaponCategoryTests, ItemMakerBatchTests, ItemHandAndCategoryTests, InventorySortUIOwnershipTests, AudioGroundworkTests.

테스트 파일 존재와 이번 테스트 통과는 다르다. 과거 대화의 통과 개수를 이번 결과로 재사용하지 않는다.

## 앞으로의 승인 게이트

1. Boot: 깨끗한 Player 실행에서 메뉴→Home→RUN→귀환→재출정. Missing Reference 및 무한 로딩 없음.
2. Raid: 5스테이지 여러 시드에서 크기/중앙/사분면/최소거리/보스 중복 없음. 저장 후 배치 안정.
3. Extract: 시작 귀환·일반 탈출·배터리 부족/성공·해치 잠김/개방·특별 탈출 비활성.
4. Combat: 실드/방탄복/기본 방탄 순서, 파손, 감염≥HP, 무기 없음/탄 없음 근접, 사용 중단.
5. Inventory: 수량/내구/장착 유지, 정렬, 드래그/거래 충돌, 루팅 수령 중 저장 중복 방지.
6. Progress: 킬/명중/수집 필터, 보상 1회성, 해금 유지, 창고 확장 후 데이터 보존.
7. Save: 구버전·잘린 본파일·백업 실패·누락 GUID·사망·강제종료 경계. 테스트용 저장으로 실행.
8. UI: KO/EN·폰트·길이·키 변경·설정 재시작·오버레이 입력 복귀·중복 정렬 버튼 없음.
9. Audio: Master/Music/SFX 실제 적용, 수색 취소 루프 정리, 표면 Cue, NPC 임시 클립 교체.
10. Visual/Build: 실제 리그/장비 장착, 렌즈·머티리얼, 13×13 성능, URP Player 출력.

## 결과 기록 양식

테스트명 / 기준 문서 / Git 또는 파일 해시 / 씬·seed·stage / 입력 / 예상 / 실제 / 로그·스크린샷 / 통과 여부를 남긴다. 문제를 수정하지 않았으면 검증 실패 또는 미검증으로 유지한다. 특히 P12 성격의 검증 항목은 자동 패치 작업으로 바꾸지 않는다.

## 정적 인덱스의 한계

이름 기반 소스 참조는 주석/동명 토큰을 포함할 수 있다. 직렬화 그래프는 비활성 객체도 포함하며 동적 로딩을 모두 증명하지 못한다. 이 인덱스로 파일별 조사 위치를 찾고 실제 기능 판정은 소유권·생성 경로·플레이 결과로 확정한다.
`,['RunLifecycleAndSaveTests.cs','RaidRuntimeContractTests.cs','RaidMapFoundationTests.cs','PlayerEmergencyMeleeTests.cs','InventorySortUIOwnershipTests.cs','AudioGroundworkTests.cs']);

doc('backlog','2523fd19-ccd6-81d5-aa9b-c5b9880f119d','작업 순서·미완성 목록 | 현재 프로젝트 기준',`
## 1. 안정된 시작점 — 먼저 해결

- Git/백업 기준선 확정. 기존 대량 사용자 변경을 잃지 않는다.
- 부팅 씬 활성/빌드 경로, 메뉴 Addressables 참조 일치.
- RuntimeContentCatalog YAML와 누락 항목, 플레이어 초기 장비·Home 상점 참조 정리.
- DefaultVolumeProfile와 최신 Unity 임포트 오류 확인.

완료 조건: 새 Player 빌드에서 메뉴→Home→RUN→귀환→재출정 가능, 필수 카탈로그 로드와 참조 검사 통과. 단순 C# 컴파일 성공은 부족하다.

## 2. 진행을 잃거나 복제하지 않게 만들기

- 루팅 수령/창고 이동/퀘스트 제출·보상/배터리 소비의 저장 단위 확정.
- 중간 OnChanged 저장의 중복 위험 재현·수정·회귀 테스트.
- 기존 세이브 이전, 누락 GUID, 백업 3세대, 사망 복원, 종료 경계 테스트.
- 전투 중 이어하기에서 무엇을 복원하고 무엇을 재생성하는지 명시.

## 3. 현재 맵 역할을 실제 콘텐츠로 연결

- Armory/Medical/Vault/Generator/Hatch 전용 프리셋·Encounter·Content 미연결 채우기.
- 프로토타입 단말을 실제 외형/상호작용으로 교체.
- 일반 탈출/허브/해치 지도와 전력 상태 확인.
- 특별 탈출은 현재 WIP 유지. 조건 합의 전 활성화하지 않음.

## 4. 전투·장비 제작

- 실더 보스 프리팹에 실드 계층 연결과 파손 연출.
- 플레이어/적 소켓 리그 연결, Face 하위 숨김과 Hair 독립 확인.
- 총·탄 패밀리·tier·적 로드아웃·루팅 조율.
- 무기/방탄 내구 주기, 응급 근접, 의료 소모품 효과·시간·상태이상 조정.
- 고단계/Stage5 템플릿 보스와 엔딩 관련 콘텐츠 구체화.

## 5. 퀘스트와 경제 — 초안과 활성 분리

- RaidProgressionDraft의 0/빈 항목을 실제 요구량·금액·퀘스트 참조로 확정.
- Normal→Rare→Unique→Legendary 제출, 보스+금액의 해금 순서 설계.
- LV1~6 상점 해금과 LV7~8 특수 획득 제한을 실제 표/보상과 검증.
- 방탄·주사기·탄약·스탯·창고 업그레이드 퀘스트 연결.
- 초기 가방, 재화 수급/지출, 상점 재고·수리·루팅 비율 검수.

## 6. 표시·음향·마감

- UI_Settings_General 중복/빈 표와 하드코딩 문구 정리, KO/EN 화면 테스트.
- 실제 소켓/머티리얼/폰트/정렬 버튼 위치 확인.
- NPC 임시 알림 → 실제 대사/목소리, 수색·UI·충돌음 음량 조정.
- 실제 최대 맵 성능, Player 빌드/재시작 연속 테스트.

## 하지 않은 일을 완료로 표시하지 않기

위 항목은 이번에 패치한 목록이 아니라 현재 감사 기반의 남은 작업이다. 새 시스템을 병렬로 계속 만드는 것보다 기존 책임·SO 연결과 검증을 닫는 순서다. ‘70~80% 완성’ 대신 각 승인 조건 통과 여부를 기록한다.

## Notion 작업 카드 필수 필드

목표 / 현재 동작 / 변경 후 동작 / 비범위 / 소유 시스템·파일 / SO·프리팹 / 저장 영향 / UI·현지화·오디오 영향 / 승인 테스트 / 결과 증거. 상태는 제안→확정→구현중→연결확인→검증완료로 구별한다. 이 상태 정책은 앞으로의 작업 방식 제안이며 런타임 기능이 아니다.
`,['Assets/Resources/RuntimeContentCatalog.asset','ProjectSettings/EditorBuildSettings.asset','RaidRuntime.cs','QuestRuntime.cs','ShooterVisualRig.cs','ShieldArmorRuntime.cs']);

doc('scriptindex',null,'스크립트 전수 인덱스·감사 원본 | 1,221개',`
## 파일별 조사 자료

이 페이지의 첨부 자료는 2026-09-22 현재 파일 스냅샷이다. 자사 소스 866개는 타입·메서드·직렬화 참조·코드 이름 참조·씬 참조 그래프·해시를, 나머지 355개는 경로·행수·해시를 기록했다. 총 Assets C# 1,221개다.

자사 분류는 Assets/Scripts, Assets/Editor, Assets/Tests 기준이다. 그 밖의 AssetSupportFile/EditorTool에도 커스텀 지원 코드가 섞일 수 있으므로 ‘나머지=전부 공급사 원본’이라고 단정하지 않는다. 외부/지원 영역의 구현 전체를 수동 정밀 리뷰한 것은 아니다.

## 인덱스 읽기

- Core: 씬 전환, 입력, 저장, 이벤트, 공통 서비스.
- Gameplay: 엔티티, 무기/아이템, 전투/감염, 섹터/맵, 퀘스트/진행.
- Presentation: HUD/패널/설정/지도, 카메라, 오디오.
- Authoring: 제작·검사·디버그 에디터 도구.
- StateMachine: 행동/조건/전이 공통 기반.
- Other: 위 모듈 밖 자사 소스와 Assets/Editor, Tests.
- External-and-generated: 공급사/지원/생성 소스 파일 목록.

## 첨부 원본 내용

scripts.json에는 전체 자사 파일의 자세한 참조와 해시가 있다. refs.json/guids.json은 자산 그래프와 GUID→경로, missing.json은 미해결 GUID 쌍, summary.json/supplement.json은 범위·KO/EN 표·Cue·우선 누락 목록이다. 사람이 읽는 모듈별 Markdown도 함께 제공한다.

참조 없음은 삭제 허가가 아니다. RequireComponent·AddComponent·Resources·Addressables·리플렉션·에디터 메뉴를 함께 본다. 참조 있음도 실제 활성 게임 콘텐츠라는 보증이 아니다. 기본 템플릿과 데모를 포함한 숫자를 ‘완성 아이템/기능 개수’로 사용하지 않는다.

## 업데이트 방법

현재 체크아웃에서 inventory.cjs와 supplement.cjs를 실행하면 Tools/Audit20260922/output에 감사 결과만 생성한다. 향후 새 날짜로 별도 스냅샷을 만들고 현재 문서의 근거/검증일을 갱신한다. 이 스크립트는 Unity 자산을 자동 복구하거나 삭제하지 않는다.
`,['Tools/Audit20260922/inventory.cjs','Tools/Audit20260922/supplement.cjs','Tools/Audit20260922/output/scripts.json']);

doc('hub','39e3fd19-ccd6-8174-813c-cc0cf073a89b','Board Chase | 현재 프로젝트 기준서 (2026-09-22)',`
## 이 문서가 기준이다

현재 체크아웃을 다시 조사해 기존 아키텍처/브레인스토밍/옛 시스템 설명을 교체했다. 과거 페인트 점유 중심 기획, 전역 감염통제력·숫자 지갑·메타 구매를 현재 게임 핵심으로 쓰지 않는다. 다만 Unity에 실제 남은 연결은 WIP/은퇴 경계 문서에 사실대로 기록한다.

이 페이지는 앞으로 Notion에서 변경안을 정하고 Unity에서 구현할 때의 출발점이다. 본문은 플레이 흐름과 시스템 규칙, 기술 부록은 코드·자산·빌드·검증, 파일 인덱스는 조사 증거다.

## 현재 게임의 흐름

Home 준비 → 스테이지 선택 → 시작 허브(-1,0) → 홀수 격자 레이드 → 방 전투/수색/장비 관리 → 일반 탈출 또는 전력 해치 → Home 귀환/퀘스트·창고 진행. 사망은 정상 귀환과 다른 런 정리 경로다.

## 읽기 순서

{{overview}}
{{flow}}
{{combat}}
{{items}}
{{loot}}
{{progression}}
{{save}}
{{ui}}
{{audio}}
{{visual}}
{{render}}
{{tools}}
{{backlog}}
{{technical}}
{{scriptindex}}

## 현재 상태를 읽는 네 가지 구분

- 코드·데이터 확인: 현재 소스와 SO/프리팹의 경로를 대조했다. 플레이 승인과는 다름.
- 기반만 있음/미연결: 클래스와 설정은 있으나 실제 자산 연결이 비어 있거나 확인되지 않음.
- WIP/은퇴: 코드가 남았지만 현재 정책의 메인 실행 경로가 꺼져 있음.
- 검증 필요: 테스트/실빌드/실제 화면·청취를 아직 통과했다고 말할 수 없음.

## 핵심 상태 요약

레이드 격자·중앙 해치·발전/탈출·이중 시간·개인 감염·다층 방탄·비상 근접·물리 화폐·인벤토리·퀘스트/저장 기반이 있다. 반면 새 퀘스트 라인, 역할별 전용 방, 소켓 리그 실연결, 실더 보스, 특별 탈출, 번역/오디오 최종 마감은 완료로 볼 수 없다.

## 가장 먼저 볼 위험

현재 Build Settings의 씬 전부 비활성, 메뉴 Addressables 이전 참조, RuntimeContentCatalog의 YAML/누락 GUID, 초기 장비/상점 참조, DefaultVolumeProfile 잔여 테스트 타입, 루팅 이동 중간 저장 위험을 확인했다. 세부 콘텐츠를 더 쌓기 전에 이 기준선을 안정화하는 것이 우선이다.

## 점검 범위와 한계

Assets C# 1,221개 중 자사 866개(168,936행)의 정적 인덱스와 전체 자산 GUID 연결을 만들고 핵심 동작 경로를 수동 대조했다. 외부/지원 355개는 별도 목록이다. 런타임/에디터 C# 컴파일은 통과했다. 모든 소스를 한 줄씩 수동 검증하거나 모든 씬을 플레이·모든 프리팹을 눈으로 검사한 작업은 아니다. Player 빌드와 테스트 전체 재실행도 미실행이다.

## 앞으로의 변경 규칙

현재 동작을 수정하기 전에 변경안/소유자/저장 영향/승인 테스트를 기록한다. 구현과 자산 연결 후 실제 검증 결과를 남기고 이 기준서를 갱신한다. 과거 완료율이나 콘셉트 이미지를 실제 Unity 적용 증거로 대신하지 않는다.
`,['Tools/Audit20260922/output/summary.json','Tools/Audit20260922/output/supplement.json']);

const mapPath='Tools/Audit20260922/notion-page-map.json';
if(fs.existsSync(mapPath)){
 const links=JSON.parse(fs.readFileSync(mapPath,'utf8'));
 for(const p of pages){p.id=links.find(x=>x.key===p.key)?.id||p.id;p.content=p.content.replace(/\{\{(\w+)\}\}/g,(_,key)=>'<mention-page url="https://www.notion.so/'+links.find(x=>x.key===key).id.replaceAll('-','')+'"/>');}
}
fs.mkdirSync('Tools/Audit20260922/pages',{recursive:true});
for(const p of pages)fs.writeFileSync('Tools/Audit20260922/pages/'+p.key+'.md','# '+p.title+'\n\n'+p.content);
fs.writeFileSync('Tools/Audit20260922/output/notion-pages.json',JSON.stringify(pages,null,2));
console.log(JSON.stringify({pages:pages.length,chars:pages.reduce((n,p)=>n+p.content.length,0),new:pages.filter(p=>!p.id).map(p=>p.key)},null,2));
