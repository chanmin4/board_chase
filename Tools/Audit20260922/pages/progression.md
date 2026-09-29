# 05. 퀘스트·상점·화폐·창고·영구 성장

검사 기준: 2026-09-22 / E:/github/board_chase/board_chase. 현재 파일의 정적 연결·핵심 실행 경로 기준이며, 별도 표시 없는 항목은 이번 감사에서 플레이 테스트한 결과가 아닙니다.

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

## 코드·데이터 근거

- `E:/github/board_chase/board_chase/Assets/Scripts/Gameplay/Progression/Quests/QuestRuntime.cs`
- `E:/github/board_chase/board_chase/Assets/Scripts/Gameplay/Progression/Quests/QuestDefinitionSO.cs`
- `E:/github/board_chase/board_chase/Assets/Scripts/Gameplay/Progression/Quests/QuestLineSO.cs`
- `E:/github/board_chase/board_chase/Assets/Scripts/Gameplay/World/Sectors/Shop/ShopConfigSO.cs`
- `E:/github/board_chase/board_chase/Assets/Scripts/Gameplay/World/Sectors/Shop/SectorShop.cs`
- `E:/github/board_chase/board_chase/Assets/Scripts/Gameplay/World/Sectors/Shop/ShopRunRuntime.cs`
- `E:/github/board_chase/board_chase/Assets/Scripts/Gameplay/World/Sectors/Shop/ShopUnlockRuntime.cs`
- `E:/github/board_chase/board_chase/Assets/Scripts/Gameplay/World/Sectors/Treasure/InheritanceWarehouseRuntime.cs`
- `E:/github/board_chase/board_chase/Assets/ScriptableObject/GameScene/Stage/RaidProgressionDraft_WIP.asset`