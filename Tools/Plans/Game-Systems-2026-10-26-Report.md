# 게임 시스템 연결 패치 — 2026-10-26

노션 ‘콘텐츠 수정·추가 계획’의 추가사항과 이번 대화의 변경 지시를 기준으로 시스템 토대를 연결했다. 노션, 상세 퀘스트 라인, 기존 저장 파일, Git 상태 초기화는 건드리지 않았다. 기존 더티 체크아웃의 변경을 유지했다.

## 이번에 확정 반영한 규칙

- 독립 경험치 툴을 만들지 않았다. 기존 Character & Status Maker를 **Tools → Authoring → Game System Tool**로 확장했다.
- 경험치·사용 레벨·캐릭터·공통 이동/AI·상태이상·감염·분해기 수치를 같은 툴에서 다룬다. Item/Stage/Sector/Quest Maker의 원본 SO는 그대로 사용한다.
- 기존 MetaUpgrade 코드의 영구 스탯 보상·프로필 저장 구조를 재사용했다. 기존 코드는 비용을 지불하는 업그레이드 레벨이며 실제 경험치 코드가 아니었다. WIP의 구매 기능을 임의로 되살리지 않고, 경험치 누적/정산 경로를 새로 연결했다.
- HOME: 패시브 장착·해제·교체·수치 리롤 가능. 레이드: 해당 변경 불가. 귀속은 해제해도 유지하며 타인이 획득할 수 없다.
- 우클릭 즉시 분해를 제거하고 시간제 분해기만 사용한다. 우클릭 수치 리롤은 HOME에서 유지한다.
- 일반 월드 상호작용은 이동·공격·대시·피격 시 취소한다. T 기능처치는 별도 기존 경로로 남겨 이동 가능한 5초 처치를 유지했다.

## 1. 경험치·레벨

실제 처치, 새 섹터 진입, 상자 최초 확인, 월드 아이템 획득, NPC 퀘스트 완료에서 경험치를 연결했다. 상자 재열기와 플레이어가 버린 아이템 재획득은 발견 경험치를 반복 지급하지 않는다. 웨이브 적은 이름이나 변하는 sibling index 대신 스테이지·시드·섹터·생성 순번으로 구분한다.

레이드 경험치는 원정 데이터에 대기한다. 성공적으로 HOME에 탈출했을 때 100%, 사망 처리 때 50%를 내림 정산한다. 해치로 다음 층에 이동할 때는 원정 ID와 경험치를 유지한다. 프로필의 정산 ID 이력과 퀘스트 ID 이력으로 재지급을 방지한다.

명시된 장비 사용 레벨, Lv.7/Lv.10 체력 +10을 설정했다. 레벨은 사용·장착 제한이며 획득·보관·스테이지 입장을 자동으로 막지 않는다. 약품 등 개별 아이템은 Item Maker의 ‘사용 레벨 개별 지정’으로 덮어쓸 수 있다.

최대 레벨 30, 경험치 곡선/처치량/발견량 등은 테스트용 기본값이다. 노션의 빈 레벨과 미정 무기/등급은 임의 확정하지 않아 제한 1(없음)로 남겼다. 기존 메타업그레이드는 WIP 상태를 유지하고 사용 레벨 조건 데이터/스냅샷만 추가했다.

인벤토리 분해기 창과 Debug Monitor에서 현재 레벨·누적/대기 경험치를 확인한다.

## 2. 시간제 분해기

인벤토리의 ‘분해기’ 버튼 → 아이템 드래그 → 슬롯별 독립 타이머 → 완료 후 클릭 수령이다. 진행 중 클릭하면 원본과 내구도를 회수하며, 재등록하면 처음부터다.

- 기본 2칸. 퀘스트 보상의 ‘분해기칸 확장’으로 늘릴 수 있다. 기본/최대 칸 수는 공용 규칙에서 수정한다.
- 무기 120초 → 같은 희귀도 무기 부품, 방탄 60초 → 방탄 섬유, 패시브 30초 → 패시브 부품.
- 효과 없음 패시브는 부품 2개. 등급별 재료와 15개 레시피를 등록했다.
- 인벤토리 공간이 없으면 완료 결과/취소 원본을 보존한다. 분해기 최대 칸 수를 낮춰도 작업 중인 슬롯은 숨기지 않는다.
- 큐와 투입품은 인벤토리와 같은 원정 저장 파티션에 저장한다. 재접속은 저장된 남은 시간부터, 오프라인 경과 시간은 적용하지 않는다. 사망 시 소지품과 함께 소실한다.
- 장착 중 패시브를 넣어 효과를 없애는 작업은 레이드에서 금지한다. 슬롯 감소로 아이템이 잘리는 해제·리롤·분해도 사전 차단한다.

