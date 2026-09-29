# 작업 순서·미완성 목록 | 현재 프로젝트 기준

검사 기준: 2026-09-22 / E:/github/board_chase/board_chase. 현재 파일의 정적 연결·핵심 실행 경로 기준이며, 별도 표시 없는 항목은 이번 감사에서 플레이 테스트한 결과가 아닙니다.

## 1. 안정된 시작점 — 먼저 해결

- Git/백업 기준선 확정. 기존 대량 사용자 변경을 잃지 않는다.
- 부팅 씬 활성/빌드 경로, 메뉴 Addressables 참조 일치.
- RuntimeContentCatalog YAML와 누락 항목, 플레이어 초기 장비·Home 상점 참조 정리.
- DefaultVolumeProfile와 최신 Unity 임포트 오류 확인.

완료 조건: 새 Player 빌드에서 메뉴→Home→RUN→귀환→재출정 가능, 필수 카탈로그 로드와 참조 검사 통과. 단순 C# 컴파일 성공은 부족하다.

## 2. 진행을 잃거나 복제하지 않게 만들기

- 루팅 수령/창고 이동/퀘스트 제출·보상/배터리 소비의 저장 단위 확정.
- 중간 OnChanged 저장의 중복 위험 재현·수정·회귀 테스트.
- 기존 세이브 이전, 누락 GUID, 백업 3세대, 사망 복원, 종료 경계 테스트.
- 전투 중 이어하기에서 무엇을 복원하고 무엇을 재생성하는지 명시.

## 3. 현재 맵 역할을 실제 콘텐츠로 연결

- Armory/Medical/Vault/Generator/Hatch 전용 프리셋·Encounter·Content 미연결 채우기.
- 프로토타입 단말을 실제 외형/상호작용으로 교체.
- 일반 탈출/허브/해치 지도와 전력 상태 확인.
- 특별 탈출은 현재 WIP 유지. 조건 합의 전 활성화하지 않음.

## 4. 전투·장비 제작

- 실더 보스 프리팹에 실드 계층 연결과 파손 연출.
- 플레이어/적 소켓 리그 연결, Face 하위 숨김과 Hair 독립 확인.
- 총·탄 패밀리·tier·적 로드아웃·루팅 조율.
- 무기/방탄 내구 주기, 응급 근접, 의료 소모품 효과·시간·상태이상 조정.
- 고단계/Stage5 템플릿 보스와 엔딩 관련 콘텐츠 구체화.

## 5. 퀘스트와 경제 — 초안과 활성 분리

- RaidProgressionDraft의 0/빈 항목을 실제 요구량·금액·퀘스트 참조로 확정.
- Normal→Rare→Unique→Legendary 제출, 보스+금액의 해금 순서 설계.
- LV1~6 상점 해금과 LV7~8 특수 획득 제한을 실제 표/보상과 검증.
- 방탄·주사기·탄약·스탯·창고 업그레이드 퀘스트 연결.
- 초기 가방, 재화 수급/지출, 상점 재고·수리·루팅 비율 검수.

## 6. 표시·음향·마감

- UI_Settings_General 중복/빈 표와 하드코딩 문구 정리, KO/EN 화면 테스트.
- 실제 소켓/머티리얼/폰트/정렬 버튼 위치 확인.
- NPC 임시 알림 → 실제 대사/목소리, 수색·UI·충돌음 음량 조정.
- 실제 최대 맵 성능, Player 빌드/재시작 연속 테스트.

## 하지 않은 일을 완료로 표시하지 않기

위 항목은 이번에 패치한 목록이 아니라 현재 감사 기반의 남은 작업이다. 새 시스템을 병렬로 계속 만드는 것보다 기존 책임·SO 연결과 검증을 닫는 순서다. ‘70~80% 완성’ 대신 각 승인 조건 통과 여부를 기록한다.

## Notion 작업 카드 필수 필드

목표 / 현재 동작 / 변경 후 동작 / 비범위 / 소유 시스템·파일 / SO·프리팹 / 저장 영향 / UI·현지화·오디오 영향 / 승인 테스트 / 결과 증거. 상태는 제안→확정→구현중→연결확인→검증완료로 구별한다. 이 상태 정책은 앞으로의 작업 방식 제안이며 런타임 기능이 아니다.

## 코드·데이터 근거

- `E:/github/board_chase/board_chase/Assets/Resources/RuntimeContentCatalog.asset`
- `E:/github/board_chase/board_chase/ProjectSettings/EditorBuildSettings.asset`
- `E:/github/board_chase/board_chase/Assets/Scripts/Gameplay/World/Sectors/StageMap/RaidRuntime.cs`
- `E:/github/board_chase/board_chase/Assets/Scripts/Gameplay/Progression/Quests/QuestRuntime.cs`
- `E:/github/board_chase/board_chase/Assets/Scripts/Gameplay/Entities/Shooter/Common/Visuals/ShooterVisualRig.cs`
- `E:/github/board_chase/board_chase/Assets/Scripts/Gameplay/Entities/Common/Equipment/ShieldArmorRuntime.cs`