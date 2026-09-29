// Current-source documentation, not a gameplay patch. Notion publishing is separate.
const fs=require('fs'),path=require('path');
const out='Tools/Audit20260922/output';
const scripts=JSON.parse(fs.readFileSync(out+'/scripts.json','utf8'));
const pages=[];
function doc(key,id,title,body,sources=[]){
 const refs=sources.map(n=>{const s=scripts.find(s=>s.path.endsWith('/'+n));return s?s.path:n;});
 pages.push({key,id,title,content:'검사 기준: 2026-09-22 / E:/github/board_chase/board_chase. 현재 파일의 정적 연결·핵심 실행 경로 기준이며, 별도 표시 없는 항목은 이번 감사에서 플레이 테스트한 결과가 아닙니다.\n\n'+body.trim()+'\n\n## 코드·데이터 근거\n\n'+refs.map(p=>'- `E:/github/board_chase/board_chase/'+p+'`').join('\n')});
}
doc('overview','3243fd19-ccd6-80ad-8f19-e798478236bd','현재 게임 한눈에 | 레이드·파밍·귀환',`
## 게임의 현재 골격

Board Chase는 Home에서 장비와 소모품을 준비하고, 해금한 스테이지에 들어가 방 단위 전투·수색·파밍을 수행한 뒤 탈출하여 영구 진행을 쌓는 싱글플레이 구조다. 전투 중 HP, 개인 감염, 탄약, 장비 내구도, 가방 공간, 방 시간, 전체 레이드 시간을 함께 관리한다.

현재 게임 설명의 중심은 ‘백신 페인트 점유율을 높여 스테이지를 확장한다’가 아니다. 그 설명은 이 기준서에서 폐기한다. 단, 페인트의 일부 실제 코드와 프리팹 연결은 남아 있으므로 기술적 제거 완료로 해석하지 않는다.

## 플레이 흐름

1. Initialization → 지속 매니저 로드 → MainMenu.
2. 새 게임/이어하기 → GamePlay 관리·UI 씬과 Home 환경 준비.
3. Home: 창고, 상점, 퀘스트 NPC, 스테이지 선택.
4. 레이드: 외부 시작 허브 (-1,0) → 첫 격자 방 (0,0) → 전투/수색/아이템 소비/장비 교체.
5. 일반 탈출구 또는 발전기 가동 후 중앙 해치로 귀환. 잘못 입장한 경우 시작 허브 귀환도 가능.
6. 퀘스트 보상과 해금·창고는 영구 진행, 이번 런의 소지품·상태·맵은 런 저장에 속한다. 사망과 정상 귀환을 같은 처리로 취급하지 않는다.

## 지금 확정해서 설명할 수 있는 규칙

- 맵 크기: 1~5스테이지 7/9/11/13/13 정사각 격자.
- 중앙 해치 고정, 발전기는 좌상단 또는 우하단 사분면, 보스 후보와 일반 탈출구는 우상단 서로 다른 칸.
- 보스 출현 확률 65%. 보스 처치가 일반 탈출의 필수 조건은 아니다.
- 전체 레이드 시간은 현재 모든 스테이지 1200초. 초과 시 개인 감염을 올리는 추가 가스가 적용된다.
- 실드·방탄복·기본 방탄은 합산 등급이 아니라 순차 피해 계산 구조. 실제 실더 보스 연결은 별도 미완성.
- 플레이어 비상 근접은 무기 없음/발사 가능한 탄약 없음일 때의 약한 고정 피해 수단. 별도 근접무기 콘텐츠는 WIP.
- 숫자형 별도 지갑 대신 인벤토리의 CurrencyItemSO 물품을 거래에 사용한다.

## 완료라고 쓰면 안 되는 부분

- 새 메인 퀘스트 단계별 제출·보스·돈 요구는 초안 자산이며 실플레이 퀘스트 연결이 끝난 상태가 아니다.
- 무기고/의무실/금고의 역할과 루팅 데이터는 있지만 역할별 전용 방 형태·적 조합은 빈 참조가 남아 있다.
- 특별 탈출구는 3스테이지부터 사용할 수 있도록 틀만 있으며 현재 비활성.
- 비주얼 소켓, 실드, 번역, 보스 밸런스, 빌드/복구 검증은 각각 별도의 남은 일이 있다.

## 기준서 사용법

현재 상태를 읽고 나서 변경안을 별도 항목으로 정의한다. 원하는 미래 규칙을 이미 구현된 것처럼 본문에 섞지 않는다. 작업 완료는 코드 존재가 아니라 자산 연결과 승인 테스트까지 확인한 뒤 표시한다.
`,['InitializationLoader.cs','RunLifecycleCoordinator.cs','RaidRuntime.cs','RaidMapGenerator.cs','PlayerEmergencyMeleeProfile.cs','Assets/ScriptableObject/GameScene/Stage/RaidProgressionDraft_WIP.asset']);

doc('flow','2523fd19-ccd6-8153-acdd-eb0282d0f20a','01. 게임 흐름·레이드 시스템 | 현재 구현',`
## 소유권을 먼저 나눈다

- SceneLoader: 어떤 관리/환경 씬이 준비되어야 하는지, 로딩 화면과 플레이 준비 시점을 담당.
- RunLifecycleCoordinator: Home/활성 런/중단 런/완료/사망 수명주기, 저장·복원 조율 담당.
- StageProgressionManager: 선택한 스테이지, 방 전투 완료와 방 시간, Home 복귀 담당.
- RaidMapGenerator: 한 런의 격자와 역할 배치. RaidRuntime: 시간·발전·해치·탈출·컨테이너 복원 상태.
- SectorStateManager / SectorPortalManager: 방 좌표·상태와 포탈 이동. UI는 이벤트로 이를 읽는다.

## 시작과 복원

InitializationLoader가 지속 매니저를 먼저 올린다. SceneLoader는 환경을 단순히 연 다음 바로 조작을 허용하지 않고 맵·서비스·플레이어·카메라 등 준비 상태를 기다린다. 준비 게이트는 안정 프레임 확인과 타임아웃 경로가 있다. 로딩 실패를 정상 시작으로 숨기지 않는 구조다.

새 게임은 저장 초기화와 시작 데이터를 거친다. 이어하기는 SaveSystem의 파티션과 플레이어 런 스냅샷을 읽고, 실제 플레이어와 방이 준비된 뒤 복원을 적용한다. Home 환경은 관리 씬과 함께 유지되는 경로가 있으므로 RUNScene 하나가 전체 게임을 모두 소유하는 구조가 아니다.

## 런 진입

선택 스테이지의 정의 → 결정적 시드와 OddSquareRaid 정책 → 역할 좌표 확정 → 섹터 프리팹/콘텐츠/전투 설정 선택 → 방 생성·등록 → 내비게이션 준비 → 발전기/탈출 단말·루팅 연결 순서다.

시작 허브 좌표는 (-1,0)이다. 맵 내 (0,0)과 혼동하지 않는다. 시작 허브에는 귀환 수단을 두는 경로가 있어, 스테이지를 잘못 골랐거나 가스 준비가 안 된 플레이어가 즉시 돌아올 수 있다.

## 런 진행과 종료

- 방 전투 완료는 현재 적 처치 완료 판정이 우선이며 옛 점유율 판정을 핵심 규칙으로 설명하지 않는다.
- 방 제한시간과 전체 레이드 제한시간은 별도 상태다.
- 발전기는 요구 배터리를 소비해 전력을 켜며, 그 런의 해치가 사용 가능해진다.
- 일반 탈출/시작 귀환은 보스 처치 여부를 요구하지 않는다.
- 탈출은 중복 커밋을 막고 Home 전환을 요청한다. 전환이 거부되면 커밋 플래그를 되돌린다.
- 사망은 런 소지품·상점 런 상태·플레이어 스냅샷을 정리하며, 정리 직후 살아 있는 상태를 다시 캡처하지 않도록 억제한다.

## 읽는 순서

아래 하위 문서는 맵 → 개인 감염 → 두 종류 타이머 → 적 → 보스 → 방 콘텐츠 → 은퇴/WIP 경계 순서다. 옛 브레인스토밍 본문과 백업 기획은 현재 기준 설명으로 교체했다.
`,['SceneLoader.cs','RunLifecycleCoordinator.cs','StageProgressionManager.cs','StageSectorInstantiator.cs','RaidRuntime.cs']);