## 3. 패시브

19종 공통 효과의 Normal/Rare/Unique/Legendary 범위·간격·고정 페널티와, Unique/Legendary 전용 도탄 정의를 만들었다. 고정 바이러스 방호·대시·불활성 모듈은 리롤 불가지만 정상 귀속/장착할 수 있다.

효과 그룹 ID를 분리해, 방탄 패시브의 이동 속도 페널티가 이동 속도 패시브와 중복 효과로 오인되지 않게 했다. 관통력/방탄 소수점, 가방 칸, 스태미나, 시야각, 피격 에임펀치, 억제제 시간, 탄약 비소모 등 런타임 적용 경로를 연결했다. 적도 공유 스탯을 사용하며 탄약 비소모/기존 피격 회피를 적용한다.

도탄은 새 반사 탄도를 만드는 기능이 아니라 기존 **DodgeChance 피격 무효 판정**을 유지한 데이터다. 새 패시브 원본·카탈로그·적 풀은 등록했다. 플레이어 루팅 테이블/상점 상품/아이콘/가격은 기존 콘텐츠를 일괄 교체하지 않았으며 실제 공급 배치는 해당 Maker에서 별도 지정한다. 예전 TestModule 원본은 기존 저장/참조 호환을 위해 삭제하지 않았다.

## 4. 섹터 이동·HFSM

일반 섹터 이동에 클리어 판정을 요구하지 않는다. 실제 탈출의 전력·클리어 등 조건은 분리해 유지한다. 포탈은 1초 상호작용, 실제 이동 완료 후 6초 쿨타임이다. 취소 시 프롬프트를 복구하며 누르고 있는 자동 사격의 실제 발사 이벤트도 취소 조건에 포함했다.

재진입으로 이미 시작된 스포너의 설정/웨이브/적 수를 초기화하지 않는다. 적 GameObject나 HFSM을 껐다 켜는 방식은 추가하지 않았다. 기존 FSM 에셋·상태 전이는 재작성하지 않고 경계 반응/기억 시간·순찰 수치만 사용했다. 런타임 충돌이 없다는 최종 확인은 Play Mode에서 해야 한다.

현재/방문한 방만 공개하고 미방문 방은 숨긴다. 보스 아이콘은 숨긴다. 새 맵 생성 때 바이러스 방·추가 적·1스테이지 보스 위치 결정을 저장하며 재진입/이어하기에서 재추첨하지 않는다. 1스테이지 보스는 전용맵/사무실/창고 후보를 사용하고 탈출 경로 보호 옵션을 유지한다.

## 5. 감염·전투·콘텐츠 설정

원형 노출 게이지 → 스택 → 5초마다 기존 치명 감염 수치 누적으로 연결했다. 가스/시체·루팅 확인/아이템 획득/피격/출혈이 게이지를 채운다. 노출 게이지는 매우 느리게 줄고, 기존 치명 감염 수치는 자연 회복하지 않는다. 앰플은 게이지/스택도 치료한다. 방호 수치는 게이지 용량을 늘린다. 방어구 등급과 감염 방호는 별개다.

스테이지별 무작위 바이러스 방 개수, 감염 적 탄의 추가 노출, 추가 적 확률, 3스테이지 일반 적의 희귀 Punisher 치환을 설정했다. 수치/확률은 테스트 기본값이다. 새 외형·전용 적 AI를 만든 것은 아니다.

낮은 엄폐물의 총알 통과·엄폐 방탄 등급을 Sector Maker에서 설정할 수 있다. 낮은 엄폐의 시야 OFF 적용은 명시적인 버튼이다. 기존 프리팹 전체를 자동 변경하지 않았다.

