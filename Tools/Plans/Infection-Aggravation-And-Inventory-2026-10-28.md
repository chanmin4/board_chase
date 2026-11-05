# 감염악화 상태이상 편입 · 장비 UI 배치 수정 (2026-10-28)

## 인벤토리 문제의 원인과 수정

EquipmentSection과 기존 가스마스크 슬롯은 삭제되지 않았다. PlayerInventoryUI.EnsureInventorySlots가 RefreshInventorySectionLayout()을 인자 없이 호출할 때 equipmentTopInset 기본값 0과 sideInset 0을 사용했다. 이 때문에 포켓·인벤토리가 패시브·장비 영역 위로 이동하여 장비 줄을 가렸다.

- 인자 없는 갱신은 현재 EquipmentSection의 위치와 공통 좌우 여백을 사용한다.
- 기존 7칸(방탄·가스마스크·무기 2·퀵슬롯 3)과 연결을 유지했다. 새로운 런타임 장비 슬롯은 만들지 않았다.
- 원본 PlayerInventoryPanel.prefab의 포켓·인벤토리 기본 위치/높이를 맞췄다.
- Canvas-GamePlay.prefab에 중복 저장된 포켓·인벤토리 위치 덮어쓰기 4개를 제거했다. 캔버스의 다른 사용자 설정은 유지했다.
- 인벤토리 스크롤 표시 높이를 남은 패널 공간에 맞춰 제한했다.

## 감염악화

InfectionExposureRulesSO를 InfectionAggravationStatusSO로 변경하고 상태이상 폴더로 이동했다. 기존 스크립트/에셋 GUID는 유지했다. 원본 에셋은 Assets/Resources/InfectionAggravation.asset이다.

Game System Tool → 상태이상 → InfectionAggravation에서 아이콘·누적량·최대 스택·자연감소·주기 피해·감염 요인을 조절한다. 별도의 감염 메뉴는 표시하지 않는다. 비활성 구형 InfectionControlRules와 사용하지 않는 PlayerInfectionRules는 이 툴 목록에서 제외했다. 기존 PaintInfectionRecoveryDown 효과는 다른 회복 저하 효과이므로 변경하지 않았다.

상태의 실제 계산과 저장 데이터 접근은 EntityStatusEffectRuntime.InfectionAggravation.cs가 담당한다. PlayerShooterInfection.Exposure.cs는 기존 입력 API를 상태이상 런타임으로 전달하는 어댑터만 남겼다. 기존 RunData.infectionExposure 저장 키와 InfectionExposureSaveData 타입 이름을 보존했다. ActiveStatusEffect가 동일 저장 객체를 참조하며 스택을 이중 저장하지 않는다.

- 원형 100 누적당 +1스택, 최대 20스택.
- 0스택에서는 주기 피해 없음. 1스택부터 5초마다 스택당 감염 수치 +1.
- HP를 직접 깎지 않는다. 기존 감염 수치가 현재 HP에 도달하는 사망 규칙을 사용한다.
- 자연감소는 초당 0.25이며 스택까지 거슬러 감소한다. 완충 직후 스택이 즉시 사라지는 것을 막도록 마지막 노출 후 5초 대기한다. 대기 시간도 설정 가능하다.
- 치료는 악화 누적량을 줄인다. 이미 받은 감염 수치 회복과는 별도이며 기존 치료품의 두 효과 연결은 유지했다.
- 일반 출혈 틱과 중복 처리하지 않고, 상태이상 피해가 상호작용을 취소하지 않는 기존 예외를 유지했다.
- 상태이상 스트립의 원형 UI가 해당 캐릭터의 런타임에 연결된다. 전역 플레이어 검색으로 별도 아이콘을 붙이던 방식은 제거했다.
- Debug Monitor에서 스택 적용·제거·게이지·감염 피해 종류를 확인할 수 있다.

## 가스 방호 장비

기존 GasMaskItemSO, EquipmentInventoryGasMaskSlotUI, 필터 저장/내구도/교체 경로를 재사용했다. 방호는 들어오는 가스 압력을 감소시키며, 이미 쌓인 악화 상태나 원형 용량을 변경하지 않는다. 필터가 소진되면 해당 프레임의 남은 노출 시간부터 무방호로 계산한다.

HOME 상점의 기본 가스마스크를 기본 구매 가능으로 변경했고, Lv.1 사용을 명시했다. 상위 마스크의 기존 해금은 유지했다.

감염방호/가스저항은 패시브 허용 스탯에서 제외했다. 적의 고정 방호 패시브 자동 배정을 제거했다. 기존 방호 패시브는 GUID와 카탈로그 참조를 보존한 채 WIP/Legacy/PassiveGasProtection으로 옮겼다. 이전 소지품의 저장을 삭제하지 않으며 방호 효과는 더 이상 적용하지 않는다.

## 감염방 확산과 타이머

NamedSectorController의 기존 카운트다운을 RuntimeCountdown으로 추출해 재사용했다. 기존 네임드 예약·재등장 로직은 다시 활성화하지 않았다.

Stage Maker → 전투에서 초기 감염방 수, 확산 사용, 공용 간격, 회당 새 감염방 상한, 가스 강도를 설정한다.

임시 테스트값:

- Stage 1~6 초기 감염방: 각각 1/2/3/4/5/6개. 적합한 방이 부족하면 가능한 만큼 배치.
- 확산 간격 60초, 가스 강도 3.
- 회당 전체 상한 0: 기존 감염방마다 1회 시도. 이 값은 '확산 없음'이 아니라 별도 전체 상한 없음이다.

