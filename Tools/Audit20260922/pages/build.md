# 기술 B. Unity·빌드·자산 연결 문제와 근거

검사 기준: 2026-09-22 / E:/github/board_chase/board_chase. 현재 파일의 정적 연결·핵심 실행 경로 기준이며, 별도 표시 없는 항목은 이번 감사에서 플레이 테스트한 결과가 아닙니다.

## 확인한 환경

Unity 6000.0.47f1, URP 17.0.4, Input System 1.14.2, Localization 1.5.9, AI Navigation 2.0.12, Cinemachine 3.1.4, Test Framework 1.5.1. Easy Save 3는 프로젝트 공급사 코드로 포함된다. 버전은 현재 파일 기준이며 과거 Notion 숫자를 복사하지 않았다.

## 우선순위 1 — 부팅/빌드

EditorBuildSettings에는 Initialization/RUNScene/PersistenceManager/GamePlay/GameOver 5개가 있고 모두 enabled:0이다. 기본 Build Settings 기준 활성 부팅 씬이 없다. 별도 빌드 스크립트가 씬을 명시하는 예외를 확인하지 않은 채 출시 가능하다고 결론내리지 않는다.

메뉴 Addressables 그룹에는 Assets/Scenes/MainMenuScene.unity 주소와 GUID 93f36624d85896646a4a01afa034899e 항목이 남아 있지만 현재 메뉴 파일은 MainMenu.unity다. 메뉴 SceneSO/Addressable 엔트리/실제 GUID를 한 세트로 복구하고 독립 Player에서 부팅해야 한다.

## 우선순위 1 — 런타임 콘텐츠 카탈로그

Assets/Resources/RuntimeContentCatalog.asset 16행에 '+- {fileID: ...}' 형태의 잘못된 YAML 줄이 실제 존재한다. 컴파일러는 .asset 문법을 검사하지 않으므로 C# 컴파일 성공과 양립한다.

같은 카탈로그에 25개의 미해결 GUID 참조 쌍이 있다. 예: 59d36ae99b6e4d539f72ccff0dc92d7e. 이 GUID는 Player.prefab의 equippedBackpack과 초기 로드아웃에도 남아 있다. 8261ecedbebf48c4ebbde438e62a742b도 초기 로드아웃/카탈로그에서 해석되지 않는다. 카탈로그 재구성과 항목별 실제 타입/저장 호환 확인이 필요하다.

HomeShopConfig는 a8c7bc4bfdd34874cb8cea35a09a004a, 75e18ec3889747049b9209e8710ab651 미해결 참조가 있다. 모든 누락 항목을 임의의 다른 아이템으로 대체하지 않는다. 원래 의도와 저장 ID를 확인한다.

## 우선순위 2 — 렌더링/임포트

DefaultVolumeProfile의 CopyPasteTestComponent1/2/3, VolumeComponentSupportedOnAnySRP 등 테스트/에디터 타입과 fileID 0, 미해결 GUID 4개를 확인했다. URP 프로필의 유효 런타임 컴포넌트만 남기는 별도 정리가 필요하다.

열린 Unity Editor 로그 스냅샷에는 폴더 meta YAML 오류와 EditorUpdateCheck 네트워크 오류가 있었다. 하지만 현재 확인한 일부 해당 meta 본문은 정상 형태였고 재임포트는 하지 않았다. 과거 로그 기록을 현재 모든 파일이 계속 깨졌다는 확정으로 쓰지 않는다. 새 Refresh/임포트의 최신 로그 시점을 기준으로 재검증한다.

## 미해결 GUID 수의 해석

전체 1,433쌍은 파일별 고유 GUID 참조 수다. Vendor 데모, WIP, 이전 머티리얼 업그레이더 데이터가 포함되어 있으므로 일괄 자동 삭제/교체하면 안 된다. 실행 루트·Resources·Addressables의 기능 필드부터 우선 분류한다.

## Git 기준선

감사 시작부터 다수의 삭제/수정/미추적 상태가 있었다. 이를 이번 감사 변경으로 오인하거나 reset/clean으로 정리하지 않았다. 안정된 커밋/백업 기준점을 먼저 확보하고 기능 수정은 작은 단위로 검증해야 한다.

## 검증의 한계

Assembly-CSharp와 Editor 컴파일은 통과했다. Player 빌드·Addressables 콘텐츠 빌드·전 자산 재임포트는 미실행이다. 부팅/카탈로그 문제를 해결하기 전 ‘시스템 전체 완료’ 또는 퍼센트 완료율을 승인 지표로 삼지 않는다.

## 코드·데이터 근거

- `E:/github/board_chase/board_chase/ProjectSettings/EditorBuildSettings.asset`
- `E:/github/board_chase/board_chase/ProjectSettings/ProjectVersion.txt`
- `E:/github/board_chase/board_chase/Packages/manifest.json`
- `E:/github/board_chase/board_chase/Assets/Resources/RuntimeContentCatalog.asset`
- `E:/github/board_chase/board_chase/Assets/Prefabs/GameScene/Shooter/Player/Player.prefab`
- `E:/github/board_chase/board_chase/Assets/ScriptableObject/GameScene/Character/Shooter/Player/Config/PlayerBasicShooterLoadout.asset`
- `E:/github/board_chase/board_chase/Assets/ScriptableObject/GameScene/Shop/HomeShopConfig.asset`
- `E:/github/board_chase/board_chase/Assets/Settings/DefaultVolumeProfile.asset`