doc('map','3763fd19-ccd6-8188-a965-eebb6d250d4a','맵·포탈·탈출구 | 좌표와 생성 규칙',`
## 좌표와 크기

Unity 내부 좌표는 0부터 시작한다. N은 홀수이며 중앙 c=(N-1)/2이다. 사용자 설명의 7×7 중앙 (4,4)는 내부 (3,3)이다. 9×9는 (4,4), 11×11은 (5,5), 13×13은 (6,6)이다.

1~5스테이지 N은 7,9,11,13,13. 외부 시작 허브 (-1,0)는 N×N 면적에 포함하지 않는다.

## 특수 방 배치

- 중앙 해치: (c,c) 한 칸 고정.
- 우상단 후보 영역: x>c, y>c. 일반 탈출구와 보스 후보를 서로 다른 칸에 배정한다.
- 발전기 후보: x<c,y>c 또는 x>c,y<c. 중앙 행·열은 제외한다. 후보 영역 안의 한 위치를 선택한다.
- 탈출 위치 선택은 시작점으로부터 경로 거리 기준도 사용한다. 단순 화면상 직선 거리와 구별한다.
- 보스 출현은 시드 기반 65%. 불출현 시 보스 후보의 역할 정보와 실제 보스 전투 유무가 같다고 가정하지 않는다. 코드에서는 후보 역할을 유지하되 일반 방 종류로 처리하는 경로가 있다.
- 특수 위치를 예약한 뒤 나머지 방에 Normal/Armory/Medical/Vault 역할을 배분한다. 기본 가중치는 70/10/15/5다.

## 재생성과 이어하기

탈출 위치/보스 위치/보스 확률/발전기/역할 배분의 난수 salt가 분리되어 있다. 생성 버전은 1이며, 확정된 배치와 해결 상태를 저장한다. 같은 런에서 메뉴 재진입이나 복원 때문에 보스 확률과 발전기를 새로 추첨하면 안 된다.

## 탈출 정책

- 시작 허브 귀환: 허브 좌표에서 가능.
- 일반 탈출: 지정 위치에서 가능하며 보스 처치가 선행 조건이 아니다.
- 해치: 중앙 위치 + 해당 런의 발전기 가동이 필요.
- 특별 탈출: 3스테이지 이상용 설정 틀은 있지만 현재 비활성이고 사용 허가 경로도 열려 있지 않다. ‘이미 세 번째 탈출구가 작동한다’고 쓰지 않는다.

## 지도 표시

시작 지점·일반 탈출구·발전기와 실제 출현 보스는 지도 표시 대상이다. 해치는 중앙으로 고정되어도 발전 전에는 위치 공개/사용 가능 상태가 열리지 않는 정책이다. 전력 투입 후 공개한다. 특별 탈출구는 향후 숨김 정책을 붙일 대상이며 현재 활성 콘텐츠가 아니다.

## 포탈과 방의 관계

SectorPortalManager는 이웃 좌표 이동과 현재 방 변경 알림을 담당한다. 방 입장 제한·전투·시간·탈출 조건을 포탈 메시 자체에 복제하지 않는다. SectorStateManager가 가진 방 상태와 이벤트를 소비한다.

## 승인 테스트

5개 스테이지×여러 시드에서 중앙 고정, 사분면, 겹침 없음, 시작 거리, 65% 추첨의 재진입 안정성, 허브 복귀, 지도 공개 시점을 검사해야 한다. 이번 감사는 생성 코드와 직렬화 설정 대조이며 대량 시드 테스트 재실행 결과는 아니다.
`,['RaidMapGenerator.cs','RaidMapSettings.cs','RaidRuntime.cs','SectorPortalManager.cs','SectorStateManager.cs']);

doc('infection','3763fd19-ccd6-81d3-93e3-f0b3dff27e8c','개인 감염·가스·방독면 | 현재 규칙',`
## 개인 감염은 현재 시스템이다

ShooterInfection은 개인 감염 수치와 사망 조건을 관리한다. 전역 감염통제력과 다른 시스템이다. 현재 HP가 감염의 치명 임계값이다. 감염이 남은 HP에 도달하면 InfectionOvertake로 사망한다. 따라서 HP 피해를 받아 HP가 이미 쌓인 감염 이하로 내려가도 즉시 사망할 수 있다.

사망 판정을 자연회복보다 먼저 수행하고 부동소수점 임계 오차도 처리한다. 감염을 단순히 최대 HP 기준의 별도 체력바로 설명하면 잘못이다.

## 플레이어 회복

PlayerShooterInfection은 AllowsNaturalRecovery=false로 오버라이드한다. 공통 감염 클래스의 자연회복 코드가 있다고 플레이어도 가만히 서 있으면 회복한다고 쓰지 않는다. 감염 치료 아이템처럼 명시적인 행동이 필요하다. 섹터 점유/Named 처치/보스 처치의 옛 자동 회복 훅도 플레이어에서는 비어 있다.

## 방 가스와 전체 레이드 가스

현재 방의 기본 가스 + 방 시간 초과 누출 + 레이드 전체 시간 초과 가스가 플레이어 노출률에 반영된다. PlayerShooterInfection이 tick마다 raw exposure를 계산하고 장착 방독면 필터 처리 후 남은 양을 개인 감염에 더한다. 이 방 가스 경로는 플레이어 전용이다.

- 스테이지1: 기본 0, 방 누출 6/초, 최대 2중첩, 보스 방 적용 꺼짐.
- 스테이지2: 기본 0, 누출 8/초, 최대 3중첩, 보스 방 적용 꺼짐.
- 스테이지3: 기본 8/초, 누출 5/초, 최대 3중첩, 보스 방 적용 켜짐.
- 스테이지4: 기본 12/초, 누출 8/초, 최대 3중첩, 보스 방 적용 켜짐.
- 스테이지5: 기본 17/초, 누출 10/초, 최대 4중첩, 보스 방 적용 켜짐.
- 현재 설정의 가스 tick은 0.5초. 전체 레이드 시간 초과 추가 감염률은 6/초.

위 수치는 현재 SO 값이지 최종 밸런스 보증이 아니다. ‘피해 6 HP/초’가 아니라 필터 처리 대상인 감염 노출률이다.

## 페인트 잔여 연결

RUNScene의 MaskRenderManager, Sector_Base의 SectorPaint/SectorOccupancy와 전투·감염·시야 코드 연결이 남아 있다. Vaccine/Virus/PoisonPuddle 채널의 GPU 마스크 및 CPU 샘플링은 기술적 현재 상태로 별도 관리한다. 옛 페인트 점유 중심 게임 기획은 폐기하되, 이 연결을 무시하고 코드를 바로 삭제하면 현재 전투·시야에 영향을 줄 수 있다.

InfectionControlManager의 전역 통제력 경로는 retiredWip 기본값과 수명주기 가드로 은퇴 처리되어 있다. 개인 감염까지 은퇴한 것은 아니다.
`,['ShooterInfection.cs','PlayerShooterInfection.cs','SectorRoomTraitRuntime.cs','InfectionControlManager.cs','MaskRenderManager.cs','SectorPaint.cs']);

