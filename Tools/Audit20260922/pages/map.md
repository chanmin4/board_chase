# 맵·포탈·탈출구 | 좌표와 생성 규칙

검사 기준: 2026-09-22 / E:/github/board_chase/board_chase. 현재 파일의 정적 연결·핵심 실행 경로 기준이며, 별도 표시 없는 항목은 이번 감사에서 플레이 테스트한 결과가 아닙니다.

## 좌표와 크기

Unity 내부 좌표는 0부터 시작한다. N은 홀수이며 중앙 c=(N-1)/2이다. 사용자 설명의 7×7 중앙 (4,4)는 내부 (3,3)이다. 9×9는 (4,4), 11×11은 (5,5), 13×13은 (6,6)이다.

1~5스테이지 N은 7,9,11,13,13. 외부 시작 허브 (-1,0)는 N×N 면적에 포함하지 않는다.

## 특수 방 배치

- 중앙 해치: (c,c) 한 칸 고정.
- 우상단 후보 영역: x>c, y>c. 일반 탈출구와 보스 후보를 서로 다른 칸에 배정한다.
- 발전기 후보: x<c,y>c 또는 x>c,y<c. 중앙 행·열은 제외한다. 후보 영역 안의 한 위치를 선택한다.
- 탈출 위치 선택은 시작점으로부터 경로 거리 기준도 사용한다. 단순 화면상 직선 거리와 구별한다.
- 보스 출현은 시드 기반 65%. 불출현 시 보스 후보의 역할 정보와 실제 보스 전투 유무가 같다고 가정하지 않는다. 코드에서는 후보 역할을 유지하되 일반 방 종류로 처리하는 경로가 있다.
- 특수 위치를 예약한 뒤 나머지 방에 Normal/Armory/Medical/Vault 역할을 배분한다. 기본 가중치는 70/10/15/5다.

## 재생성과 이어하기

탈출 위치/보스 위치/보스 확률/발전기/역할 배분의 난수 salt가 분리되어 있다. 생성 버전은 1이며, 확정된 배치와 해결 상태를 저장한다. 같은 런에서 메뉴 재진입이나 복원 때문에 보스 확률과 발전기를 새로 추첨하면 안 된다.

## 탈출 정책

- 시작 허브 귀환: 허브 좌표에서 가능.
- 일반 탈출: 지정 위치에서 가능하며 보스 처치가 선행 조건이 아니다.
- 해치: 중앙 위치 + 해당 런의 발전기 가동이 필요.
- 특별 탈출: 3스테이지 이상용 설정 틀은 있지만 현재 비활성이고 사용 허가 경로도 열려 있지 않다. ‘이미 세 번째 탈출구가 작동한다’고 쓰지 않는다.

## 지도 표시

시작 지점·일반 탈출구·발전기와 실제 출현 보스는 지도 표시 대상이다. 해치는 중앙으로 고정되어도 발전 전에는 위치 공개/사용 가능 상태가 열리지 않는 정책이다. 전력 투입 후 공개한다. 특별 탈출구는 향후 숨김 정책을 붙일 대상이며 현재 활성 콘텐츠가 아니다.

## 포탈과 방의 관계

SectorPortalManager는 이웃 좌표 이동과 현재 방 변경 알림을 담당한다. 방 입장 제한·전투·시간·탈출 조건을 포탈 메시 자체에 복제하지 않는다. SectorStateManager가 가진 방 상태와 이벤트를 소비한다.

## 승인 테스트

5개 스테이지×여러 시드에서 중앙 고정, 사분면, 겹침 없음, 시작 거리, 65% 추첨의 재진입 안정성, 허브 복귀, 지도 공개 시점을 검사해야 한다. 이번 감사는 생성 코드와 직렬화 설정 대조이며 대량 시드 테스트 재실행 결과는 아니다.

## 코드·데이터 근거

- `E:/github/board_chase/board_chase/Assets/Scripts/Gameplay/World/Sectors/StageMap/RaidMapGenerator.cs`
- `E:/github/board_chase/board_chase/Assets/Scripts/Gameplay/World/Sectors/StageMap/RaidMapSettings.cs`
- `E:/github/board_chase/board_chase/Assets/Scripts/Gameplay/World/Sectors/StageMap/RaidRuntime.cs`
- `E:/github/board_chase/board_chase/Assets/Scripts/Gameplay/World/Sectors/Runtime/SectorPortalManager.cs`
- `E:/github/board_chase/board_chase/Assets/Scripts/Gameplay/World/Sectors/Runtime/SectorStateManager.cs`