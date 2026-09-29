# Shooter prefab hierarchy — 2026-09-23

## 편집 기준

```text
Visual_Shooter (공통 외형 원본, 이번 작업에서는 수정하지 않음)
├─ Player: 기존 중첩 Visual_Shooter 그대로, Player 파일 수정 없음
├─ EnemyBasePrefab: Visual/Visual_Shooter에 원본을 직접 중첩
│  ├─ Raider / Scraper / Agent / Butcher / Punisher
│  ├─ Stage1RevolverBoss … Stage5BossTemplate
│  ├─ Stage1BossGuard / Stage2BossGuard / Stage3BossGuard
│  └─ TempBossShooter / TempBossGuardShooter / WIP/Raider_Upgrade
└─ NpcBasePrefab: Visual_Shooter의 바리언트 + 공통 HomeQuestNpc
   ├─ HomeQuestNpc: Main 역할 / 기존 메인 퀘스트
   ├─ HomeItemNpc: Item 역할 / 기존 장비 퀘스트
   └─ HomeStatsNpc: Stats 역할 / 기존 스탯 퀘스트
```

EnemyBasePrefab과 Visual_Shooter의 관계는 **중첩 프리팹**이다. 적 16개는 EnemyBasePrefab의 **직접 바리언트**이다. NPC는 Visual_Shooter → NpcBasePrefab → 개별 NPC의 바리언트 계층이다.

## 실제 변경

- `Assets/Prefabs/GameScene/Shooter/EnemyShooter/EnemyShooter_Base.prefab`을 `EnemyBasePrefab.prefab`으로 이름 변경. GUID `a605ddf42f7592244b0708ea97b2fd1e` 유지.
- Raider에만 있던 공통 이펙트/상태 머신/장비 참조를 베이스로 승격. `RaiderEffectController`는 내용이 없는 EnemyShooterEffectController 상속 클래스였으므로 공통 `EnemyShooterEffectController`로 연결. 계층 이름은 `EffectController/EnemyEffects`.
- 베이스의 `Visual_Raider` 중간 상속을 제거하고 `Visual_Shooter`에 직접 연결. 현재 외형의 위치·크기·회전, 콜라이더 및 소켓 배치 유지.
- Scraper, Agent, Butcher, Punisher, WIP/Raider_Upgrade가 Raider를 상속하지 않도록 변경.
- 스테이지 보스·보스 가드도 TempBoss 부모를 거치지 않고 베이스를 직접 상속. 기존 TempBoss에서 받던 스탯 참조·크기·로드아웃 값을 각 바리언트에 보존.
- 베이스의 무기 루트는 `Root/Socket/RightHand_WeaponSocket`, 방탄 루트는 `Root/Socket/ArmorSocket`에 명시적으로 연결. 특히 기존 보스 템플릿에서 비어 있던 연결 보완.
- 사망 시 숨기는 대상은 `Visual/Visual_Shooter`만 연결. 형제 `Skull` 시체까지 포함하는 `Visual` 전체를 끄지 않는다.
- `HomeNPC/NpcBasePrefab.prefab` 추가. GUID `c43d3fd1591b4d11986b9daf0c79796c`.
- NPC 베이스에는 공통 대화 오디오·비전투 설정을 유지하고 제공 퀘스트는 비워 둠. 각 NPC의 역할, 표시 이름, 기존 제공 퀘스트 3개씩은 개별 바리언트가 소유.
- 기존 적·NPC의 GUID 및 로딩되는 객체/컴포넌트의 local file ID 유지. 참조를 끊는 재생성 방식이 아니다.
- 추가 참조 검사에서 이미 잘못된 컴포넌트 ID를 사용하던 5개 `_enemyShooterPrefab` 연결 복구: ButcherShooterConfig, Stage5BossTemplateConfig, Stage1/2/3BossGuardConfig. 스탯 값 자체는 수정하지 않음.

## 앞으로 어디서 편집할지

| 변경할 것 | 편집 대상 |
|---|---|
| 모든 캐릭터의 공통 몸체·소켓 구조 | Visual_Shooter (Player에도 영향을 줌) |
| 모든 적의 공통 컴포넌트·장비/이펙트 연결 | EnemyBasePrefab |
| 특정 적/보스의 외형 차이·전용 설정 | 해당 적/보스 바리언트 및 Config/Customize SO |
| 모든 Home NPC의 공통 구조·대화 기본값 | NpcBasePrefab |
| NPC별 역할·이름·제공 퀘스트 | 해당 NPC 바리언트 |

새 적은 Raider가 아니라 EnemyBasePrefab에서 **Create → Prefab Variant**로 만든다. 새 NPC는 NpcBasePrefab에서 같은 방식으로 만든다. 특정 바리언트에서 만든 예외를 공통 베이스에 Apply하면 다른 형제들에게 전파되므로 공통 수정인지 확인한다.

공통 베이스의 기본 스탯은 기존 베이스의 RaiderShooterConfig 참조를 유지한다. 이번 작업은 프리팹 상속 정리이며 새 밸런스/AI 설계를 도입하지 않는다. 각 적의 실제 스폰 Config/로드아웃과 보스의 기존 크기는 보존한다.

`Raider/Visual_Raider.prefab`과 TempBoss 프리팹은 호환용 에셋으로 남겼다. 현재 적 상속의 공통 부모로는 사용하지 않는다. Player, Visual_Shooter 원본, 씬 파일은 수정하지 않았다.

## 검증 및 범위

- Unity 6000.0.47f1의 격리된 복사 프로젝트에서 수정 전/후 전체 Shooter 프리팹 로딩 비교: 변경 후 26개 로딩 성공, Missing Script 0, 누락된 override target 0.
- 기존 객체/컴포넌트 local file ID 손실 0. NPC의 기존 역할·대화·제공 퀘스트 일치. 기존 개별 적의 스탯, 크기, VFX 연결 비교.
- 프로젝트의 관련 직렬화 참조 511건 검사: 이번 상속 변경으로 새로 끊긴 참조 0. 위에 적은 스폰 Config 5곳은 기존 오류를 복구.
- 회귀 테스트: `Assets/Tests/EditMode/Editor/ShooterPrefabHierarchyTests.cs` (상속, 공통 소켓/VFX/시체, NPC 역할/에셋 ID, 스폰 참조, Missing Script). 격리 Unity에서 **9개 통과 / 실패 0**.
- 열린 실제 Unity는 Hot Reload가 신규 `.meta`보다 프리팹을 먼저 읽으며 일시적으로 Missing Parent를 기록했음. 사용자의 Assets → Refresh 이후 올바른 GUID로 두 베이스·NPC·HomeSector의 정상 재임포트를 확인.
- 전체 레이드 플레이/전투 밸런스 테스트는 별도. 기존 Player/옛 Preview/City의 오래된 참조는 이 작업에서 수정하지 않음.
- 변경 전 프리팹 백업: `Temp/ShooterPrefabRebaseBackup-20260923/`. Temp이므로 영구 보관소는 아니며, 원복할 경우 새 베이스만 단독 삭제하지 말고 관련 바리언트를 함께 복구해야 한다.
