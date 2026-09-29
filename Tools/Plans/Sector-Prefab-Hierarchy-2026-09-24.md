# Sector prefab authoring — 2026-09-24

## 편집 기준

`Assets/Prefabs/GameScene/Sector`를 방 **종류**와 실제 **배치**로 분리한다. 스테이지 번호는 방 프리팹 폴더가 아니라 StageDefinition의 선택 목록/적 인카운터가 결정한다.

```text
Sector/
  Bases/
    Sector_Base                 기존 공통 원본: 바닥, 경계/포털, 루트, 런타임
    NormalSectorBase            ┐
    BossSectorBase              │
    ArmorySectorBase            │ 모두 Sector_Base의 직접 바리언트
    MedicalSectorBase           │ 다른 방 종류를 상속하지 않는다
    TreasureSectorBase          │
    GeneratorSectorBase         │
    HatchSectorBase             │
    ExtractionSectorBase        ┘
  Rooms/
    Normal/Normal_2 .. Normal_5  기존 배치와 GUID 유지
    Boss/Boss_01                기존 보스 배치 복사, BossSectorBase 상속
    Armory/Armory_01            ┐
    Medical/Medical_01          │ 종류별 베이스 상속
    Treasure/Treasure_01        │ 편집 출발점: 기존 배치 2 복사본
    Generator/Generator_01      │ 완성된 전용 방 아트가 아님
    Hatch/Hatch_01              │
    Extraction/Extraction_01    ┘
    Start/StartSector           기존 외곽 입장/귀환 허브
  HomeSector                    기존 파일 내용/상속 유지, 새 Home 베이스 없음
  HomeServices/                 Home 서비스 + 기존 SectorShop
  Shared/                       Container, TransformPoint, Barriers&Portal, Portal, Lighting, NavMeshes
  Floor/  WALL/  SectorObject/  LootTemplate/   재사용 배치 재료: 그대로 유지
  WIP/
    UnusedLayouts/              기존 BattlePreset 1, 6, 7, 8, 9 (현재 미참조)
    LegacyRooms/                옛 Named, Wave, Shop, Treasure, QTEStation
    UnusedParts/                미참조 Decoration
```

## 어디에 무엇을 넣는지

- **Sector_Base**: 모든 방의 공통 구조만. 기존 Home도 상속하므로 Home에 적용되면 안 되는 물건은 넣지 않는다.
- **종류별 Base**: 그 종류에 항상 필요한 공통 구조. 현재는 기존 공통 원본을 상속하는 최소 골격이다. 적 스폰 위치/장애물 배치는 넣지 않았다.
- **Rooms의 개별 바리언트**: 바닥 배치, 장애물, EnemySpawnPoint, 콘텐츠 슬롯/상자, 해당 배치만의 장식. 처음 제공한 `_01`은 스폰 위치가 있는 기존 전투방 복사본이다.
- **SectorPreset SO**: 사용할 방 프리팹 + Normal policy + Content profile.
- **StageDefinition**: 방 선택 확률, 적 encounter, 레이드 제한시간, 룸 역할별 루팅. 프리팹 폴더 이름만 바꿔서는 방 역할이 바뀌지 않는다.

## 새 방 추가 순서

1. `Bases/MedicalSectorBase`처럼 해당 종류를 선택 → 우클릭 **Create → Prefab Variant** → `Rooms/Medical`에 `Medical_02`로 저장한다.
2. Prefab Mode에서 배치하고 Sector Maker로 적 스폰 위치/콘텐츠 슬롯을 넣는다. 빈 베이스는 아직 완성된 전투방이 아니므로 **Check Selected Room**으로 확인한다.
3. 해당 `SectorPreset` SO를 복제하고 `Sector Prefab`을 새 바리언트의 SectorRuntime으로 연결한다. SO 복제만 하면 기존 방을 계속 가리키므로 이 연결을 반드시 바꾼다.
4. 해당 StageDefinition의 아래 목록에 **SO**를 넣고 양수 weight를 준다. prefab 자체를 목록에 넣는 것이 아니다.

| 종류 | 등록 위치 |
|---|---|
| 일반방 | Normal Battle → Sector Presets |
| 무기고/의무실/보물방/발전소/해치/일반 탈출구 | Raid Runtime → Rooms → 해당 Role → Sector Variants |
| 보스방 | Boss Battles → Sector Preset (적 Encounter와 한 쌍) |
| 시작/귀환 허브 | Start Sector Presets |

특수방 초기 SO 위치: `Assets/ScriptableObject/GameScene/Stage/Sector/Preset/RaidRooms/<종류>/`.
보물방은 기존 enum 이름 **Vault**를 그대로 사용한다. 옛 `StageRoomType.Treasure`/SectorTreasure 보상방과 다른 시스템이다.

## 선택 규칙과 보존 범위

