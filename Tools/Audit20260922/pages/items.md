# 03. ItemSO·인벤토리·손 소켓·정렬

검사 기준: 2026-09-22 / E:/github/board_chase/board_chase. 현재 파일의 정적 연결·핵심 실행 경로 기준이며, 별도 표시 없는 항목은 이번 감사에서 플레이 테스트한 결과가 아닙니다.

## 데이터 계층

ItemSO는 GUID, 현지화 이름/설명, 아이콘, 희귀도, 가격, WorldVisualPrefab, 스택/보유 제한을 소유한다. 같은 아이템 종류의 정적 정의와 플레이어가 가진 한 스택의 현재 내구도·수량은 분리한다.

- WeaponSO → RangedWeaponSO 및 특화 총기 타입. 근접무기 정의는 WIP 보존.
- BasicBulletSO 파생: 탄 패밀리와 tier/관통/투사체 설정.
- UsableItemSO → ConsumableItemSO → MedicalItemSO: 의료는 소비품의 하위 타입.
- ThrowableUsableItemSO는 UsableItemSO 계열이다. Item Maker 화면에서는 Consumables 아래에 묶지만 C# 상속이 ConsumableItemSO인 것은 아니다.
- Armor/GasMask/Backpack: 각각 방탄 내구도, 필터 내구도, 수납/장착 설정.
- Key/KeyTool, Passive, Valuable, Currency. CurrencyItemSO는 Valuable 계열의 실제 물품이다.

## 인벤토리의 책임

PlayerInventoryRuntime은 주머니와 가방, 장비, 스택 이동·수량·소유 제한·내구 메타데이터를 관리한다. 무기 탄창/호환 탄약과 퀵슬롯은 관련 런타임이 조율한다. 정적 SO 자체의 durability를 플레이 중 깎는 설계가 아니라 개별 인벤토리 상태로 보관한다.

장비 교체는 슬롯 상태만 바꾸는 것으로 끝나지 않는다. ShooterEquipmentController, 탄약 로드아웃, 스탯, 손/몸 장착 표시, UI 변경 알림, 저장 스냅샷에 영향을 준다. 새 타입을 만들 때 이 소비자를 확인한다.

## 손에 드는 표현

일반 소비품은 전용 HeldView가 없을 때 WorldVisualPrefab을 손 소켓에 마운트하는 fallback이 있다. 보정용 부모 위치/회전/스케일과 원본 프리팹의 저장된 로컬 상태를 구별한다. 아이템마다 전용 손용 모델을 무조건 만들 필요는 없지만 크기·방향 보정은 여전히 필요하다.

Throwable은 이 일반 fallback 예외이며 전용 WeaponView/ThrowableProjectile 경로를 유지한다. 무기/투척체의 공격 로직을 단순 장식 프리팹에 자동으로 붙여 주는 기능이 아니다. 손에 든 표시용 인스턴스의 물리/충돌 동작도 월드 아이템과 구별한다.

## 정렬

- Compact: 빈칸을 뒤로 보내 앞쪽 슬롯부터 밀집 배치.
- Rarity: Legendary → Unique → Rare → Normal 순으로 정렬.
- 플레이어 주머니와 가방에 각각 적용한다. 적/창고 전부를 무조건 같이 정렬하는 글로벌 버튼이 아니다.
- 스택 병합 기능과 구별하며 스택 객체/내구 등 인스턴스 정보를 유지한다.
- 드래그·소비·거래 중 상태를 보호하는 가드가 있다.

버튼은 GamePlay UI의 인벤토리 헤더 아래에 있어야 한다. RUNScene 루트에 버튼을 계속 생성하는 방식은 현재 기준이 아니다. 관련 소유권 테스트와 마이그레이션 코드가 존재한다. 실제 화면 배치는 새 플레이 세션에서 다시 확인해야 한다.

## Item Maker 편집 흐름

카탈로그 선택 → 데이터 편집 → 이미지 미리보기 → 루팅 연결. Ctrl로 다중 선택, Shift 범위 선택, 선택 카드 드래그로 카테고리 이동 기능이 있다. 카테고리 이동은 폴더 문자열만 바꾸는 작업이 아니라 필요한 경우 SO 타입 전환/참조 검사/백업/복원까지 수행한다. 기존 GUID 보존과 타입별 전용 필드 손실 가능성을 함께 확인한다.

## 남은 점검

ItemSO 현지화/아이콘/월드 프리팹/손 보정/효과/루팅/상점/저장 식별자까지 채워야 한 아이템 완성이다. 런 카탈로그 및 초기 로드아웃의 누락 GUID가 있어 현재 자산 연결 정리가 먼저다.

## 코드·데이터 근거

- `E:/github/board_chase/board_chase/Assets/Scripts/Gameplay/Items/Definitions/ItemSO.cs`
- `E:/github/board_chase/board_chase/Assets/Scripts/Gameplay/Items/Definitions/MedicalItemSO.cs`
- `E:/github/board_chase/board_chase/Assets/Scripts/Gameplay/Items/Weapons/Definitions/ThrowableUsableItemSO.cs`
- `E:/github/board_chase/board_chase/Assets/Scripts/Gameplay/Items/Weapons/Runtime/UsableHeldVisualUtility.cs`
- `E:/github/board_chase/board_chase/Assets/Scripts/Gameplay/Entities/Shooter/Player/Inventory/PlayerInventoryRuntime.cs`
- `E:/github/board_chase/board_chase/Assets/Scripts/Gameplay/Entities/Shooter/Player/Inventory/PlayerInventoryRuntime.Sorting.cs`
- `E:/github/board_chase/board_chase/Assets/Editor/ItemMakerWindow.cs`
- `E:/github/board_chase/board_chase/Assets/Editor/ItemCategoryMoveUtility.cs`