doc('timers','3763fd19-ccd6-8132-ad28-da0ff8b02a60','방 제한시간·레이드 시간·진행 판정',`
## 서로 다른 두 시계

방 제한시간은 현재 전투 방 진행을 관리한다. 레이드 시간은 전체 출정의 체류 시간이다. 하나의 PlayerTimer 이름으로 두 의미를 섞지 않는다.

현재 모든 스테이지의 일반 방 제한시간은 30초, 전체 레이드 제한시간은 1200초다. 방 시간은 StageProgressionManager, 전체 시간은 RaidRuntime이 담당한다. 런 시간은 Time.deltaTime으로 흐르므로 일시정지 및 게임이 꺼져 있는 동안 현실 시간만큼 계속 차감하는 방식이 아니다.

## 방 성공/초과

SectorRules의 적 처치 완료 우선 정책이 켜져 있다. HasCompletedEncounter가 현재 완료 판정의 우선 경로이며, 옛 점유율 조건은 호환/대체 경로로 남아 있다. 타이머 전투 판정 옵션도 존재하고 성공·실패 카운트다운 설정은 각각 5초다.

시간 초과는 방 가스 누출 중첩으로 연결한다. 옛 전역 Infection Control 게이지 감소 규칙을 여기에 쓰지 않는다. 방을 넘기는 특수 Skip 경로는 적과 대기 웨이브를 취소한 뒤 완료 처리하며, 취소를 적 처치나 드롭 보상으로 위장하지 않는다.

## 전체 시간 초과

RaidRuntime은 제한시간이 지나면 overtime 상태를 저장하고 추가 가스 노출률을 공급한다. 만료 즉시 게임을 종료하는 대신 감염 압박을 올리는 구조다. 10게임초 간격 체크포인트와 중요 상태 변경 시 저장 요청 경로가 있다.

## 해금과 클리어의 구별

런에서 탈출했다고 다음 스테이지가 자동 개방되는 구조로 쓰지 않는다. 현재 스테이지 해금은 퀘스트 보상 경로가 소유한다. Raid 정책에서 보스 출구에 의한 자동 스테이지 완료 경로는 허용하지 않는다. ‘방 전투 완료’, ‘보스 처치’, ‘레이드 귀환’, ‘다음 스테이지 해금’은 서로 다른 사건이다.

## 검증할 경계

일시정지 중 시계, 문 이동 중 시간, 방을 재방문했을 때 누출, 전체 시간 만료 후 저장/복원, 사망 중복 종료, Skip의 킬 퀘스트 비증가를 각각 테스트한다. 현재 저장 범위가 모든 방 전투의 순간 상태까지 완전 스냅샷하는 것은 아니므로 이어하기 세부 정책은 저장 문서를 함께 본다.
`,['StageProgressionManager.cs','RaidRuntime.cs','SectorRulesSO.cs','SectorRoomTraitRuntime.cs','Assets/ScriptableObject/GameScene/Stage/RaidProgressionDraft_WIP.asset']);

doc('enemies','3763fd19-ccd6-813e-ae3b-df72c396dcc6','일반 적·웨이브·AI·난이도',`
## 적은 설정·장비·행동을 분리한다

EnemyShooterConfig가 능력치와 적 역할을 정의하고, ShooterLoadout/LoadoutTable이 무기·탄·방탄 등 장착 구성을 공급한다. EnemyShooter 초기화는 설정 주입 → 로드아웃 선택 → 탄 호환 확인 → 장착 → 루팅 데이터 준비 순서를 갖는다. 시체 드롭 컴포넌트는 런타임 생성 경로도 있으므로 프리팹 직렬화 참조가 없다는 이유만으로 미사용으로 판단하지 않는다.

SectorEnemySpawner와 Encounter 설정이 현재 방의 스폰과 웨이브를 관리한다. SectorPreset은 지형/배치, Encounter는 적 전투 구성, ContentProfile은 방 콘텐츠 책임으로 구별한다.

## 전투 AI

EnemyShooterCombatController 계열은 순찰·소리 조사·최종 발견 위치 접근·추격·엄폐·사격과 근접/투척 상태를 조정한다. 무조건 플레이어 위치를 전역 공유하는 단일 추적기로 설명하지 않는다. EntityVisionController의 거리·방향각·가림 검사를 통해 감지하며, 소리 자극은 별도 입력이다.

플레이어 화면에 렌더링되는지와 AI가 적을 인지하는지는 다른 책임이다. VisionVisibilitySystem은 표시 정책이며 총격음/시각 인지의 대체물이 아니다.

## 난이도와 파밍 의도

DifficultyRulesSO와 스테이지별 적 설정/로드아웃/웨이브로 조절한다. ‘대부분 방탄보다 약한 탄, 드물게 좋은 탄’은 데이터 튜닝 방향이지 모든 적에 자동 적용되는 단일 전역 규칙이라고 보장할 수 없다. 현재 각 적의 탄 패밀리·tier·관통력·방탄과 드롭 표를 따로 검수해야 한다.

LootProgressionConfigSO는 source와 stageIndex별 override를 지원한다. stageIndex 0 기본 풀로 내려가는 fallback이 있으며, 이번에 본 주요 적 source 데이터는 기본 풀 중심이다. 코드가 스테이지별 표를 지원한다는 것과 모든 스테이지 표가 채워졌다는 것을 분리한다.

## 다른 적 코드

Creature/기존 StateMachine 행동·조건·전이 소스가 함께 남아 있다. 전부 Shooter로 교체됐다고 쓰지 않는다. 스크립트 인덱스의 실제 SO/프리팹 연결을 보고 현재 등장 콘텐츠인지, 도구/시험용인지, WIP인지 판단한다.

## 남은 제작

무기고 중무장 적, 의무실 일반 적, 스테이지별 약탄/희귀 강탄 비율, 엄폐 전투 체감, 방별 웨이브 밀도, 적 장착품과 시체 내용 일치가 콘텐츠 검증 과제다. 이번 감사에서는 실제 모든 적을 소환해 전투 시뮬레이션한 것은 아니다.
`,['EnemyShooter.cs','EnemyShooterCombatController.cs','SectorEnemySpawner.cs','EntityVisionController.cs','LootProgressionConfigSO.cs','DifficultyRulesSO.cs']);

doc('bosses','3763fd19-ccd6-81c5-957e-e3b6d0421a7b','보스·Named·실더 준비 상태',`
## 배치와 출현

보스 후보 방은 우상단 사분면에 일반 탈출구와 겹치지 않게 정한다. 출현 확률은 65%이며 한 런의 배치 해결 결과를 재사용한다. 보스가 없다고 일반 탈출구도 사라지거나 탈출할 수 없어지는 규칙은 아니다.

## 전투 데이터

BossConfig와 전용 프리팹/프리셋, NamedBattleManager 및 관련 컨트롤러 경로가 있다. Stage1 Revolver, Stage2 Sniper, Stage3 Rocket, Stage4 LMG, Stage5 Template 명칭의 자산과 Punisher 계열이 존재한다. 자산 이름의 존재만으로 5스테이지 완성 보스와 전투 밸런스가 모두 끝났다는 의미는 아니다.

‘스테이지 첫 개방 직후에는 매우 어렵고 파밍·퀘스트 성장 후 할 만하다’는 최종 튜닝 방향이다. 실제 체력·방탄·공격탄·엄폐·소모전 길이는 각각 SO에서 맞춰야 한다. 높은 HP만으로 전투 체감이 검증되었다고 쓰지 않는다.

## 보상과 진행

기존 Named 선택 보상 UI를 현재 핵심 보상으로 설명하지 않는다. 관련 레거시 보상 이벤트 경로는 억제되어 있고, 현재 검토 기준은 시체/루팅과 퀘스트 진행이다. 보스 처치에 의한 차기 스테이지 자동 해금 대신 퀘스트 보상의 StageUnlock을 사용한다.

## 실더

ShieldArmorRuntime은 방향 없는 추가 방탄층이다. 실드3+방탄복3+기본방탄1을 방탄7로 더하지 않는다. 실드 계산을 통과한 피해에 방탄복 계산, 다시 기본 방탄 계산을 적용한다. 실드에는 별도 내구도가 있다.

이번 전수 직렬화 검사에서 ShieldArmorRuntime을 붙인 자산은 0개이며, 실제 실더 보스 프리팹/설정 연결을 확인하지 못했다. 계산 기반 코드와 테스트 소스는 있지만 실더 콘텐츠 완료로 표시하지 않는다.

## 남은 승인 항목

보스 실제 출현/불출현, 방 전투 시작·종료, 시체 내용, 퀘스트 대상 Config 일치, 귀환, 실드 파손 후 다음 공격, 비상 근접 우회, 복원 시 보스 중복 생성 정책을 점검한다.
`,['NamedBattleManager.cs','NamedSectorController.cs','ShieldArmorRuntime.cs','Damageable.cs','RaidMapGenerator.cs','BossConfigPrefabLinkProvisioner.cs']);