- Stage1–5에 Armory, Medical, Vault, Generator, Hatch, Extraction의 전용 `Sector Variants`를 연결했다. 초기에는 종류당 한 배치만 등록한다. 역할에 맞는 아트/루팅/상호작용 위치를 이후 다듬으면 된다.
- 여러 항목은 **상대 가중치**로 고른다. 예: 1:3이면 약 25%:75%. 0/음수/빈 SO/잘못된 RoomType은 제외한다.
- 변형 풀이 비었으면 기존 단일 `Sector Preset`, 그것도 없으면 기존 스테이지 일반방 풀을 사용한다.
- 선택은 stage seed + 좌표 + role 기반이며 Unity 전역 Random 상태를 소비하지 않는다. 같은 데이터/같은 레이드의 재구성은 같은 배치를 고른다. 진행 중인 레이드가 있는 상태에서 authoring 목록 자체를 바꾸면 배치가 달라질 수 있으므로 변경 후에는 새 레이드로 검증한다.
- 보스는 기존 Encounter 선택을 유지하고 전용 Boss_01에 연결한다. 기존 적/가스/타이머/65% 보스 확률/맵 크기/시작 모서리/중앙 해치/발전소 배치 규칙은 바꾸지 않는다.
- 발전소/해치/탈출의 상호작용은 기존 RaidStationFactory가 계속 맡는다. 방 이름이나 프리팹 상속으로 동작을 자동 추론하지 않는다. 전용 모델/단말 위치가 없으면 기존 프로토타입 단말을 사용한다.
- 기존 일반방 SO 1과 2가 같은 배치 2를 가리키는 구성은 그대로 두었다. 이번 작업에서 선택 확률을 임의로 재조정하지 않는다.
- 이동 에셋은 `.meta`/GUID 유지. 기존 일반방 객체의 local fileID를 보존하도록 부모 상속을 삽입한다. Home/Player/Enemy 프리팹, 씬 파일은 수정하지 않는다.
- WIP는 삭제가 아니다. 옛 Shop/Treasure는 기존 Legacy 설정 참조가 남아 있어도 현재 OddSquareRaid에서 방으로 생성하지 않으므로 WIP에 보관하고 GUID 참조는 유지한다. 단순히 미배치라는 이유로 재료 라이브러리 전체를 WIP로 옮기지는 않는다.

## 검증

검증 결과:

- Unity 6000.0.47f1 격리 프로젝트: 변경 전 26개 / 변경 후 39개 프리팹 로드. Missing Script 0, 깨진 override target 0.
- 기존 객체 및 새 `_01`의 복사 원본 비교: 26,391개 객체/컴포넌트, 680,439개 필드 검사. local fileID 손실 0, 의도한 루트 이름 변경 이외의 필드 차이 0. 공유 포털 ParticleSystem의 커브 내부는 이 수치에서 제외했으며 해당 원본은 수정하지 않았다.
- `SectorPrefabHierarchyTests` + 기존 `RaidMapFoundationTests`: **31개 통과 / 실패 0**. 맵 크기별 1,000 시드(총 5,000)의 모서리/특수방 규칙, 기존 런 저장 레이아웃 호환 포함.
- 실제 파일 최종 스캔: Sector 하위 프리팹 960개 중 source prefab 누락 0, 관련 중복 GUID 0, 고아 meta 0. SectorPreset의 SectorRuntime 참조 25개와 Stage1–5 역할별 연결 30개 정상.
- 실제 Unity 재오픈/Refresh 후 읽기 전용 검증: 상속/스폰/역할 풀/선택 규칙 5개 + Stage1–5의 Start/Normal 참조 5개, **10개 모두 PASS**. 초기 재임포트의 Invalid Start/Normal 경고는 이후 실제 에셋 로드 검사에서 정상임을 확인했다. 임시 검증 스크립트와 메타는 실행 후 제거했고 결과는 `Library/SectorHierarchyBackup-20260924/live-check.txt`에 기록했다.
- 이동 중 Unity가 조명 프리팹을 잡고 있어 사용자가 에디터를 종료한 뒤 나머지를 완료했다. Stage1에 있던 NavMesh 데이터는 사용 중이므로 WIP가 아니라 `Shared/NavMeshes`로 GUID 보존 이동했다. 빈 Stage1–5/SectorBattlePreset 폴더만 제거했다.
- Home 프리팹 내용은 변경하지 않았다. 재오픈 후 실제 레이드 전투/내비게이션/전용 아트 배치는 별도 플레이 검증 대상이다.

검증 중 **기존 문제**도 발견했다: `TempBossShooterConfig`가 존재하지 않는 프리팹 GUID `a8af717b9f104d52bf1bf8d3bf775d3f`를 가리킨다. `TempBossWave`에서 사용하는 연결이며 변경 전 격리 로딩에서도 동일하게 경고가 났다. 이번 섹터 폴더 작업에서 임의로 다른 보스에 연결하거나 보스 선택 확률을 바꾸지는 않았다. **2026-09-25 후속 정리:** Stage5 Encounter를 기존 `Stage5BossWaveTemplate`에 연결해 활성 스테이지의 잘못된 wave 선택을 해결했다. 옛 Config는 다른 호환 참조가 남아 있어 삭제하지 않았다. 상세: `Enemy-Spawn-Link-Cleanup-2026-09-25.md`.

마지막 이동 전 안전 복사본과 최종 정적 검사 결과는 `Library/SectorHierarchyBackup-20260924/`에 있다. Library는 재생성 가능한 로컬 캐시이므로 영구 버전 관리/백업 대용으로 보지 않는다. 기존 Temp 검증 프로젝트는 사용자가 Unity를 종료할 때 Unity에 의해 정리되었으며, 위 수치는 실행 시 확인한 결과를 기록한 것이다.
