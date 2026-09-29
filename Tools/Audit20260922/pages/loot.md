# 04. 루팅·수색·상자·열쇠·드롭

검사 기준: 2026-09-22 / E:/github/board_chase/board_chase. 현재 파일의 정적 연결·핵심 실행 경로 기준이며, 별도 표시 없는 항목은 이번 감사에서 플레이 테스트한 결과가 아닙니다.

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

## 코드·데이터 근거

- `E:/github/board_chase/board_chase/Assets/Scripts/Gameplay/Items/Loot/LootObjectSO.cs`
- `E:/github/board_chase/board_chase/Assets/Scripts/Gameplay/Items/Loot/LootTableSO.cs`
- `E:/github/board_chase/board_chase/Assets/Scripts/Gameplay/Items/Loot/LootProgressionConfigSO.cs`
- `E:/github/board_chase/board_chase/Assets/Scripts/Gameplay/Items/Loot/LootableObject.cs`
- `E:/github/board_chase/board_chase/Assets/Scripts/Gameplay/Items/Loot/LockableLootObject.cs`
- `E:/github/board_chase/board_chase/Assets/Scripts/Gameplay/Entities/Shooter/Enemy/Common/EnemyLootInventoryRuntime.cs`
- `E:/github/board_chase/board_chase/Assets/Scripts/Gameplay/World/Sectors/Treasure/StructureLootInventoryRuntime.cs`
- `E:/github/board_chase/board_chase/Assets/Scripts/Gameplay/Progression/Quests/QuestRuntime.cs`