대시 기본 쿨다운 10초/거리 ×1.3, 1~3스테이지 전용50/공용25/특별25 비율, 공통 등급 색상을 반영했다. 4~6스테이지 실제 콘텐츠 제작은 하지 않았다.

## 6. HOME·퀘스트·루팅 제작

퀘스트 물품 보상은 HOME 창고에 지급하며 전체 보상 공간을 먼저 검사한다. 창고가 가득 찼으면 완료하지 않는다. 개별 보상 영수증을 저장해 중간 저장/재시도 중복 지급을 방지한다. 수락 시 지급품도 공간/소유 한도를 먼저 검사한다. 패시브 지급품은 개체별 수치를 생성한다.

HOME 진입은 명시된 안전 스폰 포인트 또는 기존 플레이어 시작점을 우선하며, 바닥/충돌을 검사하고 막혀 있으면 주변 NavMesh를 찾는다. 안전 지점이 없으면 오류를 남기고 진입을 실패시킨다. HOME 맵을 자동 배치하거나 기존 구조물을 이동하지 않았다.

공용 규칙의 분해기 탭에는 선택한 기존 모델을 루팅 오브젝트로 만드는 에디터 버튼이 있다. LootObjectSO와 기존 이벤트 채널을 선택하고 중앙 상호작용 앵커를 만든다. 프리팹 모드/씬 선택 대상으로만 적용하며 Undo 가능하다. 플레이 중 자동 배치를 추가하지 않았다.

## 주요 변경 파일

- 공통 규칙/레벨: `Assets/Scripts/Gameplay/Progression/GameSystemRulesSO.cs`, `GameProgressionRuntime.cs`, `GameProgressionIdentity.cs`, `Assets/Resources/GameSystemRules.asset`.
- 저장: `Assets/Scripts/Core/Persistence/Save.cs`, `RunLifecycleCoordinator.cs`, `PlayerRunStatePersistence.cs`.
- 인벤토리/분해: `Assets/Scripts/Gameplay/Entities/Shooter/Player/Inventory/PlayerInventoryRuntime.Recycling.cs`, `PlayerInventoryRuntime.PassiveModules.cs`, `PlayerInventoryRuntime.cs`, `PlayerConsumableUseRuntime.cs`, `PlayerInventoryRuntime.GasMask.cs`.
- 패시브: `Assets/Scripts/Gameplay/Items/Definitions/PassiveItemSO.cs`, `PassiveInstanceRuntime.cs`, `Assets/Scripts/Gameplay/Entities/Shooter/Player/Passive/PlayerPassiveInventoryRuntime.cs`, `Assets/Resources/PassiveModules/Module_*.asset`, `ArmorFiber_*.asset`, `WeaponParts_*.asset`, `Assets/Resources/PassiveModuleRules.asset`, `RuntimeContentCatalog.asset`.
- 스탯/전투: `Assets/Scripts/Gameplay/Entities/Shooter/Common/Core/StatModifierTypes.cs`, `ShooterStatsRuntime.cs`, `Assets/Scripts/Gameplay/Entities/Shooter/Player/PlayerStatsRuntime.cs`, `PlayerAimController.cs`, `PlayerBulletLoadoutRuntime.cs`, `PlayerShooterShoot.cs`, 적 Stats/Shoot/Config 및 HFSM Investigation/Perception 수치 경로, `EntityEquipmentRuntime.cs`, `Damageable.cs`, `BasicBullet.cs`.
- 진행/생성: `Assets/Scripts/Gameplay/Progression/Stage/StageProgressionManager.cs`, `Assets/Scripts/Gameplay/World/Sectors/StageMap/StageSystemPolicies.cs`, `StageMapBuildPipeline.cs`, `StageMapLayout.cs`, `StageRoomNode.cs`, `RaidRuntime.cs`, `RadialBraidedRaidGenerator.Landmarks.cs`, `SectorEnemySpawner.cs`, `StageSectorInstantiator.cs`, `SectorStateManager.cs`, `SectorPortalManager.cs`.
- 감염/엄폐: `PlayerShooterInfection.Exposure.cs`, `PlayerShooterInfection.cs`, `ShooterInfection.cs`, `SectorRoomTraitRuntime.cs`, `InfectedEnemyMarker.cs`, `SectorDestructibleCover.cs`.
- HOME/보상: `Assets/Scripts/Gameplay/Progression/Hideout/HomeSector.cs`, `Assets/Scripts/Gameplay/Progression/Quests/QuestRuntime.cs`, `QuestDefinitionSO.cs`, `Assets/Scripts/Gameplay/World/Sectors/Treasure/InheritanceWarehouseRuntime.cs`, `MetaUpgradeCatalogSO.cs`.
- 툴: `Assets/Scripts/Authoring/Editor/CharacterStatusMakerWindow.cs`, `GameSystemRulesInspector.cs`, `LootObjectConversionUtility.cs`, `SectorMakerWindow.Vision.cs`, `ProgressionFoundationWindow.cs`, `DebugMonitorWindow.cs`, `Assets/Editor/ItemMakerWindow.cs`, `QuestDesignTreeWindow.cs`, Authoring Hub 진입 문구.
- UI/표현: `PlayerRecyclingPanelUI.cs`, `PlayerInventoryUI.cs`, `InventoryItemContextMenuUI.cs`, `InventoryItemSlotUI.cs`, `PlayerTooltipUI.cs`, `InfectionExposureGaugeUI.cs`, `StatusEffectIconStripUI.cs`, `ShopItemUI.cs`, `RaidMapPresentation.cs`.
- 검증: `Tools/GameSystems/VerifyGameSystems.ps1`, `Tools/CombatModules/VerifyModules.ps1`, `verify-assets.cjs`, `compile.cjs`, `Assets/Tests/EditMode/Editor/GameSystemsFoundationTests.cs`.