doc('rooms','3763fd19-ccd6-81fa-9d9c-f08ab1913114','방 종류·프리셋·배치 제작 기준',`
## 현재 방 역할

Normal, Armory, Medical, Vault, Boss, Generator, Hatch, Extraction 역할을 사용한다. 역할(role), 방 전투 종류(roomType), 섹터 지형 프리팹(preset), Encounter, 루팅 Content는 같은 필드가 아니다.

## 실제 연결 수준

StageSectorInstantiator는 역할 전용 preset/encounter/content를 찾고 없으면 기본 설정으로 내려간다. 현재 1~5스테이지 데이터에는 역할별 전용 preset/encounter/content가 null인 항목이 남아 있다. Armory/Medical/Vault의 LootObject 연결은 있지만 그 사실만으로 모든 전용 방 지형과 적이 완성된 것은 아니다.

- 일반방: 기본 전투와 일반 콘텐츠.
- 무기고: 무기·탄 중심 루팅을 지정할 수 있다. 전용 중무장 Encounter 제작/연결은 별도 확인 대상.
- 의무실: 의료 아이템 루팅. 의료실 외형과 일반방 유사 전투를 각각 지정해야 한다.
- 금고방: 귀중품과 잠금/열쇠 정책을 분리해 연결한다.
- 발전소: 발전기 단말은 런에서 붙이는 경로. 전용 지형/배터리 배치 콘텐츠는 따로 제작한다.
- 해치방: 중앙 위치와 전력 조건은 RaidRuntime이 소유한다. 외형은 프리셋/콘텐츠 책임.
- 일반 탈출방: 정상 귀환 단말과 지도 표시.
- 보스방: 전용 보스 구성. 실제 보스 출현 여부와 후보 좌표 역할을 구별한다.

## 섹터 제작 흐름

1. Sector Maker에서 지형·출입구·충돌·배치 프리셋을 편집한다.
2. 적/웨이브 Encounter를 연결한다.
3. 상자·상점·터미널 등 ContentProfile 또는 역할별 LootObject를 연결한다.
4. StageDefinition에서 역할 override를 연결한다.
5. 해당 역할이 실제 생성된 런에서 길 막힘, NavMesh, 포탈, 상호작용 거리, 수색·지도·저장을 확인한다.

## 안정된 식별자

RaidRuntime은 방 좌표와 컨테이너 이름/형제 경로 기반 키로 컨테이너 상태를 구분한다. 배치 구조·이름을 바꾸면 이전 저장의 키가 달라질 수 있으므로 단순 아트 수정처럼 취급하지 말고 저장 호환성을 점검한다.

## 지금의 우선순위

새 방 종류를 계속 추가하기 전에 현재 8개 역할의 지형·전투·콘텐츠 참조를 채워야 한다. fallback으로 생성된 방을 의도한 최종 방으로 착각하지 않도록 에디터와 문서에 연결 완료 여부를 표시한다.
`,['StageSectorInstantiator.cs','StageDefinitionSO.cs','SectorMakerWindow.cs','RaidRuntime.cs','Assets/ScriptableObject/GameScene/Stage/RaidLoot/RaidContainer_Armory.asset','Assets/ScriptableObject/GameScene/Stage/RaidLoot/RaidContainer_Medical.asset']);

doc('retired','3763fd19-ccd6-812c-9d9b-cbb7424a8bf4','WIP·은퇴·잔여 연결 경계 | 옛 기획 대체',`
## 이 문서의 목적

옛 기획의 원본 백업 본문은 사용자의 현재 프로젝트 기준 요청에 따라 제거하고 이 경계표로 대체했다. 미래 개발의 기준으로 과거 시스템을 다시 끌어오지 않도록 무엇이 실제 남아 있는지 기록한다. Unity 자산/코드는 이번 문서 작업에서 삭제하지 않았다.

## 은퇴 처리된 경로

- InfectionControlManager: 전역 감염통제력 메인 Update/구독 경로가 retiredWip 가드로 막혀 있다. 클래스와 씬 컴포넌트는 남아 있다.
- MetaProgressController: OnEnable에서 WIP 기능을 꺼 두며 구매·레벨·재화 경로가 비활성/0 반환이다. 영구 강화 상점으로 설명하지 않는다.
- PlayerCurrencyRuntime: 옛 숫자형 런 지갑은 0/비활성 호환 경로. 현재 거래는 물리 CurrencyItemSO 스택을 사용한다.
- RoguelikeCurrencyRewardApplier: 옛 런 보상 재화 경로 WIP.
- Named 선택 보상/옛 회복 훅: 현재 루팅·퀘스트 경로와 혼동하지 않는다.
- 별도 플레이어 근접무기 콘텐츠: WIP로 보존. 현재 플레이어는 무장 없음/총 탄 없음 비상 근접이 기준.

## 남아 있어서 무시하면 안 되는 것

MaskRenderManager, SectorPaint, SectorOccupancy와 페인트 샘플/투사체/감염/시야 연결은 현재 파일에 존재한다. ‘옛 점유 기획을 삭제’와 ‘페인트 코드를 삭제’는 다르다. 제거하려면 각 소비자 대체와 실제 플레이 검증이 필요하다.

설정 enum의 Paint·UpgradePanel 명칭, 구형 HUD/Creature/StateMachine, save의 legacy meta 필드도 존재한다. 이름만 보고 현재 사용자 기능이라고 판정하거나 참조 0만으로 삭제하면 안 된다.

## 기반만 있고 연결 확인이 안 된 것

- ShooterVisualRig: 자산 직렬화 참조 0, 프로젝트 런타임의 AddComponent 생성도 발견하지 못했다. 장착 컨트롤러는 기존 리그를 찾는 경로다.
- ShieldArmorRuntime: 직렬화 장착 자산 0. 실더 보스 완료 아님.
- RaidProgressionDraft_WIP: draftOnly=true, 실제 퀘스트 참조 null. 새 퀘스트 라인 완료 아님.
- 특별 탈출: 최소 스테이지와 틀만 있으며 현재 비활성.
- 역할별 전용 방 프리셋/Encounter 일부: null fallback 상태.

## 다음 삭제 작업의 원칙

정적 참조·동적 생성·Resources/Addressables·저장 GUID를 모두 확인하고 삭제 대상을 좁힌다. 기존 저장 마이그레이션, 프리팹 Missing Script, 이벤트 구독, UI 노출을 검증한 뒤 실제 제거한다. 이번 감사는 이 작업을 실행하지 않았다.
`,['InfectionControlManager.cs','MetaProgressController.cs','PlayerCurrencyRuntime.cs','RoguelikeCurrencyRewardApplier.cs','ShooterVisualRig.cs','ShieldArmorRuntime.cs']);

