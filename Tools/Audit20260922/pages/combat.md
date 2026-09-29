# 02. 플레이어·무기·피해·상태이상 | 현재 구현

검사 기준: 2026-09-22 / E:/github/board_chase/board_chase. 현재 파일의 정적 연결·핵심 실행 경로 기준이며, 별도 표시 없는 항목은 이번 감사에서 플레이 테스트한 결과가 아닙니다.

## 공격과 데이터 경계

Player 입력/공격 상태 → Shooter 장착 및 탄약 확인 → EntityShootCore/무기별 발사 → 투사체 충돌 → Damageable 피해 → 상태·사망·퀘스트/루팅 알림 흐름이다. 무기 모델, 발사 데이터, 탄종 데이터, 플레이어 인벤토리를 한 컴포넌트에 합치지 않는다.

RangedWeaponType은 Pistol/Revolver/SMG/AssaultRifle/LMG/SniperRifle/Shotgun/RocketLauncher 계열이다. 탄약 패밀리는 Universal/Light/Medium/Heavy/Sniper/ShotgunShell/Rocket이며 tier 1~8과 관통 수치를 따로 가진다. 에디터 카테고리, 무기 클래스, 탄 패밀리, 관통력은 서로 다른 축이다.

## 일반 피해 계산

CanReceiveDamage·회피 조건을 통과한 원래 피해에 다음 순서로 계산한다.

1. 실드 방탄층: 흡수와 별도 내구도 소모.
2. 장착 방탄복: 그 뒤 남은 피해에 방탄 판정.
3. 엔티티 기본 방탄: 다시 남은 피해에 판정.
4. 최종 피해 배율 및 상태이상 피해 배율.
5. HP 차감과 감염 임계 재판정/사망 처리.

각 층의 등급은 더하지 않는다. 방탄 계산 최소 배율은 차이 d=max(0,방탄등급-관통등급)에 대해 0.10+0.90×exp(-0.55×d)이고 그 최소~1 범위에서 해당 층의 피해 배율을 한 번 뽑는 구조다. 관통이 방탄 이상이면 그 층은 전량 통과한다. 방탄 마모는 해당 층이 막아낸 양을 기준으로 계산하는 경로다.

실드는 전방위이며 방향 방패/합산 방탄이 아니다. 계산 코드는 준비되어 있지만 현재 자산 연결에서 실더 개체는 확인되지 않았다.

## 비상 근접

PlayerEmergencyMeleeProfile.IsAvailable은 무기가 없거나, RangedWeaponSO를 들고 발사 가능한 탄약이 없는 경우에 허용한다. 단순히 현재 탄창이 비었다는 것과 재장전 가능한 탄약까지 없는 상태는 구별한다.

기존 근접 판정 모양·범위·각도·타이밍·쿨다운·연출을 재사용하고 공격 수락 시 PlayerStats의 MeleeDamage를 스냅샷한다. 런타임 fallback 피해는 5지만 실제 값은 플레이어 스탯/설정으로 확인해야 한다.

ReceiveFixedMeleeAttack은 실드·방탄과 일반 피해 배율을 우회하는 고정 HP 피해 경로다. 방탄 내구도도 소모하지 않는다. 다만 사망/무적 등 CanReceiveDamage와 회피 조건까지 무조건 무시하는 것은 아니다. 일반 적/기존 근접무기의 공격 계산을 전부 이 고정 피해로 바꾼 것이 아니다.

## 소모품과 상태이상

의료 아이템은 회복량·감염 회복·사용 시간·상태 효과·부상 치료를 정의한다. 사용 중 이동 허용과 공격/대시/상호작용/피격/취소에 따른 중단 경로가 있다. 사용 성공 시 효과와 소비를 처리하며 중복 완료를 막아야 한다.

EntityStatusEffectRuntime은 시간 제한 효과, 중첩/갱신 정책, 스탯 배율과 DOT 등 공통 상태를 관리한다. 기본 스탯, 장비, 패시브, 퀘스트 영구 보상, 일시 효과를 구별한다. 옛 메타 구매 레벨을 현재 성장원으로 설명하지 않는다.

## 감염과 생존

감염이 현재 HP 이상이면 사망한다. PlayerShooterInfection의 자연회복은 꺼져 있다. 방 가스는 방독면 필터 처리 후 감염으로 들어가며, HP 직접 피해와 같은 수치로 설명하면 안 된다.

## 아직 검증할 것

실드/방탄복 파손 경계, 과관통, 고정 근접과 회피, 발사 가능 탄 판정, 재장전/교체/취소 중복 입력, 상태이상 저장·복원, 총기 내구와 반동/명중/연사 밸런스는 플레이 검증 항목이다. 이번 감사의 컴파일 통과는 이를 대신하지 않는다.

## 코드·데이터 근거

- `E:/github/board_chase/board_chase/Assets/Scripts/Gameplay/Entities/Common/Core/Damageable.cs`
- `E:/github/board_chase/board_chase/Assets/Scripts/Gameplay/Entities/Common/Equipment/ShieldArmorRuntime.cs`
- `E:/github/board_chase/board_chase/Assets/Scripts/Gameplay/Entities/Shooter/Player/PlayerEmergencyMeleeProfile.cs`
- `E:/github/board_chase/board_chase/Assets/Scripts/Gameplay/Entities/Shooter/Common/Core/ShooterMeleeAttack.cs`
- `E:/github/board_chase/board_chase/Assets/Scripts/Gameplay/Entities/Shooter/Player/PlayerShooterShoot.cs`
- `E:/github/board_chase/board_chase/Assets/Scripts/Gameplay/Entities/Shooter/Player/PlayerBulletLoadoutRuntime.cs`
- `E:/github/board_chase/board_chase/Assets/Scripts/Gameplay/Entities/Common/StatusEffects/EntityStatusEffectRuntime.cs`
- `E:/github/board_chase/board_chase/Assets/Scripts/Gameplay/Entities/Shooter/Player/Inventory/PlayerConsumableUseRuntime.cs`