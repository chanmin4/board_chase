# 데모 경로 · 감염 · 패시브 교환 패치

## 데모 경로

- `StageRunFlowSO`, `StageProgressionManager.HatchProgression`, `RaidRuntime`, `HideoutStageSelectPanel`: 1 → 2/3, 2/3 → 4. 두 후보가 해금됐으면 해치에서 선택, 한 후보만 해금됐으면 바로 그 경로. 전력·해치 방 클리어·스테이지 접근 조건을 유지한다.
- `RunMain_01/02/03_Stage*_Boss.asset`: 1층 임무는 2/3을 열고, 2층 또는 3층 임무는 4를 연다. 기존 보상 영수증의 인덱스는 바꾸지 않았다. 4 이후 분기는 포함하지 않았다.

## 교환과 스택

- `HomeShopConfig.asset`: 기존 패시브 6종의 귀중품 교환과 1층 보스 임무의 기존 상점 해금에 관통 교환 추가. 관통 교환 임시 비용: 화약 2, 케이블 1.
- `LootCategorySelection`, `SectorWorldItemSpawnTableSO`: 신규 필드 추첨에서 패시브 제외. 이전 저장에서 이미 소유한 물건은 삭제하지 않는다. 기존 시작 추첨은 `PassiveModuleRules.asset`에서 비활성화.
- `PlayerInventoryRuntime.PassiveRaid`, `PlayerPassiveInventoryRuntime`: 생환 시 장착 개체와 수치 보존, 해제 시 삭제/비활성화하지 않음. 사망은 기존 소지품 손실 처리 사용.
- `ItemSO`와 `GameItem` 에셋: 탄약을 제외한 새 스택은 1. 기존 저장 슬롯 수량은 복원 시 잘라내지 않는다. 현금도 적용되므로 기존 고액 현금 거래의 지불 칸 수는 별도 조정이 필요할 수 있다.

## 감염과 무기

- `InfectionAggravation`, `Damageable`, `PlayerShooterInfection`, `GasMaskItemSO`, `Player_Info.prefab`: 실제 HP 손실의 50%를 누적. 가스·억제 시간 만료 초당 1 추가. 자연 감소 없음. 일반 누적과 용량 감소 모두 한 번 가득 찰 때마다 감염 전 최대 HP 10% 잠식, 초과분 유지. 마스크 용량 보너스 임시값은 방호 레벨당 25. 주사기는 기존 최대 HP 잠식 회복. 가로 감염 바와 숫자로 표시.
- `BulletSO`, `BasicBulletSO`, `BasicBullet`, `ThrowableUsableItemSO`, `ThrowableProjectile`: AR 전용 감염탄 5희귀도 및 충격 즉발 감염 수류탄. 감염탄 +10/15/20/25/30, 수류탄 +50은 임시값. `RuntimeContentCatalog.asset`과 `RaidLoot_Armory.asset`에 등록. 화약은 기존 군용물품 분류에 추가.
- `PlayerStatsConfigSO`, `PlayerStatsRuntime`, `RangedWeaponSO`와 무기 에셋: 플레이어 기본 사격 피해 10에 탄 기본 피해를 합산, 탄·무기 보정 후 패시브 공격 보정을 마지막에 적용. 헤드샷은 실제 명중 시 무기 배율 적용. 스나이퍼 +0.2, 권총/샷건/SMG -0.2 관통. 최종 관통 값은 음수 불가. 기존 총기별 발사 속도와 피해 배율은 유지.

## 검증 범위

- `DemoRouteAndLootRulesTests`에 분기·감염 초과분·스택·AR 호환·패시브 교환 Edit Mode 검사 추가.
- Unity 참조로 런타임 및 에디터 컴파일, 새 에셋 카탈로그/루팅 연결과 상점 재료·퀘스트 참조 정적 검증, 기존 감염 체력 상한 관리형 검사.
- Play Mode 및 실제 저장 이어하기·해치 UI·교환·투척 체감 검증은 별도. 섹터의 지형/엄폐물 배치는 수정하지 않았다.
