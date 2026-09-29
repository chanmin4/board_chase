# 01. 게임 흐름·레이드 시스템 | 현재 구현

검사 기준: 2026-09-22 / E:/github/board_chase/board_chase. 현재 파일의 정적 연결·핵심 실행 경로 기준이며, 별도 표시 없는 항목은 이번 감사에서 플레이 테스트한 결과가 아닙니다.

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

## 코드·데이터 근거

- `E:/github/board_chase/board_chase/Assets/Scripts/Core/SceneFlow/SceneLoader.cs`
- `E:/github/board_chase/board_chase/Assets/Scripts/Core/Persistence/RunLifecycleCoordinator.cs`
- `E:/github/board_chase/board_chase/Assets/Scripts/Gameplay/Progression/Stage/StageProgressionManager.cs`
- `E:/github/board_chase/board_chase/Assets/Scripts/Gameplay/World/Sectors/StageMap/StageSectorInstantiator.cs`
- `E:/github/board_chase/board_chase/Assets/Scripts/Gameplay/World/Sectors/StageMap/RaidRuntime.cs`