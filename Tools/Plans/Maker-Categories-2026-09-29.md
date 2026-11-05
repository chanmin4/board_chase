# Maker 카테고리 접기 / 펼치기

## 적용 범위

- Item Maker: Weapons / Bullets / Consumables의 하위 분류를 폴더처럼 접는다. 다른 독립 분류는 그대로 보인다.
- Loot Maker: 각 분류 아래에 해당 상자 이름을 펼쳐서 선택할 수 있다. 가운데 카드 목록은 기존대로 유지한다.
- Quest Maker: MainQuest / StatUnlock / GearUnlock 아래에 퀘스트 목록을 펼친다. 그래프의 브랜치 필터·선행 링크 편집은 그대로다.
- Sector Maker는 카테고리 트리가 아닌 작업 모드/배치 도구여서 이번 변경 대상이 아니다.

## 조작

- 왼쪽 화살표는 접기/펼치기만 한다. 분류 이름은 필터 선택만 한다. 각각 클릭 영역이 분리되어 있다.
- Expand All / Collapse All은 해당 Maker의 폴더를 한꺼번에 펼치거나 접는다. 현재 필터, 선택 항목, 편집 데이터는 바꾸지 않는다.
- 처음에는 접힌 상태이며, 프로젝트 경로와 Maker별 EditorPrefs 키에 상태를 기억한다. 창을 닫거나 다시 컴파일해도 펼침 상태는 유지된다.
- Item/Loot의 기존 카드 드래그 대상은 분류 행 전체다. 부모가 접혀 있어도 드롭 대상이다. 기존 타입 변환 확인/Undo 등 이동 처리는 변경하지 않는다.
- Item에서 생성/이동/복구로 하위 분류를 선택하면 해당 부모를 펼쳐 준다.
- Loot에서 펼친 상자 이름을 클릭하면 해당 분류와 상자를 선택하고 기존 검색어를 비운다.
- Quest 검색은 일치하는 퀘스트가 속한 브랜치를 펼친다. 그래프에서 퀘스트를 선택해도 해당 브랜치를 펼친다. 브랜치를 접어도 그래프의 필터나 선택은 유지된다.
- 긴 이름은 마우스를 올리면 툴팁으로 전체 이름/경로 또는 ID를 확인할 수 있다.

## 코드와 검증

- 공용 UI/상태: `Assets/Scripts/Authoring/Editor/AuthoringWorkspaceUi.cs`의 `MakerCategoryTreeUi`.
- 적용 창: `ItemMakerWindow`, `LootProgressionEditorWindow`, `QuestDesignTreeWindow`.
- 영구 회귀 테스트: `Assets/Tests/EditMode/Editor/MakerCategoryTreeTests.cs`.
- 편집기 컴파일 통과. 실제 Unity에서 상태/히트 영역 테스트 5개와 별도 창의 MouseDown/MouseUp/DragUpdated 상호작용 검사 29개, 총 **34개 통과**.
- 화살표 클릭 시 필터 유지, 자식 선택, 접기 시 선택 유지, 이름 클릭과 펼침 분리, 전체 접기/펼치기, 접힌 Item/Loot 분류의 드래그 허용을 검사했다. DragPerform는 보내지 않았으므로 에셋 이동은 실행하지 않았다.
- 임시 검사 창만 열고 닫았고, 검사 전 EditorPrefs와 DragAndDrop 상태를 복구했다. 보고서: `Library/MakerCategoryTree-20260929/verification-v1.txt`.
- 실행 브리지는 Assets에서 제거하고 수동 재현 소스만 `Tools/LootMaker/verify-maker-category-tree.cs.txt`에 보관한다.

런타임 코드, 아이템/루팅/퀘스트 에셋, 프리팹, 씬은 수정하지 않는 에디터 표시 변경이다.

## 후속: Item Maker 카테고리 세로 공간

- 유동 GUILayout 스크롤 영역이 최소 100 높이에 머물며 아래에 빈 공간을 남기던 구성을 명시적 뷰포트로 교체했다. 생성 영역을 제외한 남는 높이를 카테고리가 사용한다.
- 카테고리와 `CREATE ITEM` 사이 가로 경계선을 위아래로 드래그해 높이를 나눈다. 아래로 끌면 목록이 커진다. 경계선 더블클릭은 기본 분할로 복원한다.
- `CREATE ITEM` 제목을 클릭하면 생성 폼을 접고 남는 공간을 목록에 돌려준다. 다시 펼치면 이전 분할 높이를 복원한다.
- 생성 폼은 독립 스크롤을 갖는다. 작은 창에서도 목록과 생성 폼 각각 최소 높이를 확보하며, 창 크기로 일시 제한된 값은 사용자의 저장 높이를 덮어쓰지 않는다.
- 프로젝트별 `BoardChase.ItemMaker.Sidebar.v1.<Application.dataPath>` EditorPrefs에 높이/펼침 상태를 저장한다. 창 닫기·재컴파일 후 복원된다. 에셋 저장과는 무관하다.
- 생성 폼의 라벨 폭을 줄이고 폴더 경로는 전체 폭으로 표시해 과도한 줄바꿈을 줄였다. 생성·선택·분류 이동 로직은 그대로다.
- `MakerCategoryTreeTests`에 공간 배분, 드래그 높이 관계, 접기, 경계값, 창 크기 변경, 잘못된 저장 값 등 6개 회귀 테스트를 추가했다.
- 컴파일 통과. 실제 Unity에서 회귀 테스트 11개와 UI 검사 18개 통과: 남는 높이 사용, 양방향 드래그, 저장, 더블클릭 복원, 접기/펼치기, 창 재실행, 극단적 드래그, 최소 창 크기에서 폼 접근과 저장 높이 보존. 보고서: `Library/MakerCategoryTree-20260929/item-sidebar-v2.txt`.
- 위 실행의 다음 항목(창 재확대 후 최초 창과 목록 높이가 정확히 같다는 검사)은 실패했다. 최초 `ShowUtility` 창은 화면에 맞춰 1331x825로 조정되므로 요청한 1380x900을 전제로 한 절대 높이 비교를 완료 검증으로 취급하지 않았다. 재현 소스에서는 실제 뷰포트와 저장된 패널 높이를 비교하도록 보정했으며, 이 보정과 이후 분류 선택/드래그 재검사는 이번 실행에서 다시 돌리지 않았다. 해당 분류 입력 로직은 변경하지 않았다.
- 임시 실행 스크립트와 메타는 Assets에서 제거했다. 수동 재현용 소스만 `Tools/LootMaker/verify-item-sidebar.cs.txt`에 남긴다.
