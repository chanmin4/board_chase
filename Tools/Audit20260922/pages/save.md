# 06. 저장·이어하기·마이그레이션·복구

검사 기준: 2026-09-22 / E:/github/board_chase/board_chase. 현재 파일의 정적 연결·핵심 실행 경로 기준이며, 별도 표시 없는 항목은 이번 감사에서 플레이 테스트한 결과가 아닙니다.

## 세 파티션

- settings.es3: 기기 설정. 오디오, 화면/그래픽 값, 감도, locale code, 키 바인딩 JSON. schemaVersion 1.
- profile_default.es3: 창고, 스테이지/상점 해금, 퀘스트 진행·보상 영수증·스탯/창고 보상, 레거시 호환 메타 필드. schemaVersion 1.
- run_default.es3: 현재 플레이어/아이템·장비·탄약·상점 런 상태·레이드 스냅샷. schemaVersion 2.
- backups: 파티션별 최근 백업 3개 회전.

SaveSystem이 ES3 저장과 구버전 호환을 조율한다. 레거시 단일 ES3/JSON/PlayerPrefs에서 필요한 파티션을 옮기는 마이그레이션 경로가 있다. Locale Unity 객체 대신 ko/en 코드 문자열을 저장한다.

## 쓰기 절차

임시 파일에 저장 → 다시 읽어 검증 → 유효한 기존 본파일을 백업 회전 → 임시 파일을 본파일로 교체하는 흐름이다. 함수 이름에 Atomically가 있어도 여러 파티션을 한꺼번에 OS 수준 단일 트랜잭션으로 커밋한다는 보장은 아니다. 전원 차단·파일 잠금·디스크 부족 테스트는 별도로 필요하다.

설정 변경은 0.75초 디바운스로 쓰기를 합친다. RunLifecycleCoordinator가 대기 쓰기를 Update에서 flush하고 일시정지/종료 등 중요 경계에서 즉시 저장한다. 레이드 상태도 주기 체크포인트와 중요 이벤트를 통해 저장한다.

## 플레이어 스냅샷

HP·감염, 인벤토리/장착품, 스택 내구 상태, 탄약/탄창, 패시브, 지속시간 있는 효과, 상점 제안 상태 등을 복원한다. 아이템은 영구 GUID로 찾으며 하나가 사라졌다고 전체 복원을 무조건 포기하지 않고 해당 항목을 제외/경고하는 경로가 있다.

## 레이드 스냅샷

시드와 배치 해결 결과, 현재 스테이지/좌표, 경과·초과 시간, 발전기/해치, 보스 출현 결정, 컨테이너 추첨·잠금·내용 상태를 보관한다. 정확한 전투 중 모든 적 위치/HP/행동, 모든 바닥 드롭/시체, 모든 방 타이머 순간 상태까지 저장하는 완전한 월드 스냅샷이라고 쓰면 안 된다. 현재 저장 모델의 필드와 복원 훅이 있는 것만 보증 대상으로 삼는다.

## 수명주기

Booting/Home/RunActive/RunSuspended/RunCompleted/GameOver 상태를 나눈다. 사망 시 런 스냅샷을 비우고 재캡처를 억제하여 사망 직전 살아 있는 저장이 다시 덮이지 않게 한다. 프로필 영구 진행과 런 소지품 손실은 별도 정책이다.

## 현재 위험

1. RuntimeContentCatalog.asset에 실제 잘못된 YAML 줄과 누락 GUID들이 존재한다. 런타임 아이템 복원의 기본 자산을 먼저 고쳐야 한다.
2. Player 프리팹/초기 로드아웃/상점에도 누락 참조가 있다. 에디터에 우연히 로드된 SO로 복원이 되는 것을 빌드 안전성으로 착각하면 안 된다.
3. 루팅 이동 중 OnChanged→퀘스트 저장이 먼저 일어날 수 있는 중간 상태 캡처 위험이 있다. 현재 코드 경로 확인이며 이번 세션 재현 테스트는 미실행.
4. 파티션 간 아이템 이동/보상과 저장 실패의 롤백·중복 방지는 별도 테스트가 필요하다.

## 관리 도구

Tools/Board Chase/Save Inspector가 settings/profile/run과 백업을 읽는 관리 창이다. Debug Monitor의 창고/런 조작과 용도를 구별한다. 테스트 전 현재 실세이브를 별도 보존하고 폐기 가능한 프로필로 검증한다.

## 승인 테스트

빈 저장 → 첫 실행, 구버전 이전, 잘린 본파일 → 백업 복원, 백업 일부 파손, 미지 아이템 GUID, 가득 찬 창고 이동, 사망 직후 강제 종료, 발전기 배터리 소비 직후 재시작, 상자 수령 중 중단, 퀘스트 보상 중 중단을 각각 확인한다. 이번 감사에서 실세이브를 지우거나 고의 손상하지 않았다.

## 코드·데이터 근거

- `E:/github/board_chase/board_chase/Assets/Scripts/Core/Persistence/SaveSystem.cs`
- `E:/github/board_chase/board_chase/Assets/Scripts/Core/Persistence/RunLifecycleCoordinator.cs`
- `E:/github/board_chase/board_chase/Assets/Scripts/Core/Persistence/PlayerRunStatePersistence.cs`
- `E:/github/board_chase/board_chase/Assets/Scripts/Gameplay/World/Sectors/StageMap/RaidRuntime.cs`
- `E:/github/board_chase/board_chase/Assets/Editor/SaveInspectorWindow.cs`
- `E:/github/board_chase/board_chase/Assets/Resources/RuntimeContentCatalog.asset`