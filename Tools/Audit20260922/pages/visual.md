# 09. Shooter 비주얼·장비 소켓·미연결 상태

검사 기준: 2026-09-22 / E:/github/board_chase/board_chase. 현재 파일의 정적 연결·핵심 실행 경로 기준이며, 별도 표시 없는 항목은 이번 감사에서 플레이 테스트한 결과가 아닙니다.

## 요청한 소켓 정책과 코드

Hair는 Face와 동급이다. Face는 안면 Eyes+Mouth 영역을 가리는 상위 점유이며 Hair를 자동으로 숨기지 않는다. Face 장비를 빼면 하위 Eyes/Mouth 비주얼이 다시 나타나야 한다. 전면 가스마스크는 Face, 입 호흡기는 Mouth, 머리카락은 Hair로 지정하는 정책이다.

ShooterVisualSocketId에는 Armor, RightHand, LeftHand, Hair, Eyes, Back, Mask, Mouth, Face, Body, Feet가 있다. Head 소켓 enum은 없다. 별도 점유 마스크의 Head 비트나 호환 이름 탐색이 남은 것을 실제 Head 소켓 존재로 혼동하지 않는다.

ShooterVisualRig는 소켓 탐색·장착 비주얼·점유/숨김·기본 비주얼 복귀를 담당한다. ShooterCustomizeSO는 외형 설정, ShooterVisualBodyProfileSO는 바디 표시 설정, ShooterEquipmentController는 실제 장착 상태와 표시 연결을 맡는다. 외형 스케일과 게임 판정 collider 크기는 구별한다.

## 이번 감사의 중요한 결과

Assets의 직렬화 검사에서 ShooterVisualRig 스크립트 참조 자산은 0개다. 런타임 코드에서 AddComponent<ShooterVisualRig> 생성도 발견하지 못했다. 장착 컨트롤러는 기존 리그를 GetComponentInChildren으로 찾는 경로다.

따라서 ‘Capsule의 모든 새 소켓이 실제로 세팅되어 사용 중’이라고 쓰지 않는다. Customize 데이터 자산은 존재하지만 데이터의 존재와 씬/프리팹 장착은 다르다. BodyProfile 자산도 현재 검사 범위에서 생성된 실사용 자료를 확인하지 못했다. 기존 무기/방탄 장착용 경로까지 전부 없다는 의미는 아니다.

## 필요한 다음 작업

1. 실제 플레이어/적 Capsule 프리팹에서 기존 방탄 위치와 몸체 기준을 확인한다.
2. Hair/Eyes/Mouth/Face/Body/Feet anchor를 잡고 리그 컴포넌트를 연결한다.
3. 장비 SO별 소켓/점유/보정 값을 작성한다.
4. Face와 Eyes/Mouth 중복 장착, 해제 복귀, Hair 독립성을 실제 화면으로 검증한다.
5. 무기 손 소켓, 기존 Armor, Body 외형과 저장 복원을 함께 검사한다.

가스마스크 렌즈·머티리얼·형상 품질은 모델/렌더링 작업이다. 코드만 바꾸거나 콘셉트 이미지를 만든 것을 Unity 적용 완료로 기록하지 않는다. 현재 GasMaskLV3 자산의 미해결 비주얼 참조도 먼저 정리해야 한다.

## 편집 도구

Shooter Customize Station은 이 데이터를 편집/미리보기하는 도구다. 도구가 있다고 모든 프리팹에 자동으로 리그가 생기는 것은 아니다. 소켓 Gizmo와 실제 프리팹 저장 상태를 승인 근거로 남긴다.

## 코드·데이터 근거

- `E:/github/board_chase/board_chase/Assets/Scripts/Gameplay/Entities/Shooter/Common/Visuals/ShooterVisualSocketId.cs`
- `E:/github/board_chase/board_chase/Assets/Scripts/Gameplay/Entities/Shooter/Common/Visuals/ShooterVisualRig.cs`
- `E:/github/board_chase/board_chase/Assets/Scripts/Gameplay/Entities/Shooter/Common/Visuals/ShooterCustomizeSO.cs`
- `E:/github/board_chase/board_chase/Assets/Scripts/Gameplay/Entities/Shooter/Common/Visuals/ShooterVisualBodyProfileSO.cs`
- `E:/github/board_chase/board_chase/Assets/Scripts/Gameplay/Entities/Shooter/Common/Core/ShooterEquipmentController.cs`
- `E:/github/board_chase/board_chase/Assets/Editor/ShooterCustomizeStationWindow.cs`