기존 감염방은 실제 양방향 포탈로 연결된 상하좌우 미감염 방 중 하나를 추첨한다. 전체 결과를 동시에 적용하고 타이머를 한 번 초기화한다. 새 감염방은 다음 회차부터 참여하며 중복 목표는 한 번만 감염된다. 초기 감염방 선별의 기존 시작방/해치/발전실 제외는 유지했지만 이후 확산으로는 연결된 방에 도달할 수 있다.

남은 시간과 확산 회차, 각 방의 virusRoom은 레이드 저장에 포함된다. 방 이동/로드는 타이머를 초기화하지 않고, 일시정지·로딩·오프라인에서는 진행하지 않는다. 긴 프레임에 여러 차례 연쇄 확산하지 않는다. 기존 적/웨이브/HFSM은 재생성하거나 초기화하지 않는다.

## 주요 변경 파일

- Assets/Scripts/Gameplay/Entities/Common/StatusEffects/StatusEffectDefinitionSO.cs
- Assets/Scripts/Gameplay/Entities/Common/StatusEffects/EntityStatusEffectRuntime.cs
- Assets/Scripts/Gameplay/Entities/Common/StatusEffects/EntityStatusEffectRuntime.InfectionAggravation.cs
- Assets/Scripts/Gameplay/Entities/Common/StatusEffects/InfectionAggravationStatusSO.cs
- Assets/Scripts/Gameplay/Entities/Common/StatusEffects/InfectionAggravationState.cs
- Assets/Scripts/Gameplay/Entities/Shooter/Common/Core/ShooterInfection.cs / StatModifierTypes.cs
- Assets/Scripts/Gameplay/Entities/Shooter/Player/PlayerShooterInfection.cs / PlayerShooterInfection.Exposure.cs
- Assets/Scripts/Gameplay/Entities/Shooter/Player/Inventory/PlayerInventoryRuntime.GasMask.cs
- 기존 노출 원본을 참조하던 WorldItemPickup, Damageable, EnemyLootInventoryRuntime, SectorEnemySpawner
- Assets/Scripts/Gameplay/World/Sectors/Runtime/RuntimeCountdown.cs
- Assets/Scripts/Gameplay/World/Sectors/Named_Boss/NamedSectorController.cs
- Assets/Scripts/Gameplay/World/Sectors/StageMap/StageGasSpread.cs / RaidRuntime.cs / StageSectorInstantiator.cs
- Assets/Scripts/Gameplay/World/Sectors/Traits/SectorRoomTraitRuntime.cs
- Assets/Scripts/Gameplay/World/Sectors/ScriptableObject/StageEncounterRules.cs
- Assets/Scripts/Core/Persistence/RaidRunSaveData.cs
- Assets/Scripts/Presentation/UI/Gameplay/InfectionAggravationGaugeUI.cs / StatusEffectIconStripUI.cs
- Assets/Scripts/Presentation/UI/Gameplay/PanelHub/PlayerStatsStatusDetailPanelUI.cs
- Assets/Scripts/Presentation/UI/Gameplay/PanelHub/Inventory/PlayerInventoryUI.cs
- Assets/Prefabs/UI/GamePlay/PanelHub/Inventory/PlayerInventoryPanel.prefab
- Assets/Prefabs/UI/GamePlay/Canvas-GamePlay.prefab
- CharacterStatusMakerWindow / CharacterStatusMakerInspector / SystemDomainInspector / DebugMonitorWindow.StatusEffects
- PassiveModuleRulesSO / EnemyShooterStatsRuntime.Passives
- Resources/InfectionAggravation / PassiveModuleRules / RuntimeContentCatalog
- Stage1Definition~Stage6Definition / HomeShopConfig / GasMaskLV1
- Assets/Tests/EditMode/Editor/InfectionAggravationTests.cs 및 기존 패시브 테스트의 폐기 참조
- Tools/GameSystems/VerifyGameSystems.ps1 / verify-inventory-layout.cjs

## 검증 경계

- Unity 참조 분리 컴파일: 런타임·에디터 오류 0개. 기존 런타임 경고 38개.
- 관리형 실제 코드 검사: 3,907개 통과. 상태 누적/스택/치료/틱, 200개 시드 확산, 연쇄 확산 방지, 타이머 재초기화 방지 포함.
- 패시브 등 기존 모듈 4,257개, 에셋 계약 214개, 구형 정리 35개, 스태미나 73개, 상태 피해 상호작용 예외 17개 통과.
- 프리팹/장비 참조/영역 배치 정적 검사 25개 통과. 포켓 2/4/6/8칸 배치 포함.
- 기존 맵 생성/외곽탈출 보호/상인 핵심 검사 통과.
- dotnet build Assembly-CSharp.csproj --no-restore는 기존 NETSDK1004(Temp/obj의 project.assets.json 없음)로 실행 불가. 위 Unity 참조 컴파일과 구분한다.
- 새 Unity EditMode 테스트 소스는 컴파일했지만 네이티브 실행은 하지 않았다. 실제 Play Mode의 픽셀 배치·드래그·필터 소진·가스 확산·저장/복원·HFSM 실동작은 별도 확인이 필요하다.
- Notion, 상세 퀘스트라인, 사용자 저장 파일은 직접 수정하지 않았다.
