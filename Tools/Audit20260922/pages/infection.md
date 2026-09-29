# 개인 감염·가스·방독면 | 현재 규칙

검사 기준: 2026-09-22 / E:/github/board_chase/board_chase. 현재 파일의 정적 연결·핵심 실행 경로 기준이며, 별도 표시 없는 항목은 이번 감사에서 플레이 테스트한 결과가 아닙니다.

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

## 코드·데이터 근거

- `E:/github/board_chase/board_chase/Assets/Scripts/Gameplay/Entities/Shooter/Common/Core/ShooterInfection.cs`
- `E:/github/board_chase/board_chase/Assets/Scripts/Gameplay/Entities/Shooter/Player/PlayerShooterInfection.cs`
- `E:/github/board_chase/board_chase/Assets/Scripts/Gameplay/World/Sectors/Traits/SectorRoomTraitRuntime.cs`
- `E:/github/board_chase/board_chase/Assets/Scripts/Gameplay/Combat/Infection/InfectionControlManager.cs`
- `E:/github/board_chase/board_chase/Assets/Scripts/Gameplay/MaskRenderManager.cs`
- `E:/github/board_chase/board_chase/Assets/Scripts/Gameplay/World/Sectors/SectorPaint.cs`