## 검증 결과와 경계

- 실제 Unity 컴파일러 참조를 이용한 **격리 컴파일**: 런타임 오류 0/경고 38, 에디터 오류 0/경고 0. `Library/CombatModulesVerification`에만 출력하며 실제 ScriptAssemblies는 덮어쓰지 않았다.
- 실제 원본 코드를 실행하는 관리형 대역 검사: 모듈/귀속/리롤/기존 회귀 **4,257개**, 경험치/정산/분해기/감염/보스 경로 **2,643개** 통과.
- 카탈로그/에셋/소스 계약 검사 **220개** 통과.
- 기존 작은 맵/탈출 경로/상인 확률 검사 통과: 작은 맵1,344, 일반160, 경계454 사례 등.
- `dotnet build Assembly-CSharp.csproj --no-restore -v minimal`: `Temp/obj/Assembly-CSharp/project.assets.json` 부재로 NETSDK1004. 패키지/프로젝트를 임의 복원하지 않고 위 Unity 참조 격리 컴파일로 코드 오류를 확인했다.
- 새 EditMode 테스트 소스를 추가하고 컴파일했다. **Unity 내부 Test Runner 실행, 실제 import/직렬화, Play Mode, HFSM 전투, UI 배치는 아직 실행하지 않았다.** 사진을 실제 플레이 결과처럼 만들지 않았다.

## Unity에서 우선 확인할 순서

1. 재임포트/컴파일 후 Game System Tool에서 GameSystemRules와 분해기 레시피/희귀 Punisher 참조를 확인한다.
2. HOME에서 고정 패시브 장착·해제·리롤 정책을 확인하고 레이드에서는 변경이 거절되는지 본다.
3. 분해기 2칸 동시 등록 → 중간 취소 → 재등록 초기화 → 완료 → 가방 꽉 찬 상태 수령 실패 → 저장/재시작을 확인한다.
4. 적을 일부 처치한 뒤 포탈 왕복: 적 수/남은 체력/경계/HFSM 상태가 초기화되지 않는지 확인한다. 도착 후 6초, 이동 전 1초도 확인한다.
5. 일반 상호작용 중 이동·연사·대시·피격 취소와, T 기능처치 중 이동 허용을 각각 확인한다.
6. 상자 재열기 경험치 0회 추가, 해치 이동 대기 경험치 유지, HOME 탈출100%/사망50% 정산, 이어하기 중복 지급 방지를 확인한다.
7. 감염 게이지 완주/스택/5초 틱/앰플, 낮은 엄폐물의 시야·총알 통과, HOME 시작점 충돌 검사를 확인한다.
8. 창고가 가득 찬 상태에서 퀘스트 완료가 차단되고 공간 확보 후 한 번만 지급되는지 확인한다.
