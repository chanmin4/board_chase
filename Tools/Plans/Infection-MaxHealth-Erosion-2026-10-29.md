# 감염 시스템: 최대 체력 잠식 전환 (2026-10-29)

## 범위

이번 변경은 감염 누적, 최대 HP 잠식, 치료, 저장, 관련 UI/편집 도구 연결만 포함한다.
감염방 시작 수·확산 대상·확산 타이머 알고리즘, 스테이지 분기, 패시브 개편, 무기 추가, 귀중품/거래/퀘스트는 변경하지 않았다. 노션도 수정하지 않았다.

## 확정한 체력 규칙

- 사용자의 최종 답변대로 `감염 >= 현재 HP` 사망 조건을 제거했다.
- 감염 수치는 별도 처형 임계값이 아니라 최대 HP에서 빼는 잠식량이다.
- 기본 최대 HP 100, 잠식 20이면 유효 최대 HP는 80이다. 현재 HP가 100이었다면 80으로 제한된다.
- 현재 HP가 이미 30이라면 잠식 20이 생겨도 30/80을 유지한다. 현재 HP에서 20을 다시 빼지 않는다.
- 잠식 55, 현재 HP 30, 유효 최대 HP 45도 생존한다.
- 유효 최대 HP가 0이면 기존 사망 흐름으로 사망한다. 최대 HP 0에서 Revive로 HP 1을 만드는 우회도 막았다.
- 장비/패시브 등으로 기본 최대 HP를 재계산해도 잠식량은 한 번만 차감한다. 일반 회복은 감소한 최대 HP를 넘지 못한다.

## 감염악화 상태이상

- 원형 게이지를 채우면 즉시 1회 최대 HP 잠식이 발생한다. 별도 5초 감염/현재 HP 피해 틱은 제거했다.
- 표시 스택 상한에 도달해도 원형은 계속 돌고 새 완주는 잠식을 계속 발생시킨다.
- 새 감염 유입이 5초간 없으면 미완성 원형 게이지만 천천히 감소한다. 기존 잠식은 자연 회복되지 않는다.
- 가스, 출혈, 피격, 기존 조사/획득 노출 경로를 재사용한다. 감염방 피격에는 실제 피해에 비례한 추가 누적을 연결했다.
- 구형 감염 주입 DOT는 플레이어에게 현재 HP 피해 대신 게이지 노출로 전달한다. 비플레이어 주입 DOT 동작은 유지했다.
- 출혈 등 상태이상은 상호작용/의료 사용을 취소하지 않는다. 직접 무기 피격의 취소 규칙은 유지했다.

## 임시 수치와 설정 위치

- Game System Tool → 상태이상 → InfectionAggravation
  - 원형 용량 100
  - 원형 1회당 최대 HP 잠식 5
  - 미완성 게이지 자연감소 0.25/초, 새 유입 후 대기 5초
  - 일반 피격 기본 노출 5 + 감염방에서 실제 피격 피해의 10% 추가
  - 기존 가스/루팅/아이템 획득/출혈 노출 수치는 유지
- Item Maker → Medical / Infection Ampoule or Syringe
  - 기존 앰플: 15초간 감염 유입 ×0.5, 회복 대기가 지난 미완성 게이지 회복 +3/초
  - 앰플은 예방 사용 가능하며 중첩/지속시간 갱신은 불가. 잠식된 최대 HP는 복구하지 않음
  - 새 주사기: 사용 완료 시 잠식 20 복구. 현재 HP는 별도 치료. Unique, 최대 스택 5, 사용 시간 3초
  - 새 앰플/주사기는 1회 사용에 1개. 구형 억제제의 스테이지별 복수 투약 규칙을 적용하지 않음
  - 주사기는 RuntimeContentCatalog와 RaidLoot_Medical, LootTable_MedicalBox_Small에 등록
  - HOME/레이드 상점 상품 추첨에서 주사기를 제외