doc('combat','3a43fd19-ccd6-8121-b07c-d3eb0cbec126','02. 플레이어·무기·피해·상태이상 | 현재 구현',`
## 공격과 데이터 경계

Player 입력/공격 상태 → Shooter 장착 및 탄약 확인 → EntityShootCore/무기별 발사 → 투사체 충돌 → Damageable 피해 → 상태·사망·퀘스트/루팅 알림 흐름이다. 무기 모델, 발사 데이터, 탄종 데이터, 플레이어 인벤토리를 한 컴포넌트에 합치지 않는다.

RangedWeaponType은 Pistol/Revolver/SMG/AssaultRifle/LMG/SniperRifle/Shotgun/RocketLauncher 계열이다. 탄약 패밀리는 Universal/Light/Medium/Heavy/Sniper/ShotgunShell/Rocket이며 tier 1~8과 관통 수치를 따로 가진다. 에디터 카테고리, 무기 클래스, 탄 패밀리, 관통력은 서로 다른 축이다.

## 일반 피해 계산

CanReceiveDamage·회피 조건을 통과한 원래 피해에 다음 순서로 계산한다.

1. 실드 방탄층: 흡수와 별도 내구도 소모.
2. 장착 방탄복: 그 뒤 남은 피해에 방탄 판정.
3. 엔티티 기본 방탄: 다시 남은 피해에 판정.
4. 최종 피해 배율 및 상태이상 피해 배율.
5. HP 차감과 감염 임계 재판정/사망 처리.

각 층의 등급은 더하지 않는다. 방탄 계산 최소 배율은 차이 d=max(0,방탄등급-관통등급)에 대해 0.10+0.90×exp(-0.55×d)이고 그 최소~1 범위에서 해당 층의 피해 배율을 한 번 뽑는 구조다. 관통이 방탄 이상이면 그 층은 전량 통과한다. 방탄 마모는 해당 층이 막아낸 양을 기준으로 계산하는 경로다.

실드는 전방위이며 방향 방패/합산 방탄이 아니다. 계산 코드는 준비되어 있지만 현재 자산 연결에서 실더 개체는 확인되지 않았다.

## 비상 근접

PlayerEmergencyMeleeProfile.IsAvailable은 무기가 없거나, RangedWeaponSO를 들고 발사 가능한 탄약이 없는 경우에 허용한다. 단순히 현재 탄창이 비었다는 것과 재장전 가능한 탄약까지 없는 상태는 구별한다.

기존 근접 판정 모양·범위·각도·타이밍·쿨다운·연출을 재사용하고 공격 수락 시 PlayerStats의 MeleeDamage를 스냅샷한다. 런타임 fallback 피해는 5지만 실제 값은 플레이어 스탯/설정으로 확인해야 한다.

ReceiveFixedMeleeAttack은 실드·방탄과 일반 피해 배율을 우회하는 고정 HP 피해 경로다. 방탄 내구도도 소모하지 않는다. 다만 사망/무적 등 CanReceiveDamage와 회피 조건까지 무조건 무시하는 것은 아니다. 일반 적/기존 근접무기의 공격 계산을 전부 이 고정 피해로 바꾼 것이 아니다.

## 소모품과 상태이상

의료 아이템은 회복량·감염 회복·사용 시간·상태 효과·부상 치료를 정의한다. 사용 중 이동 허용과 공격/대시/상호작용/피격/취소에 따른 중단 경로가 있다. 사용 성공 시 효과와 소비를 처리하며 중복 완료를 막아야 한다.

EntityStatusEffectRuntime은 시간 제한 효과, 중첩/갱신 정책, 스탯 배율과 DOT 등 공통 상태를 관리한다. 기본 스탯, 장비, 패시브, 퀘스트 영구 보상, 일시 효과를 구별한다. 옛 메타 구매 레벨을 현재 성장원으로 설명하지 않는다.

## 감염과 생존

감염이 현재 HP 이상이면 사망한다. PlayerShooterInfection의 자연회복은 꺼져 있다. 방 가스는 방독면 필터 처리 후 감염으로 들어가며, HP 직접 피해와 같은 수치로 설명하면 안 된다.

## 아직 검증할 것

실드/방탄복 파손 경계, 과관통, 고정 근접과 회피, 발사 가능 탄 판정, 재장전/교체/취소 중복 입력, 상태이상 저장·복원, 총기 내구와 반동/명중/연사 밸런스는 플레이 검증 항목이다. 이번 감사의 컴파일 통과는 이를 대신하지 않는다.
`,['Damageable.cs','ShieldArmorRuntime.cs','PlayerEmergencyMeleeProfile.cs','ShooterMeleeAttack.cs','PlayerShooterShoot.cs','PlayerBulletLoadoutRuntime.cs','EntityStatusEffectRuntime.cs','PlayerConsumableUseRuntime.cs']);

doc('items',null,'03. ItemSO·인벤토리·손 소켓·정렬',`
## 데이터 계층

ItemSO는 GUID, 현지화 이름/설명, 아이콘, 희귀도, 가격, WorldVisualPrefab, 스택/보유 제한을 소유한다. 같은 아이템 종류의 정적 정의와 플레이어가 가진 한 스택의 현재 내구도·수량은 분리한다.

- WeaponSO → RangedWeaponSO 및 특화 총기 타입. 근접무기 정의는 WIP 보존.
- BasicBulletSO 파생: 탄 패밀리와 tier/관통/투사체 설정.
- UsableItemSO → ConsumableItemSO → MedicalItemSO: 의료는 소비품의 하위 타입.
- ThrowableUsableItemSO는 UsableItemSO 계열이다. Item Maker 화면에서는 Consumables 아래에 묶지만 C# 상속이 ConsumableItemSO인 것은 아니다.
- Armor/GasMask/Backpack: 각각 방탄 내구도, 필터 내구도, 수납/장착 설정.
- Key/KeyTool, Passive, Valuable, Currency. CurrencyItemSO는 Valuable 계열의 실제 물품이다.

## 인벤토리의 책임

PlayerInventoryRuntime은 주머니와 가방, 장비, 스택 이동·수량·소유 제한·내구 메타데이터를 관리한다. 무기 탄창/호환 탄약과 퀵슬롯은 관련 런타임이 조율한다. 정적 SO 자체의 durability를 플레이 중 깎는 설계가 아니라 개별 인벤토리 상태로 보관한다.

장비 교체는 슬롯 상태만 바꾸는 것으로 끝나지 않는다. ShooterEquipmentController, 탄약 로드아웃, 스탯, 손/몸 장착 표시, UI 변경 알림, 저장 스냅샷에 영향을 준다. 새 타입을 만들 때 이 소비자를 확인한다.

## 손에 드는 표현

일반 소비품은 전용 HeldView가 없을 때 WorldVisualPrefab을 손 소켓에 마운트하는 fallback이 있다. 보정용 부모 위치/회전/스케일과 원본 프리팹의 저장된 로컬 상태를 구별한다. 아이템마다 전용 손용 모델을 무조건 만들 필요는 없지만 크기·방향 보정은 여전히 필요하다.

Throwable은 이 일반 fallback 예외이며 전용 WeaponView/ThrowableProjectile 경로를 유지한다. 무기/투척체의 공격 로직을 단순 장식 프리팹에 자동으로 붙여 주는 기능이 아니다. 손에 든 표시용 인스턴스의 물리/충돌 동작도 월드 아이템과 구별한다.

## 정렬

- Compact: 빈칸을 뒤로 보내 앞쪽 슬롯부터 밀집 배치.
- Rarity: Legendary → Unique → Rare → Normal 순으로 정렬.
- 플레이어 주머니와 가방에 각각 적용한다. 적/창고 전부를 무조건 같이 정렬하는 글로벌 버튼이 아니다.
- 스택 병합 기능과 구별하며 스택 객체/내구 등 인스턴스 정보를 유지한다.
- 드래그·소비·거래 중 상태를 보호하는 가드가 있다.

버튼은 GamePlay UI의 인벤토리 헤더 아래에 있어야 한다. RUNScene 루트에 버튼을 계속 생성하는 방식은 현재 기준이 아니다. 관련 소유권 테스트와 마이그레이션 코드가 존재한다. 실제 화면 배치는 새 플레이 세션에서 다시 확인해야 한다.

## Item Maker 편집 흐름

카탈로그 선택 → 데이터 편집 → 이미지 미리보기 → 루팅 연결. Ctrl로 다중 선택, Shift 범위 선택, 선택 카드 드래그로 카테고리 이동 기능이 있다. 카테고리 이동은 폴더 문자열만 바꾸는 작업이 아니라 필요한 경우 SO 타입 전환/참조 검사/백업/복원까지 수행한다. 기존 GUID 보존과 타입별 전용 필드 손실 가능성을 함께 확인한다.

## 남은 점검

ItemSO 현지화/아이콘/월드 프리팹/손 보정/효과/루팅/상점/저장 식별자까지 채워야 한 아이템 완성이다. 런 카탈로그 및 초기 로드아웃의 누락 GUID가 있어 현재 자산 연결 정리가 먼저다.
`,['ItemSO.cs','MedicalItemSO.cs','ThrowableUsableItemSO.cs','UsableHeldVisualUtility.cs','PlayerInventoryRuntime.cs','PlayerInventoryRuntime.Sorting.cs','ItemMakerWindow.cs','ItemCategoryMoveUtility.cs']);

