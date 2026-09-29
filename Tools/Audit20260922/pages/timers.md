# 방 제한시간·레이드 시간·진행 판정

검사 기준: 2026-09-22 / E:/github/board_chase/board_chase. 현재 파일의 정적 연결·핵심 실행 경로 기준이며, 별도 표시 없는 항목은 이번 감사에서 플레이 테스트한 결과가 아닙니다.

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

## 코드·데이터 근거

- `E:/github/board_chase/board_chase/Assets/Scripts/Gameplay/Progression/Stage/StageProgressionManager.cs`
- `E:/github/board_chase/board_chase/Assets/Scripts/Gameplay/World/Sectors/StageMap/RaidRuntime.cs`
- `E:/github/board_chase/board_chase/Assets/Scripts/Gameplay/World/Sectors/ScriptableObject/SectorRulesSO.cs`
- `E:/github/board_chase/board_chase/Assets/Scripts/Gameplay/World/Sectors/Traits/SectorRoomTraitRuntime.cs`
- `E:/github/board_chase/board_chase/Assets/ScriptableObject/GameScene/Stage/RaidProgressionDraft_WIP.asset`