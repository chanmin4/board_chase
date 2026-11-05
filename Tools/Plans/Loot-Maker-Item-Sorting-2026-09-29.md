# Loot Maker 목록 정렬 (2026-09-29)

- Container Loot → Loot Items의 기본 표시: **등급 → 아이템 카테고리 → 이름 A–Z**.
- 등급: Normal → Rare → Unique → Legendary. 기존 등급 접기/펼치기 유지.
- 각 등급 안에 Armor, Passives 등의 카테고리 제목 표시. 분류와 순서는 Item Maker의 기존 카테고리 기준을 재사용한다. 상자 카테고리(Ammo/Medical/Safe 등)와는 별개다.
- 이름은 표시 중인 ItemSO 에셋 이름을 대소문자 구분 없이 비교한다. 동일 이름은 대소문자·에셋 경로·원래 인덱스로 안정적으로 정렬한다.
- 표시용 복사 목록만 정렬한다. 원본 tier/items 배열, 수량, 확률, 상자별 독립 테이블 연결은 바꾸지 않는다. 따라서 이 표시 변경으로 런타임 추첨 순서도 변경되지 않는다.
- 행마다 원본 인덱스와 엔트리 참조를 유지한다. 수량/비율 편집은 해당 엔트리에 적용하며 ×는 표시 순서가 아닌 정확한 원본 인덱스를 삭제한다.
- 아이템 참조가 없는 엔트리는 Missing Items로 마지막에 표시하여 삭제 가능. 아이템의 현재 등급과 테이블 등급이 다르면 기존 경고를 유지하며 자동 이동하지 않는다.

## 검증

- Editor 컴파일 통과.
- 실제 Unity **14 checks passed**: 단위 테스트 7개 + 실제 IMGUI 검증 7개.
- 등급 순서, 분류 후 이름순, 원본 데이터/수량/비율 보존, 동일 이름/중복 참조/누락 참조, 실시간 이름 변경, 화면 카테고리 제목, 등급 접기/펼치기, 정렬된 첫 줄 ×가 정확한 아이템만 삭제하는 것을 확인했다.
- 테스트는 메모리의 임시 아이템/테이블과 별도 창만 사용했다. 실제 에셋·씬·프리팹 저장 없음.
- 결과: `Library/LootMakerItemSort-20260929/result-v1.txt`.
- 임시 자동 실행 스크립트/meta 제거 완료. 수동 재실행 참고 소스: `Tools/LootMaker/verify-loot-item-sort.cs.txt`.

## 구현

- `Assets/Editor/LootMakerItemView.cs`: 비파괴 표시 정렬과 원본 행 매핑.
- `Assets/Editor/LootProgressionEditorWindow.cs`: 카테고리 제목 및 정렬 행 표시/삭제.
- `Assets/Editor/ItemMakerWindow.cs`: 기존 카테고리 순서/이름을 읽는 내부 API 추가. Item Maker 자체 정렬은 변경하지 않음.
- `Assets/Tests/EditMode/Editor/LootMakerItemViewTests.cs`: 회귀 테스트.

## 후속: 등급 안의 카테고리 접기/펼치기

- Armor, Passives 등 카테고리 제목을 아이템 개수가 표시되는 Foldout으로 변경했다. 제목/화살표를 눌러 해당 분류만 접거나 펼친다.
- `Expand Categories` / `Collapse Categories`: 선택한 상자의 모든 등급 안 카테고리를 한 번에 펼치거나 접는다. 등급 자체의 접기 상태는 바꾸지 않는다.
- 접기 상태는 프로젝트 + 테이블 GUID + 등급 + 안정적인 카테고리 식별자로 EditorPrefs에 저장한다. 상자/등급/분류별 독립이며 테이블 이름 변경·창 재열기에도 유지된다. 기본값은 펼침이다.
- 등급을 접었다 다시 펴도 하위 카테고리 상태를 기억한다. 접힌 아이템은 화면에서만 숨기며 추첨 대상에서 제외하지 않는다.
- 데이터/확률/수량/추첨 순서는 변경하지 않는다. 다시 펼친 뒤 삭제해도 정렬 행의 원본 인덱스를 정확히 사용한다.
- Editor 컴파일 및 실제 Unity **25개 검사 통과** (정렬/접기 상태 단위 테스트 10개 + 실제 클릭/재열기 UI 검사 15개).
- 검증 보고서: `Library/LootMakerItemSort-20260929/category-foldouts-v1.txt`. 임시 실행 스크립트/meta는 Assets에서 제거했으며 소스는 `Tools/LootMaker/verify-loot-category-foldouts.cs.txt`에 보관했다. 실제 에셋/씬 저장 없이 임시 메모리 데이터만 검사했다.