doc('loot',null,'04. 루팅·수색·상자·열쇠·드롭',`
## 생성과 수색과 이동을 구별한다

LootTable/LootObject가 무엇을 뽑을지 정의한다. LootableObject/LockableLootObject는 월드 상자 상호작용과 잠금·수색을 관리한다. StructureLootInventoryRuntime은 생성된 내용물을 슬롯으로 노출한다. EnemyLootInventoryRuntime은 적 시체 및 공유 이동 규칙을 제공한다. UI는 이 내용물을 플레이어 인벤토리로 옮기는 요청을 보낸다.

처음 열 때 매번 새로운 랜덤 아이템을 생성하면 안 된다. RaidRuntime은 컨테이너 키·시드·이미 추첨됨·잠금·내용을 복원하는 기반이 있다. 위치/이름/형제 경로가 식별자에 사용되므로 프리팹 계층 변경은 저장과 함께 점검한다.

## 두 종류의 루팅 설정

- 적 드롭 진행표: LootProgressionConfigSO의 source별 풀, stageIndex override, 기본0 fallback. 공통 Valuable 추가 확률은 현재 0.35 설정.
- 방/구조물 루팅: LootObjectSO/LootTableSO와 Armory/Medical/Vault 전용 RaidLoot/Container 자산.

둘은 서로 다른 공급 경로다. 적이 든 무기와 드롭 목록, 방 역할과 상자 내용을 일치시키려면 각각 연결해야 한다. ‘의무실 역할’만 지정했다고 모든 상자에 치료제가 자동 등록되는 것은 아니다.

## 잠금과 열쇠

LockedLootProfile과 Key/KeyTool 조건, 열쇠 선택 UI, 사용/소모 수명, 잠금 해제 상태를 분리한다. 열쇠를 카테고리로 옮겼다는 것만으로 어떤 상자를 여는 키인지 자동 정의되지 않는다. 조건 불충족·취소·이미 열린 상자·복원 직후를 검사한다.

## 수색음과 UI

InteractionSearchAudio는 수색 시작/진행/중단/완료에 대한 공통 오디오 경로다. 단발 클릭음과 루프 수색음을 구별하고 대상 변경·사망·패널 닫힘 때 루프가 남지 않아야 한다. 루팅 UI는 GamePlay 관리/UI 소유이며 RUN의 상자 모델 아래에 전체 UI를 복제하지 않는다.

## 발견한 저장 일관성 위험

EnemyLootInventoryRuntime.TryTakeItemStack에는 플레이어 인벤토리에 먼저 넣고 원본 스택을 나중에 비우는 순서가 있다. 인벤토리 OnChanged → QuestRuntime의 수집 목표 갱신 → 저장 호출이 그 사이에 일어날 수 있다. 저장이 이 중간 상태를 캡처하면 양쪽에 아이템이 남는 스냅샷 가능성이 있다.

이번 감사에서는 코드 경로를 확인했으며 모든 루팅에서 중복이 반드시 발생한다고 단정하지 않는다. 현재 세션에서 강제 중단 재현 테스트는 실행하지 않았다. 이동 전체를 하나의 저장 트랜잭션으로 만들거나, 변경 알림/저장을 최종 상태 뒤로 모으는 패치는 별도 승인 작업이다.

## 콘텐츠 마감 체크

모든 상자의 표·슬롯·잠금·수색시간·소리·표시명, 적 시체의 장착품과 드롭, 희귀 아이템 비율, 재방문/이어하기 때 재추첨 방지, 꽉 찬 가방/부분 스택/취소 후 양쪽 수량을 확인한다.
`,['LootObjectSO.cs','LootTableSO.cs','LootProgressionConfigSO.cs','LootableObject.cs','LockableLootObject.cs','EnemyLootInventoryRuntime.cs','StructureLootInventoryRuntime.cs','QuestRuntime.cs']);

doc('progression',null,'05. 퀘스트·상점·화폐·창고·영구 성장',`
## 영구 성장의 현재 소유자

QuestRuntime은 퀘스트 진행·완료·보상 수령을 조율한다. QuestDefinition은 목표/조건/보상을, QuestLine은 줄기와 연결을 정의한다. Home의 Main/Item/Stats 계열 NPC와 UI가 이를 제시한다. 옛 메타 포인트 구매 상점은 현재 성장 경로가 아니다.

목표 타입은 Kill, Hit, CollectItem이다. 대상 Config, 정확한 ItemSO, 요구 무기, 거리 조건 등 필터를 조합한다. 보상은 Item, Stat, ShopUnlock, StageUnlock, WarehouseSlots. 보상 수령 영수증과 영구 보상 상태는 저장에 포함된다.

## 새 레이드 퀘스트 계획의 실제 상태

RaidProgressionDraft_WIP는 draftOnly=true이며 실제 퀘스트 참조가 null이다. 아래는 초안 데이터이지 전부 활성 퀘스트가 아니다.

1. Normal valuable 10개 + 1스테이지 보스 + 돈 → 2스테이지 해금 방향.
2. Rare/Unique/Legendary valuable + 해당 보스 + 돈 → 다음 스테이지 방향.
3. 높은 단계 제출 수량/금액은 0 등 미정 값이 남아 있다. 무료 확정 요구로 해석하지 않는다.
4. 일반 상점 해금 상한 LV6, LV7/8 특수 파밍 방향이 기록되어 있지만 모든 아이템/상점 차단 규칙이 검증됐다는 의미는 아니다.
5. 방탄2/3/4, 탄약, 3스테이지 이후 주사기, 스탯, 창고 확장 서브 퀘스트는 연결·수량·보상 제작이 필요하다.

현재 Campaign 등의 별도 QuestDefinition 자산은 존재한다. 새 초안과 기존 실제 퀘스트를 합쳐 ‘모두 완성’이라고 세지 않는다. 특별맵/엔딩도 이 초안만으로 완료라고 볼 수 없다.

## 화폐와 상점

현재 거래 화폐는 인벤토리의 CurrencyItemSO 스택이다. PlayerCurrencyRuntime의 옛 숫자 지갑은 비활성 호환 코드다. 상품별 ShopTradeRequirement는 실제 아이템 조건을 소비하며, 구매 가능 여부와 인벤토리 공간을 함께 확인해야 한다.

SectorShop/ShopConfig가 상점 제안 생성·시드·재고·리필을, ShopRunRuntime이 런 상태를, ShopUnlockRuntime이 영구 해금을 관리한다. 판매/수리/교환 UI와 거래 성공·실패 피드백이 별도로 연결된다. HomeShopConfig에서 미해결 GUID 2개가 발견되어 현재 상품 연결 점검이 필요하다.

## 창고

InheritanceWarehouseRuntime은 기본 24칸, Profile 저장의 별도 영구 아이템 목록이다. 현재 런 인벤토리를 이름만 바꾼 것이 아니다. 퀘스트 용량 보상은 늘리는 방향으로 적용하며 기존 아이템을 잘라 내지 않는다.

창고의 스택은 슬롯, 아이템 GUID, 수량, 방탄/무기/필터 현재·최대 내구 정보를 저장한다. 변경 fingerprint를 비교해 저장한다. 누락 GUID는 해당 항목을 건너뛰고 오류를 남기는 경로가 있다. 모든 탈출 전리품을 창고에 자동 정리해 넣는 기능으로 설명하지 않는다.

## 제작·검증 순서

퀘스트 요구를 Notion에 확정 → 정확한 Item/Enemy Config 연결 → 선행 조건/보상 설정 → NPC/QuestLine 연결 → 실제 행동으로 목표 증가 확인 → 보상 한 번 수령 → 저장/재시작 후 중복 보상 방지 확인. 창고/상점과 같은 아이템 이동을 포함하므로 거래·저장 일관성 테스트도 필수다.
`,['QuestRuntime.cs','QuestDefinitionSO.cs','QuestLineSO.cs','ShopConfigSO.cs','SectorShop.cs','ShopRunRuntime.cs','ShopUnlockRuntime.cs','InheritanceWarehouseRuntime.cs','Assets/ScriptableObject/GameScene/Stage/RaidProgressionDraft_WIP.asset']);

