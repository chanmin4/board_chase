# Valuable 물품 분류 패치 · 2026-10-29

## 적용 내용과 사용 위치

Item Maker 상단 **물품 분류 관리**에서 분류 추가·이름 변경·순서 변경·삭제가 가능하다. 분류 정보는 `Assets/Resources/ValuableCategories.asset` 한 곳에서 공유한다. 이름과 분리된 고정 ID를 아이템·루팅 가중치가 사용하므로 이름을 바꿔도 참조가 끊어지지 않는다. 일반 Inspector에서는 고정 ID를 직접 수정하지 않게 했다.

Item Maker 왼쪽 Valuables를 선택하면 전체/미분류/각 물품 분류 필터가 나온다. 기존 카드의 Ctrl/Shift 다중 선택 후 해당 분류로 드래그해 일괄 지정할 수 있다. 개별 Item Data와 Valuable SO Inspector에도 물품 분류 선택이 있다. 관리 창의 **분류 및 물품 저장** 또는 Unity 저장으로 에셋 변경을 저장한다. 이름 변경·분류 이동·삭제는 Undo를 지원한다. Play Mode에서는 분류 관리 변경이 잠긴다.

분류 삭제 시 확인창에서 사용 아이템 개수를 보여주고 해당 아이템만 미분류로 되돌린다. 아이템·아이콘·프리팹·GUID를 삭제하지 않는다. 기존 루팅 가중치 레코드는 보존되지만 삭제된 고정 ID의 레코드는 비활성 상태로 남는다. 같은 이름을 다시 생성하면 새 ID이므로 이전 가중치를 자동 상속하지 않는다.

툴 카드/등록 정보, 인게임 아이템 상세, 마우스 툴팁에서 Valuable의 물품 분류를 표시한다.

플레이어 포켓·가방 헤더에 **분류** 정렬 버튼을 기존 프리팹에 미리 배치하고 OnClick을 연결했다. 기존 모으기·희귀도 정렬은 유지했다. 분류 정렬은 아이템 종류 → 물품 분류 순서 → 희귀도 → 에셋 이름 순이며 빈칸은 뒤로 보낸다. 포켓과 가방은 서로 섞지 않으며 스택 객체 자체를 옮기므로 개수·내구도 등 개체 데이터가 보존된다. 적 장착품의 확인 전용/소유권 규칙은 변경하지 않았다.

## 기본 분류와 기존 콘텐츠

- 도구: 9개
- 건설자재: 4개
- 군용물품: 0개 — 분류만 마련, 신규 물품을 임의로 만들지 않음
- 의료물품: 1개
- 생활품: 10개
- 전자부품: 3개
- 식료품: 9개
- 수집품: 8개
- 미분류: Cobweb 1개 — 의미가 모호하여 임의 배정하지 않음

기존 Valuable 45개 중 44개를 초기 분류했다. 예: 드릴/망치=도구, 케이블/나사/테이프=건설자재, Pillbox=의료물품, 배터리=전자부품, MarsModel=수집품. 모두 수정 가능하다. 기존 물품의 Script/GUID·가격·수량·희귀도·상점·퀘스트 참조는 유지했다.

## Loot Maker 연결

상자의 아이템 추가 목록에서 `귀중품 / 도구`, `귀중품 / 건설자재` 등으로 구분한다. 분류 필터·검색·분류 선택·선택 항목 일괄 추가는 기존 등록 UI를 그대로 사용한다. 이미 등록된 아이템도 물품 분류별로 묶이며 서로 다른 Valuable 분류의 접기/펼치기와 비중을 따로 조절할 수 있다. 해당 목록에 분류 관리 창 연결도 있다.

각 희귀도에서 **Custom category chances**를 켜고 분류별 Category Weight를 설정한다. 예를 들어 도구 70, 건설자재 30이면 두 분류만 유효할 때 70:30으로 추첨한다. 다른 유효 분류가 있으면 그 비중까지 합해 정규화한다. 0은 해당 분류를 추첨에서 제외한다. 비중을 올리려는 분류의 아이템을 먼저 해당 상자의 후보 목록에 등록해야 한다. 빈 분류에서는 아이템을 생성하지 않는다.

분류 추가·수정 자체가 모든 상자에 해당 아이템을 자동 등록하는 것은 아니다. 분류 목록에서 일괄 추가하는 방식이며 창고 상자 등을 임의의 확률로 재조정하지 않았다.

자동 확률 모드는 기존 개별 아이템 확률을 유지한다. 구형 커스텀 테이블의 `Valuable` 통합 비중은 새 하위 분류에 기존 개별 비율대로 나누어 적용한다. 처음 분류 비중을 직접 편집할 때는 기존 하위 비중과 혼합된 수동/자동 아이템 비율을 명시적 값으로 환산해 형제 분류의 확률이 뜻하지 않게 바뀌지 않도록 했다. 이때 해당 구형 Valuable 항목의 수동 비율 값은 같은 실제 확률을 나타내는 분류 내부 비율로 바뀔 수 있다.

