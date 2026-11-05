# 레이드 패시브 개편 브리핑 · 2026-10-29

## 완료 범위

Notion의 ‘레이드 패시브 개편’과 이번 대화의 귀환 처리 답변을 기준으로 코드·프리팹·기본 데이터를 반영했다. 이 작업 중 크레딧/사용량 제한 오류로 중단된 도구 호출은 없었다.

감염 확산, 7스테이지 분기, 무기 추가, Valuable 카테고리, 상세 퀘스트라인, 다른 아이템 전체의 사용 레벨 삭제는 이번 변경에 포함하지 않았다. 노션도 수정하지 않았다.

## 1. 패시브 수명과 소유권

- 레이드에서도 장착·교체·해제를 허용한다. 효과는 원정 중에만 계산한다. HOME에 준비해 둔 장착품은 출발 후 효과를 적용한다.
- 장착 시 기존 개체 귀속을 사용한다. 타인의 귀속품 획득/양도 금지는 유지한다.
- 교체/해제한 개체에는 비활성 상태를 저장한다. 재장착과 수치 리롤로의 재활성화를 막는다.
- 장착 중이거나 장착 이력이 있거나 비활성화된 패시브는 시간제 분해기에 등록할 수 없다. 즉시 우클릭 분해는 다시 만들지 않았다.
- 귀환 시 장착했던 것·해제한 것은 소멸한다. 한 번도 장착하지 않은 패시브는 HOME에 반입해 시간제 분해할 수 있다. 이는 사용자가 이번에 확인한 규칙이다.
- 사망 시 기존 소지품 전체 손실을 유지한다. 해치로 다음 스테이지에 가는 것은 원정 종료가 아니므로 패시브/추첨 횟수를 초기화하지 않는다.
- 추첨/수치 리롤은 살아 있는 플레이어의 HOME 준비 상태에서만 허용한다. 원정 로딩/진행/게임오버 상태에서 시작 추첨을 새로 할 수 없도록 구분했다.

## 2. 전투 4칸 + 생존 4칸

플레이어·적 인벤토리 프리팹에 기존 원형 아트를 복사해 8칸을 미리 배치했다. 런타임에 새 패시브 슬롯을 만드는 방식이 아니다.

- 위 행 4칸: 전투.
- 아래 행 4칸: 생존.
- 내부 인덱스는 전투1, 생존1, 전투2, 생존2 순으로 교차 저장한다.
- 처음에는 각각 1칸. 나머지는 레벨 해금이다.
- 기존 퀘스트/프로필의 단일 ‘2~5칸’ 수치는 새 슬롯 해금을 우회하지 않는다. 옛 슬롯 보상 콘텐츠를 새 레벨 구조에 맞추는 작업은 후속 퀘스트 설계에서 정리해야 한다.
- 슬롯 종류가 다르면 장착할 수 없다. 동일 효과 중복 장착 제한은 유지한다.
- 적도 카테고리별로 표시하며 장착품은 확인 전용이다. 빈 적 슬롯에 플레이어 해금 레벨을 표시하지 않는다.
- 적 설정의 총 슬롯 상한은 8로 확장했지만 기존 역할별 수량은 유지한다. 추첨은 카테고리당 4개를 넘지 않아 UI에서 효과가 숨겨지지 않는다.

### 임시 해금 레벨

- 전투: 1 / 5 / 10 / 15.
- 생존: 1 / 7 / 12 / 18.

미정 수치를 위한 테스트값이며 Game System Tool에서 바꿀 수 있다. 기존 장비·포켓·수납 위치와 6열/5줄/이후 스크롤은 유지했고, 패시브 슬롯만 4+4 배치에 맞춰 크기와 위치를 조정했다. 실제 가독성·드래그·툴팁 위치는 Play Mode 확인이 필요하다.

## 3. 통합 모듈과 시작 추첨

기존 Normal 패시브 파츠의 GUID를 유지해 ‘통합 패시브 모듈’로 사용한다. 다른 구형 등급 파츠와 보유량은 삭제하지 않았다. 수치 리롤에는 기존 등급 파츠를 호환 재료로 사용할 수 있다.

