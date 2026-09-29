# 보스·Named·실더 준비 상태

검사 기준: 2026-09-22 / E:/github/board_chase/board_chase. 현재 파일의 정적 연결·핵심 실행 경로 기준이며, 별도 표시 없는 항목은 이번 감사에서 플레이 테스트한 결과가 아닙니다.

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

## 코드·데이터 근거

- `E:/github/board_chase/board_chase/Assets/Scripts/Gameplay/World/Sectors/Named_Boss/NamedBattleManager.cs`
- `E:/github/board_chase/board_chase/Assets/Scripts/Gameplay/World/Sectors/Named_Boss/NamedSectorController.cs`
- `E:/github/board_chase/board_chase/Assets/Scripts/Gameplay/Entities/Common/Equipment/ShieldArmorRuntime.cs`
- `E:/github/board_chase/board_chase/Assets/Scripts/Gameplay/Entities/Common/Core/Damageable.cs`
- `E:/github/board_chase/board_chase/Assets/Scripts/Gameplay/World/Sectors/StageMap/RaidMapGenerator.cs`
- `E:/github/board_chase/board_chase/Assets/Scripts/Authoring/Editor/BossConfigPrefabLinkProvisioner.cs`