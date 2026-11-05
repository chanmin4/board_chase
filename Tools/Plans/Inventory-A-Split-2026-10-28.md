# 인벤토리 A안 적용 (2026-10-28)

## 적용 결과

사용자가 승인한 A안: 두 패널 모두 왼쪽 패시브/장비, 오른쪽 포켓/인벤토리로 배치했다. 기존 원형 장비·패시브 슬롯과 사각 수납 슬롯 프리팹/스프라이트를 재사용했다. AI 이미지 자체를 게임 배경으로 넣거나 새 아이템 그림으로 교체하지 않았다.

- 플레이어·적 패널: 각각 900×760, 중심 X -470/+470. 기존 상시 중앙 설명 열은 제거하고 선택 시 임시 설명 오버레이로 유지.
- 왼쪽: 폭 260. 후속 요청에 따라 패시브는 3+2, 장비는 4+3 배치로 변경. 장비 슬롯의 기존 기능/참조 유지.
- 오른쪽: 폭 576. 포켓 위, 인벤토리 아래.
- 수납 셀: 기존 120→88 (약 26.7% 축소), 간격 6, 6열.
- 인벤토리 뷰포트: 5×88 + 4×6 = 464 높이. 5줄/최대 30칸까지 한 번에 표시하며 그 이후는 세로 스크롤.
- 실제 인벤토리 용량·포켓 용량은 변경하지 않았다. 빈 용량을 새로 생성하거나 30칸으로 강제 확장하지 않는다.
- 포켓은 기본 한 줄 표시. 6칸을 초과해 확장되면 포켓 자체를 스크롤하며 아래 인벤의 5줄 표시를 밀어내지 않는다.

## 플레이어·적 데이터 경계

기존 플레이어 포켓/인벤의 절대 슬롯 인덱스를 유지한다. 적은 기존 ItemStacks의 앞 PocketCapacity개를 포켓으로 표시하고 나머지를 인벤으로 표시하되, 클릭/드래그의 ItemSlotIndexBase + 원본 인덱스는 그대로 유지한다.

적의 기존 방탄·무기 2칸·탄약 3칸은 재사용했다. 적 패시브는 플레이어의 기존 원형 슬롯 외형을 재사용하되 컴포넌트의 클릭/드래그를 비활성화하고 기존 EnemyPassiveHoverUI로 효과만 확인한다. 런타임에 패시브 줄/슬롯을 새로 생성하던 코드는 제거했다.

적은 아직 장착 가스마스크를 소유·이전하는 런타임 API가 없다. 같은 위치의 헤드 슬롯 외형은 프리팹에 미리 배치했지만 빈칸·상호작용 불가로 유지한다. 가짜 장비를 표시하거나 없는 아이템을 지급하지 않았다. 실제 적 가스마스크 소유/드롭 시스템 확장은 이 UI 변경에 포함하지 않았다.

상자/구조물 루팅은 캐릭터 전용 패시브·장비·포켓 영역을 숨기고 기존 아이템 인벤토리만 표시한다. 장비 데이터/적 HFSM/전투 상태는 변경하지 않았다.

## 프리팹 저장

PlayerInventoryPanel / EnemyInventoryPanel에 포켓 스크롤 뷰포트, 적의 패시브 영역, 적의 예약 헤드 뷰를 기존 슬롯 외형으로 미리 배치했다. Canvas-GamePlay의 오래된 내부 좌표 덮어쓰기를 제거하고 새 패널 크기/위치를 저장했다. 런타임은 동일한 InventorySplitLayout 기하를 재적용할 뿐 새 시각 슬롯을 만들지 않는다. 아이템 용량에 맞춘 기존 InventoryItemSlotUI 인스턴싱은 그대로다.

## 변경 파일