- 미장착품 시간제 분해 결과는 통합 모듈로 지급한다.
- HOME 인벤토리에 ‘시작 패시브 추첨’ 버튼을 추가했다. 기존 분해기와 같은 UI 방식이며 두 팝업이 겹치지 않게 한다.
- 추첨 결과는 인벤토리에 지급한다. 전투/생존 슬롯에 드래그해 준비한다.
- 빈 공간·모듈·레벨·허용 등급·실제 후보를 먼저 검사한다. 실패하면 모듈과 횟수를 소비하지 않는다.
- 모듈 결제와 결과 지급은 같은 동기 트랜잭션에서 처리한다.
- 추첨 횟수와 출발 확정 상태를 저장한다. 재접속·해치 이동으로 횟수를 다시 받지 않는다. HOME 준비를 취소/다시 여는 것만으로도 초기화하지 않는다.
- 실제 새 원정 시작 시 준비를 확정하고, 귀환/사망 처리에서 다음 준비 횟수를 초기화한다. 출발 실패 시 재추첨/환불을 반복하는 별도 정책은 추가하지 않았다.
- 시작 추첨 결과의 분해 반환량에는 ‘해당 추첨 당시 비용 - 1’ 상한을 개체별로 저장한다. 수치 리롤·보관·저장 복원으로 이 상한을 지우지 않는다.

### 임시 추첨 수치

- 저등급: Lv.1, 모듈 10, Normal 80 / Rare 20.
- 중등급: Lv.10, 모듈 25, Normal 50 / Rare 35 / Unique 15.
- 고등급: 기본 OFF, Lv.20/모듈 50/Normal 35·Rare 30·Unique 25·Legendary 10을 편집 가능한 검토값으로만 보관한다. 데모에서 활성화하지 않았다.
- 원정 준비당 횟수: Lv.1에 1회, Lv.10에 2회, Lv.20에 3회.
- 분해 반환량: Normal 1 / Rare 3 / Unique 8 / Legendary 15 / Mythic 25. 시작 추첨품에는 위 비용 상한도 추가 적용한다.
- 분해 시간은 기존 30초를 유지한다.

빈 후보 등급은 추첨 가능한 후보 안에서 비중을 재정규화한다. 후보가 전부 없으면 비용 없이 실패한다. 불활성 모듈은 시작 후보에서 제외했다.

## 4. 적용 강도·경험치·기존 콘텐츠

- 전투·생존 효과 강도를 각각 1.5배의 임시값으로 분리했다. 저장된 개체별 원본 추첨값은 바꾸지 않고 실제 적용 단계에서 조절한다.
- 퍼센트 곱 보정은 기준 1에서의 차이를 조절한다. Override와 기능 플래그는 임의 배율을 적용하지 않는다.
- 기존 장착·교체·해제 공통 쿨다운 10초는 유지하되 설정 원본을 PassiveModuleRules로 옮겨 Game System Tool에서 바꿀 수 있게 했다.
- 패시브 아이템 자체의 사용 레벨 제한은 우회한다. 레벨은 슬롯·추첨을 해금한다. 다른 아이템의 기존 사용 레벨 제한은 이번 범위에서 삭제하지 않았다.
- ‘학습 촉진’ 경험치 증가 패시브를 생존에 추가하고 카탈로그·시작 후보·의료/금고 보급 테이블에 등록했다.
- 경험치 보정은 실제 발견/처치/퀘스트 경험치 획득 순간 적용한다. 패시브를 바꾼 뒤 정산한다고 과거 경험치에 새 배율을 다시 적용하지 않는다. 기존 중복 경험치 획득 방지도 유지한다.
- 예전 ‘억제제 지속’ 효과는 ‘앰플 지속 강화’로 연결했다. 폐기된 강제 억제 유지 시계를 되살리지 않고 현재 앰플의 지속시간을 늘린다.
- 기존 모듈 19종과 구형 패시브 11개는 GUID/개체 추첨값을 유지해 카테고리를 지정했다. 구형 무기 부착물형 패시브의 폐기 스탯을 새 기능으로 복원하지 않았다.
- 플레이어/적 실제 적용과 상세창·공용 툴팁에 같은 효과 강도 배율을 표시한다. 경험치 보정은 %로 표시한다.
- Armory에는 전투 8종, Medical에는 생존 11종, Vault에는 양쪽 19종을 테스트 보급 후보로 등록했다. 수치 범위가 없는 등급에는 등록하지 않아 빈 결과를 만들지 않게 했다.
- 각 후보 행은 수동 비중 0.5로 시작한다. 이는 모든 상자의 최종 등장률 0.5%를 보장한다는 뜻이 아니다. 등급·범주 가중치와 다른 후보 유무로 실제 비율이 달라지므로 Loot Maker 미리보기와 플레이로 조정해야 한다.