수치는 최종 밸런스가 아니다. 앰플 회복은 새 유입이 계속되는 동안에는 시작하지 않으며, 방호 배율은 모든 감염 게이지 유입에 적용한다. 가스마스크는 기존처럼 가스 유입에만 작용한다.

## HOME 정화장치

`HomeInfectionPurifier` 컴포넌트를 추가하고 E 상호작용의 탐색·실행·문구 경로에 연결했다.
HOME에서만 동작하며, 자신의 거리 검사도 수행한다. 잠식과 원형/스택을 제거하되 일반 부상 HP를 자동으로 채우지 않는다.

이번에는 HOME 씬/프리팹에 장치 모델을 임의로 배치하지 않았다. 실제 사용할 기배치 장치에 이 컴포넌트와 상호작용 탐색에 잡히는 Collider/레이어를 설정해야 한다. 기본 자체 허용 거리는 3이며 Inspector에서 조정 가능하다.

## 저장과 UI

- 기존 `currentInfection`, `infectionExposure`, 약효 저장 필드를 유지했다. 구형 `nextTick`은 저장 호환용으로만 남기고 피해 계산에는 사용하지 않는다.
- 불러오기 때 잠식을 먼저 적용한 뒤 현재 HP를 복구한다. 예전 최대 HP 때문에 저장된 부상 HP가 잘리는 일을 방지한다.
- 앰플 유입 배율도 저장/검증하며 해치를 통한 다음 스테이지 이동 시 남은 시간·회복량과 함께 전달한다.
- 예전 저장의 누적 감염 수치는 같은 수치의 최대 HP 잠식으로 읽는다. 이전 스택에 피해를 소급 재적용하지 않는다.
- 플레이어 HUD는 현재/유효 최대 HP와 최대 HP 잠식, 게이지 노출 속도를 구분한다. 원형은 표시 스택 상한에서도 정상적으로 순환한다.
- 적 감염 오버레이도 기본 최대 HP 기준으로 정규화했다. 예전 현재 HP 임계값 진단 문구를 제거했다.
- 옛 ‘억제 유지/만료’ 시계 대신 앰플 남은 시간·감염 노출 상태를 표시한다.
- Game System Tool 상태이상 편집, Item Maker 약품 생성/편집, Stage Maker 안내, Debug Monitor 설명을 새 규칙으로 맞췄다.

## 검증

- 실제 Unity 응답 파일/참조 기반 격리 컴파일: Assembly-CSharp 오류 0 / 기존 경고 38, Editor 오류 0 / 경고 0
- VerifyInfectionErosion.ps1: 실제 체력 setter·감염 수치·사망 판정·약품 메서드를 관리형 수명주기 shim으로 실행, 10,023개 검사 통과
- VerifyGameSystems.ps1: 감염 누적/앰플/회복 지연 등 포함 3,909개 검사 통과
- VerifyInteractionDamage.ps1: 상태이상 비취소·직접 피격 취소·플레이어 감염 주입 게이지 전환 등 19개 검사 통과
- VerifyStaminaInput.ps1: 기존 스태미나 88개 검사 및 13개 설정의 공통값 연결 유지
- verify-assets.cjs: 기존 에셋/소스 계약 214개 검사 통과
- 새 주사기의 카탈로그·의료 루팅 참조, 상점 미등록, 잠식 우선 복구 순서 검사 통과
- 일반 dotnet build --no-restore는 기존 Temp/obj/Assembly-CSharp/project.assets.json 부재(NETSDK1004)로 실행 불가. 위 Unity 참조 컴파일과 별개이며 일반 빌드 성공으로 표기하지 않는다.

Unity 실제 Play Mode·네이티브 ES3 재접속·UI 육안 검증은 수행하지 않았다. 특히 HOME 장치 실제 배치, 약효 만료/필터 소진 경계 프레임, 실제 저장 후 재입장, 일시정지, 가스방 왕복은 인게임 확인이 남아 있다.