doc('save',null,'06. 저장·이어하기·마이그레이션·복구',`
## 세 파티션

- settings.es3: 기기 설정. 오디오, 화면/그래픽 값, 감도, locale code, 키 바인딩 JSON. schemaVersion 1.
- profile_default.es3: 창고, 스테이지/상점 해금, 퀘스트 진행·보상 영수증·스탯/창고 보상, 레거시 호환 메타 필드. schemaVersion 1.
- run_default.es3: 현재 플레이어/아이템·장비·탄약·상점 런 상태·레이드 스냅샷. schemaVersion 2.
- backups: 파티션별 최근 백업 3개 회전.

SaveSystem이 ES3 저장과 구버전 호환을 조율한다. 레거시 단일 ES3/JSON/PlayerPrefs에서 필요한 파티션을 옮기는 마이그레이션 경로가 있다. Locale Unity 객체 대신 ko/en 코드 문자열을 저장한다.

## 쓰기 절차

임시 파일에 저장 → 다시 읽어 검증 → 유효한 기존 본파일을 백업 회전 → 임시 파일을 본파일로 교체하는 흐름이다. 함수 이름에 Atomically가 있어도 여러 파티션을 한꺼번에 OS 수준 단일 트랜잭션으로 커밋한다는 보장은 아니다. 전원 차단·파일 잠금·디스크 부족 테스트는 별도로 필요하다.

설정 변경은 0.75초 디바운스로 쓰기를 합친다. RunLifecycleCoordinator가 대기 쓰기를 Update에서 flush하고 일시정지/종료 등 중요 경계에서 즉시 저장한다. 레이드 상태도 주기 체크포인트와 중요 이벤트를 통해 저장한다.

## 플레이어 스냅샷

HP·감염, 인벤토리/장착품, 스택 내구 상태, 탄약/탄창, 패시브, 지속시간 있는 효과, 상점 제안 상태 등을 복원한다. 아이템은 영구 GUID로 찾으며 하나가 사라졌다고 전체 복원을 무조건 포기하지 않고 해당 항목을 제외/경고하는 경로가 있다.

## 레이드 스냅샷

시드와 배치 해결 결과, 현재 스테이지/좌표, 경과·초과 시간, 발전기/해치, 보스 출현 결정, 컨테이너 추첨·잠금·내용 상태를 보관한다. 정확한 전투 중 모든 적 위치/HP/행동, 모든 바닥 드롭/시체, 모든 방 타이머 순간 상태까지 저장하는 완전한 월드 스냅샷이라고 쓰면 안 된다. 현재 저장 모델의 필드와 복원 훅이 있는 것만 보증 대상으로 삼는다.

## 수명주기

Booting/Home/RunActive/RunSuspended/RunCompleted/GameOver 상태를 나눈다. 사망 시 런 스냅샷을 비우고 재캡처를 억제하여 사망 직전 살아 있는 저장이 다시 덮이지 않게 한다. 프로필 영구 진행과 런 소지품 손실은 별도 정책이다.

## 현재 위험

1. RuntimeContentCatalog.asset에 실제 잘못된 YAML 줄과 누락 GUID들이 존재한다. 런타임 아이템 복원의 기본 자산을 먼저 고쳐야 한다.
2. Player 프리팹/초기 로드아웃/상점에도 누락 참조가 있다. 에디터에 우연히 로드된 SO로 복원이 되는 것을 빌드 안전성으로 착각하면 안 된다.
3. 루팅 이동 중 OnChanged→퀘스트 저장이 먼저 일어날 수 있는 중간 상태 캡처 위험이 있다. 현재 코드 경로 확인이며 이번 세션 재현 테스트는 미실행.
4. 파티션 간 아이템 이동/보상과 저장 실패의 롤백·중복 방지는 별도 테스트가 필요하다.

## 관리 도구

Tools/Board Chase/Save Inspector가 settings/profile/run과 백업을 읽는 관리 창이다. Debug Monitor의 창고/런 조작과 용도를 구별한다. 테스트 전 현재 실세이브를 별도 보존하고 폐기 가능한 프로필로 검증한다.

## 승인 테스트

빈 저장 → 첫 실행, 구버전 이전, 잘린 본파일 → 백업 복원, 백업 일부 파손, 미지 아이템 GUID, 가득 찬 창고 이동, 사망 직후 강제 종료, 발전기 배터리 소비 직후 재시작, 상자 수령 중 중단, 퀘스트 보상 중 중단을 각각 확인한다. 이번 감사에서 실세이브를 지우거나 고의 손상하지 않았다.
`,['SaveSystem.cs','RunLifecycleCoordinator.cs','PlayerRunStatePersistence.cs','RaidRuntime.cs','SaveInspectorWindow.cs','Assets/Resources/RuntimeContentCatalog.asset']);

doc('ui',null,'07. UI·입력·설정·한국어/영어',`
## UI 씬 소유권

GamePlay는 HUD·인벤토리·오버레이 등 공통 UI/게임 관리 씬이다. RUNScene은 월드·섹터·스폰·진행 관리 역할이다. 정렬 버튼을 RUNScene 루트에 반복 생성하는 방식은 사용하지 않는다. 현재 인벤토리 주머니/가방 헤더의 Compact/Rarity 버튼과 연결 컴포넌트를 기준으로 한다.

UIOverlayManager/Panel은 열림 순서, 입력 차단과 화면 상태를 조율한다. 아이템 상세·상점·적 루팅·퀘스트 패널은 같은 게임 상태를 각각 임의 저장하는 것이 아니라 이벤트/런타임 데이터를 표시하고 요청한다.

## 현재 설정 탭

- 일반: 언어, 해상도, 화면 모드, 조준 감도.
- 오디오: Master/Music/SFX.
- 그래픽: VSync, 프레임 제한. 현재 UISettingGraphicsComponent의 프레임 선택은 30/60/90/120/144/165/240이며 VSync가 켜지면 targetFrameRate=-1 경로를 쓴다.
- 키 설정: 이동 4방향, Attack, Paint, Dash, Interact, UpgradePanel, QTEInteract, BulletSlot1/2 등 현재 enum·입력 액션 연결.

저장 구조에 AA/Shadow 값이 있다고 설정 UI에서 둘 다 선택할 수 있다고 쓰지 않는다. Paint/UpgradePanel 명칭도 현재 기능 노출 적합성을 다시 정리할 대상이다. SettingsSystem과 SettingsSO, UI 컴포넌트의 책임을 구별한다.

## 현지화 구조

Unity Localization StringTable + UIRuntimeLocalization의 런타임 적용/캐시/대체 문자열을 사용한다. 폰트 기준은 NEXONLv1GothicRegular SDF다. ItemSO 표와 아이템 설명 완성도는 일반 UI 표와 별도 범위로 본다.

정적 KO/EN 표 검사에서 대표 표의 엔트리 수는 각각 다음과 같다: Inventory 75, Settings 43, PlayerTimer 17, Map 19, MainMenu 7, Pause 6, PlayerInfo 6, Popup 12, Ammo 1. 읽힌 이 엔트리 안에서는 빈 m_Localized가 발견되지 않았다. 이것은 화면의 모든 문장이 테이블에 등록됐다는 검증이 아니다.

UI_Settings_General은 같은 SharedData를 참조하는 중첩 경로의 빈 표와 상위 경로의 KO 3/EN 4개 표가 함께 있다. 실제 선택되는 TableCollection/Addressables 연결과 키 대응을 정리해야 한다. 중복 존재만으로 무조건 실행 오류라고 단정하지 않는다.

QuestDefinition의 요약 생성, 로딩/안내 문자열 등에 하드코딩 문자열이 남아 있다. 한국어/영어 전체 100% 완료로 표시하지 않는다. 폰트 글리프, 긴 영어 줄바꿈, 동적 수량/시간 포맷, 드롭다운 항목도 실제 화면 검사가 필요하다.

## 입력

InputReader가 플레이/메뉴 액션과 이벤트, 키 바인딩 표시 및 저장된 override 재적용을 담당한다. 재바인딩 완료 시 UI 표시뿐 아니라 실제 사용하는 액션 인스턴스에 반영되는지 확인한다. 메뉴가 켜진 상태에서 사격/소비가 중복 실행되지 않아야 한다.

## 승인 체크

Home/RUN/일시정지 각각 설정 열기, 언어 실시간 변경과 재시작 유지, 한국어 폰트, 오디오 실제 믹서 값, 프레임/VSync, 키 변경/복원, 인벤토리 정렬 대상, 중복 버튼/리스너, 패널 닫힌 후 조작 복귀를 확인한다.
`,['UISettingsController.cs','UISettingGraphicsComponent.cs','SettingsSystem.cs','UIRuntimeLocalization.cs','InputReader.cs','UIOverlayManager.cs','InventorySortButtonMigration.cs','InventorySortUIOwnershipTests.cs']);