장비 60% / 패시브 40% 기여를 실제로 달성했다고 주장하지 않는다. 위 임시 배율과 공급 데이터를 기반으로 후반 화력·조합 고점·평균 빌드를 플레이에서 맞춰야 한다.

## 5. 가방 칸 감소와 저장 호환

- 용량 감소 후 기존 배열을 정렬하거나 압축하지 않고 마지막 절대 슬롯부터 초과 스택을 드롭한다.
- 스폰에 실패하면 원본을 지우지 않고 소유한 배열에 보존한다. 1초 간격으로 재시도한다.
- 드롭 실패로 남은 초과 칸도 저장에 포함한다. 복구 시 최대 4096칸의 유효 인덱스 범위 안에서 배열을 확장해 보존하고, 패시브/장비 복구 후 용량을 다시 계산한다.
- 이 과정에서 방탄·무기·필터 상태를 공용 월드 드롭 API에 그대로 전달한다.
- 패시브 슬롯 스키마 2를 추가했다. 옛 5슬롯 저장은 카테고리에 맞는 해금 슬롯으로 재배치하고, 배치할 수 없는 소유품은 인벤토리에 남긴다.
- 옛 고정 SO 장착 참조는 소유한 개체로 한 번만 변환·귀속한다. 원본 SO를 비활성화하거나 새 추첨값으로 덮어쓰지 않는다.
- 기존 boundToOwner와 새 disabledForRaid/everEquipped/분해 상한을 개체 레코드에 유지한다. Profile/Run 양쪽 갱신 경로도 재사용한다.

## 검증 결과와 남은 경계

- VerifyRaidPassives.ps1: 실제 패시브·개체·슬롯·시작 추첨·초과 드롭 메서드 299개 검사 통과. 관리형 Unity/저장 수명주기 대역을 사용했다.
- VerifyGameSystems.ps1: 획득 시점 경험치 보정·중복·정산 비중복·사용품 분해 금지 등 포함 3,916개 검사 통과.
- verify-layout.cjs: 플레이어/적 8슬롯, 프리팹 로컬 참조, 부모/자식, 위치/크기 등 3,939개 검사 통과.
- verify-passive-raid.cjs: 분류·레벨·추첨·카탈로그·지원 등급별 공급·저장/해치 소스 계약 699개 검사 통과.
- verify-assets.cjs: 기존 전투/에셋 계약 220개 통과.
- 감염 최대 HP 잠식 검사 10,023개, 스태미나 88개 및 13개 공통 설정 검사 통과.
- Unity 실제 참조 기반 분리 컴파일: 런타임 오류 0(기존 경고 38), Editor 오류 0(경고 0).
- 일반 dotnet build --no-restore는 기존 project.assets.json 부재(NETSDK1004)로 실패했다. 성공한 Unity 참조 컴파일과 구분한다.

실제 Unity Play Mode, ES3 파일 저장·재접속, 실제 드래그/스크롤/툴팁, 월드 드롭의 물리 위치와 출발 실패/복귀 콜백 순서는 네이티브 검증이 남아 있다. 컴파일·관리형 검사 통과를 이 검증 완료로 취급하지 않는다.

## 변경 파일 목록

아래 경로는 프로젝트 루트 기준이다. 기존의 다른 미커밋 변경은 보존했다.

### 런타임·저장

