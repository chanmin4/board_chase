# 11. 제작 도구·작업 루틴·에디터 자동 변경

검사 기준: 2026-09-22 / E:/github/board_chase/board_chase. 현재 파일의 정적 연결·핵심 실행 경로 기준이며, 별도 표시 없는 항목은 이번 감사에서 플레이 테스트한 결과가 아닙니다.

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

## 코드·데이터 근거

- `E:/github/board_chase/board_chase/Assets/Scripts/Authoring/Editor/BoardChaseAuthoringHubWindow.cs`
- `E:/github/board_chase/board_chase/Assets/Editor/ItemMakerWindow.cs`
- `E:/github/board_chase/board_chase/Assets/Editor/ItemCategoryMoveUtility.cs`
- `E:/github/board_chase/board_chase/Assets/Editor/LootProgressionEditorWindow.cs`
- `E:/github/board_chase/board_chase/Assets/Editor/QuestDesignTreeWindow.cs`
- `E:/github/board_chase/board_chase/Assets/Editor/ShopProgressionTreeWindow.cs`
- `E:/github/board_chase/board_chase/Assets/Scripts/Authoring/Editor/ContentPreflightWindow.cs`
- `E:/github/board_chase/board_chase/Assets/Scripts/Authoring/Editor/BossConfigPrefabLinkProvisioner.cs`
- `E:/github/board_chase/board_chase/Assets/Editor/InventorySortButtonMigration.cs`