# 방 종류·프리셋·배치 제작 기준

검사 기준: 2026-09-22 / E:/github/board_chase/board_chase. 현재 파일의 정적 연결·핵심 실행 경로 기준이며, 별도 표시 없는 항목은 이번 감사에서 플레이 테스트한 결과가 아닙니다.

## 현재 방 역할

Normal, Armory, Medical, Vault, Boss, Generator, Hatch, Extraction 역할을 사용한다. 역할(role), 방 전투 종류(roomType), 섹터 지형 프리팹(preset), Encounter, 루팅 Content는 같은 필드가 아니다.

## 실제 연결 수준

StageSectorInstantiator는 역할 전용 preset/encounter/content를 찾고 없으면 기본 설정으로 내려간다. 현재 1~5스테이지 데이터에는 역할별 전용 preset/encounter/content가 null인 항목이 남아 있다. Armory/Medical/Vault의 LootObject 연결은 있지만 그 사실만으로 모든 전용 방 지형과 적이 완성된 것은 아니다.

- 일반방: 기본 전투와 일반 콘텐츠.
- 무기고: 무기·탄 중심 루팅을 지정할 수 있다. 전용 중무장 Encounter 제작/연결은 별도 확인 대상.
- 의무실: 의료 아이템 루팅. 의료실 외형과 일반방 유사 전투를 각각 지정해야 한다.
- 금고방: 귀중품과 잠금/열쇠 정책을 분리해 연결한다.
- 발전소: 발전기 단말은 런에서 붙이는 경로. 전용 지형/배터리 배치 콘텐츠는 따로 제작한다.
- 해치방: 중앙 위치와 전력 조건은 RaidRuntime이 소유한다. 외형은 프리셋/콘텐츠 책임.
- 일반 탈출방: 정상 귀환 단말과 지도 표시.
- 보스방: 전용 보스 구성. 실제 보스 출현 여부와 후보 좌표 역할을 구별한다.

## 섹터 제작 흐름

1. Sector Maker에서 지형·출입구·충돌·배치 프리셋을 편집한다.
2. 적/웨이브 Encounter를 연결한다.
3. 상자·상점·터미널 등 ContentProfile 또는 역할별 LootObject를 연결한다.
4. StageDefinition에서 역할 override를 연결한다.
5. 해당 역할이 실제 생성된 런에서 길 막힘, NavMesh, 포탈, 상호작용 거리, 수색·지도·저장을 확인한다.

## 안정된 식별자

RaidRuntime은 방 좌표와 컨테이너 이름/형제 경로 기반 키로 컨테이너 상태를 구분한다. 배치 구조·이름을 바꾸면 이전 저장의 키가 달라질 수 있으므로 단순 아트 수정처럼 취급하지 말고 저장 호환성을 점검한다.

## 지금의 우선순위

새 방 종류를 계속 추가하기 전에 현재 8개 역할의 지형·전투·콘텐츠 참조를 채워야 한다. fallback으로 생성된 방을 의도한 최종 방으로 착각하지 않도록 에디터와 문서에 연결 완료 여부를 표시한다.

## 코드·데이터 근거

- `E:/github/board_chase/board_chase/Assets/Scripts/Gameplay/World/Sectors/StageMap/StageSectorInstantiator.cs`
- `E:/github/board_chase/board_chase/Assets/Scripts/Gameplay/World/Sectors/ScriptableObject/StageDefinitionSO.cs`
- `E:/github/board_chase/board_chase/Assets/Scripts/Authoring/Editor/SectorMakerWindow.cs`
- `E:/github/board_chase/board_chase/Assets/Scripts/Gameplay/World/Sectors/StageMap/RaidRuntime.cs`
- `E:/github/board_chase/board_chase/Assets/ScriptableObject/GameScene/Stage/RaidLoot/RaidContainer_Armory.asset`
- `E:/github/board_chase/board_chase/Assets/ScriptableObject/GameScene/Stage/RaidLoot/RaidContainer_Medical.asset`