- Assets/Scripts/Gameplay/Items/Definitions/PassiveItemSO.cs
- Assets/Scripts/Gameplay/Items/Definitions/PassiveInstanceRuntime.cs
- Assets/Scripts/Gameplay/Items/Definitions/PassiveModuleRulesSO.cs
- Assets/Scripts/Gameplay/Entities/Shooter/Player/Passive/PlayerPassiveInventoryRuntime.cs
- Assets/Scripts/Gameplay/Entities/Shooter/Player/Inventory/PlayerInventoryRuntime.PassiveModules.cs
- Assets/Scripts/Gameplay/Entities/Shooter/Player/Inventory/PlayerInventoryRuntime.PassiveRaid.cs (+ .meta, 신규)
- Assets/Scripts/Gameplay/Entities/Shooter/Player/Inventory/PlayerInventoryRuntime.StartingPassives.cs (+ .meta, 신규)
- Assets/Scripts/Gameplay/Entities/Shooter/Player/Inventory/PlayerInventoryRuntime.Recycling.cs
- Assets/Scripts/Gameplay/Entities/Shooter/Player/Inventory/PlayerInventoryRuntime.RunPersistence.cs
- Assets/Scripts/Gameplay/Entities/Shooter/Player/PlayerStatsRuntime.cs
- Assets/Scripts/Gameplay/Entities/Shooter/Player/PlayerShooterInfection.cs
- Assets/Scripts/Gameplay/Entities/Shooter/Common/Core/StatModifierTypes.cs
- Assets/Scripts/Gameplay/Entities/Shooter/Enemy/Common/EnemyShooterStatsRuntime.cs
- Assets/Scripts/Gameplay/Entities/Shooter/Enemy/Common/EnemyShooterStatsRuntime.Passives.cs
- Assets/Scripts/Gameplay/Entities/Shooter/Enemy/Definitions/EnemyShooterConfigSO.cs
- Assets/Scripts/Gameplay/Progression/GameProgressionRuntime.cs
- Assets/Scripts/Gameplay/Progression/Stage/StageProgressionManager.cs
- Assets/Scripts/Core/Persistence/Save.cs
- Assets/Scripts/Core/Persistence/PlayerRunStatePersistence.cs

### UI·편집 도구·프리팹

- Assets/Scripts/Presentation/UI/Gameplay/PanelHub/Inventory/InventorySplitLayout.cs
- Assets/Scripts/Presentation/UI/Gameplay/PanelHub/Inventory/PassiveInventorySlotUI.cs
- Assets/Scripts/Presentation/UI/Gameplay/PanelHub/Inventory/PlayerInventoryUI.cs
- Assets/Scripts/Presentation/UI/Gameplay/PanelHub/Inventory/EnemyInventoryUI.Passives.cs
- Assets/Scripts/Presentation/UI/Gameplay/PanelHub/Inventory/PlayerRecyclingPanelUI.cs
- Assets/Scripts/Presentation/UI/Gameplay/PanelHub/Inventory/InventoryItemContextMenuUI.cs
- Assets/Scripts/Presentation/UI/Gameplay/PanelHub/Inventory/PlayerItemDetailPanelUI.cs
- Assets/Scripts/Presentation/UI/Gameplay/PanelHub/PlayerTooltipUI.cs
- Assets/Scripts/Authoring/Editor/CharacterStatusMakerInspector.cs
- Assets/Scripts/Authoring/Editor/CharacterStatusMakerWindow.cs
- Assets/Editor/ItemMakerWindow.cs
- Assets/Prefabs/UI/GamePlay/PanelHub/Inventory/PlayerInventoryPanel.prefab
- Assets/Prefabs/UI/GamePlay/PanelHub/Inventory/EnemyInventoryPanel.prefab

### 데이터·검사

- Assets/Resources/PassiveModuleRules.asset
- Assets/Resources/RuntimeContentCatalog.asset
- Assets/Resources/PassiveModules/Module_Experience.asset (+ .meta, 신규)
- Assets/Resources/PassiveModules/Module_*.asset: 기존 19개 분류, Suppression 표시명 갱신
- Assets/Resources/PassiveModules/PassiveParts_Normal.asset: 통합 모듈 표시명, GUID 유지
- Assets/ScriptableObject/GameScene/GameItem/PassiveItem/*.asset: 기존 11개 분류
- Assets/ScriptableObject/GameScene/Stage/RaidLoot/RaidLoot_Armory.asset
- Assets/ScriptableObject/GameScene/Stage/RaidLoot/RaidLoot_Medical.asset
- Assets/ScriptableObject/GameScene/Stage/RaidLoot/RaidLoot_Vault.asset
- Assets/Tests/EditMode/Editor/InfectionAggravationTests.cs
- Tools/GameSystems/VerifyRaidPassives.ps1 (신규)
- Tools/GameSystems/VerifyGameSystems.ps1
- Tools/GameSystems/VerifyInfectionErosion.ps1
- Tools/InventorySplit/author-passive-raid.cjs (신규, 직접 파일 쓰기 없이 apply_patch 입력만 생성)
- Tools/InventorySplit/verify-passive-raid.cjs (신규)
- Tools/InventorySplit/verify-layout.cjs
- 본 브리핑 문서
