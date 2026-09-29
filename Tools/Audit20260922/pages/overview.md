# 현재 게임 한눈에 | 레이드·파밍·귀환

검사 기준: 2026-09-22 / E:/github/board_chase/board_chase. 현재 파일의 정적 연결·핵심 실행 경로 기준이며, 별도 표시 없는 항목은 이번 감사에서 플레이 테스트한 결과가 아닙니다.

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

## 코드·데이터 근거

- `E:/github/board_chase/board_chase/Assets/Scripts/Core/SceneFlow/InitializationLoader.cs`
- `E:/github/board_chase/board_chase/Assets/Scripts/Core/Persistence/RunLifecycleCoordinator.cs`
- `E:/github/board_chase/board_chase/Assets/Scripts/Gameplay/World/Sectors/StageMap/RaidRuntime.cs`
- `E:/github/board_chase/board_chase/Assets/Scripts/Gameplay/World/Sectors/StageMap/RaidMapGenerator.cs`
- `E:/github/board_chase/board_chase/Assets/Scripts/Gameplay/Entities/Shooter/Player/PlayerEmergencyMeleeProfile.cs`
- `E:/github/board_chase/board_chase/Assets/ScriptableObject/GameScene/Stage/RaidProgressionDraft_WIP.asset`