## 검증과 한계

- 실제 프로덕션 분류·루팅 선택·정렬 및 분류 비중 편집 메서드를 사용한 관리형 검사: 2,025개 통과. Unity/스토리지는 테스트 대역이다.
- Valuable 에셋/분류/정렬 버튼 연결 계약: 158개 통과.
- 기존 분리형 인벤토리 프리팹 참조·배치 검사: 4,083개 통과.
- 기존 패시브 회귀 검사: 299개 통과.
- 기존 게임 시스템 회귀 검사: 3,916개 통과.
- 기존 패시브 등 콘텐츠 에셋 계약: 220개 통과.
- Unity 실제 참조 기반 C# 컴파일: 런타임·에디터 오류 0개. 런타임 기존 경고 38개.
- 일반 `dotnet build Assembly-CSharp.csproj --no-restore -v minimal`은 기존 `Temp/obj/Assembly-CSharp/project.assets.json` 부재로 NETSDK1004. 이를 성공으로 간주하지 않았으며 별도 Unity 참조 컴파일을 수행했다.
- 네이티브 EditMode 테스트 5개를 추가했으나 Unity Test Runner 실행·Play Mode 렌더링·실제 저장 후 재접속 검증은 아직 하지 않았다.
- 에셋 생성 스크립트는 apply_patch 텍스트만 출력한다. 재실행 시 패치 0개로 멱등성을 확인했다. 씬 자동 수정·런타임 UI 생성은 추가하지 않았다.
- 노션 및 아이템 정리 DB는 수정하지 않았다.

## 변경 파일

런타임:

- `Assets/Scripts/Gameplay/Items/Definitions/ValuableCategoryCatalogSO.cs` 및 meta (신규)
- `Assets/Scripts/Gameplay/Items/Definitions/ValuableItemSO.cs`
- `Assets/Scripts/Gameplay/Items/Loot/LootItemCategories.cs`
- `Assets/Scripts/Gameplay/Items/Loot/LootCategorySelection.cs`
- `Assets/Scripts/Gameplay/Entities/Shooter/Player/Inventory/PlayerInventoryRuntime.Sorting.cs`
- `Assets/Scripts/Presentation/UI/Gameplay/PanelHub/Inventory/PlayerInventoryUI.cs`
- `Assets/Scripts/Presentation/UI/Gameplay/PanelHub/Inventory/PlayerItemDetailPanelUI.cs`
- `Assets/Scripts/Presentation/UI/Gameplay/PanelHub/PlayerTooltipUI.cs`

에디터:

- `Assets/Editor/ItemMakerWindow.Valuables.cs` 및 meta (신규)
- `Assets/Editor/ItemMakerWindow.cs`
- `Assets/Editor/ItemMakerCatalogView.cs`
- `Assets/Editor/LootMakerItemView.cs`
- `Assets/Editor/LootMakerAuthoring.cs`
- `Assets/Editor/LootProgressionEditorWindow.cs`
- `Assets/Editor/LootProgressionEditorWindow.Registration.cs`
- `Assets/Editor/MakerRegistrationUi.cs`

콘텐츠:

- `Assets/Resources/ValuableCategories.asset` 및 meta (신규)
- `Assets/Prefabs/UI/GamePlay/PanelHub/Inventory/PlayerInventoryPanel.prefab`
- `Assets/ScriptableObject/GameScene/GameItem/Valuable/` 아래 기존 44개 에셋: Drill, Hammer, PipeRench, Pliers, Saw, Scissor, Screwdriver, Shovel, Spanner, Cable, FuelCan, Screwbolt, Tape, Pillbox, AbandonedBoots, Candle, Canteen, Eraser, Fork, Lighter, MatchBox, Pencil, Spoon, Stapler, BigBattery, Calculator, SmallBattery, Cake, CannedFood, Chicken, Cookie, Milk, Mushroom, Peanutbutter, TomatoSoup, Wine, Crown, EarthModel, GoldCoin, MarsModel, MercuryModel, MoonModel, Ring, Trophy.

테스트·검사·기록:

- `Assets/Tests/EditMode/Editor/ValuableCategoryTests.cs` 및 meta (신규)
- `Tools/GameSystems/VerifyValuableCategories.ps1` (신규)
- `Tools/InventorySplit/author-valuable-categories.cjs` (신규)
- `Tools/InventorySplit/verify-valuable-categories.cjs` (신규)
- 본 문서 (신규)
