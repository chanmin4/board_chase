# Loot Maker — 상자별 독립 테이블 (2026-09-29)

## 적용 결과

사용자 요청: 백팩뿐 아니라 다른 카테고리도 공유 연결을 없애고, 상자마다 별도 목록을 편집한다.

- 전체 LootObjectSO 27개 → 서로 다른 LootTableSO 27개.
- 살아 있는 상자에서 LootObjectTypeSO 연결 0개, 테이블 중복 참조 0개.
- Ammo 3 / Medical 3 / Military Gear 5 / Treasure Box 1 / Safe 4 / Backpack 6 / Metal Box 5.
- 카테고리는 분류만 담당한다. 같은 카테고리에 있다고 목록을 공유하지 않는다.
- 기존 프리팹·씬은 저장하거나 변경하지 않았다. LootObjectSO GUID와 모델 연결을 유지했다.

## 사용법

1. 카테고리 → 상자 선택 → `Loot Items`에서 편집한다. `OWN TABLE`인 상자 자신만 바뀐다.
2. 다른 상자의 목록을 출발점으로 쓰려면 A에서 `Copy Table` → B 선택 → `Paste Table...` → 확인.
3. 복사는 아이템 목록·수량·등급 내부 비중의 깊은 복사다. A/B 사이에 연결을 만들지 않으며 이후 각각 수정한다.
4. B의 전체 칸·최소/최대 생성 칸·등급 비중·카테고리는 붙여넣기로 변경하지 않는다.
5. 편집 후 `Save`. Copy 클립보드는 창을 닫거나 재컴파일하면 초기화된다.

신규 프리팹 등록도 선택한 상자의 목록을 별도 에셋으로 복사한다. 같은 프리팹 재등록은 기존 데이터 선택만 수행한다. 변형 프리팹 등록은 부모 상자의 기록을 건드리지 않고 새 기록과 테이블을 만든다.

`Use Shared List`, 공유 목록 선택/생성 패널, 공유 테이블 덮어쓰기 버튼은 제거했다. Project에서 기록을 Ctrl+D해 같은 테이블을 참조하게 된 경우 Loot Maker의 목록 편집을 막고 `Separate This Box`로 분리하도록 안내한다. Paste는 필요한 경우 먼저 분리한다. Item Maker의 Loot Links에서도 공유/미사용 테이블 편집을 막고 실제 상자의 테이블만 제안한다.

## 데이터 보존

- 각 상자의 현재 유효 목록을 새 테이블로 복제한 뒤 `_lootTable`에 연결하고 `_lootObjectType`을 비운다.
- 예전 family에서 가져오던 카테고리와 생성 칸 범위를 상자에 명시적으로 고정한다.
- 아이템 참조, 수량, 수동 비중, 등급 행, 이름/현지화, 프리팹 연결, 크기, 등급 확률을 분리 전후 비교했다.
- Unity에만 남아 있던 백팩 목록 35개와 Small02의 별도 연결도 보존했다. 저장 파일만 기준으로 분리하면 잃을 수 있어 현재 Unity의 작성 상태를 새 에셋에 저장했다.
- 결과: Medium01~04와 Small01은 각각 35개, Small02는 33개. 내용이 같아 보이는 것은 복사 당시 목록이 같기 때문이며 에셋 참조는 모두 다르다.
- 원래 비어 있던 군용/금고/금속/보물 상자는 그대로 비어 있다. 임의 아이템을 추가하지 않았다.
- 옛 테이블 42개는 SHA256 기준 모두 변경하지 않았다. 옛 family/원본 테이블은 삭제하지 않았지만 현재 상자와 공유 연결은 없다. 호환 읽기 코드는 예전 데이터/테스트용으로만 남겼다.
- Enemy Drops의 전역 설정 구조는 이번 컨테이너 분리와 별개이며 변경하지 않았다.

## 검증

- 실제 Unity: 런타임/저작 기능 테스트 70개 통과. 이 변경과 무관한 삭제/이름 변경 테스트는 이번 실행에서 제외했다.
- 동일 코드에 대해 후속 마이그레이션·재로드·보존·UI 검증을 합산한 최종 보고: **209 checks passed**, 최종 실패 0.
- 별도 검사 창에서 Copy/Paste 표시와 Copy 클릭을 검사했다. 실제 상자에 Paste를 실행하지 않고, 내용 복사와 형제 보존은 임시 에셋 테스트로 확인했다.
- 저장 파일 독립 감사: `node Tools/LootMaker/verify-independent-tables.cjs` → 27개 기록, 27개 고유 테이블, family 연결 0개.
- 보고서: `Library/LootOwnership-20260929/result-v3.txt`(70개 기능 테스트), `result-v4.txt`(최종 분리/검증). 초기 시도는 저장 파일/메모리 불일치 안전 검사에서 실제 분리 전에 중단했다.
- 임시 실행 스크립트와 meta는 Assets에서 제거했다. 실행 소스는 `Tools/LootMaker/migrate-independent-loot-tables.cs.txt`로 보관하며 자동 실행되지 않는다.
- 재컴파일로는 마이그레이션이 다시 실행되지 않는다. 사용자 작업 씬을 열거나 저장하지 않았다.

## 주요 구현 위치

후속 카테고리 확률 기능: Copy/Paste는 이제 등급별 카테고리 비중도 깊은 복사한다. 독립 소유 원칙과 상자 크기/등급 비중 보존은 그대로다. 사용법과 검증은 `Loot-Maker-Category-Chances-2026-09-29.md` 참고.

- `Assets/Editor/LootMakerAuthoring.cs`: 독립 생성, 깊은 복사/붙여넣기, 중복 연결 검사, 신규 등록.
- `Assets/Editor/LootProgressionEditorWindow*.cs`: 상자별 편집과 공유 UI 제거.
- `Assets/Editor/ItemMakerWindow.cs`: 실제 상자 테이블만 편집/제안.
- `Assets/Scripts/Gameplay/Items/Loot/LootObjectSO.cs`: 카테고리/생성 범위를 보존하는 독립 테이블 연결.
- `Assets/Tests/EditMode/Editor/LootMakerAuthoringTests.cs`: 독립 복사·등록·붙여넣기·중복 기록 방어 회귀 테스트.