## 이번 변경 파일

모든 경로는 프로젝트 루트 기준이다. 기존의 다른 미커밋 변경은 보존했다.

### 런타임

- Assets/Scripts/Gameplay/Entities/Common/Core/Damageable.cs
- Assets/Scripts/Gameplay/Entities/Shooter/Common/Core/ShooterInfection.cs
- Assets/Scripts/Gameplay/Entities/Common/StatusEffects/InfectionAggravationState.cs
- Assets/Scripts/Gameplay/Entities/Common/StatusEffects/InfectionAggravationStatusSO.cs
- Assets/Scripts/Gameplay/Entities/Common/StatusEffects/EntityStatusEffectRuntime.InfectionAggravation.cs
- Assets/Scripts/Gameplay/Entities/Shooter/Player/PlayerShooterInfection.cs
- Assets/Scripts/Gameplay/Entities/Shooter/Player/PlayerShooterInfection.Exposure.cs
- Assets/Scripts/Gameplay/Entities/Shooter/Player/Inventory/PlayerConsumableUseRuntime.cs
- Assets/Scripts/Gameplay/Items/Definitions/InfectionCureItemSO.cs
- Assets/Scripts/Gameplay/Interaction/HomeInfectionPurifier.cs (+ .meta, 신규)
- Assets/Scripts/Gameplay/Interaction/Core/InteractionManager.cs
- Assets/Scripts/Gameplay/World/Sectors/Shop/ShopConfigSO.cs
- Assets/Scripts/Gameplay/World/Sectors/Shop/ShopRoomDropTableSO.cs
- Assets/Scripts/Core/Persistence/RaidRunSaveData.cs
- Assets/Scripts/Core/Persistence/PlayerRunStatePersistence.cs
- Assets/Scripts/Gameplay/Progression/Stage/StageProgressionManager.HatchProgression.cs

### UI·편집 도구

- Assets/Scripts/Core/Events/ScriptableObject/PlayerHealthEventChannelSO.cs
- Assets/Scripts/Presentation/UI/Gameplay/PlayerInfo/PlayerInfoHUD.cs
- Assets/Scripts/Presentation/UI/Gameplay/InfectionAggravationGaugeUI.cs
- Assets/Scripts/Presentation/UI/Gameplay/EnemyBarUI/HealthBar/EnemyHealthBarWidget.cs
- Assets/Scripts/Presentation/UI/Gameplay/PlayerTimer/PlayerTimerHUD.cs
- Assets/Scripts/Presentation/UI/Gameplay/PanelHub/Inventory/PlayerItemDetailPanelUI.cs
- Assets/Scripts/Authoring/Editor/CharacterStatusMakerInspector.cs
- Assets/Scripts/Authoring/Editor/DebugMonitorWindow.StatusEffects.cs
- Assets/Scripts/Authoring/Editor/StageMakerWindow.cs
- Assets/Editor/ItemMakerWindow.cs

### 에셋·검사

- Assets/Resources/InfectionAggravation.asset
- Assets/Resources/RuntimeContentCatalog.asset
- Assets/ScriptableObject/GameScene/GameItem/Consumable/Medical/InfectionCure.asset
- Assets/ScriptableObject/GameScene/GameItem/Consumable/Medical/InfectionSyringe.asset (+ .meta, 신규)
- Assets/ScriptableObject/GameScene/Stage/RaidLoot/RaidLoot_Medical.asset
- Assets/ScriptableObject/GameScene/GameItem/Loot/LootTable/LootTable_MedicalBox_Small.asset
- Assets/Tests/EditMode/Editor/InfectionAggravationTests.cs
- Tools/GameSystems/VerifyGameSystems.ps1
- Tools/GameSystems/VerifyInteractionDamage.ps1
- Tools/GameSystems/VerifyInfectionErosion.ps1 (신규)
- 본 브리핑 문서