- Assets/Prefabs/UI/GamePlay/PanelHub/Inventory/PlayerInventoryPanel.prefab
- Assets/Prefabs/UI/GamePlay/PanelHub/Inventory/EnemyInventoryPanel.prefab
- Assets/Prefabs/UI/GamePlay/Canvas-GamePlay.prefab
- Assets/Scripts/Presentation/UI/Gameplay/PanelHub/Inventory/InventorySplitLayout.cs (+ meta)
- Assets/Scripts/Presentation/UI/Gameplay/PanelHub/Inventory/PlayerInventoryUI.cs
- Assets/Scripts/Presentation/UI/Gameplay/PanelHub/Inventory/EnemyInventoryUI.cs
- Assets/Scripts/Presentation/UI/Gameplay/PanelHub/Inventory/EnemyInventoryUI.Passives.cs
- Assets/Scripts/Presentation/UI/Gameplay/PanelHub/Inventory/PassiveInventorySlotUI.cs
- Assets/Scripts/Presentation/UI/Gameplay/PanelHub/PlayerPanelHub.cs
- Assets/Scripts/Presentation/UI/Gameplay/PanelHub/Inventory/PlayerItemDetailPanelUI.cs
- Assets/Tests/EditMode/Editor/InfectionAggravationTests.cs (기존 세로 배치 단언을 A안으로 갱신)
- Assets/Tests/EditMode/Editor/InventorySplitVerification.cs (+ meta; 명시적 배치 검증/임시 렌더 실행기)
- Tools/InventorySplit/author-layout.cjs (직접 쓰기 없이 apply_patch 입력만 생성)
- Tools/InventorySplit/verify-layout.cjs
- Tools/GameSystems/verify-inventory-layout.cjs (새 검증 진입점으로 연결)

## 검증

### 후속 변경: 장비 4+3, 패시브 확대

- 장비 첫 줄: 방어구 / 감염방호 / 무기 1 / 무기 2. 기존 크기의 56%로 나란히 배치.
- 둘째 줄: 기존 퀵슬롯 3개. 이전 78%에서 68%로 소폭 축소. 적은 기존 탄약 슬롯 동작을 그대로 유지한다.
- 패시브: 45% → 72% (직전 대비 가로·세로 약 60% 확대), 3개+2개 두 줄. 5칸 수량과 잠금 규칙은 그대로다.
- 패시브 영역 높이 272, 장비 영역 Y 368 / 높이 260. 프리팹 좌표와 런타임 공통 기하를 함께 갱신했다.
- 포켓 / 인벤토리 / 패널 크기·위치와 5줄 스크롤은 변경하지 않았다.
- 추가 변경 파일: EquipmentInventoryGasMaskSlotUI.cs (`HEAD` → `감염방호` 표시).
- 후속 정적 배치·참조 검사 3,602개 통과. Unity 참조 런타임/에디터 컴파일 오류 0개.
- 아래 네이티브 Unity 라이선스 문제는 앞선 실행 결과다. 이번에는 Unity를 다시 실행하지 않았으며 실제 Play Mode 화면·드래그 검증을 완료한 것으로 보지 않는다.

### 최초 A안 검증 기록

- Unity 참조 분리 컴파일: 런타임/에디터 오류 0개. 기존 런타임 경고 38개.
- 프리팹 문서 ID 중복, 로컬 참조, 부모/자식 연결, 스크롤 콘텐츠 참조, 열/행 기하, 적 원본 인덱스/패시브 확인 전용 경계: 3,470개 검사 통과.
- 기존 시스템 관리형 검사 3,907개, 패시브/모듈 4,257개, 스태미나 73개, 상태 피해 상호작용 예외 17개 통과.
- dotnet build --no-restore는 기존 project.assets.json 부재(NETSDK1004)로 실행 불가. 분리 참조 컴파일과 구분한다.
- 실제 Unity 프리팹 렌더/5행 뷰포트/6행 스크롤 검증을 두 차례 시도했으나, 이 환경에서 Unity 라이선스가 없어 프로젝트 로드 전에 종료됐다. 로그: Library/InventorySplitVerification/unity-native.log. 인증/라이선스 설정은 변경하지 않았다.
- 따라서 실제 Play Mode 픽셀 배치·마우스 드래그·스크롤은 미검증이다. 새 검증 실행기는 라이선스가 있는 환경에서 InventorySplitVerification.Run으로 실행 가능하며, 임시 테스트 아이템만 사용하고 씬·에셋·저장을 저장하지 않는다.
- Notion 및 플레이어 저장 파일은 수정하지 않았다.
