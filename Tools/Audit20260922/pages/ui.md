# 07. UI·입력·설정·한국어/영어

검사 기준: 2026-09-22 / E:/github/board_chase/board_chase. 현재 파일의 정적 연결·핵심 실행 경로 기준이며, 별도 표시 없는 항목은 이번 감사에서 플레이 테스트한 결과가 아닙니다.

## UI 씬 소유권

GamePlay는 HUD·인벤토리·오버레이 등 공통 UI/게임 관리 씬이다. RUNScene은 월드·섹터·스폰·진행 관리 역할이다. 정렬 버튼을 RUNScene 루트에 반복 생성하는 방식은 사용하지 않는다. 현재 인벤토리 주머니/가방 헤더의 Compact/Rarity 버튼과 연결 컴포넌트를 기준으로 한다.

UIOverlayManager/Panel은 열림 순서, 입력 차단과 화면 상태를 조율한다. 아이템 상세·상점·적 루팅·퀘스트 패널은 같은 게임 상태를 각각 임의 저장하는 것이 아니라 이벤트/런타임 데이터를 표시하고 요청한다.

## 현재 설정 탭

- 일반: 언어, 해상도, 화면 모드, 조준 감도.
- 오디오: Master/Music/SFX.
- 그래픽: VSync, 프레임 제한. 현재 UISettingGraphicsComponent의 프레임 선택은 30/60/90/120/144/165/240이며 VSync가 켜지면 targetFrameRate=-1 경로를 쓴다.
- 키 설정: 이동 4방향, Attack, Paint, Dash, Interact, UpgradePanel, QTEInteract, BulletSlot1/2 등 현재 enum·입력 액션 연결.

저장 구조에 AA/Shadow 값이 있다고 설정 UI에서 둘 다 선택할 수 있다고 쓰지 않는다. Paint/UpgradePanel 명칭도 현재 기능 노출 적합성을 다시 정리할 대상이다. SettingsSystem과 SettingsSO, UI 컴포넌트의 책임을 구별한다.

## 현지화 구조

Unity Localization StringTable + UIRuntimeLocalization의 런타임 적용/캐시/대체 문자열을 사용한다. 폰트 기준은 NEXONLv1GothicRegular SDF다. ItemSO 표와 아이템 설명 완성도는 일반 UI 표와 별도 범위로 본다.

정적 KO/EN 표 검사에서 대표 표의 엔트리 수는 각각 다음과 같다: Inventory 75, Settings 43, PlayerTimer 17, Map 19, MainMenu 7, Pause 6, PlayerInfo 6, Popup 12, Ammo 1. 읽힌 이 엔트리 안에서는 빈 m_Localized가 발견되지 않았다. 이것은 화면의 모든 문장이 테이블에 등록됐다는 검증이 아니다.

UI_Settings_General은 같은 SharedData를 참조하는 중첩 경로의 빈 표와 상위 경로의 KO 3/EN 4개 표가 함께 있다. 실제 선택되는 TableCollection/Addressables 연결과 키 대응을 정리해야 한다. 중복 존재만으로 무조건 실행 오류라고 단정하지 않는다.

QuestDefinition의 요약 생성, 로딩/안내 문자열 등에 하드코딩 문자열이 남아 있다. 한국어/영어 전체 100% 완료로 표시하지 않는다. 폰트 글리프, 긴 영어 줄바꿈, 동적 수량/시간 포맷, 드롭다운 항목도 실제 화면 검사가 필요하다.

## 입력

InputReader가 플레이/메뉴 액션과 이벤트, 키 바인딩 표시 및 저장된 override 재적용을 담당한다. 재바인딩 완료 시 UI 표시뿐 아니라 실제 사용하는 액션 인스턴스에 반영되는지 확인한다. 메뉴가 켜진 상태에서 사격/소비가 중복 실행되지 않아야 한다.

## 승인 체크

Home/RUN/일시정지 각각 설정 열기, 언어 실시간 변경과 재시작 유지, 한국어 폰트, 오디오 실제 믹서 값, 프레임/VSync, 키 변경/복원, 인벤토리 정렬 대상, 중복 버튼/리스너, 패널 닫힌 후 조작 복귀를 확인한다.

## 코드·데이터 근거

- `E:/github/board_chase/board_chase/Assets/Scripts/Presentation/UI/Common/ESCMenu/UISettingsController.cs`
- `E:/github/board_chase/board_chase/Assets/Scripts/Presentation/UI/Common/ESCMenu/UISettingGraphicsComponent.cs`
- `E:/github/board_chase/board_chase/Assets/Scripts/Presentation/UI/Common/ESCMenu/SettingsSystem.cs`
- `E:/github/board_chase/board_chase/Assets/Scripts/Presentation/UI/Common/UIRuntimeLocalization.cs`
- `E:/github/board_chase/board_chase/Assets/Scripts/Core/Input/InputReader.cs`
- `E:/github/board_chase/board_chase/Assets/Scripts/Presentation/UI/Gameplay/Overlay/UIOverlayManager.cs`
- `E:/github/board_chase/board_chase/Assets/Editor/InventorySortButtonMigration.cs`
- `E:/github/board_chase/board_chase/Assets/Tests/EditMode/Editor/InventorySortUIOwnershipTests.cs`