doc('audio','2523fd19-ccd6-811e-a9da-c519aefc1ddd','08. 오디오·믹서·Cue·표면 충돌음',`
## 현재 재생 경로

게임 행동 → AudioCueEventChannelSO → 지속 AudioManager → SoundEmitter 풀 → AudioConfiguration의 mixer output → Master.mixer 순서다. AudioCueLibrary는 등록/검색/편집 카탈로그이며 모든 소리를 매 프레임 판단하는 별도 런타임 매니저가 아니다.

현재 프로젝트의 믹서는 Master.mixer 하나이며 Master/Music/SFX/UI/Voice/Ambience 그룹이 있다. 노출 설정은 MasterVolume/MusicVolume/SFXVolume이다. UI/Voice/Ambience 그룹이 존재한다고 설정 화면에 개별 볼륨 슬라이더가 있다는 뜻은 아니다.

## Cue 구성

AudioCueSO는 클립 그룹, 순차/랜덤 선택, 반복, gain, 기본 재생 설정을 가진다. AudioConfiguration은 2D/3D·거리·라우팅 등 재생 성격을 소유한다. SoundEmitter는 풀링되며 미리 준비하는 수량 기본값은 10이다.

이번 정적 검사에서 실제 AudioCue 폴더의 72개 Cue 모두 최소 하나의 오디오 클립 참조가 있고 그 GUID가 파일로 해석됐다. 템플릿은 별도다. 이것은 72개 소리를 실제 청취해 음량·품질을 승인했다는 뜻은 아니다.

## 총알 표면 충돌음

총알 충돌이 발생하면 맞은 collider에서 부모 방향으로 ObstacleSurface를 찾는다. 표면 타입은 Default/Metal/Wood/Concrete/Sand/Rubber. 공통 BulletImpactAudioCatalog에서 해당 표면 Cue와 설정을 고른다.

Layer는 충돌 대상을 걸러내는 역할이며 ‘Metal Layer이므로 금속 소리’라는 분류가 아니다. 렌더러 Material의 색/Shader를 자동 분석해 재질을 추측하지도 않는다. 장애물에는 표면 타입만 지정하고 사운드 목록은 공통 카탈로그에서 관리한다.

BasicBullet 지원 경로이며 Rocket은 제외된다. 칼/로켓 충격음을 이 일반 총알 표에 억지로 합치지 않는다. 같은 장애물 연타에 대한 짧은 재생 간격 제한(0.04초)이 있고 Cue/Channel 연결이 없으면 소리가 나지 않는다. ObstacleSurface가 붙은 프리팹은 정적 집계 23개이며 그중 지정 씬 참조 그래프 내 15개가 연결된다. 그래프 포함은 활성 스폰 보증이 아니다.

## UI·수색·NPC·퀘스트

UIAudio: 패널/선택/장착/거래 피드백. InteractionSearchAudio: 수색 진행과 중단. NPC 프로필과 퀘스트 이벤트: 인사/수락/거절/완료 등. 이들은 기존 Cue 체계를 사용한다.

NPC_Greeting_Temp 등의 authoringNotes는 실제 녹음 대사가 아니라 임시 비언어 알림이라고 명시한다. 사운드 연결 밑작업과 최종 대사·믹싱 완성을 구별한다.

## 제작 기준

새 소리는 클립 → Cue → 기본 재생 설정 → 이벤트 호출 위치 → 믹서 경로 → Audio Cue Library 등록 → 실제 상황 청취 순으로 연결한다. 큰 총격/연사 중 동시 발음, UI 일시정지, 수색 취소, 화면 전환 후 루프 잔존, NPC 중첩을 확인한다.

## 기존 음원 자료

아래 원본 자료 DB는 보존한다. 해당 DB의 모든 행이 현재 Unity에 연결된 음원이라는 의미는 아니다. 현재 구현의 기준은 위 경로와 Unity AudioCue 자산이다.
`,['AudioManager.cs','AudioCueSO.cs','AudioCueLibraryWindow.cs','ObstacleSurface.cs','BulletImpactAudio.cs','InteractionSearchAudio.cs','UIAudio.cs','Assets/ScriptableObject/Audio/AudioCue/SFX/Interactable/NPC_Greeting_Temp.asset']);

doc('visual',null,'09. Shooter 비주얼·장비 소켓·미연결 상태',`
## 요청한 소켓 정책과 코드

Hair는 Face와 동급이다. Face는 안면 Eyes+Mouth 영역을 가리는 상위 점유이며 Hair를 자동으로 숨기지 않는다. Face 장비를 빼면 하위 Eyes/Mouth 비주얼이 다시 나타나야 한다. 전면 가스마스크는 Face, 입 호흡기는 Mouth, 머리카락은 Hair로 지정하는 정책이다.

ShooterVisualSocketId에는 Armor, RightHand, LeftHand, Hair, Eyes, Back, Mask, Mouth, Face, Body, Feet가 있다. Head 소켓 enum은 없다. 별도 점유 마스크의 Head 비트나 호환 이름 탐색이 남은 것을 실제 Head 소켓 존재로 혼동하지 않는다.

ShooterVisualRig는 소켓 탐색·장착 비주얼·점유/숨김·기본 비주얼 복귀를 담당한다. ShooterCustomizeSO는 외형 설정, ShooterVisualBodyProfileSO는 바디 표시 설정, ShooterEquipmentController는 실제 장착 상태와 표시 연결을 맡는다. 외형 스케일과 게임 판정 collider 크기는 구별한다.

## 이번 감사의 중요한 결과

Assets의 직렬화 검사에서 ShooterVisualRig 스크립트 참조 자산은 0개다. 런타임 코드에서 AddComponent<ShooterVisualRig> 생성도 발견하지 못했다. 장착 컨트롤러는 기존 리그를 GetComponentInChildren으로 찾는 경로다.

따라서 ‘Capsule의 모든 새 소켓이 실제로 세팅되어 사용 중’이라고 쓰지 않는다. Customize 데이터 자산은 존재하지만 데이터의 존재와 씬/프리팹 장착은 다르다. BodyProfile 자산도 현재 검사 범위에서 생성된 실사용 자료를 확인하지 못했다. 기존 무기/방탄 장착용 경로까지 전부 없다는 의미는 아니다.

## 필요한 다음 작업

1. 실제 플레이어/적 Capsule 프리팹에서 기존 방탄 위치와 몸체 기준을 확인한다.
2. Hair/Eyes/Mouth/Face/Body/Feet anchor를 잡고 리그 컴포넌트를 연결한다.
3. 장비 SO별 소켓/점유/보정 값을 작성한다.
4. Face와 Eyes/Mouth 중복 장착, 해제 복귀, Hair 독립성을 실제 화면으로 검증한다.
5. 무기 손 소켓, 기존 Armor, Body 외형과 저장 복원을 함께 검사한다.

가스마스크 렌즈·머티리얼·형상 품질은 모델/렌더링 작업이다. 코드만 바꾸거나 콘셉트 이미지를 만든 것을 Unity 적용 완료로 기록하지 않는다. 현재 GasMaskLV3 자산의 미해결 비주얼 참조도 먼저 정리해야 한다.

## 편집 도구

Shooter Customize Station은 이 데이터를 편집/미리보기하는 도구다. 도구가 있다고 모든 프리팹에 자동으로 리그가 생기는 것은 아니다. 소켓 Gizmo와 실제 프리팹 저장 상태를 승인 근거로 남긴다.
`,['ShooterVisualSocketId.cs','ShooterVisualRig.cs','ShooterCustomizeSO.cs','ShooterVisualBodyProfileSO.cs','ShooterEquipmentController.cs','ShooterCustomizeStationWindow.cs']);

module.exports={pages,doc};
