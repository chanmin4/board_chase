# 일반 적·웨이브·AI·난이도

검사 기준: 2026-09-22 / E:/github/board_chase/board_chase. 현재 파일의 정적 연결·핵심 실행 경로 기준이며, 별도 표시 없는 항목은 이번 감사에서 플레이 테스트한 결과가 아닙니다.

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

## 코드·데이터 근거

- `E:/github/board_chase/board_chase/Assets/Scripts/Gameplay/Entities/Shooter/Enemy/Common/EnemyShooter.cs`
- `E:/github/board_chase/board_chase/Assets/Scripts/Gameplay/Entities/Shooter/Enemy/StateMachine/EnemyShooterCombatController.cs`
- `E:/github/board_chase/board_chase/Assets/Scripts/Gameplay/World/Sectors/SectorEnemySpawner.cs`
- `E:/github/board_chase/board_chase/Assets/Scripts/Gameplay/Entities/Shooter/Common/Vision/EntityVisionController.cs`
- `E:/github/board_chase/board_chase/Assets/Scripts/Gameplay/Items/Loot/LootProgressionConfigSO.cs`
- `E:/github/board_chase/board_chase/Assets/Scripts/Gameplay/Progression/Difficulty/ScriptableObject/DifficultyRulesSO.cs`